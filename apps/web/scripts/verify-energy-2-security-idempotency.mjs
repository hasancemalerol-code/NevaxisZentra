#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "ENERGY2-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`ENERGY-2 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(MANAGER)) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM energy_intelligence_drivers
    WHERE assessment_id IN (
      SELECT id FROM energy_intelligence_assessments
      WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR assessment_reference LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM energy_audit_events
    WHERE entity_id IN (
      SELECT id FROM energy_intelligence_assessments
      WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR assessment_reference LIKE '${CLOSURE_PREFIX}%'
      UNION SELECT id FROM energy_targets
      WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR target_reference LIKE '${CLOSURE_PREFIX}%'
      UNION SELECT id FROM energy_baseline_profiles
      WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM energy_intelligence_assessments
    WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR assessment_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM energy_targets
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR target_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM energy_baseline_profiles
    WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running ENERGY-2 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Energy/EnergyIntelligenceEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Energy/EnergyEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260819150759_AddEnergyIntelligenceFoundation.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/Energy/EnergyPermissionKeys.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("intelligence workspace route", endpoints.includes("/intelligence/workspace"));
assert("baselines evaluate route", endpoints.includes("/baselines/evaluate"));
assert("assessments evaluate route", endpoints.includes("/assessments/evaluate"));
assert("energy.read reused", permissions.includes("energy.read"));
assert("energy.manage reused", permissions.includes("energy.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
forbidText(migration, "GRANT DELETE", "migration grants");
forbidText(endpoints, "MapDelete", "intelligence endpoints");

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(READONLY);
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const targetsBefore = Number(db("SELECT COUNT(*) FROM energy_targets"));
const assessmentsBefore = Number(db("SELECT COUNT(*) FROM energy_intelligence_assessments"));
const baselinesBefore = Number(db("SELECT COUNT(*) FROM energy_baseline_profiles"));

const noAuth = await fetch(`${BASE}/api/energy/intelligence/workspace?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/energy/targets?facilityId=${FACILITY}`,
  {
    targetReference: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
    targetScope: "measurement-point",
    scopeReference: "EM-INJ-001",
    targetMetric: "consumed-kwh",
    targetValue: 10,
    unit: "kWh",
    tolerance: 0.1,
    effectiveFromUtc: "2026-01-01T00:00:00Z",
    effectiveToUtc: "2027-01-01T00:00:00Z",
    sourceProvenance: "closure-test",
    idempotencyKey: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("readonly mutation 403", readonlyWrite.status === 403);

const wrongFacility = await liveRequest(
  "GET",
  `/api/energy/intelligence/workspace?facilityId=${UNAUTH_FACILITY}`,
  null,
  managerHeaders,
);
assert("wrong facility 403", wrongFacility.status === 403);

const crossTenant = await liveRequest(
  "GET",
  `/api/energy/intelligence/workspace?facilityId=${FACILITY}`,
  null,
  crossTenantHeaders,
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403);

const targetsAfterDeny = Number(db("SELECT COUNT(*) FROM energy_targets"));
const assessmentsAfterDeny = Number(db("SELECT COUNT(*) FROM energy_intelligence_assessments"));
const baselinesAfterDeny = Number(db("SELECT COUNT(*) FROM energy_baseline_profiles"));
assert("denied requests created no targets", targetsAfterDeny === targetsBefore);
assert("denied requests created no assessments", assessmentsAfterDeny === assessmentsBefore);
assert("denied requests created no baselines", baselinesAfterDeny === baselinesBefore);

const authorizedRead = await liveRequest(
  "GET",
  `/api/energy/baselines?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("authorized baselines read 200", authorizedRead.status === 200);

const targetRef = `${CLOSURE_PREFIX}-target-${RUN_SUFFIX}`;
const idemKey = `${CLOSURE_PREFIX}-create-${RUN_SUFFIX}`;
const targetBody = {
  targetReference: targetRef,
  targetScope: "measurement-point",
  scopeReference: "EM-INJ-002",
  targetMetric: "consumed-kwh",
  targetValue: 12,
  unit: "kWh",
  tolerance: 0.1,
  effectiveFromUtc: "2026-01-01T00:00:00Z",
  effectiveToUtc: "2027-01-01T00:00:00Z",
  sourceProvenance: "closure-test",
  idempotencyKey: idemKey,
};
const firstTarget = await liveRequest("POST", `/api/energy/targets?facilityId=${FACILITY}`, targetBody, managerHeaders);
assert("authorized target create 201", firstTarget.status === 201);
const replayTarget = await liveRequest("POST", `/api/energy/targets?facilityId=${FACILITY}`, targetBody, managerHeaders);
assert("target idempotent replay 200", replayTarget.status === 200 && replayTarget.json?.idempotentReplay === true);

const conflictTarget = await liveRequest(
  "POST",
  `/api/energy/targets?facilityId=${FACILITY}`,
  { ...targetBody, targetValue: 99 },
  managerHeaders,
);
assert("target payload conflict 409", conflictTarget.status === 409);
forbidText(conflictTarget.text ?? "", "Npgsql", "conflict response");

const staleUpdate = await liveRequest(
  "PUT",
  `/api/energy/targets/${encodeURIComponent("TGT-CONSUMPTION-INJ-001")}?facilityId=${FACILITY}`,
  {
    targetValue: 44,
    tolerance: 0.15,
    effectiveFromUtc: "2026-01-01T00:00:00Z",
    effectiveToUtc: "2027-01-01T00:00:00Z",
    sourceProvenance: "closure-stale-test",
    status: "approved",
    expectedVersion: 999,
  },
  managerHeaders,
);
assert("stale version conflict 409", staleUpdate.status === 409);

const auditCount = Number(db(`
  SELECT COUNT(*) FROM energy_audit_events
  WHERE event_type LIKE 'energy.intelligence.%'
`));
assert("intelligence audit events exist", auditCount > 0);

removeClosureFixtures();
const closureRemaining = Number(db(`
  SELECT COUNT(*) FROM energy_targets WHERE target_reference LIKE '${CLOSURE_PREFIX}%'
`));
assert("closure fixtures removed", closureRemaining === 0);

console.log("ENERGY-2 security/idempotency verification passed.");

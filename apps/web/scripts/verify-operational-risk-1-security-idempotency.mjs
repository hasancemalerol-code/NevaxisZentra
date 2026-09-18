#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "OPERATIONAL-RISK1-CLOSURE";
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
  console.error(`OPERATIONAL-RISK-1 security/idempotency verification failed: ${message}`);
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
    DELETE FROM operational_risk_audit_events
    WHERE entity_id IN (
      SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
      UNION SELECT id FROM operational_risk_evidence WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM operational_risk_status_history
    WHERE case_id IN (SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_evidence
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR case_id IN (SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running OPERATIONAL-RISK-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/OperationalRisk/OperationalRiskEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/OperationalRisk/OperationalRiskEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260823113228_AddOperationalRiskFoundation.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/OperationalRisk/OperationalRiskPermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("manage access helper", helpers.includes("ResolveManageAccessAsync"));
assert("operational-risk.read key", permissions.includes("operational-risk.read"));
assert("operational-risk.manage key", permissions.includes("operational-risk.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE operational_risk_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE operational_risk_audit_events", "audit events");
forbidText(endpoints, "MapDelete", "operational risk endpoints");
forbidText(endpoints, "ai-task", "operational risk endpoints");
forbidText(endpoints, "notification", "operational risk endpoints");
forbidText(endpoints, "remediation", "operational risk endpoints");
forbidText(endpoints, "machine-command", "operational risk endpoints");

assert("bootstrap read seed", bootstrap.includes("EnsureOperationalRiskReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureOperationalRiskManagePermissionAssignmentAsync"));

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

const casesBeforeDeny = Number(db("SELECT COUNT(*) FROM operational_risk_cases"));
const evidenceBeforeDeny = Number(db("SELECT COUNT(*) FROM operational_risk_evidence"));

const noAuth = await fetch(`${BASE}/api/operational-risks/cases?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/operational-risks/cases?facilityId=${FACILITY}`,
  {
    category: "other",
    scopeType: "facility",
    title: "Readonly blocked",
    description: "Should fail",
    evidenceState: "simulated",
    sourceProvenance: "closure",
    firstDetectedAtUtc: new Date().toISOString(),
    lastObservedAtUtc: new Date().toISOString(),
    idempotencyKey: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("readonly write 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/operational-risks/cases?facilityId=${UNAUTH_FACILITY}`,
  null,
  managerHeaders,
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/operational-risks/cases?facilityId=${FACILITY}`,
  null,
  crossTenantHeaders,
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const casesAfterDeny = Number(db("SELECT COUNT(*) FROM operational_risk_cases"));
const evidenceAfterDeny = Number(db("SELECT COUNT(*) FROM operational_risk_evidence"));
assert("denied requests created no cases", casesAfterDeny === casesBeforeDeny, `${casesBeforeDeny}→${casesAfterDeny}`);
assert("denied requests created no evidence", evidenceAfterDeny === evidenceBeforeDeny, `${evidenceBeforeDeny}→${evidenceAfterDeny}`);

const provenance = "Deterministic local Development operational-risk data — simulated, non-production, not real customer evidence or guaranteed loss.";
const now = new Date().toISOString();
const idemKey = `${CLOSURE_PREFIX}-case-${RUN_SUFFIX}`;
const caseBody = {
  category: "cost-deviation",
  scopeType: "facility",
  title: "Closure security case",
  description: "Security closure",
  evidenceState: "simulated",
  likelihoodLevel: 2,
  impactLevel: 2,
  sourceProvenance: provenance,
  firstDetectedAtUtc: now,
  lastObservedAtUtc: now,
  idempotencyKey: idemKey,
};
const firstCase = await liveRequest("POST", `/api/operational-risks/cases?facilityId=${FACILITY}`, caseBody, managerHeaders);
assert("authorized create 201", firstCase.status === 201, firstCase.text?.slice(0, 200));
const replayCase = await liveRequest("POST", `/api/operational-risks/cases?facilityId=${FACILITY}`, caseBody, managerHeaders);
assert("identical idempotent replay 200", replayCase.status === 200);
assert("identical replay flag", replayCase.json?.idempotentReplay === true);

const conflictCase = await liveRequest(
  "POST",
  `/api/operational-risks/cases?facilityId=${FACILITY}`,
  { ...caseBody, title: "Different payload" },
  managerHeaders,
);
assert("different payload conflict 409", conflictCase.status === 409, `status=${conflictCase.status}`);
forbidText(conflictCase.text ?? "", "Npgsql", "conflict response");
forbidText(conflictCase.text ?? "", "password", "conflict response");

const staleAssessment = await liveRequest(
  "POST",
  `/api/operational-risks/cases/${firstCase.json.id}/assessment?facilityId=${FACILITY}`,
  {
    expectedVersion: 999,
    evidenceState: "simulated",
    likelihoodLevel: 3,
    impactLevel: 3,
  },
  managerHeaders,
);
assert("stale expected version 409", staleAssessment.status === 409, `status=${staleAssessment.status}`);

removeClosureFixtures();
console.log("OPERATIONAL-RISK-1 security/idempotency verification passed.");

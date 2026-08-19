#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "ENERGY1-CLOSURE";
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
  console.error(`ENERGY-1 security/idempotency verification failed: ${message}`);
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
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
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
    DELETE FROM energy_allocations
    WHERE consumption_window_id IN (
      SELECT id FROM energy_consumption_windows WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM energy_audit_events
    WHERE entity_id IN (
      SELECT id FROM energy_consumption_windows WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'
      UNION SELECT id FROM energy_readings WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM energy_consumption_windows WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM energy_readings WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running ENERGY-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Energy/EnergyEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Energy/EnergyEndpointHelpers.cs");
const migration = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260819140015_AddEnergyMeasurementFoundation.cs");
const permissions = readRequired("packages/CrevaFactOS.Persistence/Energy/EnergyPermissionKeys.cs");
const bootstrap = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("write access helper", helpers.includes("ResolveWriteAccessAsync"));
assert("energy.read key", permissions.includes("energy.read"));
assert("energy.manage key", permissions.includes("energy.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE energy_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE energy_audit_events", "audit events");
forbidText(endpoints, "MapDelete", "energy endpoints");

assert("bootstrap read seed", bootstrap.includes("EnsureEnergyReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureEnergyManagePermissionAssignmentAsync"));

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(READONLY);
const negativeHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: "quality-negative-test-user",
  tenant: "dev-pilot-tenant",
});
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const readingsBeforeDeny = Number(db("SELECT COUNT(*) FROM energy_readings"));
const windowsBeforeDeny = Number(db("SELECT COUNT(*) FROM energy_consumption_windows"));

const noAuth = await fetch(`${BASE}/api/energy/workspace?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/energy/readings?facilityId=${FACILITY}`,
  {
    measurementPointReference: "EM-INJ-001",
    occurredAtUtc: new Date().toISOString(),
    intervalEnergyKwh: 1,
    sourceType: "closure-test",
    idempotencyKey: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("readonly write 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/energy/workspace?facilityId=${UNAUTH_FACILITY}`,
  null,
  managerHeaders,
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/energy/workspace?facilityId=${FACILITY}`,
  null,
  crossTenantHeaders,
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const readingsAfterDeny = Number(db("SELECT COUNT(*) FROM energy_readings"));
const windowsAfterDeny = Number(db("SELECT COUNT(*) FROM energy_consumption_windows"));
assert("denied requests created no readings", readingsAfterDeny === readingsBeforeDeny, `${readingsBeforeDeny}→${readingsAfterDeny}`);
assert("denied requests created no windows", windowsAfterDeny === windowsBeforeDeny, `${windowsBeforeDeny}→${windowsAfterDeny}`);

const authorizedRead = await liveRequest(
  "GET",
  `/api/energy/measurement-points?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("authorized read 200", authorizedRead.status === 200);

const idemKey = `${CLOSURE_PREFIX}-reading-${RUN_SUFFIX}`;
const readingBody = {
  measurementPointReference: "EM-INJ-001",
  occurredAtUtc: new Date("2026-08-18T08:00:00.000Z").toISOString(),
  intervalEnergyKwh: 2.5,
  sourceType: "closure-test",
  evidenceState: "simulated",
  idempotencyKey: idemKey,
};
const firstReading = await liveRequest("POST", `/api/energy/readings?facilityId=${FACILITY}`, readingBody, managerHeaders);
assert("authorized write 201", firstReading.status === 201, firstReading.text?.slice(0, 200));
const replayReading = await liveRequest("POST", `/api/energy/readings?facilityId=${FACILITY}`, readingBody, managerHeaders);
assert("identical idempotent replay 200", replayReading.status === 200);
assert("identical replay flag", replayReading.json?.idempotentReplay === true);

const conflictReading = await liveRequest(
  "POST",
  `/api/energy/readings?facilityId=${FACILITY}`,
  { ...readingBody, intervalEnergyKwh: 9.9 },
  managerHeaders,
);
assert("different payload conflict 409", conflictReading.status === 409, `status=${conflictReading.status}`);
forbidText(conflictReading.text ?? "", "Npgsql", "conflict response");
forbidText(conflictReading.text ?? "", "Exception", "conflict response");

const evalKey = `${CLOSURE_PREFIX}-eval-${RUN_SUFFIX}`;
const evalBody = {
  measurementPointReference: "EM-INJ-001",
  windowStartUtc: new Date("2026-08-18T12:00:00.000Z").toISOString(),
  windowEndUtc: new Date("2026-08-18T14:00:00.000Z").toISOString(),
  idempotencyKey: evalKey,
};
const firstEval = await liveRequest("POST", `/api/energy/consumption/evaluate?facilityId=${FACILITY}`, evalBody, managerHeaders);
assert("evaluate write 200", firstEval.status === 200);
const replayEval = await liveRequest("POST", `/api/energy/consumption/evaluate?facilityId=${FACILITY}`, evalBody, managerHeaders);
assert("evaluate idempotent replay", replayEval.status === 200 && replayEval.json?.idempotentReplay === true);

const evalConflict = await liveRequest(
  "POST",
  `/api/energy/consumption/evaluate?facilityId=${FACILITY}`,
  {
    ...evalBody,
    windowEndUtc: new Date("2026-08-18T15:00:00.000Z").toISOString(),
  },
  managerHeaders,
);
assert("evaluate payload conflict 409", evalConflict.status === 409);

removeClosureFixtures();
const closureRemaining = Number(db(`
  SELECT COUNT(*) FROM energy_readings WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
`));
assert("closure fixtures removed", closureRemaining === 0, `count=${closureRemaining}`);

console.log("ENERGY-1 security/idempotency verification passed.");

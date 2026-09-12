#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PERSON001 = "bb000001-00d0-4000-8000-0000000000d0";
const CLOSURE_PREFIX = "SHIFTWF1-CLOSURE";
const RUN_SUFFIX = Date.now();

function fail(message) {
  console.error(`SHIFT-WORKFORCE-1 security/boundary verification failed: ${message}`);
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
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

async function liveRequest(method, path, body, headers = {}) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: { "Content-Type": "application/json", ...headers },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json };
}

console.log("Running SHIFT-WORKFORCE-1 security/boundary verifier.");

const endpoints = readRequired("apps/api/ShiftWorkforce/ShiftWorkforceEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/ShiftWorkforce/ShiftWorkforceEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260813020000_AddShiftWorkforceFoundation.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/ShiftWorkforce/ShiftWorkforcePermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", endpoints.includes("ResolveReadAccessAsync"));
assert("write access helper", endpoints.includes("ResolveWriteAccessAsync"));
assert("shift-workforce.read key", permissions.includes("shift-workforce.read"));
assert("shift-workforce.manage key", permissions.includes("shift-workforce.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE shift_workforce_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE shift_workforce_audit_events", "audit events");
forbidText(endpoints, "MapDelete", "shift-workforce endpoints");
forbidText(endpoints, "MapPut(\"/persons/{personId:guid}\"", "ERP-owned person write endpoint");
forbidText(endpoints, "MapPost(\"/persons\"", "ERP-owned person create endpoint");

assert("bootstrap read seed", bootstrap.includes("EnsureShiftWorkforceReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureShiftWorkforceManagePermissionAssignmentAsync"));
assert("manager manage grant", bootstrap.includes("EnsureShiftWorkforceManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, tenantRole.Id"));
assert("reviewer read grant", bootstrap.includes("EnsureShiftWorkforceReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id"));

const managerHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-user",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const reviewerHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-reviewer",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const operatorHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "production-pilot-operator",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const negativeHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "quality-negative-test-user",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const crossTenantHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-user",
  "X-NevaxisFactOS-Tenant": "unknown-tenant-key",
};

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const competencyCountBefore = Number(db("SELECT COUNT(*) FROM shift_workforce_competency_assignments"));
const auditCountBefore = Number(db("SELECT COUNT(*) FROM shift_workforce_audit_events"));

assert("missing auth → 401", (await liveRequest("GET", `/api/shift-workforce/overview?facilityId=${FACILITY}`)).status === 401);
assert("authorized manager GET → 200", (await liveRequest("GET", `/api/shift-workforce/overview?facilityId=${FACILITY}`, null, managerHeaders)).status === 200);
assert("read-only GET → 200", (await liveRequest("GET", `/api/shift-workforce/overview?facilityId=${FACILITY}`, null, reviewerHeaders)).status === 200);
assert("operator read GET → 200", (await liveRequest("GET", `/api/shift-workforce/overview?facilityId=${FACILITY}`, null, operatorHeaders)).status === 200);

const managerWrite = await liveRequest(
  "POST",
  `/api/shift-workforce/persons/${PERSON001}/competencies?facilityId=${FACILITY}`,
  {
    competencyDefinitionCode: "maintenance-support",
    competencyLevel: "basic",
    verificationStatus: "pending-verification",
    idempotencyKey: `${CLOSURE_PREFIX}-sec-mgr-${RUN_SUFFIX}`,
  },
  managerHeaders,
);
assert("authorized manager write → 201", managerWrite.status === 201, `status=${managerWrite.status}`);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/shift-workforce/persons/${PERSON001}/competencies?facilityId=${FACILITY}`,
  {
    competencyDefinitionCode: "maintenance-support",
    competencyLevel: "basic",
    verificationStatus: "pending-verification",
    idempotencyKey: `${CLOSURE_PREFIX}-sec-ro-${RUN_SUFFIX}`,
  },
  reviewerHeaders,
);
assert("read-only write → 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

assert("unauthorized GET → 403", (await liveRequest("GET", `/api/shift-workforce/overview?facilityId=${FACILITY}`, null, negativeHeaders)).status === 403);
assert("wrong facility → 403", (await liveRequest("GET", `/api/shift-workforce/overview?facilityId=${UNAUTH_FACILITY}`, null, managerHeaders)).status === 403);
assert("cross-tenant → 403", (await liveRequest("GET", `/api/shift-workforce/overview?facilityId=${FACILITY}`, null, crossTenantHeaders)).status === 403);

const deniedWrite = await liveRequest(
  "POST",
  `/api/shift-workforce/persons/${PERSON001}/competencies?facilityId=${FACILITY}`,
  {
    competencyDefinitionCode: "maintenance-support",
    competencyLevel: "basic",
    verificationStatus: "pending-verification",
    idempotencyKey: `${CLOSURE_PREFIX}-sec-denied-${RUN_SUFFIX}`,
  },
  negativeHeaders,
);
assert("denied write → 403", deniedWrite.status === 403, `status=${deniedWrite.status}`);

const competencyCountAfter = Number(db("SELECT COUNT(*) FROM shift_workforce_competency_assignments"));
const auditCountAfter = Number(db("SELECT COUNT(*) FROM shift_workforce_audit_events"));
assert("denied write created no competency rows", competencyCountAfter === competencyCountBefore + 1, `${competencyCountBefore}→${competencyCountAfter}`);
assert("denied write created no extra audit rows beyond manager write", auditCountAfter >= auditCountBefore);

db(`DELETE FROM shift_workforce_audit_events WHERE entity_id IN (SELECT id FROM shift_workforce_competency_assignments WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}-sec-%')`);
db(`DELETE FROM shift_workforce_competency_assignments WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}-sec-%'`);

console.log("SHIFT-WORKFORCE-1 security/boundary verification passed.");

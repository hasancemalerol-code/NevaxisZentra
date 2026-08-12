#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";

function fail(message) {
  console.error(`PROCESS-OEE-1 security/boundary verification failed: ${message}`);
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

console.log("Running PROCESS-OEE-1 security/boundary verifier.");

const endpoints = readRequired("apps/api/ProcessOee/ProcessOeeEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/ProcessOee/ProcessOeeEndpointHelpers.cs");
const migration = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260812172702_AddProcessOeeFoundation.cs");
const permissions = readRequired("packages/CrevaFactOS.Persistence/Authorization/ProcessOeePermissionKeys.cs");
const bootstrap = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", endpoints.includes("ResolveReadAccessAsync"));
assert("write access helper", endpoints.includes("ResolveWriteAccessAsync"));
assert("401 mapping", helpers.includes("Status401Unauthorized") || helpers.includes("401"));
assert("403 mapping", helpers.includes("Status403Forbidden") || helpers.includes("403"));
assert("process-oee.read key", permissions.includes("process-oee.read"));
assert("process-oee.manage key", permissions.includes("process-oee.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
forbidText(migration, "GRANT DELETE ON TABLE process_oee_assessments", "assessments must not allow DELETE");
assert("append-only assessments grant", migration.includes("GRANT SELECT, INSERT ON TABLE process_oee_assessments"));
forbidText(endpoints, "MapDelete", "process-oee endpoints");

assert("bootstrap read seed", bootstrap.includes("EnsureProcessOeeReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureProcessOeeManagePermissionAssignmentAsync"));
assert("operator manage", bootstrap.includes("EnsureProcessOeeManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, operatorRole.Id"));
assert("reviewer read only", bootstrap.includes("EnsureProcessOeeReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id"));

async function liveRequest(method, path, body, headers = {}) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: { "Content-Type": "application/json", ...headers },
    body: body ? JSON.stringify(body) : undefined,
  });
  return response.status;
}

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const operatorHeaders = {
  "X-CrevaFactOS-Dev-Issuer": "crevafactos-dev",
  "X-CrevaFactOS-Dev-Subject": "production-pilot-operator",
  "X-CrevaFactOS-Tenant": "dev-pilot-tenant",
};
const reviewerHeaders = {
  "X-CrevaFactOS-Dev-Issuer": "crevafactos-dev",
  "X-CrevaFactOS-Dev-Subject": "planning-pilot-reviewer",
  "X-CrevaFactOS-Tenant": "dev-pilot-tenant",
};
const negativeHeaders = {
  "X-CrevaFactOS-Dev-Issuer": "crevafactos-dev",
  "X-CrevaFactOS-Dev-Subject": "quality-negative-test-user",
  "X-CrevaFactOS-Tenant": "dev-pilot-tenant",
};

assert("missing auth → 401", (await liveRequest("GET", `/api/process-oee/workspace-summary?facilityId=${FACILITY}`)) === 401);
assert("read-only GET → 200", (await liveRequest("GET", `/api/process-oee/workspace-summary?facilityId=${FACILITY}`, null, reviewerHeaders)) === 200);
assert(
  "read-only evaluate → 403",
  (await liveRequest(
    "POST",
    `/api/process-oee/evaluation-windows/bb000001-00c0-4000-8000-0000000000c0/evaluations?facilityId=${FACILITY}`,
    { idempotencyKey: "sec-ro-eval" },
    reviewerHeaders,
  )) === 403,
);
assert(
  "read-only window create → 403",
  (await liveRequest(
    "POST",
    `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
    {
      machineAssetId: "bb000001-0096-4000-8000-000000000096",
      windowStartUtc: "2026-08-10T06:00:00.000Z",
      windowEndUtc: "2026-08-10T14:00:00.000Z",
      idempotencyKey: "sec-ro-window",
    },
    reviewerHeaders,
  )) === 403,
);
assert("unauthorized actor → 403", (await liveRequest("GET", `/api/process-oee/workspace-summary?facilityId=${FACILITY}`, null, negativeHeaders)) === 403);
assert("unauthorized facility → 403", (await liveRequest("GET", `/api/process-oee/workspace-summary?facilityId=${UNAUTH_FACILITY}`, null, operatorHeaders)) === 403);

console.log("PROCESS-OEE-1 security/boundary verification passed.");

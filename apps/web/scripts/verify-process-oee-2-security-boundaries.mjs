#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PILOT_WINDOW = "bb000001-00c0-4000-8000-0000000000c0";
const PILOT_ASSESSMENT = "bb000001-00c3-4000-8000-0000000000c3";

function fail(message) {
  console.error(`PROCESS-OEE-2 security/boundary verification failed: ${message}`);
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

console.log("Running PROCESS-OEE-2 security/boundary verifier.");

const endpoints = readRequired("apps/api/ProcessOee/ProcessOeeEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/ProcessOee/ProcessOeeEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260812185022_AddProcessOeeLossDrivers.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/Authorization/ProcessOeePermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");
const entityConfig = readRequired("packages/NevaxisZentra.Persistence/Configurations/ProcessOeeEntityConfigurations.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", endpoints.includes("ResolveReadAccessAsync"));
assert("write access helper", endpoints.includes("ResolveWriteAccessAsync"));
assert("401 mapping", helpers.includes("Status401Unauthorized") || helpers.includes("401"));
assert("403 mapping", helpers.includes("Status403Forbidden") || helpers.includes("403"));
assert("process-oee.read key", permissions.includes("process-oee.read"));
assert("process-oee.manage key", permissions.includes("process-oee.manage"));
assert("losses read endpoint", endpoints.includes("/assessments/{assessmentId:guid}/losses"));
assert("loss overview endpoint", endpoints.includes("/loss-overview"));
assert("losses use read access", endpoints.includes("GetAssessmentLossesAsync"));
assert("RLS enabled on loss drivers", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS on loss drivers", migration.includes("FORCE ROW LEVEL SECURITY"));
forbidText(migration, "GRANT DELETE ON TABLE process_oee_loss_drivers", "loss drivers must not allow DELETE");
forbidText(migration, "GRANT UPDATE ON TABLE process_oee_loss_drivers", "loss drivers must not allow UPDATE");
assert("append-only loss drivers grant", migration.includes("GRANT SELECT, INSERT ON TABLE process_oee_loss_drivers"));
assert(
  "loss driver unique index",
  migration.includes("ix_process_oee_loss_drivers_tenant_assessment_reason") && migration.includes("unique: true"),
);
assert(
  "entity loss driver append-only FK",
  entityConfig.includes("DeleteBehavior.Restrict"),
);
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
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json };
}

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const operatorHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "production-pilot-operator",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};
const reviewerHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-reviewer",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};
const negativeHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "quality-negative-test-user",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};

assert(
  "missing auth → 401",
  (await liveRequest("GET", `/api/process-oee/assessments/${PILOT_ASSESSMENT}/losses?facilityId=${FACILITY}`)).status === 401,
);

const managerEvaluate = await liveRequest(
  "POST",
  `/api/process-oee/evaluation-windows/${PILOT_WINDOW}/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `poe2-sec-eval-${Date.now()}` },
  operatorHeaders,
);
assert(
  "manager evaluate → 200/201",
  managerEvaluate.status === 200 || managerEvaluate.status === 201,
  `status=${managerEvaluate.status}`,
);

const assessmentId = managerEvaluate.json?.id ?? PILOT_ASSESSMENT;

const managerLosses = await liveRequest(
  "GET",
  `/api/process-oee/assessments/${assessmentId}/losses?facilityId=${FACILITY}`,
  null,
  operatorHeaders,
);
assert("manager read losses → 200", managerLosses.status === 200, `status=${managerLosses.status}`);

const reviewerLosses = await liveRequest(
  "GET",
  `/api/process-oee/assessments/${assessmentId}/losses?facilityId=${FACILITY}`,
  null,
  reviewerHeaders,
);
assert("read-only read losses → 200", reviewerLosses.status === 200, `status=${reviewerLosses.status}`);

assert(
  "read-only evaluate → 403",
  (
    await liveRequest(
      "POST",
      `/api/process-oee/evaluation-windows/${PILOT_WINDOW}/evaluations?facilityId=${FACILITY}`,
      { idempotencyKey: `poe2-sec-ro-${Date.now()}` },
      reviewerHeaders,
    )
  ).status === 403,
);

assert(
  "unauthorized actor → 403",
  (
    await liveRequest(
      "GET",
      `/api/process-oee/assessments/${assessmentId}/losses?facilityId=${FACILITY}`,
      null,
      negativeHeaders,
    )
  ).status === 403,
);

assert(
  "unauthorized facility → 403",
  (
    await liveRequest(
      "GET",
      `/api/process-oee/assessments/${assessmentId}/losses?facilityId=${UNAUTH_FACILITY}`,
      null,
      operatorHeaders,
    )
  ).status === 403,
);

console.log("PROCESS-OEE-2 security/boundary verification passed.");

#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PILOT_WINDOW = "bb000001-00c0-4000-8000-0000000000c0";
const PILOT_ASSESSMENT = "bb000001-00c3-4000-8000-0000000000c3";

function fail(message) {
  console.error(`PROCESS-OEE-3 security/boundary verification failed: ${message}`);
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

console.log("Running PROCESS-OEE-3 security/boundary verifier.");

const endpoints = readRequired("apps/api/ProcessOee/ProcessOeeEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/ProcessOee/ProcessOeeEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260812200000_AddProcessOeeStabilityIntelligence.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/Authorization/ProcessOeePermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");
const entityConfig = readRequired("packages/NevaxisFactOS.Persistence/Configurations/ProcessOeeEntityConfigurations.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", endpoints.includes("ResolveReadAccessAsync"));
assert("write access helper", endpoints.includes("ResolveWriteAccessAsync"));
assert("401 mapping", helpers.includes("Status401Unauthorized") || helpers.includes("401"));
assert("403 mapping", helpers.includes("Status403Forbidden") || helpers.includes("403"));
assert("process-oee.read key", permissions.includes("process-oee.read"));
assert("process-oee.manage key", permissions.includes("process-oee.manage"));
assert("stability overview endpoint", endpoints.includes("/stability-overview"));
assert("assessment stability endpoint", endpoints.includes("/assessments/{assessmentId:guid}/stability"));
assert("window stability endpoint", endpoints.includes("/evaluation-windows/{windowId:guid}/stability"));
assert("downtime pareto endpoint", endpoints.includes("/assessments/{assessmentId:guid}/downtime-pareto"));
assert("shift comparison endpoint", endpoints.includes("/shift-comparison"));
assert("shift comparison evaluation endpoint", endpoints.includes("/shift-comparison/evaluations"));
assert("stability uses read access", endpoints.includes("GetAssessmentStabilityAsync"));
assert("shift comparison uses write access", endpoints.includes("EvaluateShiftComparisonAsync"));
assert("RLS enabled on stability assessments", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS on stability assessments", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("RLS enabled on stability drivers", migration.includes("process_oee_stability_drivers"));
assert("RLS enabled on downtime breakdown rows", migration.includes("process_oee_downtime_breakdown_rows"));
assert("RLS enabled on shift comparison batches", migration.includes("process_oee_shift_comparison_batches"));
assert("RLS enabled on shift comparison rows", migration.includes("process_oee_shift_comparison_rows"));
forbidText(migration, "GRANT DELETE ON TABLE process_oee_stability_assessments", "stability assessments must not allow DELETE");
forbidText(migration, "GRANT UPDATE ON TABLE process_oee_stability_assessments", "stability assessments must not allow UPDATE");
forbidText(migration, "GRANT DELETE ON TABLE process_oee_stability_drivers", "stability drivers must not allow DELETE");
forbidText(migration, "GRANT UPDATE ON TABLE process_oee_stability_drivers", "stability drivers must not allow UPDATE");
forbidText(migration, "GRANT DELETE ON TABLE process_oee_downtime_breakdown_rows", "downtime breakdown rows must not allow DELETE");
forbidText(migration, "GRANT UPDATE ON TABLE process_oee_downtime_breakdown_rows", "downtime breakdown rows must not allow UPDATE");
forbidText(migration, "GRANT DELETE ON TABLE process_oee_shift_comparison_batches", "shift comparison batches must not allow DELETE");
forbidText(migration, "GRANT UPDATE ON TABLE process_oee_shift_comparison_batches", "shift comparison batches must not allow UPDATE");
forbidText(migration, "GRANT DELETE ON TABLE process_oee_shift_comparison_rows", "shift comparison rows must not allow DELETE");
forbidText(migration, "GRANT UPDATE ON TABLE process_oee_shift_comparison_rows", "shift comparison rows must not allow UPDATE");
assert("append-only stability assessments grant", migration.includes("GRANT SELECT, INSERT ON TABLE process_oee_stability_assessments"));
assert("append-only stability drivers grant", migration.includes("GRANT SELECT, INSERT ON TABLE process_oee_stability_drivers"));
assert("append-only downtime breakdown grant", migration.includes("GRANT SELECT, INSERT ON TABLE process_oee_downtime_breakdown_rows"));
assert("append-only shift comparison batches grant", migration.includes("GRANT SELECT, INSERT ON TABLE process_oee_shift_comparison_batches"));
assert("append-only shift comparison rows grant", migration.includes("GRANT SELECT, INSERT ON TABLE process_oee_shift_comparison_rows"));
assert(
  "stability driver unique index",
  migration.includes("ix_process_oee_stability_drivers_tenant_assessment_reason") && migration.includes("unique: true"),
);
assert(
  "downtime breakdown unique index",
  migration.includes("ix_process_oee_downtime_breakdown_tenant_assessment_reason") && migration.includes("unique: true"),
);
assert(
  "shift comparison idempotency index",
  migration.includes("ix_process_oee_shift_comparison_batches_tenant_idempotency") && migration.includes("unique: true"),
);
assert(
  "entity stability driver append-only FK",
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
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const reviewerHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-reviewer",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const negativeHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "quality-negative-test-user",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};

assert(
  "missing auth → 401 stability",
  (await liveRequest("GET", `/api/process-oee/assessments/${PILOT_ASSESSMENT}/stability?facilityId=${FACILITY}`)).status === 401,
);
assert(
  "missing auth → 401 stability overview",
  (await liveRequest("GET", `/api/process-oee/stability-overview?facilityId=${FACILITY}`)).status === 401,
);
assert(
  "missing auth → 401 shift comparison",
  (
    await liveRequest(
      "GET",
      `/api/process-oee/shift-comparison?facilityId=${FACILITY}&periodStartUtc=2026-08-15T06:00:00.000Z&periodEndUtc=2026-08-15T22:00:00.000Z`,
    )
  ).status === 401,
);

const managerEvaluate = await liveRequest(
  "POST",
  `/api/process-oee/evaluation-windows/${PILOT_WINDOW}/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `poe3-sec-eval-${Date.now()}` },
  operatorHeaders,
);
assert(
  "manager evaluate → 200/201",
  managerEvaluate.status === 200 || managerEvaluate.status === 201,
  `status=${managerEvaluate.status}`,
);

const assessmentId = managerEvaluate.json?.id ?? PILOT_ASSESSMENT;
const securityAssessmentId = PILOT_ASSESSMENT;

const managerStability = await liveRequest(
  "GET",
  `/api/process-oee/assessments/${assessmentId}/stability?facilityId=${FACILITY}`,
  null,
  operatorHeaders,
);
assert(
  "manager read stability → 200/404",
  managerStability.status === 200 || managerStability.status === 404,
  `status=${managerStability.status}`,
);

const managerOverview = await liveRequest(
  "GET",
  `/api/process-oee/stability-overview?facilityId=${FACILITY}`,
  null,
  operatorHeaders,
);
assert(
  "manager read stability overview → 200/404",
  managerOverview.status === 200 || managerOverview.status === 404,
  `status=${managerOverview.status}`,
);

const managerPareto = await liveRequest(
  "GET",
  `/api/process-oee/assessments/${assessmentId}/downtime-pareto?facilityId=${FACILITY}`,
  null,
  operatorHeaders,
);
assert(
  "manager read downtime pareto → 200/404",
  managerPareto.status === 200 || managerPareto.status === 404,
  `status=${managerPareto.status}`,
);

const reviewerStability = await liveRequest(
  "GET",
  `/api/process-oee/assessments/${assessmentId}/stability?facilityId=${FACILITY}`,
  null,
  reviewerHeaders,
);
assert(
  "read-only read stability → 200/404",
  reviewerStability.status === 200 || reviewerStability.status === 404,
  `status=${reviewerStability.status}`,
);

assert(
  "read-only evaluate → 403",
  (
    await liveRequest(
      "POST",
      `/api/process-oee/evaluation-windows/${PILOT_WINDOW}/evaluations?facilityId=${FACILITY}`,
      { idempotencyKey: `poe3-sec-ro-${Date.now()}` },
      reviewerHeaders,
    )
  ).status === 403,
);

const reviewerShiftComparisonEval = await liveRequest(
  "POST",
  `/api/process-oee/shift-comparison/evaluations?facilityId=${FACILITY}`,
  {
    machineAssetId: "bb000001-0096-4000-8000-000000000096",
    periodStartUtc: "2026-08-15T06:00:00.000Z",
    periodEndUtc: "2026-08-15T22:00:00.000Z",
    idempotencyKey: `poe3-sec-shift-ro-${Date.now()}`,
  },
  reviewerHeaders,
);
assert(
  "read-only shift comparison evaluation → 403/404",
  reviewerShiftComparisonEval.status === 403 || reviewerShiftComparisonEval.status === 404,
  `status=${reviewerShiftComparisonEval.status}`,
);

assert(
  "unauthorized actor → 403",
  (
    await liveRequest(
      "GET",
      `/api/process-oee/assessments/${securityAssessmentId}/losses?facilityId=${FACILITY}`,
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
      `/api/process-oee/assessments/${securityAssessmentId}/losses?facilityId=${UNAUTH_FACILITY}`,
      null,
      operatorHeaders,
    )
  ).status === 403,
);

const operatorStabilityOverview = await liveRequest(
  "GET",
  `/api/process-oee/stability-overview?facilityId=${FACILITY}`,
  null,
  operatorHeaders,
);
if (operatorStabilityOverview.status === 200) {
  assert(
    "unauthorized actor → 403 stability overview",
    (
      await liveRequest(
        "GET",
        `/api/process-oee/stability-overview?facilityId=${FACILITY}`,
        null,
        negativeHeaders,
      )
    ).status === 403,
  );
  assert(
    "unauthorized facility → 403 stability overview",
    (
      await liveRequest(
        "GET",
        `/api/process-oee/stability-overview?facilityId=${UNAUTH_FACILITY}`,
        null,
        operatorHeaders,
      )
    ).status === 403,
  );
}

console.log("PROCESS-OEE-3 security/boundary verification passed.");

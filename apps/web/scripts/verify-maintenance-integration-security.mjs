import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const TENANT = process.env.NEVAXIS_FACTOS_TENANT ?? "dev-pilot-tenant";
const OPERATOR = process.env.NEVAXIS_FACTOS_SUBJECT ?? "production-pilot-operator";
const NEGATIVE = "quality-negative-test-user";
const ISSUER = "crevafactos-dev";
const AUTHORIZED_FACILITY = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const UNAUTHORIZED_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PILOT_PLAN = "bb000001-0082-4000-8000-000000000082";

function fail(message) {
  console.error(`Maintenance integration security verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

function headers(subject = OPERATOR, extra = {}) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
    ...extra,
  };
}

async function statusFor(method, path, body, subject = OPERATOR, extra = {}) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(subject, extra),
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  return response.status;
}

console.log("Running maintenance integration security verifier.");

const endpoints = readRequired("apps/api/Maintenance/MaintenanceEndpointRouteBuilderExtensions.cs");
const executionEndpoints = readRequired(
  "apps/api/Maintenance/MaintenanceExecutionEndpointRouteBuilderExtensions.cs",
);
const reportingEndpoints = readRequired(
  "apps/api/Maintenance/MaintenanceReportingEndpointRouteBuilderExtensions.cs",
);
const helpers = readRequired("apps/api/Maintenance/MaintenanceEndpointHelpers.cs");
const migrationCore = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260809170000_AddMaintenanceWorkOrderCore.cs",
);
const migrationExec = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260809180000_AddMaintenanceExecutionAndPreventiveScheduling.cs",
);
const auditConstants = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceAuditConstants.cs");
const requestRepo = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceRequestRepository.cs");
const workOrderRepo = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceWorkOrderRepository.cs");
const executionRepo = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceExecutionRepository.cs");

requireText(endpoints, "RequireAuthorization", "maintenance endpoints");
requireText(helpers, "MaintenancePermissionKeys.Read", "helpers");
requireText(endpoints, "MaintenancePermissionKeys.Manage", "maintenance endpoints");
requireText(helpers, "MaintenancePermissionKeys.Execute", "helpers");
requireText(helpers, "Status401Unauthorized", "helpers");
requireText(helpers, "Status403Forbidden", "helpers");
requireText(helpers, "Status409Conflict", "helpers");

requireText(migrationCore, "ENABLE ROW LEVEL SECURITY", "core migration");
requireText(migrationCore, "FORCE ROW LEVEL SECURITY", "core migration");
requireText(migrationExec, "maintenance_work_notes", "execution migration");
requireText(migrationExec, "preventive_maintenance_dues", "execution migration");
requireText(migrationExec, "maintenance_part_usages", "execution migration");
forbidText(migrationCore, "GRANT DELETE", "core migration");
forbidText(migrationExec, "GRANT DELETE", "execution migration");
forbidText(migrationExec, "GRANT UPDATE ON TABLE maintenance_work_notes", "execution migration");
forbidText(migrationExec, "GRANT UPDATE ON TABLE maintenance_part_usages", "execution migration");

requireText(auditConstants, "maintenance.request.created", "audit constants");
requireText(auditConstants, "maintenance.workorder.completed", "audit constants");
requireText(auditConstants, "maintenance.worknote.created", "audit constants");
requireText(auditConstants, "maintenance.partusage.recorded", "audit constants");
requireText(auditConstants, "maintenance.preventive.due.generated", "audit constants");
requireText(auditConstants, "maintenance-request-created", "outbox constants");
requireText(auditConstants, "maintenance-work-order-completed", "outbox constants");

forbidText(endpoints, "MapDelete", "maintenance endpoints");
forbidText(executionEndpoints, "MapDelete", "execution endpoints");
forbidText(reportingEndpoints, "MapDelete", "reporting endpoints");
forbidText(reportingEndpoints, "MapPost", "reporting endpoints");
forbidText(requestRepo, "DELETE", "request repository");

console.log("Running maintenance integration security live checks.");

try {
  const unauthenticated = await fetch(
    `${BASE}/api/maintenance/requests?facilityId=${AUTHORIZED_FACILITY}`,
  ).then((response) => response.status);
  if (unauthenticated !== 401) {
    fail(`unauthenticated read must return HTTP 401, got ${unauthenticated}.`);
  }

  const missingRead = await statusFor(
    "GET",
    `/api/maintenance/requests?facilityId=${AUTHORIZED_FACILITY}`,
    undefined,
    NEGATIVE,
  );
  if (missingRead !== 403) {
    fail(`missing maintenance.read must return HTTP 403, got ${missingRead}.`);
  }

  const missingManage = await statusFor(
    "POST",
    `/api/maintenance/preventive-plans/${PILOT_PLAN}/generate-next-due?facilityId=${AUTHORIZED_FACILITY}`,
    undefined,
    NEGATIVE,
  );
  if (missingManage !== 403) {
    fail(`missing maintenance.manage must return HTTP 403, got ${missingManage}.`);
  }

  const missingExecute = await statusFor(
    "POST",
    `/api/maintenance/work-orders/bb000001-0081-4000-8000-000000000081/notes?facilityId=${AUTHORIZED_FACILITY}`,
    { noteType: "general", text: "gate security probe", isImportant: false },
    NEGATIVE,
  );
  if (missingExecute !== 403) {
    fail(`missing maintenance.execute must return HTTP 403, got ${missingExecute}.`);
  }

  const missingFacility = await statusFor("GET", "/api/maintenance/preventive-dues");
  if (missingFacility !== 400) {
    fail(`missing facilityId must return HTTP 400, got ${missingFacility}.`);
  }

  const wrongFacility = await statusFor(
    "GET",
    `/api/maintenance/preventive-dues?facilityId=${UNAUTHORIZED_FACILITY}`,
  );
  if (wrongFacility !== 403) {
    fail(`wrong facility must return HTTP 403, got ${wrongFacility}.`);
  }

  const crossTenantRequest = await statusFor(
    "GET",
    `/api/maintenance/requests?facilityId=${AUTHORIZED_FACILITY}`,
    undefined,
    OPERATOR,
    { "X-NevaxisFactOS-Tenant": "other-tenant" },
  );
  if (crossTenantRequest !== 403) {
    fail(`cross-tenant request read must return HTTP 403, got ${crossTenantRequest}.`);
  }

  const crossTenantReporting = await statusFor(
    "GET",
    `/api/maintenance/reporting/overview?facilityId=${AUTHORIZED_FACILITY}`,
    undefined,
    OPERATOR,
    { "X-NevaxisFactOS-Tenant": "other-tenant" },
  );
  if (crossTenantReporting !== 403) {
    fail(`cross-tenant reporting read must return HTTP 403, got ${crossTenantReporting}.`);
  }

  const deleteRequest = await statusFor(
    "DELETE",
    `/api/maintenance/requests/bb000001-0080-4000-8000-000000000080?facilityId=${AUTHORIZED_FACILITY}`,
  );
  if (deleteRequest !== 405) {
    fail(`HTTP DELETE must return HTTP 405, got ${deleteRequest}.`);
  }

  const createForConcurrency = await fetch(`${BASE}/api/maintenance/requests?facilityId=${AUTHORIZED_FACILITY}`, {
    method: "POST",
    headers: headers(),
    body: JSON.stringify({
      assetType: "machine",
      assetReference: "GATE-CONCURRENCY-MACHINE",
      type: "fault",
      priority: "normal",
      title: `GATE-CONCURRENCY-${Date.now()}`,
      description: "Concurrency probe",
      source: "manual",
    }),
  });
  if (createForConcurrency.status !== 201) {
    fail(`concurrency probe request must return HTTP 201, got ${createForConcurrency.status}.`);
  }
  const created = await createForConcurrency.json();
  const convertResponse = await fetch(
    `${BASE}/api/maintenance/requests/${created.id}/convert-to-work-order?facilityId=${AUTHORIZED_FACILITY}`,
    {
      method: "POST",
      headers: headers(),
      body: JSON.stringify({ expectedVersion: created.version }),
    },
  );
  if (convertResponse.status !== 201) {
    fail(`concurrency probe convert must return HTTP 201, got ${convertResponse.status}.`);
  }
  const converted = await convertResponse.json();
  const assignBody = { expectedVersion: converted.version, assignedTechnicianSubjectId: "production-pilot-operator" };
  const [firstAssign, secondAssign] = await Promise.all([
    statusFor(
      "POST",
      `/api/maintenance/work-orders/${converted.id}/assign?facilityId=${AUTHORIZED_FACILITY}`,
      assignBody,
    ),
    statusFor(
      "POST",
      `/api/maintenance/work-orders/${converted.id}/assign?facilityId=${AUTHORIZED_FACILITY}`,
      assignBody,
    ),
  ]);
  const successCount = [firstAssign, secondAssign].filter((status) => status === 200).length;
  const conflictCount = [firstAssign, secondAssign].filter((status) => status === 409).length;
  if (successCount !== 1 || conflictCount !== 1) {
    fail(`same-version concurrency must be one 200 and one 409, got ${firstAssign}/${secondAssign}.`);
  }
  if ([firstAssign, secondAssign].some((status) => status >= 500)) {
    fail("concurrency must not surface 5xx.");
  }

  const staleAssign = await statusFor(
    "POST",
    `/api/maintenance/work-orders/${converted.id}/assign?facilityId=${AUTHORIZED_FACILITY}`,
    { expectedVersion: converted.version, assignedTechnicianSubjectId: "production-pilot-operator" },
  );
  if (staleAssign !== 409) {
    fail(`stale expectedVersion must return HTTP 409, got ${staleAssign}.`);
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("Maintenance integration security verification passed.");

import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://localhost:5011";
const AUTHORIZED_FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTHORIZED_FACILITY = "bb000001-0005-4000-8000-000000000099";
const NONEXISTENT_FACILITY = "bb000001-0005-4000-8000-000000000098";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const NEGATIVE = "quality-negative-test-user";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`Maintenance 2L security verification failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": TENANT,
    ...extra,
  };
}

async function getMaintenance(path, subject = OPERATOR, facilityId, extraHeaders = {}) {
  const url = facilityId
    ? `${BASE}${path}?facilityId=${facilityId}`
    : `${BASE}${path}`;
  const response = await fetch(url, { headers: headers(subject, extraHeaders) });
  return response.status;
}

console.log("Running maintenance 2L security verifier.");

const executionEndpoints = readRequired(
  "apps/api/Maintenance/MaintenanceExecutionEndpointRouteBuilderExtensions.cs",
);
const parentEndpoints = readRequired(
  "apps/api/Maintenance/MaintenanceEndpointRouteBuilderExtensions.cs",
);
const helpers = readRequired("apps/api/Maintenance/MaintenanceEndpointHelpers.cs");
const resolver = readRequired("apps/api/Facilities/FacilityAccessResolver.cs");
const pilotIds = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotIds.cs");
const bootstrap = readRequired(
  "packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs",
);
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809180000_AddMaintenanceExecutionAndPreventiveScheduling.cs",
);
const executionRepo = readRequired(
  "packages/NevaxisZentra.Persistence/Maintenance/MaintenanceExecutionRepository.cs",
);

requireText(parentEndpoints, 'MapGroup("/api/maintenance").RequireAuthorization()', "maintenance group auth");
requireText(parentEndpoints, "MapMaintenanceExecutionEndpoints", "maintenance group — execution endpoints wired");
requireText(executionEndpoints, "ResolveReadAccessAsync", "execution endpoints");
requireText(executionEndpoints, "ResolveExecuteAccessAsync", "execution endpoints — notes/parts");
requireText(executionEndpoints, "MaintenancePermissionKeys.Manage", "execution endpoints — due generation");
requireText(helpers, "MaintenancePermissionKeys.Execute", "helpers");
requireText(helpers, "ConcurrencyConflict", "helpers");
requireText(helpers, "Status409Conflict", "helpers");
requireText(helpers, "FacilityAccessFailureCodes.FacilityAccessDenied", "helpers — facility access denied mapping");
requireText(helpers, "Status403Forbidden", "helpers — forbidden mapping");
requireText(helpers, "FacilityAccessFailureCodes.FacilityMissing", "helpers — missing facility mapping");
requireText(helpers, "Status400BadRequest", "helpers — bad request mapping");
requireText(resolver, "FacilityAccessFailureCodes.FacilityAccessDenied", "facility resolver — access denied");
requireText(pilotIds, "UnauthorizedFacilityId", "DevelopmentPilotIds — unauthorized facility fixture");
requireText(pilotIds, UNAUTHORIZED_FACILITY, "DevelopmentPilotIds — unauthorized facility id");
requireText(bootstrap, "EnsureUnauthorizedFacilityAsync", "bootstrap — unauthorized facility seed");

requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration");
requireText(migration, "FORCE ROW LEVEL SECURITY", "migration");
requireText(migration, "ApplyTenantRlsAppendOnly", "migration — append-only RLS");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(migration, "GRANT UPDATE ON TABLE maintenance_work_notes", "migration");
forbidText(migration, "GRANT UPDATE ON TABLE maintenance_part_usages", "migration");

requireText(executionRepo, "expectedVersion", "execution repository");
requireText(executionRepo, "ConcurrencyFail", "execution repository");
requireText(executionRepo, "TenantId == context.TenantId", "execution repository");

forbidText(executionEndpoints, "MapDelete", "execution endpoints");
forbidText(executionEndpoints, "AllowAnonymous", "execution endpoints");
forbidText(executionRepo, "Payroll", "execution repository");
forbidText(executionRepo, "EmployeeScore", "execution repository");

console.log("Running maintenance 2L security live authorization checks.");

try {
  const missingFacilityStatus = await getMaintenance("/api/maintenance/preventive-dues");
  if (missingFacilityStatus !== 400) {
    fail(`missing facilityId must return HTTP 400, got ${missingFacilityStatus}.`);
  }

  const unauthorizedFacilityStatus = await getMaintenance(
    "/api/maintenance/preventive-dues",
    OPERATOR,
    UNAUTHORIZED_FACILITY,
  );
  if (unauthorizedFacilityStatus !== 403) {
    fail(
      `existing unauthorized facility must return HTTP 403, got ${unauthorizedFacilityStatus}.`,
    );
  }

  const nonexistentFacilityStatus = await getMaintenance(
    "/api/maintenance/preventive-dues",
    OPERATOR,
    NONEXISTENT_FACILITY,
  );
  if (nonexistentFacilityStatus !== 403) {
    fail(
      `non-existent facility must return HTTP 403 via facility-access-denied, got ${nonexistentFacilityStatus}.`,
    );
  }

  const missingPermissionStatus = await getMaintenance(
    "/api/maintenance/preventive-dues",
    NEGATIVE,
    AUTHORIZED_FACILITY,
  );
  if (missingPermissionStatus !== 403) {
    fail(`missing permission must return HTTP 403, got ${missingPermissionStatus}.`);
  }

  const crossTenantStatus = await getMaintenance(
    "/api/maintenance/preventive-dues",
    OPERATOR,
    AUTHORIZED_FACILITY,
    { "X-NevaxisZentra-Tenant": "other-tenant" },
  );
  if (crossTenantStatus !== 403) {
    fail(`cross-tenant access must return HTTP 403, got ${crossTenantStatus}.`);
  }

  const unauthenticatedStatus = await fetch(
    `${BASE}/api/maintenance/preventive-dues?facilityId=${AUTHORIZED_FACILITY}`,
  ).then((response) => response.status);
  if (unauthenticatedStatus !== 401) {
    fail(`unauthenticated access must return HTTP 401, got ${unauthenticatedStatus}.`);
  }
} catch (error) {
  fail(`live authorization checks could not reach API at ${BASE}: ${error.message}`);
}

console.log("Maintenance 2L security verification passed.");

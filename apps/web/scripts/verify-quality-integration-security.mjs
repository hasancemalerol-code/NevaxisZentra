import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality integration security verification failed: ${message}`);
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

console.log("Running quality integration security verifier.");

const qualityEndpoints = readRequired("apps/api/Quality/QualityEndpointRouteBuilderExtensions.cs");
const ncrEndpoints = readRequired("apps/api/Quality/QualityNcrEndpointRouteBuilderExtensions.cs");
const reportingEndpoints = readRequired(
  "apps/api/Quality/QualityReportingEndpointRouteBuilderExtensions.cs",
);
const helpers = readRequired("apps/api/Quality/QualityEndpointHelpers.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/Authorization/QualityPermissionKeys.cs");
const inspectionRepo = readRequired("packages/NevaxisFactOS.Persistence/Quality/QualityInspectionRepository.cs");
const ncrRepo = readRequired("packages/NevaxisFactOS.Persistence/Quality/QualityNonconformanceRepository.cs");
const auditConstants = readRequired("packages/NevaxisFactOS.Persistence/Quality/QualityAuditConstants.cs");
const apiClient = readRequired("apps/web/src/features/quality-control/server/quality-control-api-client.ts");
const migrationCore = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260809140000_AddQualityControlCore.cs",
);
const migrationNcr = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260809150000_AddQualityNcrHoldAndCorrectiveAction.cs",
);

requireText(qualityEndpoints, "RequireAuthorization", "quality endpoints");
requireText(ncrEndpoints, "ResolveReadAccessAsync", "NCR endpoints");
requireText(reportingEndpoints, "RequireAuthorization", "reporting endpoints");
requireText(qualityEndpoints, "IFacilityAccessResolver", "quality endpoints");
requireText(reportingEndpoints, "ResolveReadAccessAsync", "reporting endpoints");

requireText(helpers, "Status401Unauthorized", "QualityEndpointHelpers");
requireText(helpers, "Status403Forbidden", "QualityEndpointHelpers");
requireText(helpers, "ConcurrencyConflict", "QualityEndpointHelpers");

requireText(permissions, "quality.read", "QualityPermissionKeys");
requireText(permissions, "quality.release", "QualityPermissionKeys");
requireText(permissions, "quality.hold", "QualityPermissionKeys");
requireText(permissions, "quality.ncr.manage", "QualityPermissionKeys");

requireText(inspectionRepo, "QualityFailureCodes.ReleaseNotAllowed", "inspection repository");
requireText(inspectionRepo, "IQualityReleaseGuard", "inspection repository");
requireText(ncrRepo, "PlaceHoldAsync", "NCR repository");
requireText(ncrRepo, "ReleaseHoldAsync", "NCR repository");

requireText(auditConstants, "quality-inspection-completed", "audit constants");
requireText(auditConstants, "quality-ncr-created", "audit constants");
requireText(auditConstants, "quality-hold-placed", "audit constants");
requireText(auditConstants, "quality-corrective-action-completed", "audit constants");

requireText(migrationCore, "ENABLE ROW LEVEL SECURITY", "core migration");
requireText(migrationNcr, "ENABLE ROW LEVEL SECURITY", "NCR migration");
forbidText(migrationCore, "GRANT DELETE", "core migration");
forbidText(migrationNcr, "GRANT DELETE", "NCR migration");

forbidText(qualityEndpoints, "MapDelete", "quality endpoints");
forbidText(ncrEndpoints, "MapDelete", "NCR endpoints");
forbidText(reportingEndpoints, "MapDelete", "reporting endpoints");
forbidText(reportingEndpoints, "MapPost", "reporting endpoints");
forbidText(apiClient, 'method: "DELETE"', "quality api client");

requireText(
  readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs"),
  "quality-negative-test-user",
  "negative test fixture",
);

console.log("Quality integration security verification passed.");

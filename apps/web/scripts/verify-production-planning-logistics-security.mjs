import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(
    `Production planning logistics security verification failed: ${message}`,
  );
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

const endpoints = readRequired(
  "apps/api/ProductionPlanning/PlanningLogisticsEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "RequireAuthorization", "PlanningLogistics endpoints");
requireText(endpoints, "IFacilityAccessResolver", "PlanningLogistics endpoints");
requireText(endpoints, "facilityId is null || facilityId == Guid.Empty", "PlanningLogistics endpoints");
requireText(endpoints, "ProductionPlanningPermissionKeys.Read", "PlanningLogistics endpoints");
requireText(endpoints, "ProductionPlanningPermissionKeys.Write", "PlanningLogistics endpoints");
forbidText(endpoints, "MapDelete", "PlanningLogistics endpoints");
forbidText(endpoints, "ExportShipment", "PlanningLogistics endpoints");

const program = readRequired("apps/api/Program.cs");
requireText(program, "MapProductionPlanningLogisticsEndpoints", "Program.cs");

const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808210000_AddPlanningLogisticsConstraints.cs",
);
requireText(migration, "REVOKE ALL", "Migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE", "Migration");
forbidText(migration, "GRANT DELETE", "Migration");

const apiClient = readRequired(
  "apps/web/src/features/production-planning/server/production-plan-logistics-api-client.ts",
);
requireText(apiClient, 'import "./production-plan-server-boundary"', "logistics api client");
forbidText(apiClient, 'method: "PUT"', "logistics api client");
forbidText(apiClient, 'method: "DELETE"', "logistics api client");

console.log("Production planning logistics security verification passed.");

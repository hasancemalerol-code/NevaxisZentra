import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Maintenance 1L security verification failed: ${message}`);
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

console.log("Running maintenance 1L security verifier.");

const endpoints = readRequired("apps/api/Maintenance/MaintenanceEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Maintenance/MaintenanceEndpointHelpers.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809170000_AddMaintenanceWorkOrderCore.cs",
);
const requestRepo = readRequired("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceRequestRepository.cs");

requireText(endpoints, "RequireAuthorization", "maintenance endpoints");
requireText(helpers, "MaintenancePermissionKeys.Read", "MaintenanceEndpointHelpers");
requireText(endpoints, "MaintenancePermissionKeys.Manage", "maintenance endpoints");
requireText(helpers, "MaintenancePermissionKeys.Execute", "MaintenanceEndpointHelpers");
requireText(helpers, "ConcurrencyConflict", "MaintenanceEndpointHelpers");
requireText(helpers, "Status409Conflict", "MaintenanceEndpointHelpers");

requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration");
requireText(migration, "FORCE ROW LEVEL SECURITY", "migration");
requireText(migration, "REVOKE ALL ON TABLE maintenance_requests", "migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE maintenance_requests", "migration");
forbidText(migration, "GRANT DELETE", "migration");

requireText(requestRepo, "ExpectedVersion", "MaintenanceRequestRepository");
requireText(requestRepo, "ConcurrencyFail", "MaintenanceRequestRepository");

forbidText(endpoints, "MapDelete", "maintenance endpoints");
forbidText(endpoints, "AllowAnonymous", "maintenance endpoints");

console.log("Maintenance 1L security verification passed.");

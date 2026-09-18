import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 1L security verification failed: ${message}`);
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

console.log("Running quality 1L security verifier.");

const endpoints = readRequired("apps/api/Quality/QualityEndpointRouteBuilderExtensions.cs");
const endpointHelpers = readRequired("apps/api/Quality/QualityEndpointHelpers.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809140000_AddQualityControlCore.cs",
);
const permissions = readRequired("packages/NevaxisZentra.Persistence/Authorization/QualityPermissionKeys.cs");

requireText(endpoints, "RequireAuthorization", "quality endpoints");
requireText(endpoints, "IFacilityAccessResolver", "quality endpoints");
requireText(endpointHelpers, "Status401Unauthorized", "quality endpoint helpers");
requireText(endpointHelpers, "Status403Forbidden", "quality endpoint helpers");
requireText(endpointHelpers, "ConcurrencyConflict", "quality endpoint helpers");
requireText(endpointHelpers, "QualityPermissionKeys.Read", "quality endpoint helpers");
requireText(endpoints, "QualityPermissionKeys.Manage", "quality endpoints");
requireText(endpoints, "QualityPermissionKeys.Inspect", "quality endpoints");
requireText(endpoints, "QualityPermissionKeys.Release", "quality endpoints");

requireText(permissions, "quality.read", "QualityPermissionKeys");
requireText(permissions, "quality.manage", "QualityPermissionKeys");
requireText(permissions, "quality.inspect", "QualityPermissionKeys");
requireText(permissions, "quality.release", "QualityPermissionKeys");

requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE", "migration");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "quality endpoints");
forbidText(endpoints, "Ai", "quality endpoints");

console.log("Quality 1L security verification passed.");

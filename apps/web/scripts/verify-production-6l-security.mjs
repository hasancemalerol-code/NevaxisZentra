import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production 6L security verification failed: ${message}`);
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

console.log("Running production 6L security verifier.");

const endpoints = readRequired("apps/api/Production/ProductionReportingEndpointRouteBuilderExtensions.cs");
const executionEndpoints = readRequired("apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs");

requireText(endpoints, "RequireAuthorization", "reporting endpoints");
requireText(endpoints, "IFacilityAccessResolver", "reporting endpoints");
requireText(endpoints, "ProductionExecutionPermissionKeys.Read", "reporting endpoints");
requireText(endpoints, "Status401Unauthorized", "reporting endpoints");
requireText(endpoints, "Status403Forbidden", "reporting endpoints");
forbidText(endpoints, "MapDelete", "reporting endpoints");

forbidText(executionEndpoints, "MapDelete", "production execution endpoints");

console.log("Production 6L security verification passed.");

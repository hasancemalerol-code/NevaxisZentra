import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 2L security verification failed: ${message}`);
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

console.log("Running quality 2L security verifier.");

const endpoints = readRequired("apps/api/Quality/QualityNcrEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Quality/QualityEndpointHelpers.cs");
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260809150000_AddQualityNcrHoldAndCorrectiveAction.cs",
);
const permissions = readRequired("packages/CrevaFactOS.Persistence/Authorization/QualityPermissionKeys.cs");
const apiClient = readRequired("apps/web/src/features/quality-control/server/quality-control-api-client.ts");
const writer = readRequired("apps/web/src/features/quality-control/server/quality-control-writer.ts");
const ncrRepo = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityNonconformanceRepository.cs");

requireText(endpoints, "IFacilityAccessResolver", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.NcrManage", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.Hold", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.Disposition", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.Release", "NCR endpoints");
requireText(helpers, "Status401Unauthorized", "QualityEndpointHelpers");
requireText(helpers, "Status403Forbidden", "QualityEndpointHelpers");
requireText(helpers, "ConcurrencyConflict", "QualityEndpointHelpers");

requireText(permissions, "quality.ncr.manage", "QualityPermissionKeys");
requireText(permissions, "quality.hold", "QualityPermissionKeys");
requireText(permissions, "quality.disposition", "QualityPermissionKeys");

requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE", "migration");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "NCR endpoints");
forbidText(endpoints, "OpenAI", "NCR endpoints");

requireText(apiClient, 'import "./quality-control-server-boundary"', "quality-control-api-client");
forbidText(apiClient, 'method: "DELETE"', "quality-control-api-client");
forbidText(apiClient, "method: 'DELETE'", "quality-control-api-client");

requireText(writer, '"use server"', "quality-control-writer");

forbidText(ncrRepo, "ProductionOrders.Update", "QualityNonconformanceRepository");
forbidText(ncrRepo, "GoodQuantity =", "QualityNonconformanceRepository");

console.log("Quality 2L security verification passed.");

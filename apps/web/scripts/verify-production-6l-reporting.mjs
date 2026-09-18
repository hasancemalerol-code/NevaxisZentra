import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production 6L reporting verification failed: ${message}`);
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

console.log("Running production 6L reporting verifier.");

const endpoints = readRequired("apps/api/Production/ProductionReportingEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/Production/ProductionReportingRepository.cs");
const models = readRequired("packages/NevaxisZentra.Persistence/Production/ProductionReportingModels.cs");
const program = readRequired("apps/api/Program.cs");

requireText(endpoints, "MapGroup(\"/api/production/reporting\")", "reporting endpoints");
requireText(endpoints, "MapGet(\"/overview\"", "reporting endpoints");
requireText(endpoints, "MapGet(\"/orders\"", "reporting endpoints");
requireText(endpoints, "MapGet(\"/downtime\"", "reporting endpoints");
requireText(endpoints, "MapGet(\"/scrap\"", "reporting endpoints");
requireText(endpoints, "MapGet(\"/shifts\"", "reporting endpoints");
requireText(endpoints, "MapGet(\"/exceptions\"", "reporting endpoints");
requireText(endpoints, "ProductionExecutionPermissionKeys.Read", "reporting endpoints");
forbidText(endpoints, "MapPost", "reporting endpoints");
forbidText(endpoints, "MapPut", "reporting endpoints");
forbidText(endpoints, "MapDelete", "reporting endpoints");

requireText(repository, "AsNoTracking", "ProductionReportingRepository");
requireText(models, "ProductionOperationalOverviewReadModel", "reporting models");
requireText(program, "MapProductionReportingEndpoints", "Program.cs");

console.log("Production 6L reporting verification passed.");

import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production integration security verification failed: ${message}`);
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

console.log("Running production integration security verifier.");

const executionEndpoints = readRequired(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);
const reportingEndpoints = readRequired(
  "apps/api/Production/ProductionReportingEndpointRouteBuilderExtensions.cs",
);
const writeRepository = readRequired(
  "packages/NevaxisZentra.Persistence/Production/ProductionOrderWriteRepository.cs",
);

// Authorization on all Production routes
requireText(executionEndpoints, "RequireAuthorization", "execution endpoints");
requireText(reportingEndpoints, "RequireAuthorization", "reporting endpoints");
requireText(executionEndpoints, "IFacilityAccessResolver", "execution endpoints");
requireText(reportingEndpoints, "IFacilityAccessResolver", "reporting endpoints");

// HTTP status contracts
requireText(executionEndpoints, "Status401Unauthorized", "execution endpoints");
requireText(executionEndpoints, "Status403Forbidden", "execution endpoints");
requireText(executionEndpoints, "Status409Conflict", "execution endpoints");
requireText(reportingEndpoints, "Status401Unauthorized", "reporting endpoints");
requireText(reportingEndpoints, "Status403Forbidden", "reporting endpoints");

// No DELETE routes
forbidText(executionEndpoints, "MapDelete", "execution endpoints");
forbidText(reportingEndpoints, "MapDelete", "reporting endpoints");

// Concurrency conflict mapping intact
requireText(executionEndpoints, "ConcurrencyConflict", "execution endpoint mapper");
requireText(executionEndpoints, "Status409Conflict", "execution endpoint mapper");
requireText(writeRepository, "DbUpdateConcurrencyException", "write repository");
requireText(writeRepository, "ProductionOrderFailureCodes.ConcurrencyConflict", "write repository");
requireText(writeRepository, "IsUniqueViolation", "write repository");

// expectedVersion contract
requireText(writeRepository, "expectedVersion", "write repository");

console.log("Production integration security verification passed.");

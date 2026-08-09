import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production integration boundaries verification failed: ${message}`);
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

console.log("Running production integration boundaries verifier.");

const signalReader = readRequired(
  "packages/CrevaFactOS.Persistence/Production/ProductionPlanningLogisticsSignalReader.cs",
);
const reportingRepo = readRequired(
  "packages/CrevaFactOS.Persistence/Production/ProductionReportingRepository.cs",
);
const reportingEngine = readRequired(
  "packages/CrevaFactOS.Persistence/Production/ProductionReportingEngine.cs",
);
const workflowRepo = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanWorkflowRepository.cs",
);
const writeRepo = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanWriteRepository.cs",
);

// Planning ownership preserved — Production must not mutate planning schedule
forbidText(workflowRepo, "ProductionOrders.Update", "Planning workflow");
forbidText(writeRepo, "dbContext.ProductionOrders", "Planning write");

// Export logistics not duplicated in Production
requireText(signalReader, "PlanningLogisticsConstraintEvaluations", "logistics signal reader");
forbidText(signalReader, "ExportShipments", "logistics signal reader");
forbidText(signalReader, "ExportBroker", "logistics signal reader");
forbidText(signalReader, "ExportCompliance", "logistics signal reader");
forbidText(reportingRepo, "ExportShipments", "ProductionReportingRepository");

// Quality not duplicated
forbidText(reportingRepo, "Ncr", "ProductionReportingRepository");
forbidText(reportingRepo, "QualityHold", "ProductionReportingRepository");
forbidText(reportingRepo, "Capa", "ProductionReportingRepository");

// ERP not duplicated
forbidText(reportingRepo, "Logo", "ProductionReportingRepository");
forbidText(reportingRepo, "Mikro", "ProductionReportingRepository");
forbidText(reportingRepo, "Netsis", "ProductionReportingRepository");

// No HR scoring
forbidText(reportingEngine, "ranking", "ProductionReportingEngine");
forbidText(reportingEngine, "disciplinary", "ProductionReportingEngine");
forbidText(reportingEngine, "payroll", "ProductionReportingEngine");
forbidText(reportingEngine, "performanceScore", "ProductionReportingEngine");

// Reporting read-only
const reportingEndpoints = readRequired(
  "apps/api/Production/ProductionReportingEndpointRouteBuilderExtensions.cs",
);
forbidText(reportingEndpoints, "MapPost", "reporting endpoints");
forbidText(reportingEndpoints, "MapPut", "reporting endpoints");
forbidText(reportingEndpoints, "MapDelete", "reporting endpoints");

console.log("Production integration boundaries verification passed.");

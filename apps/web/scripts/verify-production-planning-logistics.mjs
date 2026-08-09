import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production planning logistics verification failed: ${message}`);
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

console.log("Running production planning logistics verifier.");

const engine = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/PlanningLogisticsConstraintEngine.cs",
);
const sourceReader = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/PlanningLogisticsSourceReader.cs",
);
const bufferPolicy = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/PlanningLogisticsModels.cs",
);
const riskPolicy = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/PlanningLogisticsRiskPolicy.cs",
);
const endpoints = readRequired(
  "apps/api/ProductionPlanning/PlanningLogisticsEndpointRouteBuilderExtensions.cs",
);
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260808210000_AddPlanningLogisticsConstraints.cs",
);

requireText(engine, "PlanningLogisticsConstraintEngine", "PlanningLogisticsConstraintEngine");
requireText(engine, "SlackMinutes", "PlanningLogisticsConstraintEngine");
requireText(engine, "LatestProductionCompletionUtc", "PlanningLogisticsConstraintEngine");
requireText(engine, "MapSlackToRisk", "PlanningLogisticsConstraintEngine");
forbidText(engine, "BrokerCorrection", "PlanningLogisticsConstraintEngine");
forbidText(engine, "ComplianceBlocking", "PlanningLogisticsConstraintEngine");
forbidText(engine, "CustomerDelivery", "PlanningLogisticsConstraintEngine");
forbidText(engine, "CustomsCutoff", "PlanningLogisticsConstraintEngine");
forbidText(engine, "LLM", "PlanningLogisticsConstraintEngine");

requireText(sourceReader, "IPlanningLogisticsSourceReader", "PlanningLogisticsSourceReader");
requireText(sourceReader, "ExportShipments", "PlanningLogisticsSourceReader");
requireText(sourceReader, "ExportCases", "PlanningLogisticsSourceReader");
requireText(sourceReader, "SourceOrderReference", "PlanningLogisticsSourceReader");
forbidText(sourceReader, "ExportBrokerCorrections", "PlanningLogisticsSourceReader");
forbidText(sourceReader, "ExportBrokerCorrection", "PlanningLogisticsSourceReader");
requireText(sourceReader, "ExportHandoffReady", "PlanningLogisticsSourceReader");
requireText(sourceReader, "NormalizedBlockingReason", "PlanningLogisticsSourceReader");

requireText(bufferPolicy, "PlanningLogisticsBufferPolicy", "PlanningLogisticsModels");
requireText(bufferPolicy, "DevelopmentDefault", "PlanningLogisticsModels");
requireText(bufferPolicy, "IPlanningMaterialInboundContextReader", "PlanningLogisticsModels");
requireText(bufferPolicy, "material-context-unavailable", "PlanningLogisticsModels");

requireText(riskPolicy, "MapSlackToRisk", "PlanningLogisticsRiskPolicy");
requireText(riskPolicy, "HighSlackThresholdMinutes", "PlanningLogisticsRiskPolicy");

requireText(endpoints, "/logistics-constraints", "PlanningLogistics endpoints");
requireText(endpoints, "/logistics-constraints/evaluate", "PlanningLogistics endpoints");
requireText(endpoints, "ProductionPlanningPermissionKeys.Read", "PlanningLogistics endpoints");
requireText(endpoints, "ProductionPlanningPermissionKeys.Write", "PlanningLogistics endpoints");
forbidText(endpoints, "MapPut", "PlanningLogistics endpoints");
forbidText(endpoints, "MapDelete", "PlanningLogistics endpoints");
forbidText(endpoints, "ExportShipment", "PlanningLogistics endpoints");

requireText(migration, "planning_logistics_constraint_evaluations", "Migration");
requireText(migration, "source_export_case_id", "Migration");
requireText(migration, "source_shipment_id", "Migration");
requireText(migration, "ROW LEVEL SECURITY", "Migration");
forbidText(migration, "CREATE TABLE export_shipments", "Migration");

const entity = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/PlanningLogisticsConstraintEvaluation.cs",
);
forbidText(entity, "CarrierName", "PlanningLogisticsConstraintEvaluation");
forbidText(entity, "BookingReference", "PlanningLogisticsConstraintEvaluation");
requireText(entity, "SourceExportCaseId", "PlanningLogisticsConstraintEvaluation");
requireText(entity, "SourceShipmentId", "PlanningLogisticsConstraintEvaluation");

console.log("Production planning logistics verification passed.");

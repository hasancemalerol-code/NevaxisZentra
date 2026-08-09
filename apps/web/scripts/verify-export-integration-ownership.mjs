import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Export integration ownership verification failed: ${message}`);
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

console.log("Running export integration ownership verifier.");

const exportCase = readRequired(
  "packages/CrevaFactOS.Persistence/Export/ExportCase.cs",
);
requireText(exportCase, "SourceOrderId", "ExportCase");
requireText(exportCase, "SourceOrderReference", "ExportCase");
forbidText(exportCase, "DuplicateOrder", "ExportCase");

const exportWrite = readRequired(
  "packages/CrevaFactOS.Persistence/Export/ExportCaseWriteRepository.cs",
);
requireText(exportWrite, "EnsureExportCaseForOrderAsync", "ExportCaseWriteRepository");
requireText(exportWrite, "IsExport", "ExportCaseWriteRepository");

const shipment = readRequired(
  "packages/CrevaFactOS.Persistence/Export/ExportShipment.cs",
);
requireText(shipment, "ExportCaseId", "ExportShipment");
requireText(shipment, "PlannedPickupAtUtc", "ExportShipment");
forbidText(shipment, "ProductionPlanId", "ExportShipment");

const planningEntity = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/PlanningLogisticsConstraintEvaluation.cs",
);
requireText(planningEntity, "SourceExportCaseId", "PlanningLogisticsConstraintEvaluation");
requireText(planningEntity, "SourceShipmentId", "PlanningLogisticsConstraintEvaluation");
forbidText(planningEntity, "CarrierName", "PlanningLogisticsConstraintEvaluation");
forbidText(planningEntity, "ForwarderName", "PlanningLogisticsConstraintEvaluation");
forbidText(planningEntity, "BookingReference", "PlanningLogisticsConstraintEvaluation");
forbidText(planningEntity, "TrackingReference", "PlanningLogisticsConstraintEvaluation");

const planningReader = readRequired(
  "packages/CrevaFactOS.Persistence/ProductionPlanning/PlanningLogisticsSourceReader.cs",
);
forbidText(planningReader, "ExportBrokerCorrections", "PlanningLogisticsSourceReader");
forbidText(planningReader, "ExportCompliance", "PlanningLogisticsSourceReader");
forbidText(planningReader, "ExportCountryCompliance", "PlanningLogisticsSourceReader");
requireText(planningReader, "ExportHandoffReady", "PlanningLogisticsSourceReader");

const aiProvider = readRequired(
  "packages/CrevaFactOS.Persistence/Export/DeterministicExportAiProvider.cs",
);
forbidText(aiProvider, "ModifyProduction", "DeterministicExportAiProvider");
forbidText(aiProvider, "SubmitCustoms", "DeterministicExportAiProvider");
forbidText(aiProvider, "CreateBooking", "DeterministicExportAiProvider");
requireText(aiProvider, "BuildReadinessOutput", "DeterministicExportAiProvider");

const aiSafety = readRequired(
  "packages/CrevaFactOS.Persistence/Export/Export4LEnums.cs",
);
requireText(aiSafety, "ExportAiProposedActionType", "Export4LEnums");

const orderDraft = readRequired(
  "apps/web/src/features/orders/domain/order-draft.ts",
);
requireText(orderDraft, "isExport", "order-draft.ts");
requireText(orderDraft, "destinationCountryCode", "order-draft.ts");

const traceability = readRequired(
  "packages/CrevaFactOS.Persistence/Export/ExportTraceabilityReadRepository.cs",
);
requireText(traceability, "SourceOrderReference", "ExportTraceabilityReadRepository");
requireText(traceability, "ExportCaseId", "ExportTraceabilityReadRepository");

console.log("Export integration ownership verification passed.");

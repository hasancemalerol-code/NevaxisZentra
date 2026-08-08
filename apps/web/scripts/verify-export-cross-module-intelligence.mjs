import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export cross-module intelligence verification failed: ${message}`);
  process.exit(1);
}

function readRequired(baseUrl, relativePath) {
  const filePath = resolvePath(baseUrl, relativePath);
  if (!existsSync(filePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(filePath, "utf8").replaceAll("\r\n", "\n");
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

const intelligence = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/ExportCrossModuleIntelligence.cs",
);
requireText(intelligence, "LoadProductionAsync", "ExportCrossModuleIntelligence");
requireText(intelligence, "LoadPlanningAsync", "ExportCrossModuleIntelligence");
requireText(intelligence, "LoadQuality", "ExportCrossModuleIntelligence");
requireText(intelligence, "LoadErp", "ExportCrossModuleIntelligence");
requireText(intelligence, "DetectRisks", "ExportCrossModuleIntelligence");
requireText(intelligence, "ExportPlanningLogisticsConstraintRef", "ExportCrossModuleIntelligence");
requireText(intelligence, "Quality module context is not yet available", "ExportCrossModuleIntelligence");
requireText(intelligence, "UnavailableProduction", "ExportCrossModuleIntelligence");
requireText(intelligence, "UnavailablePlanning", "ExportCrossModuleIntelligence");
requireText(intelligence, "UnavailableErp", "ExportCrossModuleIntelligence");
forbidText(intelligence, "HasUnresolvedHold = false", "ExportCrossModuleIntelligence");
forbidText(intelligence, "ReleaseStatus = \"released\"", "ExportCrossModuleIntelligence");

const models = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/Export4LModels.cs",
);
requireText(models, "ExportProductionIntelligenceRef", "Export4LModels");
requireText(models, "ExportPlanningIntelligenceRef", "Export4LModels");
requireText(models, "ExportQualityIntelligenceRef", "Export4LModels");
requireText(models, "ExportErpIntelligenceRef", "Export4LModels");
requireText(models, "ExportPlanningLogisticsConstraintRef", "Export4LModels");
requireText(models, "ExportCrossModuleRiskReadModel", "Export4LModels");
requireText(models, "Read-only production intelligence adapter contract", "Export4LModels");
requireText(models, "Read-only planning intelligence + logistics constraint contract", "Export4LModels");
requireText(models, "Future Planning Logistics Constraints integration contract", "Export4LModels");

const contextBuilder = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/ExportAiContextBuilder.cs",
);
requireText(contextBuilder, "ExportCrossModuleIntelligence.LoadProductionAsync", "ExportAiContextBuilder");
requireText(contextBuilder, "ExportCrossModuleIntelligence.LoadPlanningAsync", "ExportAiContextBuilder");
requireText(contextBuilder, "ExportCrossModuleIntelligence.LoadQuality", "ExportAiContextBuilder");
requireText(contextBuilder, "ExportCrossModuleIntelligence.LoadErp", "ExportAiContextBuilder");
requireText(contextBuilder, "ExportCrossModuleIntelligence.DetectRisks", "ExportAiContextBuilder");

const provider = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/DeterministicExportAiProvider.cs",
);
requireText(provider, "CrossModuleRisks", "DeterministicExportAiProvider");
requireText(provider, "BuildCrossModuleFindings", "DeterministicExportAiProvider");
requireText(provider, "ContextUnavailable", "DeterministicExportAiProvider");

const enums = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/Export4LEnums.cs",
);
requireText(enums, "ExportCrossModuleRiskType", "Export4LEnums");
requireText(enums, "ExportCrossModuleSourceModule", "Export4LEnums");
requireText(enums, "CrossModuleRiskAnalysis", "Export4LEnums");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export4LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "CrossModuleRisks", "Export 4L endpoints");
requireText(endpoints, "ExportCrossModuleRiskDto", "Export 4L endpoints");

console.log("Export cross-module intelligence verification passed.");

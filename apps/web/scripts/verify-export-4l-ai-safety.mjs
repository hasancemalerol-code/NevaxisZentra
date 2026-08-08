import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export 4L AI safety verification failed: ${message}`);
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

const FORBIDDEN_ACTIONS = [
  "VerifyCompliance",
  "VerifyRulePack",
  "ModifyRulePack",
  "SetGtip",
  "SetHsCode",
  "SetOrigin",
  "ConfirmOrigin",
  "ReleaseShipment",
  "ModifyOrder",
  "ModifyProduction",
  "ModifyQuality",
  "ErpWriteback",
  "verify-compliance",
  "verify-rule-pack",
  "modify-rule-pack",
  "set-gtip",
  "set-hs-code",
  "set-origin",
  "confirm-origin",
  "release-shipment",
  "modify-order",
  "modify-production",
  "modify-quality",
  "erp-writeback",
];

const enums = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/Export4LEnums.cs",
);
requireText(enums, "ExportAiProposedActionType", "Export4LEnums");
requireText(enums, "RequestHumanReview", "Export4LEnums");
requireText(enums, "OpenComplianceRequirement", "Export4LEnums");
for (const forbidden of FORBIDDEN_ACTIONS) {
  forbidText(enums, forbidden, "ExportAiProposedActionType");
}

const actionConfig = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Configurations/ExportAiRecommendationEntityConfiguration.cs",
);
for (const forbidden of FORBIDDEN_ACTIONS) {
  forbidText(actionConfig, forbidden, "ExportAiRecommendationEntityConfiguration");
}

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export4LEndpointRouteBuilderExtensions.cs",
);
forbidText(endpoints, "/verify", "Export 4L endpoints");
forbidText(endpoints, "/release", "Export 4L endpoints");
forbidText(endpoints, "/writeback", "Export 4L endpoints");
forbidText(endpoints, "ComplianceVerify", "Export 4L endpoints");
forbidText(endpoints, "ShipmentManage", "Export 4L endpoints");

const writeRepo = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/ExportAiWriteRepository.cs",
);
forbidText(writeRepo, "ExportComplianceWriteRepository", "ExportAiWriteRepository");
forbidText(writeRepo, "ExportShipmentWriteRepository", "ExportAiWriteRepository");
forbidText(writeRepo, "ProductionOrder", "ExportAiWriteRepository");
forbidText(writeRepo, "VerifyCompliance", "ExportAiWriteRepository");
forbidText(writeRepo, "ReleaseShipment", "ExportAiWriteRepository");
forbidText(writeRepo, "ErpWriteback", "ExportAiWriteRepository");
requireText(writeRepo, "ReviewRecommendationAsync", "ExportAiWriteRepository");
requireText(writeRepo, "HumanReviewRequired", "ExportAiWriteRepository");

const provider = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/DeterministicExportAiProvider.cs",
);
requireText(provider, "RequestHumanReview", "DeterministicExportAiProvider");
requireText(provider, "true, true", "DeterministicExportAiProvider");
requireText(provider, "kesin değerlendirme yapılamıyor", "DeterministicExportAiProvider");
requireText(provider, "NOT real AI intelligence", "DeterministicExportAiProvider");
forbidText(provider, "VerifyCompliance", "DeterministicExportAiProvider");
forbidText(provider, "ReleaseShipment", "DeterministicExportAiProvider");
forbidText(provider, "ModifyOrder", "DeterministicExportAiProvider");
forbidText(provider, "ErpWriteback", "DeterministicExportAiProvider");

const providerInterface = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/IExportAiProvider.cs",
);
requireText(providerInterface, "AnalyzeExportCaseAsync", "IExportAiProvider");
requireText(providerInterface, "ExplainComplianceAsync", "IExportAiProvider");
forbidText(providerInterface, "Verify", "IExportAiProvider");
forbidText(providerInterface, "Writeback", "IExportAiProvider");

const copilotPanel = readRequired(
  webRootUrl,
  "src/features/export/components/export-ai-copilot-panel.tsx",
);
requireText(copilotPanel, "İnsan onayı gerekli", "export-ai-copilot-panel");
requireText(copilotPanel, "Kaynak tabanlı analiz", "export-ai-copilot-panel");

console.log("Export 4L AI safety verification passed.");

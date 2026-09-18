import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export AI copilot verification failed: ${message}`);
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

const permissions = readRequired(
  repoRootUrl,
  "packages/NevaxisZentra.Persistence/Authorization/ExportPermissionKeys.cs",
);
requireText(permissions, "export.ai.read", "ExportPermissionKeys");
requireText(permissions, "export.ai.run", "ExportPermissionKeys");
requireText(permissions, "export.ai.review", "ExportPermissionKeys");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export4LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "RequireAuthorization", "Export 4L endpoints");
requireText(endpoints, "ExportPermissionKeys.AiRead", "Export 4L endpoints");
requireText(endpoints, "ExportPermissionKeys.AiRun", "Export 4L endpoints");
requireText(endpoints, "ExportPermissionKeys.AiReview", "Export 4L endpoints");
requireText(endpoints, 'MapPost("/analyze"', "Export 4L case AI");
requireText(endpoints, 'MapGet("/runs"', "Export 4L case AI");
requireText(endpoints, 'MapPost("/compliance-explanation"', "Export 4L case AI");
requireText(endpoints, 'MapPost("/next-actions"', "Export 4L case AI");
requireText(endpoints, 'MapGet("/recommendations"', "Export 4L case AI");
requireText(endpoints, 'MapPost("/recommendations/{id:guid}/accept"', "Export 4L case AI");
requireText(endpoints, 'MapPost("/recommendations/{id:guid}/reject"', "Export 4L case AI");
requireText(endpoints, 'MapPost("/recommendations/{id:guid}/dismiss"', "Export 4L case AI");
requireText(endpoints, "/api/export/country-research", "Export 4L research AI");
requireText(endpoints, "/api/ai/usage/current", "Export 4L usage API");

const program = readRequired(repoRootUrl, "apps/api/Program.cs");
requireText(program, "MapExport4LEndpoints", "Program.cs");

const copilotPanel = readRequired(
  webRootUrl,
  "src/features/export/components/export-ai-copilot-panel.tsx",
);
requireText(copilotPanel, "export-ai-copilot-panel", "export-ai-copilot-panel");
requireText(copilotPanel, "ExportAiCopilotPanel", "export-ai-copilot-panel");
requireText(copilotPanel, "analyzeExportCaseAction", "export-ai-copilot-panel");
requireText(copilotPanel, "explainExportReadinessAction", "export-ai-copilot-panel");
requireText(copilotPanel, "suggestExportNextActionsAction", "export-ai-copilot-panel");

const domain = readRequired(webRootUrl, "src/features/export/domain/export-4l.ts");
requireText(domain, "ExportAiAnalysisRun", "export-4l domain");
requireText(domain, "ExportAiRunDetail", "export-4l domain");
requireText(domain, "AiUsageSummary", "export-4l domain");

const contracts = readRequired(
  webRootUrl,
  "src/features/export/server/export-4l-api-contracts.ts",
);
requireText(contracts, 'import "./export-case-server-boundary"', "export-4l-api-contracts");
requireText(contracts, "startExportAiAnalysis", "export-4l-api-contracts");
requireText(contracts, "getCurrentAiUsage", "export-4l-api-contracts");
requireText(contracts, "/ai/analyze", "export-4l-api-contracts");

const writer = readRequired(
  webRootUrl,
  "src/features/export/server/export-4l-writer.ts",
);
requireText(writer, "analyzeExportCaseAction", "export-4l-writer");
requireText(writer, "acceptExportAiRecommendationAction", "export-4l-writer");
requireText(writer, "summarizeResearchAction", "export-4l-writer");

const reader = readRequired(
  webRootUrl,
  "src/features/export/server/export-4l-reader.ts",
);
requireText(reader, "readLatestExportAiRunDetail", "export-4l-reader");
requireText(reader, "readCurrentAiUsage", "export-4l-reader");

const detailPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
requireText(detailPage, "readLatestExportAiRunDetail", "export detail page");
requireText(detailPage, "readCurrentAiUsage", "export detail page");
requireText(detailPage, "analyzeExportCaseAction", "export detail page");
requireText(detailPage, 'case "ai-copilot"', "export detail page");

const workspace = readRequired(
  webRootUrl,
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(workspace, "ai-copilot", "export-detail-workspace");
requireText(workspace, "AI Copilot", "export-detail-workspace");
requireText(workspace, "ExportAiCopilotPanel", "export-detail-workspace");

const researchPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/ulke-arastirma/[researchCaseId]/page.tsx",
);
requireText(researchPage, "summarizeResearchAction", "research detail page");
requireText(researchPage, "findResearchMissingSourcesAction", "research detail page");

console.log("Export AI copilot verification passed.");

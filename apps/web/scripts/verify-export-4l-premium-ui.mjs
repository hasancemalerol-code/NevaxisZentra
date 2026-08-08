import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);

function resolveWebFile(relativePath) {
  return fileURLToPath(new URL(relativePath, webRootUrl));
}

function fail(message) {
  console.error(`Export 4L premium UI verification failed: ${message}`);
  process.exit(1);
}

function readRequiredFile(relativePath) {
  const filePath = resolveWebFile(relativePath);
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

const detailPage = readRequiredFile(
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
requireText(detailPage, "ExportDetailWorkspace", "detail page");
requireText(detailPage, "readLatestExportAiRunDetail", "detail page");
requireText(detailPage, "readCurrentAiUsage", "detail page");
requireText(detailPage, "readExportAiRecommendations", "detail page");
requireText(detailPage, 'case "ai-copilot"', "detail page");

const workspace = readRequiredFile(
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(workspace, "ai-copilot", "export-detail-workspace");
requireText(workspace, "AI Copilot", "export-detail-workspace");
requireText(workspace, "ExportAiCopilotPanel", "export-detail-workspace");
requireText(workspace, "export-detail-workspace__tabs", "export-detail-workspace");

const copilotIndex = workspace.indexOf('{ id: "ai-copilot"');
const overviewIndex = workspace.indexOf('{ id: "overview"');
if (copilotIndex === -1 || overviewIndex === -1 || copilotIndex <= overviewIndex) {
  fail("AI Copilot tab must appear after Genel Bakış in export-detail-workspace.");
}

const copilotPanel = readRequiredFile(
  "src/features/export/components/export-ai-copilot-panel.tsx",
);
requireText(copilotPanel, "export-ai-copilot-panel", "export-ai-copilot-panel");
requireText(copilotPanel, "İhracat AI asistanı", "export-ai-copilot-panel");
requireText(copilotPanel, "İhracat dosyasını analiz et", "export-ai-copilot-panel");
requireText(copilotPanel, "Hazırlığı açıkla", "export-ai-copilot-panel");
requireText(copilotPanel, "Sonraki adımları öner", "export-ai-copilot-panel");
requireText(copilotPanel, "export-ai-copilot-panel__recommendations", "export-ai-copilot-panel");
requireText(copilotPanel, "export-ai-copilot-panel__usage", "export-ai-copilot-panel");

const exportCss = readRequiredFile("src/app/export.css");
requireText(exportCss, ".export-ai-copilot-panel", "export.css");
requireText(exportCss, ".export-ai-copilot-panel__header", "export.css");
requireText(exportCss, ".export-ai-copilot-panel__actions", "export.css");
requireText(exportCss, ".export-ai-copilot-panel__summary", "export.css");
requireText(exportCss, ".export-ai-copilot-panel__recommendations", "export.css");
requireText(exportCss, ".export-ai-copilot-panel__usage", "export.css");
requireText(exportCss, ".export-detail-workspace__tabs", "export.css");

forbidText(copilotPanel, "coming soon", "export-ai-copilot-panel");
forbidText(copilotPanel, "Coming soon", "export-ai-copilot-panel");

console.log("Export 4L premium UI verification passed.");

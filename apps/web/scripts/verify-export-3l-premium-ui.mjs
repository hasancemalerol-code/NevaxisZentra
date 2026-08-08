import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);

function resolveWebFile(relativePath) {
  return fileURLToPath(new URL(relativePath, webRootUrl));
}

function fail(message) {
  console.error(`Export 3L premium UI verification failed: ${message}`);
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
const researchPage = readRequiredFile(
  "src/app/(app)/ihracat/ulke-arastirma/page.tsx",
);
const knowledgePage = readRequiredFile(
  "src/app/(app)/ihracat/ulke-bilgisi/page.tsx",
);
const workspace = readRequiredFile(
  "src/features/export/components/export-detail-workspace.tsx",
);
const compliancePanel = readRequiredFile(
  "src/features/export/components/export-compliance-panel.tsx",
);
const traceabilityPanel = readRequiredFile(
  "src/features/export/components/export-traceability-panel.tsx",
);
const researchWorkspace = readRequiredFile(
  "src/features/export/components/export-country-research-workspace.tsx",
);
const knowledgePanel = readRequiredFile(
  "src/features/export/components/export-country-knowledge-panel.tsx",
);
const exportCss = readRequiredFile("src/app/export.css");

requireText(detailPage, "readComplianceSummary", "detail page");
requireText(detailPage, "readTraceability", "detail page");
requireText(researchPage, "Yeni Ülke Araştır", "research list page");
requireText(knowledgePage, "Ülke Bilgisi", "knowledge page");

requireText(workspace, "Uygunluk", "export-detail-workspace");
requireText(workspace, "İzlenebilirlik", "export-detail-workspace");
requireText(workspace, "ExportCompliancePanel", "export-detail-workspace");
requireText(workspace, "ExportTraceabilityPanel", "export-detail-workspace");

requireText(compliancePanel, "export-compliance-panel", "compliance panel");
requireText(traceabilityPanel, "export-traceability-panel", "traceability panel");
requireText(researchWorkspace, "export-country-research-workspace", "research workspace");
requireText(knowledgePanel, "export-country-knowledge-panel", "knowledge panel");

requireText(exportCss, ".export-compliance-panel", "export.css");
requireText(exportCss, ".export-traceability-panel", "export.css");
requireText(exportCss, ".export-country-research-workspace", "export.css");
requireText(exportCss, ".export-country-knowledge-panel", "export.css");
requireText(exportCss, ".export-detail-workspace__tabs", "export.css");

forbidText(detailPage, "coming soon", "detail page");
forbidText(researchPage, "Coming soon", "research list page");

console.log("Export 3L premium UI verification passed.");

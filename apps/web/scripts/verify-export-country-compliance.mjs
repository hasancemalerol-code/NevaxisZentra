import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export country compliance verification failed: ${message}`);
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

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export3LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "/api/export/compliance/rule-packs", "Export 3L endpoints");
requireText(endpoints, 'MapPost("/evaluate"', "Export 3L endpoints");
requireText(endpoints, 'MapPost("/revalidate"', "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.ComplianceRead", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.ComplianceManage", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.ComplianceVerify", "Export 3L endpoints");
requireText(endpoints, "Export3LFailureCodes", "Export 3L endpoints");

const domain = readRequired(webRootUrl, "src/features/export/domain/export-3l.ts");
requireText(domain, "ExportComplianceSummary", "export-3l domain");
requireText(domain, "getComplianceStatusLabel", "export-3l domain");

const panel = readRequired(
  webRootUrl,
  "src/features/export/components/export-compliance-panel.tsx",
);
requireText(panel, "evaluateComplianceAction", "export-compliance-panel");
requireText(panel, "revalidateComplianceAction", "export-compliance-panel");
requireText(panel, "satisfyComplianceRequirementAction", "export-compliance-panel");
requireText(panel, "expectedCaseVersion", "export-compliance-panel");

const detailPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
requireText(detailPage, "readComplianceSummary", "detail page");
requireText(detailPage, "ExportDetailWorkspace", "detail page");

const workspace = readRequired(
  webRootUrl,
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(workspace, "ExportCompliancePanel", "export-detail-workspace");
requireText(workspace, "Uygunluk", "export-detail-workspace");

const writer = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-writer.ts",
);
requireText(writer, "evaluateComplianceAction", "export-case-writer");
requireText(writer, "revalidateComplianceAction", "export-case-writer");
requireText(writer, "satisfyComplianceRequirementAction", "export-case-writer");

console.log("Export country compliance verification passed.");

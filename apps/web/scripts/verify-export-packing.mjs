import {
  existsSync,
  readFileSync,
} from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export packing verification failed: ${message}`);
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
  "apps/api/Export/ExportEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "/{exportCaseId:guid}/packing", "Export endpoints");
requireText(endpoints, "/packing/units", "Export endpoints");
requireText(endpoints, "/packing/complete", "Export endpoints");
requireText(endpoints, "/packing/verify", "Export endpoints");
requireText(endpoints, "/packing/reconciliation", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.PackingManage", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.PackingVerify", "Export endpoints");

const domain = readRequired(
  webRootUrl,
  "src/features/export/domain/export-2l.ts",
);
requireText(domain, "PackingStatus", "export-2l domain");
requireText(domain, 'getPackingStatusLabel', "export-2l domain");

const panel = readRequired(
  webRootUrl,
  "src/features/export/components/export-packing-panel.tsx",
);
requireText(panel, "createPackingUnitAction", "export-packing-panel");
requireText(panel, "completePackingAction", "export-packing-panel");
requireText(panel, "verifyPackingAction", "export-packing-panel");
requireText(panel, "expectedCaseVersion", "export-packing-panel");

const detailPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
requireText(detailPage, "readPacking", "detail page");
requireText(detailPage, "ExportDetailWorkspace", "detail page");

const workspace = readRequired(
  webRootUrl,
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(workspace, "ExportPackingPanel", "export-detail-workspace");

const writer = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-writer.ts",
);
requireText(writer, "createPackingUnitAction", "export-case-writer");
requireText(writer, "completePackingAction", "export-case-writer");
requireText(writer, "verifyPackingAction", "export-case-writer");

console.log("Export packing verification passed.");

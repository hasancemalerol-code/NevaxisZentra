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
  console.error(`Export broker workspace verification failed: ${message}`);
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
requireText(endpoints, "/{exportCaseId:guid}/broker", "Export endpoints");
requireText(endpoints, "/broker/package/prepare", "Export endpoints");
requireText(endpoints, "/broker/package/send", "Export endpoints");
requireText(endpoints, "/broker/acknowledge", "Export endpoints");
requireText(endpoints, "/broker/corrections", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.BrokerManage", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.BrokerHandoff", "Export endpoints");

const domain = readRequired(
  webRootUrl,
  "src/features/export/domain/export-2l.ts",
);
requireText(domain, "BrokerWorkspaceStatus", "export-2l domain");
requireText(domain, "getBrokerWorkspaceStatusLabel", "export-2l domain");

const panel = readRequired(
  webRootUrl,
  "src/features/export/components/export-broker-panel.tsx",
);
requireText(panel, "createBrokerWorkspaceAction", "export-broker-panel");
requireText(panel, "prepareBrokerPackageAction", "export-broker-panel");
requireText(panel, "sendBrokerPackageAction", "export-broker-panel");
requireText(panel, "gümrük danışmanlığı", "export-broker-panel");

const detailPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
requireText(detailPage, "readBrokerWorkspace", "detail page");
requireText(detailPage, "ExportDetailWorkspace", "detail page");

const workspace = readRequired(
  webRootUrl,
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(workspace, "ExportBrokerPanel", "export-detail-workspace");

const writer = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-writer.ts",
);
requireText(writer, "createBrokerWorkspaceAction", "export-case-writer");
requireText(writer, "acknowledgeBrokerAction", "export-case-writer");
requireText(writer, "resolveBrokerCorrectionAction", "export-case-writer");

console.log("Export broker workspace verification passed.");

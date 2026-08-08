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
  console.error(`Export shipment verification failed: ${message}`);
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
requireText(endpoints, "/{exportCaseId:guid}/shipments", "Export endpoints");
requireText(endpoints, "/transition", "Export endpoints");
requireText(endpoints, "/readiness", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.ShipmentManage", "Export endpoints");

const domain = readRequired(
  webRootUrl,
  "src/features/export/domain/export-2l.ts",
);
requireText(domain, "ShipmentStatus", "export-2l domain");
requireText(domain, "getShipmentStatusLabel", "export-2l domain");
requireText(domain, "getTransportModeLabel", "export-2l domain");

const panel = readRequired(
  webRootUrl,
  "src/features/export/components/export-shipment-panel.tsx",
);
requireText(panel, "createShipmentAction", "export-shipment-panel");
requireText(panel, "transitionShipmentAction", "export-shipment-panel");
requireText(panel, "shipmentReadinessById", "export-shipment-panel");

const detailPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
requireText(detailPage, "readShipments", "detail page");
requireText(detailPage, "readShipmentReadiness", "detail page");
requireText(detailPage, "ExportDetailWorkspace", "detail page");

const workspace = readRequired(
  webRootUrl,
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(workspace, "ExportShipmentPanel", "export-detail-workspace");

const writer = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-writer.ts",
);
requireText(writer, "createShipmentAction", "export-case-writer");
requireText(writer, "updateShipmentAction", "export-case-writer");
requireText(writer, "transitionShipmentAction", "export-case-writer");

console.log("Export shipment verification passed.");

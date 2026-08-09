import {
  existsSync,
  readFileSync,
} from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);

function resolveWebFile(relativePath) {
  return fileURLToPath(new URL(relativePath, webRootUrl));
}

function fail(message) {
  console.error(`Export 2L premium UI verification failed: ${message}`);
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
const workspace = readRequiredFile(
  "src/features/export/components/export-detail-workspace.tsx",
);
const packingPanel = readRequiredFile(
  "src/features/export/components/export-packing-panel.tsx",
);
const shipmentPanel = readRequiredFile(
  "src/features/export/components/export-shipment-panel.tsx",
);
const brokerPanel = readRequiredFile(
  "src/features/export/components/export-broker-panel.tsx",
);
const exportCss = readRequiredFile("src/app/export.css");
const navigation = readRequiredFile(
  "src/lib/navigation/primary-navigation.ts",
);

requireText(detailPage, "ExportDetailWorkspace", "detail page");
requireText(detailPage, "readPacking", "detail page");
requireText(detailPage, "readShipments", "detail page");
requireText(detailPage, "readBrokerWorkspace", "detail page");
requireText(detailPage, "/siparisler/", "detail page");
requireText(detailPage, 'href="/ihracat"', "detail page");

requireText(workspace, "Genel Bakış", "export-detail-workspace");
requireText(workspace, "Belgeler", "export-detail-workspace");
requireText(workspace, "Paketleme", "export-detail-workspace");
requireText(workspace, "Sevkiyat", "export-detail-workspace");
requireText(workspace, "Gümrük Müşaviri", "export-detail-workspace");
requireText(workspace, "Aktivite", "export-detail-workspace");
requireText(workspace, "ExportPackingPanel", "export-detail-workspace");
requireText(workspace, "ExportShipmentPanel", "export-detail-workspace");
requireText(workspace, "ExportBrokerPanel", "export-detail-workspace");

requireText(packingPanel, "export-packing-panel", "packing panel");
requireText(shipmentPanel, "export-shipment-panel", "shipment panel");
requireText(brokerPanel, "export-broker-panel", "broker panel");
requireText(shipmentPanel, "Lojistik Firmasına Teslim", "shipment panel");
requireText(brokerPanel, "Gümrük Müşaviri Teslim", "broker panel");
requireText(brokerPanel, "gümrük danışmanlığı", "broker panel disclaimer");

requireText(exportCss, ".export-packing-panel", "export.css");
requireText(exportCss, ".export-shipment-panel", "export.css");
requireText(exportCss, ".export-broker-panel", "export.css");
requireText(exportCss, ".export-detail-workspace__tabs", "export.css");

forbidText(detailPage, "coming soon", "detail page");
forbidText(detailPage, "Coming soon", "detail page");
forbidText(navigation, "Paketleme", "primary-navigation.ts");
forbidText(navigation, "Gümrük Broker", "primary-navigation.ts");

console.log("Export 2L premium UI verification passed.");

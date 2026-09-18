import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Export handoff scope verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
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

console.log("Running export handoff scope verifier.");

const endpoints = readRequired(
  "apps/api/Export/ExportEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "/broker/package/prepare", "Export endpoints");
requireText(endpoints, "/broker/package/send", "Export endpoints");
requireText(endpoints, "/broker/acknowledge", "Export endpoints");
requireText(endpoints, "/broker/corrections", "Export endpoints");
requireText(endpoints, "/shipments", "Export endpoints");
requireText(endpoints, "GetShipmentReadinessAsync", "Export endpoints");
forbidText(endpoints, "customs-declaration", "Export endpoints");
forbidText(endpoints, "MapDelete", "Export endpoints");
forbidText(endpoints, "booking/create", "Export endpoints");
forbidText(endpoints, "gps", "Export endpoints");
forbidText(endpoints, "pod", "Export endpoints");
forbidText(endpoints, "freight-marketplace", "Export endpoints");

const brokerRepo = readRequired(
  "packages/NevaxisZentra.Persistence/Export/ExportBrokerWriteRepository.cs",
);
requireText(brokerRepo, "PreparePackageAsync", "ExportBrokerWriteRepository");
requireText(brokerRepo, "SendPackageAsync", "ExportBrokerWriteRepository");
requireText(brokerRepo, "AcknowledgeAsync", "ExportBrokerWriteRepository");
requireText(brokerRepo, "CreateCorrectionAsync", "ExportBrokerWriteRepository");
requireText(brokerRepo, "ResolveCorrectionAsync", "ExportBrokerWriteRepository");
forbidText(brokerRepo, "SubmitDeclaration", "ExportBrokerWriteRepository");

const brokerPackageDoc = readRequired(
  "packages/NevaxisZentra.Persistence/Export/ExportBrokerPackageDocument.cs",
);
requireText(brokerPackageDoc, "DocumentId", "ExportBrokerPackageDocument");
requireText(brokerPackageDoc, "PackageVersionId", "ExportBrokerPackageDocument");

const shipmentEntity = readRequired(
  "packages/NevaxisZentra.Persistence/Export/ExportShipment.cs",
);
requireText(shipmentEntity, "PlannedPickupAtUtc", "ExportShipment");
requireText(shipmentEntity, "ShipmentReference", "ExportShipment");
requireText(shipmentEntity, "IncotermCode", "ExportShipment");
forbidText(shipmentEntity, "GpsLatitude", "ExportShipment");

const readiness = readRequired(
  "packages/NevaxisZentra.Persistence/Export/ExportReadinessEngine.cs",
);
requireText(readiness, "EvaluateBrokerCorrectionsClear", "ExportReadinessEngine");
requireText(readiness, "EvaluatePackingDataComplete", "ExportReadinessEngine");
requireText(readiness, "EvaluateShipmentTransportModeKnown", "ExportReadinessEngine");

const brokerPanel = readRequired(
  "apps/web/src/features/export/components/export-broker-panel.tsx",
);
requireText(brokerPanel, "prepareBrokerPackageAction", "export-broker-panel");
requireText(brokerPanel, "sendBrokerPackageAction", "export-broker-panel");
requireText(brokerPanel, "resolveBrokerCorrectionAction", "export-broker-panel");
forbidText(brokerPanel, "Broker Operations", "export-broker-panel");
forbidText(brokerPanel, "Freight Marketplace", "export-broker-panel");

const shipmentPanel = readRequired(
  "apps/web/src/features/export/components/export-shipment-panel.tsx",
);
requireText(shipmentPanel, "createShipmentAction", "export-shipment-panel");
requireText(shipmentPanel, "updateShipmentAction", "export-shipment-panel");
forbidText(shipmentPanel, "Dispatch Control", "export-shipment-panel");
forbidText(shipmentPanel, "Fleet Management", "export-shipment-panel");

console.log("Export handoff scope verification passed.");

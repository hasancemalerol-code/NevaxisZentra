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
  console.error(`Export 2L security verification failed: ${message}`);
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

const permissions = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Authorization/ExportPermissionKeys.cs",
);
requireText(permissions, "export.packing.manage", "ExportPermissionKeys");
requireText(permissions, "export.packing.verify", "ExportPermissionKeys");
requireText(permissions, "export.shipment.manage", "ExportPermissionKeys");
requireText(permissions, "export.broker.manage", "ExportPermissionKeys");
requireText(permissions, "export.broker.handoff", "ExportPermissionKeys");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/ExportEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "RequireAuthorization", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.PackingManage", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.PackingVerify", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.ShipmentManage", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.BrokerManage", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.BrokerHandoff", "Export endpoints");
forbidText(endpoints, "MapDelete", "Export endpoints");
forbidText(endpoints, ".MapMethods(", "Export endpoints");

const apiClient = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-api-client.ts",
);
requireText(apiClient, 'import "./export-case-server-boundary"', "export-case-api-client");
forbidText(apiClient, 'method: "DELETE"', "export-case-api-client");
forbidText(apiClient, "method: 'DELETE'", "export-case-api-client");

const contracts = readRequired(
  webRootUrl,
  "src/features/export/server/export-2l-api-contracts.ts",
);
requireText(
  contracts,
  'import "./export-case-server-boundary"',
  "export-2l-api-contracts",
);

const navigation = readRequired(
  webRootUrl,
  "src/lib/navigation/primary-navigation.ts",
);
requireText(navigation, 'href: "/ihracat"', "primary-navigation.ts");

console.log("Export 2L security verification passed.");

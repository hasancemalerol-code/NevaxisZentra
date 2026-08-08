import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export 3L security verification failed: ${message}`);
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
requireText(permissions, "export.compliance.read", "ExportPermissionKeys");
requireText(permissions, "export.compliance.manage", "ExportPermissionKeys");
requireText(permissions, "export.compliance.verify", "ExportPermissionKeys");
requireText(permissions, "export.country-research.read", "ExportPermissionKeys");
requireText(permissions, "export.country-research.manage", "ExportPermissionKeys");
requireText(permissions, "export.country-research.verify", "ExportPermissionKeys");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export3LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "RequireAuthorization", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.ComplianceRead", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.ComplianceManage", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.ComplianceVerify", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.CountryResearchRead", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.CountryResearchManage", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.CountryResearchVerify", "Export 3L endpoints");
forbidText(endpoints, "MapDelete", "Export 3L endpoints");

const program = readRequired(repoRootUrl, "apps/api/Program.cs");
requireText(program, "MapExport3LEndpoints", "Program.cs");

const apiClient = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-api-client.ts",
);
requireText(apiClient, 'import "./export-case-server-boundary"', "export-case-api-client");
forbidText(apiClient, 'method: "DELETE"', "export-case-api-client");

const contracts = readRequired(
  webRootUrl,
  "src/features/export/server/export-3l-api-contracts.ts",
);
requireText(
  contracts,
  'import "./export-case-server-boundary"',
  "export-3l-api-contracts",
);

console.log("Export 3L security verification passed.");

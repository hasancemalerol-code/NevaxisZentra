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
  console.error(`Export security verification failed: ${message}`);
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
  "packages/NevaxisFactOS.Persistence/Authorization/ExportPermissionKeys.cs",
);
requireText(permissions, 'export.read', "ExportPermissionKeys");
requireText(permissions, 'export.write', "ExportPermissionKeys");
requireText(permissions, 'export.document.manage', "ExportPermissionKeys");
requireText(permissions, 'export.document.verify', "ExportPermissionKeys");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/ExportEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "RequireAuthorization", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.Read", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.Write", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.DocumentManage", "Export endpoints");
requireText(endpoints, "ExportPermissionKeys.DocumentVerify", "Export endpoints");
forbidText(endpoints, "MapDelete", "Export endpoints");
forbidText(endpoints, ".MapMethods(", "Export endpoints");

const migration = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260808140000_AddExportCaseReadinessAndDocumentCore.cs",
);
requireText(migration, "ENABLE ROW LEVEL SECURITY", "Export migration");
requireText(migration, "FORCE ROW LEVEL SECURITY", "Export migration");
forbidText(migration, "GRANT DELETE", "Export migration");

const boundary = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-server-boundary.ts",
);
requireText(
  boundary,
  "cannot be imported from the client",
  "export-case-server-boundary",
);

const apiClient = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-api-client.ts",
);
requireText(apiClient, 'import "./export-case-server-boundary"', "export-case-api-client");

console.log("Export security verification passed.");

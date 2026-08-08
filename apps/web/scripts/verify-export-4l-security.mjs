import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export 4L security verification failed: ${message}`);
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
requireText(permissions, "export.ai.read", "ExportPermissionKeys");
requireText(permissions, "export.ai.run", "ExportPermissionKeys");
requireText(permissions, "export.ai.review", "ExportPermissionKeys");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export4LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "RequireAuthorization", "Export 4L endpoints");
requireText(endpoints, "ExportPermissionKeys.AiRead", "Export 4L endpoints");
requireText(endpoints, "ExportPermissionKeys.AiRun", "Export 4L endpoints");
requireText(endpoints, "ExportPermissionKeys.AiReview", "Export 4L endpoints");
requireText(endpoints, "ResolveAccessAsync", "Export 4L endpoints");
requireText(endpoints, "facilityId is null || facilityId == Guid.Empty", "Export 4L endpoints");
requireText(endpoints, "IFacilityAccessResolver", "Export 4L endpoints");
forbidText(endpoints, "MapDelete", "Export 4L endpoints");

const program = readRequired(repoRootUrl, "apps/api/Program.cs");
requireText(program, "MapExport4LEndpoints", "Program.cs");

const apiClient = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-api-client.ts",
);
requireText(apiClient, 'import "./export-case-server-boundary"', "export-case-api-client");
forbidText(apiClient, 'method: "DELETE"', "export-case-api-client");
forbidText(apiClient, "method: 'DELETE'", "export-case-api-client");

const contracts = readRequired(
  webRootUrl,
  "src/features/export/server/export-4l-api-contracts.ts",
);
requireText(
  contracts,
  'import "./export-case-server-boundary"',
  "export-4l-api-contracts",
);
requireText(contracts, "caseFacilityUrl", "export-4l-api-contracts");
requireText(contracts, "exportFacilityUrl", "export-4l-api-contracts");

const entities = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/Export/ExportAiEntities.cs",
);
requireText(entities, "ITenantScopedEntity", "ExportAiEntities");
requireText(entities, "FacilityId", "ExportAiEntities");

const usageRecord = readRequired(
  repoRootUrl,
  "packages/CrevaFactOS.Persistence/AiUsage/AiUsageRecord.cs",
);
requireText(usageRecord, "ITenantScopedEntity", "AiUsageRecord");
requireText(usageRecord, "FacilityId", "AiUsageRecord");

console.log("Export 4L security verification passed.");

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
  console.error(`Export foundation verification failed: ${message}`);
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

const persistenceFiles = [
  "packages/NevaxisZentra.Persistence/Export/ExportCase.cs",
  "packages/NevaxisZentra.Persistence/Export/ExportCaseWriteRepository.cs",
  "packages/NevaxisZentra.Persistence/Export/ExportCaseReadRepository.cs",
  "packages/NevaxisZentra.Persistence/Export/ExportReadinessEngine.cs",
  "packages/NevaxisZentra.Persistence/Export/ExportDocument.cs",
  "packages/NevaxisZentra.Persistence/Export/ExportCaseActivity.cs",
  "packages/NevaxisZentra.Persistence/Authorization/ExportPermissionKeys.cs",
  "apps/api/Export/ExportEndpointRouteBuilderExtensions.cs",
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808140000_AddExportCaseReadinessAndDocumentCore.cs",
];

for (const relativePath of persistenceFiles) {
  if (!existsSync(resolvePath(repoRootUrl, relativePath))) {
    fail(`Required file is missing: ${relativePath}`);
  }
}

const writeRepo = readRequired(
  repoRootUrl,
  "packages/NevaxisZentra.Persistence/Export/ExportCaseWriteRepository.cs",
);
requireText(writeRepo, "EnsureExportCaseForOrderAsync", "ExportCaseWriteRepository");
requireText(writeRepo, "IsExport", "ExportCaseWriteRepository");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/ExportEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, '/api/export/cases', "Export endpoints");
requireText(endpoints, "/from-order", "Export endpoints");
requireText(endpoints, "/by-order/", "Export endpoints");
requireText(endpoints, "MapExportEndpoints", "Export endpoints");

const program = readRequired(repoRootUrl, "apps/api/Program.cs");
requireText(program, "AddNevaxisZentraExport", "Program.cs");
requireText(program, "MapExportEndpoints", "Program.cs");

const migration = readRequired(
  repoRootUrl,
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808140000_AddExportCaseReadinessAndDocumentCore.cs",
);
requireText(migration, "export_cases", "Export migration");
requireText(migration, "export_documents", "Export migration");
requireText(migration, "export_case_activities", "Export migration");
requireText(migration, "ENABLE ROW LEVEL SECURITY", "Export migration");

const orderDraft = readRequired(
  webRootUrl,
  "src/features/orders/domain/order-draft.ts",
);
requireText(orderDraft, "isExport", "order-draft.ts");
requireText(orderDraft, "destinationCountryCode", "order-draft.ts");

const webBridge = [
  "src/features/export/server/export-case-server-boundary.ts",
  "src/features/export/server/export-case-api-client.ts",
  "src/features/export/server/export-case-reader.ts",
  "src/features/export/server/export-case-writer.ts",
  "src/app/(app)/ihracat/page.tsx",
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
];

for (const relativePath of webBridge) {
  if (!existsSync(resolvePath(webRootUrl, relativePath))) {
    fail(`Required web file is missing: ${relativePath}`);
  }
}

console.log("Export foundation verification passed.");

import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export traceability verification failed: ${message}`);
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
  "apps/api/Export/Export3LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "/traceability", "Export 3L endpoints");
requireText(endpoints, "GetTraceabilityAsync", "Export 3L endpoints");

const domain = readRequired(webRootUrl, "src/features/export/domain/export-3l.ts");
requireText(domain, "ExportTraceability", "export-3l domain");

const panel = readRequired(
  webRootUrl,
  "src/features/export/components/export-traceability-panel.tsx",
);
requireText(panel, "export-traceability-panel", "traceability panel");
requireText(panel, "İZLENEBİLİRLİK", "traceability panel");

const detailPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
requireText(detailPage, "readTraceability", "detail page");

const workspace = readRequired(
  webRootUrl,
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(workspace, "ExportTraceabilityPanel", "export-detail-workspace");
requireText(workspace, "İzlenebilirlik", "export-detail-workspace");

const reader = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-reader.ts",
);
requireText(reader, "readTraceability", "export-case-reader");

console.log("Export traceability verification passed.");

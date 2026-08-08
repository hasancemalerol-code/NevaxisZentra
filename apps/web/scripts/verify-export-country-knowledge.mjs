import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export country knowledge verification failed: ${message}`);
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
requireText(endpoints, "/api/export/country-knowledge", "Export 3L endpoints");
requireText(endpoints, 'MapGet("/match"', "Export 3L endpoints");
requireText(endpoints, "/verify", "Export 3L endpoints");

const page = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/ulke-bilgisi/page.tsx",
);
requireText(page, "ExportCountryKnowledgePanel", "ulke-bilgisi page");
requireText(page, "readCountryKnowledgeEntries", "ulke-bilgisi page");
requireText(page, "Ülke Bilgisi", "ulke-bilgisi page");

const panel = readRequired(
  webRootUrl,
  "src/features/export/components/export-country-knowledge-panel.tsx",
);
requireText(panel, "createKnowledgeEntryAction", "knowledge panel");
requireText(panel, "verifyKnowledgeEntryAction", "knowledge panel");

const writer = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-writer.ts",
);
requireText(writer, "createKnowledgeEntryAction", "export-case-writer");
requireText(writer, "verifyKnowledgeEntryAction", "export-case-writer");

console.log("Export country knowledge verification passed.");

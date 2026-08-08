import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export country research verification failed: ${message}`);
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
requireText(endpoints, "/api/export/country-research", "Export 3L endpoints");
requireText(endpoints, "/findings", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.CountryResearchRead", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.CountryResearchManage", "Export 3L endpoints");
requireText(endpoints, "ExportPermissionKeys.CountryResearchVerify", "Export 3L endpoints");

const listPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/ulke-arastirma/page.tsx",
);
requireText(listPage, "Yeni Ülke Araştır", "ulke-arastirma page");
requireText(listPage, "readCountryResearchCases", "ulke-arastirma page");

const detailPage = readRequired(
  webRootUrl,
  "src/app/(app)/ihracat/ulke-arastirma/[researchCaseId]/page.tsx",
);
requireText(detailPage, "ExportCountryResearchWorkspace", "research detail page");
requireText(detailPage, "readCountryResearchCase", "research detail page");

const workspace = readRequired(
  webRootUrl,
  "src/features/export/components/export-country-research-workspace.tsx",
);
requireText(workspace, "addResearchFindingAction", "research workspace");
requireText(workspace, "reviewResearchCaseAction", "research workspace");
requireText(workspace, "completeResearchCaseAction", "research workspace");

const writer = readRequired(
  webRootUrl,
  "src/features/export/server/export-case-writer.ts",
);
requireText(writer, "createResearchCaseAction", "export-case-writer");
requireText(writer, "addResearchFindingAction", "export-case-writer");

console.log("Export country research verification passed.");

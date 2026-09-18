import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MOLD-1 authorization regression verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(projectRoot, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running MOLD-1 authorization regression verifier.");

const navigationVisibility = readRequired("apps/web/src/lib/navigation/navigation-visibility.ts");
const moldReader = readRequired("apps/web/src/features/molds/server/mold-reader.ts");
const moldApiClient = readRequired("apps/web/src/features/molds/server/mold-api-client.ts");
const page = readRequired("apps/web/src/app/(app)/kaliplar/page.tsx");
const helpers = readRequired("apps/api/Mold/MoldEndpointHelpers.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/Authorization/MoldPermissionKeys.cs");

requireText(permissions, "mold.read", "normalized read permission key");
requireText(permissions, "mold.manage", "normalized manage permission key");
requireText(navigationVisibility, 'item.permissionKey === "mold.read"', "navigation uses mold.read");
requireText(moldReader, "resolveMoldRequestContext", "shared mold request context");
requireText(moldReader, "probeMoldReadPermission", "server read probe");
requireText(moldApiClient, "getMoldWorkspaceSummary", "read permission probe uses mold API");
requireText(moldApiClient, "capabilities.canManage", "manage capability from workspace-summary");
requireText(moldApiClient, "capabilities.canRead", "read capability from workspace-summary");
const manageProbe = moldApiClient.slice(moldApiClient.indexOf("export async function probeMoldManagePermission"));
if (manageProbe.slice(0, manageProbe.indexOf("export ")).includes('method: "POST"')) {
  fail("probeMoldManagePermission must not use POST");
}
requireText(moldApiClient, "permission-denied", "API client maps 403");
requireText(helpers, "MoldPermissionKeys.Read", "route guard read key");
requireText(helpers, "MoldPermissionKeys.Manage", "route guard manage key");
requireText(page, "readMoldRegistry", "route uses server reader");
forbidText(navigationVisibility, '"use client"', "navigation filtering stays server-side");
forbidText(moldReader, "return true;", "mold reader must not force navigation visibility");

console.log("MOLD-1 authorization regression verification passed.");

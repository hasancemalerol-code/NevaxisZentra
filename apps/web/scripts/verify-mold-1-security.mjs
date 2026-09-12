import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MOLD-1 security verification failed: ${message}`);
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

console.log("Running MOLD-1 security verifier.");

const endpoints = readRequired("apps/api/Mold/MoldEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Mold/MoldEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260811200000_AddMoldRegistryLifecycleFoundation.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/Authorization/MoldPermissionKeys.cs");

requireText(endpoints, "RequireAuthorization", "mold endpoints");
requireText(endpoints, "IFacilityAccessResolver", "mold endpoints");
requireText(endpoints, "MoldEndpointHelpers.ResolveReadAccessAsync", "mold read access helper");
requireText(endpoints, "MoldEndpointHelpers.ResolveWriteAccessAsync", "mold manage access helper");
requireText(helpers, "Status401Unauthorized", "auth failures");
requireText(helpers, "Status403Forbidden", "permission failures");
requireText(permissions, "mold.read", "permission keys");
requireText(permissions, "mold.manage", "permission keys");
requireText(migration, "ENABLE ROW LEVEL SECURITY", "RLS");
requireText(migration, "FORCE ROW LEVEL SECURITY", "FORCE RLS");
requireText(migration, "GRANT SELECT, INSERT, UPDATE", "runtime grants");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "mold endpoints");

console.log("MOLD-1 security verification passed.");

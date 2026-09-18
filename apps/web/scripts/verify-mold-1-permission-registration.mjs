import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Mold permission-registration verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(projectRoot, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) fail(`${label} must contain "${expected}"`);
}

console.log("Running Mold permission-registration verifier.");

const permissions = readRequired("packages/NevaxisZentra.Persistence/Authorization/MoldPermissionKeys.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260811200000_AddMoldRegistryLifecycleFoundation.cs");
const pilotIds = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotIds.cs");
const helpers = readRequired("apps/api/Mold/MoldEndpointHelpers.cs");

requireText(permissions, 'Read = "mold.read"', "MoldPermissionKeys.Read");
requireText(permissions, 'Manage = "mold.manage"', "MoldPermissionKeys.Manage");
requireText(migration, "'mold.read'", "migration mold.read registration");
requireText(migration, "'mold.manage'", "migration mold.manage registration");
requireText(pilotIds, "MoldReadPermissionDefinitionId", "pilot mold read id");
requireText(pilotIds, "MoldManagePermissionDefinitionId", "pilot mold manage id");
requireText(helpers, "MoldPermissionKeys.Read", "API read guard");
requireText(helpers, "MoldPermissionKeys.Manage", "API manage guard");

console.log("Mold permission-registration verification passed.");

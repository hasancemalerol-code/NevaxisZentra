import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Mold role-assignment verification failed: ${message}`);
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

console.log("Running Mold role-assignment verifier.");

const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

requireText(bootstrap, "EnsureMoldReadPermissionAssignmentAsync", "mold read seed");
requireText(bootstrap, "EnsureMoldManagePermissionAssignmentAsync", "mold manage seed");
requireText(bootstrap, "EnsureMoldReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, tenantRole.Id", "tenant role mold read");
requireText(bootstrap, "EnsureMoldReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id", "reviewer role mold read");
requireText(bootstrap, "EnsureMoldReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, operatorRole.Id", "operator role mold read");
requireText(bootstrap, "EnsureMoldManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, operatorRole.Id", "operator role mold manage");
forbidText(bootstrap, "EnsureMoldManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, tenantRole.Id", "tenant role must not receive mold manage");
forbidText(bootstrap, "EnsureMoldManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id", "reviewer role must not receive mold manage");
forbidText(bootstrap, "EnsureMoldReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, qualityNegativeTestRole.Id", "negative-test role must not receive mold read");

console.log("Mold role-assignment verification passed.");

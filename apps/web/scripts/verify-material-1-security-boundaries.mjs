import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MATERIAL-1 security/boundary verification failed: ${message}`);
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

console.log("Running MATERIAL-1 security/boundary verifier.");

const endpoints = readRequired("apps/api/Material/MaterialEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Material/MaterialEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260812104059_AddMaterialRegistryTechnicalSpecifications.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/Authorization/MaterialPermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

requireText(endpoints, "RequireAuthorization", "material endpoints");
requireText(endpoints, "IFacilityAccessResolver", "material endpoints");
requireText(endpoints, "MaterialEndpointHelpers.ResolveReadAccessAsync", "material read access");
requireText(endpoints, "MaterialEndpointHelpers.ResolveWriteAccessAsync", "material manage access");
requireText(helpers, "Status401Unauthorized", "auth failures");
requireText(helpers, "Status403Forbidden", "permission failures");
requireText(permissions, "material.read", "permission keys");
requireText(permissions, "material.manage", "permission keys");
requireText(migration, "ENABLE ROW LEVEL SECURITY", "RLS");
requireText(migration, "FORCE ROW LEVEL SECURITY", "FORCE RLS");
requireText(migration, "GRANT SELECT, INSERT, UPDATE", "runtime grants");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "material endpoints");

requireText(bootstrap, "EnsureMaterialReadPermissionAssignmentAsync", "material read seed");
requireText(bootstrap, "EnsureMaterialManagePermissionAssignmentAsync", "material manage seed");
requireText(bootstrap, "EnsureMaterialReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, tenantRole.Id", "tenant role material read");
requireText(bootstrap, "EnsureMaterialReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id", "reviewer role material read");
requireText(bootstrap, "EnsureMaterialReadPermissionAssignmentAsync(\n            dbContext, tenant.Id, operatorRole.Id", "operator role material read");
requireText(bootstrap, "EnsureMaterialManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, operatorRole.Id", "operator role material manage");
forbidText(bootstrap, "EnsureMaterialManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, tenantRole.Id", "tenant role must not receive material manage");
forbidText(bootstrap, "EnsureMaterialManagePermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id", "reviewer must not receive material manage");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";

async function liveRequest(method, path, body, headers = {}) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: { "Content-Type": "application/json", ...headers },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json };
}

console.log("Running MATERIAL-1 live security scenarios.");
const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`API not ready: ${ready.status}`);

const operatorHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "production-pilot-operator",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};
const reviewerHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-reviewer",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};
const negativeHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "quality-negative-test-user",
  "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
};

const missingAuth = await liveRequest("GET", `/api/materials/workspace-summary?facilityId=${FACILITY}`);
assertLive("missing auth → 401", missingAuth.status === 401);

const readonlyGet = await liveRequest("GET", `/api/materials/workspace-summary?facilityId=${FACILITY}`, null, reviewerHeaders);
assertLive("read-only GET → 200", readonlyGet.status === 200);

const readonlyWrite = await liveRequest("POST", `/api/materials?facilityId=${FACILITY}`, {
  code: `SEC-RO-${Date.now()}`,
  name: "Should fail",
  materialCategory: "other",
  polymerFamily: "other",
  physicalForm: "granule",
  unitOfMeasure: "kg",
  isActive: true,
  sourceType: "manual",
}, reviewerHeaders);
assertLive("read-only write → 403", readonlyWrite.status === 403);

const unauthorized = await liveRequest("GET", `/api/materials/workspace-summary?facilityId=${FACILITY}`, null, negativeHeaders);
assertLive("unauthorized actor → 403", unauthorized.status === 403);

const unauthorizedFacility = await liveRequest("GET", `/api/materials/workspace-summary?facilityId=${UNAUTH_FACILITY}`, null, operatorHeaders);
assertLive("unauthorized facility → 403", unauthorizedFacility.status === 403);

function assertLive(label, ok) {
  if (!ok) fail(label);
  console.log(`✓ ${label}`);
}

console.log("MATERIAL-1 security/boundary verification passed.");

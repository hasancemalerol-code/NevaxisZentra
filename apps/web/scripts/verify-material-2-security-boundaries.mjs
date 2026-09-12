import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MATERIAL-2 security/boundary verification failed: ${message}`);
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

console.log("Running MATERIAL-2 security/boundary verifier.");

const endpoints = readRequired("apps/api/Material/MaterialLotEndpointRouteBuilderExtensions.cs");
const materialEndpoints = readRequired("apps/api/Material/MaterialEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Material/MaterialEndpointHelpers.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/Authorization/MaterialPermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/Material/MaterialLotRepository.cs");

requireText(endpoints, "RequireAuthorization", "lot endpoints");
requireText(endpoints, "IFacilityAccessResolver", "lot endpoints");
requireText(endpoints, "ResolveReadAccessAsync", "lot read access");
requireText(endpoints, "ResolveWriteAccessAsync", "lot manage access");
requireText(helpers, "MapLotWriteResult", "lot write result mapping");
requireText(helpers, "material.lot.concurrency-conflict", "lot concurrency failure code");
requireText(helpers, "material.lot.idempotency-conflict", "lot idempotency failure code");
requireText(helpers, "material.lot.invalid-quantity", "lot quantity failure code");
requireText(permissions, "material.read", "permission keys");
requireText(permissions, "material.manage", "permission keys");
requireText(materialEndpoints, "MapMaterialLotEndpoints", "material endpoint registration");
requireText(repository, "CreateIdempotencyKey", "lot repository idempotency");
forbidText(endpoints, "MapDelete", "lot endpoints must not expose delete");

requireText(bootstrap, "EnsureMaterialReadPermissionAssignmentAsync", "material read seed");
requireText(bootstrap, "EnsureMaterialManagePermissionAssignmentAsync", "material manage seed");
requireText(bootstrap, "EnsureMaterialLotPilotRecordsAsync", "lot pilot seed");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PILOT_LOT = "bb000001-00aa-4000-8000-0000000000aa";

async function liveRequest(method, path, body, headers = {}) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: { "Content-Type": "application/json", ...headers },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json };
}

function assertLive(label, ok) {
  if (!ok) fail(label);
  console.log(`✓ ${label}`);
}

console.log("Running MATERIAL-2 live security scenarios.");
const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`API not ready: ${ready.status}`);

const operatorHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "production-pilot-operator",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const reviewerHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-reviewer",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const negativeHeaders = {
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "quality-negative-test-user",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};

const missingAuth = await liveRequest("GET", `/api/materials/lots/workspace-summary?facilityId=${FACILITY}`);
assertLive("missing auth → 401", missingAuth.status === 401);

const readonlyGet = await liveRequest(
  "GET",
  `/api/materials/lots/workspace-summary?facilityId=${FACILITY}`,
  null,
  reviewerHeaders,
);
assertLive("read-only lot GET → 200", readonlyGet.status === 200);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/materials/lots?facilityId=${FACILITY}`,
  {
    materialId: "bb000001-00a2-4000-8000-0000000000a2",
    lotNumber: `SEC-RO-${Date.now()}`,
    unitOfMeasure: "kg",
    qualityStatus: "pending",
    sourceType: "manual",
    idempotencyKey: crypto.randomUUID(),
  },
  reviewerHeaders,
);
assertLive("read-only lot write → 403", readonlyWrite.status === 403);

const unauthorized = await liveRequest(
  "GET",
  `/api/materials/lots/workspace-summary?facilityId=${FACILITY}`,
  null,
  negativeHeaders,
);
assertLive("unauthorized actor → 403", unauthorized.status === 403);

const unauthorizedFacility = await liveRequest(
  "GET",
  `/api/materials/lots/workspace-summary?facilityId=${UNAUTH_FACILITY}`,
  null,
  operatorHeaders,
);
assertLive("unauthorized facility → 403", unauthorizedFacility.status === 403);

const operatorLotDetail = await liveRequest(
  "GET",
  `/api/materials/lots/${PILOT_LOT}?facilityId=${FACILITY}`,
  null,
  operatorHeaders,
);
assertLive("operator lot detail → 200", operatorLotDetail.status === 200);

console.log("MATERIAL-2 security/boundary verification passed.");

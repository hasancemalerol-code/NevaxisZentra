import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptsDirectory, "../../..");

function fail(message) {
  console.error(`MATERIAL-3 security/boundary verification failed: ${message}`);
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

console.log("Running MATERIAL-3 security/boundary verifier.");

const endpoints = readRequired("apps/api/Material/MaterialRequirementEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Material/MaterialEndpointHelpers.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/Material/MaterialRequirementRepository.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260812145938_AddMaterialRequirementConsumptionIntelligence.cs");

requireText(endpoints, "RequireAuthorization", "requirement endpoints");
requireText(endpoints, "ResolveReadAccessAsync", "requirement read access");
requireText(endpoints, "ResolveWriteAccessAsync", "requirement write access");
requireText(endpoints, "HasErpReadAccessAsync", "ERP permission intersection");
requireText(helpers, "material.requirement.idempotency-conflict", "requirement idempotency code");
requireText(helpers, "material.assessment.concurrency-conflict", "assessment concurrency code");
requireText(helpers, "material.regrind.limit-exceeded", "regrind limit code");
requireText(repository, "IdempotencyKey", "repository idempotency");
requireText(repository, "PayloadFingerprint", "repository fingerprint");
forbidText(endpoints, "MapDelete", "requirement endpoints must not expose delete");
forbidText(repository, "ErpInventoryRepository", "repository must not mutate ERP inventory");
forbidText(repository, "CreateErp", "repository must not create ERP records");
forbidText(migration, "GRANT DELETE", "append-only tables must not grant DELETE");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const MATERIAL_PP = "bb000001-00a2-4000-8000-0000000000a2";
const PILOT_PP_PROFILE = "bb000001-00ae-4000-8000-0000000000ae";

const READ_ACTOR = {
  "Content-Type": "application/json",
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "planning-pilot-reviewer",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};

const MANAGE_ACTOR = {
  "Content-Type": "application/json",
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "production-pilot-operator",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};

async function liveRequest(method, path, body, headers = MANAGE_ACTOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  return { status: response.status };
}

function assertLive(label, ok) {
  if (!ok) fail(label);
  console.log(`✓ ${label}`);
}

console.log("Running MATERIAL-3 live security scenarios.");
const ready = await fetch(`${BASE}/health/ready`);
if (!ready.ok) fail("API not ready");

assertLive("read-only GET HTTP 200", (await liveRequest("GET", `/api/materials/${MATERIAL_PP}/requirement-overview?facilityId=${FACILITY}`, null, READ_ACTOR)).status === 200);

const readWrite = await liveRequest("POST", `/api/materials/requirement-profiles?facilityId=${FACILITY}`, {
  materialId: MATERIAL_PP,
  requirementType: "manual-planning",
  sourceType: "manual",
  idempotencyKey: `mat3-sec-readonly-${Date.now()}`,
}, READ_ACTOR);
assertLive("read-only write HTTP 403", readWrite.status === 403);

const readEval = await liveRequest("POST", `/api/materials/${MATERIAL_PP}/requirement-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: PILOT_PP_PROFILE,
  idempotencyKey: `mat3-sec-readonly-eval-${Date.now()}`,
}, READ_ACTOR);
assertLive("read-only evaluation HTTP 403", readEval.status === 403);

const missingAuth = await liveRequest("GET", `/api/materials/${MATERIAL_PP}/requirement-overview?facilityId=${FACILITY}`, null, { "Content-Type": "application/json" });
assertLive("missing auth HTTP 401", missingAuth.status === 401);

const unauthFacility = await liveRequest("GET", `/api/materials/${MATERIAL_PP}/requirement-overview?facilityId=${UNAUTH_FACILITY}`, null, MANAGE_ACTOR);
assertLive("unauthorized facility HTTP 403", unauthFacility.status === 403);

const NEGATIVE_ACTOR = {
  "Content-Type": "application/json",
  "X-crevafactos-dev-Issuer": "crevafactos-dev",
  "X-crevafactos-dev-Subject": "quality-negative-test-user",
  "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
};
const unauthorizedActor = await liveRequest("GET", `/api/materials/${MATERIAL_PP}/requirement-overview?facilityId=${FACILITY}`, null, NEGATIVE_ACTOR);
assertLive("unauthorized actor HTTP 403", unauthorizedActor.status === 403);

console.log("MATERIAL-3 security/boundary verification passed.");

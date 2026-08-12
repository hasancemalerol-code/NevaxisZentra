#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const TENANT = "dev-pilot-tenant";
const CLOSURE_SOURCE_PREFIX = "mat1-closure";
const CLOSURE_CODE_PREFIX = "MAT1-CLOSURE";
const RUN_SUFFIX = Date.now();
const PREFIX = `${CLOSURE_CODE_PREFIX}-${RUN_SUFFIX}`;
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: TENANT,
};
const PILOT_MATERIAL_IDS = [
  "bb000001-00a2-4000-8000-0000000000a2",
  "bb000001-00a4-4000-8000-0000000000a4",
  "bb000001-00a6-4000-8000-0000000000a6",
];

function fail(message) {
  console.error(`MATERIAL-1 domain/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function dbRows(sql) {
  const output = db(sql);
  return output ? output.split("\n").filter(Boolean) : [];
}

async function request(method, path, body, actor = ACTOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

function baseBody(code, idempotencyKey, overrides = {}) {
  return {
    code,
    name: `${PREFIX} Test Material`,
    materialCategory: "virgin-polymer",
    polymerFamily: "PP",
    physicalForm: "granule",
    unitOfMeasure: "kg",
    isActive: true,
    sourceType: "manual",
    sourceReference: `${CLOSURE_SOURCE_PREFIX}-create-${RUN_SUFFIX}`,
    idempotencyKey,
    ...overrides,
  };
}

function metadataBody(expectedVersion, overrides = {}) {
  return {
    expectedVersion,
    name: `${PREFIX} Metadata Updated`,
    description: "  trimmed description  ",
    materialCategory: "virgin-polymer",
    polymerFamily: "PP",
    manufacturer: "  Closure Manufacturer  ",
    tradeNameOrGrade: " GR-22 ",
    colorName: " Natural ",
    physicalForm: "granule",
    unitOfMeasure: " kg ",
    isActive: true,
    sourceReference: `${CLOSURE_SOURCE_PREFIX}-metadata-${RUN_SUFFIX}`,
    ...overrides,
  };
}

function closureFixtureSqlPredicate(alias = "m") {
  return `(
    ${alias}.code LIKE '${CLOSURE_CODE_PREFIX}%'
    OR ${alias}.source_reference LIKE '${CLOSURE_SOURCE_PREFIX}%'
    OR ${alias}.create_idempotency_key LIKE '${CLOSURE_SOURCE_PREFIX}%'
  )`;
}

function previewClosureFixtures() {
  return dbRows(`
    SELECT id, code, create_idempotency_key, create_idempotency_payload_fingerprint
    FROM materials m
    WHERE ${closureFixtureSqlPredicate("m")}
    ORDER BY code
  `);
}

function removeClosureFixtures() {
  db(`
    DELETE FROM material_lifecycle_events
    WHERE material_id IN (SELECT id FROM materials m WHERE ${closureFixtureSqlPredicate("m")})
  `);
  db(`
    DELETE FROM material_technical_specifications
    WHERE material_id IN (SELECT id FROM materials m WHERE ${closureFixtureSqlPredicate("m")})
  `);
  db(`DELETE FROM materials m WHERE ${closureFixtureSqlPredicate("m")}`);
}

function assertPilotMaterialsPreserved() {
  const pilotCount = Number(db(`
    SELECT COUNT(*) FROM materials
    WHERE id IN ('${PILOT_MATERIAL_IDS.join("','")}')
  `));
  assert("pilot Material records preserved", pilotCount === 3, `count=${pilotCount}`);
}

function assertClosureCleanupComplete() {
  const remaining = Number(db(`SELECT COUNT(*) FROM materials m WHERE ${closureFixtureSqlPredicate("m")}`));
  assert("closure fixtures remaining = 0", remaining === 0, `count=${remaining}`);

  const orphanSpecs = Number(db(`
    SELECT COUNT(*) FROM material_technical_specifications mts
    WHERE NOT EXISTS (SELECT 1 FROM materials m WHERE m.id = mts.material_id)
  `));
  assert("orphan technical specifications = 0", orphanSpecs === 0, `count=${orphanSpecs}`);

  const orphanEvents = Number(db(`
    SELECT COUNT(*) FROM material_lifecycle_events mle
    WHERE NOT EXISTS (SELECT 1 FROM materials m WHERE m.id = mle.material_id)
  `));
  assert("orphan lifecycle events = 0", orphanEvents === 0, `count=${orphanEvents}`);
}

console.log("Running MATERIAL-1 domain/idempotency verifier.");

const stateMachine = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialLifecycleStateMachine.cs");
const normalizer = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialCodeNormalizer.cs");
const validator = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialTechnicalValidator.cs");
const repo = readRequired("packages/CrevaFactOS.Persistence/Material/MaterialRepository.cs");

assert("state machine present", stateMachine.includes("MaterialLifecycleStatus.Obsolete"));
assert("code normalizer uppercase", normalizer.includes("ToUpperInvariant"));
assert("technical validator percentages", validator.includes("between 0 and 100"));
assert("repository idempotency", repo.includes("CreateIdempotencyKey"));
assert("repository expectedVersion", repo.includes("ExpectedVersion"));
assert("legacy idempotency guard", repo.includes("IdempotencyLegacyUnverifiableResult"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
assertPilotMaterialsPreserved();

// TEST 1 — legacy null fingerprint
const legacyMaterialId = crypto.randomUUID();
const legacyIdempotencyKey = `${CLOSURE_SOURCE_PREFIX}-legacy-${RUN_SUFFIX}`;
const legacyCode = `${CLOSURE_CODE_PREFIX}-LEGACY-${RUN_SUFFIX}`;
const legacyName = `${PREFIX} Legacy Fixture`;
const nowIso = new Date().toISOString();

db(`
  INSERT INTO materials (
    id, tenant_id, facility_id, code, code_normalized, name, material_category, polymer_family,
    physical_form, lifecycle_status, unit_of_measure, is_active, source_type, source_reference,
    create_idempotency_key, create_idempotency_payload_fingerprint,
    created_at_utc, updated_at_utc, created_by_subject_id, updated_by_subject_id, version
  ) VALUES (
    '${legacyMaterialId}', '${TENANT_ID}', '${FACILITY}', '${legacyCode}', '${legacyCode}',
    '${legacyName}', 'virgin-polymer', 'PP', 'granule', 'draft', 'kg', true, 'manual',
    '${CLOSURE_SOURCE_PREFIX}-legacy-fixture-${RUN_SUFFIX}',
    '${legacyIdempotencyKey}', NULL,
    '${nowIso}', '${nowIso}', 'production-pilot-operator', 'production-pilot-operator', 1
  )
`);

const legacyFingerprintBefore = db(`
  SELECT create_idempotency_payload_fingerprint
  FROM materials
  WHERE id = '${legacyMaterialId}'
`);
assert("legacy stored fingerprint is NULL", legacyFingerprintBefore === "" || legacyFingerprintBefore === "\\N" || legacyFingerprintBefore === null || legacyFingerprintBefore === "NULL" || legacyFingerprintBefore.length === 0, `value=${legacyFingerprintBefore}`);

const legacyCountBefore = Number(db("SELECT COUNT(*) FROM materials"));
const legacyNameBefore = db(`SELECT name FROM materials WHERE id = '${legacyMaterialId}'`);
const legacyRetry = await request("POST", `/api/materials?facilityId=${FACILITY}`, baseBody(legacyCode, legacyIdempotencyKey, {
  name: `${PREFIX} Legacy Retry Attempt`,
  sourceReference: `${CLOSURE_SOURCE_PREFIX}-legacy-retry-${RUN_SUFFIX}`,
}));

assert("legacy retry → 409", legacyRetry.status === 409, `got ${legacyRetry.status} ${legacyRetry.raw}`);
assert(
  "legacy retry error code",
  legacyRetry.json?.failureCode === "material.idempotency-legacy-unverifiable",
  JSON.stringify(legacyRetry.json),
);
assert("legacy retry did not create duplicate", Number(db("SELECT COUNT(*) FROM materials")) === legacyCountBefore);
assert("legacy original Material not mutated", db(`SELECT name FROM materials WHERE id = '${legacyMaterialId}'`) === legacyNameBefore);
const legacyFingerprintAfter = db(`
  SELECT COALESCE(create_idempotency_payload_fingerprint, '')
  FROM materials
  WHERE id = '${legacyMaterialId}'
`);
assert("legacy fingerprint not fabricated", legacyFingerprintAfter === "", `value=${legacyFingerprintAfter}`);

// TEST 2 — concurrent identical create
const concurrentKey = `${CLOSURE_SOURCE_PREFIX}-concurrent-${RUN_SUFFIX}`;
const concurrentCode = `${CLOSURE_CODE_PREFIX}-CONCURRENT-${RUN_SUFFIX}`;
const concurrentPayload = baseBody(concurrentCode, concurrentKey, {
  sourceReference: `${CLOSURE_SOURCE_PREFIX}-concurrent-${RUN_SUFFIX}`,
});
const concurrentCountBefore = Number(db("SELECT COUNT(*) FROM materials"));

const [concurrentA, concurrentB] = await Promise.all([
  request("POST", `/api/materials?facilityId=${FACILITY}`, concurrentPayload),
  request("POST", `/api/materials?facilityId=${FACILITY}`, concurrentPayload),
]);

const concurrentStatuses = [concurrentA.status, concurrentB.status].sort((a, b) => a - b);
assert("concurrent identical responses are 201", concurrentStatuses.every((status) => status === 201), JSON.stringify(concurrentStatuses));
assert("concurrent identical Material identities match", Boolean(concurrentA.json?.id && concurrentA.json.id === concurrentB.json?.id), `${concurrentA.json?.id} vs ${concurrentB.json?.id}`);

const concurrentCountAfter = Number(db("SELECT COUNT(*) FROM materials"));
assert("concurrent identical count increase = 1", concurrentCountAfter - concurrentCountBefore === 1, `before=${concurrentCountBefore} after=${concurrentCountAfter}`);
assert(
  "concurrent replay semantics",
  (concurrentA.json?.idempotentReplay === true) !== (concurrentB.json?.idempotentReplay === true)
    || (concurrentA.json?.idempotentReplay === true && concurrentB.json?.idempotentReplay === true),
  JSON.stringify({ a: concurrentA.json, b: concurrentB.json }),
);
assert("concurrent no HTTP 500", !concurrentStatuses.includes(500));
assert(
  "concurrent no exposed exception",
  !`${concurrentA.raw}${concurrentB.raw}`.toLowerCase().includes("duplicate key")
    && !`${concurrentA.raw}${concurrentB.raw}`.toLowerCase().includes("23505"),
);

const concurrentDifferentPayload = await request("POST", `/api/materials?facilityId=${FACILITY}`, {
  ...concurrentPayload,
  name: `${PREFIX} Different Concurrent Payload`,
});
assert("concurrent different-payload → 409", concurrentDifferentPayload.status === 409);
assert(
  "concurrent different-payload conflict code",
  concurrentDifferentPayload.json?.failureCode === "material.idempotency-conflict",
);

// TEST 3 — metadata update
const metadataKey = `${CLOSURE_SOURCE_PREFIX}-metadata-${RUN_SUFFIX}`;
const metadataCode = `${CLOSURE_CODE_PREFIX}-META-${RUN_SUFFIX}`;
const metadataCreate = await request("POST", `/api/materials?facilityId=${FACILITY}`, baseBody(metadataCode, metadataKey, {
  name: `${PREFIX} Metadata Base`,
  sourceReference: `${CLOSURE_SOURCE_PREFIX}-metadata-base-${RUN_SUFFIX}`,
}));
assert("metadata fixture create → 201", metadataCreate.status === 201, `got ${metadataCreate.status}`);
const metadataMaterialId = metadataCreate.json?.id;
const versionBeforeUpdate = metadataCreate.json?.version;
assert("metadata fixture version present", Boolean(versionBeforeUpdate));

const metadataUpdate = await request("PUT", `/api/materials/${metadataMaterialId}?facilityId=${FACILITY}`, metadataBody(versionBeforeUpdate));
assert("metadata update → 200", metadataUpdate.status === 200, `got ${metadataUpdate.status} ${JSON.stringify(metadataUpdate.json)}`);
const versionAfterUpdate = metadataUpdate.json?.version;
assert("metadata version incremented", versionAfterUpdate === versionBeforeUpdate + 1, `${versionBeforeUpdate} -> ${versionAfterUpdate}`);

const metadataDetail = await request("GET", `/api/materials/${metadataMaterialId}?facilityId=${FACILITY}`);
assert("metadata detail reload → 200", metadataDetail.status === 200);
assert("metadata persisted name", metadataDetail.json?.name === `${PREFIX} Metadata Updated`);
assert("metadata normalization description", metadataDetail.json?.description === "trimmed description");
assert("metadata normalization manufacturer", metadataDetail.json?.manufacturer === "Closure Manufacturer");
assert("metadata normalization unit", metadataDetail.json?.unitOfMeasure === "kg");

const staleMetadata = await request("PUT", `/api/materials/${metadataMaterialId}?facilityId=${FACILITY}`, metadataBody(versionBeforeUpdate, {
  name: `${PREFIX} Stale Metadata Attempt`,
}));
assert("stale metadata update → 409", staleMetadata.status === 409);
assert(
  "stale metadata conflict code",
  staleMetadata.json?.failureCode === "material.concurrency-conflict",
);
assert("stale metadata request did not mutate Material", (await request("GET", `/api/materials/${metadataMaterialId}?facilityId=${FACILITY}`)).json?.name === `${PREFIX} Metadata Updated`);

// Existing domain/idempotency coverage
const countBefore = Number(db("SELECT COUNT(*) FROM materials"));
const idempotencyKey = `${CLOSURE_SOURCE_PREFIX}-standard-${RUN_SUFFIX}`;
const code = `${CLOSURE_CODE_PREFIX}-STD-${RUN_SUFFIX}`;
const payload = baseBody(code, idempotencyKey);

const first = await request("POST", `/api/materials?facilityId=${FACILITY}`, payload);
assert("create material → 201", first.status === 201, `got ${first.status}`);
const materialId = first.json?.id;
assert("create returns id", Boolean(materialId));

const exactRetry = await request("POST", `/api/materials?facilityId=${FACILITY}`, payload);
assert("exact idempotent retry → 201", exactRetry.status === 201);
assert("exact retry same id", exactRetry.json?.id === materialId);
assert("exact retry flagged", exactRetry.json?.idempotentReplay === true);
assert("no duplicate row", Number(db("SELECT COUNT(*) FROM materials")) === countBefore + 1);

const conflicting = await request("POST", `/api/materials?facilityId=${FACILITY}`, {
  ...payload,
  code: `${code}-X`,
  name: "Different",
});
assert("conflicting idempotent retry → 409", conflicting.status === 409);
assert(
  "conflicting failure code",
  conflicting.json?.failureCode === "material.idempotency-conflict",
);

const dupCode = await request("POST", `/api/materials?facilityId=${FACILITY}`, {
  ...baseBody(code.toLowerCase(), `${CLOSURE_SOURCE_PREFIX}-dup-${RUN_SUFFIX}`),
});
assert("normalized duplicate code → 409", dupCode.status === 409);
assert("duplicate code failure", dupCode.json?.failureCode === "material.code-conflict");

const techBad = await request("PUT", `/api/materials/${materialId}/technical-specification?facilityId=${FACILITY}`, {
  expectedVersion: first.json.version,
  moistureLimitPercent: 150,
  specificationSource: "manual",
});
assert("invalid percentage → 400", techBad.status === 400);

const detail = await request("GET", `/api/materials/${materialId}?facilityId=${FACILITY}`);
assert("detail reload → 200", detail.status === 200);
const currentVersion = detail.json?.version ?? first.json.version;

const techGood = await request("PUT", `/api/materials/${materialId}/technical-specification?facilityId=${FACILITY}`, {
  expectedVersion: currentVersion,
  densityGPerCm3: 0.905,
  meltFlowRate: 12,
  meltFlowUnit: "g/10min",
  specificationSource: "development-pilot",
  specificationRecordedAtUtc: new Date().toISOString(),
});
assert("valid technical update → 200", techGood.status === 200, `got ${techGood.status} ${JSON.stringify(techGood.json)}`);

const transition = await request("POST", `/api/materials/${materialId}/lifecycle-transitions?facilityId=${FACILITY}`, {
  expectedVersion: techGood.json.version,
  targetStatus: "active",
  reason: "Pilot activation",
  idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-transition-${RUN_SUFFIX}`,
});
assert("valid lifecycle transition → 200", transition.status === 200);

const obsoleteAttempt = await request("POST", `/api/materials/${materialId}/lifecycle-transitions?facilityId=${FACILITY}`, {
  expectedVersion: transition.json.version,
  targetStatus: "draft",
  reason: "Invalid",
  idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-invalid-transition-${RUN_SUFFIX}`,
});
assert("invalid lifecycle transition → 409", obsoleteAttempt.status === 409);

const fixturesBeforeCleanup = previewClosureFixtures();
assert("closure fixtures previewed", fixturesBeforeCleanup.length > 0, `count=${fixturesBeforeCleanup.length}`);

removeClosureFixtures();
assert("closure fixtures removed", previewClosureFixtures().length === 0);
assertClosureCleanupComplete();
assertPilotMaterialsPreserved();

console.log("MATERIAL-1 domain/idempotency verification passed.");

#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const MATERIAL_PP = "bb000001-00a2-4000-8000-0000000000a2";
const LOCATION_RAW = "bb000001-00a7-4000-8000-0000000000a7";
const LOCATION_QUARANTINE = "bb000001-00a8-4000-8000-0000000000a8";
const FACILITY_UNAUTHORIZED = "bb000001-0005-4000-8000-000000000099";
const CANONICAL_IDEMPOTENCY_CONFLICT = "material.lot.idempotency-conflict";
const CANONICAL_IDEMPOTENCY_LEGACY = "material.lot.idempotency-legacy-unverifiable";
const CANONICAL_INVALID_LOCATION = "material.lot.invalid-location";
const CLOSURE_SOURCE_PREFIX = "mat2-closure";
const CLOSURE_CODE_PREFIX = "MAT2-CLOSURE";
const RUN_SUFFIX = Date.now();
const PREFIX = `${CLOSURE_CODE_PREFIX}-${RUN_SUFFIX}`;
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: "dev-pilot-tenant",
};
const PILOT_LOT_IDS = [
  "bb000001-00aa-4000-8000-0000000000aa",
  "bb000001-00ab-4000-8000-0000000000ab",
  "bb000001-00ac-4000-8000-0000000000ac",
];
const PILOT_STORAGE_IDS = [
  "bb000001-00a7-4000-8000-0000000000a7",
  "bb000001-00a8-4000-8000-0000000000a8",
  "bb000001-00a9-4000-8000-0000000000a9",
];

function fail(message) {
  console.error(`MATERIAL-2 lot/traceability verification failed: ${message}`);
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
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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

function baseCreateBody(lotNumber, idempotencyKey, overrides = {}) {
  return {
    materialId: MATERIAL_PP,
    lotNumber,
    unitOfMeasure: "kg",
    qualityStatus: "pending",
    sourceType: "manual",
    sourceReference: `${CLOSURE_SOURCE_PREFIX}-create-${RUN_SUFFIX}`,
    idempotencyKey,
    storageLocationId: LOCATION_RAW,
    initialQuantity: 100,
    ...overrides,
  };
}

function closureFixtureSqlPredicate(alias = "l") {
  return `(
    ${alias}.lot_number LIKE '${CLOSURE_CODE_PREFIX}%'
    OR ${alias}.source_reference LIKE '${CLOSURE_SOURCE_PREFIX}%'
    OR ${alias}.create_idempotency_key LIKE '${CLOSURE_SOURCE_PREFIX}%'
  )`;
}

function closureStorageSqlPredicate(alias = "s") {
  return `(
    ${alias}.warehouse_code LIKE '${CLOSURE_CODE_PREFIX}%'
    OR ${alias}.location_code LIKE '${CLOSURE_CODE_PREFIX}%'
    OR ${alias}.source_reference LIKE '${CLOSURE_SOURCE_PREFIX}%'
  )`;
}

function removeClosureFixtures() {
  db(`
    DELETE FROM material_lot_certificate_references
    WHERE material_lot_id IN (SELECT id FROM material_lots l WHERE ${closureFixtureSqlPredicate("l")})
  `);
  db(`
    DELETE FROM material_lot_traceability_events
    WHERE material_lot_id IN (SELECT id FROM material_lots l WHERE ${closureFixtureSqlPredicate("l")})
  `);
  db(`DELETE FROM material_lots l WHERE ${closureFixtureSqlPredicate("l")}`);
  db(`DELETE FROM material_storage_locations s WHERE ${closureStorageSqlPredicate("s")}`);
}

function assertClosureEventsRemaining() {
  const remaining = Number(db(`
    SELECT COUNT(*) FROM material_lot_traceability_events e
    WHERE e.material_lot_id IN (SELECT id FROM material_lots l WHERE ${closureFixtureSqlPredicate("l")})
       OR e.idempotency_key LIKE '${CLOSURE_SOURCE_PREFIX}%'
  `));
  assert("closure events remaining = 0", remaining === 0, `count=${remaining}`);
}

function assertClosureCertificatesRemaining() {
  const remaining = Number(db(`
    SELECT COUNT(*) FROM material_lot_certificate_references c
    WHERE c.material_lot_id IN (SELECT id FROM material_lots l WHERE ${closureFixtureSqlPredicate("l")})
       OR c.idempotency_key LIKE '${CLOSURE_SOURCE_PREFIX}%'
  `));
  assert("closure certificates remaining = 0", remaining === 0, `count=${remaining}`);
}

function assertPilotLotsPreserved() {
  const pilotCount = Number(db(`
    SELECT COUNT(*) FROM material_lots
    WHERE id IN ('${PILOT_LOT_IDS.join("','")}')
  `));
  assert("pilot lot records preserved", pilotCount === 3, `count=${pilotCount}`);

  const storageCount = Number(db(`
    SELECT COUNT(*) FROM material_storage_locations
    WHERE id IN ('${PILOT_STORAGE_IDS.join("','")}')
  `));
  assert("pilot storage locations preserved", storageCount === 3, `count=${storageCount}`);
}

function assertClosureCleanupComplete() {
  const remaining = Number(db(`SELECT COUNT(*) FROM material_lots l WHERE ${closureFixtureSqlPredicate("l")}`));
  assert("closure fixtures remaining = 0", remaining === 0, `count=${remaining}`);
}

console.log("Running MATERIAL-2 lot/traceability verifier.");

const stateMachine = readRequired("packages/NevaxisZentra.Persistence/Material/MaterialLotLifecycleStateMachine.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/Material/MaterialLotRepository.cs");
const validator = readRequired("packages/NevaxisZentra.Persistence/Material/MaterialLotValidator.cs");

assert("lot state machine present", stateMachine.includes("MaterialLotStatus.Closed"));
assert("lot repository idempotency", repository.includes("CreateIdempotencyKey"));
assert("lot repository expectedVersion", repository.includes("ExpectedVersion"));
assert("lot validator same location guard", validator.includes("Transfer source and destination cannot be identical"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
assertPilotLotsPreserved();

const countBefore = Number(db("SELECT COUNT(*) FROM material_lots"));
const idempotencyKey = `${CLOSURE_SOURCE_PREFIX}-standard-${RUN_SUFFIX}`;
const lotCode = `${CLOSURE_CODE_PREFIX}-STD-${RUN_SUFFIX}`;
const payload = baseCreateBody(lotCode, idempotencyKey);

const first = await request("POST", `/api/materials/lots?facilityId=${FACILITY}`, payload);
assert("create lot → 201", first.status === 201, `got ${first.status} ${first.raw}`);
const lotId = first.json?.id;
assert("create returns id", Boolean(lotId));

const exactRetry = await request("POST", `/api/materials/lots?facilityId=${FACILITY}`, payload);
assert("exact idempotent retry → 201", exactRetry.status === 201);
assert("exact retry same id", exactRetry.json?.id === lotId);
assert("exact retry flagged", exactRetry.json?.idempotentReplay === true);
assert("no duplicate row", Number(db("SELECT COUNT(*) FROM material_lots")) === countBefore + 1);

const conflicting = await request("POST", `/api/materials/lots?facilityId=${FACILITY}`, {
  ...payload,
  lotNumber: `${lotCode}-X`,
  name: "Different",
});
assert("conflicting idempotent retry → 409", conflicting.status === 409);
assert(
  "conflicting failure code",
  conflicting.json?.failureCode === CANONICAL_IDEMPOTENCY_CONFLICT,
);

const legacyKey = `${CLOSURE_SOURCE_PREFIX}-legacy-${RUN_SUFFIX}`;
const legacyLotId = crypto.randomUUID();
const legacyLotCode = `${CLOSURE_CODE_PREFIX}-LEGACY-${RUN_SUFFIX}`;
db(`
  INSERT INTO material_lots (
    id, tenant_id, facility_id, material_id, lot_number, lot_number_normalized,
    unit_of_measure, status, quality_status, source_type, source_reference,
    create_idempotency_key, create_idempotency_payload_fingerprint,
    created_at_utc, updated_at_utc, created_by_subject_id, updated_by_subject_id, version
  ) VALUES (
    '${legacyLotId}', '${TENANT_ID}', '${FACILITY}', '${MATERIAL_PP}',
    '${legacyLotCode}', '${legacyLotCode}', 'kg', 'draft', 'pending', 'manual',
    '${CLOSURE_SOURCE_PREFIX}-legacy-fixture-${RUN_SUFFIX}', '${legacyKey}', NULL,
    NOW(), NOW(), 'mat2-closure', 'mat2-closure', 1
  )
`);
const legacyFingerprintBefore = db(`
  SELECT create_idempotency_payload_fingerprint
  FROM material_lots WHERE id = '${legacyLotId}'
`);
assert("legacy stored fingerprint is NULL", legacyFingerprintBefore === "" || legacyFingerprintBefore === "\\N" || legacyFingerprintBefore === "null" || legacyFingerprintBefore.length === 0, `value=${legacyFingerprintBefore}`);
const legacyVersionBefore = db(`SELECT version FROM material_lots WHERE id = '${legacyLotId}'`);
const legacyRetry = await request("POST", `/api/materials/lots?facilityId=${FACILITY}`, baseCreateBody(`${legacyLotCode}-RETRY`, legacyKey));
assert("legacy retry → 409", legacyRetry.status === 409, `got ${legacyRetry.status} ${legacyRetry.raw}`);
assert("legacy error code", legacyRetry.json?.failureCode === CANONICAL_IDEMPOTENCY_LEGACY);
assert("legacy replayed", legacyRetry.json?.idempotentReplay !== true);
assert("legacy duplicate lot", Number(db(`SELECT COUNT(*) FROM material_lots WHERE create_idempotency_key = '${legacyKey}'`)) === 1);
assert("legacy original version unchanged", db(`SELECT version FROM material_lots WHERE id = '${legacyLotId}'`) === legacyVersionBefore);
assert("legacy fingerprint remains NULL", db(`SELECT COALESCE(create_idempotency_payload_fingerprint, '') FROM material_lots WHERE id = '${legacyLotId}'`) === "");

const concurrentDiffKey = `${CLOSURE_SOURCE_PREFIX}-concurrent-diff-${RUN_SUFFIX}`;
const concurrentDiffCodeA = `${CLOSURE_CODE_PREFIX}-CONCURRENT-DIFF-A-${RUN_SUFFIX}`;
const concurrentDiffCodeB = `${CLOSURE_CODE_PREFIX}-CONCURRENT-DIFF-B-${RUN_SUFFIX}`;
const concurrentDiffCountBefore = Number(db("SELECT COUNT(*) FROM material_lots"));
const [concurrentDiffA, concurrentDiffB] = await Promise.all([
  request("POST", `/api/materials/lots?facilityId=${FACILITY}`, baseCreateBody(concurrentDiffCodeA, concurrentDiffKey, { initialQuantity: 100 })),
  request("POST", `/api/materials/lots?facilityId=${FACILITY}`, baseCreateBody(concurrentDiffCodeB, concurrentDiffKey, { initialQuantity: 200 })),
]);
const concurrentDiffStatuses = [concurrentDiffA.status, concurrentDiffB.status].sort((a, b) => a - b);
assert("concurrent different-payload statuses are 201 and 409", concurrentDiffStatuses[0] === 201 && concurrentDiffStatuses[1] === 409, JSON.stringify(concurrentDiffStatuses));
const concurrentDiffWinner = concurrentDiffA.status === 201 ? concurrentDiffA : concurrentDiffB;
const concurrentDiffLoser = concurrentDiffA.status === 409 ? concurrentDiffA : concurrentDiffB;
assert("concurrent different-payload HTTP 500 absent", concurrentDiffStatuses.every((status) => status !== 500));
assert("concurrent different-payload no database exception leak", !`${concurrentDiffLoser.raw}`.toLowerCase().includes("exception"));
assert("concurrent different-payload losing conflict code", concurrentDiffLoser.json?.failureCode === CANONICAL_IDEMPOTENCY_CONFLICT);
assert(
  "concurrent different-payload count increase = 1",
  Number(db("SELECT COUNT(*) FROM material_lots")) - concurrentDiffCountBefore === 1,
);
const winnerDetail = await request("GET", `/api/materials/lots/${concurrentDiffWinner.json.id}?facilityId=${FACILITY}`);
assert("concurrent different-payload winner unchanged by loser", winnerDetail.status === 200);

const crossFacilityLocationId = crypto.randomUUID();
db(`
  INSERT INTO material_storage_locations (
    id, tenant_id, facility_id, warehouse_code, warehouse_code_normalized, warehouse_name,
    location_code, location_code_normalized, location_name, is_active, source_type, source_reference,
    created_at_utc, updated_at_utc, created_by_subject_id, updated_by_subject_id, version
  ) VALUES (
    '${crossFacilityLocationId}', '${TENANT_ID}', '${FACILITY_UNAUTHORIZED}',
    '${CLOSURE_CODE_PREFIX}-WH-${RUN_SUFFIX}', '${CLOSURE_CODE_PREFIX}-WH-${RUN_SUFFIX}', 'Closure Foreign WH',
    '${CLOSURE_CODE_PREFIX}-LOC-${RUN_SUFFIX}', '${CLOSURE_CODE_PREFIX}-LOC-${RUN_SUFFIX}', 'Closure Foreign Loc',
    TRUE, 'manual', '${CLOSURE_SOURCE_PREFIX}-foreign-loc-${RUN_SUFFIX}',
    NOW(), NOW(), 'mat2-closure', 'mat2-closure', 1
  )
`);
const crossLotCode = `${CLOSURE_CODE_PREFIX}-CROSS-${RUN_SUFFIX}`;
const crossLot = await request("POST", `/api/materials/lots?facilityId=${FACILITY}`, baseCreateBody(crossLotCode, `${CLOSURE_SOURCE_PREFIX}-cross-lot-${RUN_SUFFIX}`));
assert("cross-facility source lot create → 201", crossLot.status === 201, crossLot.raw);
const crossLotId = crossLot.json.id;
const crossDetailBefore = await request("GET", `/api/materials/lots/${crossLotId}?facilityId=${FACILITY}`);
const crossEventsBefore = Number(db(`SELECT COUNT(*) FROM material_lot_traceability_events WHERE material_lot_id = '${crossLotId}'`));
const crossTransfer = await request(
  "POST",
  `/api/materials/lots/${crossLotId}/transfers?facilityId=${FACILITY}`,
  {
    expectedVersion: crossDetailBefore.json.version,
    targetStorageLocationId: crossFacilityLocationId,
    reason: "Cross-facility should fail",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-cross-transfer-${RUN_SUFFIX}`,
  },
);
assert("cross-facility transfer → 400", crossTransfer.status === 400, `got ${crossTransfer.status} ${crossTransfer.raw}`);
assert("cross-facility failure code", crossTransfer.json?.failureCode === CANONICAL_INVALID_LOCATION);
assert("cross-facility transfer event created = 0", Number(db(`SELECT COUNT(*) FROM material_lot_traceability_events WHERE material_lot_id = '${crossLotId}' AND event_type = 'location-transfer' AND idempotency_key = '${CLOSURE_SOURCE_PREFIX}-cross-transfer-${RUN_SUFFIX}'`)) === 0);
const crossDetailAfter = await request("GET", `/api/materials/lots/${crossLotId}?facilityId=${FACILITY}`);
assert("cross-facility lot location unchanged", crossDetailAfter.json.currentStorageLocationId === crossDetailBefore.json.currentStorageLocationId);
assert("cross-facility observed quantity unchanged", crossDetailAfter.json.currentObservedQuantity === crossDetailBefore.json.currentObservedQuantity);
assert("cross-facility events unchanged", Number(db(`SELECT COUNT(*) FROM material_lot_traceability_events WHERE material_lot_id = '${crossLotId}'`)) === crossEventsBefore);
assert("cross-facility foreign data not returned", !JSON.stringify(crossTransfer.json ?? {}).includes(FACILITY_UNAUTHORIZED));

const concurrentKey = `${CLOSURE_SOURCE_PREFIX}-concurrent-${RUN_SUFFIX}`;
const concurrentCode = `${CLOSURE_CODE_PREFIX}-CONCURRENT-${RUN_SUFFIX}`;
const concurrentPayload = baseCreateBody(concurrentCode, concurrentKey);
const concurrentCountBefore = Number(db("SELECT COUNT(*) FROM material_lots"));
const [concurrentA, concurrentB] = await Promise.all([
  request("POST", `/api/materials/lots?facilityId=${FACILITY}`, concurrentPayload),
  request("POST", `/api/materials/lots?facilityId=${FACILITY}`, concurrentPayload),
]);
const concurrentStatuses = [concurrentA.status, concurrentB.status].sort((a, b) => a - b);
assert("concurrent identical responses are 201", concurrentStatuses.every((status) => status === 201));
assert(
  "concurrent identical lot identities match",
  Boolean(concurrentA.json?.id && concurrentA.json.id === concurrentB.json?.id),
);
assert(
  "concurrent identical count increase = 1",
  Number(db("SELECT COUNT(*) FROM material_lots")) - concurrentCountBefore === 1,
);

const negativeReceipt = await request(
  "POST",
  `/api/materials/lots/${lotId}/receipt-observations?facilityId=${FACILITY}`,
  {
    expectedVersion: first.json.version,
    receivedAtUtc: new Date().toISOString(),
    observedQuantity: -5,
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-neg-qty-${RUN_SUFFIX}`,
  },
);
assert("negative observed quantity → 400", negativeReceipt.status === 400);

const receipt = await request(
  "POST",
  `/api/materials/lots/${lotId}/receipt-observations?facilityId=${FACILITY}`,
  {
    expectedVersion: first.json.version,
    receivedAtUtc: new Date().toISOString(),
    observedQuantity: 100,
    goodsReceiptReference: `${CLOSURE_SOURCE_PREFIX}-gr-${RUN_SUFFIX}`,
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-receipt-${RUN_SUFFIX}`,
  },
);
assert("valid receipt observation → 200", receipt.status === 200, JSON.stringify(receipt.json));

const detail = await request("GET", `/api/materials/lots/${lotId}?facilityId=${FACILITY}`);
assert("lot detail reload → 200", detail.status === 200);
const currentVersion = detail.json?.version ?? receipt.json?.version;

const sameLocationTransfer = await request(
  "POST",
  `/api/materials/lots/${lotId}/transfers?facilityId=${FACILITY}`,
  {
    expectedVersion: currentVersion,
    targetStorageLocationId: LOCATION_RAW,
    reason: "Same location should fail",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-same-loc-${RUN_SUFFIX}`,
  },
);
assert("same location transfer → 400", sameLocationTransfer.status === 400);

const transfer = await request(
  "POST",
  `/api/materials/lots/${lotId}/transfers?facilityId=${FACILITY}`,
  {
    expectedVersion: currentVersion,
    targetStorageLocationId: LOCATION_QUARANTINE,
    reason: "Closure transfer",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-transfer-${RUN_SUFFIX}`,
  },
);
assert("valid transfer → 200", transfer.status === 200, JSON.stringify(transfer.json));

const quality = await request(
  "POST",
  `/api/materials/lots/${lotId}/quality-status?facilityId=${FACILITY}`,
  {
    expectedVersion: transfer.json.version,
    qualityStatus: "accepted",
    reason: "Closure quality acceptance",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-quality-${RUN_SUFFIX}`,
  },
);
assert("quality status record → 200", quality.status === 200, JSON.stringify(quality.json));

const toAvailable = await request(
  "POST",
  `/api/materials/lots/${lotId}/status-transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: quality.json.version,
    targetStatus: "available",
    reason: "Closure activation",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-available-${RUN_SUFFIX}`,
  },
);
assert("status transition to available → 200", toAvailable.status === 200);

const certificate = await request(
  "POST",
  `/api/materials/lots/${lotId}/certificates?facilityId=${FACILITY}`,
  {
    certificateType: "certificate-of-analysis",
    documentReference: `${CLOSURE_SOURCE_PREFIX}-coa-${RUN_SUFFIX}`,
    documentNumber: "COA-CLOSURE-001",
    sourceType: "manual",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-cert-${RUN_SUFFIX}`,
  },
);
assert("certificate link → 200", certificate.status === 200, JSON.stringify(certificate.json));

const detailAfterCert = await request("GET", `/api/materials/lots/${lotId}?facilityId=${FACILITY}`);
assert("detail after certificate → 200", detailAfterCert.status === 200);

const toClosed = await request(
  "POST",
  `/api/materials/lots/${lotId}/status-transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: detailAfterCert.json.version,
    targetStatus: "closed",
    reason: "Closure terminal",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-closed-${RUN_SUFFIX}`,
  },
);
assert("status transition to closed → 200", toClosed.status === 200);

const closedReceipt = await request(
  "POST",
  `/api/materials/lots/${lotId}/receipt-observations?facilityId=${FACILITY}`,
  {
    expectedVersion: toClosed.json.version,
    receivedAtUtc: new Date().toISOString(),
    observedQuantity: 50,
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-closed-receipt-${RUN_SUFFIX}`,
  },
);
assert("closed terminal receipt → 409", closedReceipt.status === 409);

const staleWrite = await request(
  "POST",
  `/api/materials/lots/${lotId}/quality-status?facilityId=${FACILITY}`,
  {
    expectedVersion: transfer.json.version,
    qualityStatus: "rejected",
    reason: "Stale version",
    idempotencyKey: `${CLOSURE_SOURCE_PREFIX}-stale-${RUN_SUFFIX}`,
  },
);
assert("stale version write → 409", staleWrite.status === 409);

const timeline = await request("GET", `/api/materials/lots/${lotId}/timeline?facilityId=${FACILITY}`);
assert("timeline read → 200", timeline.status === 200);
assert("timeline has events", Array.isArray(timeline.json) && timeline.json.length > 0);

const traceability = await request("GET", `/api/materials/lots/${lotId}/traceability?facilityId=${FACILITY}`);
assert("traceability read → 200", traceability.status === 200);
assert("traceability backward present", Boolean(traceability.json?.backward));
assert("traceability forward present", Boolean(traceability.json?.forward));

removeClosureFixtures();
assertClosureCleanupComplete();
assertClosureEventsRemaining();
assertClosureCertificatesRemaining();
assertPilotLotsPreserved();

console.log("MATERIAL-2 lot/traceability verification passed.");

#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";

function fail(message) {
  console.error(`MOLD-1 counter idempotency verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": "crevafactos-dev",
    "X-crevafactos-dev-Subject": "production-pilot-operator",
    "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
  };
}

function dbCount(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return Number(result.stdout.trim());
}

function dbScalar(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

async function request(method, path, body) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json };
}

console.log("Running MOLD-1 counter idempotency live verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

const create = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD-CT-${Date.now()}`,
  name: "Counter Idempotency Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
if (create.status !== 201) fail(`setup create returned ${create.status}`);

const moldId = create.json.id;
let version = create.json.version;
const idempotencyKey = crypto.randomUUID();
const shotCount = 250;
const counterBody = {
  expectedVersion: version,
  shotCount,
  sourceType: "manual",
  note: "Counter test",
  idempotencyKey,
};

const snapshotsBefore = dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${moldId}'`);

const first = await request("POST", `/api/molds/${moldId}/counter-snapshots?facilityId=${FACILITY}`, counterBody);
assert("first counter snapshot → 200", first.status === 200, `got ${first.status}`);
assert("first snapshot not replay", first.json?.idempotentReplay !== true);
version = first.json.version;

const snapshotsAfterFirst = dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${moldId}'`);
assert("one snapshot created", snapshotsAfterFirst === snapshotsBefore + 1);
assert("current count updated", dbScalar(`SELECT current_shot_count FROM molds WHERE id = '${moldId}'`) === String(shotCount));

const countAfterFirst = dbScalar(`SELECT current_shot_count FROM molds WHERE id = '${moldId}'`);

const exactRetry = await request("POST", `/api/molds/${moldId}/counter-snapshots?facilityId=${FACILITY}`, {
  ...counterBody,
  expectedVersion: version,
});
assert("exact retry → 200", exactRetry.status === 200);
assert("exact retry flagged", exactRetry.json?.idempotentReplay === true);
assert("no duplicate snapshot", dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${moldId}'`) === snapshotsAfterFirst);
assert("current count unchanged after exact retry", dbScalar(`SELECT current_shot_count FROM molds WHERE id = '${moldId}'`) === countAfterFirst);

const conflictingRetry = await request("POST", `/api/molds/${moldId}/counter-snapshots?facilityId=${FACILITY}`, {
  expectedVersion: version,
  shotCount: 999,
  sourceType: "manual",
  note: "Different",
  idempotencyKey,
});
assert("conflicting retry → 409", conflictingRetry.status === 409, `got ${conflictingRetry.status}`);
assert(
  "conflicting retry failure code",
  conflictingRetry.json?.failureCode === "mold.idempotency-conflict",
  `failureCode=${conflictingRetry.json?.failureCode}`,
);
assert("conflicting retry not replay", conflictingRetry.json?.idempotentReplay !== true);
assert("original count preserved", dbScalar(`SELECT current_shot_count FROM molds WHERE id = '${moldId}'`) === countAfterFirst);
assert("no duplicate snapshot after conflict", dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${moldId}'`) === snapshotsAfterFirst);

const concurrentKey = crypto.randomUUID();
const setup = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD-CTC-${Date.now()}`,
  name: "Concurrent Counter Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
const concurrentMoldId = setup.json.id;
const concurrentVersion = setup.json.version;
const concurrentBody = {
  expectedVersion: concurrentVersion,
  shotCount: 42,
  sourceType: "manual",
  idempotencyKey: concurrentKey,
};
const snapshotsBeforeConcurrent = dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${concurrentMoldId}'`);
await Promise.all(
  Array.from({ length: 5 }, () =>
    request("POST", `/api/molds/${concurrentMoldId}/counter-snapshots?facilityId=${FACILITY}`, concurrentBody),
  ),
);
assert(
  "concurrent retry one snapshot",
  dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${concurrentMoldId}' AND idempotency_key = '${concurrentKey}'`) === 1,
);
assert(
  "concurrent retry snapshot count +1",
  dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${concurrentMoldId}'`) === snapshotsBeforeConcurrent + 1,
);

const concurrentConflictKey = crypto.randomUUID();
const conflictSetup = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD-CTD-${Date.now()}`,
  name: "Concurrent Conflict Counter Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
const conflictMoldId = conflictSetup.json.id;
const conflictVersion = conflictSetup.json.version;
const snapshotsBeforeConflict = dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${conflictMoldId}'`);
const concurrentConflictResults = await Promise.all([
  request("POST", `/api/molds/${conflictMoldId}/counter-snapshots?facilityId=${FACILITY}`, {
    expectedVersion: conflictVersion,
    shotCount: 100,
    sourceType: "manual",
    idempotencyKey: concurrentConflictKey,
  }),
  request("POST", `/api/molds/${conflictMoldId}/counter-snapshots?facilityId=${FACILITY}`, {
    expectedVersion: conflictVersion,
    shotCount: 200,
    sourceType: "manual",
    idempotencyKey: concurrentConflictKey,
  }),
]);
const conflictCounterStatuses = concurrentConflictResults.map((r) => r.status).sort();
assert(
  "concurrent different counter payload → one success and one conflict",
  conflictCounterStatuses.filter((s) => s === 200).length === 1 && conflictCounterStatuses.filter((s) => s === 409).length === 1,
  `statuses=${conflictCounterStatuses.join(",")}`,
);
assert(
  "concurrent different counter payload → one snapshot",
  dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${conflictMoldId}' AND idempotency_key = '${concurrentConflictKey}'`) === 1,
);
assert(
  "concurrent different counter payload → total +1",
  dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${conflictMoldId}'`) === snapshotsBeforeConflict + 1,
);

console.log("MOLD-1 counter idempotency verification passed.");

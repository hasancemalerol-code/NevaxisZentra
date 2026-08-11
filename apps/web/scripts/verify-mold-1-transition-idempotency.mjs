#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`MOLD-1 transition idempotency verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ACTOR.issuer,
    "X-CrevaFactOS-Dev-Subject": ACTOR.subject,
    "X-CrevaFactOS-Tenant": ACTOR.tenant,
  };
}

function dbCount(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return Number(result.stdout.trim());
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

console.log("Running MOLD-1 transition idempotency live verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

const create = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD-TR-${Date.now()}`,
  name: "Transition Idempotency Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
if (create.status !== 201) fail(`setup create returned ${create.status}`);

const moldId = create.json.id;
let version = create.json.version;
const idempotencyKey = crypto.randomUUID();
const transitionBody = {
  expectedVersion: version,
  targetState: "active",
  reason: "Approved for production",
  idempotencyKey,
};

const eventsBefore = dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${moldId}'`);

const first = await request("POST", `/api/molds/${moldId}/lifecycle-transitions?facilityId=${FACILITY}`, transitionBody);
assert("first transition → 200", first.status === 200, `got ${first.status}`);
assert("first transition not replay", first.json?.idempotentReplay !== true);
version = first.json.version;

const eventsAfterFirst = dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${moldId}'`);
assert("one lifecycle event added", eventsAfterFirst === eventsBefore + 1);

const detailAfterFirst = await request("GET", `/api/molds/${moldId}?facilityId=${FACILITY}`);
assert("state updated to active", detailAfterFirst.json?.lifecycleState === "active");

const exactRetry = await request("POST", `/api/molds/${moldId}/lifecycle-transitions?facilityId=${FACILITY}`, {
  ...transitionBody,
  expectedVersion: version,
});
assert("exact retry → 200", exactRetry.status === 200);
assert("exact retry flagged", exactRetry.json?.idempotentReplay === true);
assert("no duplicate event after exact retry", dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${moldId}'`) === eventsAfterFirst);

const conflictingRetry = await request("POST", `/api/molds/${moldId}/lifecycle-transitions?facilityId=${FACILITY}`, {
  expectedVersion: version,
  targetState: "maintenance",
  reason: "Different reason",
  idempotencyKey,
});
assert("conflicting retry → 409", conflictingRetry.status === 409, `got ${conflictingRetry.status}`);
assert(
  "conflicting retry failure code",
  conflictingRetry.json?.failureCode === "mold.idempotency-conflict",
  `failureCode=${conflictingRetry.json?.failureCode}`,
);
assert("conflicting retry not replay", conflictingRetry.json?.idempotentReplay !== true);
assert("state unchanged after conflicting retry", (await request("GET", `/api/molds/${moldId}?facilityId=${FACILITY}`)).json?.lifecycleState === "active");
assert("no duplicate event after conflicting retry", dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${moldId}'`) === eventsAfterFirst);

const concurrentKey = crypto.randomUUID();
const setup = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD-TRC-${Date.now()}`,
  name: "Concurrent Transition Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
const concurrentMoldId = setup.json.id;
const concurrentVersion = setup.json.version;
const concurrentBody = {
  expectedVersion: concurrentVersion,
  targetState: "active",
  reason: "Concurrent approval",
  idempotencyKey: concurrentKey,
};
const eventsBeforeConcurrent = dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${concurrentMoldId}'`);
const concurrentResults = await Promise.all(
  Array.from({ length: 5 }, () =>
    request("POST", `/api/molds/${concurrentMoldId}/lifecycle-transitions?facilityId=${FACILITY}`, concurrentBody),
  ),
);
const versions = new Set(concurrentResults.map((r) => r.json?.version).filter(Boolean));
assert("concurrent transition deterministic version", versions.size === 1);
assert(
  "concurrent transition one event",
  dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${concurrentMoldId}'`) === eventsBeforeConcurrent + 1,
);

const concurrentConflictKey = crypto.randomUUID();
const conflictSetup = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD-TRD-${Date.now()}`,
  name: "Concurrent Conflict Transition Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
const conflictMoldId = conflictSetup.json.id;
const conflictVersion = conflictSetup.json.version;
const eventsBeforeConflict = dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${conflictMoldId}'`);
const concurrentConflictResults = await Promise.all([
  request("POST", `/api/molds/${conflictMoldId}/lifecycle-transitions?facilityId=${FACILITY}`, {
    expectedVersion: conflictVersion,
    targetState: "active",
    reason: "Winner",
    idempotencyKey: concurrentConflictKey,
  }),
  request("POST", `/api/molds/${conflictMoldId}/lifecycle-transitions?facilityId=${FACILITY}`, {
    expectedVersion: conflictVersion,
    targetState: "maintenance",
    reason: "Loser",
    idempotencyKey: concurrentConflictKey,
  }),
]);
const conflictTransitionStatuses = concurrentConflictResults.map((r) => r.status).sort();
assert(
  "concurrent different transition payload → one success and one conflict",
  conflictTransitionStatuses.filter((s) => s === 200).length === 1 && conflictTransitionStatuses.filter((s) => s === 409).length === 1,
  `statuses=${conflictTransitionStatuses.join(",")}`,
);
assert(
  "concurrent different transition payload → one event",
  dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${conflictMoldId}'`) === eventsBeforeConflict + 1,
);

const scrappedRow = spawnSync(
  "docker",
  [
    "exec",
    "crevafactos-local-postgres-1",
    "psql",
    "-U",
    "crevafactos",
    "-d",
    "crevafactos_local",
    "-t",
    "-A",
    "-c",
    "SELECT id::text, version FROM molds WHERE lifecycle_state = 'scrapped' LIMIT 1",
  ],
  { encoding: "utf8" },
);
if (scrappedRow.status !== 0) fail(scrappedRow.stderr || scrappedRow.stdout);
const scrappedParts = scrappedRow.stdout.trim().split("|");
if (scrappedParts.length >= 2 && scrappedParts[0]) {
  const scrappedId = scrappedParts[0];
  const scrappedVersion = Number(scrappedParts[1]);
  const eventsBeforeScrapped = dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${scrappedId}'`);
  const scrappedAttempt = await request("POST", `/api/molds/${scrappedId}/lifecycle-transitions?facilityId=${FACILITY}`, {
    expectedVersion: scrappedVersion,
    targetState: "active",
    reason: "Should fail",
    idempotencyKey: crypto.randomUUID(),
  });
  assert("scrapped terminal rejection → 409", scrappedAttempt.status === 409, `got ${scrappedAttempt.status}`);
  assert(
    "scrapped terminal no new event",
    dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${scrappedId}'`) === eventsBeforeScrapped,
  );
} else {
  fail("no scrapped mold available for terminal rejection test");
}

console.log("MOLD-1 transition idempotency verification passed.");

#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT = "dev-pilot-tenant";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: TENANT,
};

function fail(message) {
  console.error(`MOLD-1 create idempotency verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
  };
}

function dbCount(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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

function baseBody(code, idempotencyKey) {
  return {
    code,
    name: "Idempotency Test Mold",
    sourceType: "manual",
    ownershipType: "company-owned",
    locationType: "warehouse",
    locationLabel: "Depo A",
    idempotencyKey,
  };
}

console.log("Running MOLD-1 create idempotency live verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

const moldCountBefore = dbCount("SELECT COUNT(*) FROM molds");
const idempotencyKey = crypto.randomUUID();
const code = `MOLD-IDEM-${Date.now()}`;
const payload = baseBody(code, idempotencyKey);

const first = await request("POST", `/api/molds?facilityId=${FACILITY}`, payload);
assert("first create → 201", first.status === 201, `got ${first.status}`);
assert("first create returns mold id", Boolean(first.json?.id));
assert("first create not replay", first.json?.idempotentReplay !== true);

const moldCountAfterFirst = dbCount("SELECT COUNT(*) FROM molds");
assert("one mold added", moldCountAfterFirst === moldCountBefore + 1);

const exactRetry = await request("POST", `/api/molds?facilityId=${FACILITY}`, payload);
assert("exact retry → 201", exactRetry.status === 201, `got ${exactRetry.status}`);
assert("exact retry same id", exactRetry.json?.id === first.json.id);
assert("exact retry flagged", exactRetry.json?.idempotentReplay === true);

const moldCountAfterRetry = dbCount("SELECT COUNT(*) FROM molds");
assert("no duplicate after exact retry", moldCountAfterRetry === moldCountAfterFirst);

const conflictingPayload = { ...payload, code: `${code}-X`, name: "Different Name" };
const conflictingRetry = await request("POST", `/api/molds?facilityId=${FACILITY}`, conflictingPayload);
assert("conflicting retry → 409", conflictingRetry.status === 409, `got ${conflictingRetry.status}`);
assert(
  "conflicting retry failure code",
  conflictingRetry.json?.failureCode === "mold.idempotency-conflict",
  `failureCode=${conflictingRetry.json?.failureCode}`,
);
assert("conflicting retry not replay", conflictingRetry.json?.idempotentReplay !== true);
assert("no duplicate after conflicting retry", dbCount("SELECT COUNT(*) FROM molds") === moldCountAfterFirst);

const concurrentKey = crypto.randomUUID();
const concurrentCode = `MOLD-CONC-${Date.now()}`;
const concurrentBody = baseBody(concurrentCode, concurrentKey);
const concurrentResults = await Promise.all(
  Array.from({ length: 5 }, () => request("POST", `/api/molds?facilityId=${FACILITY}`, concurrentBody)),
);
const concurrentIds = new Set(concurrentResults.map((r) => r.json?.id).filter(Boolean));
assert("concurrent duplicate → one mold id", concurrentIds.size === 1);
const concurrentCreated = dbCount(`SELECT COUNT(*) FROM molds WHERE create_idempotency_key = '${concurrentKey}'`);
assert("concurrent duplicate → one row", concurrentCreated === 1);

const concurrentConflictKey = crypto.randomUUID();
const concurrentConflictCodeA = `MOLD-CONC-A-${Date.now()}`;
const concurrentConflictCodeB = `MOLD-CONC-B-${Date.now()}`;
const concurrentConflictResults = await Promise.all([
  request("POST", `/api/molds?facilityId=${FACILITY}`, baseBody(concurrentConflictCodeA, concurrentConflictKey)),
  request("POST", `/api/molds?facilityId=${FACILITY}`, baseBody(concurrentConflictCodeB, concurrentConflictKey)),
]);
const conflictStatuses = concurrentConflictResults.map((r) => r.status).sort();
assert(
  "concurrent different payload → one success and one conflict",
  conflictStatuses.filter((s) => s === 201).length === 1 && conflictStatuses.filter((s) => s === 409).length === 1,
  `statuses=${conflictStatuses.join(",")}`,
);
assert(
  "concurrent different payload → one row",
  dbCount(`SELECT COUNT(*) FROM molds WHERE create_idempotency_key = '${concurrentConflictKey}'`) === 1,
);

console.log("MOLD-1 create idempotency verification passed.");

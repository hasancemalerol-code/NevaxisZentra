#!/usr/bin/env node
import { spawnSync } from "node:child_process";

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";

function fail(message) {
  console.error(`MOLD-1 optimistic concurrency verifier failed: ${message}`);
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
    "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
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

console.log("Running MOLD-1 optimistic concurrency live verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

const create = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD-OC-${Date.now()}`,
  name: "Optimistic Concurrency Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
if (create.status !== 201) fail(`setup create returned ${create.status}`);

const moldId = create.json.id;
const readN = await request("GET", `/api/molds/${moldId}?facilityId=${FACILITY}`);
const versionN = readN.json.version;
assert("read mold at version N", typeof versionN === "number" && versionN >= 1);

const eventsBefore = dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${moldId}'`);
const snapshotsBefore = dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${moldId}'`);

const updateBody = {
  expectedVersion: versionN,
  name: "Updated Via Concurrency Test",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  locationLabel: "Depo OC",
};

const successUpdate = await request("PUT", `/api/molds/${moldId}?facilityId=${FACILITY}`, updateBody);
assert("successful versioned update → 200", successUpdate.status === 200, `got ${successUpdate.status}`);
const versionAfter = successUpdate.json.version;
assert("version incremented", versionAfter === versionN + 1);

const staleUpdate = await request("PUT", `/api/molds/${moldId}?facilityId=${FACILITY}`, {
  ...updateBody,
  expectedVersion: versionN,
  name: "Stale Update Attempt",
});
assert("stale version update → 409", staleUpdate.status === 409, `got ${staleUpdate.status}`);

const afterStale = await request("GET", `/api/molds/${moldId}?facilityId=${FACILITY}`);
assert("newer name preserved", afterStale.json.name === "Updated Via Concurrency Test");
assert("newer version preserved", afterStale.json.version === versionAfter);
assert("lifecycle history unaffected", dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${moldId}'`) === eventsBefore);
assert("counter snapshots unaffected", dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${moldId}'`) === snapshotsBefore);

console.log("MOLD-1 optimistic concurrency verification passed.");

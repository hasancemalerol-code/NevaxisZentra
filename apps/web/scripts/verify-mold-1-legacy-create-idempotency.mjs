#!/usr/bin/env node
import { spawnSync } from "node:child_process";

const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";

function fail(message) {
  console.error(`MOLD-1 legacy create idempotency verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
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

function dbCount(sql) {
  return Number(db(sql));
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": "crevafactos-dev",
    "X-CrevaFactOS-Dev-Subject": "production-pilot-operator",
    "X-CrevaFactOS-Tenant": "dev-pilot-tenant",
  };
}

async function request(body) {
  const response = await fetch(`${BASE}/api/molds?facilityId=${FACILITY}`, {
    method: "POST",
    headers: headers(),
    body: JSON.stringify(body),
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

console.log("Running MOLD-1 legacy null-fingerprint create idempotency verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

let legacyRow = db(
  "SELECT id::text || '|' || code || '|' || name || '|' || COALESCE(version::text, '') FROM molds WHERE create_idempotency_key IS NOT NULL AND create_idempotency_payload_fingerprint IS NULL LIMIT 1",
);

if (!legacyRow) {
  const setupKey = crypto.randomUUID();
  const setupCode = `MOLD-LEGACY-${Date.now()}`;
  const setup = await request({
    code: setupCode,
    name: "Legacy Guard Setup",
    sourceType: "manual",
    ownershipType: "company-owned",
    locationType: "warehouse",
    idempotencyKey: setupKey,
  });
  if (setup.status !== 201) fail(`legacy setup create returned ${setup.status}`);
  db(`UPDATE molds SET create_idempotency_payload_fingerprint = NULL WHERE id = '${setup.json.id}'`);
  legacyRow = db(
    `SELECT id::text || '|' || code || '|' || name || '|' || COALESCE(version::text, '') FROM molds WHERE id = '${setup.json.id}'`,
  );
}

const [legacyId, legacyCode, legacyName, legacyVersion] = legacyRow.split("|");
assert("legacy record has idempotency key", Boolean(db(`SELECT create_idempotency_key FROM molds WHERE id = '${legacyId}'`)));
assert("legacy fingerprint is null", db(`SELECT create_idempotency_payload_fingerprint IS NULL FROM molds WHERE id = '${legacyId}'`) === "t");

const moldCountBefore = dbCount("SELECT COUNT(*) FROM molds");
const lifecycleBefore = dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${legacyId}'`);
const snapshotBefore = dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${legacyId}'`);

const retry = await request({
  code: legacyCode,
  name: legacyName,
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: db(`SELECT create_idempotency_key FROM molds WHERE id = '${legacyId}'`),
});

assert("legacy retry → 409", retry.status === 409, `got ${retry.status}`);
assert(
  "legacy retry failure code",
  retry.json?.failureCode === "mold.idempotency-legacy-unverifiable",
  `failureCode=${retry.json?.failureCode}`,
);
assert("legacy retry not replay", retry.json?.idempotentReplay !== true);
assert("Mold count unchanged", dbCount("SELECT COUNT(*) FROM molds") === moldCountBefore);
assert("legacy name unchanged", db(`SELECT name FROM molds WHERE id = '${legacyId}'`) === legacyName);
assert("legacy version unchanged", db(`SELECT version::text FROM molds WHERE id = '${legacyId}'`) === legacyVersion);
assert("legacy fingerprint still null", db(`SELECT create_idempotency_payload_fingerprint IS NULL FROM molds WHERE id = '${legacyId}'`) === "t");
assert("no lifecycle event added", dbCount(`SELECT COUNT(*) FROM mold_lifecycle_events WHERE mold_id = '${legacyId}'`) === lifecycleBefore);
assert("no counter snapshot added", dbCount(`SELECT COUNT(*) FROM mold_counter_snapshots WHERE mold_id = '${legacyId}'`) === snapshotBefore);

console.log("MOLD-1 legacy null-fingerprint create idempotency verification passed.");

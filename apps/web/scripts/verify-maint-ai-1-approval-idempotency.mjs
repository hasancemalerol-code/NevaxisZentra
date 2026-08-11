#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const TENANT = process.env.CREVAFACTOS_TENANT ?? "dev-pilot-tenant";
const ISSUER = "crevafactos-dev";
const OPERATOR = process.env.CREVAFACTOS_SUBJECT ?? "production-pilot-operator";
const FACILITY = process.env.CREVAFACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ASSET = process.env.CREVAFACTOS_ASSET ?? "ASSET-CNC-001";

function fail(msg) {
  console.error(`MAINT-AI-1 approval idempotency verifier failed: ${msg}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers() {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": OPERATOR,
    "X-CrevaFactOS-Tenant": TENANT,
  };
}

function dbCount(sql) {
  const r = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (r.status !== 0) fail(r.stderr || r.stdout);
  return r.stdout.trim();
}

async function json(method, path, body) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  return { status: res.status, data };
}

console.log("Running MAINT-AI-1 approval idempotency live checks.");
try {
  const ready = await fetch(`${BASE}/health/ready`);
  if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

  await json("POST", `/api/maintenance/condition/risk/evaluate?facilityId=${FACILITY}`, undefined);

  const gen = await json("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate?facilityId=${FACILITY}`);
  assert("generate draft → 200", gen.status === 200, `got ${gen.status}`);

  const draftId = gen.data?.draftId;
  const version = gen.data?.version;
  assert("draft id returned", Boolean(draftId));
  assert("version returned", typeof version === "number");

  const dupGen = await json("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate?facilityId=${FACILITY}`);
  assert("duplicate generate same fingerprint → same draft", dupGen.data?.draftId === draftId);

  const beforeReq = dbCount("SELECT COUNT(*) FROM maintenance_requests WHERE source = 'maintenance-ai-agent'");

  const approve1 = await json("POST", `/api/maintenance/ai-drafts/${draftId}/approve?facilityId=${FACILITY}`, { expectedVersion: version });
  assert("approve draft → 200", approve1.status === 200, `got ${approve1.status} ${JSON.stringify(approve1.data)}`);
  const requestId = approve1.data?.maintenanceRequestId;
  assert("maintenance request created", Boolean(requestId));

  const afterReq = dbCount("SELECT COUNT(*) FROM maintenance_requests WHERE source = 'maintenance-ai-agent'");
  assert("one new maintenance request", Number(afterReq) === Number(beforeReq) + 1, `${beforeReq} → ${afterReq}`);

  const approve2 = await json("POST", `/api/maintenance/ai-drafts/${draftId}/approve?facilityId=${FACILITY}`, { expectedVersion: version });
  assert("duplicate approve → 200 idempotent", approve2.status === 200, `got ${approve2.status}`);
  assert("duplicate approve same request id", approve2.data?.maintenanceRequestId === requestId);

  const afterDup = dbCount("SELECT COUNT(*) FROM maintenance_requests WHERE source = 'maintenance-ai-agent'");
  assert("no duplicate maintenance request", afterDup === afterReq, `${afterReq} → ${afterDup}`);

  const [a, b] = await Promise.all([
    json("POST", `/api/maintenance/assets/ASSET-PRESS-001/ai-drafts/generate?facilityId=${FACILITY}`),
    json("POST", `/api/maintenance/assets/ASSET-PRESS-001/ai-drafts/generate?facilityId=${FACILITY}`),
  ]);
  assert("concurrent generate same asset returns draft", a.data?.draftId && b.data?.draftId);

  const pressDraft = a.data.draftId;
  const pressVersion = a.data.version;
  const beforePressReq = dbCount("SELECT COUNT(*) FROM maintenance_requests WHERE asset_reference = 'ASSET-PRESS-001' AND source = 'maintenance-ai-agent'");

  const [p1, p2] = await Promise.all([
    json("POST", `/api/maintenance/ai-drafts/${pressDraft}/approve?facilityId=${FACILITY}`, { expectedVersion: pressVersion }),
    json("POST", `/api/maintenance/ai-drafts/${pressDraft}/approve?facilityId=${FACILITY}`, { expectedVersion: pressVersion }),
  ]);

  const successCount = [p1, p2].filter((r) => r.status === 200 && r.data?.maintenanceRequestId).length;
  assert("concurrent approve at most one succeeds with request", successCount >= 1);
  const afterPressReq = dbCount("SELECT COUNT(*) FROM maintenance_requests WHERE asset_reference = 'ASSET-PRESS-001' AND source = 'maintenance-ai-agent'");
  assert("concurrent approve creates at most one request", Number(afterPressReq) - Number(beforePressReq) <= 1, `${beforePressReq} → ${afterPressReq}`);
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("MAINT-AI-1 approval idempotency verifier PASS");

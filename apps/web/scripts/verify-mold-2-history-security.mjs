#!/usr/bin/env node
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const OTHER_FACILITY = "bb000001-0006-4000-8000-000000000006";
const TENANT = "dev-pilot-tenant";
const MANAGE_ACTOR = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: TENANT };
const READ_ACTOR = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: TENANT };

function fail(message) {
  console.error(`MOLD-2 history security verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = MANAGE_ACTOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = text; }
  return { status: response.status, json };
}

console.log("Running MOLD-2 history security/mutation-boundary verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (ready.status !== 200) fail(`/health/ready returned ${ready.status}`);

const create = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: `MOLD2-SEC-${Date.now()}`,
  name: "Security Test Mold",
  sourceType: "manual",
  ownershipType: "company-owned",
  locationType: "warehouse",
  idempotencyKey: crypto.randomUUID(),
});
if (create.status !== 201) fail(`create mold ${create.status}`);
const moldId = create.json.id;

const readOnlyWrite = await request(
  "POST",
  `/api/molds/${moldId}/maintenance-history?facilityId=${FACILITY}`,
  {
    maintenanceType: "inspection",
    status: "planned",
    openedAtUtc: new Date().toISOString(),
    summary: "Read-only denial test",
    sourceType: "manual",
    idempotencyKey: crypto.randomUUID(),
  },
  READ_ACTOR,
);
assert("read-only write denied", readOnlyWrite.status === 403, String(readOnlyWrite.status));

const noAuth = await fetch(`${BASE}/api/molds/${moldId}/timeline?facilityId=${FACILITY}`);
assert("missing auth denied", noAuth.status === 401, String(noAuth.status));

const wrongFacility = await request(
  "GET",
  `/api/molds/${moldId}/timeline?facilityId=${OTHER_FACILITY}`,
);
assert("unauthorized facility denied", wrongFacility.status === 403, String(wrongFacility.status));

const readOk = await request("GET", `/api/molds/${moldId}/timeline?facilityId=${FACILITY}`, null, READ_ACTOR);
assert("read-only GET allowed", readOk.status === 200, String(readOk.status));

console.log("MOLD-2 history security/mutation-boundary verifier passed.");

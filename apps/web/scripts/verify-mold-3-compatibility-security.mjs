#!/usr/bin/env node
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const OTHER_FACILITY = "bb000001-0006-4000-8000-000000000006";
const PRESS_MACHINE = "bb000001-0096-4000-8000-000000000096";
const TENANT = "dev-pilot-tenant";
const MANAGE_ACTOR = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: TENANT };
const READ_ACTOR = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: TENANT };

function fail(message) {
  console.error(`MOLD-3 security verifier failed: ${message}`);
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

async function request(method, path, body, actor) {
  const response = await fetch(`${API}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  return response.status;
}

console.log("Running MOLD-3 security/mutation-boundary verifier.");

const molds = await request("GET", `/api/molds?facilityId=${FACILITY}`, undefined, READ_ACTOR);
assert("read-only GET molds", molds === 200, String(molds));

const writeReq = await request(
  "PUT",
  `/api/molds/${"00000000-0000-4000-8000-000000000001"}/compatibility-requirement?facilityId=${FACILITY}`,
  { sourceType: "manual" },
  READ_ACTOR,
);
assert("read-only write requirement", writeReq === 403, String(writeReq));

const evalDenied = await request(
  "POST",
  `/api/molds/${"00000000-0000-4000-8000-000000000001"}/compatibility-evaluations/${PRESS_MACHINE}?facilityId=${FACILITY}`,
  {},
  READ_ACTOR,
);
assert("read-only evaluation", evalDenied === 403, String(evalDenied));

const noAuth = await fetch(`${API}/api/molds?facilityId=${FACILITY}`);
assert("missing auth", noAuth.status === 401, String(noAuth.status));

const wrongFacility = await request(
  "POST",
  `/api/molds/${"00000000-0000-4000-8000-000000000001"}/compatibility-evaluations/${PRESS_MACHINE}?facilityId=${OTHER_FACILITY}`,
  {},
  MANAGE_ACTOR,
);
assert("unauthorized facility evaluation", wrongFacility === 403, String(wrongFacility));

console.log("MOLD-3 security/mutation-boundary verifier passed.");

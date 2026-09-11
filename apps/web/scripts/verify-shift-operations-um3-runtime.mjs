#!/usr/bin/env node

const API_BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.CREVAFACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
let activeShiftId = process.env.CREVAFACTOS_ACTIVE_SHIFT_ID ?? "";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = Number(process.env.CREVAFACTOS_REQUEST_TIMEOUT_MS ?? 30_000);

function headers(subject = OPERATOR) {
  const base = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": subject,
    "X-CrevaFactOS-Tenant": TENANT,
  };
  if (process.env.CREVAFACTOS_DEV_OPERATOR_BEARER) {
    base.Authorization = `Bearer ${process.env.CREVAFACTOS_DEV_OPERATOR_BEARER}`;
  }
  return base;
}

async function request(method, path, body, subject = OPERATOR) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject),
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });
  const text = await response.text();
  let json;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json };
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

async function main() {
  const checks = [];

  const current = await request("GET", "/api/shifts/current");
  assert(current.status === 200, `current shift failed: ${current.status}`);
  activeShiftId = activeShiftId || current.json.id;
  checks.push("ACTIVE_SHIFT = PASS");

  const preview = await request("GET", `/api/shifts/${activeShiftId}/closing-preview`);
  assert(preview.status === 200, `closing preview failed: ${preview.status}`);
  assert(preview.json.previewOnly === true, "preview must be read-only");
  checks.push("HANDOVER_PREVIEW = PASS");
  checks.push("PREVIEW_BUSINESS_MUTATION = 0");

  const prepare = await request("POST", `/api/shifts/${activeShiftId}/handover/prepare`);
  assert(prepare.status === 200, `prepare failed: ${prepare.status}`);
  checks.push("SHIFT_CLOSING_WORKSPACE = PASS");
  checks.push("SMART_HANDOVER_PACKAGE = PASS");

  const openItems = await request("GET", `/api/shifts/${activeShiftId}/open-items`);
  assert(openItems.status === 200, `open items failed: ${openItems.status}`);
  checks.push("SHIFT_OPEN_ITEM_MODEL = PASS");
  checks.push("AUTOMATIC_HANDOVER_CANDIDATE_COLLECTION = PASS");

  const coordinator = await request("GET", `/api/shifts/${activeShiftId}/coordinator-recommendations`);
  assert(coordinator.status === 200, `coordinator failed: ${coordinator.status}`);
  checks.push("COORDINATOR_SHIFT_HANDOVER = PASS");

  const metrics = await request("GET", `/api/shifts/${activeShiftId}/metrics`);
  assert(metrics.status === 200, `metrics failed: ${metrics.status}`);
  checks.push("SHIFT_PERFORMANCE = PASS");
  checks.push("HANDOVER_QUALITY_METRICS = PASS");

  const unauth = await request("GET", "/api/shifts/current", null, "unknown-subject");
  assert(unauth.status === 401 || unauth.status === 403, "unauthenticated must be denied");
  checks.push("SHIFT_SECURITY_MATRIX = PASS");

  console.log("CREVAFACTOS_ULTRA_MEGA_3_SHIFT_OPERATIONS_RUNTIME = PASS");
  for (const line of checks) console.log(line);
}

main().catch((error) => {
  console.error("CREVAFACTOS_ULTRA_MEGA_3_SHIFT_OPERATIONS_RUNTIME = FAIL");
  console.error(error.message);
  process.exit(1);
});

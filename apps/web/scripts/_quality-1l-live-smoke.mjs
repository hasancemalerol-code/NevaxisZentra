#!/usr/bin/env node
/**
 * QUALITY-1L mandatory live smoke — narrow proof script.
 */
const BASE = process.argv[2] ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const WRONG_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PILOT_PLAN_ID = "bb000001-0060-4000-8000-000000000060";
const PILOT_DEF_ID = "bb000001-0061-4000-8000-000000000061";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";

let failures = 0;
function pass(k, d = "") { console.log(`✅ ${k}${d ? `: ${d}` : ""}`); }
function fail(k, d = "") { failures++; console.error(`❌ ${k}${d ? `: ${d}` : ""}`); }

const h = (subject = OPERATOR, extra = {}) => ({
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-crevafactos-dev-Issuer": ISSUER,
  "X-crevafactos-dev-Subject": subject,
  "X-NevaxisFactOS-Tenant": TENANT,
  ...extra,
});
const q = (path, facility = FACILITY) =>
  `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facility}`;

async function api(method, path, body, headers = h()) {
  const resp = await fetch(path.startsWith("http") ? path : q(path), {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await resp.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: resp.status, json, text };
}

async function main() {
  console.log(`\nQUALITY-1L live smoke → ${BASE}\n`);

  let r = await api("GET", "/api/quality/control-plans");
  if (r.status === 200) pass("plan-list", `${(r.json ?? []).length} plans`);
  else fail("plan-list", String(r.status));

  r = await api("GET", `/api/quality/control-plans/${PILOT_PLAN_ID}`);
  if (r.status === 200 && r.json?.code === "QC-PLAN-001") pass("plan-read", r.json.status);
  else fail("plan-read", `${r.status} ${r.text?.slice(0, 120)}`);

  r = await api("GET", "/api/production/orders?limit=5");
  const orderId = r.json?.[0]?.id;
  if (r.status === 200 && orderId) pass("production-order-list", orderId);
  else fail("production-order-list", String(r.status));

  r = await api("POST", "/api/quality/inspections", {
    productionOrderId: orderId,
    qualityControlPlanId: PILOT_PLAN_ID,
    notes: "QUALITY-1L smoke",
  });
  const inspectionId = r.json?.id;
  let version = r.json?.version ?? 1;
  if ((r.status === 200 || r.status === 201) && inspectionId) pass("inspection-create", inspectionId);
  else fail("inspection-create", `${r.status} ${r.text?.slice(0, 120)}`);

  if (!inspectionId) {
    console.error("\nAborting remaining smoke — no inspection id.");
    process.exit(1);
  }

  r = await api("POST", `/api/quality/inspections/${inspectionId}/start`, { expectedVersion: version });
  if (r.status === 200) {
    version = r.json.version ?? version + 1;
    pass("inspection-start", `v${version}`);
  } else fail("inspection-start", String(r.status));

  r = await api("POST", `/api/quality/inspections/${inspectionId}/results`, {
    expectedVersion: version,
    inspectionDefinitionId: PILOT_DEF_ID,
    measuredNumericValue: 55.0,
  });
  if (r.status === 200) {
    version = r.json.version ?? version + 1;
    pass("in-tolerance-result", "pass expected");
  } else fail("in-tolerance-result", `${r.status} ${r.text?.slice(0, 160)}`);

  r = await api("POST", `/api/quality/inspections/${inspectionId}/results`, {
    expectedVersion: version,
    inspectionDefinitionId: PILOT_DEF_ID,
    measuredNumericValue: 56.0,
  });
  if (r.status === 200) {
    version = r.json.version ?? version + 1;
    pass("out-tolerance-result", "fail expected");
  } else fail("out-tolerance-result", String(r.status));

  r = await api("GET", `/api/quality/inspections/${inspectionId}`);
  if (r.status === 200 && r.json?.overallResult === "fail") pass("overall-result", r.json.overallResult);
  else fail("overall-result", `${r.status} ${r.json?.overallResult}`);

  r = await api("POST", `/api/quality/inspections/${inspectionId}/complete`, { expectedVersion: version });
  if (r.status === 200) {
    version = r.json.version ?? version + 1;
    pass("inspection-complete", `v${version}`);
  } else fail("inspection-complete", String(r.status));

  r = await api("POST", `/api/quality/inspections/${inspectionId}/release`, {
    expectedVersion: version,
    note: "operator should fail",
  }, h(OPERATOR));
  if (r.status === 403) pass("release-permission-403", "operator denied");
  else fail("release-permission-403", String(r.status));

  r = await api("POST", `/api/quality/inspections/${inspectionId}/reject`, {
    expectedVersion: version,
    note: "reviewer reject smoke",
  }, h(REVIEWER));
  if (r.status === 200) {
    version = r.json.version ?? version + 1;
    pass("authorized-reject", `v${version}`);
  } else fail("authorized-reject", String(r.status));

  r = await api("POST", `/api/quality/inspections/${inspectionId}/reject`, {
    expectedVersion: version,
    note: "stale",
  }, h(REVIEWER));
  if (r.status === 409) pass("stale-version-409", "ok");
  else fail("stale-version-409", String(r.status));

  r = await fetch(q("/api/quality/control-plans"), {
    method: "GET",
    headers: {
      Accept: "application/json",
      "X-crevafactos-dev-Subject": "missing-auth-subject",
    },
  });
  if (r.status === 401) pass("401-unauthenticated", "ok");
  else fail("401-unauthenticated", String(r.status));

  r = await api("GET", q("/api/quality/control-plans", WRONG_FACILITY));
  if (r.status === 403) pass("wrong-facility-403", "ok");
  else fail("wrong-facility-403", String(r.status));

  r = await api("GET", `/api/quality/inspections/${inspectionId}`);
  if (r.status === 200 && r.json?.releaseStatus === "rejected") pass("readback", r.json.releaseStatus);
  else fail("readback", `${r.status} ${r.json?.releaseStatus}`);

  console.log(failures === 0 ? "\nQUALITY-1L live smoke PASSED\n" : `\nQUALITY-1L live smoke FAILED (${failures})\n`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

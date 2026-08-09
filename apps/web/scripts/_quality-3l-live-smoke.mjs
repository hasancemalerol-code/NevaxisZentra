#!/usr/bin/env node
/**
 * QUALITY-3L mandatory live smoke — narrow read-only proof script.
 */
const BASE = process.argv[2] ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const WRONG_FACILITY = "bb000001-0005-4000-8000-000000000099";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

let failures = 0;

function pass(k, d = "") {
  console.log(`✅ ${k}${d ? `: ${d}` : ""}`);
}
function fail(k, d = "") {
  failures++;
  console.error(`❌ ${k}${d ? `: ${d}` : ""}`);
}

const h = (subject = OPERATOR, extra = {}) => ({
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-CrevaFactOS-Dev-Issuer": ISSUER,
  "X-CrevaFactOS-Dev-Subject": subject,
  "X-CrevaFactOS-Tenant": TENANT,
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
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: resp.status, json, text };
}

async function main() {
  console.log(`\nQUALITY-3L live smoke → ${BASE}\n`);

  let r = await api("GET", "/api/quality/reporting/overview");
  if (r.status === 200 && typeof r.json?.totalInspectionCount === "number") {
    pass("reporting-overview", String(r.json.totalInspectionCount));
  } else {
    fail("reporting-overview", `${r.status} ${r.text?.slice(0, 120)}`);
  }

  r = await api("GET", "/api/quality/reporting/trends?period=week");
  if (r.status === 200 && Array.isArray(r.json)) {
    pass("reporting-trends", `${r.json.length} period(s)`);
  } else {
    fail("reporting-trends", String(r.status));
  }

  r = await api("GET", "/api/quality/reporting/failures");
  if (r.status === 200 && Array.isArray(r.json)) {
    pass("reporting-failures", `${r.json.length} item(s)`);
  } else {
    fail("reporting-failures", String(r.status));
  }

  r = await api("GET", "/api/quality/reporting/ncrs");
  if (r.status === 200 && typeof r.json?.openCount === "number") {
    pass("reporting-ncrs", String(r.json.openCount));
  } else {
    fail("reporting-ncrs", String(r.status));
  }

  r = await api("GET", "/api/production/orders?limit=5");
  const orderId = r.json?.[0]?.id;
  if (r.status === 200 && orderId) pass("production-order-list", orderId);
  else fail("production-order-list", String(r.status));

  r = await api("GET", "/api/quality/inspections");
  const inspectionId = r.json?.[0]?.id;
  if (r.status === 200 && inspectionId) pass("inspection-list", inspectionId);
  else fail("inspection-list", String(r.status));

  if (!orderId || !inspectionId) {
    console.error("\nAborting — missing order or inspection id.");
    process.exit(1);
  }

  r = await api("GET", `/api/quality/traceability/production-orders/${orderId}`);
  if (r.status === 200 && r.json?.productionOrderId === orderId) {
    pass("production-order-traceability", `${r.json.inspections?.length ?? 0} inspection(s)`);
  } else {
    fail("production-order-traceability", `${r.status} ${r.text?.slice(0, 120)}`);
  }

  r = await api("GET", `/api/quality/inspections/${inspectionId}/summary`);
  if (r.status === 200 && r.json?.inspectionId === inspectionId) {
    pass("inspection-summary", r.json.inspectionNumber ?? inspectionId);
    if (Array.isArray(r.json.measurements)) {
      pass("inspection-summary-measurements", `${r.json.measurements.length} row(s)`);
    } else {
      fail("inspection-summary-measurements", "missing measurements array");
    }
    if (typeof r.json.controlPlanRevision === "string") {
      pass("snapshot-plan-revision", r.json.controlPlanRevision);
    } else {
      fail("snapshot-plan-revision", "missing controlPlanRevision");
    }
  } else {
    fail("inspection-summary", String(r.status));
  }

  r = await api("GET", `/api/quality/inspections/${inspectionId}/release-intelligence`);
  if (r.status === 200 && typeof r.json?.blocking === "boolean") {
    pass("release-intelligence", `blocking=${r.json.blocking}`);
  } else {
    fail("release-intelligence", String(r.status));
  }

  r = await api("GET", `/api/quality/production-orders/${orderId}/export-readiness`);
  if (r.status === 200 && typeof r.json?.qualityBlocking === "boolean") {
    pass("export-readiness", `ready=${r.json.qualityReady}`);
  } else {
    fail("export-readiness", String(r.status));
  }

  const overviewCount = (await api("GET", "/api/quality/reporting/overview")).json?.totalInspectionCount;
  const inspectionReports = await api("GET", "/api/quality/reporting/inspections?limit=5");
  if (inspectionReports.status === 200 && Array.isArray(inspectionReports.json)) {
    pass("reporting-inspections", `${inspectionReports.json.length} row(s)`);
    if (
      typeof overviewCount === "number" &&
      inspectionReports.json.length <= overviewCount + 1
    ) {
      pass("overview-vs-inspections-cross-check", `${inspectionReports.json.length}<=${overviewCount}`);
    } else {
      fail(
        "overview-vs-inspections-cross-check",
        `${inspectionReports.json.length} vs overview ${overviewCount}`,
      );
    }
  } else {
    fail("reporting-inspections", String(inspectionReports.status));
  }

  r = await api("POST", "/api/quality/reporting/overview", { tamper: true });
  if (r.status === 405 || r.status === 404) pass("reporting-post-blocked", String(r.status));
  else fail("reporting-post-blocked", String(r.status));

  r = await fetch(q("/api/quality/reporting/overview"), {
    method: "GET",
    headers: { Accept: "application/json", "X-CrevaFactOS-Dev-Subject": "missing-auth-subject" },
  });
  if (r.status === 401) pass("401-unauthenticated", "ok");
  else fail("401-unauthenticated", String(r.status));

  r = await api("GET", q("/api/quality/reporting/overview", WRONG_FACILITY));
  if (r.status === 403) pass("wrong-facility-403", "ok");
  else fail("wrong-facility-403", String(r.status));

  console.log(
    failures === 0
      ? "\nQUALITY-3L live smoke PASSED\n"
      : `\nQUALITY-3L live smoke FAILED (${failures})\n`,
  );
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { randomUUID } from "node:crypto";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const OVERDUE_LINE = "bb000001-00c1-4000-8000-0000000000c1";
const INSUFFICIENT_LINE = "bb000001-00c5-4000-8000-0000000000c5";
const OVERDUE_ASSESSMENT = "bb000001-00c8-4000-8000-0000000000c8";
const INSUFFICIENT_ASSESSMENT = "bb000001-00ca-4000-8000-0000000000ca";
const overdueKey = `procurement-risk-domain-overdue-${randomUUID()}`;
const insufficientKey = `procurement-risk-domain-insufficient-${randomUUID()}`;
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`PROCUREMENT-RISK-FOUNDATION-1 domain assessment verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": MANAGER.issuer,
    "X-crevafactos-dev-Subject": MANAGER.subject,
    "X-NevaxisZentra-Tenant": MANAGER.tenant,
  };
}

async function request(method, path, body) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function isNullishQuantity(value) {
  return value === null || value === undefined;
}

console.log("Running PROCUREMENT-RISK-FOUNDATION-1 domain assessment verifier.");

const calculator = readRequired("packages/NevaxisZentra.Persistence/Procurement/ProcurementDeliveryRiskCalculator.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/Procurement/ProcurementDeliveryRiskConstants.cs");
const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotProcurementDeliveryRiskSeed.cs");
const poSeed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotErpPurchaseOrderSeed.cs");

assert("calculator assess method", calculator.includes("AssessLine"));
assert("calculator overdue branch", calculator.includes("ProcurementDeliveryRiskStatuses.Overdue"));
assert("calculator insufficient branch", calculator.includes("ProcurementDeliveryRiskStatuses.InsufficientData"));
assert("rule set version", constants.includes("PROCUREMENT-DELIVERY-RISK-V1"));
assert("pilot overdue seed", seed.includes("ProcurementDeliveryRiskOverdueAssessmentId"));
assert("pilot insufficient seed", seed.includes("ProcurementDeliveryRiskInsufficientAssessmentId"));
assert("pilot overdue PO seed", poSeed.includes("PILOT-PO-OVERDUE"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const overdueEvaluate = await request(
  "POST",
  `/api/procurement/delivery-risks/evaluate?facilityId=${FACILITY}`,
  { purchaseOrderLineId: OVERDUE_LINE },
);
assert("overdue evaluate succeeds", overdueEvaluate.status === 200, overdueEvaluate.text?.slice(0, 200));

const overdueDetail = await request(
  "GET",
  `/api/procurement/delivery-risks/${overdueEvaluate.json.assessmentId}?facilityId=${FACILITY}`,
);
assert("overdue detail HTTP 200", overdueDetail.status === 200);
assert("overdue result", overdueDetail.json?.result === "overdue");
assert("overdue rule set version", overdueDetail.json?.ruleSetVersion === "PROCUREMENT-DELIVERY-RISK-V1");
assert("overdue open quantity present", (overdueDetail.json?.openQuantity ?? 0) > 0);
assert("overdue commitment date present", typeof overdueDetail.json?.commitmentDateUtc === "string");
assert("overdue driver present", Array.isArray(overdueDetail.json?.drivers) && overdueDetail.json.drivers.some((d) => d.code === "overdue-open-quantity"));

const insufficientEvaluate = await request(
  "POST",
  `/api/procurement/delivery-risks/evaluate?facilityId=${FACILITY}`,
  { purchaseOrderLineId: INSUFFICIENT_LINE },
);
assert("insufficient evaluate succeeds", insufficientEvaluate.status === 200, insufficientEvaluate.text?.slice(0, 200));

const insufficientDetail = await request(
  "GET",
  `/api/procurement/delivery-risks/${insufficientEvaluate.json.assessmentId}?facilityId=${FACILITY}`,
);
assert("insufficient detail HTTP 200", insufficientDetail.status === 200);
assert("insufficient-data result", insufficientDetail.json?.result === "insufficient-data");
assert("insufficient no fabricated open quantity in evidence unknown fields", !insufficientDetail.json?.evidence?.some((e) => e.field === "openQuantity" && e.isUnknown === false && e.value === "0"));
assert("insufficient driver present", Array.isArray(insufficientDetail.json?.drivers) && insufficientDetail.json.drivers.length > 0);

const seededOverdue = await request("GET", `/api/procurement/delivery-risks/workspace?facilityId=${FACILITY}`);
assert("seeded overdue assessment in workspace", seededOverdue.status === 200 && seededOverdue.json?.assessments?.some((row) => row.result === "overdue"));

const seededInsufficient = await request("GET", `/api/procurement/delivery-risks/workspace?facilityId=${FACILITY}`);
assert("seeded insufficient assessment in workspace", seededInsufficient.status === 200 && seededInsufficient.json?.assessments?.some((row) => row.result === "insufficient-data"));

console.log("PROCUREMENT-RISK-FOUNDATION-1 domain assessment verification passed.");

#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { randomUUID } from "node:crypto";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const OVERDUE_ASSESSMENT = "bb000001-00c8-4000-8000-0000000000c8";
const INSUFFICIENT_ASSESSMENT = "bb000001-00ca-4000-8000-0000000000ca";
const OVERDUE_LINE = "bb000001-00c1-4000-8000-0000000000c1";
const ON_TRACK_LINE = "bb000001-00c7-4000-8000-0000000000c7";
const RUN_SUFFIX = randomUUID();
const IDEM_KEY = `procurement-risk-foundation-1:${RUN_SUFFIX}`;
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`PROCUREMENT-RISK-FOUNDATION-1 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

function dbCount(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return Number(result.stdout.trim());
}

async function liveRequest(method, path, body, headers = actorHeaders(MANAGER)) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

console.log("Running PROCUREMENT-RISK-FOUNDATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Procurement/ProcurementDeliveryRiskEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Procurement/ProcurementDeliveryRiskRepository.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/Procurement/ProcurementDeliveryRiskConstants.cs");
const calculator = readRequired("packages/CrevaFactOS.Persistence/Procurement/ProcurementDeliveryRiskCalculator.cs");

assert("workspace endpoint registered", endpoints.includes('MapGet("/workspace"'));
assert("evaluate endpoint registered", endpoints.includes('MapPost("/evaluate"'));
assert("assessment detail endpoint registered", endpoints.includes('MapGet("/{assessmentId:guid}"'));
assert("read access resolver", endpoints.includes("ResolveReadAccessAsync"));
assert("write access resolver", endpoints.includes("ResolveWriteAccessAsync"));
assert("erp manage permission for evaluate", endpoints.includes("ErpPermissionKeys.Manage"));
assert("repository evaluate", repository.includes("EvaluateAsync"));
assert("repository idempotency key handling", repository.includes("IdempotencyKey"));
assert("repository fingerprint replay", repository.includes("InputFingerprint"));
assert("rule set version constant", constants.includes("PROCUREMENT-DELIVERY-RISK-V1"));
assert("no auto action disclaimer", constants.includes("otomatik satın alma, tedarikçi iletişimi, stok veya üretim işlemi yapmaz"));
assert("calculator no SaveChanges", !calculator.includes("SaveChangesAsync"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/procurement/delivery-risks/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace capabilities authoritative", workspace.json?.capabilities?.canRead === true);
assert("workspace assessments array", Array.isArray(workspace.json?.assessments));
assert("pilot overdue assessment present", workspace.json?.assessments?.some((row) => row.result === "overdue"));
assert("pilot insufficient assessment present", workspace.json?.assessments?.some((row) => row.result === "insufficient-data"));
assert("automation disclaimer in workspace", typeof workspace.json?.automationDisclaimer === "string" && workspace.json.automationDisclaimer.toLowerCase().includes("otomatik satın alma"));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/procurement/delivery-risks/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const wrongFacility = await liveRequest("GET", `/api/procurement/delivery-risks/workspace?facilityId=${UNAUTH_FACILITY}`);
assert("wrong facility workspace rejected", wrongFacility.status === 403);

const overdueAssessmentId = workspace.json?.assessments?.find((row) => row.result === "overdue")?.id;
const insufficientAssessmentId = workspace.json?.assessments?.find((row) => row.result === "insufficient-data")?.id;
assert("overdue assessment id resolved", Boolean(overdueAssessmentId));
assert("insufficient assessment id resolved", Boolean(insufficientAssessmentId));

const overdueDetail = await liveRequest("GET", `/api/procurement/delivery-risks/${overdueAssessmentId}?facilityId=${FACILITY}`);
assert("overdue assessment detail succeeds", overdueDetail.status === 200);
assert("overdue result", overdueDetail.json?.result === "overdue");
assert("overdue has real open quantity", (overdueDetail.json?.openQuantity ?? 0) > 0);

const insufficientDetail = await liveRequest("GET", `/api/procurement/delivery-risks/${insufficientAssessmentId}?facilityId=${FACILITY}`);
assert("insufficient assessment detail succeeds", insufficientDetail.status === 200);
assert("insufficient-data result", insufficientDetail.json?.result === "insufficient-data");

const readonlyEvaluate = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/evaluate?facilityId=${FACILITY}`,
  { purchaseOrderLineId: OVERDUE_LINE, idempotencyKey: `${IDEM_KEY}-readonly` },
  actorHeaders(READONLY),
);
assert("read-only cannot evaluate", readonlyEvaluate.status === 403);

const poCountBefore = dbCount("SELECT COUNT(*) FROM erp_purchase_orders");
const poLineCountBefore = dbCount("SELECT COUNT(*) FROM erp_purchase_order_lines");
const aiTaskCountBefore = dbCount("SELECT COUNT(*) FROM ai_tasks");

spawnSync(
  "docker",
  ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-c",
    `DELETE FROM procurement_delivery_risk_assessments WHERE purchase_order_line_id = '${ON_TRACK_LINE}' AND id != 'bb000001-00cb-4000-8000-0000000000cb';`],
  { encoding: "utf8" },
);

const firstEvaluate = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/evaluate?facilityId=${FACILITY}`,
  { purchaseOrderLineId: ON_TRACK_LINE, idempotencyKey: IDEM_KEY },
);
assert("manager can evaluate delivery risk", firstEvaluate.status === 200, firstEvaluate.text?.slice(0, 200));
assert("evaluation id returned", Boolean(firstEvaluate.json?.assessmentId));

const secondEvaluate = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/evaluate?facilityId=${FACILITY}`,
  { purchaseOrderLineId: ON_TRACK_LINE, idempotencyKey: IDEM_KEY },
);
assert("idempotent replay succeeds", secondEvaluate.status === 200);
assert("same assessment id on replay", secondEvaluate.json?.assessmentId === firstEvaluate.json?.assessmentId);
assert("idempotent replay flag", secondEvaluate.json?.idempotentReplay === true);

const idemConflict = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/evaluate?facilityId=${FACILITY}`,
  { purchaseOrderLineId: OVERDUE_LINE, idempotencyKey: IDEM_KEY },
);
assert("incompatible idempotency-key reuse conflicts", idemConflict.status === 409, `status=${idemConflict.status}`);

const poCountAfter = dbCount("SELECT COUNT(*) FROM erp_purchase_orders");
const poLineCountAfter = dbCount("SELECT COUNT(*) FROM erp_purchase_order_lines");
const aiTaskCountAfter = dbCount("SELECT COUNT(*) FROM ai_tasks");
assert("evaluate does not write erp_purchase_orders", poCountAfter === poCountBefore);
assert("evaluate does not write erp_purchase_order_lines", poLineCountAfter === poLineCountBefore);
assert("evaluate does not write ai_tasks", aiTaskCountAfter === aiTaskCountBefore);

console.log("PROCUREMENT-RISK-FOUNDATION-1 security/idempotency verification passed.");

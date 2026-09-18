#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL = "bb000001-00a2-4000-8000-0000000000a2";
const MATERIAL_BOUNDARY = "bb000001-00a3-4000-8000-0000000000a3";
const PROFILE = "bb000001-0220-4000-8000-000000000220";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const BOUNDARY_PREFIX = "COMMINT2-BOUNDARY";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const CURRENT_START = "2026-07-01T00:00:00.000Z";
const CURRENT_END = "2026-08-01T00:00:00.000Z";
const REFERENCE_START = "2026-04-01T00:00:00.000Z";
const REFERENCE_END = "2026-06-30T23:59:59.000Z";

const PILOT_RUNS = {
  stable: "bb000001-0260-4000-8000-000000000260",
  upward: "bb000001-0261-4000-8000-000000000261",
  favorable: "bb000001-0262-4000-8000-000000000262",
  spread: "bb000001-0263-4000-8000-000000000263",
  tie: "bb000001-0264-4000-8000-000000000264",
  insufficient: "bb000001-0265-4000-8000-000000000265",
  conflict: "bb000001-0266-4000-8000-000000000266",
};

function fail(message) {
  console.error(`COMMERCIAL-INTELLIGENCE-2 deviation/supplier-comparison verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function assertNear(label, actual, expected, tolerance = 0.0001) {
  assert(label, actual !== null && actual !== undefined && Math.abs(actual - expected) < tolerance, `expected ${expected} got ${actual}`);
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
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

function median(values) {
  const ordered = [...values].sort((a, b) => a - b);
  const middle = Math.floor(ordered.length / 2);
  return ordered.length % 2 === 0
    ? (ordered[middle - 1] + ordered[middle]) / 2
    : ordered[middle];
}

function signedVarianceRatio(currentMedian, referenceMedian) {
  if (!referenceMedian || referenceMedian === 0) return null;
  return (currentMedian - referenceMedian) / referenceMedian;
}

function supplierSpreadRatio(highestMedian, lowestMedian) {
  if (!lowestMedian || lowestMedian === 0) return null;
  return (highestMedian - lowestMedian) / lowestMedian;
}

function windowsOverlap(currentStart, currentEnd, referenceStart, referenceEnd) {
  return new Date(currentStart) < new Date(referenceEnd) && new Date(referenceStart) < new Date(currentEnd);
}

function removeBoundaryFixtures() {
  db(`
    DELETE FROM commercial_purchase_price_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
    );
    DELETE FROM commercial_purchase_price_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
    );
    DELETE FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%';
    DELETE FROM commercial_purchase_price_evidence_observations
    WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%';
  `);
}

function insertBoundaryHighVarianceObservations() {
  const rows = [];
  const referencePrices = [100, 100, 100, 100, 100, 100];
  const currentPrices = [250, 250, 250, 250, 250, 250];
  for (let index = 0; index < 6; index += 1) {
    const refId = `bb000001-02f${index}-4000-8000-0000000002f${index}`;
    const curId = `bb000001-030${index}-4000-8000-00000000030${index}`;
    const refEvent = new Date(REFERENCE_START);
    refEvent.setUTCDate(refEvent.getUTCDate() + index * 10);
    const curEvent = new Date(CURRENT_START);
    curEvent.setUTCDate(curEvent.getUTCDate() + index * 3);
    rows.push(`
      INSERT INTO commercial_purchase_price_evidence_observations (
        id, tenant_id, facility_id, material_id, supplier_reference, source_document_type,
        source_document_reference, source_line_reference, commercial_event_at_utc, quantity,
        source_unit, normalized_quantity, normalized_unit, unit_price, currency_code,
        price_basis_tax_inclusion, price_basis_discount_inclusion, price_basis_freight_inclusion,
        evidence_status, source_fingerprint, source_provenance, observation_set_code,
        created_at_utc, created_by)
      VALUES (
        '${refId}', '${TENANT}', '${FACILITY}', '${MATERIAL_BOUNDARY}', 'SUP-BND-HV', 'simulated-purchase-order',
        'PO-BND-HV-REF-${index + 1}', 'LINE-001', '${refEvent.toISOString()}', 100, 'kg', 100, 'kg', ${referencePrices[index]}, 'TRY',
        'excluded', 'excluded', 'excluded', 'simulated', 'commint2-boundary-hv-ref-${index}', 'boundary fixture', '${BOUNDARY_PREFIX}-HIGH-VAR',
        '${refEvent.toISOString()}', 'boundary-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);
    rows.push(`
      INSERT INTO commercial_purchase_price_evidence_observations (
        id, tenant_id, facility_id, material_id, supplier_reference, source_document_type,
        source_document_reference, source_line_reference, commercial_event_at_utc, quantity,
        source_unit, normalized_quantity, normalized_unit, unit_price, currency_code,
        price_basis_tax_inclusion, price_basis_discount_inclusion, price_basis_freight_inclusion,
        evidence_status, source_fingerprint, source_provenance, observation_set_code,
        created_at_utc, created_by)
      VALUES (
        '${curId}', '${TENANT}', '${FACILITY}', '${MATERIAL_BOUNDARY}', 'SUP-BND-HV', 'simulated-purchase-order',
        'PO-BND-HV-CUR-${index + 1}', 'LINE-001', '${curEvent.toISOString()}', 100, 'kg', 100, 'kg', ${currentPrices[index]}, 'TRY',
        'excluded', 'excluded', 'excluded', 'simulated', 'commint2-boundary-hv-cur-${index}', 'boundary fixture', '${BOUNDARY_PREFIX}-HIGH-VAR',
        '${curEvent.toISOString()}', 'boundary-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);
  }
  db(rows.join("\n"));
}

console.log("Running COMMERCIAL-INTELLIGENCE-2 deviation/supplier-comparison verifier.");

const evaluator = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialPurchasePriceComparisonEvaluator.cs");
const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialIntelligenceComparisonSeed.cs");
const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialIntelligenceEndpointRouteBuilderExtensions.cs");

assert("comparison evaluator present", evaluator.includes("EvaluateTemporalDeviation"));
assert("supplier position evaluator present", evaluator.includes("EvaluateSupplierPricePosition"));
assert("non-overlap validation in repository", readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligenceComparisonOperations.cs").includes("must not overlap"));
assert("comparison endpoints present", endpoints.includes("/comparisons"));
forbidText(evaluator, "recommended supplier", "evaluator");
forbidText(evaluator, "guaranteed savings", "evaluator");
forbidText(seed, "recommended supplier", "seed");

assertNear("median calculation", median([100, 100, 101, 101, 102, 102]), 101);
assertNear("signed variance ratio >1", signedVarianceRatio(250, 100), 1.5);
assert("non-overlapping windows", !windowsOverlap(CURRENT_START, CURRENT_END, REFERENCE_START, REFERENCE_END));
assertNear("supplier spread ratio", supplierSpreadRatio(111.5, 96.5), 0.1554404, 0.001);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeBoundaryFixtures();

const provenance = "Deterministic local Development commercial-intelligence data — simulated, non-production, not real customer evidence, market prices, or guaranteed savings.";
const basis = {
  priceBasisTaxInclusion: "excluded",
  priceBasisDiscountInclusion: "excluded",
  priceBasisFreightInclusion: "excluded",
};

async function getPilotRun(runId) {
  const response = await liveRequest(
    "GET",
    `/api/commercial-intelligence/purchase-prices/comparisons/${runId}?facilityId=${FACILITY}`,
  );
  assert(`pilot run ${runId} 200`, response.status === 200, response.text?.slice(0, 200));
  return response.json;
}

async function evaluateComparison(name, body, extra = {}) {
  const response = await liveRequest(
    "POST",
    `/api/commercial-intelligence/purchase-prices/comparisons/evaluate?facilityId=${FACILITY}`,
    body,
  );
  if (extra.expectReject) {
    assert(`${name} rejected`, response.status !== 201, `status=${response.status}`);
    return response;
  }
  assert(`${name} evaluate 201`, response.status === 201, response.text?.slice(0, 300));
  return response.json.comparison;
}

const stable = await getPilotRun(PILOT_RUNS.stable);
assert("stable outcome", stable.comparisonOutcome === "within-reference-range", stable.comparisonOutcome);
assertNear("stable signed variance", stable.signedVarianceRatio, 0.05);

const upward = await getPilotRun(PILOT_RUNS.upward);
assert("upward outcome", upward.comparisonOutcome === "attention", upward.comparisonOutcome);
assert("upward leading driver", upward.leadingDriverCode === "current-window-price-increase", upward.leadingDriverCode);
assertNear("upward signed variance", upward.signedVarianceRatio, 0.15);

const favorable = await getPilotRun(PILOT_RUNS.favorable);
assert("favorable outcome", favorable.comparisonOutcome === "favorable-variance", favorable.comparisonOutcome);
assertNear("favorable signed variance", favorable.signedVarianceRatio, -0.1);

const spread = await getPilotRun(PILOT_RUNS.spread);
assert("supplier spread outcome", spread.comparisonOutcome === "attention", spread.comparisonOutcome);
assert("supplier spread driver present", spread.drivers.some((d) => d.reasonCode === "supplier-price-spread"));
assert("supplier spread results count", spread.supplierResults.length >= 2, String(spread.supplierResults.length));
assertNear("supplier spread ratio", spread.supplierSpreadRatio, 0.1554404, 0.001);

const tie = await getPilotRun(PILOT_RUNS.tie);
const tiedResults = tie.supplierResults.filter((r) => r.isTied);
assert("supplier tie results", tiedResults.length >= 2, String(tiedResults.length));
assert("supplier tie state", tiedResults.every((r) => r.positionState === "tied-comparable-price"));

const insufficient = await getPilotRun(PILOT_RUNS.insufficient);
assert("insufficient outcome", insufficient.comparisonOutcome === "insufficient-data", insufficient.comparisonOutcome);
assert("insufficient leading driver", insufficient.leadingDriverCode === "insufficient-current-window", insufficient.leadingDriverCode);

const conflict = await getPilotRun(PILOT_RUNS.conflict);
assert("source conflict outcome", conflict.comparisonOutcome === "source-conflict", conflict.comparisonOutcome);

insertBoundaryHighVarianceObservations();
const boundaryReferenceStart = "2026-04-01T00:00:00.000Z";
const boundaryReferenceEnd = "2026-05-21T00:00:00.000Z";
const boundaryCurrentStart = "2026-07-01T00:00:00.000Z";
const boundaryCurrentEnd = "2026-07-16T00:00:00.000Z";
const highVariance = await evaluateComparison("boundary-high-variance", {
  profileId: PROFILE,
  expectedProfileVersion: 1,
  comparisonType: "temporal-price-deviation",
  materialId: MATERIAL_BOUNDARY,
  currentWindowStartUtc: boundaryCurrentStart,
  currentWindowEndUtc: boundaryCurrentEnd,
  referenceWindowStartUtc: boundaryReferenceStart,
  referenceWindowEndUtc: boundaryReferenceEnd,
  currencyCode: "TRY",
  normalizedUnit: "kg",
  ...basis,
  sourceProvenance: provenance,
  idempotencyKey: `${BOUNDARY_PREFIX}-HIGH-VAR`,
});
assertNear("boundary high variance ratio", highVariance.signedVarianceRatio, 1.5);
assert("boundary high variance outcome", highVariance.comparisonOutcome === "attention", highVariance.comparisonOutcome);

const overlapReject = await evaluateComparison("overlap-reject", {
  profileId: PROFILE,
  expectedProfileVersion: 1,
  comparisonType: "temporal-price-deviation",
  materialId: MATERIAL,
  currentWindowStartUtc: "2026-05-01T00:00:00.000Z",
  currentWindowEndUtc: "2026-07-01T00:00:00.000Z",
  referenceWindowStartUtc: "2026-06-01T00:00:00.000Z",
  referenceWindowEndUtc: "2026-08-01T00:00:00.000Z",
  currencyCode: "TRY",
  normalizedUnit: "kg",
  ...basis,
  sourceProvenance: provenance,
  idempotencyKey: `${BOUNDARY_PREFIX}-OVERLAP`,
}, { expectReject: true });
assert("overlap windows rejected", overlapReject.status === 400, `status=${overlapReject.status}`);

const supplierEval = await evaluateComparison("supplier-position-live", {
  profileId: PROFILE,
  expectedProfileVersion: 1,
  comparisonType: "supplier-price-position",
  materialId: MATERIAL,
  currentWindowStartUtc: "2026-07-25T00:00:00.000Z",
  currentWindowEndUtc: "2026-08-10T00:00:00.000Z",
  currencyCode: "TRY",
  normalizedUnit: "kg",
  ...basis,
  sourceProvenance: provenance,
  idempotencyKey: `${BOUNDARY_PREFIX}-SUPPLIER`,
});
const ranks = supplierEval.supplierResults.map((r) => r.priceRank).sort((a, b) => a - b);
assert("supplier results present", supplierEval.supplierResults.length >= 2, String(supplierEval.supplierResults.length));
assert("supplier ranks include 1", ranks.includes(1), JSON.stringify(ranks));
assert("denominator counts present", supplierEval.comparableObservationCount > 0);
assert("excluded counts present", supplierEval.excludedObservationCount >= 0);

removeBoundaryFixtures();
const boundaryRuns = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_comparison_runs WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'`));
const boundaryObservations = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_evidence_observations WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%'`));
assert("boundary runs cleaned", boundaryRuns === 0, `count=${boundaryRuns}`);
assert("boundary observations cleaned", boundaryObservations === 0, `count=${boundaryObservations}`);

console.log("COMMERCIAL-INTELLIGENCE-2 deviation/supplier-comparison verification passed.");

#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL = "bb000001-00a2-4000-8000-0000000000a2";
const PROFILE = "bb000001-0220-4000-8000-000000000220";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const BOUNDARY_PREFIX = "COMMINT1-BOUNDARY";
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`COMMERCIAL-INTELLIGENCE-1 price-analysis verification failed: ${message}`);
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
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
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

function signedVarianceRatio(current, referenceMedian) {
  if (!referenceMedian) return null;
  if (referenceMedian === 0) return null;
  return (current - referenceMedian) / referenceMedian;
}

function supplierSpreadRatio(maxMedian, minMedian) {
  if (!minMedian || minMedian === 0) return null;
  return (maxMedian - minMedian) / minMedian;
}

function removeBoundaryFixtures() {
  db(`
    DELETE FROM commercial_purchase_price_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
    );
    DELETE FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%';
    DELETE FROM commercial_purchase_price_evidence_observations
    WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%';
  `);
}

function insertBoundarySpreadObservations() {
  const eventBase = "2026-02-01T12:00:00.000Z";
  const rows = [];
  const sup001Ids = [
    "bb000001-0240-4000-8000-000000000240",
    "bb000001-0241-4000-8000-000000000241",
    "bb000001-0242-4000-8000-000000000242",
    "bb000001-0243-4000-8000-000000000243",
    "bb000001-0244-4000-8000-000000000244",
    "bb000001-0245-4000-8000-000000000245",
  ];
  const sup002Ids = [
    "bb000001-0250-4000-8000-000000000250",
    "bb000001-0251-4000-8000-000000000251",
    "bb000001-0252-4000-8000-000000000252",
    "bb000001-0253-4000-8000-000000000253",
    "bb000001-0254-4000-8000-000000000254",
    "bb000001-0255-4000-8000-000000000255",
  ];
  for (let index = 0; index < 6; index += 1) {
    const sup001Id = sup001Ids[index];
    const sup002Id = sup002Ids[index];
    const eventAt = new Date(eventBase);
    eventAt.setUTCDate(eventAt.getUTCDate() - index);
    const eventIso = eventAt.toISOString();
    rows.push(`
      INSERT INTO commercial_purchase_price_evidence_observations (
        id, tenant_id, facility_id, material_id, supplier_reference, source_document_type,
        source_document_reference, source_line_reference, commercial_event_at_utc, quantity,
        source_unit, normalized_quantity, normalized_unit, unit_price, currency_code,
        price_basis_tax_inclusion, price_basis_discount_inclusion, price_basis_freight_inclusion,
        evidence_status, source_fingerprint, source_provenance, observation_set_code,
        created_at_utc, created_by)
      VALUES (
        '${sup001Id}', '${TENANT}', '${FACILITY}', '${MATERIAL}', 'SUP-BND-001', 'simulated-purchase-order',
        'PO-BND-001-${index + 1}', 'LINE-001', '${eventIso}', 100, 'kg', 100, 'kg', 100, 'TRY',
        'excluded', 'excluded', 'excluded', 'simulated', 'commint1-boundary-sup001-${index}', 'boundary fixture', '${BOUNDARY_PREFIX}-SPREAD',
        '${eventIso}', 'boundary-verifier')
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
        '${sup002Id}', '${TENANT}', '${FACILITY}', '${MATERIAL}', 'SUP-BND-002', 'simulated-purchase-order',
        'PO-BND-002-${index + 1}', 'LINE-001', '${eventIso}', 100, 'kg', 100, 'kg', 250, 'TRY',
        'excluded', 'excluded', 'excluded', 'simulated', 'commint1-boundary-sup002-${index}', 'boundary fixture', '${BOUNDARY_PREFIX}-SPREAD',
        '${eventIso}', 'boundary-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);
  }
  db(rows.join("\n"));
}

console.log("Running COMMERCIAL-INTELLIGENCE-1 price-analysis verifier.");

const evaluator = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialIntelligencePurchasePriceEvaluator.cs");
const reader = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialIntelligencePriceEvidenceReader.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
const seed = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotCommercialIntelligenceSeed.cs");
const ratioMigration = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260824134830_RelaxCommercialPurchasePriceRatioUpperBounds.cs");

assert("median helper present", evaluator.includes("ComputeMedian"));
assert("signed variance helper present", evaluator.includes("ComputeSignedVarianceRatio"));
assert("default min observations 6", constants.includes("DefaultMinimumComparableObservations = 6"));
assert("reference center median", constants.includes('ReferenceCenterMedian = "median"'));
assert("compatibility reader present", reader.includes("EvaluateCompatibility"));
assert("ratio migration relaxes upper bound", ratioMigration.includes("variance_ratio > -1"));
const ratioMigrationUp = ratioMigration.split("protected override void Down")[0];
forbidText(ratioMigrationUp, "variance_ratio <= 1", "ratio migration up");
forbidText(evaluator, "guaranteed savings", "evaluator");
forbidText(seed, "guaranteed savings", "seed");

const prices = [100, 100, 101, 101, 102, 102];
const referenceMedian = median(prices);
assert("median calculation", referenceMedian === 101, `expected 101 got ${referenceMedian}`);
const ratio = signedVarianceRatio(115, referenceMedian);
assert("signed variance ratio", Math.abs(ratio - 0.13861386138613864) < 0.0001, `ratio=${ratio}`);
assert("zero median guard", signedVarianceRatio(10, 0) === null);
assertNear("supplier spread ratio 1.5", supplierSpreadRatio(250, 100), 1.5);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeBoundaryFixtures();

const eventTime = new Date("2026-08-24T12:15:00.000Z");
const windowStart = new Date(eventTime);
windowStart.setUTCDate(windowStart.getUTCDate() - 120);
const windowEnd = new Date(eventTime);
windowEnd.setUTCDate(windowEnd.getUTCDate() + 1);
const withinRangeWindowStart = new Date(eventTime);
withinRangeWindowStart.setUTCDate(withinRangeWindowStart.getUTCDate() - 65);
const withinRangeWindowEnd = new Date(eventTime);
withinRangeWindowEnd.setUTCDate(withinRangeWindowEnd.getUTCDate() - 30);
const boundarySpreadWindowStart = new Date("2026-01-25T00:00:00.000Z");
const boundarySpreadWindowEnd = new Date("2026-02-10T00:00:00.000Z");
const provenance = "Deterministic local Development commercial-intelligence data - simulated, non-production, not real customer evidence, market prices, or guaranteed savings.";
const basis = {
  priceBasisTaxInclusion: "excluded",
  priceBasisDiscountInclusion: "excluded",
  priceBasisFreightInclusion: "excluded",
};

async function evaluateScenario(name, currentUnitPrice, extra = {}) {
  const suffix = extra.suffix ?? Date.now();
  const response = await liveRequest(
    "POST",
    `/api/commercial-intelligence/purchase-prices/assessments/evaluate?facilityId=${FACILITY}`,
    {
      profileId: PROFILE,
      materialId: MATERIAL,
      supplierReference: extra.supplierReference ?? null,
      comparisonDimension: extra.comparisonDimension ?? "material",
      currentUnitPrice,
      currencyCode: extra.currencyCode ?? "TRY",
      normalizedUnit: extra.normalizedUnit ?? "kg",
      windowStartUtc: (extra.windowStartUtc ?? windowStart).toISOString(),
      windowEndUtc: (extra.windowEndUtc ?? windowEnd).toISOString(),
      ...basis,
      sourceProvenance: provenance,
      idempotencyKey: extra.idempotencyKey ?? `COMMINT1-ANALYSIS-${name}-${suffix}`,
    },
  );
  if (extra.expectReject) {
    assert(`${name} rejected`, response.status !== 201, `status=${response.status}`);
    return response;
  }
  assert(`${name} evaluate 201`, response.status === 201, response.text?.slice(0, 300));
  return response.json.assessment;
}

const profiles = await liveRequest(
  "GET",
  `/api/commercial-intelligence/purchase-prices/profiles?facilityId=${FACILITY}`,
);
const pilotProfile = profiles.json?.items?.find((item) => item.id === PROFILE);
assert("pilot maximumPositiveVarianceRatio 0.10", pilotProfile?.maximumPositiveVarianceRatio === 0.1, String(pilotProfile?.maximumPositiveVarianceRatio));
assert("pilot supplierSpreadThreshold 0.10", pilotProfile?.supplierSpreadThreshold === 0.1, String(pilotProfile?.supplierSpreadThreshold));

const highIncrease = await evaluateScenario("boundary-high-increase", 303, {
  windowStartUtc: withinRangeWindowStart,
  windowEndUtc: withinRangeWindowEnd,
  idempotencyKey: `${BOUNDARY_PREFIX}-HIGH-INCREASE`,
});
assertNear("boundary high increase variance ratio", highIncrease.varianceRatio, 2.0);
assert("boundary high increase outcome", highIncrease.assessmentOutcome === "attention", highIncrease.assessmentOutcome);
assert("boundary high increase leading driver", highIncrease.leadingDriverCode === "purchase-price-increase", highIncrease.leadingDriverCode);
const highIncreaseDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${BOUNDARY_PREFIX}-HIGH-INCREASE'`));
assertNear("boundary high increase persisted ratio", highIncreaseDb, 2.0);

const unitIncrease = await evaluateScenario("boundary-unit-increase", 202, {
  windowStartUtc: withinRangeWindowStart,
  windowEndUtc: withinRangeWindowEnd,
  idempotencyKey: `${BOUNDARY_PREFIX}-UNIT-INCREASE`,
});
assertNear("boundary unit increase variance ratio", unitIncrease.varianceRatio, 1.0);
const unitIncreaseDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${BOUNDARY_PREFIX}-UNIT-INCREASE'`));
assertNear("boundary unit increase persisted ratio", unitIncreaseDb, 1.0);

const deepDiscount = await evaluateScenario("boundary-deep-discount", 1, {
  windowStartUtc: withinRangeWindowStart,
  windowEndUtc: withinRangeWindowEnd,
  idempotencyKey: `${BOUNDARY_PREFIX}-DEEP-DISCOUNT`,
});
assertNear("boundary deep discount variance ratio", deepDiscount.varianceRatio, -0.990099);
assert("boundary deep discount outcome", deepDiscount.assessmentOutcome === "favorable-variance", deepDiscount.assessmentOutcome);
const deepDiscountDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${BOUNDARY_PREFIX}-DEEP-DISCOUNT'`));
assertNear("boundary deep discount persisted ratio", deepDiscountDb, -0.990099);

await evaluateScenario("boundary-zero-price", 0, { expectReject: true });

insertBoundarySpreadObservations();
const spreadBoundary = await evaluateScenario("boundary-supplier-spread", 175, {
  windowStartUtc: boundarySpreadWindowStart,
  windowEndUtc: boundarySpreadWindowEnd,
  idempotencyKey: `${BOUNDARY_PREFIX}-SUPPLIER-SPREAD`,
});
assert(
  "boundary supplier spread driver present",
  spreadBoundary.drivers.some((driver) => driver.reasonCode === "supplier-price-spread"),
  JSON.stringify(spreadBoundary.drivers.map((driver) => driver.reasonCode)),
);
const spreadDriver = spreadBoundary.drivers.find((driver) => driver.reasonCode === "supplier-price-spread");
assertNear("boundary supplier spread ratio", spreadDriver?.normalizedVariance ?? spreadDriver?.actualValue, 1.5);

const withinRange = await evaluateScenario("within-range", 101);
assert("within-range outcome", withinRange.assessmentOutcome === "within-reference-range", withinRange.assessmentOutcome);
assert("within-range comparable count", withinRange.comparableObservationCount >= 6, String(withinRange.comparableObservationCount));

const attention = await evaluateScenario("attention", 115);
assert("attention outcome", attention.assessmentOutcome === "attention", attention.assessmentOutcome);
assert("attention leading driver", attention.leadingDriverCode === "purchase-price-increase", attention.leadingDriverCode);

const favorable = await evaluateScenario("favorable", 90);
assert("favorable outcome", favorable.assessmentOutcome === "favorable-variance", favorable.assessmentOutcome);

const supplierSpread = await evaluateScenario("supplier-spread", 108, {
  comparisonDimension: "material",
  supplierReference: "SUP-001",
});
assert("supplier-spread comparable count", supplierSpread.comparableObservationCount >= 6, String(supplierSpread.comparableObservationCount));
assert(
  "supplier-spread driver present",
  supplierSpread.drivers.some((driver) => driver.reasonCode === "supplier-price-spread"),
  JSON.stringify(supplierSpread.drivers.map((driver) => driver.reasonCode)),
);

const insufficientWindowStart = new Date(eventTime);
insufficientWindowStart.setUTCDate(insufficientWindowStart.getUTCDate() - 4);
const insufficient = await evaluateScenario("insufficient", 100, {
  windowStartUtc: insufficientWindowStart,
  windowEndUtc: eventTime,
});
assert("insufficient outcome", insufficient.assessmentOutcome === "insufficient-data", insufficient.assessmentOutcome);
assert("insufficient comparable count", insufficient.comparableObservationCount < 6, String(insufficient.comparableObservationCount));

const conflict = await evaluateScenario("source-conflict", 100, {
  windowStartUtc: new Date(eventTime.getTime() - 15 * 24 * 60 * 60 * 1000),
  windowEndUtc: eventTime,
});
assert("source-conflict outcome", conflict.assessmentOutcome === "source-conflict", conflict.assessmentOutcome);
assert("source-conflict excluded count", conflict.excludedObservationCount >= 2, String(conflict.excludedObservationCount));

const driverOrder = attention.drivers.map((driver) => driver.reasonCode);
assert("drivers returned", driverOrder.length > 0);
assert("leading driver flagged", attention.drivers.some((driver) => driver.isLeadingDriver));

removeBoundaryFixtures();
const boundaryAssessments = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'`));
const boundaryObservations = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_evidence_observations WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%'`));
assert("boundary assessments cleaned", boundaryAssessments === 0, `count=${boundaryAssessments}`);
assert("boundary observations cleaned", boundaryObservations === 0, `count=${boundaryObservations}`);

console.log("COMMERCIAL-INTELLIGENCE-1 price-analysis verification passed.");

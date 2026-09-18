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
const BOUNDARY_PREFIX = "COMMINT1-BOUNDARY";
const ISOLATION_PREFIX = "COMMINT1-RATIO-ISOLATION";
const SUPPLIER_BOUNDARY = "SUP-RATIO-ISO";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
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
      SELECT id FROM commercial_purchase_price_assessments
      WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
         OR idempotency_key LIKE '${ISOLATION_PREFIX}%'
    );
    DELETE FROM commercial_purchase_price_assessments
    WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
       OR idempotency_key LIKE '${ISOLATION_PREFIX}%';
    DELETE FROM commercial_purchase_price_evidence_observations
    WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%'
       OR observation_set_code LIKE '${ISOLATION_PREFIX}%';
  `);
}

function isolationFixtureCounts() {
  return {
    assessments: Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${ISOLATION_PREFIX}%'`)),
    drivers: Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessment_drivers WHERE assessment_id IN (SELECT id FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${ISOLATION_PREFIX}%')`)),
    observations: Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_evidence_observations WHERE observation_set_code LIKE '${ISOLATION_PREFIX}%'`)),
    orphanDrivers: Number(db(`
      SELECT COUNT(*) FROM commercial_purchase_price_assessment_drivers d
      WHERE NOT EXISTS (SELECT 1 FROM commercial_purchase_price_assessments a WHERE a.id = d.assessment_id)
    `)),
  };
}

function insertRatioIsolationObservations({
  setCode,
  prices,
  observationIds,
  windowStartUtc,
  windowEndUtc,
  materialId = MATERIAL_BOUNDARY,
  supplierReference = SUPPLIER_BOUNDARY,
}) {
  const rows = [];
  const startMs = windowStartUtc.getTime();
  const endMs = windowEndUtc.getTime();
  const stepMs = prices.length > 1 ? (endMs - startMs) / (prices.length - 1) : 0;
  for (let index = 0; index < prices.length; index += 1) {
    const observationId = observationIds[index];
    const eventAt = new Date(startMs + stepMs * index).toISOString();
    rows.push(`
      INSERT INTO commercial_purchase_price_evidence_observations (
        id, tenant_id, facility_id, material_id, supplier_reference, source_document_type,
        source_document_reference, source_line_reference, commercial_event_at_utc, quantity,
        source_unit, normalized_quantity, normalized_unit, unit_price, currency_code,
        price_basis_tax_inclusion, price_basis_discount_inclusion, price_basis_freight_inclusion,
        evidence_status, source_fingerprint, source_provenance, observation_set_code,
        created_at_utc, created_by)
      VALUES (
        '${observationId}', '${TENANT}', '${FACILITY}', '${materialId}', '${supplierReference}', 'simulated-purchase-order',
        'PO-${setCode}-${index + 1}', 'LINE-001', '${eventAt}', 100, 'kg', 100, 'kg', ${prices[index]}, 'TRY',
        'excluded', 'excluded', 'excluded', 'simulated', '${setCode.toLowerCase()}-${index}', 'ratio isolation fixture', '${setCode}',
        '${eventAt}', 'ratio-isolation-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);
  }
  db(rows.join("\n"));
}

function insertBoundarySpreadObservations() {
  const eventBase = "2025-02-01T12:00:00.000Z";
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
        '${sup001Id}', '${TENANT}', '${FACILITY}', '${MATERIAL_BOUNDARY}', 'SUP-BND-001', 'simulated-purchase-order',
        'PO-BND-001-${index + 1}', 'LINE-001', '${eventIso}', 100, 'kg', 100, 'kg', 100, 'TRY',
        'excluded', 'excluded', 'excluded', 'simulated', 'commint1-boundary-sup001-${index}', 'boundary fixture', '${ISOLATION_PREFIX}-SPREAD',
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
        '${sup002Id}', '${TENANT}', '${FACILITY}', '${MATERIAL_BOUNDARY}', 'SUP-BND-002', 'simulated-purchase-order',
        'PO-BND-002-${index + 1}', 'LINE-001', '${eventIso}', 100, 'kg', 100, 'kg', 250, 'TRY',
        'excluded', 'excluded', 'excluded', 'simulated', 'commint1-boundary-sup002-${index}', 'boundary fixture', '${ISOLATION_PREFIX}-SPREAD',
        '${eventIso}', 'boundary-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);
  }
  db(rows.join("\n"));
}

console.log("Running COMMERCIAL-INTELLIGENCE-1 price-analysis verifier.");

const evaluator = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligencePurchasePriceEvaluator.cs");
const reader = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligencePriceEvidenceReader.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialIntelligenceSeed.cs");
const ratioMigration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260824134830_RelaxCommercialPurchasePriceRatioUpperBounds.cs");

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
const boundarySpreadWindowStart = new Date("2025-01-25T00:00:00.000Z");
const boundarySpreadWindowEnd = new Date("2025-02-10T00:00:00.000Z");
const median101WindowStart = new Date("2025-06-01T00:00:00.000Z");
const median101WindowEnd = new Date("2025-08-01T00:00:00.000Z");
const median100WindowStart = new Date("2025-03-01T00:00:00.000Z");
const median100WindowEnd = new Date("2025-05-01T00:00:00.000Z");
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
      materialId: extra.materialId ?? MATERIAL,
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

function windowObservationPrices(materialId, windowStartUtc, windowEndUtc) {
  const rows = db(`
    SELECT unit_price::text
    FROM commercial_purchase_price_evidence_observations
    WHERE tenant_id = '${TENANT}'
      AND facility_id = '${FACILITY}'
      AND material_id = '${materialId}'
      AND commercial_event_at_utc >= '${windowStartUtc.toISOString()}'
      AND commercial_event_at_utc <= '${windowEndUtc.toISOString()}'
    ORDER BY commercial_event_at_utc
  `);
  return rows.split("\n").filter(Boolean).map((value) => Number(value));
}

function assertRatioMatrixCase({
  name,
  referenceMedian,
  expectedRatio,
  windowStartUtc,
  windowEndUtc,
}) {
  const prices = windowObservationPrices(MATERIAL_BOUNDARY, windowStartUtc, windowEndUtc);
  const computedMedian = median(prices);
  assert(
    `${name} reference median proof`,
    computedMedian === referenceMedian,
    `included=${JSON.stringify(prices)} median=${computedMedian}`,
  );
  if (expectedRatio < 0) {
    assert(`${name} negative ratio greater than -1`, expectedRatio > -1, String(expectedRatio));
  }
}

const preIsolationCounts = isolationFixtureCounts();
assert("pre-setup isolation assessments zero", preIsolationCounts.assessments === 0, `count=${preIsolationCounts.assessments}`);
assert("pre-setup isolation observations zero", preIsolationCounts.observations === 0, `count=${preIsolationCounts.observations}`);

const median101SetCode = `${ISOLATION_PREFIX}-MEDIAN-101`;
const median100SetCode = `${ISOLATION_PREFIX}-MEDIAN-100`;
const median101ObservationIds = [
  "bb000001-03c0-4000-8000-0000000003c0",
  "bb000001-03c1-4000-8000-0000000003c1",
  "bb000001-03c2-4000-8000-0000000003c2",
  "bb000001-03c3-4000-8000-0000000003c3",
  "bb000001-03c4-4000-8000-0000000003c4",
  "bb000001-03c5-4000-8000-0000000003c5",
];
const median100ObservationIds = [
  "bb000001-03d0-4000-8000-0000000003d0",
  "bb000001-03d1-4000-8000-0000000003d1",
  "bb000001-03d2-4000-8000-0000000003d2",
  "bb000001-03d3-4000-8000-0000000003d3",
  "bb000001-03d4-4000-8000-0000000003d4",
  "bb000001-03d5-4000-8000-0000000003d5",
];

try {
insertRatioIsolationObservations({
  setCode: median101SetCode,
  prices: [100, 100, 101, 101, 102, 102],
  observationIds: median101ObservationIds,
  windowStartUtc: median101WindowStart,
  windowEndUtc: median101WindowEnd,
});
insertRatioIsolationObservations({
  setCode: median100SetCode,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: median100ObservationIds,
  windowStartUtc: median100WindowStart,
  windowEndUtc: median100WindowEnd,
});

const boundaryMaterial = { materialId: MATERIAL_BOUNDARY };
const median101Window = {
  windowStartUtc: median101WindowStart,
  windowEndUtc: median101WindowEnd,
};
const median100Window = {
  windowStartUtc: median100WindowStart,
  windowEndUtc: median100WindowEnd,
};

const profiles = await liveRequest(
  "GET",
  `/api/commercial-intelligence/purchase-prices/profiles?facilityId=${FACILITY}`,
);
const pilotProfile = profiles.json?.items?.find((item) => item.id === PROFILE);
assert("pilot maximumPositiveVarianceRatio 0.10", pilotProfile?.maximumPositiveVarianceRatio === 0.1, String(pilotProfile?.maximumPositiveVarianceRatio));
assert("pilot supplierSpreadThreshold 0.10", pilotProfile?.supplierSpreadThreshold === 0.1, String(pilotProfile?.supplierSpreadThreshold));

const highIncrease = await evaluateScenario("boundary-high-increase", 303, {
  ...median101Window,
  ...boundaryMaterial,
  idempotencyKey: `${ISOLATION_PREFIX}-RATIO-2-0`,
});
assertRatioMatrixCase({
  name: "boundary ratio 2.0",
  referenceMedian: 101,
  expectedRatio: 2.0,
  ...median101Window,
});
assertNear("boundary high increase variance ratio", highIncrease.varianceRatio, 2.0);
assert("boundary high increase outcome", highIncrease.assessmentOutcome === "attention", highIncrease.assessmentOutcome);
assert("boundary high increase leading driver", highIncrease.leadingDriverCode === "purchase-price-increase", highIncrease.leadingDriverCode);
assert("boundary high increase ratio exceeds 1", highIncrease.varianceRatio > 1, String(highIncrease.varianceRatio));
const highIncreaseDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${ISOLATION_PREFIX}-RATIO-2-0'`));
assertNear("boundary high increase persisted ratio", highIncreaseDb, 2.0);
assertNear("boundary high increase api/db ratio reconciliation", highIncrease.varianceRatio, highIncreaseDb);

const unitIncrease = await evaluateScenario("boundary-unit-increase", 202, {
  ...median101Window,
  ...boundaryMaterial,
  idempotencyKey: `${ISOLATION_PREFIX}-RATIO-1-0`,
});
assertRatioMatrixCase({
  name: "boundary ratio 1.0",
  referenceMedian: 101,
  expectedRatio: 1.0,
  ...median101Window,
});
assertNear("boundary unit increase variance ratio", unitIncrease.varianceRatio, 1.0);
const unitIncreaseDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${ISOLATION_PREFIX}-RATIO-1-0'`));
assertNear("boundary unit increase persisted ratio", unitIncreaseDb, 1.0);
assertNear("boundary unit increase api/db ratio reconciliation", unitIncrease.varianceRatio, unitIncreaseDb);

const deepDiscount = await evaluateScenario("boundary-deep-discount", 1, {
  ...median101Window,
  ...boundaryMaterial,
  idempotencyKey: `${ISOLATION_PREFIX}-RATIO-DEEP-DISCOUNT`,
});
assertRatioMatrixCase({
  name: "boundary ratio deep discount",
  referenceMedian: 101,
  expectedRatio: -0.9900990099,
  ...median101Window,
});
assertNear("boundary deep discount variance ratio", deepDiscount.varianceRatio, -0.9900990099);
assert("boundary deep discount outcome", deepDiscount.assessmentOutcome === "favorable-variance", deepDiscount.assessmentOutcome);
const deepDiscountDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${ISOLATION_PREFIX}-RATIO-DEEP-DISCOUNT'`));
assertNear("boundary deep discount persisted ratio", deepDiscountDb, -0.9900990099);
assertNear("boundary deep discount api/db ratio reconciliation", deepDiscount.varianceRatio, deepDiscountDb);

const ratio150 = await evaluateScenario("boundary-ratio-1-5", 250, {
  ...median100Window,
  ...boundaryMaterial,
  idempotencyKey: `${ISOLATION_PREFIX}-RATIO-1-5`,
});
assertRatioMatrixCase({
  name: "boundary ratio 1.5",
  referenceMedian: 100,
  expectedRatio: 1.5,
  ...median100Window,
});
assertNear("boundary ratio 1.5 variance ratio", ratio150.varianceRatio, 1.5);
const ratio150Db = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${ISOLATION_PREFIX}-RATIO-1-5'`));
assertNear("boundary ratio 1.5 persisted ratio", ratio150Db, 1.5);
assertNear("boundary ratio 1.5 api/db ratio reconciliation", ratio150.varianceRatio, ratio150Db);

const ratioZero = await evaluateScenario("boundary-ratio-zero", 100, {
  ...median100Window,
  ...boundaryMaterial,
  idempotencyKey: `${ISOLATION_PREFIX}-RATIO-ZERO`,
});
assertRatioMatrixCase({
  name: "boundary ratio 0",
  referenceMedian: 100,
  expectedRatio: 0,
  ...median100Window,
});
assertNear("boundary ratio zero variance ratio", ratioZero.varianceRatio, 0);
const ratioZeroDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${ISOLATION_PREFIX}-RATIO-ZERO'`));
assertNear("boundary ratio zero persisted ratio", ratioZeroDb, 0);

const ratioNegativeHalf = await evaluateScenario("boundary-ratio-negative-half", 50, {
  ...median100Window,
  ...boundaryMaterial,
  idempotencyKey: `${ISOLATION_PREFIX}-RATIO-NEG-HALF`,
});
assertRatioMatrixCase({
  name: "boundary ratio -0.5",
  referenceMedian: 100,
  expectedRatio: -0.5,
  ...median100Window,
});
assertNear("boundary ratio negative half variance ratio", ratioNegativeHalf.varianceRatio, -0.5);
const ratioNegativeHalfDb = Number(db(`SELECT variance_ratio FROM commercial_purchase_price_assessments WHERE idempotency_key = '${ISOLATION_PREFIX}-RATIO-NEG-HALF'`));
assertNear("boundary ratio negative half persisted ratio", ratioNegativeHalfDb, -0.5);

const zeroPriceKey = `${ISOLATION_PREFIX}-ZERO-PRICE`;
const zeroPriceAssessmentsBefore = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessments WHERE idempotency_key = '${zeroPriceKey}'`));
const zeroPriceDriversBefore = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessment_drivers WHERE assessment_id IN (SELECT id FROM commercial_purchase_price_assessments WHERE idempotency_key = '${zeroPriceKey}')`));
await evaluateScenario("boundary-zero-price", 0, {
  ...median100Window,
  ...boundaryMaterial,
  idempotencyKey: zeroPriceKey,
  expectReject: true,
});
const zeroPriceAssessmentsAfter = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessments WHERE idempotency_key = '${zeroPriceKey}'`));
const zeroPriceDriversAfter = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessment_drivers WHERE assessment_id IN (SELECT id FROM commercial_purchase_price_assessments WHERE idempotency_key = '${zeroPriceKey}')`));
assert("boundary zero price assessment rows unchanged", zeroPriceAssessmentsBefore === 0 && zeroPriceAssessmentsAfter === 0, `before=${zeroPriceAssessmentsBefore} after=${zeroPriceAssessmentsAfter}`);
assert("boundary zero price driver rows unchanged", zeroPriceDriversBefore === 0 && zeroPriceDriversAfter === 0, `before=${zeroPriceDriversBefore} after=${zeroPriceDriversAfter}`);

insertBoundarySpreadObservations();
const spreadBoundary = await evaluateScenario("boundary-supplier-spread", 175, {
  windowStartUtc: boundarySpreadWindowStart,
  windowEndUtc: boundarySpreadWindowEnd,
  ...boundaryMaterial,
  idempotencyKey: `${ISOLATION_PREFIX}-SUPPLIER-SPREAD`,
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

} finally {
removeBoundaryFixtures();
const postIsolationCounts = isolationFixtureCounts();
assert("post-cleanup isolation assessments zero", postIsolationCounts.assessments === 0, `count=${postIsolationCounts.assessments}`);
assert("post-cleanup isolation drivers zero", postIsolationCounts.drivers === 0, `count=${postIsolationCounts.drivers}`);
assert("post-cleanup isolation observations zero", postIsolationCounts.observations === 0, `count=${postIsolationCounts.observations}`);
assert("post-cleanup orphan drivers zero", postIsolationCounts.orphanDrivers === 0, `count=${postIsolationCounts.orphanDrivers}`);
const legacyBoundaryAssessments = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'`));
const legacyBoundaryObservations = Number(db(`SELECT COUNT(*) FROM commercial_purchase_price_evidence_observations WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%'`));
assert("legacy boundary assessments cleaned", legacyBoundaryAssessments === 0, `count=${legacyBoundaryAssessments}`);
assert("legacy boundary observations cleaned", legacyBoundaryObservations === 0, `count=${legacyBoundaryObservations}`);
}

console.log("COMMERCIAL-INTELLIGENCE-1 price-analysis verification passed.");

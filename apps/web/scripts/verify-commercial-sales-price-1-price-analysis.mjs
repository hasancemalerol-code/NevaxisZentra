#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL = "bb000001-00a2-4000-8000-0000000000a2";
const MATERIAL_BOUNDARY = "bb000001-00a3-4000-8000-0000000000a3";
const PROFILE = "bb000001-0270-4000-8000-000000000270";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const BOUNDARY_PREFIX = "COMMSALE1-BOUNDARY";
const SOURCE_EVIDENCE_KIND = "manual-development-simulator";
const PRICE_BASIS_GROSS_NET = "net";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const SALES_SOURCE_FILES = [
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceEvaluator.cs",
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceEvidenceReader.cs",
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceRepository.cs",
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceModels.cs",
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceEntities.cs",
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceEntityConfigurations.cs",
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceIdempotencyFingerprint.cs",
  "packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligenceSalesPriceContracts.cs",
  "packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialSalesPriceSeed.cs",
  "apps/api/CommercialIntelligence/CommercialSalesPriceEndpointRouteBuilderExtensions.cs",
];

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-1 price-analysis verification failed: ${message}`);
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
  if (source.toLowerCase().includes(forbidden.toLowerCase())) {
    fail(`${label} must not contain "${forbidden}"`);
  }
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

function assertCandidateCounts(assessment, label) {
  assert(
    `${label} candidate = comparable + excluded`,
    assessment.candidateObservationCount === assessment.comparableObservationCount + assessment.excludedObservationCount,
    `candidate=${assessment.candidateObservationCount} comparable=${assessment.comparableObservationCount} excluded=${assessment.excludedObservationCount}`,
  );
}

function removeBoundaryFixtures() {
  db(`
    DELETE FROM commercial_sales_price_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_sales_price_assessments
      WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
    );
    DELETE FROM commercial_sales_price_assessments
    WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%';
    DELETE FROM commercial_sales_price_evidence_observations
    WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%';
  `);
}

function boundaryFixtureCounts() {
  return {
    assessments: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'`)),
    drivers: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_assessment_drivers WHERE assessment_id IN (SELECT id FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%')`)),
    observations: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_evidence_observations WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%'`)),
    orphanDrivers: Number(db(`
      SELECT COUNT(*) FROM commercial_sales_price_assessment_drivers d
      WHERE NOT EXISTS (SELECT 1 FROM commercial_sales_price_assessments a WHERE a.id = d.assessment_id)
    `)),
  };
}

function insertBoundaryObservations({
  setCode,
  prices,
  observationIds,
  windowStartUtc,
  windowEndUtc,
  materialId = MATERIAL_BOUNDARY,
  customerReference = "CUST-BND",
}) {
  const rows = [];
  const startMs = windowStartUtc.getTime();
  const endMs = windowEndUtc.getTime();
  const stepMs = prices.length > 1 ? (endMs - startMs) / (prices.length - 1) : 0;
  for (let index = 0; index < prices.length; index += 1) {
    const observationId = observationIds[index];
    const eventAt = new Date(startMs + stepMs * index).toISOString();
    rows.push(`
      INSERT INTO commercial_sales_price_evidence_observations (
        id, tenant_id, facility_id, material_id, customer_reference, source_evidence_kind,
        source_document_reference, source_line_reference, commercial_event_at_utc, received_at_utc,
        quantity, source_unit, normalized_quantity, normalized_unit, original_unit_price, comparable_unit_price,
        currency_code, price_basis_gross_net, price_basis_tax_inclusion, price_basis_discount_inclusion,
        price_basis_freight_inclusion, is_cancelled_or_returned, evidence_status, source_fingerprint,
        source_provenance, observation_set_code, created_at_utc, created_by)
      VALUES (
        '${observationId}', '${TENANT}', '${FACILITY}', '${materialId}', '${customerReference}', '${SOURCE_EVIDENCE_KIND}',
        'SO-${setCode}-${index + 1}', 'LINE-001', '${eventAt}', '${eventAt}', 10, 'kg', 10, 'kg',
        ${prices[index]}, ${prices[index]}, 'TRY', '${PRICE_BASIS_GROSS_NET}', 'excluded', 'excluded', 'excluded',
        false, 'simulated', '${setCode.toLowerCase()}-${index}', 'boundary fixture', '${setCode}',
        '${eventAt}', 'boundary-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);
  }
  db(rows.join("\n"));
}

console.log("Running COMMERCIAL-SALES-PRICE-1 price-analysis verifier.");

const evaluator = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceEvaluator.cs");
const reader = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialSalesPriceEvidenceReader.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialSalesPriceSeed.cs");

assert("median helper present", evaluator.includes("ComputeMedian"));
assert("signed variance helper present", evaluator.includes("ComputeSignedVarianceRatio"));
assert("default min observations 6", constants.includes("DefaultMinimumComparableObservations = 6"));
assert("reference center median", constants.includes('ReferenceCenterMedian = "median"'));
assert("compatibility reader present", reader.includes("EvaluateCompatibility"));
assert("seed bootstrap present", seed.includes("EnsureCommercialSalesPricePilotRecordsAsync"));
assert("seed within-range fixture present", seed.includes("COMMSALE1-SET-WITHIN-RANGE"));

for (const relativePath of SALES_SOURCE_FILES) {
  const source = readRequired(relativePath);
  forbidText(source, "recommendation", relativePath);
  forbidText(source, "guaranteed savings", relativePath);
}

const prices = [100, 100, 101, 101, 102, 102];
const referenceMedian = median(prices);
assert("median calculation", referenceMedian === 101, `expected 101 got ${referenceMedian}`);
const ratio = signedVarianceRatio(115, referenceMedian);
assert("signed variance ratio", Math.abs(ratio - 0.13861386138613864) < 0.0001, `ratio=${ratio}`);
assert("zero median guard", signedVarianceRatio(10, 0) === null);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeBoundaryFixtures();

const eventTime = new Date("2026-06-15T12:00:00.000Z");
const windowStart = new Date(eventTime);
windowStart.setUTCDate(windowStart.getUTCDate() - 120);
const windowEnd = new Date(eventTime);
windowEnd.setUTCDate(windowEnd.getUTCDate() + 1);
const median101WindowStart = new Date("2025-06-01T00:00:00.000Z");
const median101WindowEnd = new Date("2025-08-01T00:00:00.000Z");
const median100WindowStart = new Date("2025-03-01T00:00:00.000Z");
const median100WindowEnd = new Date("2025-05-01T00:00:00.000Z");
const provenance =
  "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.";
const basis = {
  sourceEvidenceKind: SOURCE_EVIDENCE_KIND,
  priceBasisGrossNet: PRICE_BASIS_GROSS_NET,
  priceBasisTaxInclusion: "excluded",
  priceBasisDiscountInclusion: "excluded",
  priceBasisFreightInclusion: "excluded",
};

async function evaluateScenario(name, currentUnitPrice, extra = {}) {
  const suffix = extra.suffix ?? Date.now();
  const response = await liveRequest(
    "POST",
    `/api/commercial-intelligence/sales-prices/assessments/evaluate?facilityId=${FACILITY}`,
    {
      profileId: PROFILE,
      materialId: extra.materialId ?? MATERIAL,
      customerReference: extra.customerReference ?? null,
      comparisonDimension: extra.comparisonDimension ?? "material",
      currentUnitPrice,
      currencyCode: extra.currencyCode ?? "TRY",
      normalizedUnit: extra.normalizedUnit ?? "kg",
      windowStartUtc: (extra.windowStartUtc ?? windowStart).toISOString(),
      windowEndUtc: (extra.windowEndUtc ?? windowEnd).toISOString(),
      ...basis,
      sourceProvenance: provenance,
      idempotencyKey: extra.idempotencyKey ?? `COMMSALE1-ANALYSIS-${name}-${suffix}`,
    },
  );
  if (extra.expectReject) {
    assert(`${name} rejected`, response.status !== 201, `status=${response.status}`);
    return response;
  }
  assert(`${name} evaluate 201`, response.status === 201, response.text?.slice(0, 300));
  assertCandidateCounts(response.json.assessment, name);
  return response.json.assessment;
}

function windowObservationPrices(materialId, windowStartUtc, windowEndUtc) {
  const rows = db(`
    SELECT comparable_unit_price::text
    FROM commercial_sales_price_evidence_observations
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
  const observedPrices = windowObservationPrices(MATERIAL_BOUNDARY, windowStartUtc, windowEndUtc);
  const computedMedian = median(observedPrices);
  assert(
    `${name} reference median proof`,
    computedMedian === referenceMedian,
    `included=${JSON.stringify(observedPrices)} median=${computedMedian}`,
  );
  if (expectedRatio < 0) {
    assert(`${name} negative ratio greater than -1`, expectedRatio > -1, String(expectedRatio));
  }
}

const preBoundaryCounts = boundaryFixtureCounts();
assert("pre-setup boundary assessments zero", preBoundaryCounts.assessments === 0, `count=${preBoundaryCounts.assessments}`);
assert("pre-setup boundary observations zero", preBoundaryCounts.observations === 0, `count=${preBoundaryCounts.observations}`);

const median101SetCode = `${BOUNDARY_PREFIX}-MEDIAN-101`;
const median100SetCode = `${BOUNDARY_PREFIX}-MEDIAN-100`;
const median101ObservationIds = [
  "bb000001-03e0-4000-8000-0000000003e0",
  "bb000001-03e1-4000-8000-0000000003e1",
  "bb000001-03e2-4000-8000-0000000003e2",
  "bb000001-03e3-4000-8000-0000000003e3",
  "bb000001-03e4-4000-8000-0000000003e4",
  "bb000001-03e5-4000-8000-0000000003e5",
];
const median100ObservationIds = [
  "bb000001-03f0-4000-8000-0000000003f0",
  "bb000001-03f1-4000-8000-0000000003f1",
  "bb000001-03f2-4000-8000-0000000003f2",
  "bb000001-03f3-4000-8000-0000000003f3",
  "bb000001-03f4-4000-8000-0000000003f4",
  "bb000001-03f5-4000-8000-0000000003f5",
];

try {
  insertBoundaryObservations({
    setCode: median101SetCode,
    prices: [100, 100, 101, 101, 102, 102],
    observationIds: median101ObservationIds,
    windowStartUtc: median101WindowStart,
    windowEndUtc: median101WindowEnd,
  });
  insertBoundaryObservations({
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
    `/api/commercial-intelligence/sales-prices/profiles?facilityId=${FACILITY}`,
  );
  const pilotProfile = profiles.json?.items?.find((item) => item.id === PROFILE);
  assert("pilot maximumPositiveVarianceRatio 0.10", pilotProfile?.maximumPositiveVarianceRatio === 0.1, String(pilotProfile?.maximumPositiveVarianceRatio));
  assert("pilot requiredGrossNetBasis net", pilotProfile?.requiredGrossNetBasis === PRICE_BASIS_GROSS_NET, String(pilotProfile?.requiredGrossNetBasis));

  const highIncrease = await evaluateScenario("boundary-high-increase", 303, {
    ...median101Window,
    ...boundaryMaterial,
    idempotencyKey: `${BOUNDARY_PREFIX}-RATIO-2-0`,
  });
  assertRatioMatrixCase({
    name: "boundary ratio 2.0",
    referenceMedian: 101,
    expectedRatio: 2.0,
    ...median101Window,
  });
  assertNear("boundary high increase variance ratio", highIncrease.varianceRatio, 2.0);
  assert("boundary high increase outcome", highIncrease.assessmentOutcome === "attention", highIncrease.assessmentOutcome);
  assert("boundary high increase leading driver", highIncrease.leadingDriverCode === "sales-price-increase", highIncrease.leadingDriverCode);
  const highIncreaseDb = Number(db(`SELECT variance_ratio FROM commercial_sales_price_assessments WHERE idempotency_key = '${BOUNDARY_PREFIX}-RATIO-2-0'`));
  assertNear("boundary high increase persisted ratio", highIncreaseDb, 2.0);
  assert(
    "boundary high increase db candidate counts",
    Number(db(`
      SELECT COUNT(*) FROM commercial_sales_price_assessments
      WHERE idempotency_key = '${BOUNDARY_PREFIX}-RATIO-2-0'
        AND candidate_observation_count = comparable_observation_count + excluded_observation_count
    `)) === 1,
  );

  const deepDiscount = await evaluateScenario("boundary-deep-discount", 1, {
    ...median101Window,
    ...boundaryMaterial,
    idempotencyKey: `${BOUNDARY_PREFIX}-RATIO-DEEP-DISCOUNT`,
  });
  assertRatioMatrixCase({
    name: "boundary ratio deep discount",
    referenceMedian: 101,
    expectedRatio: -0.9900990099,
    ...median101Window,
  });
  assertNear("boundary deep discount variance ratio", deepDiscount.varianceRatio, -0.9900990099);
  assert("boundary deep discount outcome", deepDiscount.assessmentOutcome === "favorable-variance", deepDiscount.assessmentOutcome);

  const ratio150 = await evaluateScenario("boundary-ratio-1-5", 250, {
    ...median100Window,
    ...boundaryMaterial,
    idempotencyKey: `${BOUNDARY_PREFIX}-RATIO-1-5`,
  });
  assertNear("boundary ratio 1.5 variance ratio", ratio150.varianceRatio, 1.5);

  const zeroPriceKey = `${BOUNDARY_PREFIX}-ZERO-PRICE`;
  const zeroPriceAssessmentsBefore = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_assessments WHERE idempotency_key = '${zeroPriceKey}'`));
  await evaluateScenario("boundary-zero-price", 0, {
    ...median100Window,
    ...boundaryMaterial,
    idempotencyKey: zeroPriceKey,
    expectReject: true,
  });
  const zeroPriceAssessmentsAfter = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_assessments WHERE idempotency_key = '${zeroPriceKey}'`));
  assert("boundary zero price assessment rows unchanged", zeroPriceAssessmentsBefore === 0 && zeroPriceAssessmentsAfter === 0, `before=${zeroPriceAssessmentsBefore} after=${zeroPriceAssessmentsAfter}`);

  const withinRange = await evaluateScenario("within-range", 101);
  assert("within-range outcome", withinRange.assessmentOutcome === "within-reference-range", withinRange.assessmentOutcome);
  assert("within-range comparable count", withinRange.comparableObservationCount >= 6, String(withinRange.comparableObservationCount));

  const attention = await evaluateScenario("attention", 115);
  assert("attention outcome", attention.assessmentOutcome === "attention", attention.assessmentOutcome);
  assert("attention leading driver", attention.leadingDriverCode === "sales-price-increase", attention.leadingDriverCode);

  const favorable = await evaluateScenario("favorable", 90);
  assert("favorable outcome", favorable.assessmentOutcome === "favorable-variance", favorable.assessmentOutcome);

  const insufficientWindowStart = new Date(eventTime);
  insufficientWindowStart.setUTCDate(insufficientWindowStart.getUTCDate() - 31);
  const insufficientWindowEnd = new Date(eventTime);
  insufficientWindowEnd.setUTCDate(insufficientWindowEnd.getUTCDate() - 19);
  const insufficient = await evaluateScenario("insufficient", 100, {
    windowStartUtc: insufficientWindowStart,
    windowEndUtc: insufficientWindowEnd,
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
  const postBoundaryCounts = boundaryFixtureCounts();
  assert("post-cleanup boundary assessments zero", postBoundaryCounts.assessments === 0, `count=${postBoundaryCounts.assessments}`);
  assert("post-cleanup boundary drivers zero", postBoundaryCounts.drivers === 0, `count=${postBoundaryCounts.drivers}`);
  assert("post-cleanup boundary observations zero", postBoundaryCounts.observations === 0, `count=${postBoundaryCounts.observations}`);
  assert("post-cleanup orphan drivers zero", postBoundaryCounts.orphanDrivers === 0, `count=${postBoundaryCounts.orphanDrivers}`);
}

console.log("COMMERCIAL-SALES-PRICE-1 price-analysis verification passed.");

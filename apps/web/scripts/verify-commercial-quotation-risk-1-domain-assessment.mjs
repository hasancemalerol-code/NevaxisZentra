#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL_BOUNDARY = "bb000001-0281-4000-8000-000000000281";
const PROFILE = "bb000001-0280-4000-8000-000000000280";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const BOUNDARY_PREFIX = "COMMQUOTE1-BOUNDARY";
const CLOSURE_PREFIX = "COMMQUOTE1-CLOSURE";
const SOURCE_EVIDENCE_KIND = "manual-development-simulator";
const QUOTATION_SOURCE_KIND = "manual-development-simulator";
const PRICE_BASIS_GROSS_NET = "net";
const WINDOW_START = "2026-02-01T00:00:00.000Z";
const WINDOW_END = "2026-06-15T12:00:00.000Z";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const QUOTATION_SOURCE_FILES = [
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskEvaluator.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskSalesEvidenceReader.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskRepository.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskModels.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskEntities.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskEntityConfigurations.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskIdempotencyFingerprint.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskContracts.cs",
  "packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialQuotationRiskSeed.cs",
  "apps/api/CommercialIntelligence/CommercialQuotationRiskEndpointRouteBuilderExtensions.cs",
];

const PILOT_SCENARIOS = [
  {
    id: "bb000001-02a1-4000-8000-0000000002a1",
    name: "within-threshold",
    outcome: "within-threshold",
    leadingDriver: "within-profile",
    quotedPrice: 100,
    referenceMedian: 100,
    varianceRatio: 0,
    comparableCount: 6,
  },
  {
    id: "bb000001-02a2-4000-8000-0000000002a2",
    name: "below-reference",
    outcome: "attention",
    leadingDriver: "quoted-price-below-reference",
    quotedPrice: 89,
    referenceMedian: 100,
    varianceRatio: -0.11,
    comparableCount: 6,
  },
  {
    id: "bb000001-02a3-4000-8000-0000000002a3",
    name: "above-reference",
    outcome: "attention",
    leadingDriver: "quoted-price-above-reference",
    quotedPrice: 116,
    referenceMedian: 100,
    varianceRatio: 0.16,
    comparableCount: 6,
  },
  {
    id: "bb000001-02a4-4000-8000-0000000002a4",
    name: "expiring-soon",
    outcome: "attention",
    leadingDriver: "quotation-expiring-soon",
    quotedPrice: 100,
    referenceMedian: 100,
    varianceRatio: 0,
    comparableCount: 6,
  },
  {
    id: "bb000001-02a5-4000-8000-0000000002a5",
    name: "expired",
    outcome: "attention",
    leadingDriver: "quotation-expired",
    quotedPrice: 100,
    referenceMedian: 100,
    varianceRatio: 0,
    comparableCount: 6,
  },
  {
    id: "bb000001-02a6-4000-8000-0000000002a6",
    name: "payment-exceeded",
    outcome: "attention",
    leadingDriver: "payment-term-exceeded",
    quotedPrice: 100,
    referenceMedian: 100,
    varianceRatio: 0,
    paymentTermDays: 91,
    comparableCount: 6,
  },
  {
    id: "bb000001-02a7-4000-8000-0000000002a7",
    name: "insufficient-evidence",
    outcome: "insufficient-data",
    leadingDriver: "insufficient-comparable-sales-evidence",
    quotedPrice: 100,
    comparableCount: 3,
  },
  {
    id: "bb000001-02a8-4000-8000-0000000002a8",
    name: "source-conflict",
    outcome: "source-conflict",
    leadingDriver: "source-conflict",
    quotedPrice: 100,
    comparableCount: 6,
  },
];

function fail(message) {
  console.error(`COMMERCIAL-QUOTATION-RISK-1 domain-assessment verification failed: ${message}`);
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
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
    "X-NevaxisFactOS-Tenant": actor.tenant,
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

function signedVarianceRatio(quoted, referenceMedian) {
  if (!referenceMedian) return null;
  if (referenceMedian === 0) return null;
  return (quoted - referenceMedian) / referenceMedian;
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
    DELETE FROM commercial_quotation_risk_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_quotation_risk_assessments
      WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
         OR idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_quotation_risk_assessments
    WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
       OR idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_quotation_risk_evidence_observations
    WHERE ingest_idempotency_key LIKE '${BOUNDARY_PREFIX}%'
       OR ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_sales_price_evidence_observations
    WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%';
  `);
}

function boundaryFixtureCounts() {
  return {
    assessments: Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'`)),
    drivers: Number(db(`
      SELECT COUNT(*) FROM commercial_quotation_risk_assessment_drivers
      WHERE assessment_id IN (
        SELECT id FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
      )
    `)),
    quotationEvidence: Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_evidence_observations WHERE ingest_idempotency_key LIKE '${BOUNDARY_PREFIX}%'`)),
    salesObservations: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_evidence_observations WHERE observation_set_code LIKE '${BOUNDARY_PREFIX}%'`)),
    orphanDrivers: Number(db(`
      SELECT COUNT(*) FROM commercial_quotation_risk_assessment_drivers d
      WHERE NOT EXISTS (SELECT 1 FROM commercial_quotation_risk_assessments a WHERE a.id = d.assessment_id)
    `)),
  };
}

function insertBoundarySalesObservations({
  setCode,
  prices,
  observationIds,
  windowStartUtc,
  windowEndUtc,
  materialId = MATERIAL_BOUNDARY,
  customerReference = "COMMQUOTE1-CUST-BOUNDARY",
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
        false, 'simulated', '${setCode.toLowerCase()}-sales-${index}', 'boundary fixture', '${setCode}',
        '${eventAt}', 'boundary-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);
  }
  db(rows.join("\n"));
}

function insertBoundaryQuotationEvidence({
  evidenceId,
  idempotencyKey,
  quotedPrice,
  paymentTermDays,
  expiresAtUtc,
  customerReference = "COMMQUOTE1-CUST-BOUNDARY",
  materialId = MATERIAL_BOUNDARY,
  evidenceStatus = "simulated",
}) {
  const quotedAt = new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString();
  const receivedAt = quotedAt;
  const expiresClause = expiresAtUtc ? `'${expiresAtUtc.toISOString()}'` : "NULL";
  db(`
    INSERT INTO commercial_quotation_risk_evidence_observations (
      id, tenant_id, facility_id, quotation_reference, customer_reference, material_id, source_kind,
      evidence_status, quoted_at_utc, expires_at_utc, quoted_unit_price, currency_code, unit_of_measure,
      gross_net_basis, tax_basis, discount_basis, freight_basis, payment_term_days, source_reference,
      source_provenance, source_fingerprint, ingest_idempotency_key, ingest_idempotency_payload_fingerprint,
      received_at_utc, created_at_utc, created_by)
    VALUES (
      '${evidenceId}', '${TENANT}', '${FACILITY}', 'QTE-${idempotencyKey}', '${customerReference}', '${materialId}',
      '${QUOTATION_SOURCE_KIND}', '${evidenceStatus}', '${quotedAt}', ${expiresClause}, ${quotedPrice}, 'TRY', 'kg',
      '${PRICE_BASIS_GROSS_NET}', 'excluded', 'excluded', 'excluded', ${paymentTermDays}, 'QTE-${idempotencyKey}',
      'boundary fixture', 'commquote1-boundary-${evidenceId.replaceAll("-", "")}', '${idempotencyKey}', '${idempotencyKey}-fp',
      '${receivedAt}', '${receivedAt}', 'boundary-verifier')
    ON CONFLICT (id) DO NOTHING;
  `);
}

function windowSalesPrices(materialId, windowStartUtc, windowEndUtc, setCode) {
  const rows = db(`
    SELECT comparable_unit_price::text
    FROM commercial_sales_price_evidence_observations
    WHERE tenant_id = '${TENANT}'
      AND facility_id = '${FACILITY}'
      AND material_id = '${materialId}'
      AND observation_set_code = '${setCode}'
      AND commercial_event_at_utc >= '${windowStartUtc.toISOString()}'
      AND commercial_event_at_utc <= '${windowEndUtc.toISOString()}'
    ORDER BY commercial_event_at_utc
  `);
  return rows.split("\n").filter(Boolean).map((value) => Number(value));
}

console.log("Running COMMERCIAL-QUOTATION-RISK-1 domain-assessment verifier.");

const evaluator = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskEvaluator.cs");
const reader = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskSalesEvidenceReader.cs");
const constants = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
const seed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialQuotationRiskSeed.cs");

assert("median helper present", evaluator.includes("ComputeMedian"));
assert("signed variance helper present", evaluator.includes("ComputeSignedPriceVarianceRatio"));
assert("default min observations 6", constants.includes("DefaultMinimumComparableObservations = 6"));
assert("default payment term max 90", constants.includes("DefaultQuotationRiskMaximumPaymentTermDays = 90"));
assert("default expiry warning 7", constants.includes("DefaultQuotationRiskExpiryWarningDays = 7"));
assert("compatibility reader present", reader.includes("EvaluateCompatibility"));
assert("seed bootstrap present", seed.includes("EnsureCommercialQuotationRiskPilotRecordsAsync"));
assert("seed within-threshold fixture present", seed.includes("COMMQUOTE1-SET-WITHIN"));

for (const relativePath of QUOTATION_SOURCE_FILES) {
  const source = readRequired(relativePath);
  forbidText(source, "recommendation", relativePath);
  forbidText(source, "guaranteed savings", relativePath);
  forbidText(source, "decision", relativePath);
}

const prices = [100, 100, 100, 100, 100, 100];
const referenceMedian = median(prices);
assert("median calculation", referenceMedian === 100, `expected 100 got ${referenceMedian}`);
const belowRatio = signedVarianceRatio(89, referenceMedian);
const aboveRatio = signedVarianceRatio(116, referenceMedian);
assertNear("signed variance below reference", belowRatio, -0.11);
assertNear("signed variance above reference", aboveRatio, 0.16);
assert("zero median guard", signedVarianceRatio(10, 0) === null);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const profileVersion = Number(db(`SELECT version FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`));
assert("pilot profile version present", profileVersion >= 1, String(profileVersion));

const provenance =
  "Deterministic local Development quotation-risk evidence — simulated, non-production, not real customer, quotation, invoice, order, or market evidence.";

async function evaluateBoundary({
  name,
  evidenceId,
  idempotencyKey,
  expectReject = false,
}) {
  const response = await liveRequest(
    "POST",
    `/api/commercial-intelligence/quotation-risks/assessments/evaluate?facilityId=${FACILITY}`,
    {
      profileId: PROFILE,
      expectedProfileVersion: profileVersion,
      evidenceObservationId: evidenceId,
      referenceWindowStartUtc: WINDOW_START,
      referenceWindowEndUtc: WINDOW_END,
      sourceProvenance: provenance,
      idempotencyKey,
    },
  );
  forbidText(response.text ?? "", "recommendation", `${name} response`);
  forbidText(response.text ?? "", "decision", `${name} response`);
  if (expectReject) {
    assert(`${name} rejected`, response.status !== 201, `status=${response.status}`);
    return response;
  }
  assert(`${name} evaluate 201`, response.status === 201, response.text?.slice(0, 300));
  const assessment = response.json?.assessment ?? response.json;
  assertCandidateCounts(assessment, name);
  return assessment;
}

function assertDbReconciliation(idempotencyKey, assessment) {
  const row = db(`
    SELECT outcome, leading_driver_code, signed_price_variance_ratio::text, reference_median_unit_price::text,
           candidate_observation_count::text, comparable_observation_count::text, excluded_observation_count::text
    FROM commercial_quotation_risk_assessments
    WHERE idempotency_key = '${idempotencyKey}'
  `);
  const [outcome, leadingDriver, ratio, medianValue, candidate, comparable, excluded] = row.split("|");
  assert(`db reconciliation outcome ${idempotencyKey}`, outcome === assessment.outcome, `${outcome} vs ${assessment.outcome}`);
  assert(`db reconciliation leading driver ${idempotencyKey}`, leadingDriver === assessment.leadingDriverCode, `${leadingDriver} vs ${assessment.leadingDriverCode}`);
  if (assessment.signedPriceVarianceRatio !== null && assessment.signedPriceVarianceRatio !== undefined) {
    assertNear(`db reconciliation variance ${idempotencyKey}`, Number(ratio), assessment.signedPriceVarianceRatio);
  }
  if (assessment.referenceMedianUnitPrice !== null && assessment.referenceMedianUnitPrice !== undefined) {
    assertNear(`db reconciliation median ${idempotencyKey}`, Number(medianValue), assessment.referenceMedianUnitPrice);
  }
  assert(`db reconciliation candidate counts ${idempotencyKey}`, candidate === String(assessment.candidateObservationCount));
  assert(`db reconciliation comparable counts ${idempotencyKey}`, comparable === String(assessment.comparableObservationCount));
  assert(`db reconciliation excluded counts ${idempotencyKey}`, excluded === String(assessment.excludedObservationCount));
  assert(
    `db reconciliation denominator honesty ${idempotencyKey}`,
    Number(candidate) === Number(comparable) + Number(excluded),
    `${candidate} != ${comparable} + ${excluded}`,
  );
}

const preBoundaryCounts = boundaryFixtureCounts();
assert("pre-setup boundary assessments zero", preBoundaryCounts.assessments === 0, `count=${preBoundaryCounts.assessments}`);
assert("pre-setup boundary quotation evidence zero", preBoundaryCounts.quotationEvidence === 0, `count=${preBoundaryCounts.quotationEvidence}`);

try {
  for (const scenario of PILOT_SCENARIOS) {
    const response = await liveRequest(
      "GET",
      `/api/commercial-intelligence/quotation-risks/assessments/${scenario.id}?facilityId=${FACILITY}`,
    );
    assert(`${scenario.name} pilot GET 200`, response.status === 200, response.text?.slice(0, 200));
    forbidText(response.text ?? "", "recommendation", `${scenario.name} response`);
    forbidText(response.text ?? "", "decision", `${scenario.name} response`);
    const assessment = response.json;
    assert(`${scenario.name} outcome`, assessment.outcome === scenario.outcome, assessment.outcome);
    assert(`${scenario.name} leading driver`, assessment.leadingDriverCode === scenario.leadingDriver, assessment.leadingDriverCode);
    assertCandidateCounts(assessment, scenario.name);
    if (scenario.referenceMedian !== undefined) {
      assertNear(`${scenario.name} reference median`, assessment.referenceMedianUnitPrice, scenario.referenceMedian);
    }
    if (scenario.varianceRatio !== undefined) {
      assertNear(`${scenario.name} signed variance ratio`, assessment.signedPriceVarianceRatio, scenario.varianceRatio);
      const proofRatio = signedVarianceRatio(scenario.quotedPrice, scenario.referenceMedian);
      assertNear(`${scenario.name} variance math proof`, assessment.signedPriceVarianceRatio, proofRatio);
    }
    if (scenario.paymentTermDays !== undefined) {
      assert(`${scenario.name} payment term days`, assessment.paymentTermDays === scenario.paymentTermDays, String(assessment.paymentTermDays));
    }
    assert(`${scenario.name} comparable count`, assessment.comparableObservationCount === scenario.comparableCount, String(assessment.comparableObservationCount));
    assert(`${scenario.name} leading driver flagged`, assessment.drivers?.some((driver) => driver.isLeadingDriver));
    const dbOutcome = db(`SELECT outcome FROM commercial_quotation_risk_assessments WHERE id = '${scenario.id}'`);
    assert(`${scenario.name} api/db outcome reconciliation`, dbOutcome === assessment.outcome, dbOutcome);
  }

  const median100SetCode = `${BOUNDARY_PREFIX}-MEDIAN-100`;
  const median100ObservationIds = [
    "bb000001-03a0-4000-8000-0000000003a0",
    "bb000001-03a1-4000-8000-0000000003a1",
    "bb000001-03a2-4000-8000-0000000003a2",
    "bb000001-03a3-4000-8000-0000000003a3",
    "bb000001-03a4-4000-8000-0000000003a4",
    "bb000001-03a5-4000-8000-0000000003a5",
  ];
  const windowStart = new Date(WINDOW_START);
  const windowEnd = new Date(WINDOW_END);

  insertBoundarySalesObservations({
    setCode: median100SetCode,
    prices: [100, 100, 100, 100, 100, 100],
    observationIds: median100ObservationIds,
    windowStartUtc: windowStart,
    windowEndUtc: windowEnd,
  });

  const observedPrices = windowSalesPrices(MATERIAL_BOUNDARY, windowStart, windowEnd, median100SetCode);
  const computedMedian = median(observedPrices);
  assert("boundary median-100 proof", computedMedian === 100, `included=${JSON.stringify(observedPrices)} median=${computedMedian}`);

  const profiles = await liveRequest("GET", `/api/commercial-intelligence/quotation-risks/profiles?facilityId=${FACILITY}`);
  const pilotProfile = profiles.json?.items?.find((item) => item.id === PROFILE);
  assert("pilot maximumNegativePriceVarianceRatio 0.10", pilotProfile?.maximumNegativePriceVarianceRatio === 0.1, String(pilotProfile?.maximumNegativePriceVarianceRatio));
  assert("pilot maximumPositivePriceVarianceRatio 0.15", pilotProfile?.maximumPositivePriceVarianceRatio === 0.15, String(pilotProfile?.maximumPositivePriceVarianceRatio));
  assert("pilot maximumPaymentTermDays 90", pilotProfile?.maximumPaymentTermDays === 90, String(pilotProfile?.maximumPaymentTermDays));
  assert("pilot expiryWarningDays 7", pilotProfile?.expiryWarningDays === 7, String(pilotProfile?.expiryWarningDays));

  const now = new Date();
  const expiresIn30Days = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000);
  const expiresIn7Days = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);
  const expiresIn8Days = new Date(now.getTime() + 9 * 24 * 60 * 60 * 1000);
  const expiredYesterday = new Date(now.getTime() - 24 * 60 * 60 * 1000);

  const boundaryCases = [
    {
      name: "price-below-threshold",
      evidenceId: "bb000001-03b0-4000-8000-0000000003b0",
      key: `${BOUNDARY_PREFIX}-PRICE-BELOW`,
      quotedPrice: 89,
      expectedOutcome: "attention",
      expectedDriver: "quoted-price-below-reference",
      expectedRatio: -0.11,
    },
    {
      name: "price-at-negative-boundary",
      evidenceId: "bb000001-03b1-4000-8000-0000000003b1",
      key: `${BOUNDARY_PREFIX}-PRICE-NEG-BOUNDARY`,
      quotedPrice: 90,
      expectedOutcome: "within-threshold",
      expectedDriver: "within-profile",
      expectedRatio: -0.1,
    },
    {
      name: "price-above-threshold",
      evidenceId: "bb000001-03b2-4000-8000-0000000003b2",
      key: `${BOUNDARY_PREFIX}-PRICE-ABOVE`,
      quotedPrice: 116,
      expectedOutcome: "attention",
      expectedDriver: "quoted-price-above-reference",
      expectedRatio: 0.16,
    },
    {
      name: "price-at-positive-boundary",
      evidenceId: "bb000001-03b3-4000-8000-0000000003b3",
      key: `${BOUNDARY_PREFIX}-PRICE-POS-BOUNDARY`,
      quotedPrice: 115,
      expectedOutcome: "within-threshold",
      expectedDriver: "within-profile",
      expectedRatio: 0.15,
    },
    {
      name: "payment-term-at-max",
      evidenceId: "bb000001-03b4-4000-8000-0000000003b4",
      key: `${BOUNDARY_PREFIX}-PAYMENT-AT-MAX`,
      paymentTermDays: 90,
      expectedOutcome: "within-threshold",
      expectedDriver: "within-profile",
    },
    {
      name: "payment-term-exceeded",
      evidenceId: "bb000001-03b5-4000-8000-0000000003b5",
      key: `${BOUNDARY_PREFIX}-PAYMENT-EXCEEDED`,
      paymentTermDays: 91,
      expectedOutcome: "attention",
      expectedDriver: "payment-term-exceeded",
    },
    {
      name: "expiry-warning-boundary",
      evidenceId: "bb000001-03b6-4000-8000-0000000003b6",
      key: `${BOUNDARY_PREFIX}-EXPIRY-WARNING`,
      expiresAtUtc: expiresIn7Days,
      expectedOutcome: "attention",
      expectedDriver: "quotation-expiring-soon",
    },
    {
      name: "expiry-outside-warning",
      evidenceId: "bb000001-03b7-4000-8000-0000000003b7",
      key: `${BOUNDARY_PREFIX}-EXPIRY-OUTSIDE`,
      expiresAtUtc: expiresIn8Days,
      expectedOutcome: "within-threshold",
      expectedDriver: "within-profile",
    },
    {
      name: "expiry-expired",
      evidenceId: "bb000001-03b8-4000-8000-0000000003b8",
      key: `${BOUNDARY_PREFIX}-EXPIRY-EXPIRED`,
      expiresAtUtc: expiredYesterday,
      expectedOutcome: "attention",
      expectedDriver: "quotation-expired",
    },
  ];

  for (const boundaryCase of boundaryCases) {
    insertBoundaryQuotationEvidence({
      evidenceId: boundaryCase.evidenceId,
      idempotencyKey: boundaryCase.key,
      quotedPrice: boundaryCase.quotedPrice ?? 100,
      paymentTermDays: boundaryCase.paymentTermDays ?? 30,
      expiresAtUtc: boundaryCase.expiresAtUtc ?? expiresIn30Days,
    });

    const assessment = await evaluateBoundary({
      name: boundaryCase.name,
      evidenceId: boundaryCase.evidenceId,
      idempotencyKey: boundaryCase.key,
      quotedPrice: boundaryCase.quotedPrice,
      paymentTermDays: boundaryCase.paymentTermDays,
      expiresAtUtc: boundaryCase.expiresAtUtc,
    });

    assert(`${boundaryCase.name} outcome`, assessment.outcome === boundaryCase.expectedOutcome, assessment.outcome);
    assert(`${boundaryCase.name} leading driver`, assessment.leadingDriverCode === boundaryCase.expectedDriver, assessment.leadingDriverCode);
    if (boundaryCase.expectedRatio !== undefined) {
      assertNear(`${boundaryCase.name} variance ratio`, assessment.signedPriceVarianceRatio, boundaryCase.expectedRatio);
      assertNear(`${boundaryCase.name} variance math proof`, assessment.signedPriceVarianceRatio, signedVarianceRatio(boundaryCase.quotedPrice ?? 100, 100));
    }
    assertDbReconciliation(boundaryCase.key, assessment);
  }
} finally {
  removeBoundaryFixtures();
  const postBoundaryCounts = boundaryFixtureCounts();
  assert("post-cleanup boundary assessments zero", postBoundaryCounts.assessments === 0, `count=${postBoundaryCounts.assessments}`);
  assert("post-cleanup boundary drivers zero", postBoundaryCounts.drivers === 0, `count=${postBoundaryCounts.drivers}`);
  assert("post-cleanup boundary quotation evidence zero", postBoundaryCounts.quotationEvidence === 0, `count=${postBoundaryCounts.quotationEvidence}`);
  assert("post-cleanup boundary sales observations zero", postBoundaryCounts.salesObservations === 0, `count=${postBoundaryCounts.salesObservations}`);
  assert("post-cleanup orphan drivers zero", postBoundaryCounts.orphanDrivers === 0, `count=${postBoundaryCounts.orphanDrivers}`);
}

console.log("COMMERCIAL-QUOTATION-RISK-1 domain-assessment verification passed.");

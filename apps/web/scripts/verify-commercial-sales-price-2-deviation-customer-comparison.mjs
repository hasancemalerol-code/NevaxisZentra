#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const SCENARIO_MATERIAL = "bb000001-00b4-4000-8000-0000000000b4";
const BOUNDARY_MATERIAL = "bb000001-00c4-4000-8000-0000000000c4";
const PROFILE = "bb000001-0270-4000-8000-000000000270";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const CLOSURE_PREFIX = "COMMSALE2-CLOSURE";
const BOUNDARY_PREFIX = "COMMSALE2-BOUNDARY";
const SOURCE_EVIDENCE_KIND = "manual-development-simulator";
const PRICE_BASIS_GROSS_NET = "net";
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const REF_START = "2025-09-01T00:00:00.000Z";
const REF_END = "2025-09-30T23:59:59.000Z";
const CUR_START = "2025-10-01T00:00:00.000Z";
const CUR_END = "2025-10-31T23:59:59.000Z";
const BOUNDARY_REF_101_START = "2025-06-01T00:00:00.000Z";
const BOUNDARY_REF_101_END = "2025-06-30T23:59:59.000Z";
const BOUNDARY_CUR_START = "2025-07-01T00:00:00.000Z";
const BOUNDARY_CUR_END = "2025-07-31T23:59:59.000Z";
const BOUNDARY_REF_100_START = "2025-03-01T00:00:00.000Z";
const BOUNDARY_REF_100_END = "2025-03-31T23:59:59.000Z";
const PILOT_RUNS = {
  stable: "bb000001-02f0-4000-8000-0000000002f0",
  decrease: "bb000001-02f1-4000-8000-0000000002f1",
  positive: "bb000001-02f2-4000-8000-0000000002f2",
  spread: "bb000001-02f3-4000-8000-0000000002f3",
  tie: "bb000001-02f4-4000-8000-0000000002f4",
  insufficient: "bb000001-02f5-4000-8000-0000000002f5",
  conflict: "bb000001-02f6-4000-8000-0000000002f6",
};

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-2 deviation/customer-comparison verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function assertNear(label, actual, expected, tolerance = 0.0001) {
  if (expected === null || expected === undefined) {
    assert(label, actual === null || actual === undefined, `expected null got ${actual}`);
    return;
  }
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

const FIXTURE_PREFIXES = [CLOSURE_PREFIX, BOUNDARY_PREFIX];

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function dbExec(sql) {
  const result = spawnSync(
    "docker",
    [
      "exec",
      "crevafactos-local-postgres-1",
      "psql",
      "-U",
      "crevafactos",
      "-d",
      "crevafactos_local",
      "-v",
      "ON_ERROR_STOP=1",
      "-c",
      sql,
    ],
    { encoding: "utf8" },
  );
  if (result.status !== 0) {
    fail(`db exec failed: ${result.stderr || result.stdout || sql}`);
  }
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

function signedVarianceRatio(currentMedian, referenceMedian) {
  if (!referenceMedian || referenceMedian === 0) return null;
  return (currentMedian - referenceMedian) / referenceMedian;
}

function windowsOverlap(currentStart, currentEnd, referenceStart, referenceEnd) {
  return new Date(currentStart) < new Date(referenceEnd) && new Date(referenceStart) < new Date(currentEnd);
}

function assertCandidateCounts(run, label) {
  assert(
    `${label} candidate = comparable + excluded`,
    run.candidateObservationCount === run.comparableObservationCount + run.excludedObservationCount,
    `candidate=${run.candidateObservationCount} comparable=${run.comparableObservationCount} excluded=${run.excludedObservationCount}`,
  );
}

function removeFixturePrefix(prefix) {
  dbExec(`
    DELETE FROM commercial_sales_price_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    )
  `);
  dbExec(`
    DELETE FROM commercial_sales_price_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    )
  `);
  dbExec(`DELETE FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${prefix}%'`);
  dbExec(`DELETE FROM commercial_sales_price_evidence_observations WHERE observation_set_code LIKE '${prefix}%'`);
}

function cleanupVerifierFixtures() {
  for (const prefix of FIXTURE_PREFIXES) {
    removeFixturePrefix(prefix);
  }
}

function fixtureCounts() {
  const runs = Number(db(`
    SELECT COUNT(*)
    FROM commercial_sales_price_comparison_runs
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR idempotency_key LIKE '${BOUNDARY_PREFIX}%'
  `));
  const observations = Number(db(`
    SELECT COUNT(*)
    FROM commercial_sales_price_evidence_observations
    WHERE observation_set_code LIKE '${CLOSURE_PREFIX}%'
       OR observation_set_code LIKE '${BOUNDARY_PREFIX}%'
  `));
  const orphanResults = Number(db(`
    SELECT COUNT(*)
    FROM commercial_sales_price_comparison_results r
    LEFT JOIN commercial_sales_price_comparison_runs cr ON cr.id = r.comparison_run_id
    WHERE cr.id IS NULL
  `));
  const orphanDrivers = Number(db(`
    SELECT COUNT(*)
    FROM commercial_sales_price_comparison_drivers d
    LEFT JOIN commercial_sales_price_comparison_runs cr ON cr.id = d.comparison_run_id
    WHERE cr.id IS NULL
  `));
  return { runs, observations, orphanResults, orphanDrivers };
}

function assertFixtureIsolation(label) {
  const counts = fixtureCounts();
  assert(`${label} closure/boundary runs zero`, counts.runs === 0, `runs=${counts.runs}`);
  assert(`${label} closure/boundary observations zero`, counts.observations === 0, `observations=${counts.observations}`);
  assert(`${label} orphan results zero`, counts.orphanResults === 0, `count=${counts.orphanResults}`);
  assert(`${label} orphan drivers zero`, counts.orphanDrivers === 0, `count=${counts.orphanDrivers}`);
}

function insertObservations({
  setCode,
  materialId,
  prices,
  observationIds,
  windowStartUtc,
  windowEndUtc,
  customerReference = "CUST-FIX",
  currencyCode = "TRY",
  normalizedUnit = "kg",
  grossNet = PRICE_BASIS_GROSS_NET,
}) {
  const rows = [];
  const startMs = new Date(windowStartUtc).getTime();
  const endMs = new Date(windowEndUtc).getTime();
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
        'SO-${setCode}-${index + 1}', 'LINE-001', '${eventAt}', '${eventAt}', 10, '${normalizedUnit}', 10, '${normalizedUnit}',
        ${prices[index]}, ${prices[index]}, '${currencyCode}', '${grossNet}', 'excluded', 'excluded', 'excluded',
        false, 'simulated', '${setCode.toLowerCase()}-${index}', 'closure fixture', '${setCode}',
        '${eventAt}', 'closure-verifier')
      ON CONFLICT (id) DO UPDATE SET
        material_id = EXCLUDED.material_id,
        customer_reference = EXCLUDED.customer_reference,
        commercial_event_at_utc = EXCLUDED.commercial_event_at_utc,
        received_at_utc = EXCLUDED.received_at_utc,
        original_unit_price = EXCLUDED.original_unit_price,
        comparable_unit_price = EXCLUDED.comparable_unit_price,
        observation_set_code = EXCLUDED.observation_set_code,
        source_fingerprint = EXCLUDED.source_fingerprint;
    `);
  }
  db(rows.join("\n"));
}

function loadRunFromDb(runId) {
  const line = db(`
    SELECT comparison_outcome,
           COALESCE(signed_variance_ratio::text, ''),
           COALESCE(reference_median_unit_price::text, ''),
           COALESCE(current_median_unit_price::text, ''),
           COALESCE(leading_driver_code, ''),
           candidate_observation_count,
           comparable_observation_count,
           excluded_observation_count,
           COALESCE(customer_spread_ratio::text, '')
    FROM commercial_sales_price_comparison_runs
    WHERE id = '${runId}'
  `);
  const parts = line.split("|");
  return {
    comparisonOutcome: parts[0],
    signedVarianceRatio: parts[1] === "" ? null : Number(parts[1]),
    referenceMedianUnitPrice: parts[2] === "" ? null : Number(parts[2]),
    currentMedianUnitPrice: parts[3] === "" ? null : Number(parts[3]),
    leadingDriverCode: parts[4] === "" ? null : parts[4],
    candidateObservationCount: Number(parts[5]),
    comparableObservationCount: Number(parts[6]),
    excludedObservationCount: Number(parts[7]),
    customerSpreadRatio: parts[8] === "" ? null : Number(parts[8]),
  };
}

function loadCustomerResultsFromDb(runId) {
  const rows = db(`
    SELECT customer_reference,
           comparable_observation_count,
           COALESCE(relative_price_position_ratio::text, ''),
           price_rank,
           is_tied,
           position_state
    FROM commercial_sales_price_comparison_results
    WHERE comparison_run_id = '${runId}'
    ORDER BY customer_reference
  `);
  if (!rows) return [];
  return rows.split("\n").filter(Boolean).map((row) => {
    const [customerReference, comparableObservationCount, relativePricePositionRatio, priceRank, isTied, positionState] = row.split("|");
    return {
      customerReference,
      comparableObservationCount: Number(comparableObservationCount),
      relativePricePositionRatio: relativePricePositionRatio === "" ? null : Number(relativePricePositionRatio),
      priceRank: Number(priceRank),
      isTied: isTied === "t",
      positionState,
    };
  });
}

function reconcileRun(apiRun, label) {
  const dbRun = loadRunFromDb(apiRun.id);
  assert(`${label} db outcome`, dbRun.comparisonOutcome === apiRun.comparisonOutcome, `${dbRun.comparisonOutcome} vs ${apiRun.comparisonOutcome}`);
  assert(`${label} db leading driver`, dbRun.leadingDriverCode === apiRun.leadingDriverCode, `${dbRun.leadingDriverCode} vs ${apiRun.leadingDriverCode}`);
  assertNear(`${label} db signed variance`, dbRun.signedVarianceRatio, apiRun.signedVarianceRatio);
  assert(`${label} db candidate count`, dbRun.candidateObservationCount === apiRun.candidateObservationCount);
  assert(`${label} db comparable count`, dbRun.comparableObservationCount === apiRun.comparableObservationCount);
  assert(`${label} db excluded count`, dbRun.excludedObservationCount === apiRun.excludedObservationCount);
  assertCandidateCounts(dbRun, `${label} db`);
  assertCandidateCounts(apiRun, `${label} api`);
}

console.log("Running COMMERCIAL-SALES-PRICE-2 deviation/customer-comparison verifier.");

const evaluator = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialSalesPriceComparisonEvaluator.cs");
const operations = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialSalesPriceComparisonOperations.cs");
const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialSalesPriceEndpointRouteBuilderExtensions.cs");
const seed = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotCommercialSalesPriceSeed.cs");
const comparisonSeed = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotCommercialSalesPriceComparisonSeed.cs");

assert("comparison evaluator present", evaluator.includes("EvaluateTemporalDeviation"));
assert("customer position evaluator present", evaluator.includes("EvaluateCustomerPricePosition"));
assert("non-overlap validation", operations.includes("must not overlap"));
assert("comparison endpoints present", endpoints.includes("/comparisons"));
assert("sales-price-decrease driver", readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialIntelligenceSalesPriceContracts.cs").includes("sales-price-decrease"));
assert("positive-price-variance outcome", evaluator.includes("PositivePriceVariance") || evaluator.includes("positive-price-variance"));
forbidText(evaluator, "guaranteed savings", "evaluator");
forbidText(evaluator, "recommended customer", "evaluator");
forbidText(seed, "recommended customer", "seed");
assert("comparison seed stable pilot", comparisonSeed.includes("COMMSALE2-CMP-PILOT-STABLE"));
assert("comparison seed seven scenarios", comparisonSeed.includes("commsale2-pilot-source-conflict"));
assert("position ratio uses highest median denominator", evaluator.includes("(ranked.Median - lowestMedian) / highestMedian"));
assert("spread ratio uses lowest median denominator", evaluator.includes("(highestMedian - lowestMedian) / lowestMedian"));

assertNear("median calculation", median([100, 100, 101, 101, 102, 102]), 101);
assertNear("signed variance ratio >1", signedVarianceRatio(303, 101), 2.0);
assert("non-overlapping windows", !windowsOverlap(CUR_START, CUR_END, REF_START, REF_END));

async function main() {
  cleanupVerifierFixtures();
  assertFixtureIsolation("pre-run");

  try {
  const ready = await fetch(`${BASE}/health/ready`);
  assert("API ready", ready.status === 200, `status=${ready.status}`);

const provenance =
  "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.";
const basis = {
  sourceEvidenceKind: SOURCE_EVIDENCE_KIND,
  priceBasisGrossNet: PRICE_BASIS_GROSS_NET,
  priceBasisTaxInclusion: "excluded",
  priceBasisDiscountInclusion: "excluded",
  priceBasisFreightInclusion: "excluded",
};

const profiles = await liveRequest("GET", `/api/commercial-intelligence/sales-prices/profiles?facilityId=${FACILITY}`);
const pilotProfile = profiles.json?.items?.find((item) => item.id === PROFILE);
assert("pilot profile present", Boolean(pilotProfile));
assert("pilot maximumPositiveVarianceRatio 0.10", pilotProfile.maximumPositiveVarianceRatio === 0.1);
assert("pilot customerSpreadThreshold 0.10", pilotProfile.customerSpreadThreshold === 0.1);
const profileVersion = pilotProfile.version;

async function getPilotRun(runId) {
  const response = await liveRequest(
    "GET",
    `/api/commercial-intelligence/sales-prices/comparisons/${runId}?facilityId=${FACILITY}`,
  );
  assert(`pilot run ${runId} 200`, response.status === 200, response.text?.slice(0, 200));
  return response.json;
}

const stablePilot = await getPilotRun(PILOT_RUNS.stable);
assert("pilot stable outcome", stablePilot.comparisonOutcome === "within-reference-range", stablePilot.comparisonOutcome);
assertNear("pilot stable signed variance", stablePilot.signedVarianceRatio, 0.05);

const decreasePilot = await getPilotRun(PILOT_RUNS.decrease);
assert("pilot decrease outcome", decreasePilot.comparisonOutcome === "attention", decreasePilot.comparisonOutcome);
assert("pilot decrease leading driver", decreasePilot.leadingDriverCode === "sales-price-decrease", decreasePilot.leadingDriverCode);
assertNear("pilot decrease signed variance", decreasePilot.signedVarianceRatio, -0.2);

const positivePilot = await getPilotRun(PILOT_RUNS.positive);
assert("pilot positive outcome", positivePilot.comparisonOutcome === "positive-price-variance", positivePilot.comparisonOutcome);
assertNear("pilot positive signed variance", positivePilot.signedVarianceRatio, 0.15);

const spreadPilot = await getPilotRun(PILOT_RUNS.spread);
assert("pilot spread outcome", spreadPilot.comparisonOutcome === "attention", spreadPilot.comparisonOutcome);
assert("pilot spread driver present", spreadPilot.drivers.some((d) => d.reasonCode === "customer-price-spread"));
assert("pilot spread customer rows", spreadPilot.customerResults.length === 2, String(spreadPilot.customerResults.length));

const tiePilot = await getPilotRun(PILOT_RUNS.tie);
const tiedPilotResults = tiePilot.customerResults.filter((r) => r.isTied);
assert("pilot tie results", tiedPilotResults.length === 2, String(tiedPilotResults.length));
assert("pilot tie position state", tiedPilotResults.every((r) => r.positionState === "tied-comparable-price"));
assert("pilot tie driver code", tiePilot.drivers.some((d) => d.reasonCode === "customer-median-tie"));
assert("pilot tie no unique lowest", !tiePilot.customerResults.some((r) => r.positionState === "lowest-comparable-price"));
assert("pilot tie same rank", tiedPilotResults[0].priceRank === tiedPilotResults[1].priceRank);

const insufficientPilot = await getPilotRun(PILOT_RUNS.insufficient);
assert("pilot insufficient outcome", insufficientPilot.comparisonOutcome === "insufficient-data", insufficientPilot.comparisonOutcome);
assert("pilot insufficient leading driver", insufficientPilot.leadingDriverCode === "insufficient-current-window", insufficientPilot.leadingDriverCode);

const conflictPilot = await getPilotRun(PILOT_RUNS.conflict);
assert("pilot source conflict outcome", conflictPilot.comparisonOutcome === "source-conflict", conflictPilot.comparisonOutcome);

const pilotRunIds = Object.values(PILOT_RUNS).join("','");
const pilotRunCount = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE id IN ('${pilotRunIds}')`));
assert("seven pilot comparison runs", pilotRunCount === 7, `count=${pilotRunCount}`);
const pilotFingerprintRows = db(`
  SELECT idempotency_key || '|' || idempotency_payload_fingerprint
  FROM commercial_sales_price_comparison_runs
  WHERE id IN ('${pilotRunIds}')
  ORDER BY idempotency_key
`);
assert("pilot fingerprints present", pilotFingerprintRows.split("\n").filter(Boolean).length === 7);

async function evaluateComparison(name, body, extra = {}) {
  const response = await liveRequest(
    "POST",
    `/api/commercial-intelligence/sales-prices/comparisons/evaluate?facilityId=${FACILITY}`,
    body,
  );
  if (extra.expectReject) {
    assert(`${name} rejected`, response.status !== 201, `status=${response.status}`);
    return response;
  }
  assert(`${name} evaluate 201`, response.status === 201, response.text?.slice(0, 300));
  return response.json.comparison;
}

function temporalBody(materialId, refStart, refEnd, curStart, curEnd, idempotencyKey) {
  return {
    profileId: PROFILE,
    expectedProfileVersion: profileVersion,
    comparisonType: "temporal-sales-price-deviation",
    materialId,
    ...basis,
    currentWindowStartUtc: curStart,
    currentWindowEndUtc: curEnd,
    referenceWindowStartUtc: refStart,
    referenceWindowEndUtc: refEnd,
    currencyCode: "TRY",
    normalizedUnit: "kg",
    sourceProvenance: provenance,
    idempotencyKey,
  };
}

function customerBody(materialId, windowStart, windowEnd, idempotencyKey) {
  return {
    profileId: PROFILE,
    expectedProfileVersion: profileVersion,
    comparisonType: "customer-price-position",
    materialId,
    ...basis,
    currentWindowStartUtc: windowStart,
    currentWindowEndUtc: windowEnd,
    currencyCode: "TRY",
    normalizedUnit: "kg",
    sourceProvenance: provenance,
    idempotencyKey,
  };
}

const stableRefIds = [
  "bb000001-0400-4000-8000-000000000400",
  "bb000001-0401-4000-8000-000000000401",
  "bb000001-0402-4000-8000-000000000402",
  "bb000001-0403-4000-8000-000000000403",
  "bb000001-0404-4000-8000-000000000404",
  "bb000001-0405-4000-8000-000000000405",
];
const stableCurIds = [
  "bb000001-0410-4000-8000-000000000410",
  "bb000001-0411-4000-8000-000000000411",
  "bb000001-0412-4000-8000-000000000412",
  "bb000001-0413-4000-8000-000000000413",
  "bb000001-0414-4000-8000-000000000414",
  "bb000001-0415-4000-8000-000000000415",
];
insertObservations({
  setCode: `${CLOSURE_PREFIX}-STABLE-REF`,
  materialId: SCENARIO_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: stableRefIds,
  windowStartUtc: REF_START,
  windowEndUtc: REF_END,
});
insertObservations({
  setCode: `${CLOSURE_PREFIX}-STABLE-CUR`,
  materialId: SCENARIO_MATERIAL,
  prices: [105, 105, 105, 105, 105, 105],
  observationIds: stableCurIds,
  windowStartUtc: CUR_START,
  windowEndUtc: CUR_END,
});
const stable = await evaluateComparison(
  "stable-temporal",
  temporalBody(SCENARIO_MATERIAL, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-STABLE`),
);
assert("stable outcome", stable.comparisonOutcome === "within-reference-range", stable.comparisonOutcome);
assertNear("stable signed variance", stable.signedVarianceRatio, 0.05);
reconcileRun(stable, "stable");

const decreaseCurIds = [
  "bb000001-0420-4000-8000-000000000420",
  "bb000001-0421-4000-8000-000000000421",
  "bb000001-0422-4000-8000-000000000422",
  "bb000001-0423-4000-8000-000000000423",
  "bb000001-0424-4000-8000-000000000424",
  "bb000001-0425-4000-8000-000000000425",
];
insertObservations({
  setCode: `${CLOSURE_PREFIX}-DECREASE-REF`,
  materialId: SCENARIO_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-0426-4000-8000-000000000426",
    "bb000001-0427-4000-8000-000000000427",
    "bb000001-0428-4000-8000-000000000428",
    "bb000001-0429-4000-8000-000000000429",
    "bb000001-042a-4000-8000-00000000042a",
    "bb000001-042b-4000-8000-00000000042b",
  ],
  windowStartUtc: "2025-08-01T00:00:00.000Z",
  windowEndUtc: "2025-08-31T23:59:59.000Z",
});
insertObservations({
  setCode: `${CLOSURE_PREFIX}-DECREASE-CUR`,
  materialId: SCENARIO_MATERIAL,
  prices: [80, 80, 80, 80, 80, 80],
  observationIds: decreaseCurIds,
  windowStartUtc: "2025-11-01T00:00:00.000Z",
  windowEndUtc: "2025-11-30T23:59:59.000Z",
});
const decrease = await evaluateComparison(
  "sales-price-decrease",
  temporalBody(
    SCENARIO_MATERIAL,
    "2025-08-01T00:00:00.000Z",
    "2025-08-31T23:59:59.000Z",
    "2025-11-01T00:00:00.000Z",
    "2025-11-30T23:59:59.000Z",
    `${CLOSURE_PREFIX}-DECREASE`,
  ),
);
assert("decrease outcome", decrease.comparisonOutcome === "attention", decrease.comparisonOutcome);
assert("decrease leading driver", decrease.leadingDriverCode === "sales-price-decrease", decrease.leadingDriverCode);
assertNear("decrease signed variance", decrease.signedVarianceRatio, -0.2);
reconcileRun(decrease, "decrease");

insertObservations({
  setCode: `${CLOSURE_PREFIX}-POSITIVE-REF`,
  materialId: SCENARIO_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-0430-4000-8000-000000000430",
    "bb000001-0431-4000-8000-000000000431",
    "bb000001-0432-4000-8000-000000000432",
    "bb000001-0433-4000-8000-000000000433",
    "bb000001-0434-4000-8000-000000000434",
    "bb000001-0435-4000-8000-000000000435",
  ],
  windowStartUtc: "2025-12-01T00:00:00.000Z",
  windowEndUtc: "2025-12-31T23:59:59.000Z",
});
insertObservations({
  setCode: `${CLOSURE_PREFIX}-POSITIVE-CUR`,
  materialId: SCENARIO_MATERIAL,
  prices: [115, 115, 115, 115, 115, 115],
  observationIds: [
    "bb000001-0440-4000-8000-000000000440",
    "bb000001-0441-4000-8000-000000000441",
    "bb000001-0442-4000-8000-000000000442",
    "bb000001-0443-4000-8000-000000000443",
    "bb000001-0444-4000-8000-000000000444",
    "bb000001-0445-4000-8000-000000000445",
  ],
  windowStartUtc: "2026-01-01T00:00:00.000Z",
  windowEndUtc: "2026-01-31T23:59:59.000Z",
});
const positiveVariance = await evaluateComparison(
  "positive-price-variance",
  temporalBody(
    SCENARIO_MATERIAL,
    "2025-12-01T00:00:00.000Z",
    "2025-12-31T23:59:59.000Z",
    "2026-01-01T00:00:00.000Z",
    "2026-01-31T23:59:59.000Z",
    `${CLOSURE_PREFIX}-POSITIVE`,
  ),
);
assert("positive variance outcome", positiveVariance.comparisonOutcome === "positive-price-variance", positiveVariance.comparisonOutcome);
forbidText(positiveVariance.comparisonOutcome, "favorable-variance", "positive variance outcome");
forbidText(JSON.stringify(positiveVariance.drivers ?? []), "profitable", "positive variance drivers");
assertNear("positive variance ratio", positiveVariance.signedVarianceRatio, 0.15);
reconcileRun(positiveVariance, "positive-variance");

const spreadCustomerA = [100, 100, 100, 100, 100, 100];
const spreadCustomerB = [130, 130, 130, 130, 130, 130];
insertObservations({
  setCode: `${CLOSURE_PREFIX}-SPREAD-A`,
  materialId: SCENARIO_MATERIAL,
  prices: spreadCustomerA,
  observationIds: [
    "bb000001-0450-4000-8000-000000000450",
    "bb000001-0451-4000-8000-000000000451",
    "bb000001-0452-4000-8000-000000000452",
    "bb000001-0453-4000-8000-000000000453",
    "bb000001-0454-4000-8000-000000000454",
    "bb000001-0455-4000-8000-000000000455",
  ],
  windowStartUtc: "2026-02-01T00:00:00.000Z",
  windowEndUtc: "2026-02-28T23:59:59.000Z",
  customerReference: "CUST-SPR-A",
});
insertObservations({
  setCode: `${CLOSURE_PREFIX}-SPREAD-B`,
  materialId: SCENARIO_MATERIAL,
  prices: spreadCustomerB,
  observationIds: [
    "bb000001-0460-4000-8000-000000000460",
    "bb000001-0461-4000-8000-000000000461",
    "bb000001-0462-4000-8000-000000000462",
    "bb000001-0463-4000-8000-000000000463",
    "bb000001-0464-4000-8000-000000000464",
    "bb000001-0465-4000-8000-000000000465",
  ],
  windowStartUtc: "2026-02-01T00:00:00.000Z",
  windowEndUtc: "2026-02-28T23:59:59.000Z",
  customerReference: "CUST-SPR-B",
});
const spread = await evaluateComparison(
  "customer-price-spread",
  customerBody(
    SCENARIO_MATERIAL,
    "2026-02-01T00:00:00.000Z",
    "2026-02-28T23:59:59.000Z",
    `${CLOSURE_PREFIX}-SPREAD`,
  ),
);
assert("spread outcome", spread.comparisonOutcome === "attention", spread.comparisonOutcome);
assert("spread driver present", spread.drivers.some((d) => d.reasonCode === "customer-price-spread"));
assert("spread customer rows", spread.customerResults.length === 2, String(spread.customerResults.length));
assertNear("spread ratio", spread.customerSpreadRatio, 0.3);
reconcileRun(spread, "spread");
const spreadDbResults = loadCustomerResultsFromDb(spread.id);
assert("spread db customer rows", spreadDbResults.length === 2);

const tiePrices = [110, 111, 112, 113, 114, 115];
insertObservations({
  setCode: `${CLOSURE_PREFIX}-TIE-A`,
  materialId: SCENARIO_MATERIAL,
  prices: tiePrices,
  observationIds: [
    "bb000001-0470-4000-8000-000000000470",
    "bb000001-0471-4000-8000-000000000471",
    "bb000001-0472-4000-8000-000000000472",
    "bb000001-0473-4000-8000-000000000473",
    "bb000001-0474-4000-8000-000000000474",
    "bb000001-0475-4000-8000-000000000475",
  ],
  windowStartUtc: "2026-03-01T00:00:00.000Z",
  windowEndUtc: "2026-03-31T23:59:59.000Z",
  customerReference: "CUST-TIE-A",
});
insertObservations({
  setCode: `${CLOSURE_PREFIX}-TIE-B`,
  materialId: SCENARIO_MATERIAL,
  prices: tiePrices,
  observationIds: [
    "bb000001-0480-4000-8000-000000000480",
    "bb000001-0481-4000-8000-000000000481",
    "bb000001-0482-4000-8000-000000000482",
    "bb000001-0483-4000-8000-000000000483",
    "bb000001-0484-4000-8000-000000000484",
    "bb000001-0485-4000-8000-000000000485",
  ],
  windowStartUtc: "2026-03-01T00:00:00.000Z",
  windowEndUtc: "2026-03-31T23:59:59.000Z",
  customerReference: "CUST-TIE-B",
});
const tie = await evaluateComparison(
  "customer-median-tie",
  customerBody(
    SCENARIO_MATERIAL,
    "2026-03-01T00:00:00.000Z",
    "2026-03-31T23:59:59.000Z",
    `${CLOSURE_PREFIX}-TIE`,
  ),
);
const tiedResults = tie.customerResults.filter((r) => r.isTied);
assert("tie results count", tiedResults.length === 2, String(tiedResults.length));
assert("tie position state", tiedResults.every((r) => r.positionState === "tied-comparable-price"));
assert("tie driver code", tie.drivers.some((d) => d.reasonCode === "customer-median-tie"));
assert("tie no unique lowest", !tie.customerResults.some((r) => r.positionState === "lowest-comparable-price"));
assert("tie same rank", tiedResults[0].priceRank === tiedResults[1].priceRank);
reconcileRun(tie, "tie");

insertObservations({
  setCode: `${CLOSURE_PREFIX}-INSUFFICIENT`,
  materialId: SCENARIO_MATERIAL,
  prices: [100, 101, 102],
  observationIds: [
    "bb000001-0490-4000-8000-000000000490",
    "bb000001-0491-4000-8000-000000000491",
    "bb000001-0492-4000-8000-000000000492",
  ],
  windowStartUtc: "2026-04-01T00:00:00.000Z",
  windowEndUtc: "2026-04-30T23:59:59.000Z",
});
const insufficient = await evaluateComparison(
  "insufficient-data",
  temporalBody(
    SCENARIO_MATERIAL,
    "2026-04-01T00:00:00.000Z",
    "2026-04-30T23:59:59.000Z",
    "2026-05-01T00:00:00.000Z",
    "2026-05-31T23:59:59.000Z",
    `${CLOSURE_PREFIX}-INSUFFICIENT`,
  ),
);
assert("insufficient outcome", insufficient.comparisonOutcome === "insufficient-data", insufficient.comparisonOutcome);
assert("insufficient no signed ratio", insufficient.signedVarianceRatio === null || insufficient.signedVarianceRatio === undefined);
assert("insufficient no reference median", insufficient.referenceMedianUnitPrice === null || insufficient.referenceMedianUnitPrice === undefined);
reconcileRun(insufficient, "insufficient");

insertObservations({
  setCode: `${CLOSURE_PREFIX}-CONFLICT-TRY`,
  materialId: SCENARIO_MATERIAL,
  prices: [100, 100, 100],
  observationIds: [
    "bb000001-04a0-4000-8000-0000000004a0",
    "bb000001-04a1-4000-8000-0000000004a1",
    "bb000001-04a2-4000-8000-0000000004a2",
  ],
  windowStartUtc: "2026-06-01T00:00:00.000Z",
  windowEndUtc: "2026-06-30T23:59:59.000Z",
  currencyCode: "TRY",
});
insertObservations({
  setCode: `${CLOSURE_PREFIX}-CONFLICT-USD`,
  materialId: SCENARIO_MATERIAL,
  prices: [100, 100, 100],
  observationIds: [
    "bb000001-04b0-4000-8000-0000000004b0",
    "bb000001-04b1-4000-8000-0000000004b1",
    "bb000001-04b2-4000-8000-0000000004b2",
  ],
  windowStartUtc: "2026-06-01T00:00:00.000Z",
  windowEndUtc: "2026-06-30T23:59:59.000Z",
  currencyCode: "USD",
});
insertObservations({
  setCode: `${CLOSURE_PREFIX}-CONFLICT-UOM`,
  materialId: SCENARIO_MATERIAL,
  prices: [100, 100, 100],
  observationIds: [
    "bb000001-04c0-4000-8000-0000000004c0",
    "bb000001-04c1-4000-8000-0000000004c1",
    "bb000001-04c2-4000-8000-0000000004c2",
  ],
  windowStartUtc: "2026-06-01T00:00:00.000Z",
  windowEndUtc: "2026-06-30T23:59:59.000Z",
  normalizedUnit: "ton",
});
const conflict = await evaluateComparison(
  "source-conflict",
  customerBody(
    SCENARIO_MATERIAL,
    "2026-06-01T00:00:00.000Z",
    "2026-06-30T23:59:59.000Z",
    `${CLOSURE_PREFIX}-CONFLICT`,
  ),
);
assert("source conflict outcome", conflict.comparisonOutcome === "source-conflict", conflict.comparisonOutcome);
assert("source conflict no healthy ratio", conflict.signedVarianceRatio === null || conflict.signedVarianceRatio === undefined);
assert("source conflict not within-range", conflict.comparisonOutcome !== "within-reference-range");
reconcileRun(conflict, "conflict");

const median101ObservationIds = [
  "bb000001-04d0-4000-8000-0000000004d0",
  "bb000001-04d1-4000-8000-0000000004d1",
  "bb000001-04d2-4000-8000-0000000004d2",
  "bb000001-04d3-4000-8000-0000000004d3",
  "bb000001-04d4-4000-8000-0000000004d4",
  "bb000001-04d5-4000-8000-0000000004d5",
];
const median100ObservationIds = [
  "bb000001-04e0-4000-8000-0000000004e0",
  "bb000001-04e1-4000-8000-0000000004e1",
  "bb000001-04e2-4000-8000-0000000004e2",
  "bb000001-04e3-4000-8000-0000000004e3",
  "bb000001-04e4-4000-8000-0000000004e4",
  "bb000001-04e5-4000-8000-0000000004e5",
];
insertObservations({
  setCode: `${BOUNDARY_PREFIX}-MEDIAN-101`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 101, 101, 102, 102],
  observationIds: median101ObservationIds,
  windowStartUtc: BOUNDARY_REF_101_START,
  windowEndUtc: BOUNDARY_REF_101_END,
});
insertObservations({
  setCode: `${BOUNDARY_PREFIX}-MEDIAN-100`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: median100ObservationIds,
  windowStartUtc: BOUNDARY_REF_100_START,
  windowEndUtc: BOUNDARY_REF_100_END,
});

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-RATIO-2-0-CUR`,
  materialId: BOUNDARY_MATERIAL,
  prices: [303, 303, 303, 303, 303, 303],
  observationIds: [
    "bb000001-0500-4000-8000-000000000500",
    "bb000001-0501-4000-8000-000000000501",
    "bb000001-0502-4000-8000-000000000502",
    "bb000001-0503-4000-8000-000000000503",
    "bb000001-0504-4000-8000-000000000504",
    "bb000001-0505-4000-8000-000000000505",
  ],
  windowStartUtc: BOUNDARY_CUR_START,
  windowEndUtc: BOUNDARY_CUR_END,
});
const ratio203 = await evaluateComparison(
  "ratio-2-0",
  temporalBody(
    BOUNDARY_MATERIAL,
    BOUNDARY_REF_101_START,
    BOUNDARY_REF_101_END,
    BOUNDARY_CUR_START,
    BOUNDARY_CUR_END,
    `${BOUNDARY_PREFIX}-RATIO-2-0`,
  ),
);
assertNear("ratio 2.0", ratio203.signedVarianceRatio, 2.0);
assert("ratio 2.0 not capped at 1", ratio203.signedVarianceRatio > 1);
reconcileRun(ratio203, "ratio-2-0");

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-RATIO-1-0-CUR`,
  materialId: BOUNDARY_MATERIAL,
  prices: [202, 202, 202, 202, 202, 202],
  observationIds: [
    "bb000001-0520-4000-8000-000000000520",
    "bb000001-0521-4000-8000-000000000521",
    "bb000001-0522-4000-8000-000000000522",
    "bb000001-0523-4000-8000-000000000523",
    "bb000001-0524-4000-8000-000000000524",
    "bb000001-0525-4000-8000-000000000525",
  ],
  windowStartUtc: "2025-08-01T00:00:00.000Z",
  windowEndUtc: "2025-08-31T23:59:59.000Z",
});
const ratio100 = await evaluateComparison(
  "ratio-1-0",
  temporalBody(
    BOUNDARY_MATERIAL,
    BOUNDARY_REF_101_START,
    BOUNDARY_REF_101_END,
    "2025-08-01T00:00:00.000Z",
    "2025-08-31T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-RATIO-1-0`,
  ),
);
assertNear("ratio 1.0", ratio100.signedVarianceRatio, 1.0);
reconcileRun(ratio100, "ratio-1-0");

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-RATIO-NEAR-MINUS-1-CUR`,
  materialId: BOUNDARY_MATERIAL,
  prices: [1, 1, 1, 1, 1, 1],
  observationIds: [
    "bb000001-0530-4000-8000-000000000530",
    "bb000001-0531-4000-8000-000000000531",
    "bb000001-0532-4000-8000-000000000532",
    "bb000001-0533-4000-8000-000000000533",
    "bb000001-0534-4000-8000-000000000534",
    "bb000001-0535-4000-8000-000000000535",
  ],
  windowStartUtc: "2025-10-01T00:00:00.000Z",
  windowEndUtc: "2025-10-31T23:59:59.000Z",
});
const ratioNearMinusOne = await evaluateComparison(
  "ratio-near-minus-one",
  temporalBody(
    BOUNDARY_MATERIAL,
    BOUNDARY_REF_101_START,
    BOUNDARY_REF_101_END,
    "2025-10-01T00:00:00.000Z",
    "2025-10-31T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-RATIO-NEAR-MINUS-1`,
  ),
);
assertNear("ratio approximately -0.990099", ratioNearMinusOne.signedVarianceRatio, -0.990099, 0.00001);
reconcileRun(ratioNearMinusOne, "ratio-near-minus-one");

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-RATIO-MINUS-HALF-CUR`,
  materialId: BOUNDARY_MATERIAL,
  prices: [50, 50, 50, 50, 50, 50],
  observationIds: [
    "bb000001-0540-4000-8000-000000000540",
    "bb000001-0541-4000-8000-000000000541",
    "bb000001-0542-4000-8000-000000000542",
    "bb000001-0543-4000-8000-000000000543",
    "bb000001-0544-4000-8000-000000000544",
    "bb000001-0545-4000-8000-000000000545",
  ],
  windowStartUtc: "2025-11-01T00:00:00.000Z",
  windowEndUtc: "2025-11-30T23:59:59.000Z",
});
const ratioMinusHalf = await evaluateComparison(
  "ratio-minus-half",
  temporalBody(
    BOUNDARY_MATERIAL,
    BOUNDARY_REF_100_START,
    BOUNDARY_REF_100_END,
    "2025-11-01T00:00:00.000Z",
    "2025-11-30T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-RATIO-MINUS-HALF`,
  ),
);
assertNear("ratio -0.5", ratioMinusHalf.signedVarianceRatio, -0.5);
reconcileRun(ratioMinusHalf, "ratio-minus-half");

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-RATIO-ZERO-CUR`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-0550-4000-8000-000000000550",
    "bb000001-0551-4000-8000-000000000551",
    "bb000001-0552-4000-8000-000000000552",
    "bb000001-0553-4000-8000-000000000553",
    "bb000001-0554-4000-8000-000000000554",
    "bb000001-0555-4000-8000-000000000555",
  ],
  windowStartUtc: "2025-12-01T00:00:00.000Z",
  windowEndUtc: "2025-12-31T23:59:59.000Z",
});
const ratioZero = await evaluateComparison(
  "ratio-zero",
  temporalBody(
    BOUNDARY_MATERIAL,
    BOUNDARY_REF_100_START,
    BOUNDARY_REF_100_END,
    "2025-12-01T00:00:00.000Z",
    "2025-12-31T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-RATIO-ZERO`,
  ),
);
assertNear("ratio 0", ratioZero.signedVarianceRatio, 0);
reconcileRun(ratioZero, "ratio-zero");

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-ZERO-CURRENT-REF`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-0570-4000-8000-000000000570",
    "bb000001-0571-4000-8000-000000000571",
    "bb000001-0572-4000-8000-000000000572",
    "bb000001-0573-4000-8000-000000000573",
    "bb000001-0574-4000-8000-000000000574",
    "bb000001-0575-4000-8000-000000000575",
  ],
  windowStartUtc: "2026-06-01T00:00:00.000Z",
  windowEndUtc: "2026-06-30T23:59:59.000Z",
});
const zeroCurrent = await evaluateComparison(
  "zero-current-price",
  temporalBody(
    BOUNDARY_MATERIAL,
    "2026-06-01T00:00:00.000Z",
    "2026-06-30T23:59:59.000Z",
    "2026-07-01T00:00:00.000Z",
    "2026-07-31T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-ZERO-CURRENT`,
  ),
);
assert("zero current insufficient", zeroCurrent.comparisonOutcome === "insufficient-data", zeroCurrent.comparisonOutcome);
assert("zero current no median", zeroCurrent.currentMedianUnitPrice === null || zeroCurrent.currentMedianUnitPrice === undefined);
const zeroCurrentRuns = Number(db(`
  SELECT COUNT(*) FROM commercial_sales_price_comparison_runs
  WHERE idempotency_key = '${BOUNDARY_PREFIX}-ZERO-CURRENT'
    AND (current_median_unit_price = 0 OR current_median_unit_price IS NOT NULL AND current_median_unit_price <= 0)
`));
assert("zero current price not persisted", zeroCurrentRuns === 0, `count=${zeroCurrentRuns}`);

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-ZERO-REF-CUR`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-0590-4000-8000-000000000590",
    "bb000001-0591-4000-8000-000000000591",
    "bb000001-0592-4000-8000-000000000592",
    "bb000001-0593-4000-8000-000000000593",
    "bb000001-0594-4000-8000-000000000594",
    "bb000001-0595-4000-8000-000000000595",
  ],
  windowStartUtc: "2026-09-01T00:00:00.000Z",
  windowEndUtc: "2026-09-30T23:59:59.000Z",
});
const zeroReference = await evaluateComparison(
  "zero-reference-median",
  temporalBody(
    BOUNDARY_MATERIAL,
    "2026-08-01T00:00:00.000Z",
    "2026-08-31T23:59:59.000Z",
    "2026-09-01T00:00:00.000Z",
    "2026-09-30T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-ZERO-REF`,
  ),
);
assert("zero reference insufficient", zeroReference.comparisonOutcome === "insufficient-data", zeroReference.comparisonOutcome);
assert("zero reference no ratio", zeroReference.signedVarianceRatio === null || zeroReference.signedVarianceRatio === undefined);
const zeroReferenceRuns = Number(db(`
  SELECT COUNT(*) FROM commercial_sales_price_comparison_runs
  WHERE idempotency_key = '${BOUNDARY_PREFIX}-ZERO-REF'
    AND signed_variance_ratio IS NOT NULL
    AND reference_median_unit_price IS NOT NULL
    AND reference_median_unit_price <= 0
`));
assert("non-positive reference median not persisted", zeroReferenceRuns === 0, `count=${zeroReferenceRuns}`);

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-SPREAD-HIGH-A`,
  materialId: BOUNDARY_MATERIAL,
  prices: [50, 50, 50, 50, 50, 50],
  observationIds: [
    "bb000001-05a0-4000-8000-0000000005a0",
    "bb000001-05a1-4000-8000-0000000005a1",
    "bb000001-05a2-4000-8000-0000000005a2",
    "bb000001-05a3-4000-8000-0000000005a3",
    "bb000001-05a4-4000-8000-0000000005a4",
    "bb000001-05a5-4000-8000-0000000005a5",
  ],
  windowStartUtc: "2026-10-01T00:00:00.000Z",
  windowEndUtc: "2026-10-31T23:59:59.000Z",
  customerReference: "CUST-HIGH-SPREAD-A",
});
insertObservations({
  setCode: `${BOUNDARY_PREFIX}-SPREAD-HIGH-B`,
  materialId: BOUNDARY_MATERIAL,
  prices: [200, 200, 200, 200, 200, 200],
  observationIds: [
    "bb000001-05b0-4000-8000-0000000005b0",
    "bb000001-05b1-4000-8000-0000000005b1",
    "bb000001-05b2-4000-8000-0000000005b2",
    "bb000001-05b3-4000-8000-0000000005b3",
    "bb000001-05b4-4000-8000-0000000005b4",
    "bb000001-05b5-4000-8000-0000000005b5",
  ],
  windowStartUtc: "2026-10-01T00:00:00.000Z",
  windowEndUtc: "2026-10-31T23:59:59.000Z",
  customerReference: "CUST-HIGH-SPREAD-B",
});
const highSpread = await evaluateComparison(
  "customer-spread-above-one",
  customerBody(
    BOUNDARY_MATERIAL,
    "2026-10-01T00:00:00.000Z",
    "2026-10-31T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-SPREAD-HIGH`,
  ),
);
assert("customer spread may exceed 1", highSpread.customerSpreadRatio > 1, String(highSpread.customerSpreadRatio));
const highSpreadResults = highSpread.customerResults.slice().sort((left, right) => left.priceRank - right.priceRank);
const highSpreadHigher = highSpreadResults.find((item) => item.customerReference === "CUST-HIGH-SPREAD-B");
assertNear("high spread higher customer position", highSpreadHigher?.relativePricePositionRatio, 0.75);
assertNear("high spread ratio", highSpread.customerSpreadRatio, 3.0);

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-POS-100-200-A`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-05e0-4000-8000-0000000005e0",
    "bb000001-05e1-4000-8000-0000000005e1",
    "bb000001-05e2-4000-8000-0000000005e2",
    "bb000001-05e3-4000-8000-0000000005e3",
    "bb000001-05e4-4000-8000-0000000005e4",
    "bb000001-05e5-4000-8000-0000000005e5",
  ],
  windowStartUtc: "2026-12-01T00:00:00.000Z",
  windowEndUtc: "2026-12-31T23:59:59.000Z",
  customerReference: "CUST-POS-100-200-A",
});
insertObservations({
  setCode: `${BOUNDARY_PREFIX}-POS-100-200-B`,
  materialId: BOUNDARY_MATERIAL,
  prices: [200, 200, 200, 200, 200, 200],
  observationIds: [
    "bb000001-05f0-4000-8000-0000000005f0",
    "bb000001-05f1-4000-8000-0000000005f1",
    "bb000001-05f2-4000-8000-0000000005f2",
    "bb000001-05f3-4000-8000-0000000005f3",
    "bb000001-05f4-4000-8000-0000000005f4",
    "bb000001-05f5-4000-8000-0000000005f5",
  ],
  windowStartUtc: "2026-12-01T00:00:00.000Z",
  windowEndUtc: "2026-12-31T23:59:59.000Z",
  customerReference: "CUST-POS-100-200-B",
});
const position100200 = await evaluateComparison(
  "position-ratio-100-200",
  customerBody(
    BOUNDARY_MATERIAL,
    "2026-12-01T00:00:00.000Z",
    "2026-12-31T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-POS-100-200`,
  ),
);
assertNear("position 100/200 spread ratio", position100200.customerSpreadRatio, 1.0);
const position100200Results = position100200.customerResults.slice().sort((left, right) => left.priceRank - right.priceRank);
assert("position 100/200 lowest rank", position100200Results[0]?.priceRank === 1, JSON.stringify(position100200Results));
assert("position 100/200 higher rank", position100200Results[1]?.priceRank === 2, JSON.stringify(position100200Results));
assertNear("position 100/200 lowest customer position", position100200Results[0]?.relativePricePositionRatio, 0);
assertNear("position 100/200 higher customer position", position100200Results[1]?.relativePricePositionRatio, 0.5);

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-POS-100-250-A`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-0600-4000-8000-000000000600",
    "bb000001-0601-4000-8000-000000000601",
    "bb000001-0602-4000-8000-000000000602",
    "bb000001-0603-4000-8000-000000000603",
    "bb000001-0604-4000-8000-000000000604",
    "bb000001-0605-4000-8000-000000000605",
  ],
  windowStartUtc: "2027-01-01T00:00:00.000Z",
  windowEndUtc: "2027-01-31T23:59:59.000Z",
  customerReference: "CUST-POS-100-250-A",
});
insertObservations({
  setCode: `${BOUNDARY_PREFIX}-POS-100-250-B`,
  materialId: BOUNDARY_MATERIAL,
  prices: [250, 250, 250, 250, 250, 250],
  observationIds: [
    "bb000001-0610-4000-8000-000000000610",
    "bb000001-0611-4000-8000-000000000611",
    "bb000001-0612-4000-8000-000000000612",
    "bb000001-0613-4000-8000-000000000613",
    "bb000001-0614-4000-8000-000000000614",
    "bb000001-0615-4000-8000-000000000615",
  ],
  windowStartUtc: "2027-01-01T00:00:00.000Z",
  windowEndUtc: "2027-01-31T23:59:59.000Z",
  customerReference: "CUST-POS-100-250-B",
});
const position100250 = await evaluateComparison(
  "position-ratio-100-250",
  customerBody(
    BOUNDARY_MATERIAL,
    "2027-01-01T00:00:00.000Z",
    "2027-01-31T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-POS-100-250`,
  ),
);
assertNear("position 100/250 spread ratio", position100250.customerSpreadRatio, 1.5);
const position100250Results = position100250.customerResults.slice().sort((left, right) => left.priceRank - right.priceRank);
assertNear("position 100/250 lowest customer position", position100250Results[0]?.relativePricePositionRatio, 0);
assertNear("position 100/250 higher customer position", position100250Results[1]?.relativePricePositionRatio, 0.6);

insertObservations({
  setCode: `${BOUNDARY_PREFIX}-POSITION-A`,
  materialId: BOUNDARY_MATERIAL,
  prices: [100, 100, 100, 100, 100, 100],
  observationIds: [
    "bb000001-05c0-4000-8000-0000000005c0",
    "bb000001-05c1-4000-8000-0000000005c1",
    "bb000001-05c2-4000-8000-0000000005c2",
    "bb000001-05c3-4000-8000-0000000005c3",
    "bb000001-05c4-4000-8000-0000000005c4",
    "bb000001-05c5-4000-8000-0000000005c5",
  ],
  windowStartUtc: "2026-11-01T00:00:00.000Z",
  windowEndUtc: "2026-11-30T23:59:59.000Z",
  customerReference: "CUST-POS-A",
});
insertObservations({
  setCode: `${BOUNDARY_PREFIX}-POSITION-B`,
  materialId: BOUNDARY_MATERIAL,
  prices: [150, 150, 150, 150, 150, 150],
  observationIds: [
    "bb000001-05d0-4000-8000-0000000005d0",
    "bb000001-05d1-4000-8000-0000000005d1",
    "bb000001-05d2-4000-8000-0000000005d2",
    "bb000001-05d3-4000-8000-0000000005d3",
    "bb000001-05d4-4000-8000-0000000005d4",
    "bb000001-05d5-4000-8000-0000000005d5",
  ],
  windowStartUtc: "2026-11-01T00:00:00.000Z",
  windowEndUtc: "2026-11-30T23:59:59.000Z",
  customerReference: "CUST-POS-B",
});
const positionRun = await evaluateComparison(
  "relative-position-bounds",
  customerBody(
    BOUNDARY_MATERIAL,
    "2026-11-01T00:00:00.000Z",
    "2026-11-30T23:59:59.000Z",
    `${BOUNDARY_PREFIX}-POSITION`,
  ),
);
for (const result of positionRun.customerResults) {
  if (result.relativePricePositionRatio !== null && result.relativePricePositionRatio !== undefined) {
    assert(
      `${result.customerReference} relative position >= 0`,
      result.relativePricePositionRatio >= 0,
      String(result.relativePricePositionRatio),
    );
    assert(
      `${result.customerReference} relative position < 1`,
      result.relativePricePositionRatio < 1,
      String(result.relativePricePositionRatio),
    );
  }
}

const overlapReject = await evaluateComparison(
  "overlap-reject",
  {
    ...temporalBody(
      SCENARIO_MATERIAL,
      REF_START,
      REF_END,
      CUR_START,
      CUR_END,
      `${BOUNDARY_PREFIX}-OVERLAP`,
    ),
    currentWindowStartUtc: "2026-05-01T00:00:00.000Z",
    currentWindowEndUtc: "2026-07-01T00:00:00.000Z",
    referenceWindowStartUtc: "2026-06-01T00:00:00.000Z",
    referenceWindowEndUtc: "2026-08-01T00:00:00.000Z",
  },
  { expectReject: true },
);
assert("overlap windows rejected", overlapReject.status === 400, `status=${overlapReject.status}`);
} finally {
  cleanupVerifierFixtures();
  assertFixtureIsolation("post-cleanup");
}

console.log("COMMERCIAL-SALES-PRICE-2 deviation/customer-comparison verification passed.");
}

main().catch((error) => {
  console.error(`COMMERCIAL-SALES-PRICE-2 deviation/customer-comparison verification failed: ${error?.message ?? error}`);
  process.exit(1);
});

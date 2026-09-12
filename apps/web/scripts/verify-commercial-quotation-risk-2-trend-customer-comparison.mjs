#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const SCENARIO_MATERIAL = "bb000001-02d1-4000-8000-0000000002d1";
const CUSTOMER_TIE_MATERIAL = "bb000001-02d7-4000-8000-0000000002d7";
const PROFILE = "bb000001-0280-4000-8000-000000000280";
const CLOSURE_PREFIX = "COMMQUOTE2-CLOSURE";
const BOUNDARY_PREFIX = "COMMQUOTE2-BOUNDARY";
const QUOTATION_SOURCE_KIND = "manual-development-simulator";
const PRICE_BASIS_GROSS_NET = "net";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const REF_START = "2026-01-01T00:00:00.000Z";
const REF_END = "2026-06-30T23:59:59.000Z";
const CUR_START = "2026-07-01T00:00:00.000Z";
const CUR_END = "2026-12-31T23:59:59.000Z";

const PILOT_RUNS = {
  stable: "bb000001-02c1-4000-8000-0000000002c1",
  increase: "bb000001-02c2-4000-8000-0000000002c2",
  decrease: "bb000001-02c3-4000-8000-0000000002c3",
  expiry: "bb000001-02c4-4000-8000-0000000002c4",
  payment: "bb000001-02c5-4000-8000-0000000002c5",
  customerPosition: "bb000001-02c6-4000-8000-0000000002c6",
  customerTie: "bb000001-02c7-4000-8000-0000000002c7",
  insufficient: "bb000001-02c8-4000-8000-0000000002c8",
  conflict: "bb000001-02c9-4000-8000-0000000002c9",
};

function fail(message) {
  console.error(`COMMERCIAL-QUOTATION-RISK-2 trend/customer-comparison verification failed: ${message}`);
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
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
      "NevaxisFactOS-local-postgres-1",
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

function riskIncidenceDelta(referenceIncidence, currentIncidence) {
  return currentIncidence - referenceIncidence;
}

function windowsOverlap(currentStart, currentEnd, referenceStart, referenceEnd) {
  return new Date(currentStart) < new Date(referenceEnd) && new Date(referenceStart) < new Date(currentEnd);
}

function assertAssessmentCounts(run, label) {
  assert(
    `${label} candidate = comparable + excluded`,
    run.candidateAssessmentCount === run.comparableAssessmentCount + run.excludedAssessmentCount,
    `candidate=${run.candidateAssessmentCount} comparable=${run.comparableAssessmentCount} excluded=${run.excludedAssessmentCount}`,
  );
}

function removeFixturePrefix(prefix) {
  dbExec(`
    DELETE FROM commercial_quotation_risk_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${prefix}%';
    DELETE FROM commercial_quotation_risk_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${prefix}%';
    DELETE FROM commercial_quotation_risk_evidence_observations WHERE ingest_idempotency_key LIKE '${prefix}%';
  `);
}

function cleanupVerifierFixtures() {
  for (const prefix of FIXTURE_PREFIXES) {
    removeFixturePrefix(prefix);
  }
  db(`
    DELETE FROM materials
    WHERE id IN (
      'bb000001-04ff-4000-8000-0000000004ff',
      'bb000001-04fe-4000-8000-0000000004fe',
      'bb000001-04fd-4000-8000-0000000004fd',
      'bb000001-04fc-4000-8000-0000000004fc'
    );
  `);
}

function insertAssessment({
  assessmentId,
  evidenceId,
  idempotencyKey,
  materialId,
  customerReference,
  outcome,
  leadingDriverCode,
  evaluatedAtUtc,
  windowStartUtc,
  windowEndUtc,
  evidenceStatus = "simulated",
}) {
  const quotedAt = new Date(new Date(evaluatedAtUtc).getTime() - 5 * 24 * 60 * 60 * 1000).toISOString();
  db(`
    INSERT INTO commercial_quotation_risk_evidence_observations (
      id, tenant_id, facility_id, quotation_reference, customer_reference, material_id, source_kind,
      evidence_status, quoted_at_utc, expires_at_utc, quoted_unit_price, currency_code, unit_of_measure,
      gross_net_basis, tax_basis, discount_basis, freight_basis, payment_term_days, source_reference,
      source_provenance, source_fingerprint, ingest_idempotency_key, ingest_idempotency_payload_fingerprint,
      received_at_utc, created_at_utc, created_by)
    VALUES (
      '${evidenceId}', '${TENANT}', '${FACILITY}', 'QTE-${idempotencyKey}', '${customerReference}', '${materialId}',
      '${QUOTATION_SOURCE_KIND}', '${evidenceStatus}', '${quotedAt}', '${evaluatedAtUtc}', 100, 'TRY', 'kg',
      '${PRICE_BASIS_GROSS_NET}', 'excluded', 'excluded', 'excluded', 30, 'QTE-${idempotencyKey}',
      'closure fixture', '${idempotencyKey}-fp', '${idempotencyKey}', '${idempotencyKey}-fp',
      '${quotedAt}', '${quotedAt}', 'closure-verifier')
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO commercial_quotation_risk_assessments (
      id, tenant_id, facility_id, assessment_reference, profile_id, profile_version, evidence_observation_id,
      quotation_reference, customer_reference, material_id, reference_window_start_utc, reference_window_end_utc,
      reference_median_unit_price, quoted_comparable_unit_price, signed_price_variance_ratio,
      candidate_observation_count, comparable_observation_count, excluded_observation_count,
      payment_term_days, remaining_validity_days, currency_code, unit_of_measure, gross_net_basis,
      tax_basis, discount_basis, freight_basis, evidence_status, outcome, leading_driver_code,
      rule_set_version, idempotency_key, idempotency_payload_fingerprint, source_provenance,
      evaluated_at_utc, created_at_utc, created_by)
    VALUES (
      '${assessmentId}', '${TENANT}', '${FACILITY}', 'ASM-${idempotencyKey}', '${PROFILE}', '1', '${evidenceId}',
      'QTE-${idempotencyKey}', '${customerReference}', '${materialId}', '${windowStartUtc}', '${windowEndUtc}',
      100, 100, 0, 6, 6, 0, 30, 30, 'TRY', 'kg', '${PRICE_BASIS_GROSS_NET}', 'excluded', 'excluded', 'excluded',
      '${evidenceStatus}', '${outcome}', '${leadingDriverCode}', 'COMMERCIAL-QUOTATION-RISK-1.0',
      '${idempotencyKey}', '${idempotencyKey}-fp', 'closure fixture', '${evaluatedAtUtc}', '${evaluatedAtUtc}', 'closure-verifier')
    ON CONFLICT (id) DO UPDATE SET
      evaluated_at_utc = EXCLUDED.evaluated_at_utc,
      outcome = EXCLUDED.outcome,
      leading_driver_code = EXCLUDED.leading_driver_code,
      evidence_status = EXCLUDED.evidence_status,
      customer_reference = EXCLUDED.customer_reference;
  `);
}

function assessmentGuid(offset) {
  return `bb000001-${offset.toString(16).padStart(4, "0")}-4000-8000-${offset.toString(16).padStart(12, "0")}`;
}

function seedAssessmentBatch({
  prefix,
  materialId,
  customerReference,
  windowKind,
  count,
  outcome,
  leadingDriverCode,
  startIndex,
  evidenceStatus = "simulated",
}) {
  const windowStart = windowKind === "reference" ? REF_START : CUR_START;
  const windowEnd = windowKind === "reference" ? REF_END : CUR_END;
  const startMs = new Date(windowStart).getTime();
  const endMs = new Date(windowEnd).getTime();
  const stepMs = count > 1 ? (endMs - startMs) / (count - 1) : 0;
  for (let index = 0; index < count; index += 1) {
    const evaluatedAt = new Date(startMs + stepMs * index).toISOString();
    const suffix = `${windowKind}-${index}`;
    const offset = 0x400 + startIndex + index;
    const evidenceOffset = 0x500 + startIndex + index;
    insertAssessment({
      assessmentId: assessmentGuid(offset),
      evidenceId: assessmentGuid(evidenceOffset),
      idempotencyKey: `${prefix}-${suffix}`,
      materialId,
      customerReference,
      outcome,
      leadingDriverCode,
      evaluatedAtUtc: evaluatedAt,
      windowStartUtc: windowStart,
      windowEndUtc: windowEnd,
      evidenceStatus,
    });
  }
}

function loadRunFromDb(runId) {
  const line = db(`
    SELECT comparison_outcome,
           COALESCE(risk_incidence_delta::text, ''),
           COALESCE(reference_risk_incidence::text, ''),
           COALESCE(current_risk_incidence::text, ''),
           COALESCE(leading_driver_code, ''),
           candidate_assessment_count,
           comparable_assessment_count,
           excluded_assessment_count
    FROM commercial_quotation_risk_comparison_runs
    WHERE id = '${runId}'
  `);
  const parts = line.split("|");
  return {
    comparisonOutcome: parts[0],
    riskIncidenceDelta: parts[1] === "" ? null : Number(parts[1]),
    referenceRiskIncidence: parts[2] === "" ? null : Number(parts[2]),
    currentRiskIncidence: parts[3] === "" ? null : Number(parts[3]),
    leadingDriverCode: parts[4] === "" ? null : parts[4],
    candidateAssessmentCount: Number(parts[5]),
    comparableAssessmentCount: Number(parts[6]),
    excludedAssessmentCount: Number(parts[7]),
  };
}

function reconcileRun(apiRun, label) {
  const dbRun = loadRunFromDb(apiRun.id);
  assert(`${label} db outcome`, dbRun.comparisonOutcome === apiRun.comparisonOutcome);
  assert(`${label} db leading driver`, dbRun.leadingDriverCode === apiRun.leadingDriverCode);
  assertNear(`${label} db incidence delta`, dbRun.riskIncidenceDelta, apiRun.riskIncidenceDelta);
  assertAssessmentCounts(dbRun, `${label} db`);
  assertAssessmentCounts(apiRun, `${label} api`);
}

console.log("Running COMMERCIAL-QUOTATION-RISK-2 trend/customer-comparison verifier.");

const evaluator = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskComparisonEvaluator.cs");
const operations = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskComparisonOperations.cs");
const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialQuotationRiskEndpointRouteBuilderExtensions.cs");
const comparisonSeed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialQuotationRiskComparisonSeed.cs");

assert("temporal risk trend evaluator", evaluator.includes("EvaluateTemporalRiskTrend"));
assert("customer risk position evaluator", evaluator.includes("EvaluateCustomerRiskPosition"));
assert("non-overlap validation", operations.includes("must not overlap"));
assert("comparison endpoints present", endpoints.includes("/comparisons"));
assert("risk-incidence-increase driver", readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialQuotationRiskComparisonContracts.cs").includes("risk-incidence-increase"));
assert("risk-decrease outcome", evaluator.includes("RiskDecrease") || evaluator.includes("risk-decrease"));
forbidText(evaluator, "guaranteed savings", "evaluator");
forbidText(evaluator, "recommended customer", "evaluator");
assert("comparison seed stable pilot", comparisonSeed.includes("COMMQUOTE2-CMP-PILOT-STABLE"));
assert("comparison seed nine scenarios", comparisonSeed.includes("commquote2-pilot-source-conflict"));
assert("maximum risk incidence delta reconciliation", comparisonSeed.includes("MaximumRiskIncidenceDelta"));

assertNear("risk incidence delta helper", riskIncidenceDelta(0, 1), 1);
assert("non-overlapping windows", !windowsOverlap(CUR_START, CUR_END, REF_START, REF_END));

async function main() {
  cleanupVerifierFixtures();

  try {
    const ready = await fetch(`${BASE}/health/ready`);
    assert("API ready", ready.status === 200, `status=${ready.status}`);

    const profiles = await liveRequest("GET", `/api/commercial-intelligence/quotation-risks/profiles?facilityId=${FACILITY}`);
    const pilotProfile = profiles.json?.items?.find((item) => item.id === PROFILE);
    assert("pilot profile present", Boolean(pilotProfile));
    assert("pilot maximumRiskIncidenceDelta 0.10", pilotProfile.maximumRiskIncidenceDelta === 0.1);
    const profileVersion = pilotProfile.version;

    const provenance =
      "Deterministic local Development quotation-risk evidence — simulated, non-production, not real customer, quotation, invoice, order, or market evidence.";

    async function getPilotRun(runId) {
      const response = await liveRequest(
        "GET",
        `/api/commercial-intelligence/quotation-risks/comparisons/${runId}?facilityId=${FACILITY}`,
      );
      assert(`pilot run ${runId} 200`, response.status === 200, response.text?.slice(0, 200));
      return response.json;
    }

    const stablePilot = await getPilotRun(PILOT_RUNS.stable);
    assert("pilot stable outcome", stablePilot.comparisonOutcome === "within-risk-band", stablePilot.comparisonOutcome);
    assertNear("pilot stable incidence delta", stablePilot.riskIncidenceDelta, 0);

    const increasePilot = await getPilotRun(PILOT_RUNS.increase);
    assert("pilot increase outcome", increasePilot.comparisonOutcome === "risk-increase", increasePilot.comparisonOutcome);
    assert("pilot increase leading driver", increasePilot.leadingDriverCode === "risk-incidence-increase", increasePilot.leadingDriverCode);
    assertNear("pilot increase incidence delta", increasePilot.riskIncidenceDelta, 1);

    const decreasePilot = await getPilotRun(PILOT_RUNS.decrease);
    assert("pilot decrease outcome", decreasePilot.comparisonOutcome === "risk-decrease", decreasePilot.comparisonOutcome);
    assertNear("pilot decrease incidence delta", decreasePilot.riskIncidenceDelta, -1);

    const expiryPilot = await getPilotRun(PILOT_RUNS.expiry);
    assert("pilot expiry concentration driver", expiryPilot.drivers.some((d) => d.reasonCode === "expiry-risk-concentration"));

    const paymentPilot = await getPilotRun(PILOT_RUNS.payment);
    assert("pilot payment concentration driver", paymentPilot.drivers.some((d) => d.reasonCode === "payment-term-risk-concentration"));

    const customerPositionPilot = await getPilotRun(PILOT_RUNS.customerPosition);
    assert("pilot customer position driver", customerPositionPilot.drivers.some((d) => d.reasonCode === "customer-risk-concentration"));
    assert("pilot customer position rows", customerPositionPilot.customerResults.length === 2);

    const customerTiePilot = await getPilotRun(PILOT_RUNS.customerTie);
    assert("pilot customer tie driver", customerTiePilot.drivers.some((d) => d.reasonCode === "customer-risk-tie"));
    const tiedResults = customerTiePilot.customerResults.filter((r) => r.isTied);
    assert("pilot tie results", tiedResults.length === 2, String(tiedResults.length));

    const insufficientPilot = await getPilotRun(PILOT_RUNS.insufficient);
    assert("pilot insufficient outcome", insufficientPilot.comparisonOutcome === "insufficient-data", insufficientPilot.comparisonOutcome);

    const conflictPilot = await getPilotRun(PILOT_RUNS.conflict);
    assert("pilot source conflict outcome", conflictPilot.comparisonOutcome === "source-conflict", conflictPilot.comparisonOutcome);

    const pilotRunIds = Object.values(PILOT_RUNS).join("','");
    const pilotRunCount = Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_comparison_runs WHERE id IN ('${pilotRunIds}')`));
    assert("nine pilot comparison runs", pilotRunCount === 9, `count=${pilotRunCount}`);

    async function evaluateComparison(name, body, extra = {}) {
      const response = await liveRequest(
        "POST",
        `/api/commercial-intelligence/quotation-risks/comparisons/evaluate?facilityId=${FACILITY}`,
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
        comparisonType: "temporal-risk-trend",
        materialId,
        currentWindowStartUtc: curStart,
        currentWindowEndUtc: curEnd,
        referenceWindowStartUtc: refStart,
        referenceWindowEndUtc: refEnd,
        sourceProvenance: provenance,
        idempotencyKey,
      };
    }

    function customerBody(materialId, windowStart, windowEnd, idempotencyKey) {
      return {
        profileId: PROFILE,
        expectedProfileVersion: profileVersion,
        comparisonType: "customer-risk-position",
        materialId,
        currentWindowStartUtc: windowStart,
        currentWindowEndUtc: windowEnd,
        sourceProvenance: provenance,
        idempotencyKey,
      };
    }

    const closureMaterial = "bb000001-04ff-4000-8000-0000000004ff";
    const closureMaterialIncrease = "bb000001-04fe-4000-8000-0000000004fe";
    const closureMaterialCustomer = "bb000001-04fd-4000-8000-0000000004fd";
    const closureMaterialInsufficient = "bb000001-04fc-4000-8000-0000000004fc";
    db(`
      INSERT INTO materials (
        id, tenant_id, facility_id, code, code_normalized, name, material_category, polymer_family,
        manufacturer, trade_name_or_grade, color_name, physical_form, lifecycle_status, unit_of_measure,
        is_active, source_type, source_system, source_reference, data_recorded_at_utc,
        created_at_utc, updated_at_utc, created_by_subject_id, updated_by_subject_id, version)
      VALUES (
        '${closureMaterial}', '${TENANT}', '${FACILITY}', 'COMMQUOTE2-MAT-CLOSURE', 'COMMQUOTE2-MAT-CLOSURE',
        'Closure comparison material', 'virgin-polymer', 'PP', 'Pilot', 'COMMQUOTE2-MAT-CLOSURE', 'Doğal',
        'granule', 'active', 'kg', true, 'development-pilot', 'development-pilot', 'commquote2-mat-closure',
        NOW(), NOW(), NOW(), 'closure-verifier', 'closure-verifier', 1
      ) ON CONFLICT (id) DO NOTHING;
      INSERT INTO materials (
        id, tenant_id, facility_id, code, code_normalized, name, material_category, polymer_family,
        manufacturer, trade_name_or_grade, color_name, physical_form, lifecycle_status, unit_of_measure,
        is_active, source_type, source_system, source_reference, data_recorded_at_utc, created_at_utc,
        updated_at_utc, created_by_subject_id, updated_by_subject_id, version
      ) VALUES (
        '${closureMaterialIncrease}', '${TENANT}', '${FACILITY}', 'COMMQUOTE2-MAT-CLOSURE-INCREASE', 'COMMQUOTE2-MAT-CLOSURE-INCREASE',
        'Closure increase material', 'virgin-polymer', 'PP', 'Pilot', 'COMMQUOTE2-MAT-CLOSURE-INCREASE', 'Doğal',
        'granule', 'active', 'kg', true, 'development-pilot', 'development-pilot', 'commquote2-mat-closure-increase',
        NOW(), NOW(), NOW(), 'closure-verifier', 'closure-verifier', 1)
      ON CONFLICT (id) DO NOTHING;
      INSERT INTO materials (
        id, tenant_id, facility_id, code, code_normalized, name, material_category, polymer_family,
        manufacturer, trade_name_or_grade, color_name, physical_form, lifecycle_status, unit_of_measure,
        is_active, source_type, source_system, source_reference, data_recorded_at_utc, created_at_utc,
        updated_at_utc, created_by_subject_id, updated_by_subject_id, version
      ) VALUES (
        '${closureMaterialInsufficient}', '${TENANT}', '${FACILITY}', 'COMMQUOTE2-MAT-CLOSURE-INSUFFICIENT', 'COMMQUOTE2-MAT-CLOSURE-INSUFFICIENT',
        'Closure insufficient material', 'virgin-polymer', 'PP', 'Pilot', 'COMMQUOTE2-MAT-CLOSURE-INSUFFICIENT', 'Doğal',
        'granule', 'active', 'kg', true, 'development-pilot', 'development-pilot', 'commquote2-mat-closure-insufficient',
        NOW(), NOW(), NOW(), 'closure-verifier', 'closure-verifier', 1)
      ON CONFLICT (id) DO NOTHING;
      INSERT INTO materials (
        id, tenant_id, facility_id, code, code_normalized, name, material_category, polymer_family,
        manufacturer, trade_name_or_grade, color_name, physical_form, lifecycle_status, unit_of_measure,
        is_active, source_type, source_system, source_reference, data_recorded_at_utc, created_at_utc,
        updated_at_utc, created_by_subject_id, updated_by_subject_id, version
      ) VALUES (
        '${closureMaterialCustomer}', '${TENANT}', '${FACILITY}', 'COMMQUOTE2-MAT-CLOSURE-CUSTOMER', 'COMMQUOTE2-MAT-CLOSURE-CUSTOMER',
        'Closure customer material', 'virgin-polymer', 'PP', 'Pilot', 'COMMQUOTE2-MAT-CLOSURE-CUSTOMER', 'Doğal',
        'granule', 'active', 'kg', true, 'development-pilot', 'development-pilot', 'commquote2-mat-closure-customer',
        NOW(), NOW(), NOW(), 'closure-verifier', 'closure-verifier', 1)
      ON CONFLICT (id) DO NOTHING;
    `);

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-STABLE-REF`,
      materialId: closureMaterial,
      customerReference: "COMMQUOTE2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 0,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-STABLE-CUR`,
      materialId: closureMaterial,
      customerReference: "COMMQUOTE2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 10,
    });
    const stable = await evaluateComparison(
      "stable-trend",
      temporalBody(closureMaterial, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-STABLE`),
    );
    assert("stable outcome", stable.comparisonOutcome === "within-risk-band", stable.comparisonOutcome);
    assertNear("stable incidence delta", stable.riskIncidenceDelta, 0);
    reconcileRun(stable, "stable");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-INCREASE-REF`,
      materialId: closureMaterialIncrease,
      customerReference: "COMMQUOTE2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 20,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-INCREASE-CUR`,
      materialId: closureMaterialIncrease,
      customerReference: "COMMQUOTE2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      outcome: "attention",
      leadingDriverCode: "quoted-price-below-reference",
      startIndex: 30,
    });
    const increase = await evaluateComparison(
      "risk-increase",
      temporalBody(closureMaterialIncrease, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-INCREASE`),
    );
    assert("increase outcome", increase.comparisonOutcome === "risk-increase", increase.comparisonOutcome);
    assertNear("increase incidence delta", increase.riskIncidenceDelta, 1);
    reconcileRun(increase, "increase");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-CUSTOMER-A`,
      materialId: closureMaterialCustomer,
      customerReference: "COMMQUOTE2-CUST-CLOSURE-A",
      windowKind: "current",
      count: 6,
      outcome: "attention",
      leadingDriverCode: "quoted-price-below-reference",
      startIndex: 40,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-CUSTOMER-B`,
      materialId: closureMaterialCustomer,
      customerReference: "COMMQUOTE2-CUST-CLOSURE-B",
      windowKind: "current",
      count: 6,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 50,
    });
    const customerPosition = await evaluateComparison(
      "customer-position",
      customerBody(closureMaterialCustomer, CUR_START, CUR_END, `${CLOSURE_PREFIX}-CUSTOMER-POS`),
    );
    assert("customer position driver", customerPosition.drivers.some((d) => d.reasonCode === "customer-risk-concentration"));
    assert("customer position rows", customerPosition.customerResults.length === 2);
    reconcileRun(customerPosition, "customer-position");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-TIE-A-ATT`,
      materialId: CUSTOMER_TIE_MATERIAL,
      customerReference: "COMMQUOTE2-CUST-TIE-A",
      windowKind: "current",
      count: 3,
      outcome: "attention",
      leadingDriverCode: "quoted-price-below-reference",
      startIndex: 60,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-TIE-A-WITH`,
      materialId: CUSTOMER_TIE_MATERIAL,
      customerReference: "COMMQUOTE2-CUST-TIE-A",
      windowKind: "current",
      count: 3,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 63,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-TIE-B-ATT`,
      materialId: CUSTOMER_TIE_MATERIAL,
      customerReference: "COMMQUOTE2-CUST-TIE-B",
      windowKind: "current",
      count: 3,
      outcome: "attention",
      leadingDriverCode: "quoted-price-below-reference",
      startIndex: 70,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-TIE-B-WITH`,
      materialId: CUSTOMER_TIE_MATERIAL,
      customerReference: "COMMQUOTE2-CUST-TIE-B",
      windowKind: "current",
      count: 3,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 73,
    });
    const customerTie = await evaluateComparison(
      "customer-tie",
      customerBody(CUSTOMER_TIE_MATERIAL, CUR_START, CUR_END, `${CLOSURE_PREFIX}-CUSTOMER-TIE`),
    );
    assert("customer tie driver", customerTie.drivers.some((d) => d.reasonCode === "customer-risk-tie"));
    reconcileRun(customerTie, "customer-tie");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-INSUFFICIENT-REF`,
      materialId: closureMaterialInsufficient,
      customerReference: "COMMQUOTE2-CUST-BOUNDARY",
      windowKind: "reference",
      count: 6,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 80,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-INSUFFICIENT-CUR`,
      materialId: closureMaterialInsufficient,
      customerReference: "COMMQUOTE2-CUST-BOUNDARY",
      windowKind: "current",
      count: 3,
      outcome: "within-threshold",
      leadingDriverCode: "within-profile",
      startIndex: 90,
    });
    const insufficient = await evaluateComparison(
      "insufficient-data",
      temporalBody(closureMaterialInsufficient, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-INSUFFICIENT`),
    );
    assert("insufficient outcome", insufficient.comparisonOutcome === "insufficient-data", insufficient.comparisonOutcome);
    reconcileRun(insufficient, "insufficient");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-CONFLICT-CUR`,
      materialId: closureMaterial,
      customerReference: "COMMQUOTE2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      outcome: "source-conflict",
      leadingDriverCode: "source-conflict",
      startIndex: 100,
      evidenceStatus: "source-conflict",
    });
    const conflict = await evaluateComparison(
      "source-conflict",
      temporalBody(closureMaterial, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-CONFLICT`),
    );
    assert("source conflict outcome", conflict.comparisonOutcome === "source-conflict", conflict.comparisonOutcome);
    reconcileRun(conflict, "conflict");

    const overlapReject = await evaluateComparison(
      "overlap-reject",
      {
        ...temporalBody(SCENARIO_MATERIAL, REF_START, REF_END, CUR_START, CUR_END, `${BOUNDARY_PREFIX}-OVERLAP`),
        currentWindowStartUtc: "2026-06-01T00:00:00.000Z",
        currentWindowEndUtc: "2026-08-01T00:00:00.000Z",
        referenceWindowStartUtc: "2026-07-01T00:00:00.000Z",
        referenceWindowEndUtc: "2026-09-01T00:00:00.000Z",
      },
      { expectReject: true },
    );
    assert("overlap windows rejected", overlapReject.status === 400, `status=${overlapReject.status}`);
  } finally {
    cleanupVerifierFixtures();
  }

  console.log("COMMERCIAL-QUOTATION-RISK-2 trend/customer-comparison verification passed.");
}

main().catch((error) => {
  console.error(`COMMERCIAL-QUOTATION-RISK-2 trend/customer-comparison verification failed: ${error?.message ?? error}`);
  process.exit(1);
});

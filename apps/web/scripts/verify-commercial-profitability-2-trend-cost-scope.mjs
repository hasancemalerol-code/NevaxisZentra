#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const SCENARIO_MATERIAL = "bb000001-03c0-4000-8000-0000000003c0";
const CLOSURE_PREFIX = "COMMPROFIT2-CLOSURE";
const BOUNDARY_PREFIX = "COMMPROFIT2-BOUNDARY";
const SOURCE_KIND = "manual-development-simulator";
const COST_BASIS = "net";
const PRICE_BASIS_GROSS_NET = "net";
const SALES_UNIT_PRICE = 100;
const MARGIN_DELTA_THRESHOLD = 0.05;
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const REF_START = "2026-01-01T00:00:00.000Z";
const REF_END = "2026-06-30T23:59:59.000Z";
const CUR_START = "2026-07-01T00:00:00.000Z";
const CUR_END = "2026-12-31T23:59:59.000Z";

const COST_MIXES = {
  margin30: { directMaterial: 35, directLabor: 15, machine: 10, energy: 10 },
  margin24: { directMaterial: 35, directLabor: 15, machine: 16, energy: 10 },
  margin36: { directMaterial: 31, directLabor: 15, machine: 8, energy: 10 },
  margin25: { directMaterial: 35, directLabor: 15, machine: 15, energy: 10 },
  margin35: { directMaterial: 33, directLabor: 15, machine: 9, energy: 8 },
};

const PILOT_RUNS = {
  stable: {
    id: "bb000001-03b0-4000-8000-0000000003b0",
    outcome: "within-comparison-band",
    leadingDriver: "within-comparison-band",
    expectedDelta: 0,
  },
  deterioration: {
    id: "bb000001-03b1-4000-8000-0000000003b1",
    outcome: "contribution-deterioration",
    leadingDriver: "contribution-margin-deterioration",
    expectedDelta: -0.06,
  },
  improvement: {
    id: "bb000001-03b2-4000-8000-0000000003b2",
    outcome: "contribution-improvement",
    leadingDriver: "contribution-margin-improvement",
    expectedDelta: 0.06,
  },
  exact: {
    id: "bb000001-03b3-4000-8000-0000000003b3",
    outcome: "within-comparison-band",
    leadingDriver: "within-comparison-band",
    expectedDelta: -0.05,
  },
  directMaterial: {
    id: "bb000001-03b4-4000-8000-0000000003b4",
    outcome: "within-comparison-band",
    comparisonType: "cost-component-mix-change",
    driver: "direct-material-cost-increase",
  },
  energy: {
    id: "bb000001-03b5-4000-8000-0000000003b5",
    outcome: "within-comparison-band",
    comparisonType: "cost-component-mix-change",
    driver: "energy-cost-increase",
  },
  customerPosition: {
    id: "bb000001-03b6-4000-8000-0000000003b6",
    outcome: "within-comparison-band",
    comparisonType: "customer-contribution-position",
    driver: "customer-contribution-concentration",
    customerResultCount: 2,
  },
  materialPosition: {
    id: "bb000001-03b7-4000-8000-0000000003b7",
    outcome: "within-comparison-band",
    comparisonType: "material-contribution-position",
    driver: "material-contribution-concentration",
    materialResultCount: 2,
  },
  tie: {
    id: "bb000001-03b8-4000-8000-0000000003b8",
    outcome: "within-comparison-band",
    comparisonType: "customer-contribution-position",
    driver: "contribution-position-tie",
    tiedResultCount: 2,
  },
  insufficient: {
    id: "bb000001-03b9-4000-8000-0000000003b9",
    outcome: "insufficient-data",
  },
  incomplete: {
    id: "bb000001-03ba-4000-8000-0000000003ba",
    outcome: "insufficient-data",
  },
  conflict: {
    id: "bb000001-03bb-4000-8000-0000000003bb",
    outcome: "source-conflict",
    leadingDriver: "source-conflict",
  },
};

const COMPARISON_SOURCE_FILES = [
  "packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityComparisonEvaluator.cs",
  "packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityComparisonOperations.cs",
  "packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityComparisonContracts.cs",
  "packages/CrevaFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilityComparisonSeed.cs",
  "apps/api/CommercialIntelligence/CommercialProfitabilityEndpointRouteBuilderExtensions.cs",
];

const FIXTURE_PREFIXES = [CLOSURE_PREFIX, BOUNDARY_PREFIX];

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-2 trend/cost-scope verification failed: ${message}`);
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

function readProvenance() {
  const constants = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
  const match = constants.match(/ProfitabilityComparisonDevelopmentPilotProvenance\s*=\s*\n\s*"([^"]+)"/);
  if (!match) fail("ProfitabilityComparisonDevelopmentPilotProvenance not found in CommercialIntelligenceConstants.cs");
  return match[1];
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

function totalCostFromMix(costMix) {
  return costMix.directMaterial + costMix.directLabor + costMix.machine + costMix.energy;
}

function contributionMarginRatio(totalCost, salesPrice = SALES_UNIT_PRICE) {
  return (salesPrice - totalCost) / salesPrice;
}

function contributionMarginDelta(referenceMargin, currentMargin) {
  return currentMargin - referenceMargin;
}

function computeComponentCostShare(componentCost, totalCost) {
  if (!totalCost || totalCost <= 0) return null;
  return componentCost / totalCost;
}

function computeSignedUnitCostChangeRatio(signedChange, referenceMedianUnitCost) {
  if (signedChange === null || signedChange === undefined || !referenceMedianUnitCost || referenceMedianUnitCost <= 0) {
    return null;
  }
  return signedChange / referenceMedianUnitCost;
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

function assertCostShareHonesty(run, label) {
  const componentResults = (run.results ?? []).filter((result) => result.resultDimension === "cost-component");
  for (const result of componentResults) {
    if (result.referenceMedianCostShare !== null && result.referenceMedianCostShare !== undefined) {
      assert(
        `${label} ${result.componentKind} reference share bounded`,
        result.referenceMedianCostShare >= 0 && result.referenceMedianCostShare <= 1,
        String(result.referenceMedianCostShare),
      );
    }
    if (result.currentMedianCostShare !== null && result.currentMedianCostShare !== undefined) {
      assert(
        `${label} ${result.componentKind} current share bounded`,
        result.currentMedianCostShare >= 0 && result.currentMedianCostShare <= 1,
        String(result.currentMedianCostShare),
      );
    }
    if (result.signedUnitCostChangeRatio !== null && result.signedUnitCostChangeRatio !== undefined) {
      assert(
        `${label} ${result.componentKind} unit cost ratio > -1`,
        result.signedUnitCostChangeRatio > -1,
        String(result.signedUnitCostChangeRatio),
      );
    }
  }
}

function assertMarginDeltaMath(run, label) {
  if (
    run.referenceMedianContributionMarginRatio === null
    || run.referenceMedianContributionMarginRatio === undefined
    || run.currentMedianContributionMarginRatio === null
    || run.currentMedianContributionMarginRatio === undefined
    || run.contributionMarginDelta === null
    || run.contributionMarginDelta === undefined
  ) {
    return;
  }
  const proofDelta = contributionMarginDelta(
    run.referenceMedianContributionMarginRatio,
    run.currentMedianContributionMarginRatio,
  );
  assertNear(`${label} contribution margin delta math`, run.contributionMarginDelta, proofDelta);
}

function removeFixturePrefix(prefix) {
  dbExec(`
    DELETE FROM commercial_profitability_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_profitability_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_profitability_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_profitability_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_profitability_comparison_runs WHERE idempotency_key LIKE '${prefix}%';
    DELETE FROM commercial_profitability_assessment_cost_components
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_profitability_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_profitability_assessments WHERE idempotency_key LIKE '${prefix}%';
    DELETE FROM commercial_profitability_evidence_observations WHERE ingest_idempotency_key LIKE '${prefix}%';
  `);
}

function cleanupVerifierFixtures() {
  for (const prefix of FIXTURE_PREFIXES) {
    removeFixturePrefix(prefix);
  }
  db(`
    DELETE FROM materials
    WHERE id IN (
      'bb000001-04f0-4000-8000-0000000004f0',
      'bb000001-04f1-4000-8000-0000000004f1',
      'bb000001-04f2-4000-8000-0000000004f2',
      'bb000001-04f3-4000-8000-0000000004f3',
      'bb000001-04f4-4000-8000-0000000004f4',
      'bb000001-04f5-4000-8000-0000000004f5',
      'bb000001-04f6-4000-8000-0000000004f6',
      'bb000001-04f7-4000-8000-0000000004f7'
    );
  `);
}

function assessmentGuid(offset) {
  return `bb000001-${offset.toString(16).padStart(4, "0")}-4000-8000-${offset.toString(16).padStart(12, "0")}`;
}

function closureFingerprint(assessmentId, suffix = "sales") {
  const kindAbbrev = {
    "direct-material": "dm",
    "direct-labor": "dl",
    machine: "mc",
    energy: "en",
    sales: "sales",
  };
  const normalized = assessmentId.replaceAll("-", "");
  return `cp2c-${normalized}-${kindAbbrev[suffix] ?? suffix}`;
}

function insertClosureMaterial(materialId, code) {
  const now = new Date().toISOString();
  db(`
    INSERT INTO materials (
      id, tenant_id, facility_id, code, code_normalized, name, material_category, polymer_family,
      manufacturer, trade_name_or_grade, color_name, physical_form, lifecycle_status, unit_of_measure,
      is_active, source_type, source_system, source_reference, data_recorded_at_utc,
      created_at_utc, updated_at_utc, created_by_subject_id, updated_by_subject_id, version)
    VALUES (
      '${materialId}', '${TENANT}', '${FACILITY}', '${code}', '${code}',
      'Closure profitability comparison material', 'virgin-polymer', 'PP', 'Pilot', '${code}', 'Doğal',
      'granule', 'active', 'kg', true, 'development-pilot', 'development-pilot', '${code.toLowerCase()}',
      '${now}', '${now}', '${now}', 'closure-verifier', 'closure-verifier', 1)
    ON CONFLICT (id) DO NOTHING;
  `);
}

function insertClosureAssessment({
  assessmentId,
  salesEvidenceId,
  idempotencyKey,
  materialId,
  customerReference,
  costMix,
  evaluatedAtUtc,
  outcome = "within-threshold",
  leadingDriverCode = "within-profile",
  evidenceStatus = "simulated",
  costCoverageStatus = "complete-required-cost-coverage",
  partialCoverage = false,
  sourceConflict = false,
  includeCostComponents = false,
}) {
  const evidenceTimestamp = new Date(new Date(evaluatedAtUtc).getTime() - 5 * 24 * 60 * 60 * 1000).toISOString();
  const setCode = `${idempotencyKey}-SET`;
  const salesFingerprint = closureFingerprint(assessmentId, "sales");
  const totalCost = partialCoverage ? totalCostFromMix({ ...costMix, energy: 0 }) : totalCostFromMix(costMix);
  const unitContribution = SALES_UNIT_PRICE - totalCost;
  const marginRatio = contributionMarginRatio(totalCost);
  const includedCount = partialCoverage ? 3 : 4;
  const missingCount = partialCoverage ? 1 : 0;
  const candidateCount = includedCount + missingCount;

  db(`
    INSERT INTO commercial_profitability_evidence_observations (
      id, tenant_id, facility_id, observation_kind, evidence_set_reference, customer_reference, material_id,
      quotation_reference, comparable_net_sales_unit_price, currency_code, unit_of_measure, gross_net_basis,
      tax_basis, discount_basis, freight_basis, source_kind, evidence_status, evidence_timestamp_utc,
      is_cancelled_or_returned, source_reference, source_provenance, source_fingerprint,
      ingest_idempotency_key, ingest_idempotency_payload_fingerprint, received_at_utc, created_at_utc, created_by)
    VALUES (
      '${salesEvidenceId}', '${TENANT}', '${FACILITY}', 'sales', '${setCode}', '${customerReference}', '${materialId}',
      'QTE-${idempotencyKey}', ${SALES_UNIT_PRICE}, 'TRY', 'kg', '${PRICE_BASIS_GROSS_NET}',
      'excluded', 'excluded', 'excluded', '${SOURCE_KIND}', '${evidenceStatus}', '${evidenceTimestamp}',
      false, 'SO-${idempotencyKey}', 'closure fixture', '${salesFingerprint}',
      '${idempotencyKey}', '${salesFingerprint}-fp', '${evidenceTimestamp}', '${evidenceTimestamp}', 'closure-verifier')
    ON CONFLICT (id) DO NOTHING;
  `);

  const costs = [
    { kind: "direct-material", unitCost: costMix.directMaterial },
    { kind: "direct-labor", unitCost: costMix.directLabor },
    { kind: "machine", unitCost: costMix.machine },
  ];
  if (!partialCoverage) {
    costs.push({ kind: "energy", unitCost: costMix.energy });
  }

  costs.forEach((entry, index) => {
    const costEvidenceId = assessmentGuid(0x510 + Number.parseInt(assessmentId.slice(-3), 16) + index);
    const componentEvidenceStatus = sourceConflict ? "source-conflict" : evidenceStatus;
    const costFingerprint = closureFingerprint(assessmentId, entry.kind);
    db(`
      INSERT INTO commercial_profitability_evidence_observations (
        id, tenant_id, facility_id, observation_kind, evidence_set_reference, customer_reference, material_id,
        component_kind, unit_cost, currency_code, unit_of_measure, gross_net_basis, tax_basis, discount_basis,
        freight_basis, cost_basis, source_kind, evidence_status, evidence_timestamp_utc, is_cancelled_or_returned,
        source_reference, source_provenance, source_fingerprint, ingest_idempotency_key,
        ingest_idempotency_payload_fingerprint, received_at_utc, created_at_utc, created_by)
      VALUES (
        '${costEvidenceId}', '${TENANT}', '${FACILITY}', 'cost-component', '${setCode}', '${customerReference}', '${materialId}',
        '${entry.kind}', ${entry.unitCost}, 'TRY', 'kg', '${PRICE_BASIS_GROSS_NET}', 'excluded', 'excluded', 'excluded',
        '${COST_BASIS}', '${SOURCE_KIND}', '${componentEvidenceStatus}', '${evidenceTimestamp}', false,
        'COST-${idempotencyKey}-${entry.kind}', 'closure fixture', '${costFingerprint}',
        '${idempotencyKey}-${entry.kind}', '${costFingerprint}-fp', '${evidenceTimestamp}', '${evidenceTimestamp}', 'closure-verifier')
      ON CONFLICT (id) DO NOTHING;
    `);

    if (includeCostComponents) {
      db(`
        INSERT INTO commercial_profitability_assessment_cost_components (
          id, tenant_id, facility_id, assessment_id, source_evidence_observation_id, component_kind, unit_cost,
          currency_code, unit_of_measure, cost_basis, evidence_timestamp_utc, source_kind, source_reference,
          evidence_status, inclusion_status, source_provenance, sort_order, created_at_utc)
        VALUES (
          gen_random_uuid(), '${TENANT}', '${FACILITY}', '${assessmentId}', '${costEvidenceId}', '${entry.kind}', ${entry.unitCost},
          'TRY', 'kg', '${COST_BASIS}', '${evidenceTimestamp}', '${SOURCE_KIND}', 'COST-${idempotencyKey}-${entry.kind}',
          '${componentEvidenceStatus}', 'included', 'closure fixture', ${index + 1}, '${evidenceTimestamp}')
        ON CONFLICT DO NOTHING;
      `);
    }
  });

  db(`
    INSERT INTO commercial_profitability_assessments (
      id, tenant_id, facility_id, assessment_reference, profile_id, profile_version, sales_evidence_observation_id,
      customer_reference, material_id, quotation_reference, evidence_set_reference, comparable_net_sales_unit_price,
      total_included_unit_cost, unit_contribution, contribution_margin_ratio, markup_ratio,
      candidate_cost_component_count, included_cost_component_count, excluded_cost_component_count,
      required_cost_component_count, available_required_cost_component_count, missing_required_cost_component_count,
      missing_required_component_kinds, cost_coverage_status, currency_code, unit_of_measure, gross_net_basis,
      tax_basis, discount_basis, freight_basis, evidence_status, outcome, leading_driver_code, rule_set_version,
      idempotency_key, idempotency_payload_fingerprint, source_provenance, evaluated_at_utc, created_at_utc, created_by)
    VALUES (
      '${assessmentId}', '${TENANT}', '${FACILITY}', 'ASM-${idempotencyKey}', '${PROFILE}', '1', '${salesEvidenceId}',
      '${customerReference}', '${materialId}', 'QTE-${idempotencyKey}', '${setCode}', ${SALES_UNIT_PRICE},
      ${totalCost}, ${unitContribution}, ${marginRatio}, ${totalCost <= 0 ? "NULL" : (unitContribution / totalCost)},
      ${candidateCount}, ${includedCount}, 0, 4, ${includedCount}, ${missingCount},
      ${partialCoverage ? "'energy'" : "''"}, '${costCoverageStatus}', 'TRY', 'kg', '${PRICE_BASIS_GROSS_NET}',
      'excluded', 'excluded', 'excluded', '${evidenceStatus}', '${outcome}', '${leadingDriverCode}',
      'COMMERCIAL-PROFITABILITY-1.0', '${idempotencyKey}', '${salesFingerprint}-fp', 'closure fixture',
      '${evaluatedAtUtc}', '${evaluatedAtUtc}', 'closure-verifier')
    ON CONFLICT (id) DO UPDATE SET
      evaluated_at_utc = EXCLUDED.evaluated_at_utc,
      outcome = EXCLUDED.outcome,
      leading_driver_code = EXCLUDED.leading_driver_code,
      evidence_status = EXCLUDED.evidence_status,
      cost_coverage_status = EXCLUDED.cost_coverage_status,
      contribution_margin_ratio = EXCLUDED.contribution_margin_ratio,
      total_included_unit_cost = EXCLUDED.total_included_unit_cost,
      unit_contribution = EXCLUDED.unit_contribution;
  `);
}

function seedAssessmentBatch({
  prefix,
  materialId,
  customerReference,
  windowKind,
  count,
  costMix,
  startIndex,
  outcome = "within-threshold",
  leadingDriverCode = "within-profile",
  evidenceStatus = "simulated",
  costCoverageStatus = "complete-required-cost-coverage",
  partialCoverage = false,
  sourceConflict = false,
  includeCostComponents = false,
}) {
  const windowStart = windowKind === "reference" ? REF_START : CUR_START;
  const windowEnd = windowKind === "reference" ? REF_END : CUR_END;
  const startMs = new Date(windowStart).getTime();
  const endMs = new Date(windowEnd).getTime();
  const stepMs = count > 1 ? (endMs - startMs) / (count - 1) : 0;
  for (let index = 0; index < count; index += 1) {
    const evaluatedAt = new Date(startMs + stepMs * index).toISOString();
    const suffix = `${index}`;
    const offset = 0x410 + startIndex + index;
    const evidenceOffset = 0x420 + startIndex + index;
    insertClosureAssessment({
      assessmentId: assessmentGuid(offset),
      salesEvidenceId: assessmentGuid(evidenceOffset),
      idempotencyKey: `${prefix}-${suffix}`,
      materialId,
      customerReference,
      costMix,
      evaluatedAtUtc: evaluatedAt,
      outcome,
      leadingDriverCode,
      evidenceStatus,
      costCoverageStatus,
      partialCoverage,
      sourceConflict,
      includeCostComponents,
    });
  }
}

function loadRunFromDb(runId) {
  const line = db(`
    SELECT comparison_outcome,
           COALESCE(contribution_margin_delta::text, ''),
           COALESCE(reference_median_contribution_margin_ratio::text, ''),
           COALESCE(current_median_contribution_margin_ratio::text, ''),
           COALESCE(leading_driver_code, ''),
           candidate_assessment_count,
           comparable_assessment_count,
           excluded_assessment_count
    FROM commercial_profitability_comparison_runs
    WHERE id = '${runId}'
  `);
  const parts = line.split("|");
  return {
    comparisonOutcome: parts[0],
    contributionMarginDelta: parts[1] === "" ? null : Number(parts[1]),
    referenceMedianContributionMarginRatio: parts[2] === "" ? null : Number(parts[2]),
    currentMedianContributionMarginRatio: parts[3] === "" ? null : Number(parts[3]),
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
  assertNear(`${label} db margin delta`, dbRun.contributionMarginDelta, apiRun.contributionMarginDelta);
  assertAssessmentCounts(dbRun, `${label} db`);
  assertAssessmentCounts(apiRun, `${label} api`);
}

console.log("Running COMMERCIAL-PROFITABILITY-2 trend/cost-scope verifier.");

const evaluator = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityComparisonEvaluator.cs");
const operations = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityComparisonOperations.cs");
const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialProfitabilityEndpointRouteBuilderExtensions.cs");
const comparisonSeed = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilityComparisonSeed.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
const provenance = readProvenance();

assert("temporal contribution trend evaluator", evaluator.includes("EvaluateTemporalContributionTrend"));
assert("cost component mix evaluator", evaluator.includes("EvaluateCostComponentMixChange"));
assert("customer contribution position evaluator", evaluator.includes("EvaluateCustomerContributionPosition"));
assert("material contribution position evaluator", evaluator.includes("EvaluateMaterialContributionPosition"));
assert("non-overlap validation", operations.includes("must not overlap"));
assert("comparison endpoints present", endpoints.includes("/comparisons"));
assert("direct-material-cost-increase driver", readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityComparisonContracts.cs").includes("direct-material-cost-increase"));
assert("contribution-deterioration outcome", evaluator.includes("ContributionDeterioration") || evaluator.includes("contribution-deterioration"));
assert("default adverse margin delta 0.05", constants.includes("DefaultProfitabilityMaximumAdverseContributionMarginDelta = 0.05m"));
assert("default favorable margin delta 0.05", constants.includes("DefaultProfitabilityMinimumFavorableContributionMarginDelta = 0.05m"));
assert("comparison seed stable pilot", comparisonSeed.includes("COMMPROFIT2-CMP-PILOT-STABLE"));
assert("comparison seed twelve scenarios", comparisonSeed.includes("commprofit2-pilot-source-conflict"));
assert("signed unit cost change ratio helper", evaluator.includes("ComputeSignedUnitCostChangeRatio"));
assert("component cost share helper", evaluator.includes("ComputeComponentCostShare"));

for (const relativePath of COMPARISON_SOURCE_FILES) {
  const source = readRequired(relativePath);
  forbidText(source, "guaranteed profit", relativePath);
  forbidText(source, "guaranteed savings", relativePath);
  forbidText(source, "recommendation", relativePath);
}

assertNear(
  "margin delta helper deterioration",
  contributionMarginDelta(contributionMarginRatio(totalCostFromMix(COST_MIXES.margin30)), contributionMarginRatio(totalCostFromMix(COST_MIXES.margin24))),
  -0.06,
);
assertNear(
  "margin delta helper improvement",
  contributionMarginDelta(contributionMarginRatio(totalCostFromMix(COST_MIXES.margin30)), contributionMarginRatio(totalCostFromMix(COST_MIXES.margin36))),
  0.06,
);
assertNear(
  "margin delta helper exact adverse",
  contributionMarginDelta(contributionMarginRatio(totalCostFromMix(COST_MIXES.margin30)), contributionMarginRatio(totalCostFromMix(COST_MIXES.margin25))),
  -0.05,
);
assertNear("cost share sum proof", computeComponentCostShare(35, 70) + computeComponentCostShare(15, 70) + computeComponentCostShare(10, 70) + computeComponentCostShare(10, 70), 1);
assertNear("unit cost ratio proof", computeSignedUnitCostChangeRatio(6, 35), 6 / 35);
assert("non-overlapping windows", !windowsOverlap(CUR_START, CUR_END, REF_START, REF_END));

async function main() {
  cleanupVerifierFixtures();

  try {
    const ready = await fetch(`${BASE}/health/ready`);
    assert("API ready", ready.status === 200, `status=${ready.status}`);

    const profileVersion = Number(db(`SELECT version FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`));
    assert("pilot profile version present", profileVersion >= 1, String(profileVersion));

    const adverseDelta = db(`SELECT maximum_adverse_contribution_margin_delta FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`);
    const favorableDelta = db(`SELECT minimum_favorable_contribution_margin_delta FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`);
    assert("pilot maximumAdverseContributionMarginDelta 0.05", adverseDelta === "0.050000", adverseDelta);
    assert("pilot minimumFavorableContributionMarginDelta 0.05", favorableDelta === "0.050000", favorableDelta);

    async function getPilotRun(runId) {
      const response = await liveRequest(
        "GET",
        `/api/commercial-intelligence/profitability/comparisons/${runId}?facilityId=${FACILITY}`,
      );
      assert(`pilot run ${runId} 200`, response.status === 200, response.text?.slice(0, 200));
      forbidText(response.text ?? "", "guaranteed profit", `pilot ${runId}`);
      forbidText(response.text ?? "", "recommendation", `pilot ${runId}`);
      return response.json;
    }

    for (const [name, scenario] of Object.entries(PILOT_RUNS)) {
      const pilot = await getPilotRun(scenario.id);
      assert(`${name} outcome`, pilot.comparisonOutcome === scenario.outcome, pilot.comparisonOutcome);
      assertAssessmentCounts(pilot, name);
      assertMarginDeltaMath(pilot, name);
      if (scenario.leadingDriver) {
        assert(`${name} leading driver`, pilot.leadingDriverCode === scenario.leadingDriver, pilot.leadingDriverCode);
      }
      if (scenario.expectedDelta !== undefined) {
        assertNear(`${name} contribution margin delta`, pilot.contributionMarginDelta, scenario.expectedDelta);
      }
      if (scenario.comparisonType) {
        assert(`${name} comparison type`, pilot.comparisonType === scenario.comparisonType, pilot.comparisonType);
      }
      if (scenario.driver) {
        assert(`${name} driver present`, pilot.drivers.some((driver) => driver.reasonCode === scenario.driver));
      }
      if (scenario.customerResultCount) {
        assert(
          `${name} customer results`,
          pilot.results.filter((result) => result.resultDimension === "customer").length === scenario.customerResultCount,
        );
      }
      if (scenario.materialResultCount) {
        assert(
          `${name} material results`,
          pilot.results.filter((result) => result.resultDimension === "material").length === scenario.materialResultCount,
        );
      }
      if (scenario.tiedResultCount) {
        assert(`${name} tied results`, pilot.results.filter((result) => result.isTied).length === scenario.tiedResultCount);
      }
      if (name === "directMaterial" || name === "energy") {
        assertCostShareHonesty(pilot, name);
      }
    }

    const pilotRunIds = Object.values(PILOT_RUNS).map((scenario) => scenario.id).join("','");
    const pilotRunCount = Number(db(`SELECT COUNT(*) FROM commercial_profitability_comparison_runs WHERE id IN ('${pilotRunIds}')`));
    assert("twelve pilot comparison runs", pilotRunCount === 12, `count=${pilotRunCount}`);

    async function evaluateComparison(name, body, extra = {}) {
      const response = await liveRequest(
        "POST",
        `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`,
        body,
      );
      forbidText(response.text ?? "", "guaranteed profit", `${name} response`);
      forbidText(response.text ?? "", "recommendation", `${name} response`);
      if (extra.expectReject) {
        assert(`${name} rejected`, response.status !== 201, `status=${response.status}`);
        return response;
      }
      assert(`${name} evaluate 201`, response.status === 201, response.text?.slice(0, 300));
      return response.json.comparison;
    }

    function temporalBody(materialId, refStart, refEnd, curStart, curEnd, idempotencyKey, comparisonType = "temporal-contribution-trend") {
      return {
        profileId: PROFILE,
        expectedProfileVersion: profileVersion,
        comparisonType,
        materialId,
        currentWindowStartUtc: curStart,
        currentWindowEndUtc: curEnd,
        referenceWindowStartUtc: refStart,
        referenceWindowEndUtc: refEnd,
        sourceProvenance: provenance,
        idempotencyKey,
      };
    }

    const closureMaterialStable = "bb000001-04f0-4000-8000-0000000004f0";
    const closureMaterialDeterioration = "bb000001-04f1-4000-8000-0000000004f1";
    const closureMaterialImprovement = "bb000001-04f2-4000-8000-0000000004f2";
    const closureMaterialExactAdverse = "bb000001-04f3-4000-8000-0000000004f3";
    const closureMaterialExactFavorable = "bb000001-04f4-4000-8000-0000000004f4";
    const closureMaterialInsufficient = "bb000001-04f5-4000-8000-0000000004f5";
    const closureMaterialConflict = "bb000001-04f6-4000-8000-0000000004f6";

    for (const [materialId, code] of [
      [closureMaterialStable, "COMMPROFIT2-MAT-CLOSURE-STABLE"],
      [closureMaterialDeterioration, "COMMPROFIT2-MAT-CLOSURE-DETERIORATION"],
      [closureMaterialImprovement, "COMMPROFIT2-MAT-CLOSURE-IMPROVEMENT"],
      [closureMaterialExactAdverse, "COMMPROFIT2-MAT-CLOSURE-EXACT-ADV"],
      [closureMaterialExactFavorable, "COMMPROFIT2-MAT-CLOSURE-EXACT-FAV"],
      [closureMaterialInsufficient, "COMMPROFIT2-MAT-CLOSURE-INSUFFICIENT"],
      [closureMaterialConflict, "COMMPROFIT2-MAT-CLOSURE-CONFLICT"],
    ]) {
      insertClosureMaterial(materialId, code);
    }

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-ST-R`,
      materialId: closureMaterialStable,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 0,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-ST-C`,
      materialId: closureMaterialStable,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 10,
    });
    const stable = await evaluateComparison(
      "stable-trend",
      temporalBody(closureMaterialStable, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-STAB`),
    );
    assert("stable outcome", stable.comparisonOutcome === "within-comparison-band", stable.comparisonOutcome);
    assertNear("stable margin delta", stable.contributionMarginDelta, 0);
    reconcileRun(stable, "stable");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-DE-R`,
      materialId: closureMaterialDeterioration,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 20,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-DE-C`,
      materialId: closureMaterialDeterioration,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      costMix: COST_MIXES.margin24,
      startIndex: 30,
    });
    const deterioration = await evaluateComparison(
      "contribution-deterioration",
      temporalBody(closureMaterialDeterioration, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-DET`),
    );
    assert("deterioration outcome", deterioration.comparisonOutcome === "contribution-deterioration", deterioration.comparisonOutcome);
    assertNear("deterioration margin delta", deterioration.contributionMarginDelta, -0.06);
    assert(
      "deterioration beyond adverse threshold",
      deterioration.contributionMarginDelta < -MARGIN_DELTA_THRESHOLD,
      String(deterioration.contributionMarginDelta),
    );
    reconcileRun(deterioration, "deterioration");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-IM-R`,
      materialId: closureMaterialImprovement,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 40,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-IM-C`,
      materialId: closureMaterialImprovement,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      costMix: COST_MIXES.margin36,
      startIndex: 50,
    });
    const improvement = await evaluateComparison(
      "contribution-improvement",
      temporalBody(closureMaterialImprovement, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-IMP`),
    );
    assert("improvement outcome", improvement.comparisonOutcome === "contribution-improvement", improvement.comparisonOutcome);
    assertNear("improvement margin delta", improvement.contributionMarginDelta, 0.06);
    assert(
      "improvement beyond favorable threshold",
      improvement.contributionMarginDelta > MARGIN_DELTA_THRESHOLD,
      String(improvement.contributionMarginDelta),
    );
    reconcileRun(improvement, "improvement");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-EA-R`,
      materialId: closureMaterialExactAdverse,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 60,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-EA-C`,
      materialId: closureMaterialExactAdverse,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      costMix: COST_MIXES.margin25,
      startIndex: 70,
    });
    const exactAdverse = await evaluateComparison(
      "exact-adverse-threshold",
      temporalBody(closureMaterialExactAdverse, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-EXA`),
    );
    assert("exact adverse outcome", exactAdverse.comparisonOutcome === "within-comparison-band", exactAdverse.comparisonOutcome);
    assertNear("exact adverse margin delta", exactAdverse.contributionMarginDelta, -0.05);
    reconcileRun(exactAdverse, "exact-adverse");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-EF-R`,
      materialId: closureMaterialExactFavorable,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 80,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-EF-C`,
      materialId: closureMaterialExactFavorable,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      costMix: COST_MIXES.margin35,
      startIndex: 90,
    });
    const exactFavorable = await evaluateComparison(
      "exact-favorable-threshold",
      temporalBody(closureMaterialExactFavorable, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-EXF`),
    );
    assert("exact favorable outcome", exactFavorable.comparisonOutcome === "within-comparison-band", exactFavorable.comparisonOutcome);
    assertNear("exact favorable margin delta", exactFavorable.contributionMarginDelta, 0.05);
    reconcileRun(exactFavorable, "exact-favorable");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-IN-R`,
      materialId: closureMaterialInsufficient,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 100,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-IN-C`,
      materialId: closureMaterialInsufficient,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "current",
      count: 2,
      costMix: COST_MIXES.margin30,
      startIndex: 110,
    });
    const insufficient = await evaluateComparison(
      "insufficient-data",
      temporalBody(closureMaterialInsufficient, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-INS`),
    );
    assert("insufficient outcome", insufficient.comparisonOutcome === "insufficient-data", insufficient.comparisonOutcome);
    reconcileRun(insufficient, "insufficient");

    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-CO-R`,
      materialId: closureMaterialConflict,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "reference",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 120,
    });
    seedAssessmentBatch({
      prefix: `${CLOSURE_PREFIX}-CO-C`,
      materialId: closureMaterialConflict,
      customerReference: "COMMPROFIT2-CUST-CLOSURE",
      windowKind: "current",
      count: 6,
      costMix: COST_MIXES.margin30,
      startIndex: 130,
      outcome: "source-conflict",
      leadingDriverCode: "source-conflict",
      evidenceStatus: "source-conflict",
      costCoverageStatus: "source-conflict",
      sourceConflict: true,
    });
    const conflict = await evaluateComparison(
      "source-conflict",
      temporalBody(closureMaterialConflict, REF_START, REF_END, CUR_START, CUR_END, `${CLOSURE_PREFIX}-CON`),
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

  console.log("COMMERCIAL-PROFITABILITY-2 trend/cost-scope verification passed.");
}

main().catch((error) => {
  console.error(`COMMERCIAL-PROFITABILITY-2 trend/cost-scope verification failed: ${error?.message ?? error}`);
  process.exit(1);
});

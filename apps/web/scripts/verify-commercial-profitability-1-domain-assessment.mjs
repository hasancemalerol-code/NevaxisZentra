#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL_BOUNDARY = "bb000001-031b-4000-8000-00000000031b";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const BOUNDARY_PREFIX = "COMMPROFIT1-BOUNDARY";
const CLOSURE_PREFIX = "COMMPROFIT1-CLOSURE";
const SOURCE_KIND = "manual-development-simulator";
const COST_BASIS = "net";
const PRICE_BASIS_GROSS_NET = "net";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const PROFITABILITY_SOURCE_FILES = [
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityEvaluator.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityCostEvidenceReader.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityRepository.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityModels.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityEntities.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityEntityConfigurations.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityIdempotencyFingerprint.cs",
  "packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityContracts.cs",
  "packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilitySeed.cs",
  "apps/api/CommercialIntelligence/CommercialProfitabilityEndpointRouteBuilderExtensions.cs",
];

const PILOT_SCENARIOS = [
  {
    id: "bb000001-0391-4000-8000-000000000391",
    name: "within-threshold",
    outcome: "within-threshold",
    leadingDriver: "within-profile",
    salesPrice: 100,
    totalCost: 70,
    marginRatio: 0.3,
    markupRatio: 30 / 70,
    includedCostCount: 4,
    excludedCostCount: 0,
    costCoverageStatus: "complete-required-cost-coverage",
  },
  {
    id: "bb000001-0392-4000-8000-000000000392",
    name: "exact-threshold",
    outcome: "within-threshold",
    leadingDriver: "within-profile",
    salesPrice: 100,
    totalCost: 85,
    marginRatio: 0.15,
    markupRatio: 15 / 85,
    includedCostCount: 4,
    excludedCostCount: 0,
    costCoverageStatus: "complete-required-cost-coverage",
  },
  {
    id: "bb000001-0393-4000-8000-000000000393",
    name: "below-threshold",
    outcome: "below-threshold",
    leadingDriver: "contribution-margin-below-threshold",
    salesPrice: 100,
    totalCost: 86,
    marginRatio: 0.14,
    markupRatio: 14 / 86,
    includedCostCount: 4,
    excludedCostCount: 0,
    costCoverageStatus: "complete-required-cost-coverage",
  },
  {
    id: "bb000001-0394-4000-8000-000000000394",
    name: "zero-contribution",
    outcome: "below-threshold",
    leadingDriver: "contribution-margin-below-threshold",
    salesPrice: 100,
    totalCost: 100,
    marginRatio: 0,
    markupRatio: 0,
    includedCostCount: 4,
    excludedCostCount: 0,
    costCoverageStatus: "complete-required-cost-coverage",
  },
  {
    id: "bb000001-0395-4000-8000-000000000395",
    name: "negative-contribution",
    outcome: "negative-contribution",
    leadingDriver: "negative-unit-contribution",
    salesPrice: 100,
    totalCost: 110,
    marginRatio: -0.1,
    markupRatio: -10 / 110,
    includedCostCount: 4,
    excludedCostCount: 0,
    costCoverageStatus: "complete-required-cost-coverage",
  },
  {
    id: "bb000001-0396-4000-8000-000000000396",
    name: "missing-cost",
    outcome: "insufficient-data",
    leadingDriver: "missing-energy-cost",
    salesPrice: 100,
    totalCost: 70,
    includedCostCount: 3,
    excludedCostCount: 0,
    missingRequiredCount: 1,
    costCoverageStatus: "partial-cost-coverage",
  },
  {
    id: "bb000001-0397-4000-8000-000000000397",
    name: "source-conflict",
    outcome: "source-conflict",
    leadingDriver: "source-conflict",
    salesPrice: 100,
    totalCost: 60,
    includedCostCount: 3,
    excludedCostCount: 1,
    costCoverageStatus: "source-conflict",
  },
  {
    id: "bb000001-0398-4000-8000-000000000398",
    name: "incompatible",
    outcome: "within-threshold",
    leadingDriver: "incompatible-currency",
    salesPrice: 100,
    totalCost: 70,
    marginRatio: 0.3,
    markupRatio: 30 / 70,
    includedCostCount: 4,
    excludedCostCount: 1,
    costCoverageStatus: "complete-required-cost-coverage",
  },
  {
    id: "bb000001-0399-4000-8000-000000000399",
    name: "stale-evidence",
    outcome: "insufficient-data",
    leadingDriver: "stale-cost-evidence",
    salesPrice: 100,
    totalCost: 0,
    includedCostCount: 0,
    excludedCostCount: 4,
    missingRequiredCount: 4,
    costCoverageStatus: "insufficient-cost-evidence",
  },
  {
    id: "bb000001-039a-4000-8000-00000000039a",
    name: "margin-markup",
    outcome: "within-threshold",
    leadingDriver: "within-profile",
    salesPrice: 100,
    totalCost: 70,
    marginRatio: 0.3,
    markupRatio: 30 / 70,
    includedCostCount: 4,
    excludedCostCount: 0,
    costCoverageStatus: "complete-required-cost-coverage",
  },
];

const COMPONENT_KINDS = ["direct-material", "direct-labor", "machine", "energy"];

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-1 domain-assessment verification failed: ${message}`);
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

function computeUnitContribution(salesPrice, totalCost) {
  return salesPrice - totalCost;
}

function computeContributionMarginRatio(unitContribution, salesPrice) {
  if (!salesPrice || salesPrice <= 0) return null;
  return unitContribution / salesPrice;
}

function computeMarkupRatio(unitContribution, totalCost) {
  if (!totalCost || totalCost <= 0) return null;
  return unitContribution / totalCost;
}

function distributeUnitCosts(totalCost) {
  if (totalCost <= 0) {
    return COMPONENT_KINDS.map((kind) => ({ kind, unitCost: 0 }));
  }
  if (totalCost < 4) {
    const each = totalCost / 4;
    return COMPONENT_KINDS.map((kind) => ({ kind, unitCost: each }));
  }
  const remainder = totalCost - 3;
  return [
    { kind: "direct-material", unitCost: remainder },
    { kind: "direct-labor", unitCost: 1 },
    { kind: "machine", unitCost: 1 },
    { kind: "energy", unitCost: 1 },
  ];
}

function assertCostCoverageCounts(assessment, label) {
  assert(
    `${label} candidate = included + excluded`,
    assessment.candidateCostComponentCount
      === assessment.includedCostComponentCount + assessment.excludedCostComponentCount,
    `candidate=${assessment.candidateCostComponentCount} included=${assessment.includedCostComponentCount} excluded=${assessment.excludedCostComponentCount}`,
  );
  assert(
    `${label} required denominator reconciliation`,
    assessment.availableRequiredCostComponentCount + assessment.missingRequiredCostComponentCount
      === assessment.requiredCostComponentCount,
    `available=${assessment.availableRequiredCostComponentCount} missing=${assessment.missingRequiredCostComponentCount} required=${assessment.requiredCostComponentCount}`,
  );
}

function assertDriverOrdering(assessment, label) {
  const drivers = assessment.drivers ?? [];
  assert(`${label} drivers returned`, drivers.length > 0);
  const leadingDrivers = drivers.filter((driver) => driver.isLeadingDriver);
  assert(`${label} exactly one leading driver`, leadingDrivers.length === 1, `count=${leadingDrivers.length}`);
  assert(`${label} leading driver matches code`, leadingDrivers[0].reasonCode === assessment.leadingDriverCode);
  const sortOrders = drivers.map((driver) => driver.sortOrder);
  const sorted = [...sortOrders].sort((a, b) => a - b);
  assert(`${label} driver sort order ascending`, JSON.stringify(sortOrders) === JSON.stringify(sorted));
}

function removeBoundaryFixtures() {
  db(`
    DELETE FROM commercial_profitability_assessment_cost_components
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments
      WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
         OR idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_profitability_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments
      WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
         OR idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_profitability_assessments
    WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
       OR idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_profitability_evidence_observations
    WHERE ingest_idempotency_key LIKE '${BOUNDARY_PREFIX}%'
       OR ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

function boundaryFixtureCounts() {
  return {
    assessments: Number(db(`SELECT COUNT(*) FROM commercial_profitability_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'`)),
    drivers: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_assessment_drivers
      WHERE assessment_id IN (
        SELECT id FROM commercial_profitability_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
      )
    `)),
    components: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_assessment_cost_components
      WHERE assessment_id IN (
        SELECT id FROM commercial_profitability_assessments WHERE idempotency_key LIKE '${BOUNDARY_PREFIX}%'
      )
    `)),
    evidence: Number(db(`SELECT COUNT(*) FROM commercial_profitability_evidence_observations WHERE ingest_idempotency_key LIKE '${BOUNDARY_PREFIX}%'`)),
    orphanDrivers: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_assessment_drivers d
      WHERE NOT EXISTS (SELECT 1 FROM commercial_profitability_assessments a WHERE a.id = d.assessment_id)
    `)),
  };
}

function insertBoundaryMaterial() {
  const now = new Date().toISOString();
  db(`
    INSERT INTO materials (
      id, tenant_id, facility_id, code, code_normalized, name, description, material_category,
      polymer_family, manufacturer, trade_name_or_grade, color_name, physical_form, lifecycle_status,
      unit_of_measure, is_active, source_type, source_system, source_reference, data_recorded_at_utc,
      created_at_utc, updated_at_utc, created_by_subject_id, updated_by_subject_id, version)
    VALUES (
      '${MATERIAL_BOUNDARY}', '${TENANT}', '${FACILITY}', '${BOUNDARY_PREFIX}-MAT', '${BOUNDARY_PREFIX}-MAT',
      'Boundary commercial profitability material', 'Boundary verifier material', 'virgin-polymer',
      'PP', 'Boundary Polimer', '${BOUNDARY_PREFIX}-MAT', 'Doğal', 'granule', 'active',
      'kg', true, 'development-pilot', 'boundary-verifier', '${BOUNDARY_PREFIX}-mat', '${now}',
      '${now}', '${now}', 'boundary-verifier', 'boundary-verifier', 1)
    ON CONFLICT (id) DO NOTHING;
  `);
}

function insertBoundarySalesEvidence({
  evidenceId,
  idempotencyKey,
  setCode,
  salesPrice,
  customerReference = "COMMPROFIT1-CUST-BOUNDARY",
}) {
  const evidenceTimestamp = new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString();
  const salesPriceClause = salesPrice === null ? "NULL" : salesPrice;
  db(`
    INSERT INTO commercial_profitability_evidence_observations (
      id, tenant_id, facility_id, observation_kind, evidence_set_reference, customer_reference, material_id,
      quotation_reference, comparable_net_sales_unit_price, currency_code, unit_of_measure, gross_net_basis,
      tax_basis, discount_basis, freight_basis, source_kind, evidence_status, evidence_timestamp_utc,
      is_cancelled_or_returned, source_reference, source_provenance, source_fingerprint,
      ingest_idempotency_key, ingest_idempotency_payload_fingerprint, received_at_utc, created_at_utc, created_by)
    VALUES (
      '${evidenceId}', '${TENANT}', '${FACILITY}', 'sales', '${setCode}', '${customerReference}', '${MATERIAL_BOUNDARY}',
      'QTE-${idempotencyKey}', ${salesPriceClause}, 'TRY', 'kg', '${PRICE_BASIS_GROSS_NET}',
      'excluded', 'excluded', 'excluded', '${SOURCE_KIND}', 'simulated', '${evidenceTimestamp}',
      false, 'SO-${idempotencyKey}', 'boundary fixture', 'commprofit1-boundary-${evidenceId.replaceAll("-", "")}',
      '${idempotencyKey}', '${idempotencyKey}-fp', '${evidenceTimestamp}', '${evidenceTimestamp}', 'boundary-verifier')
    ON CONFLICT (id) DO NOTHING;
  `);
}

function insertBoundaryCostEvidence({
  evidenceId,
  idempotencyKey,
  setCode,
  componentKind,
  unitCost,
  evidenceStatus = "simulated",
}) {
  const evidenceTimestamp = new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString();
  db(`
    INSERT INTO commercial_profitability_evidence_observations (
      id, tenant_id, facility_id, observation_kind, evidence_set_reference, customer_reference, material_id,
      component_kind, unit_cost, currency_code, unit_of_measure, gross_net_basis, tax_basis, discount_basis,
      freight_basis, cost_basis, source_kind, evidence_status, evidence_timestamp_utc, is_cancelled_or_returned,
      source_reference, source_provenance, source_fingerprint, ingest_idempotency_key,
      ingest_idempotency_payload_fingerprint, received_at_utc, created_at_utc, created_by)
    VALUES (
      '${evidenceId}', '${TENANT}', '${FACILITY}', 'cost-component', '${setCode}', 'COMMPROFIT1-CUST-BOUNDARY', '${MATERIAL_BOUNDARY}',
      '${componentKind}', ${unitCost}, 'TRY', 'kg', '${PRICE_BASIS_GROSS_NET}', 'excluded', 'excluded', 'excluded',
      '${COST_BASIS}', '${SOURCE_KIND}', '${evidenceStatus}', '${evidenceTimestamp}', false,
      'COST-${idempotencyKey}-${componentKind}', 'boundary fixture', 'commprofit1-boundary-${evidenceId.replaceAll("-", "")}',
      '${idempotencyKey}-${componentKind}', '${idempotencyKey}-${componentKind}-fp', '${evidenceTimestamp}', '${evidenceTimestamp}', 'boundary-verifier')
    ON CONFLICT (id) DO NOTHING;
  `);
}

function insertBoundaryCostBundle({
  setCode,
  idempotencyKey,
  totalCost,
  evidenceStatus = "simulated",
  costEvidenceIds,
}) {
  const costs = distributeUnitCosts(totalCost);
  costs.forEach((entry, index) => {
    insertBoundaryCostEvidence({
      evidenceId: costEvidenceIds[index],
      idempotencyKey: `${idempotencyKey}-${entry.kind}`,
      setCode,
      componentKind: entry.kind,
      unitCost: entry.unitCost,
      evidenceStatus,
    });
  });
}

console.log("Running COMMERCIAL-PROFITABILITY-1 domain-assessment verifier.");

const evaluator = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityEvaluator.cs");
const reader = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityCostEvidenceReader.cs");
const constants = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
const seed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilitySeed.cs");

assert("total included cost helper present", evaluator.includes("ComputeTotalIncludedUnitCost"));
assert("unit contribution helper present", evaluator.includes("ComputeUnitContribution"));
assert("contribution margin helper present", evaluator.includes("ComputeContributionMarginRatio"));
assert("markup helper present", evaluator.includes("ComputeMarkupRatio"));
assert("driver ordering helper present", evaluator.includes("OrderDrivers"));
assert("default min margin 0.15", constants.includes("DefaultProfitabilityMinimumContributionMarginRatio = 0.15m"));
assert("default max evidence age 180", constants.includes("DefaultProfitabilityMaximumEvidenceAgeDays = 180"));
assert("cost evidence reader present", reader.includes("ResolveExclusionReason"));
assert("seed bootstrap present", seed.includes("EnsureCommercialProfitabilityPilotRecordsAsync"));
assert("seed within-threshold fixture present", seed.includes("COMMPROFIT1-SET-WITHIN"));

for (const relativePath of PROFITABILITY_SOURCE_FILES) {
  const source = readRequired(relativePath);
  forbidText(source, "recommendation", relativePath);
  forbidText(source, "guaranteed savings", relativePath);
  forbidText(source, "decision", relativePath);
}

assertNear("margin math proof 100/70", computeContributionMarginRatio(30, 100), 0.3);
assertNear("markup math proof 100/70", computeMarkupRatio(30, 70), 30 / 70);
assertNear("markup math proof approximate 0.428571", computeMarkupRatio(30, 70), 0.428571);
assert("zero sales guard", computeContributionMarginRatio(10, 0) === null);
assert("zero cost markup guard", computeMarkupRatio(10, 0) === null);

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const profileVersion = Number(db(`SELECT version FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`));
assert("pilot profile version present", profileVersion >= 1, String(profileVersion));

const provenance =
  "Deterministic local Development contribution evidence — simulated, non-production, not real customer, quotation, invoice, order, production, cost, accounting, or market evidence.";

async function evaluateBoundary({
  name,
  evidenceId,
  idempotencyKey,
  expectReject = false,
}) {
  const response = await liveRequest(
    "POST",
    `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`,
    {
      profileId: PROFILE,
      expectedProfileVersion: profileVersion,
      salesEvidenceObservationId: evidenceId,
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
  assertCostCoverageCounts(assessment, name);
  assertDriverOrdering(assessment, name);
  return assessment;
}

function assertDbReconciliation(idempotencyKey, assessment) {
  const row = db(`
    SELECT outcome, leading_driver_code, comparable_net_sales_unit_price::text, total_included_unit_cost::text,
           unit_contribution::text, contribution_margin_ratio::text, markup_ratio::text,
           candidate_cost_component_count::text, included_cost_component_count::text, excluded_cost_component_count::text,
           available_required_cost_component_count::text, missing_required_cost_component_count::text,
           cost_coverage_status
    FROM commercial_profitability_assessments
    WHERE idempotency_key = '${idempotencyKey}'
  `);
  const [
    outcome,
    leadingDriver,
    salesPrice,
    totalCost,
    unitContribution,
    marginRatio,
    markupRatio,
    candidate,
    included,
    excluded,
    availableRequired,
    missingRequired,
    costCoverageStatus,
  ] = row.split("|");
  assert(`db reconciliation outcome ${idempotencyKey}`, outcome === assessment.outcome, `${outcome} vs ${assessment.outcome}`);
  assert(`db reconciliation leading driver ${idempotencyKey}`, leadingDriver === assessment.leadingDriverCode, `${leadingDriver} vs ${assessment.leadingDriverCode}`);
  assertNear(`db reconciliation sales price ${idempotencyKey}`, Number(salesPrice), assessment.comparableNetSalesUnitPrice);
  assertNear(`db reconciliation total cost ${idempotencyKey}`, Number(totalCost), assessment.totalIncludedUnitCost);
  assertNear(`db reconciliation unit contribution ${idempotencyKey}`, Number(unitContribution), assessment.unitContribution);
  if (assessment.contributionMarginRatio !== null && assessment.contributionMarginRatio !== undefined) {
    assertNear(`db reconciliation margin ${idempotencyKey}`, Number(marginRatio), assessment.contributionMarginRatio);
  }
  if (assessment.markupRatio !== null && assessment.markupRatio !== undefined) {
    assertNear(`db reconciliation markup ${idempotencyKey}`, Number(markupRatio), assessment.markupRatio);
  }
  assert(`db reconciliation candidate counts ${idempotencyKey}`, candidate === String(assessment.candidateCostComponentCount));
  assert(`db reconciliation included counts ${idempotencyKey}`, included === String(assessment.includedCostComponentCount));
  assert(`db reconciliation excluded counts ${idempotencyKey}`, excluded === String(assessment.excludedCostComponentCount));
  assert(`db reconciliation available required ${idempotencyKey}`, availableRequired === String(assessment.availableRequiredCostComponentCount));
  assert(`db reconciliation missing required ${idempotencyKey}`, missingRequired === String(assessment.missingRequiredCostComponentCount));
  assert(
    `db reconciliation denominator honesty ${idempotencyKey}`,
    Number(candidate) === Number(included) + Number(excluded),
    `${candidate} != ${included} + ${excluded}`,
  );
  assert(
    `db reconciliation required denominator ${idempotencyKey}`,
    Number(availableRequired) + Number(missingRequired) === assessment.requiredCostComponentCount,
    `${availableRequired} + ${missingRequired} != ${assessment.requiredCostComponentCount}`,
  );
  assert(`db reconciliation cost coverage ${idempotencyKey}`, costCoverageStatus === assessment.costCoverageStatus, `${costCoverageStatus} vs ${assessment.costCoverageStatus}`);
}

const preBoundaryCounts = boundaryFixtureCounts();
assert("pre-setup boundary assessments zero", preBoundaryCounts.assessments === 0, `count=${preBoundaryCounts.assessments}`);
assert("pre-setup boundary evidence zero", preBoundaryCounts.evidence === 0, `count=${preBoundaryCounts.evidence}`);

try {
  for (const scenario of PILOT_SCENARIOS) {
    const response = await liveRequest(
      "GET",
      `/api/commercial-intelligence/profitability/assessments/${scenario.id}?facilityId=${FACILITY}`,
    );
    assert(`${scenario.name} pilot GET 200`, response.status === 200, response.text?.slice(0, 200));
    forbidText(response.text ?? "", "recommendation", `${scenario.name} response`);
    forbidText(response.text ?? "", "decision", `${scenario.name} response`);
    const assessment = response.json;
    assert(`${scenario.name} outcome`, assessment.outcome === scenario.outcome, assessment.outcome);
    assert(`${scenario.name} leading driver`, assessment.leadingDriverCode === scenario.leadingDriver, assessment.leadingDriverCode);
    assertCostCoverageCounts(assessment, scenario.name);
    assertDriverOrdering(assessment, scenario.name);
    if (scenario.totalCost !== undefined) {
      assertNear(`${scenario.name} total included cost`, assessment.totalIncludedUnitCost, scenario.totalCost);
      const proofContribution = computeUnitContribution(scenario.salesPrice, scenario.totalCost);
      assertNear(`${scenario.name} unit contribution proof`, assessment.unitContribution, proofContribution);
    }
    if (scenario.marginRatio !== undefined) {
      assertNear(`${scenario.name} contribution margin ratio`, assessment.contributionMarginRatio, scenario.marginRatio);
      const proofMargin = computeContributionMarginRatio(assessment.unitContribution, scenario.salesPrice);
      assertNear(`${scenario.name} margin math proof`, assessment.contributionMarginRatio, proofMargin);
    }
    if (scenario.markupRatio !== undefined) {
      assertNear(`${scenario.name} markup ratio`, assessment.markupRatio, scenario.markupRatio);
      const proofMarkup = computeMarkupRatio(assessment.unitContribution, assessment.totalIncludedUnitCost);
      assertNear(`${scenario.name} markup math proof`, assessment.markupRatio, proofMarkup);
    }
    if (scenario.includedCostCount !== undefined) {
      assert(`${scenario.name} included cost count`, assessment.includedCostComponentCount === scenario.includedCostCount, String(assessment.includedCostComponentCount));
    }
    if (scenario.excludedCostCount !== undefined) {
      assert(`${scenario.name} excluded cost count`, assessment.excludedCostComponentCount === scenario.excludedCostCount, String(assessment.excludedCostComponentCount));
    }
    if (scenario.missingRequiredCount !== undefined) {
      assert(`${scenario.name} missing required count`, assessment.missingRequiredCostComponentCount === scenario.missingRequiredCount, String(assessment.missingRequiredCostComponentCount));
    }
    if (scenario.costCoverageStatus !== undefined) {
      assert(`${scenario.name} cost coverage status`, assessment.costCoverageStatus === scenario.costCoverageStatus, assessment.costCoverageStatus);
    }
    if (scenario.name === "margin-markup") {
      assertNear(`${scenario.name} margin vs markup distinction margin`, assessment.contributionMarginRatio, 0.3);
      assertNear(`${scenario.name} margin vs markup distinction markup`, assessment.markupRatio, 0.428571);
      assert(
        `${scenario.name} margin not equal markup`,
        Math.abs(assessment.contributionMarginRatio - assessment.markupRatio) > 0.05,
        `${assessment.contributionMarginRatio} vs ${assessment.markupRatio}`,
      );
    }
    const dbOutcome = db(`SELECT outcome FROM commercial_profitability_assessments WHERE id = '${scenario.id}'`);
    assert(`${scenario.name} api/db outcome reconciliation`, dbOutcome === assessment.outcome, dbOutcome);
  }

  insertBoundaryMaterial();

  const profiles = await liveRequest("GET", `/api/commercial-intelligence/profitability/profiles?facilityId=${FACILITY}`);
  const pilotProfile = profiles.json?.items?.find((item) => item.id === PROFILE);
  assert("pilot minimumContributionMarginRatio 0.15", pilotProfile?.minimumContributionMarginRatio === 0.15, String(pilotProfile?.minimumContributionMarginRatio));
  assert("pilot maximumEvidenceAgeDays 180", pilotProfile?.maximumEvidenceAgeDays === 180, String(pilotProfile?.maximumEvidenceAgeDays));
  assert("pilot required cost kinds", pilotProfile?.requiredCostComponentKinds === "direct-material,direct-labor,machine,energy", pilotProfile?.requiredCostComponentKinds);

  const boundaryCases = [
    {
      name: "cost-within-threshold",
      evidenceId: "bb000001-03c0-4000-8000-0000000003c0",
      costEvidenceIds: [
        "bb000001-03d0-4000-8000-0000000003d0",
        "bb000001-03d1-4000-8000-0000000003d1",
        "bb000001-03d2-4000-8000-0000000003d2",
        "bb000001-03d3-4000-8000-0000000003d3",
      ],
      key: `${BOUNDARY_PREFIX}-COST-70`,
      setCode: `${BOUNDARY_PREFIX}-SET-70`,
      salesPrice: 100,
      totalCost: 70,
      expectedOutcome: "within-threshold",
      expectedDriver: "within-profile",
      expectedMargin: 0.3,
    },
    {
      name: "cost-exact-threshold",
      evidenceId: "bb000001-03c1-4000-8000-0000000003c1",
      costEvidenceIds: [
        "bb000001-03d4-4000-8000-0000000003d4",
        "bb000001-03d5-4000-8000-0000000003d5",
        "bb000001-03d6-4000-8000-0000000003d6",
        "bb000001-03d7-4000-8000-0000000003d7",
      ],
      key: `${BOUNDARY_PREFIX}-COST-85`,
      setCode: `${BOUNDARY_PREFIX}-SET-85`,
      salesPrice: 100,
      totalCost: 85,
      expectedOutcome: "within-threshold",
      expectedDriver: "within-profile",
      expectedMargin: 0.15,
    },
    {
      name: "cost-below-threshold",
      evidenceId: "bb000001-03c2-4000-8000-0000000003c2",
      costEvidenceIds: [
        "bb000001-03d8-4000-8000-0000000003d8",
        "bb000001-03d9-4000-8000-0000000003d9",
        "bb000001-03da-4000-8000-0000000003da",
        "bb000001-03db-4000-8000-0000000003db",
      ],
      key: `${BOUNDARY_PREFIX}-COST-86`,
      setCode: `${BOUNDARY_PREFIX}-SET-86`,
      salesPrice: 100,
      totalCost: 86,
      expectedOutcome: "below-threshold",
      expectedDriver: "contribution-margin-below-threshold",
      expectedMargin: 0.14,
    },
    {
      name: "cost-zero-contribution",
      evidenceId: "bb000001-03c3-4000-8000-0000000003c3",
      costEvidenceIds: [
        "bb000001-03dc-4000-8000-0000000003dc",
        "bb000001-03dd-4000-8000-0000000003dd",
        "bb000001-03de-4000-8000-0000000003de",
        "bb000001-03df-4000-8000-0000000003df",
      ],
      key: `${BOUNDARY_PREFIX}-COST-100`,
      setCode: `${BOUNDARY_PREFIX}-SET-100`,
      salesPrice: 100,
      totalCost: 100,
      expectedOutcome: "below-threshold",
      expectedDriver: "contribution-margin-below-threshold",
      expectedMargin: 0,
    },
    {
      name: "cost-negative-contribution",
      evidenceId: "bb000001-03c4-4000-8000-0000000003c4",
      costEvidenceIds: [
        "bb000001-03e0-4000-8000-0000000003e0",
        "bb000001-03e1-4000-8000-0000000003e1",
        "bb000001-03e2-4000-8000-0000000003e2",
        "bb000001-03e3-4000-8000-0000000003e3",
      ],
      key: `${BOUNDARY_PREFIX}-COST-110`,
      setCode: `${BOUNDARY_PREFIX}-SET-110`,
      salesPrice: 100,
      totalCost: 110,
      expectedOutcome: "negative-contribution",
      expectedDriver: "negative-unit-contribution",
      expectedMargin: -0.1,
    },
    {
      name: "cost-high-margin",
      evidenceId: "bb000001-03c5-4000-8000-0000000003c5",
      costEvidenceIds: [
        "bb000001-03e4-4000-8000-0000000003e4",
        "bb000001-03e5-4000-8000-0000000003e5",
        "bb000001-03e6-4000-8000-0000000003e6",
        "bb000001-03e7-4000-8000-0000000003e7",
      ],
      key: `${BOUNDARY_PREFIX}-COST-1`,
      setCode: `${BOUNDARY_PREFIX}-SET-1`,
      salesPrice: 100,
      totalCost: 1,
      expectedOutcome: "within-threshold",
      expectedDriver: "within-profile",
      expectedMargin: 0.99,
    },
    {
      name: "cost-zero-rejected",
      evidenceId: "bb000001-03c6-4000-8000-0000000003c6",
      costEvidenceIds: [
        "bb000001-03e8-4000-8000-0000000003e8",
        "bb000001-03e9-4000-8000-0000000003e9",
        "bb000001-03ea-4000-8000-0000000003ea",
        "bb000001-03eb-4000-8000-0000000003eb",
      ],
      key: `${BOUNDARY_PREFIX}-COST-0`,
      setCode: `${BOUNDARY_PREFIX}-SET-0`,
      salesPrice: 0,
      totalCost: 0,
      expectReject: true,
    },
  ];

  for (const boundaryCase of boundaryCases) {
    insertBoundarySalesEvidence({
      evidenceId: boundaryCase.evidenceId,
      idempotencyKey: boundaryCase.key,
      setCode: boundaryCase.setCode,
      salesPrice: boundaryCase.salesPrice,
    });

    if (!boundaryCase.expectReject) {
      insertBoundaryCostBundle({
        setCode: boundaryCase.setCode,
        idempotencyKey: boundaryCase.key,
        totalCost: boundaryCase.totalCost,
        costEvidenceIds: boundaryCase.costEvidenceIds,
      });
    } else {
      boundaryCase.costEvidenceIds.forEach((evidenceId, index) => {
        insertBoundaryCostEvidence({
          evidenceId,
          idempotencyKey: `${boundaryCase.key}-${COMPONENT_KINDS[index]}`,
          setCode: boundaryCase.setCode,
          componentKind: COMPONENT_KINDS[index],
          unitCost: 0,
        });
      });
    }

    const assessmentsBefore = Number(db(`SELECT COUNT(*) FROM commercial_profitability_assessments WHERE idempotency_key = '${boundaryCase.key}'`));

    if (boundaryCase.expectReject) {
      const response = await evaluateBoundary({
        name: boundaryCase.name,
        evidenceId: boundaryCase.evidenceId,
        idempotencyKey: boundaryCase.key,
        expectReject: true,
      });
      assert(`${boundaryCase.name} validation failure`, response.status === 400, `status=${response.status}`);
      const assessmentsAfter = Number(db(`SELECT COUNT(*) FROM commercial_profitability_assessments WHERE idempotency_key = '${boundaryCase.key}'`));
      assert(`${boundaryCase.name} no persistence`, assessmentsBefore === assessmentsAfter && assessmentsAfter === 0, `before=${assessmentsBefore} after=${assessmentsAfter}`);
      continue;
    }

    const assessment = await evaluateBoundary({
      name: boundaryCase.name,
      evidenceId: boundaryCase.evidenceId,
      idempotencyKey: boundaryCase.key,
    });

    assert(`${boundaryCase.name} outcome`, assessment.outcome === boundaryCase.expectedOutcome, assessment.outcome);
    assert(`${boundaryCase.name} leading driver`, assessment.leadingDriverCode === boundaryCase.expectedDriver, assessment.leadingDriverCode);
    assertNear(`${boundaryCase.name} total included cost`, assessment.totalIncludedUnitCost, boundaryCase.totalCost);
    if (boundaryCase.expectedMargin !== undefined) {
      assertNear(`${boundaryCase.name} margin ratio`, assessment.contributionMarginRatio, boundaryCase.expectedMargin);
      const proofMargin = computeContributionMarginRatio(
        computeUnitContribution(boundaryCase.salesPrice, boundaryCase.totalCost),
        boundaryCase.salesPrice,
      );
      assertNear(`${boundaryCase.name} margin math proof`, assessment.contributionMarginRatio, proofMargin);
    }
    assertDbReconciliation(boundaryCase.key, assessment);
  }
} finally {
  removeBoundaryFixtures();
  const postBoundaryCounts = boundaryFixtureCounts();
  assert("post-cleanup boundary assessments zero", postBoundaryCounts.assessments === 0, `count=${postBoundaryCounts.assessments}`);
  assert("post-cleanup boundary drivers zero", postBoundaryCounts.drivers === 0, `count=${postBoundaryCounts.drivers}`);
  assert("post-cleanup boundary components zero", postBoundaryCounts.components === 0, `count=${postBoundaryCounts.components}`);
  assert("post-cleanup boundary evidence zero", postBoundaryCounts.evidence === 0, `count=${postBoundaryCounts.evidence}`);
  assert("post-cleanup orphan drivers zero", postBoundaryCounts.orphanDrivers === 0, `count=${postBoundaryCounts.orphanDrivers}`);
}

console.log("COMMERCIAL-PROFITABILITY-1 domain-assessment verification passed.");

#!/usr/bin/env node
import { randomUUID } from "node:crypto";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const SHIFT = "bb000001-0040-4000-8000-000000000040";
const INJ1 = "bb000001-0096-4000-8000-000000000096";
const INJ2 = "bb000001-0103-4000-8000-000000000103";
const INJ3 = "bb000001-0104-4000-8000-000000000104";
const INJECTION = "injection-machine-operation";
const PO_COVERED = "PILOT-PO-PP-001";
const PO_GAP = "PILOT-PO-INJ-002";
const PO_CRITICAL = "PILOT-PO-INJ-003";
const PO_CONFLICT = "PILOT-PO-INJ-CONFLICT";
const PO_STALE = "PILOT-PO-INJ-STALE";
const ASSESS_COVERED = "bb000001-0120-4000-8000-000000000120";
const ASSESS_GAP = "bb000001-0121-4000-8000-000000000121";
const ASSESS_CRITICAL = "bb000001-0122-4000-8000-000000000122";
const ASSESS_CONFLICT = "bb000001-0123-4000-8000-000000000123";
const ASSESS_STALE = "bb000001-0124-4000-8000-000000000124";
const REQ_COVERED = "bb000001-0110-4000-8000-000000000110";
const DRAFT_CONFLICT_ASSIGNMENT = "bb000001-0125-4000-8000-000000000125";
const PERSON002 = "bb000001-00d1-4000-8000-0000000000d1";
const ELIGIBILITY_ASSESSMENT = "bb000001-0100-4000-8000-000000000100";
const ELIGIBILITY_RESULT002 = "bb000001-0101-4000-8000-000000000101";
const CLOSURE_PREFIX = "SHIFTRISK1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`SHIFT-RISK-1 coverage/conflict verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
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

function headers(actor = MANAGER) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = MANAGER) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

function closureAssessmentPredicate() {
  return `evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
}

function closureRequirementPredicate() {
  return `create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
}

function closureAssignmentPredicate() {
  return `create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
}

function removeClosureFixtures() {
  db(`
    DELETE FROM shift_risk_drivers
    WHERE assessment_id IN (
      SELECT id FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()}
    );
    DELETE FROM shift_risk_audit_events
    WHERE entity_id IN (
      SELECT id FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()}
      UNION
      SELECT id FROM shift_coverage_requirements WHERE ${closureRequirementPredicate()}
    );
    DELETE FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()};
    DELETE FROM shift_coverage_requirements WHERE ${closureRequirementPredicate()};
    DELETE FROM shift_operator_assignment_audit_events
    WHERE entity_id IN (
      SELECT id FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()}
    );
    DELETE FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()};
    DELETE FROM shift_workforce_persons
    WHERE employee_code LIKE '${CLOSURE_PREFIX}%';
  `);
}

function assertPilotPreserved() {
  const assessments = Number(db(`
    SELECT COUNT(*) FROM shift_risk_assessments
    WHERE id IN (
      '${ASSESS_COVERED}', '${ASSESS_GAP}', '${ASSESS_CRITICAL}',
      '${ASSESS_CONFLICT}', '${ASSESS_STALE}'
    )
  `));
  assert("pilot risk assessments preserved", assessments === 5, `count=${assessments}`);

  const requirements = Number(db(`
    SELECT COUNT(*) FROM shift_coverage_requirements
    WHERE id IN (
      '${REQ_COVERED}', 'bb000001-0111-4000-8000-000000000111',
      'bb000001-0112-4000-8000-000000000112'
    )
  `));
  assert("pilot coverage requirements preserved", requirements === 3, `count=${requirements}`);
}

function findDriver(detail, reasonCode) {
  return detail.json?.drivers?.find((item) => item.reasonCode === reasonCode);
}

function pilotShiftDate() {
  return db(`SELECT shift_date::text FROM shift_coverage_requirements WHERE id = '${REQ_COVERED}'`);
}

function seedClosureDuplicateAssignments() {
  const firstId = randomUUID();
  const secondId = randomUUID();
  const keyA = `${CLOSURE_PREFIX}-assign-a-${RUN_SUFFIX}`;
  const keyB = `${CLOSURE_PREFIX}-assign-b-${RUN_SUFFIX}`;
  db(`
    INSERT INTO shift_operator_assignments (
      id, tenant_id, facility_id, production_order_reference, machine_asset_id,
      production_shift_id, shift_date, workforce_person_id, planned_start_utc, planned_end_utc,
      status, eligibility_assessment_id, eligibility_result_id, required_competency_code,
      approved_by_subject_id, approved_at_utc, rule_set_version, version,
      created_at_utc, updated_at_utc, created_by, updated_by, create_idempotency_key
    )
    SELECT
      '${firstId}'::uuid,
      tenant_id, facility_id, '${PO_GAP}', '${INJ2}'::uuid,
      production_shift_id, shift_date, '${PERSON002}'::uuid,
      planned_start_utc + interval '15 minutes', planned_start_utc + interval '5 hours',
      'approved', '${ELIGIBILITY_ASSESSMENT}'::uuid, '${ELIGIBILITY_RESULT002}'::uuid,
      '${INJECTION}', created_by, NOW(), 'SHIFT-OPERATOR-ELIGIBILITY-V1', 1,
      NOW(), NOW(), created_by, created_by, '${keyA}'
    FROM shift_coverage_requirements
    WHERE id = 'bb000001-0111-4000-8000-000000000111'
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO shift_operator_assignments (
      id, tenant_id, facility_id, production_order_reference, machine_asset_id,
      production_shift_id, shift_date, workforce_person_id, planned_start_utc, planned_end_utc,
      status, eligibility_assessment_id, eligibility_result_id, required_competency_code,
      approved_by_subject_id, approved_at_utc, rule_set_version, version,
      created_at_utc, updated_at_utc, created_by, updated_by, create_idempotency_key
    )
    SELECT
      '${secondId}'::uuid,
      tenant_id, facility_id, '${PO_GAP}', '${INJ2}'::uuid,
      production_shift_id, shift_date, '${PERSON002}'::uuid,
      planned_start_utc + interval '2 hours', planned_end_utc,
      'approved', '${ELIGIBILITY_ASSESSMENT}'::uuid, '${ELIGIBILITY_RESULT002}'::uuid,
      '${INJECTION}', created_by, NOW(), 'SHIFT-OPERATOR-ELIGIBILITY-V1', 1,
      NOW(), NOW(), created_by, created_by, '${keyB}'
    FROM shift_coverage_requirements
    WHERE id = 'bb000001-0111-4000-8000-000000000111'
    ON CONFLICT (id) DO NOTHING;
  `);
  return { firstId, secondId };
}

function seedEvidenceConflictFixture(orderReference) {
  const personId = randomUUID();
  const assignmentId = randomUUID();
  const employeeCode = `${CLOSURE_PREFIX}-person-${RUN_SUFFIX}`;
  const assignKey = `${CLOSURE_PREFIX}-evidence-assign-${RUN_SUFFIX}`;
  db(`
    INSERT INTO shift_workforce_persons (
      id, tenant_id, facility_id, source_system, external_employee_ref,
      employee_code, employee_code_normalized, display_name, department, job_title,
      is_active, availability_state, source_shift_reference, source_calendar_reference,
      source_updated_at_utc, synchronized_at_utc, source_fingerprint, freshness_state,
      source_limitations_json, created_at_utc, updated_at_utc, version
    )
    SELECT
      '${personId}'::uuid,
      tenant_id,
      facility_id,
      'local-development-pilot',
      '${employeeCode}',
      '${employeeCode}',
      lower('${employeeCode}'),
      'Closure Evidence Conflict Person',
      'Enjeksiyon',
      'Operatör',
      true,
      'unknown',
      null,
      null,
      NOW(),
      NOW(),
      '${employeeCode}-fp',
      'fresh',
      '[]',
      NOW(),
      NOW(),
      1
    FROM shift_coverage_requirements
    WHERE id = '${REQ_COVERED}'
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO shift_operator_assignments (
      id, tenant_id, facility_id, production_order_reference, machine_asset_id,
      production_shift_id, shift_date, workforce_person_id, planned_start_utc, planned_end_utc,
      status, eligibility_assessment_id, eligibility_result_id, required_competency_code,
      approved_by_subject_id, approved_at_utc, rule_set_version, version,
      created_at_utc, updated_at_utc, created_by, updated_by, create_idempotency_key
    )
    SELECT
      '${assignmentId}'::uuid,
      tenant_id,
      facility_id,
      '${orderReference}',
      '${INJ2}'::uuid,
      production_shift_id,
      shift_date,
      '${personId}'::uuid,
      planned_start_utc,
      planned_end_utc,
      'approved',
      '${ELIGIBILITY_ASSESSMENT}'::uuid,
      '${ELIGIBILITY_RESULT002}'::uuid,
      '${INJECTION}',
      created_by,
      NOW(),
      'SHIFT-OPERATOR-ELIGIBILITY-V1',
      1,
      NOW(),
      NOW(),
      created_by,
      created_by,
      '${assignKey}'
    FROM shift_coverage_requirements
    WHERE id = '${REQ_COVERED}'
    ON CONFLICT (id) DO NOTHING;
  `);
  return { personId, assignmentId };
}

console.log("Running SHIFT-RISK-1 coverage/conflict verifier.");

const evaluator = readRequired("packages/CrevaFactOS.Persistence/ShiftRisk/ShiftCoverageRiskEvaluator.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/ShiftRisk/ShiftRiskConstants.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/ShiftRisk/ShiftRiskRepository.cs");

assert("rule-set version constant", constants.includes("SHIFT-COVERAGE-RISK-V1"));
assert("clear outcome", constants.includes('"clear"') || constants.includes('Clear = "clear"'));
assert("attention outcome", constants.includes("attention"));
assert("critical outcome", constants.includes("critical"));
assert("insufficient-data outcome", constants.includes("insufficient-data"));
assert("coverage gap rule", evaluator.includes("CoverageGap"));
assert("critical role gap rule", evaluator.includes("CriticalRoleGap"));
assert("assignment conflict rule", evaluator.includes("AssignmentConflict"));
assert("draft exclusion rule", evaluator.includes("AssignmentExcludedDraft"));
assert("duplicate person exclusion rule", evaluator.includes("AssignmentExcludedDuplicatePerson"));
assert("requirement missing rule", evaluator.includes("RequirementMissing"));
assert("source stale rule", evaluator.includes("SourceStale"));
assert("no scoring percentage", !evaluator.includes("score") && !evaluator.includes("probability"));
assert("repository EvaluateRiskAsync", repository.includes("EvaluateRiskAsync"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
assertPilotPreserved();

const shiftDate = pilotShiftDate();
assert("pilot shift date loaded", Boolean(shiftDate), shiftDate);

// A — fully covered → clear
const covered = await request("GET", `/api/shift-risks/assessments/${ASSESS_COVERED}?facilityId=${FACILITY}`);
assert("A covered assessment → 200", covered.status === 200, `status=${covered.status}`);
assert("A covered outcome clear", covered.json?.outcome === "clear", covered.json?.outcome);
assert("A covered rule-set", covered.json?.ruleSetVersion === "SHIFT-COVERAGE-RISK-V1");
assert("A covered gap zero", covered.json?.coverageGap === 0, String(covered.json?.coverageGap));

// B — gap → attention with required/observed/gap
const gap = await request("GET", `/api/shift-risks/assessments/${ASSESS_GAP}?facilityId=${FACILITY}`);
assert("B gap assessment → 200", gap.status === 200, `status=${gap.status}`);
assert("B gap outcome attention", gap.json?.outcome === "attention", gap.json?.outcome);
const gapDriver = findDriver(gap, "coverage-gap");
assert("B gap leading driver", gap.json?.leadingReasonCode === "coverage-gap", gap.json?.leadingReasonCode);
assert("B gap required value", Boolean(gapDriver?.requiredValue), gapDriver?.requiredValue);
assert("B gap observed value", gapDriver?.observedValue !== undefined && gapDriver?.observedValue !== null, gapDriver?.observedValue);
assert("B gap gap value", Number(gapDriver?.gapValue) > 0, gapDriver?.gapValue);

// C — critical → critical with critical-role-gap leading
const critical = await request("GET", `/api/shift-risks/assessments/${ASSESS_CRITICAL}?facilityId=${FACILITY}`);
assert("C critical assessment → 200", critical.status === 200, `status=${critical.status}`);
assert("C critical outcome", critical.json?.outcome === "critical", critical.json?.outcome);
assert("C critical-role-gap leading", critical.json?.leadingDriverCategory === "critical-role-gap", critical.json?.leadingDriverCategory);
assert("C critical-role-gap reason", critical.json?.leadingReasonCode === "critical-role-gap", critical.json?.leadingReasonCode);

// D — draft excluded from coverage count
const conflict = await request("GET", `/api/shift-risks/assessments/${ASSESS_CONFLICT}?facilityId=${FACILITY}`);
assert("D/F conflict assessment → 200", conflict.status === 200, `status=${conflict.status}`);
const draftExcluded = conflict.json?.excludedAssignments?.find(
  (item) => item.assignmentId === DRAFT_CONFLICT_ASSIGNMENT,
);
assert("D draft assignment excluded", Boolean(draftExcluded), JSON.stringify(conflict.json?.excludedAssignments));
assert("D draft exclusion reason", draftExcluded?.exclusionReasonCode === "assignment-excluded-draft", draftExcluded?.exclusionReasonCode);
assert(
  "D draft not counted",
  !conflict.json?.countedAssignments?.some((item) => item.assignmentId === DRAFT_CONFLICT_ASSIGNMENT),
);

// E — duplicate person not counted twice
const { firstId, secondId } = seedClosureDuplicateAssignments();
const duplicateEvaluate = await request(
  "POST",
  `/api/shift-risks/evaluations?facilityId=${FACILITY}`,
  {
    productionShiftId: SHIFT,
    shiftDate,
    productionOrderReference: PO_GAP,
    machineAssetId: INJ2,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-duplicate-${RUN_SUFFIX}`,
  },
);
assert("E duplicate evaluate → 200", duplicateEvaluate.status === 200, `status=${duplicateEvaluate.status} ${duplicateEvaluate.raw}`);
const duplicateDetail = await request(
  "GET",
  `/api/shift-risks/assessments/${duplicateEvaluate.json?.assessmentId}?facilityId=${FACILITY}`,
);
assert("E duplicate detail → 200", duplicateDetail.status === 200, `status=${duplicateDetail.status}`);
assert("E duplicate person counted once", duplicateDetail.json?.approvedEligibleCount <= 1, String(duplicateDetail.json?.approvedEligibleCount));
const duplicateExcluded = duplicateDetail.json?.excludedAssignments?.find(
  (item) => item.exclusionReasonCode === "assignment-excluded-duplicate-person",
);
assert("E duplicate person exclusion recorded", Boolean(duplicateExcluded), JSON.stringify(duplicateDetail.json?.excludedAssignments));
assert(
  "E closure duplicate assignments present",
  [firstId, secondId].every((id) =>
    duplicateDetail.json?.countedAssignments?.some((item) => item.assignmentId === id)
    || duplicateDetail.json?.excludedAssignments?.some((item) => item.assignmentId === id),
  ),
);

// F — conflict driver without invalid approved duplicate
assert("F conflict driver present", Boolean(findDriver(conflict, "assignment-conflict")), conflict.json?.leadingReasonCode);
assert(
  "F no duplicate-person on conflict pilot",
  !conflict.json?.drivers?.some((item) => item.reasonCode === "assignment-excluded-duplicate-person"),
);
assert(
  "F approved count not inflated by draft",
  conflict.json?.approvedEligibleCount === 0,
  String(conflict.json?.approvedEligibleCount),
);

// G — missing requirement → insufficient-data
const missingReq = await request(
  "POST",
  `/api/shift-risks/evaluations?facilityId=${FACILITY}`,
  {
    productionShiftId: SHIFT,
    shiftDate,
    productionOrderReference: `${CLOSURE_PREFIX}-NO-REQ-${RUN_SUFFIX}`,
    machineAssetId: INJ2,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-missing-${RUN_SUFFIX}`,
  },
);
assert("G missing requirement evaluate → 200", missingReq.status === 200, `status=${missingReq.status}`);
const missingDetail = await request(
  "GET",
  `/api/shift-risks/assessments/${missingReq.json?.assessmentId}?facilityId=${FACILITY}`,
);
assert("G missing requirement detail → 200", missingDetail.status === 200, `status=${missingDetail.status}`);
assert("G insufficient-data outcome", missingDetail.json?.outcome === "insufficient-data", missingDetail.json?.outcome);
assert("G requirement-missing leading", missingDetail.json?.leadingReasonCode === "requirement-missing", missingDetail.json?.leadingReasonCode);

// H — stale evidence
const stale = await request("GET", `/api/shift-risks/assessments/${ASSESS_STALE}?facilityId=${FACILITY}`);
assert("H stale assessment → 200", stale.status === 200, `status=${stale.status}`);
assert("H stale insufficient-data outcome", stale.json?.outcome === "insufficient-data", stale.json?.outcome);
assert("H stale source-stale driver", Boolean(findDriver(stale, "source-stale")), stale.json?.leadingReasonCode);
assert(
  "H stale assignment excluded",
  stale.json?.excludedAssignments?.some((item) => item.exclusionReasonCode === "source-stale"),
);

// I — evidence-conflict scenario
const evidenceOrderRef = `${CLOSURE_PREFIX}-EVIDENCE-${RUN_SUFFIX}`;
const evidenceReq = await request("POST", `/api/shift-risks/requirements?facilityId=${FACILITY}`, {
  productionShiftId: SHIFT,
  shiftDate,
  productionOrderReference: evidenceOrderRef,
  machineAssetId: INJ2,
  requiredCompetencyCode: INJECTION,
  minimumApprovedOperatorCount: 1,
  isCriticalRole: false,
  sourceReference: "closure-evidence-conflict",
  isActive: true,
  idempotencyKey: `${CLOSURE_PREFIX}-evidence-req-${RUN_SUFFIX}`,
});
assert("I evidence-conflict requirement → 201", evidenceReq.status === 201, `status=${evidenceReq.status}`);
seedEvidenceConflictFixture(evidenceOrderRef);
const evidenceEvaluate = await request("POST", `/api/shift-risks/evaluations?facilityId=${FACILITY}`, {
  productionShiftId: SHIFT,
  shiftDate,
  productionOrderReference: evidenceOrderRef,
  machineAssetId: INJ2,
  requiredCompetencyCode: INJECTION,
  idempotencyKey: `${CLOSURE_PREFIX}-evidence-eval-${RUN_SUFFIX}`,
});
assert("I evidence-conflict evaluate → 200", evidenceEvaluate.status === 200, `status=${evidenceEvaluate.status}`);
const evidenceDetail = await request(
  "GET",
  `/api/shift-risks/assessments/${evidenceEvaluate.json?.assessmentId}?facilityId=${FACILITY}`,
);
assert("I evidence-conflict detail → 200", evidenceDetail.status === 200, `status=${evidenceDetail.status}`);
const evidenceDriver = findDriver(evidenceDetail, "evidence-conflict");
assert("I evidence-conflict driver", Boolean(evidenceDriver), evidenceDetail.json?.leadingReasonCode);
assert(
  "I evidence-conflict structured evidence",
  Boolean(evidenceDetail.json?.structuredEvidenceJson)
    && Boolean(evidenceDetail.json?.sourceFreshnessSnapshotJson),
);
assert(
  "I evidence-conflict outcome",
  evidenceDetail.json?.outcome === "attention",
  evidenceDetail.json?.outcome,
);
assert(
  "I evidence-conflict exclusion recorded",
  evidenceDetail.json?.excludedAssignments?.some((item) => item.exclusionReasonCode === "evidence-conflict"),
);
assert("I no fabricated clear result", evidenceDetail.json?.outcome !== "clear");
assert("I no fabricated zero gap inflation", evidenceDetail.json?.approvedEligibleCount !== 1);

removeClosureFixtures();
const remainingClosure = Number(db(`SELECT COUNT(*) FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()}`));
assert("closure fixtures removed", remainingClosure === 0, `count=${remainingClosure}`);
assertPilotPreserved();

console.log("SHIFT-RISK-1 coverage/conflict verification passed.");

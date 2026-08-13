#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MACHINE = "bb000001-0096-4000-8000-000000000096";
const SHIFT = "bb000001-0040-4000-8000-000000000040";
const PO = "PILOT-PO-PP-001";
const INJECTION = "injection-machine-operation";
const MOLD = "mold-change-setup";
const QUALITY = "quality-check";
const PERSON001 = "bb000001-00d0-4000-8000-0000000000d0";
const PERSON002 = "bb000001-00d1-4000-8000-0000000000d1";
const PERSON003 = "bb000001-00d2-4000-8000-0000000000d2";
const PERSON004 = "bb000001-00d3-4000-8000-0000000000d3";
const PERSON005 = "bb000001-00d4-4000-8000-0000000000d4";
const PERSON006 = "bb000001-00d5-4000-8000-0000000000d5";
const CLOSURE_PREFIX = "SHIFTASSIGN1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`SHIFT-ASSIGN-1 eligibility verification failed: ${message}`);
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

function candidateQuery(shiftDate, competencyCode = INJECTION, extra = "") {
  const params = new URLSearchParams({
    facilityId: FACILITY,
    productionOrderReference: PO,
    machineAssetId: MACHINE,
    productionShiftId: SHIFT,
    shiftDate,
    requiredCompetencyCode: competencyCode,
  });
  if (extra) {
    for (const [key, value] of new URLSearchParams(extra)) params.set(key, value);
  }
  return `/api/shift-assignments/candidates?${params}`;
}

function findCandidate(result, personId) {
  return result.json?.candidates?.find((item) => item.personId === personId);
}

function closureAssignmentPredicate(tableName = "shift_operator_assignments") {
  return `${tableName}.create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
}

function previewClosureFixtures() {
  return db(`
    SELECT id, create_idempotency_key
    FROM shift_operator_assignments
    WHERE ${closureAssignmentPredicate("shift_operator_assignments")}
    ORDER BY create_idempotency_key
  `);
}

function removeClosureFixtures() {
  db(`
    DELETE FROM shift_operator_assignment_audit_events
    WHERE entity_id IN (
      SELECT id FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()}
    );
    DELETE FROM shift_operator_eligibility_reasons
    WHERE result_id IN (
      SELECT r.id
      FROM shift_operator_eligibility_results r
      JOIN shift_operator_eligibility_assessments a ON a.id = r.assessment_id
      WHERE a.production_order_reference LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM shift_operator_eligibility_results
    WHERE assessment_id IN (
      SELECT id FROM shift_operator_eligibility_assessments
      WHERE production_order_reference LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM shift_operator_eligibility_assessments
    WHERE production_order_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()};
  `);
}

function assertPilotPreserved() {
  const workforce = Number(db(`
    SELECT COUNT(*) FROM shift_workforce_persons
    WHERE id IN (
      '${PERSON001}', '${PERSON002}', '${PERSON003}',
      '${PERSON004}', '${PERSON005}', '${PERSON006}'
    )
  `));
  assert("pilot workforce records preserved", workforce === 6, `count=${workforce}`);

  const pilotAssignment = Number(db(`
    SELECT COUNT(*) FROM shift_operator_assignments
    WHERE id = 'bb000001-0102-4000-8000-000000000102'
  `));
  assert("pilot approved assignment preserved", pilotAssignment === 1, `count=${pilotAssignment}`);
}

console.log("Running SHIFT-ASSIGN-1 eligibility verifier.");

const evaluator = readRequired("packages/CrevaFactOS.Persistence/ShiftAssignment/ShiftOperatorEligibilityEvaluator.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/ShiftAssignment/ShiftAssignmentConstants.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/ShiftAssignment/ShiftAssignmentRepository.cs");

assert("rule-set version constant", constants.includes("SHIFT-OPERATOR-ELIGIBILITY-V1"));
assert("eligible outcome", constants.includes('"eligible"') || constants.includes("Eligible = \"eligible\""));
assert("conditional outcome", constants.includes("conditional"));
assert("ineligible outcome", constants.includes("ineligible"));
assert("insufficient-data outcome", constants.includes("insufficient-data"));
assert("employment inactive rule", evaluator.includes("EmploymentInactive"));
assert("availability leave rule", evaluator.includes("AvailabilityLeave"));
assert("availability unknown rule", evaluator.includes("AvailabilityUnknown"));
assert("source stale rule", evaluator.includes("SourceStale"));
assert("competency missing rule", evaluator.includes("CompetencyMissing"));
assert("overlap rule", evaluator.includes("AssignmentOverlap"));
assert("no scoring percentage", !evaluator.includes("score") && !evaluator.includes("probability"));
assert("repository BuildEvaluationContextAsync", repository.includes("BuildEvaluationContextAsync"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

console.log("Preview closure fixtures:", previewClosureFixtures() || "(none)");
removeClosureFixtures();
assertPilotPreserved();

// Align pilot eligible operator shift reference for deterministic eligible outcome.
db(`UPDATE shift_workforce_persons SET source_shift_reference = 'V1' WHERE id = '${PERSON002}'`);

const eligibleDate = `2027-${String((RUN_SUFFIX % 11) + 1).padStart(2, "0")}-${String((RUN_SUFFIX % 26) + 1).padStart(2, "0")}`;
const candidates = await request("GET", candidateQuery(eligibleDate));
assert("candidates list → 200", candidates.status === 200, `status=${candidates.status} ${candidates.raw}`);
assert("rule-set version in response", candidates.json?.ruleSetVersion === "SHIFT-OPERATOR-ELIGIBILITY-V1");

const eligible = findCandidate(candidates, PERSON002);
assert("A eligible candidate outcome", eligible?.outcome === "eligible", eligible?.outcome);

const unavailable = findCandidate(candidates, PERSON003);
assert("B unavailable candidate outcome", unavailable?.outcome === "ineligible", unavailable?.outcome);
assert("B unavailable primary reason", unavailable?.primaryReasonCode === "availability-leave");

const missingCompetency = findCandidate(candidates, PERSON004);
assert("C missing-competency candidate outcome", missingCompetency?.outcome === "ineligible", missingCompetency?.outcome);
assert("C missing-competency primary reason", missingCompetency?.primaryReasonCode === "competency-missing");

const inactive = findCandidate(candidates, PERSON006);
assert("inactive candidate outcome", inactive?.outcome === "ineligible", inactive?.outcome);

const conditionalMold = await request("GET", candidateQuery(eligibleDate, MOLD));
const conditional = findCandidate(conditionalMold, PERSON004);
assert("conditional candidate outcome", conditional?.outcome === "conditional", conditional?.outcome);

const conditionalQuality = await request("GET", candidateQuery(eligibleDate, QUALITY));
const conditionalPending = findCandidate(conditionalQuality, PERSON005);
assert("conditional pending verification candidate", conditionalPending?.outcome === "conditional", conditionalPending?.outcome);

const detail = await request("GET", `/api/shift-assignments/candidates/${PERSON002}${candidateQuery(eligibleDate).replace("/api/shift-assignments/candidates", "")}`);
assert("candidate detail → 200", detail.status === 200, `status=${detail.status}`);
assert("structured reasons present", Array.isArray(detail.json?.reasons) && detail.json.reasons.length > 0);
assert("rule-set on detail", detail.json?.ruleSetVersion === "SHIFT-OPERATOR-ELIGIBILITY-V1");
assert("Turkish reason labels", detail.json.reasons.every((r) => typeof r.labelTr === "string" && r.labelTr.length > 0));

const blockedCreate = await request(
  "POST",
  `/api/shift-assignments/assignments?facilityId=${FACILITY}`,
  {
    productionOrderReference: PO,
    machineAssetId: MACHINE,
    productionShiftId: SHIFT,
    shiftDate: eligibleDate,
    workforcePersonId: PERSON003,
    plannedStartUtc: `${eligibleDate}T05:00:00.000Z`,
    plannedEndUtc: `${eligibleDate}T13:00:00.000Z`,
    eligibilityAssessmentId: candidates.json.assessmentId,
    eligibilityResultId: unavailable.eligibilityResultId,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-blocked-${RUN_SUFFIX}`,
  },
);
assert("B/C assignment blocked for ineligible", blockedCreate.status === 400, `status=${blockedCreate.status}`);
assert("blocked failure code", blockedCreate.json?.failureCode === "shift-assign.eligibility-blocked");

const insufficient = await request(
  "GET",
  `/api/shift-assignments/candidates?facilityId=${FACILITY}&productionOrderReference=${PO}&machineAssetId=${MACHINE}&productionShiftId=${SHIFT}&shiftDate=${eligibleDate}&requiredCompetencyCode=`,
);
assert("D insufficient-data when competency missing", insufficient.status === 200, `status=${insufficient.status}`);
const insufficientSample = insufficient.json?.candidates?.[0];
assert("D insufficient-data outcome", insufficientSample?.outcome === "insufficient-data", insufficientSample?.outcome);
assert("D insufficient-data blocks persisted assessment", insufficient.json?.assessmentId === "00000000-0000-0000-0000-000000000000");

removeClosureFixtures();
const remainingClosure = Number(db(`SELECT COUNT(*) FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()}`));
assert("closure fixtures removed", remainingClosure === 0, `count=${remainingClosure}`);
assertPilotPreserved();

console.log("SHIFT-ASSIGN-1 eligibility verification passed.");

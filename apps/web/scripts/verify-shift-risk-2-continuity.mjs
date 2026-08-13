#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const SHIFT = "bb000001-0040-4000-8000-000000000040";
const INJ2 = "bb000001-0103-4000-8000-000000000103";
const INJECTION = "injection-machine-operation";
const PERSON002 = "bb000001-00d1-4000-8000-0000000000d1";
const PERSON003 = "bb000001-00d2-4000-8000-0000000000d2";
const ELIGIBILITY_ASSESSMENT = "bb000001-0100-4000-8000-000000000100";
const ELIGIBILITY_RESULT002 = "bb000001-0101-4000-8000-000000000101";
const REQ_COVERED = "bb000001-0110-4000-8000-000000000110";
const ASSESS_CLEAR = "bb000001-0130-4000-8000-000000000130";
const ASSESS_ABSENCE = "bb000001-0131-4000-8000-000000000131";
const ASSESS_EXPIRED = "bb000001-0132-4000-8000-000000000132";
const ASSESS_EXPIRING_URGENT = "bb000001-0133-4000-8000-000000000133";
const ASSESS_EXPIRING_UPCOMING = "bb000001-0134-4000-8000-000000000134";
const ASSESS_PENDING = "bb000001-0135-4000-8000-000000000135";
const ASSESS_STALE = "bb000001-0136-4000-8000-000000000136";
const ASSESS_UNKNOWN = "bb000001-0137-4000-8000-000000000137";
const ASSESS_OVERLAP = "bb000001-0138-4000-8000-000000000138";
const ASSESS_WORKLOAD = "bb000001-0139-4000-8000-000000000139";
const ASSESS_CRITICAL_COVERAGE = "bb000001-0140-4000-8000-000000000140";
const CLOSURE_PREFIX = "SHIFTRISK2-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`SHIFT-RISK-2 continuity verification failed: ${message}`);
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
    );
    DELETE FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()};
    DELETE FROM shift_operator_assignment_audit_events
    WHERE entity_id IN (
      SELECT id FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()}
    );
    DELETE FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()};
  `);
}

function assertPilotPreserved() {
  const assessments = Number(db(`
    SELECT COUNT(*) FROM shift_risk_assessments
    WHERE id IN (
      '${ASSESS_CLEAR}', '${ASSESS_ABSENCE}', '${ASSESS_EXPIRED}',
      '${ASSESS_EXPIRING_URGENT}', '${ASSESS_EXPIRING_UPCOMING}', '${ASSESS_PENDING}',
      '${ASSESS_STALE}', '${ASSESS_UNKNOWN}', '${ASSESS_OVERLAP}',
      '${ASSESS_WORKLOAD}', '${ASSESS_CRITICAL_COVERAGE}'
    )
  `));
  assert("pilot continuity assessments preserved", assessments === 11, `count=${assessments}`);
}

function findDriver(detail, reasonCode) {
  return detail.json?.drivers?.find((item) => item.reasonCode === reasonCode);
}

function pilotShiftDate() {
  return db(`SELECT shift_date::text FROM shift_coverage_requirements WHERE id = '${REQ_COVERED}'`);
}

console.log("Running SHIFT-RISK-2 continuity verifier.");

const evaluator = readRequired("packages/CrevaFactOS.Persistence/ShiftRisk/ShiftWorkforceContinuityRiskEvaluator.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/ShiftRisk/ShiftWorkforceContinuityConstants.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/ShiftRisk/ShiftRiskRepository.cs");
const migration = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260813170000_AddShiftWorkforceContinuityRisk.cs");

assert("rule-set version constant", constants.includes("SHIFT-WORKFORCE-CONTINUITY-RISK-V1"));
assert("continuity-clear reason", constants.includes("continuity-clear"));
assert("assignment-load-facts reason with load-pressure category", evaluator.includes("AssignmentLoadFacts") && evaluator.includes("AssignmentLoadPressure"));
assert("repository EvaluateWorkforceContinuityAsync", repository.includes("EvaluateWorkforceContinuityAsync"));
assert("migration assessment_type column", migration.includes("assessment_type"));
assert("migration driver person index", migration.includes("ix_shift_risk_drivers_tenant_assessment_reason_person"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
assertPilotPreserved();

const shiftDate = pilotShiftDate();
assert("pilot shift date loaded", Boolean(shiftDate), shiftDate);

// A — trustworthy person → clear
const clear = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_CLEAR}?facilityId=${FACILITY}`);
assert("A clear assessment → 200", clear.status === 200, `status=${clear.status}`);
assert("A clear outcome", clear.json?.outcome === "clear", clear.json?.outcome);
assert("A continuity rule-set", clear.json?.ruleSetVersion === "SHIFT-WORKFORCE-CONTINUITY-RISK-V1");
assert("A continuity-clear driver", Boolean(findDriver(clear, "continuity-clear")), clear.json?.leadingReasonCode);

// B — leave/absence → attention with absence-impact
const absence = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_ABSENCE}?facilityId=${FACILITY}`);
assert("B absence assessment → 200", absence.status === 200, `status=${absence.status}`);
assert("B absence outcome attention", absence.json?.outcome === "attention", absence.json?.outcome);
assert("B absence-impact leading", absence.json?.leadingReasonCode === "absence-impact", absence.json?.leadingReasonCode);

// C — expired competency → attention
const expired = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_EXPIRED}?facilityId=${FACILITY}`);
assert("C expired assessment → 200", expired.status === 200, `status=${expired.status}`);
assert("C expired outcome attention", expired.json?.outcome === "attention", expired.json?.outcome);
assert("C competency-expired driver", Boolean(findDriver(expired, "competency-expired")), expired.json?.leadingReasonCode);

// D — expiring 14/30 days
const urgent = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_EXPIRING_URGENT}?facilityId=${FACILITY}`);
assert("D urgent expiry → 200", urgent.status === 200, `status=${urgent.status}`);
assert("D urgent outcome attention", urgent.json?.outcome === "attention", urgent.json?.outcome);
assert("D competency-expiring-urgent driver", Boolean(findDriver(urgent, "competency-expiring-urgent")));

const upcoming = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_EXPIRING_UPCOMING}?facilityId=${FACILITY}`);
assert("D upcoming expiry → 200", upcoming.status === 200, `status=${upcoming.status}`);
assert("D upcoming outcome attention", upcoming.json?.outcome === "attention", upcoming.json?.outcome);
assert("D competency-expiring-upcoming driver", Boolean(findDriver(upcoming, "competency-expiring-upcoming")));

// E — pending verification → attention
const pending = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_PENDING}?facilityId=${FACILITY}`);
assert("E pending verification → 200", pending.status === 200, `status=${pending.status}`);
assert("E pending outcome attention", pending.json?.outcome === "attention", pending.json?.outcome);
assert("E competency-pending-verification driver", Boolean(findDriver(pending, "competency-pending-verification")));

// F — stale source → insufficient-data
const stale = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_STALE}?facilityId=${FACILITY}`);
assert("F stale source → 200", stale.status === 200, `status=${stale.status}`);
assert("F stale insufficient-data", stale.json?.outcome === "insufficient-data", stale.json?.outcome);
assert("F source-stale driver", Boolean(findDriver(stale, "source-stale")));

// G — active person + unknown availability only → insufficient-data
const unknown = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_UNKNOWN}?facilityId=${FACILITY}`);
assert("G unknown availability → 200", unknown.status === 200, `status=${unknown.status}`);
assert("G unknown outcome insufficient-data", unknown.json?.outcome === "insufficient-data", unknown.json?.outcome);
assert("G availability-unknown driver", Boolean(findDriver(unknown, "availability-unknown")));
assert("G no inactive-employment driver", !findDriver(unknown, "inactive-employment"));

// H — pilot assignment overlap
const overlap = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_OVERLAP}?facilityId=${FACILITY}`);
assert("H overlap assessment → 200", overlap.status === 200, `status=${overlap.status}`);
assert("H assignment-overlap driver", Boolean(findDriver(overlap, "assignment-overlap")));

// I — workload → insufficient-data (threshold missing)
const workload = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_WORKLOAD}?facilityId=${FACILITY}`);
assert("I workload facts → 200", workload.status === 200, `status=${workload.status}`);
assert("I workload insufficient-data", workload.json?.outcome === "insufficient-data", workload.json?.outcome);
assert("I assignment-load-facts driver", Boolean(findDriver(workload, "assignment-load-facts")));
assert("I assignment-load-threshold-missing driver", Boolean(findDriver(workload, "assignment-load-threshold-missing")));

// J — coverage impact → critical
const critical = await request("GET", `/api/shift-risks/workforce-continuity/assessments/${ASSESS_CRITICAL_COVERAGE}?facilityId=${FACILITY}`);
assert("J critical coverage → 200", critical.status === 200, `status=${critical.status}`);
assert("J critical outcome", critical.json?.outcome === "critical", critical.json?.outcome);
assert("J coverage-impact leading", critical.json?.leadingReasonCode === "coverage-impact", critical.json?.leadingReasonCode);

removeClosureFixtures();
const remainingClosure = Number(db(`SELECT COUNT(*) FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()}`));
assert("closure fixtures removed", remainingClosure === 0, `count=${remainingClosure}`);
assertPilotPreserved();

console.log("SHIFT-RISK-2 continuity verification passed.");

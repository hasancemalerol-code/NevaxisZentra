#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const SHIFT = "bb000001-0040-4000-8000-000000000040";
const INJ2 = "bb000001-0103-4000-8000-000000000103";
const INJECTION = "injection-machine-operation";
const PERSON002 = "bb000001-00d1-4000-8000-0000000000d1";
const PILOT_CONTINUITY = "bb000001-0130-4000-8000-000000000130";
const CLOSURE_PREFIX = "SHIFTRISK2-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`SHIFT-RISK-2 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
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
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, text };
}

function closureAssessmentPredicate() {
  return `evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
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
  `);
}

function pilotShiftDate() {
  return db(`SELECT shift_date::text FROM shift_coverage_requirements WHERE id = 'bb000001-0110-4000-8000-000000000110'`);
}

console.log("Running SHIFT-RISK-2 security/idempotency verifier.");

const endpoints = readRequired("apps/api/ShiftRisk/ShiftRiskEndpointRouteBuilderExtensions.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260813170000_AddShiftWorkforceContinuityRisk.cs");
const fingerprint = readRequired("packages/NevaxisFactOS.Persistence/ShiftRisk/ShiftRiskIdempotencyFingerprint.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("continuity evaluate endpoint", endpoints.includes("/workforce-continuity/evaluations"));
assert("continuity overview endpoint", endpoints.includes("/workforce-continuity/overview"));
assert("continuity assessments endpoint", endpoints.includes("/workforce-continuity/assessments"));
assert("continuity fingerprint includes workforcePersonId", fingerprint.includes("workforcePersonId"));
assert("continuity fingerprint rule set", fingerprint.includes("ShiftWorkforceContinuityConstants.RuleSetVersion"));
assert("migration assessment_type default coverage", migration.includes('defaultValue: "coverage"'));
assert("bootstrap continuity seed", bootstrap.includes("EnsureShiftContinuityRiskPilotRecordsAsync"));
forbidText(endpoints, "MapDelete", "shift-risk endpoints");

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(READONLY);
const negativeHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: "quality-negative-test-user",
  tenant: "dev-pilot-tenant",
});
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const assessmentCountBefore = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
const auditCountBefore = Number(db("SELECT COUNT(*) FROM shift_risk_audit_events"));
const shiftDate = pilotShiftDate();
assert("pilot shift date loaded", Boolean(shiftDate), shiftDate);

// L — auth matrix for continuity
assert(
  "missing auth continuity overview → 401",
  (await liveRequest("GET", `/api/shift-risks/workforce-continuity/overview?facilityId=${FACILITY}`, null, { "Content-Type": "application/json" })).status === 401,
);
assert(
  "manager continuity overview → 200",
  (await liveRequest("GET", `/api/shift-risks/workforce-continuity/overview?facilityId=${FACILITY}`, null, managerHeaders)).status === 200,
);
assert(
  "read-only continuity assessments → 200",
  (await liveRequest("GET", `/api/shift-risks/workforce-continuity/assessments?facilityId=${FACILITY}`, null, readonlyHeaders)).status === 200,
);

const readonlyEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`,
  {
    productionShiftId: SHIFT,
    shiftDate,
    workforcePersonId: PERSON002,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-ro-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("read-only continuity evaluate → 403", readonlyEvaluate.status === 403, `status=${readonlyEvaluate.status}`);

assert(
  "unauthorized continuity overview → 403",
  (await liveRequest("GET", `/api/shift-risks/workforce-continuity/overview?facilityId=${FACILITY}`, null, negativeHeaders)).status === 403,
);
assert(
  "wrong facility continuity → 403",
  (await liveRequest("GET", `/api/shift-risks/workforce-continuity/overview?facilityId=${UNAUTH_FACILITY}`, null, managerHeaders)).status === 403,
);
assert(
  "cross-tenant continuity → 403",
  (await liveRequest("GET", `/api/shift-risks/workforce-continuity/overview?facilityId=${FACILITY}`, null, crossTenantHeaders)).status === 403,
);

const deniedEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`,
  {
    productionShiftId: SHIFT,
    shiftDate,
    workforcePersonId: PERSON002,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-denied-${RUN_SUFFIX}`,
  },
  negativeHeaders,
);
assert("denied continuity evaluate → 403", deniedEvaluate.status === 403, `status=${deniedEvaluate.status}`);
const assessmentCountAfterDenied = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
assert("denied evaluate created no assessments", assessmentCountAfterDenied === assessmentCountBefore);

// I — exact idempotent continuity evaluate replay
const evaluateKey = `${CLOSURE_PREFIX}-evaluate-${RUN_SUFFIX}`;
const evaluateBody = {
  productionShiftId: SHIFT,
  shiftDate,
  workforcePersonId: PERSON002,
  productionOrderReference: `${CLOSURE_PREFIX}-EVAL-${RUN_SUFFIX}`,
  machineAssetId: INJ2,
  requiredCompetencyCode: INJECTION,
  idempotencyKey: evaluateKey,
};

const firstEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`,
  evaluateBody,
  managerHeaders,
);
assert("I continuity evaluate created", firstEvaluate.status === 200, `status=${firstEvaluate.status} ${firstEvaluate.text}`);
const firstAssessmentId = firstEvaluate.json?.assessmentId;
assert("I assessment id returned", Boolean(firstAssessmentId), firstEvaluate.text);

const replayEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`,
  evaluateBody,
  managerHeaders,
);
assert("I exact idempotent replay", replayEvaluate.status === 200 && replayEvaluate.json?.idempotentReplay === true);
assert("I replay same assessment id", replayEvaluate.json?.assessmentId === firstAssessmentId);

const assessmentCountAfterReplay = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
assert("I no duplicate assessment row", assessmentCountAfterReplay === assessmentCountBefore + 1);

// M — concurrent identical continuity evaluation
const concurrentCountBefore = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
const concurrentKey = `${CLOSURE_PREFIX}-concurrent-${RUN_SUFFIX}`;
const concurrentBody = {
  productionShiftId: SHIFT,
  shiftDate,
  workforcePersonId: PERSON002,
  productionOrderReference: `${CLOSURE_PREFIX}-CONCURRENT-${RUN_SUFFIX}`,
  machineAssetId: INJ2,
  requiredCompetencyCode: INJECTION,
  idempotencyKey: concurrentKey,
};
const concurrentResults = await Promise.all([
  liveRequest("POST", `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`, concurrentBody, managerHeaders),
  liveRequest("POST", `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`, concurrentBody, managerHeaders),
]);
assert(
  "M concurrent identical requests",
  concurrentResults.every((result) => result.status === 200),
  concurrentResults.map((result) => result.status).join(","),
);
const concurrentIds = new Set(concurrentResults.map((result) => result.json?.assessmentId).filter(Boolean));
assert("M concurrent same assessment identity", concurrentIds.size === 1, [...concurrentIds].join(","));
const concurrentAssessmentId = [...concurrentIds][0];
const concurrentCountAfter = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
assert("M concurrent assessment count increase", concurrentCountAfter === concurrentCountBefore + 1);
const duplicateDriverRows = Number(
  db(`
    SELECT COUNT(*) FROM shift_risk_drivers d
    WHERE d.assessment_id = '${concurrentAssessmentId}'
      AND EXISTS (
        SELECT 1 FROM shift_risk_drivers d2
        WHERE d2.assessment_id = d.assessment_id
          AND d2.reason_code = d.reason_code
          AND COALESCE(d2.workforce_person_id, '00000000-0000-0000-0000-000000000000'::uuid)
              = COALESCE(d.workforce_person_id, '00000000-0000-0000-0000-000000000000'::uuid)
          AND d2.id <> d.id
      )
  `),
);
assert("M concurrent duplicate drivers", duplicateDriverRows === 0, `count=${duplicateDriverRows}`);

// N — conflicting idempotent retry
const conflictEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`,
  {
    ...evaluateBody,
    productionOrderReference: `${CLOSURE_PREFIX}-EVAL-CONFLICT-${RUN_SUFFIX}`,
  },
  managerHeaders,
);
assert("N conflicting idempotent retry → 409", conflictEvaluate.status === 409, `status=${conflictEvaluate.status}`);
assert("N stable conflict code", conflictEvaluate.json?.failureCode === "shift-risk.idempotency-conflict");

// O — input fingerprint dedupe for same person/shift context
const fingerprintBody = {
  productionShiftId: SHIFT,
  shiftDate,
  workforcePersonId: PERSON002,
  productionOrderReference: "PILOT-PO-PP-001",
  machineAssetId: "bb000001-0096-4000-8000-000000000096",
  requiredCompetencyCode: INJECTION,
  idempotencyKey: `${CLOSURE_PREFIX}-fingerprint-a-${RUN_SUFFIX}`,
};
const fingerprintFirst = await liveRequest(
  "POST",
  `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`,
  fingerprintBody,
  managerHeaders,
);
assert("O fingerprint first evaluate → 200", fingerprintFirst.status === 200, fingerprintFirst.text);
const fingerprintSecond = await liveRequest(
  "POST",
  `/api/shift-risks/workforce-continuity/evaluations?facilityId=${FACILITY}`,
  {
    ...fingerprintBody,
    idempotencyKey: `${CLOSURE_PREFIX}-fingerprint-b-${RUN_SUFFIX}`,
  },
  managerHeaders,
);
assert(
  "O fingerprint dedupe replay",
  fingerprintSecond.status === 200 && fingerprintSecond.json?.idempotentReplay === true,
  fingerprintSecond.text,
);
assert(
  "O fingerprint same assessment id",
  fingerprintSecond.json?.assessmentId === fingerprintFirst.json?.assessmentId,
);

// K — pilot continuity readback survives closure cleanup
const pilotReadback = await liveRequest(
  "GET",
  `/api/shift-risks/workforce-continuity/assessments/${PILOT_CONTINUITY}?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("K pilot continuity readback", pilotReadback.status === 200 && pilotReadback.json?.outcome === "clear");

removeClosureFixtures();
const remainingAssessments = Number(db(`SELECT COUNT(*) FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()}`));
assert("closure assessments removed", remainingAssessments === 0, `count=${remainingAssessments}`);

const pilotStillThere = Number(db(`SELECT COUNT(*) FROM shift_risk_assessments WHERE id = '${PILOT_CONTINUITY}'`));
assert("pilot continuity assessment preserved", pilotStillThere === 1);

const auditCountAfter = Number(db("SELECT COUNT(*) FROM shift_risk_audit_events"));
assert("audit events append-only growth", auditCountAfter >= auditCountBefore, `${auditCountBefore}→${auditCountAfter}`);

const assessmentTypeColumn = db(`
  SELECT COUNT(*) FROM information_schema.columns
  WHERE table_name = 'shift_risk_assessments' AND column_name = 'assessment_type'
`);
assert("DB assessment_type column exists", Number(assessmentTypeColumn) === 1, assessmentTypeColumn);

const driverPersonIndex = db(`
  SELECT COUNT(*) FROM pg_indexes
  WHERE indexname = 'ix_shift_risk_drivers_tenant_assessment_reason_person'
`);
assert("DB driver person unique index exists", Number(driverPersonIndex) === 1, driverPersonIndex);

console.log("SHIFT-RISK-2 security/idempotency verification passed.");

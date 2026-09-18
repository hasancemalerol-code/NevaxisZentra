#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MACHINE = "bb000001-0096-4000-8000-000000000096";
const PREFIX = `poe1-calc-${Date.now()}`;
const CLOSURE_PREFIX = "POEE1-CLOSURE";
const CLOSURE_RUN = Date.now();
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`PROCESS-OEE-1 calculation verification failed: ${message}`);
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
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = ACTOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, raw: text };
}

function deleteStabilityChildrenMatchingAssessmentFilter(assessmentFilterSql, batchPrefix = CLOSURE_PREFIX) {
  db(`
    DELETE FROM process_oee_shift_comparison_rows
    WHERE batch_id IN (
      SELECT id FROM process_oee_shift_comparison_batches
      WHERE idempotency_key LIKE '${batchPrefix}%'
    );
    DELETE FROM process_oee_shift_comparison_batches
    WHERE idempotency_key LIKE '${batchPrefix}%';
    DELETE FROM process_oee_downtime_breakdown_rows
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE ${assessmentFilterSql}
    );
    DELETE FROM process_oee_stability_drivers
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE ${assessmentFilterSql}
    );
    DELETE FROM process_oee_stability_assessments
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE ${assessmentFilterSql}
    );
  `);
}

function deleteLossDriversMatchingAssessmentFilter(assessmentFilterSql) {
  db(`
    DELETE FROM process_oee_loss_drivers
    WHERE assessment_id IN (
      SELECT id FROM process_oee_assessments
      WHERE ${assessmentFilterSql}
    )
  `);
}

function cleanupFixtures() {
  deleteStabilityChildrenMatchingAssessmentFilter(`idempotency_key LIKE '${PREFIX}%' OR input_fingerprint LIKE '${PREFIX}%'`, PREFIX);
  deleteLossDriversMatchingAssessmentFilter(`idempotency_key LIKE '${PREFIX}%' OR input_fingerprint LIKE '${PREFIX}%'`);
  db(`
    DELETE FROM process_oee_assessments
    WHERE idempotency_key LIKE '${PREFIX}%'
       OR input_fingerprint LIKE '${PREFIX}%'
  `);
  db(`
    DELETE FROM process_oee_evaluation_windows
    WHERE create_idempotency_key LIKE '${PREFIX}%'
  `);
}

function cleanupClosureFixtures() {
  deleteStabilityChildrenMatchingAssessmentFilter(`idempotency_key LIKE '${CLOSURE_PREFIX}%' OR evaluation_window_id IN (
         SELECT id FROM process_oee_evaluation_windows
         WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       )`);
  deleteLossDriversMatchingAssessmentFilter(`idempotency_key LIKE '${CLOSURE_PREFIX}%' OR evaluation_window_id IN (
         SELECT id FROM process_oee_evaluation_windows
         WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       )`);
  db(`
    DELETE FROM process_oee_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR evaluation_window_id IN (
         SELECT id FROM process_oee_evaluation_windows
         WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       )
  `);
  db(`
    DELETE FROM process_oee_evaluation_windows
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `);
}

function closureWindowCount() {
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_evaluation_windows
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `));
}

function closureAssessmentCount() {
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
  `));
}

function assessmentCountForWindow(windowId) {
  return Number(db(`
    SELECT COUNT(*) FROM process_oee_assessments
    WHERE evaluation_window_id = '${windowId}'
  `));
}

function readFailureCode(json) {
  return json?.failureCode ?? json?.extensions?.failureCode ?? null;
}

console.log("Running PROCESS-OEE-1 calculation verifier.");

const constants = readRequired("packages/NevaxisZentra.Persistence/ProcessOee/ProcessOeeConstants.cs");
const calculator = readRequired("packages/NevaxisZentra.Persistence/ProcessOee/ProcessOeeCalculator.cs");
const operating = readRequired("packages/NevaxisZentra.Persistence/ProcessOee/ProcessOeeOperatingTimeCalculator.cs");
assert("rule-set PROCESS-OEE-V1", constants.includes("PROCESS-OEE-V1"));
assert("availability formula", calculator.includes("OperatingSeconds") && calculator.includes("PlannedProductionSeconds"));
assert("performance raw warning", calculator.includes("performance-above-100"));
assert("derived shot fallback", calculator.includes("derived-shot-count"));
assert("downtime merge", operating.includes("MergeOverlappingIntervals"));
assert("window clip", operating.includes("ClipIntervalsToWindow"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

cleanupFixtures();

const start = new Date("2026-08-10T06:00:00.000Z");
const end = new Date("2026-08-10T14:00:00.000Z");
const completeKey = `${PREFIX}-complete`;
const completeBody = {
  machineAssetId: MACHINE,
  productionOrderReference: "PILOT-PO-PP-001",
  moldReference: "PILOT-MOLD-PP-4CAV",
  windowStartUtc: start.toISOString(),
  windowEndUtc: end.toISOString(),
  plannedProductionSeconds: 28800,
  idealCycleTimeSeconds: 45,
  effectiveCavityCount: 4,
  idempotencyKey: completeKey,
};

const completeEval = await request(
  "POST",
  `/api/process-oee/evaluations/bounded?facilityId=${FACILITY}`,
  completeBody,
);
assert("bounded evaluation accepted", completeEval.status === 201 || completeEval.status === 200, `status=${completeEval.status}`);

const completeAssessmentId = completeEval.json?.entityId ?? completeEval.json?.id;
assert("complete assessment id returned", Boolean(completeAssessmentId));

const completeDetail = await request(
  "GET",
  `/api/process-oee/assessments/${completeAssessmentId}?facilityId=${FACILITY}`,
);
assert("complete assessment readable", completeDetail.status === 200);
assert(
  "complete status calculated or insufficient",
  ["calculated", "insufficient-data", "review-required", "stale-data"].includes(completeDetail.json?.status),
  `status=${completeDetail.json?.status}`,
);
assert("rule-set exposed", completeDetail.json?.ruleSetVersion === "PROCESS-OEE-V1");

const replayEval = await request(
  "POST",
  `/api/process-oee/evaluation-windows/bb000001-00c0-4000-8000-0000000000c0/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `${PREFIX}-pilot-replay` },
);
assert("pilot window evaluation accepted", replayEval.status === 201 || replayEval.status === 200, `status=${replayEval.status}`);
const replayAssessmentId = replayEval.json?.entityId ?? replayEval.json?.id;

const replayEval2 = await request(
  "POST",
  `/api/process-oee/evaluation-windows/bb000001-00c0-4000-8000-0000000000c0/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `${PREFIX}-pilot-replay` },
);
assert("idempotent replay", replayEval2.status === 201 || replayEval2.status === 200);
assert(
  "idempotent replay same entity",
  (replayEval2.json?.entityId ?? replayEval2.json?.id) === replayAssessmentId || replayEval2.json?.idempotentReplay === true,
);

const conflictEval = await request(
  "POST",
  `/api/process-oee/evaluation-windows/bb000001-00c0-4000-8000-0000000000c0/evaluations?facilityId=${FACILITY}`,
  { idempotencyKey: `${PREFIX}-pilot-replay` },
);
assert("second idempotent call still succeeds or replays", conflictEval.status === 201 || conflictEval.status === 200);

const windowConflictKey = `${PREFIX}-window-conflict`;
const windowFirst = await request(
  "POST",
  `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
  {
    machineAssetId: MACHINE,
    windowStartUtc: start.toISOString(),
    windowEndUtc: end.toISOString(),
    plannedProductionSeconds: 28800,
    idempotencyKey: windowConflictKey,
  },
);
assert("window create accepted", windowFirst.status === 201 || windowFirst.status === 200, `status=${windowFirst.status}`);
const windowConflict = await request(
  "POST",
  `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
  {
    machineAssetId: MACHINE,
    windowStartUtc: start.toISOString(),
    windowEndUtc: end.toISOString(),
    plannedProductionSeconds: 14400,
    idempotencyKey: windowConflictKey,
  },
);
assert("conflicting window replay → 409", windowConflict.status === 409);

const insufficientBody = {
  machineAssetId: MACHINE,
  windowStartUtc: start.toISOString(),
  windowEndUtc: end.toISOString(),
  idempotencyKey: `${PREFIX}-insufficient`,
};
const insufficientEval = await request(
  "POST",
  `/api/process-oee/evaluations/bounded?facilityId=${FACILITY}`,
  insufficientBody,
);
assert("insufficient evaluation accepted", insufficientEval.status === 201 || insufficientEval.status === 200);
const insufficientId = insufficientEval.json?.entityId;
const insufficientDetail = await request(
  "GET",
  `/api/process-oee/assessments/${insufficientId}?facilityId=${FACILITY}`,
);
assert(
  "insufficient-data status",
  insufficientDetail.json?.status === "insufficient-data"
    || insufficientDetail.json?.oeePercent == null,
  `status=${insufficientDetail.json?.status}`,
);

const invalidBody = {
  machineAssetId: MACHINE,
  windowStartUtc: end.toISOString(),
  windowEndUtc: start.toISOString(),
  plannedProductionSeconds: -10,
  idempotencyKey: `${PREFIX}-invalid`,
};
const invalid = await request(
  "POST",
  `/api/process-oee/evaluations/bounded?facilityId=${FACILITY}`,
  invalidBody,
);
assert("negative/invalid values → 400", invalid.status === 400);

cleanupFixtures();

cleanupClosureFixtures();

const closureWindowStart = new Date("2026-08-11T06:00:00.000Z");
const closureWindowEnd = new Date("2026-08-11T10:00:00.000Z");
const closureWindowKey = `${CLOSURE_PREFIX}-WINDOW-${CLOSURE_RUN}`;
const closureEvalKey = `${CLOSURE_PREFIX}-EVAL-${CLOSURE_RUN}`;

const closureWindowCreate = await request(
  "POST",
  `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
  {
    machineAssetId: MACHINE,
    windowStartUtc: closureWindowStart.toISOString(),
    windowEndUtc: closureWindowEnd.toISOString(),
    plannedProductionSeconds: 14400,
    idealCycleTimeSeconds: 45,
    effectiveCavityCount: 4,
    sourceReference: `${CLOSURE_PREFIX}-CONCURRENT-WINDOW`,
    idempotencyKey: closureWindowKey,
  },
);
assert(
  "closure concurrent window created",
  closureWindowCreate.status === 201 || closureWindowCreate.status === 200,
  `status=${closureWindowCreate.status}`,
);
const closureWindowId = closureWindowCreate.json?.id;
assert("closure concurrent window id returned", Boolean(closureWindowId));

const assessmentCountBefore = assessmentCountForWindow(closureWindowId);
assert("closure concurrent assessment baseline zero", assessmentCountBefore === 0, `count=${assessmentCountBefore}`);

const concurrentPath = `/api/process-oee/evaluation-windows/${closureWindowId}/evaluations?facilityId=${FACILITY}`;
const concurrentBody = { idempotencyKey: closureEvalKey };
const [concurrentA, concurrentB] = await Promise.all([
  request("POST", concurrentPath, concurrentBody),
  request("POST", concurrentPath, concurrentBody),
]);

assert("concurrent identical evaluation A accepted", concurrentA.status === 200 || concurrentA.status === 201, `status=${concurrentA.status}`);
assert("concurrent identical evaluation B accepted", concurrentB.status === 200 || concurrentB.status === 201, `status=${concurrentB.status}`);
assert("concurrent identical evaluation no HTTP 500", concurrentA.status !== 500 && concurrentB.status !== 500);
assert(
  "concurrent identical evaluation raw database exception not exposed",
  !concurrentA.raw.includes("duplicate key") && !concurrentB.raw.includes("duplicate key"),
);

const concurrentIdA = concurrentA.json?.id;
const concurrentIdB = concurrentB.json?.id;
assert("concurrent identical evaluation ids returned", Boolean(concurrentIdA) && Boolean(concurrentIdB));
assert("concurrent identical evaluation same assessment identity", concurrentIdA === concurrentIdB, `a=${concurrentIdA} b=${concurrentIdB}`);

const replayFlags = [concurrentA.json?.idempotentReplay, concurrentB.json?.idempotentReplay];
assert(
  "concurrent replay semantics",
  replayFlags.filter(Boolean).length === 1 && replayFlags.includes(false),
  `flags=${JSON.stringify(replayFlags)}`,
);

const assessmentCountAfter = assessmentCountForWindow(closureWindowId);
assert(
  "concurrent identical evaluation count increase exactly one",
  assessmentCountAfter - assessmentCountBefore === 1,
  `before=${assessmentCountBefore} after=${assessmentCountAfter}`,
);
assert("concurrent duplicate assessment not created", assessmentCountAfter === 1);
assert("concurrent duplicate evaluation window not created", closureWindowCount() === 1);

const legacyWindowKey = `${CLOSURE_PREFIX}-LEGACY-${CLOSURE_RUN}`;
const legacyWindowBody = {
  machineAssetId: MACHINE,
  windowStartUtc: new Date("2026-08-11T11:00:00.000Z").toISOString(),
  windowEndUtc: new Date("2026-08-11T15:00:00.000Z").toISOString(),
  plannedProductionSeconds: 14400,
  idealCycleTimeSeconds: 45,
  effectiveCavityCount: 4,
  sourceReference: `${CLOSURE_PREFIX}-LEGACY-WINDOW`,
  idempotencyKey: legacyWindowKey,
};

const legacyCreate = await request(
  "POST",
  `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
  legacyWindowBody,
);
assert("legacy window fixture created", legacyCreate.status === 201 || legacyCreate.status === 200, `status=${legacyCreate.status}`);
const legacyWindowId = legacyCreate.json?.id;
assert("legacy window id returned", Boolean(legacyWindowId));

db(`
  UPDATE process_oee_evaluation_windows
  SET create_idempotency_payload_fingerprint = NULL
  WHERE id = '${legacyWindowId}'
`);

assert(
  "legacy stored fingerprint is NULL",
  db(`SELECT create_idempotency_payload_fingerprint IS NULL FROM process_oee_evaluation_windows WHERE id = '${legacyWindowId}'`) === "t",
);

const legacyWindowCountBefore = closureWindowCount();
const legacyAssessmentCountBefore = closureAssessmentCount();
const legacyVersionBefore = db(`SELECT version::text FROM process_oee_evaluation_windows WHERE id = '${legacyWindowId}'`);

const legacyRetry = await request(
  "POST",
  `/api/process-oee/evaluation-windows?facilityId=${FACILITY}`,
  legacyWindowBody,
);

assert("legacy null fingerprint retry → 409", legacyRetry.status === 409, `status=${legacyRetry.status}`);
assert(
  "legacy null fingerprint failure code",
  readFailureCode(legacyRetry.json) === "process-oee.window.idempotency-legacy-unverifiable",
  `failureCode=${readFailureCode(legacyRetry.json)}`,
);
assert("legacy null fingerprint not replayed", legacyRetry.json?.idempotentReplay !== true);
assert("legacy duplicate window not created", closureWindowCount() === legacyWindowCountBefore);
assert("legacy duplicate assessment not created", closureAssessmentCount() === legacyAssessmentCountBefore);
assert(
  "legacy original window unchanged",
  db(`SELECT version::text FROM process_oee_evaluation_windows WHERE id = '${legacyWindowId}'`) === legacyVersionBefore,
);
assert(
  "legacy fingerprint remains NULL",
  db(`SELECT create_idempotency_payload_fingerprint IS NULL FROM process_oee_evaluation_windows WHERE id = '${legacyWindowId}'`) === "t",
);

const closureWindowsBeforeCleanup = closureWindowCount();
const closureAssessmentsBeforeCleanup = closureAssessmentCount();
assert("closure fixtures previewed", closureWindowsBeforeCleanup >= 2 && closureAssessmentsBeforeCleanup >= 1);

cleanupClosureFixtures();

assert("closure windows remaining zero", closureWindowCount() === 0);
assert("closure assessments remaining zero", closureAssessmentCount() === 0);
assert(
  "orphan assessments zero",
  Number(db(`
    SELECT COUNT(*) FROM process_oee_assessments a
    LEFT JOIN process_oee_evaluation_windows w ON w.id = a.evaluation_window_id
    WHERE w.id IS NULL
  `)) === 0,
);

const pilotCount = Number(db(`
  SELECT COUNT(*) FROM process_oee_evaluation_windows
  WHERE id IN (
    'bb000001-00c0-4000-8000-0000000000c0',
    'bb000001-00c1-4000-8000-0000000000c1',
    'bb000001-00c2-4000-8000-0000000000c2'
  )
`));
assert("pilot windows preserved", pilotCount === 3, `count=${pilotCount}`);

console.log("PROCESS-OEE-1 calculation verification passed.");

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
const PILOT_ASSESSMENT = "bb000001-0120-4000-8000-000000000120";
const CLOSURE_PREFIX = "SHIFTRISK1-CLOSURE";
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
  console.error(`SHIFT-RISK-1 security/idempotency verification failed: ${message}`);
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

function closureRequirementPredicate() {
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
    DELETE FROM shift_workforce_persons
    WHERE employee_code LIKE '${CLOSURE_PREFIX}%';
  `);
}

function pilotShiftDate() {
  return db(`SELECT shift_date::text FROM shift_coverage_requirements WHERE id = 'bb000001-0110-4000-8000-000000000110'`);
}

console.log("Running SHIFT-RISK-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/ShiftRisk/ShiftRiskEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/ShiftRisk/ShiftRiskEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260813050000_AddShiftCoverageRiskFoundation.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/ShiftRisk/ShiftRiskPermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("write access helper", helpers.includes("ResolveWriteAccessAsync"));
assert("shift-risk.read key", permissions.includes("shift-risk.read"));
assert("shift-risk.manage key", permissions.includes("shift-risk.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE shift_risk_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE shift_risk_audit_events", "audit events");
forbidText(endpoints, "MapDelete", "shift-risk endpoints");

assert("bootstrap read seed", bootstrap.includes("EnsureShiftRiskReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureShiftRiskManagePermissionAssignmentAsync"));

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
const requirementCountBefore = Number(db("SELECT COUNT(*) FROM shift_coverage_requirements"));
const auditCountBefore = Number(db("SELECT COUNT(*) FROM shift_risk_audit_events"));

const shiftDate = pilotShiftDate();
assert("pilot shift date loaded", Boolean(shiftDate), shiftDate);

// L — auth matrix
assert("missing auth → 401", (await liveRequest("GET", `/api/shift-risks/overview?facilityId=${FACILITY}`, null, { "Content-Type": "application/json" })).status === 401);
assert("authorized manager overview → 200", (await liveRequest("GET", `/api/shift-risks/overview?facilityId=${FACILITY}`, null, managerHeaders)).status === 200);
assert("authorized manager assessments → 200", (await liveRequest("GET", `/api/shift-risks/assessments?facilityId=${FACILITY}`, null, managerHeaders)).status === 200);
assert("read-only overview → 200", (await liveRequest("GET", `/api/shift-risks/overview?facilityId=${FACILITY}`, null, readonlyHeaders)).status === 200);
assert("read-only assessments → 200", (await liveRequest("GET", `/api/shift-risks/assessments?facilityId=${FACILITY}`, null, readonlyHeaders)).status === 200);

const readonlyEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/evaluations?facilityId=${FACILITY}`,
  {
    productionShiftId: SHIFT,
    shiftDate,
    productionOrderReference: "PILOT-PO-PP-001",
    machineAssetId: INJ2,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-ro-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("read-only evaluate → 403", readonlyEvaluate.status === 403, `status=${readonlyEvaluate.status}`);

const readonlyCreate = await liveRequest(
  "POST",
  `/api/shift-risks/requirements?facilityId=${FACILITY}`,
  {
    productionShiftId: SHIFT,
    shiftDate,
    productionOrderReference: `${CLOSURE_PREFIX}-ro-req-${RUN_SUFFIX}`,
    machineAssetId: INJ2,
    requiredCompetencyCode: INJECTION,
    minimumApprovedOperatorCount: 1,
    isCriticalRole: false,
    sourceReference: "closure-readonly",
    isActive: true,
    idempotencyKey: `${CLOSURE_PREFIX}-ro-create-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("read-only create requirement → 403", readonlyCreate.status === 403, `status=${readonlyCreate.status}`);

assert("unauthorized actor → 403", (await liveRequest("GET", `/api/shift-risks/overview?facilityId=${FACILITY}`, null, negativeHeaders)).status === 403);
assert("wrong facility → 403", (await liveRequest("GET", `/api/shift-risks/overview?facilityId=${UNAUTH_FACILITY}`, null, managerHeaders)).status === 403);
assert("cross-tenant → 403", (await liveRequest("GET", `/api/shift-risks/overview?facilityId=${FACILITY}`, null, crossTenantHeaders)).status === 403);

const deniedEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/evaluations?facilityId=${FACILITY}`,
  {
    productionShiftId: SHIFT,
    shiftDate,
    productionOrderReference: "PILOT-PO-PP-001",
    machineAssetId: INJ2,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-denied-${RUN_SUFFIX}`,
  },
  negativeHeaders,
);
assert("denied evaluate → 403", deniedEvaluate.status === 403, `status=${deniedEvaluate.status}`);

const assessmentCountAfterDenied = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
assert("denied evaluate created no assessments", assessmentCountAfterDenied === assessmentCountBefore, `${assessmentCountBefore}→${assessmentCountAfterDenied}`);

// I — exact idempotent evaluate replay
const evaluateKey = `${CLOSURE_PREFIX}-evaluate-${RUN_SUFFIX}`;
const evaluateBody = {
  productionShiftId: SHIFT,
  shiftDate,
  productionOrderReference: `${CLOSURE_PREFIX}-EVAL-${RUN_SUFFIX}`,
  machineAssetId: INJ2,
  requiredCompetencyCode: INJECTION,
  idempotencyKey: evaluateKey,
};

const firstEvaluate = await liveRequest("POST", `/api/shift-risks/evaluations?facilityId=${FACILITY}`, evaluateBody, managerHeaders);
assert("I evaluate created", firstEvaluate.status === 200, `status=${firstEvaluate.status} ${firstEvaluate.text}`);
const firstAssessmentId = firstEvaluate.json?.assessmentId;
assert("I assessment id returned", Boolean(firstAssessmentId), firstEvaluate.text);

const replayEvaluate = await liveRequest("POST", `/api/shift-risks/evaluations?facilityId=${FACILITY}`, evaluateBody, managerHeaders);
assert("I exact idempotent evaluate replay", replayEvaluate.status === 200 && replayEvaluate.json?.idempotentReplay === true);
assert("I replay same assessment id", replayEvaluate.json?.assessmentId === firstAssessmentId);

const assessmentCountAfterReplay = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
assert("I no duplicate assessment row", assessmentCountAfterReplay === assessmentCountBefore + 1, `${assessmentCountBefore + 1} expected, got ${assessmentCountAfterReplay}`);

// L — concurrent identical evaluation
const concurrentCountBefore = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
const concurrentKey = `${CLOSURE_PREFIX}-concurrent-${RUN_SUFFIX}`;
const concurrentBody = {
  productionShiftId: SHIFT,
  shiftDate,
  productionOrderReference: `${CLOSURE_PREFIX}-CONCURRENT-${RUN_SUFFIX}`,
  machineAssetId: INJ2,
  requiredCompetencyCode: INJECTION,
  idempotencyKey: concurrentKey,
};
const concurrentResults = await Promise.all([
  liveRequest("POST", `/api/shift-risks/evaluations?facilityId=${FACILITY}`, concurrentBody, managerHeaders),
  liveRequest("POST", `/api/shift-risks/evaluations?facilityId=${FACILITY}`, concurrentBody, managerHeaders),
]);
assert(
  "concurrent identical requests",
  concurrentResults.every((result) => result.status === 200),
  concurrentResults.map((result) => result.status).join(","),
);
const concurrentStatuses = concurrentResults.map((result) => result.status).join(",");
const concurrentIds = new Set(concurrentResults.map((result) => result.json?.assessmentId).filter(Boolean));
assert("concurrent same assessment identity", concurrentIds.size === 1, [...concurrentIds].join(","));
const concurrentAssessmentId = [...concurrentIds][0];
const concurrentCountAfter = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
assert(
  "concurrent assessment count increase",
  concurrentCountAfter === concurrentCountBefore + 1,
  `${concurrentCountBefore + 1} expected, got ${concurrentCountAfter}`,
);
const duplicateDriverRows = Number(
  db(`
    SELECT COUNT(*) FROM shift_risk_drivers d
    WHERE d.assessment_id = '${concurrentAssessmentId}'
      AND EXISTS (
        SELECT 1 FROM shift_risk_drivers d2
        WHERE d2.assessment_id = d.assessment_id
          AND d2.reason_code = d.reason_code
          AND d2.id <> d.id
      )
  `),
);
assert("concurrent duplicate drivers", duplicateDriverRows === 0, `count=${duplicateDriverRows}`);
assert(
  "concurrent no HTTP 500",
  concurrentResults.every((result) => result.status !== 500),
  concurrentStatuses,
);

// M — concurrent different-payload idempotency conflict
const concurrentConflictResults = await Promise.all([
  liveRequest("POST", `/api/shift-risks/evaluations?facilityId=${FACILITY}`, concurrentBody, managerHeaders),
  liveRequest(
    "POST",
    `/api/shift-risks/evaluations?facilityId=${FACILITY}`,
    {
      ...concurrentBody,
      productionOrderReference: `${CLOSURE_PREFIX}-CONCURRENT-CONFLICT-${RUN_SUFFIX}`,
    },
    managerHeaders,
  ),
]);
const conflictStatuses = concurrentConflictResults.map((result) => result.status).sort();
assert(
  "concurrent different-payload result",
  conflictStatuses.includes(200) && conflictStatuses.includes(409),
  conflictStatuses.join(","),
);
const conflictWinner = concurrentConflictResults.find((result) => result.status === 200);
assert("concurrent conflict HTTP status", conflictStatuses.includes(409), conflictStatuses.join(","));
const conflictFailure = concurrentConflictResults.find((result) => result.status === 409);
assert(
  "concurrent conflict error code",
  conflictFailure?.json?.failureCode === "shift-risk.idempotency-conflict",
  conflictFailure?.json?.failureCode,
);
const winnerReadback = await liveRequest(
  "GET",
  `/api/shift-risks/assessments/${conflictWinner?.json?.assessmentId}?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert(
  "concurrent winner not mutated",
  winnerReadback.json?.productionOrderReference === concurrentBody.productionOrderReference,
  winnerReadback.json?.productionOrderReference,
);
const concurrentCountAfterConflict = Number(db("SELECT COUNT(*) FROM shift_risk_assessments"));
assert(
  "concurrent conflict no extra assessment",
  concurrentCountAfterConflict === concurrentCountBefore + 1,
  `${concurrentCountBefore + 1} expected, got ${concurrentCountAfterConflict}`,
);

// J — conflicting idempotent retry
const conflictEvaluate = await liveRequest(
  "POST",
  `/api/shift-risks/evaluations?facilityId=${FACILITY}`,
  {
    ...evaluateBody,
    productionOrderReference: `${CLOSURE_PREFIX}-EVAL-CONFLICT-${RUN_SUFFIX}`,
  },
  managerHeaders,
);
assert("J conflicting idempotent retry → 409", conflictEvaluate.status === 409, `status=${conflictEvaluate.status}`);
assert("J stable conflict code", conflictEvaluate.json?.failureCode === "shift-risk.idempotency-conflict");

const createKey = `${CLOSURE_PREFIX}-create-req-${RUN_SUFFIX}`;
const createBody = {
  productionShiftId: SHIFT,
  shiftDate,
  productionOrderReference: `${CLOSURE_PREFIX}-REQ-${RUN_SUFFIX}`,
  machineAssetId: INJ2,
  requiredCompetencyCode: INJECTION,
  minimumApprovedOperatorCount: 1,
  isCriticalRole: false,
  sourceReference: "closure-create",
  isActive: true,
  idempotencyKey: createKey,
};

const createdRequirement = await liveRequest("POST", `/api/shift-risks/requirements?facilityId=${FACILITY}`, createBody, managerHeaders);
assert("requirement create for idempotency", createdRequirement.status === 201, `status=${createdRequirement.status} ${createdRequirement.text}`);
const requirementId = createdRequirement.json?.id;

const replayCreate = await liveRequest("POST", `/api/shift-risks/requirements?facilityId=${FACILITY}`, createBody, managerHeaders);
assert("create requirement idempotent replay", replayCreate.status === 201 && replayCreate.json?.idempotentReplay === true);
assert("create requirement same id", replayCreate.json?.id === requirementId);

const conflictCreate = await liveRequest(
  "POST",
  `/api/shift-risks/requirements?facilityId=${FACILITY}`,
  { ...createBody, minimumApprovedOperatorCount: 2 },
  managerHeaders,
);
assert("create requirement idempotency conflict → 409", conflictCreate.status === 409, `status=${conflictCreate.status}`);

// N — optimistic concurrency on coverage requirement
const ocVersionBefore = Number(createdRequirement.json?.version);
assert("requirement version before update", ocVersionBefore >= 1, String(ocVersionBefore));
const currentUpdate = await liveRequest(
  "PUT",
  `/api/shift-risks/requirements/${requirementId}?facilityId=${FACILITY}`,
  {
    expectedVersion: ocVersionBefore,
    minimumApprovedOperatorCount: 2,
    isCriticalRole: false,
    sourceReference: "closure-create-updated",
    effectiveFrom: null,
    effectiveUntil: null,
    isActive: true,
  },
  managerHeaders,
);
assert("current-version update result", currentUpdate.status === 200, `status=${currentUpdate.status} ${currentUpdate.text}`);
const ocVersionAfter = Number(currentUpdate.json?.version);
assert("requirement version incremented", ocVersionAfter === ocVersionBefore + 1, `${ocVersionBefore + 1} expected, got ${ocVersionAfter}`);
const staleUpdate = await liveRequest(
  "PUT",
  `/api/shift-risks/requirements/${requirementId}?facilityId=${FACILITY}`,
  {
    expectedVersion: ocVersionBefore,
    minimumApprovedOperatorCount: 3,
    isCriticalRole: true,
    sourceReference: "closure-create-stale",
    effectiveFrom: null,
    effectiveUntil: null,
    isActive: true,
  },
  managerHeaders,
);
assert("stale-version update result", staleUpdate.status === 409, `status=${staleUpdate.status}`);
assert(
  "concurrency conflict code",
  staleUpdate.json?.failureCode === "shift-risk.concurrency-conflict",
  staleUpdate.json?.failureCode,
);
const requirementAfterStale = await liveRequest(
  "GET",
  `/api/shift-risks/requirements?facilityId=${FACILITY}&productionOrderReference=${createBody.productionOrderReference}`,
  null,
  managerHeaders,
);
const requirementItem = requirementAfterStale.json?.items?.[0];
assert(
  "stale update did not mutate requirement",
  requirementItem?.minimumApprovedOperatorCount === 2 && requirementItem?.version === ocVersionAfter,
  JSON.stringify(requirementItem),
);

// K — pilot readback survives closure cleanup
const pilotReadback = await liveRequest("GET", `/api/shift-risks/assessments/${PILOT_ASSESSMENT}?facilityId=${FACILITY}`, null, managerHeaders);
assert("K restart/readback pilot assessment", pilotReadback.status === 200 && pilotReadback.json?.outcome === "clear");

removeClosureFixtures();
const remainingAssessments = Number(db(`SELECT COUNT(*) FROM shift_risk_assessments WHERE ${closureAssessmentPredicate()}`));
const remainingRequirements = Number(db(`SELECT COUNT(*) FROM shift_coverage_requirements WHERE ${closureRequirementPredicate()}`));
assert("closure assessments removed", remainingAssessments === 0, `count=${remainingAssessments}`);
assert("closure requirements removed", remainingRequirements === 0, `count=${remainingRequirements}`);

const pilotStillThere = Number(db(`SELECT COUNT(*) FROM shift_risk_assessments WHERE id = '${PILOT_ASSESSMENT}'`));
assert("pilot assessment preserved", pilotStillThere === 1);

const requirementCountAfter = Number(db("SELECT COUNT(*) FROM shift_coverage_requirements"));
assert("pilot requirements preserved after closure", requirementCountAfter === requirementCountBefore, `${requirementCountBefore}→${requirementCountAfter}`);

const auditCountAfter = Number(db("SELECT COUNT(*) FROM shift_risk_audit_events"));
assert("audit events append-only growth", auditCountAfter >= auditCountBefore, `${auditCountBefore}→${auditCountAfter}`);

console.log("SHIFT-RISK-1 security/idempotency verification passed.");

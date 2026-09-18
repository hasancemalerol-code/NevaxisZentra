#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const MACHINE = "bb000001-0096-4000-8000-000000000096";
const SHIFT = "bb000001-0040-4000-8000-000000000040";
const PO = "PILOT-PO-PP-001";
const INJECTION = "injection-machine-operation";
const PERSON002 = "bb000001-00d1-4000-8000-0000000000d1";
const PILOT_ASSIGNMENT = "bb000001-0102-4000-8000-000000000102";
const CLOSURE_PREFIX = "SHIFTASSIGN1-CLOSURE";
const RUN_SUFFIX = Date.now();

function fail(message) {
  console.error(`SHIFT-ASSIGN-1 security/idempotency verification failed: ${message}`);
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
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(subject, tenant = "dev-pilot-tenant") {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": "crevafactos-dev",
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisZentra-Tenant": tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders("planning-pilot-user")) {
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

function candidateQuery(shiftDate) {
  const params = new URLSearchParams({
    facilityId: FACILITY,
    productionOrderReference: PO,
    machineAssetId: MACHINE,
    productionShiftId: SHIFT,
    shiftDate,
    requiredCompetencyCode: INJECTION,
  });
  return `/api/shift-assignments/candidates?${params}`;
}

function closureAssignmentPredicate(tableName = "shift_operator_assignments") {
  return `${tableName}.create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
}

function removeClosureFixtures() {
  db(`
    DELETE FROM shift_operator_assignment_audit_events
    WHERE entity_id IN (SELECT id FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()});
    DELETE FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()};
  `);
}

console.log("Running SHIFT-ASSIGN-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/ShiftAssignment/ShiftAssignmentEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/ShiftAssignment/ShiftAssignmentEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260813040000_AddShiftOperatorAssignmentFoundation.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/ShiftAssignment/ShiftAssignmentPermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("write access helper", helpers.includes("ResolveWriteAccessAsync"));
assert("shift-assign.read key", permissions.includes("shift-assign.read"));
assert("shift-assign.manage key", permissions.includes("shift-assign.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE shift_operator_assignment_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE shift_operator_assignment_audit_events", "audit events");
forbidText(endpoints, "MapDelete", "shift-assign endpoints");

assert("bootstrap read seed", bootstrap.includes("EnsureShiftAssignReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureShiftAssignManagePermissionAssignmentAsync"));

const managerHeaders = actorHeaders("planning-pilot-user");
const reviewerHeaders = actorHeaders("planning-pilot-reviewer");
const negativeHeaders = actorHeaders("quality-negative-test-user");
const crossTenantHeaders = actorHeaders("planning-pilot-user", "unknown-tenant-key");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();
db(`UPDATE shift_workforce_persons SET source_shift_reference = 'V1' WHERE id = '${PERSON002}'`);

const assignmentCountBefore = Number(db("SELECT COUNT(*) FROM shift_operator_assignments"));
const auditCountBefore = Number(db("SELECT COUNT(*) FROM shift_operator_assignment_audit_events"));

assert("missing auth → 401", (await liveRequest("GET", candidateQuery("2026-10-01"), null, { "Content-Type": "application/json" })).status === 401);
assert("authorized manager GET → 200", (await liveRequest("GET", candidateQuery("2026-10-01"), null, managerHeaders)).status === 200);
assert("read-only GET → 200", (await liveRequest("GET", candidateQuery("2026-10-01"), null, reviewerHeaders)).status === 200);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/shift-assignments/assignments?facilityId=${FACILITY}`,
  {
    productionOrderReference: PO,
    machineAssetId: MACHINE,
    productionShiftId: SHIFT,
    shiftDate: "2026-10-01",
    workforcePersonId: PERSON002,
    plannedStartUtc: "2026-10-01T05:00:00.000Z",
    plannedEndUtc: "2026-10-01T13:00:00.000Z",
    eligibilityAssessmentId: "00000000-0000-4000-8000-000000000001",
    eligibilityResultId: "00000000-0000-4000-8000-000000000002",
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-ro-${RUN_SUFFIX}`,
  },
  reviewerHeaders,
);
assert("read-only write → 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

assert("unauthorized actor → 403", (await liveRequest("GET", candidateQuery("2026-10-01"), null, negativeHeaders)).status === 403);
assert("wrong facility → 403", (await liveRequest("GET", candidateQuery("2026-10-01").replace(FACILITY, UNAUTH_FACILITY), null, managerHeaders)).status === 403);
assert("cross-tenant → 403", (await liveRequest("GET", candidateQuery("2026-10-01"), null, crossTenantHeaders)).status === 403);

const deniedWrite = await liveRequest(
  "POST",
  `/api/shift-assignments/assignments?facilityId=${FACILITY}`,
  {
    productionOrderReference: PO,
    machineAssetId: MACHINE,
    productionShiftId: SHIFT,
    shiftDate: "2026-10-01",
    workforcePersonId: PERSON002,
    plannedStartUtc: "2026-10-01T05:00:00.000Z",
    plannedEndUtc: "2026-10-01T13:00:00.000Z",
    eligibilityAssessmentId: "00000000-0000-4000-8000-000000000001",
    eligibilityResultId: "00000000-0000-4000-8000-000000000002",
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-denied-${RUN_SUFFIX}`,
  },
  negativeHeaders,
);
assert("denied write → 403", deniedWrite.status === 403, `status=${deniedWrite.status}`);

const assignmentCountAfterDenied = Number(db("SELECT COUNT(*) FROM shift_operator_assignments"));
assert("denied request created no assignment rows", assignmentCountAfterDenied === assignmentCountBefore, `${assignmentCountBefore}→${assignmentCountAfterDenied}`);

const shiftDate = "2026-10-15";
const candidates = await liveRequest("GET", candidateQuery(shiftDate), null, managerHeaders);
assert("candidate read for approval flow", candidates.status === 200, `status=${candidates.status}`);
const person002 = candidates.json.candidates.find((item) => item.personId === PERSON002);
assert("eligible/conditional candidate for approval", person002?.outcome === "eligible" || person002?.outcome === "conditional", person002?.outcome);

const createKey = `${CLOSURE_PREFIX}-create-${RUN_SUFFIX}`;
const createBody = {
  productionOrderReference: PO,
  machineAssetId: MACHINE,
  productionShiftId: SHIFT,
  shiftDate,
  workforcePersonId: PERSON002,
  plannedStartUtc: `${shiftDate}T05:00:00.000Z`,
  plannedEndUtc: `${shiftDate}T13:00:00.000Z`,
  eligibilityAssessmentId: candidates.json.assessmentId,
  eligibilityResultId: person002.eligibilityResultId,
  requiredCompetencyCode: INJECTION,
  idempotencyKey: createKey,
};

const draft = await liveRequest("POST", `/api/shift-assignments/assignments?facilityId=${FACILITY}`, createBody, managerHeaders);
assert("E draft assignment created", draft.status === 201, `status=${draft.status} ${draft.text}`);
const assignmentId = draft.json.id;
assert("draft before approval status", (await liveRequest("GET", `/api/shift-assignments/assignments/${assignmentId}?facilityId=${FACILITY}`, null, managerHeaders)).json.status === "draft");

const approveKey = `${CLOSURE_PREFIX}-approve-${RUN_SUFFIX}`;
const approve = await liveRequest(
  "POST",
  `/api/shift-assignments/assignments/${assignmentId}/approve?facilityId=${FACILITY}`,
  { expectedVersion: 1, idempotencyKey: approveKey },
  managerHeaders,
);
assert("E human approval succeeds", approve.status === 200, `status=${approve.status} ${approve.text}`);

const approved = await liveRequest("GET", `/api/shift-assignments/assignments/${assignmentId}?facilityId=${FACILITY}`, null, managerHeaders);
assert("E approved status persisted", approved.json.status === "approved");
assert("E approver recorded", Boolean(approved.json.approvedByDisplayName));
assert("E approval timestamp recorded", Boolean(approved.json.approvedAtUtc));
assert("E eligibility assessment linked", approved.json.eligibilityAssessmentId === candidates.json.assessmentId);

const replayApprove = await liveRequest(
  "POST",
  `/api/shift-assignments/assignments/${assignmentId}/approve?facilityId=${FACILITY}`,
  { expectedVersion: 1, idempotencyKey: approveKey },
  managerHeaders,
);
assert("F exact idempotent approve replay", replayApprove.status === 200 && replayApprove.json.idempotentReplay === true);

const duplicateCreate = await liveRequest("POST", `/api/shift-assignments/assignments?facilityId=${FACILITY}`, createBody, managerHeaders);
assert("F exact idempotent create replay", duplicateCreate.status === 201 && duplicateCreate.json.idempotentReplay === true);
assert("F no duplicate assignment id", duplicateCreate.json.id === assignmentId);

const conflictCreate = await liveRequest(
  "POST",
  `/api/shift-assignments/assignments?facilityId=${FACILITY}`,
  { ...createBody, plannedEndUtc: `${shiftDate}T14:00:00.000Z` },
  managerHeaders,
);
assert("G conflicting idempotent retry → 409", conflictCreate.status === 409, `status=${conflictCreate.status}`);
assert("G stable conflict code", conflictCreate.json?.failureCode === "shift-assign.idempotency-conflict");

const concurrencyCreateKey = `${CLOSURE_PREFIX}-concurrent-${RUN_SUFFIX}`;
const concurrentCandidates = await liveRequest("GET", candidateQuery("2026-10-16"), null, managerHeaders);
const concurrentPerson = concurrentCandidates.json.candidates.find((item) => item.personId === PERSON002);
const concurrentBody = {
  productionOrderReference: PO,
  machineAssetId: MACHINE,
  productionShiftId: SHIFT,
  shiftDate: "2026-10-16",
  workforcePersonId: PERSON002,
  plannedStartUtc: "2026-10-16T05:00:00.000Z",
  plannedEndUtc: "2026-10-16T13:00:00.000Z",
  eligibilityAssessmentId: concurrentCandidates.json.assessmentId,
  eligibilityResultId: concurrentPerson.eligibilityResultId,
  requiredCompetencyCode: INJECTION,
  idempotencyKey: concurrencyCreateKey,
};
const concurrentDrafts = await Promise.all([
  liveRequest("POST", `/api/shift-assignments/assignments?facilityId=${FACILITY}`, concurrentBody, managerHeaders),
  liveRequest("POST", `/api/shift-assignments/assignments?facilityId=${FACILITY}`, concurrentBody, managerHeaders),
]);
const concurrentIds = new Set(concurrentDrafts.map((r) => r.json?.id).filter(Boolean));
assert("H concurrent create resolves to one assignment", concurrentIds.size === 1, JSON.stringify(concurrentDrafts.map((r) => r.status)));

const concurrentDraftId = [...concurrentIds][0];
const concurrentApproveKey = `${CLOSURE_PREFIX}-capprove-${RUN_SUFFIX}`;
const concurrentApproves = await Promise.all([
  liveRequest(
    "POST",
    `/api/shift-assignments/assignments/${concurrentDraftId}/approve?facilityId=${FACILITY}`,
    { expectedVersion: 1, idempotencyKey: concurrentApproveKey },
    managerHeaders,
  ),
  liveRequest(
    "POST",
    `/api/shift-assignments/assignments/${concurrentDraftId}/approve?facilityId=${FACILITY}`,
    { expectedVersion: 1, idempotencyKey: concurrentApproveKey },
    managerHeaders,
  ),
]);
const approveStatuses = concurrentApproves.map((r) => r.status);
assert("H concurrent approval no HTTP 500", approveStatuses.every((s) => s === 200), approveStatuses.join(","));
assert("H concurrent approval idempotent replay", concurrentApproves.some((r) => r.json?.idempotentReplay === true));
const approvedConcurrent = await liveRequest("GET", `/api/shift-assignments/assignments/${concurrentDraftId}?facilityId=${FACILITY}`, null, managerHeaders);
assert("H one persisted approved assignment", approvedConcurrent.json.status === "approved");

const overlapCandidates = await liveRequest("GET", candidateQuery("2026-08-13"), null, managerHeaders);
const overlapPerson = overlapCandidates.json.candidates.find((item) => item.personId === PERSON002);
assert("I overlap context candidate loaded", Boolean(overlapPerson));

const overlapCreate = await liveRequest(
  "POST",
  `/api/shift-assignments/assignments?facilityId=${FACILITY}`,
  {
    productionOrderReference: PO,
    machineAssetId: MACHINE,
    productionShiftId: SHIFT,
    shiftDate: "2026-08-13",
    workforcePersonId: PERSON002,
    plannedStartUtc: "2026-08-13T05:00:00.000Z",
    plannedEndUtc: "2026-08-13T13:00:00.000Z",
    eligibilityAssessmentId: overlapCandidates.json.assessmentId,
    eligibilityResultId: overlapPerson.eligibilityResultId,
    requiredCompetencyCode: INJECTION,
    idempotencyKey: `${CLOSURE_PREFIX}-overlap-${RUN_SUFFIX}`,
  },
  managerHeaders,
);
if (overlapCreate.status === 201) {
  const overlapApprove = await liveRequest(
    "POST",
    `/api/shift-assignments/assignments/${overlapCreate.json.id}/approve?facilityId=${FACILITY}`,
    { expectedVersion: 1, idempotencyKey: `${CLOSURE_PREFIX}-overlap-approve-${RUN_SUFFIX}` },
    managerHeaders,
  );
  assert("I overlapping approval blocked", overlapApprove.status === 409, `status=${overlapApprove.status}`);
} else {
  assert("I overlapping draft blocked", overlapCreate.status === 400 || overlapCreate.status === 409, `status=${overlapCreate.status}`);
}

const optimistic = await liveRequest(
  "POST",
  `/api/shift-assignments/assignments/${assignmentId}/approve?facilityId=${FACILITY}`,
  { expectedVersion: 99, idempotencyKey: `${CLOSURE_PREFIX}-stale-${RUN_SUFFIX}` },
  managerHeaders,
);
assert("optimistic concurrency conflict", optimistic.status === 409, `status=${optimistic.status}`);

const pilotReadback = await liveRequest("GET", `/api/shift-assignments/assignments/${PILOT_ASSIGNMENT}?facilityId=${FACILITY}`, null, managerHeaders);
assert("K restart/readback pilot assignment", pilotReadback.status === 200 && pilotReadback.json.status === "approved");

removeClosureFixtures();
const remaining = Number(db(`SELECT COUNT(*) FROM shift_operator_assignments WHERE ${closureAssignmentPredicate()}`));
assert("closure fixtures removed", remaining === 0, `count=${remaining}`);
const pilotStillThere = Number(db(`SELECT COUNT(*) FROM shift_operator_assignments WHERE id = '${PILOT_ASSIGNMENT}'`));
assert("pilot assignment preserved", pilotStillThere === 1);

console.log("SHIFT-ASSIGN-1 security/idempotency verification passed.");

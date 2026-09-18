#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "AI-TASK-SLA1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";

function fail(message) {
  console.error(`AI-TASK-SLA-1 permissions verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
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

function sampleCreateBody(overrides = {}) {
  return {
    title: `${CLOSURE_PREFIX} permissions ${RUN_SUFFIX}`,
    summary: "Permission boundary verification task",
    category: "commercial-intelligence",
    priority: "medium",
    riskLevel: "medium",
    detectedIssue: "Permission boundary detected issue",
    reasoning: "Permission boundary reasoning",
    recommendation: {
      title: "Review permission boundary",
      description: "Verify SLA operation permissions",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.8,
      explanation: "Permission boundary confidence",
      modelVersion: "sla-1",
    },
    impact: { summary: "Permission boundary impact" },
    sources: [{
      sourceType: "manual",
      sourceLabel: "Permission verifier",
      valueSummary: "Manual permission source",
    }],
    ...overrides,
  };
}

console.log("Running AI-TASK-SLA-1 permissions verifier.");

const operationsEndpoints = readRequired("apps/api/AiTasks/AiTaskOperationsEndpointRouteBuilderExtensions.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/AiTasks/AiTaskPermissionKeys.cs");
const actions = readRequired("apps/web/src/features/ai-task-center/server/ai-task-actions.ts");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260902125759_AddAiTaskSlaAssignmentEscalationFoundation.cs");

assert("assign permission key", permissions.includes("ai-task.assign"));
assert("acknowledge permission key", permissions.includes("ai-task.acknowledge"));
assert("escalate permission key", permissions.includes("ai-task.escalate"));
assert("manage permission key", permissions.includes("ai-task.manage"));
assert("sla manage permission key", permissions.includes("ai-task.sla.manage"));
assert("assign endpoint uses assign permission", operationsEndpoints.includes("AiTaskPermissionKeys.Assign"));
assert("acknowledge endpoint uses acknowledge permission", operationsEndpoints.includes("AiTaskPermissionKeys.Acknowledge"));
assert("escalate endpoint uses escalate permission", operationsEndpoints.includes("AiTaskPermissionKeys.Escalate"));
assert("evaluate endpoint uses escalate permission", operationsEndpoints.includes("EvaluateEscalationsAsync"));
assert("assign action gate", actions.includes('resolvePermissionContext("ai-task.assign")'));
assert("acknowledge action gate", actions.includes('resolvePermissionContext("ai-task.acknowledge")'));
assert("escalate action gate", actions.includes('resolvePermissionContext("ai-task.escalate")'));
assert("migration seeds assign permission", migration.includes("ai-task.assign"));
assert("migration seeds acknowledge permission", migration.includes("ai-task.acknowledge"));
assert("migration seeds escalate permission", migration.includes("ai-task.escalate"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await fetch(`${BASE}/api/ai-tasks/${crypto.randomUUID()}/assign?facilityId=${FACILITY}`);
assert("unauthenticated assign 401", unauth.status === 401);

const create = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody());
assert("manager create 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;

const detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("manager detail includes sla", Boolean(detail.json?.sla?.state));

const readonlyAssign = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: detail.json.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "Readonly assign attempt",
    source: "manual",
  },
  actorHeaders(REVIEWER),
);
assert("reviewer without assign permission denied", readonlyAssign.status === 403, `status=${readonlyAssign.status}`);

const managerAssign = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: detail.json.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "Manager assign for permission checks",
    source: "manual",
  },
);
assert("manager assign 200", managerAssign.status === 200);

const afterAssign = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
const negativeAck = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/acknowledge?facilityId=${FACILITY}`,
  { expectedVersion: afterAssign.json.version, note: "Negative user acknowledge attempt" },
  actorHeaders(NEGATIVE),
);
assert("non-assignee acknowledge denied", negativeAck.status === 403 || negativeAck.status === 409, `status=${negativeAck.status}`);

const negativeEscalate = await liveRequest(
  "POST",
  `/api/ai-tasks/escalations/evaluate?facilityId=${FACILITY}`,
  {},
  actorHeaders(NEGATIVE),
);
assert("negative user escalate evaluate denied", negativeEscalate.status === 403, `status=${negativeEscalate.status}`);

const wrongFacility = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${UNAUTH_FACILITY}`,
  {
    expectedVersion: afterAssign.json.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "Wrong facility assign attempt",
    source: "manual",
  },
);
assert("wrong facility assign denied", wrongFacility.status === 403 || wrongFacility.status === 404, `status=${wrongFacility.status}`);

const invalidMembership = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: afterAssign.json.version,
    targetMembershipId: "bb000001-0099-4000-8000-000000000099",
    reason: "Invalid membership assign attempt",
    source: "manual",
  },
);
assert("invalid membership assign 400", invalidMembership.status === 400 || invalidMembership.status === 409, `status=${invalidMembership.status}`);

console.log("AI-TASK-SLA-1 permissions verification passed.");

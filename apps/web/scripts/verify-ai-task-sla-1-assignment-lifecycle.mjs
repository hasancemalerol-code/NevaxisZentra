#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CLOSURE_PREFIX = "AI-TASK-SLA1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const MANAGER_MEMBERSHIP = "bb000001-0003-4000-8000-000000000003";
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";

function fail(message) {
  console.error(`AI-TASK-SLA-1 assignment lifecycle verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
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

function sampleCreateBody(overrides = {}) {
  return {
    title: `${CLOSURE_PREFIX} assignment ${RUN_SUFFIX}`,
    summary: "Assignment lifecycle verification task",
    category: "commercial-intelligence",
    priority: "medium",
    riskLevel: "medium",
    detectedIssue: "Assignment lifecycle detected issue",
    reasoning: "Assignment lifecycle reasoning",
    recommendation: {
      title: "Review assignment lifecycle",
      description: "Verify assign, reassign, acknowledge",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.84,
      explanation: "Assignment lifecycle confidence",
      modelVersion: "sla-1",
    },
    impact: { summary: "Assignment lifecycle impact" },
    sources: [{
      sourceType: "manual",
      sourceLabel: "Assignment lifecycle verifier",
      valueSummary: "Manual assignment source",
    }],
    ...overrides,
  };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM ai_task_audit_events
    WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM ai_task_escalation_events
    WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running AI-TASK-SLA-1 assignment lifecycle verifier.");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const create = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody());
assert("task created 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;
assert("task id returned", Boolean(taskId));

const initialDetail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("initial detail 200", initialDetail.status === 200);
assert("initially unassigned membership", !initialDetail.json?.assignedMembershipId);
assert("initially unassigned team", !initialDetail.json?.assignedTeamChannelId);

const assign = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: initialDetail.json.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "İlk atama doğrulaması",
    source: "manual",
  },
);
assert("assign 200", assign.status === 200, assign.text?.slice(0, 200));

const afterAssign = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("assignee membership persisted", afterAssign.json?.assignedMembershipId === REVIEWER_MEMBERSHIP);
assert("assignment source manual", afterAssign.json?.assignmentSource === "manual");
assert(
  "assigned audit event present",
  (afterAssign.json?.auditTrail ?? []).some((event) => event.eventType === "assigned"),
);

const reassign = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: afterAssign.json.version,
    targetMembershipId: MANAGER_MEMBERSHIP,
    reason: "Yeniden atama doğrulaması",
    source: "reassignment",
  },
);
assert("reassign 200", reassign.status === 200, reassign.text?.slice(0, 200));

const afterReassign = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("reassigned membership persisted", afterReassign.json?.assignedMembershipId === MANAGER_MEMBERSHIP);
assert(
  "reassigned audit event present",
  (afterReassign.json?.auditTrail ?? []).some((event) => event.eventType === "reassigned"),
);

const assignBack = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: afterReassign.json.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "Teslim alma için geri atama",
    source: "reassignment",
  },
);
assert("assign back to reviewer 200", assignBack.status === 200, assignBack.text?.slice(0, 200));

const beforeAck = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
const wrongAck = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/acknowledge?facilityId=${FACILITY}`,
  { expectedVersion: beforeAck.json.version, note: "Yanlış kullanıcı teslim alma denemesi" },
  actorHeaders(MANAGER),
);
assert("non-assignee acknowledge denied", wrongAck.status === 403 || wrongAck.status === 409, `status=${wrongAck.status}`);

const acknowledge = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/acknowledge?facilityId=${FACILITY}`,
  { expectedVersion: beforeAck.json.version, note: "Teslim alındı" },
  actorHeaders(REVIEWER),
);
assert("acknowledge 200", acknowledge.status === 200, acknowledge.text?.slice(0, 200));

const afterAck = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("acknowledgedAtUtc set", Boolean(afterAck.json?.acknowledgedAtUtc));
assert(
  "acknowledged audit event present",
  (afterAck.json?.auditTrail ?? []).some((event) => event.eventType === "acknowledged"),
);
const duplicateAck = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/acknowledge?facilityId=${FACILITY}`,
  { expectedVersion: afterAck.json.version, note: "Tekrar teslim alma" },
  actorHeaders(REVIEWER),
);
assert("duplicate acknowledge rejected", duplicateAck.status === 400 || duplicateAck.status === 409, `status=${duplicateAck.status}`);

removeClosureFixtures();
console.log("AI-TASK-SLA-1 assignment lifecycle verification passed.");

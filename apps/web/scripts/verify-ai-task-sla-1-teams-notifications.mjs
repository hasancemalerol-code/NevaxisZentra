#!/usr/bin/env node
import { spawnSync } from "node:child_process";
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CLOSURE_PREFIX = "AI-TASK-SLA1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";

function fail(message) {
  console.error(`AI-TASK-SLA-1 teams notifications verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
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

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
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
    title: `${CLOSURE_PREFIX} notify ${RUN_SUFFIX}`,
    summary: "Teams notification verification task",
    category: "commercial-intelligence",
    priority: "critical",
    riskLevel: "critical",
    detectedIssue: "Teams notification detected issue",
    reasoning: "Teams notification reasoning",
    recommendation: {
      title: "Review teams notification",
      description: "Verify assignment and escalation notifications",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.87,
      explanation: "Teams notification confidence",
      modelVersion: "sla-1",
    },
    impact: { summary: "Teams notification impact" },
    sources: [{
      sourceType: "manual",
      sourceLabel: "Teams notification verifier",
      valueSummary: "Manual notification source",
    }],
    ...overrides,
  };
}

function removeClosureFixtures(taskIds = []) {
  if (taskIds.length > 0) {
    const idList = taskIds.map((id) => `'${id}'`).join(",");
    db(`
      DELETE FROM teams_notification_audit_events
      WHERE notification_id IN (
        SELECT id FROM teams_notifications WHERE source_entity_id IN (${idList})
      );
      DELETE FROM teams_notifications WHERE source_entity_id IN (${idList});
    `);
  }
  db(`
    DELETE FROM ai_task_audit_events
    WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM ai_task_escalation_events
    WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running AI-TASK-SLA-1 teams notifications verifier.");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures([]);

const reviewerBefore = await liveRequest(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  actorHeaders(REVIEWER),
);
assert("reviewer notifications list 200", reviewerBefore.status === 200);
const beforeCount = reviewerBefore.json?.items?.length ?? 0;

const create = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody());
assert("task created 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;

const detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
const assign = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: detail.json.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "Bildirim doğrulaması ataması",
    source: "manual",
  },
);
assert("assign for notification 200", assign.status === 200, assign.text?.slice(0, 200));

const reviewerAfterAssign = await liveRequest(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  actorHeaders(REVIEWER),
);
assert("reviewer notifications after assign 200", reviewerAfterAssign.status === 200);
const assignmentNotification = (reviewerAfterAssign.json?.items ?? []).find(
  (item) => item.notificationType === "task-assigned" && item.sourceEntityId === taskId,
);
assert("reviewer received task-assigned notification", Boolean(assignmentNotification), JSON.stringify(reviewerAfterAssign.json?.items?.slice(0, 3)));

const ownerAfterAssign = await liveRequest(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  actorHeaders(MANAGER),
);
assert("assigner does not receive own assignment notification", ownerAfterAssign.status === 200);
assert(
  "assigner list excludes current assignment notification",
  !(ownerAfterAssign.json?.items ?? []).some(
    (item) => item.id === assignmentNotification?.id,
  ),
);

const afterAssignDetail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert(
  "teams notification emitted audit event",
  (afterAssignDetail.json?.auditTrail ?? []).some((event) => event.eventType === "teams-notification-emitted"),
);

db(`
  UPDATE ai_tasks
  SET created_at_utc = NOW() - INTERVAL '10 years',
      updated_at_utc = NOW() - INTERVAL '10 years'
  WHERE id = '${taskId}';
`);

const evaluate = await liveRequest(
  "POST",
  `/api/ai-tasks/escalations/evaluate?facilityId=${FACILITY}&maxTasks=50`,
  {},
);
assert("escalation evaluate 200", evaluate.status === 200, evaluate.text?.slice(0, 200));
assert(
  "task escalated in batch",
  (evaluate.json?.escalatedTaskIds ?? []).includes(taskId),
  JSON.stringify(evaluate.json),
);

const reviewerAfterEscalation = await liveRequest(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  actorHeaders(REVIEWER),
);
assert("reviewer notifications after escalation 200", reviewerAfterEscalation.status === 200);
const escalationNotification = (reviewerAfterEscalation.json?.items ?? []).find(
  (item) => item.notificationType === "task-escalated" && item.sourceEntityId === taskId,
);
assert("reviewer received task-escalated notification", Boolean(escalationNotification));

assert(
  "notification count increased",
  (reviewerAfterEscalation.json?.items?.length ?? 0) >= beforeCount + 1,
  `before=${beforeCount} after=${reviewerAfterEscalation.json?.items?.length}`,
);

removeClosureFixtures([taskId]);
console.log("AI-TASK-SLA-1 teams notifications verification passed.");

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

function fail(message) {
  console.error(`AI-TASK-SLA-1 escalation idempotency verification failed: ${message}`);
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
    title: `${CLOSURE_PREFIX} escalation ${RUN_SUFFIX}`,
    summary: "Escalation idempotency verification task",
    category: "commercial-intelligence",
    priority: "critical",
    riskLevel: "critical",
    detectedIssue: "Escalation idempotency detected issue",
    reasoning: "Escalation idempotency reasoning",
    recommendation: {
      title: "Review escalation idempotency",
      description: "Verify evaluate batch idempotency",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.91,
      explanation: "Escalation idempotency confidence",
      modelVersion: "sla-1",
    },
    impact: { summary: "Escalation idempotency impact" },
    sources: [{
      sourceType: "manual",
      sourceLabel: "Escalation idempotency verifier",
      valueSummary: "Manual escalation source",
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

console.log("Running AI-TASK-SLA-1 escalation idempotency verifier.");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const create = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody());
assert("critical task created 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;
assert("task id returned", Boolean(taskId));

db(`
  UPDATE ai_tasks
  SET created_at_utc = NOW() - INTERVAL '10 years',
      updated_at_utc = NOW() - INTERVAL '10 years'
  WHERE id = '${taskId}';
`);

const beforeCount = Number(db(`SELECT COUNT(*) FROM ai_task_escalation_events WHERE task_id = '${taskId}'`));
assert("no escalation events before evaluate", beforeCount === 0);

const firstEvaluate = await liveRequest(
  "POST",
  `/api/ai-tasks/escalations/evaluate?facilityId=${FACILITY}&maxTasks=50`,
  {},
);
assert("first evaluate 200", firstEvaluate.status === 200, firstEvaluate.text?.slice(0, 200));
assert("first evaluate created escalation", (firstEvaluate.json?.escalationsCreated ?? 0) >= 1, JSON.stringify(firstEvaluate.json));
assert(
  "task included in escalated ids",
  (firstEvaluate.json?.escalatedTaskIds ?? []).includes(taskId),
  JSON.stringify(firstEvaluate.json?.escalatedTaskIds),
);

const afterFirstCount = Number(db(`SELECT COUNT(*) FROM ai_task_escalation_events WHERE task_id = '${taskId}'`));
assert("exactly one escalation event after first evaluate", afterFirstCount === 1, `count=${afterFirstCount}`);

const detailAfterFirst = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("escalation level incremented", (detailAfterFirst.json?.escalationLevel ?? 0) >= 1);
assert("escalation state active", detailAfterFirst.json?.escalationState === "active");

const secondEvaluate = await liveRequest(
  "POST",
  `/api/ai-tasks/escalations/evaluate?facilityId=${FACILITY}&maxTasks=50`,
  {},
);
assert("second evaluate 200", secondEvaluate.status === 200, secondEvaluate.text?.slice(0, 200));
assert(
  "second evaluate no duplicate on task",
  !(secondEvaluate.json?.escalatedTaskIds ?? []).includes(taskId),
  JSON.stringify(secondEvaluate.json?.escalatedTaskIds),
);

const afterSecondCount = Number(db(`SELECT COUNT(*) FROM ai_task_escalation_events WHERE task_id = '${taskId}'`));
assert("escalation event count unchanged after replay", afterSecondCount === 1, `count=${afterSecondCount}`);

const manualReplay = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/escalate?facilityId=${FACILITY}`,
  {
    expectedVersion: detailAfterFirst.json.version,
    breachType: "acknowledgment",
    note: "Manuel eskalasyon tekrar denemesi",
  },
);
assert(
  "manual escalate replay idempotent",
  manualReplay.status === 200 && manualReplay.json?.idempotentReplay === true,
  manualReplay.text?.slice(0, 200),
);

const finalCount = Number(db(`SELECT COUNT(*) FROM ai_task_escalation_events WHERE task_id = '${taskId}'`));
assert("final escalation event count still one", finalCount === 1, `count=${finalCount}`);

removeClosureFixtures();
console.log("AI-TASK-SLA-1 escalation idempotency verification passed.");

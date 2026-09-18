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
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";

function fail(message) {
  console.error(`AI-TASK-SLA-1 timeline audit verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
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
    title: `${CLOSURE_PREFIX} timeline ${RUN_SUFFIX}`,
    summary: "Timeline audit verification task",
    category: "maintenance",
    priority: "high",
    riskLevel: "high",
    detectedIssue: "Timeline audit detected issue",
    reasoning: "Timeline audit reasoning",
    recommendation: {
      title: "Review timeline audit",
      description: "Verify structured timeline events",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.86,
      explanation: "Timeline audit confidence",
      modelVersion: "sla-1",
    },
    impact: { summary: "Timeline audit impact" },
    sources: [{
      sourceType: "manual",
      sourceLabel: "Timeline verifier",
      valueSummary: "Manual timeline source",
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

function hasEvent(events, eventType) {
  return (events ?? []).some((event) => event.eventType === eventType);
}

console.log("Running AI-TASK-SLA-1 timeline audit verifier.");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const create = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody());
assert("task created 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;

let detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("created audit event present", hasEvent(detail.json?.auditTrail, "created"));
assert("routed audit event present", hasEvent(detail.json?.auditTrail, "routed"));
assert("assigned audit event present", hasEvent(detail.json?.auditTrail, "assigned"));

const reassign = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: detail.json.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "Zaman çizelgesi yeniden atama",
    source: "reassignment",
  },
);
assert("reassign 200", reassign.status === 200);

detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("reassigned audit event present", hasEvent(detail.json?.auditTrail, "reassigned"));

const priority = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/priority?facilityId=${FACILITY}`,
  {
    expectedVersion: detail.json.version,
    priority: "critical",
    reason: "Zaman çizelgesi öncelik değişimi",
  },
);
assert("priority change 200", priority.status === 200);

detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("priority-changed audit event present", hasEvent(detail.json?.auditTrail, "priority-changed"));

const dueDate = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/due-date?facilityId=${FACILITY}`,
  {
    expectedVersion: detail.json.version,
    dueAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    reason: "Zaman çizelgesi son tarih değişimi",
  },
);
assert("due date change 200", dueDate.status === 200);

detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("due-date-changed audit event present", hasEvent(detail.json?.auditTrail, "due-date-changed"));

const acknowledge = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/acknowledge?facilityId=${FACILITY}`,
  { expectedVersion: detail.json.version, note: "Zaman çizelgesi teslim alma" },
  actorHeaders(REVIEWER),
);
assert("acknowledge 200", acknowledge.status === 200);

detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("acknowledged audit event present", hasEvent(detail.json?.auditTrail, "acknowledged"));

const escalate = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/escalate?facilityId=${FACILITY}`,
  {
    expectedVersion: detail.json.version,
    breachType: "manual",
    note: "Zaman çizelgesi manuel eskalasyon",
  },
);
assert("manual escalate 200", escalate.status === 200, escalate.text?.slice(0, 200));

detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("escalated audit event present", hasEvent(detail.json?.auditTrail, "escalated"));
assert(
  "teams notification emitted audit event present",
  hasEvent(detail.json?.auditTrail, "teams-notification-emitted"),
);

const auditRows = Number(db(`SELECT COUNT(*) FROM ai_task_audit_events WHERE task_id = '${taskId}'`));
assert("append-only audit growth", auditRows >= 8, `count=${auditRows}`);

const ordered = [...(detail.json?.auditTrail ?? [])];
for (let index = 1; index < ordered.length; index += 1) {
  const previous = new Date(ordered[index - 1].occurredAt).getTime();
  const current = new Date(ordered[index].occurredAt).getTime();
  assert(
    `audit timeline ordered at index ${index}`,
    current >= previous,
    `${ordered[index - 1].occurredAt} -> ${ordered[index].occurredAt}`,
  );
}

removeClosureFixtures();
console.log("AI-TASK-SLA-1 timeline audit verification passed.");

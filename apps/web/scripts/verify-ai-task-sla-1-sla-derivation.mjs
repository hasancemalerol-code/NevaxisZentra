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
  console.error(`AI-TASK-SLA-1 SLA derivation verification failed: ${message}`);
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
    title: `${CLOSURE_PREFIX} sla ${RUN_SUFFIX}`,
    summary: "SLA derivation verification task",
    category: "commercial-intelligence",
    priority: "critical",
    riskLevel: "high",
    detectedIssue: "SLA derivation detected issue",
    reasoning: "SLA derivation reasoning",
    recommendation: {
      title: "Review SLA derivation",
      description: "Verify SLA state on read",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.9,
      explanation: "SLA derivation confidence",
      modelVersion: "sla-1",
    },
    impact: { summary: "SLA derivation impact" },
    sources: [{
      sourceType: "manual",
      sourceLabel: "SLA derivation verifier",
      valueSummary: "Manual SLA source",
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

console.log("Running AI-TASK-SLA-1 SLA derivation verifier.");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const create = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody());
assert("critical task created 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;
assert("task id returned", Boolean(taskId));

const freshDetail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("fresh detail 200", freshDetail.status === 200);
assert("sla policy applied", Boolean(freshDetail.json?.slaPolicyId));
assert("fresh sla on-track", freshDetail.json?.sla?.state === "on-track", `state=${freshDetail.json?.sla?.state}`);
assert("acknowledgment due present", Boolean(freshDetail.json?.sla?.acknowledgmentDueAtUtc));
assert("resolution due present", Boolean(freshDetail.json?.sla?.resolutionDueAtUtc));
assert("fresh task not overdue", freshDetail.json?.sla?.isOverdue === false);
assert("fresh task no breach type", !freshDetail.json?.sla?.breachType);

db(`
  UPDATE ai_tasks
  SET created_at_utc = NOW() - INTERVAL '20 minutes',
      updated_at_utc = NOW() - INTERVAL '20 minutes'
  WHERE id = '${taskId}';
`);

const breachedDetail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("backdated detail 200", breachedDetail.status === 200);
assert("derived sla breached", breachedDetail.json?.sla?.state === "breached", `state=${breachedDetail.json?.sla?.state}`);
assert("breach type acknowledgment", breachedDetail.json?.sla?.breachType === "acknowledgment", `breachType=${breachedDetail.json?.sla?.breachType}`);
assert("breached task marked overdue", breachedDetail.json?.sla?.isOverdue === true);

const acknowledge = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`,
  {
    expectedVersion: breachedDetail.json.version,
    targetMembershipId: "bb000001-0003-4000-8000-000000000003",
    reason: "SLA doğrulaması için atama",
    source: "manual",
  },
);
assert("assign for acknowledge 200", acknowledge.status === 200);

const afterAssign = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
const ack = await liveRequest(
  "POST",
  `/api/ai-tasks/${taskId}/acknowledge?facilityId=${FACILITY}`,
  { expectedVersion: afterAssign.json.version, note: "SLA doğrulaması teslim alma" },
);
assert("acknowledge after breach 200", ack.status === 200);

const afterAck = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("acknowledgedAtUtc set", Boolean(afterAck.json?.acknowledgedAtUtc));
assert(
  "post-ack sla no longer acknowledgment breach",
  afterAck.json?.sla?.breachType !== "acknowledgment",
  `breachType=${afterAck.json?.sla?.breachType}`,
);

removeClosureFixtures();
console.log("AI-TASK-SLA-1 SLA derivation verification passed.");

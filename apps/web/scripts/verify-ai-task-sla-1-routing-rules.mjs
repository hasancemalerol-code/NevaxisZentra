#!/usr/bin/env node
import { spawnSync } from "node:child_process";
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CLOSURE_PREFIX = "AI-TASK-SLA1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const MAINTENANCE_TEAM_CHANNEL = "bb000001-03d3-4000-8000-0000000003d3";

function fail(message) {
  console.error(`AI-TASK-SLA-1 routing rules verification failed: ${message}`);
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
    title: `${CLOSURE_PREFIX} routing ${RUN_SUFFIX}`,
    summary: "Maintenance routing verification task",
    category: "maintenance",
    priority: "high",
    riskLevel: "high",
    detectedIssue: "Maintenance routing detected issue",
    reasoning: "Maintenance routing reasoning",
    recommendation: {
      title: "Route to maintenance team",
      description: "Verify maintenance routing rule",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.88,
      explanation: "Maintenance routing confidence",
      modelVersion: "sla-1",
    },
    impact: { summary: "Maintenance routing impact" },
    sources: [{
      sourceType: "manual",
      sourceLabel: "Routing verifier",
      valueSummary: "Maintenance routing source",
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

console.log("Running AI-TASK-SLA-1 routing rules verifier.");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const create = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody());
assert("maintenance task created 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;
assert("task id returned", Boolean(taskId));

const detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, null);
assert("detail 200", detail.status === 200);
assert(
  "routed to maintenance team channel",
  detail.json?.assignedTeamChannelId === MAINTENANCE_TEAM_CHANNEL,
  `assignedTeamChannelId=${detail.json?.assignedTeamChannelId}`,
);
assert("assignment source routing", detail.json?.assignmentSource === "routing");
assert("routing reason present", Boolean(detail.json?.assignmentReason));
assert(
  "routed audit event present",
  (detail.json?.auditTrail ?? []).some((event) => event.eventType === "routed"),
);
assert(
  "assigned audit event present",
  (detail.json?.auditTrail ?? []).some((event) => event.eventType === "assigned"),
);

const nonMatch = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody({
  title: `${CLOSURE_PREFIX} unassigned ${RUN_SUFFIX}`,
  category: "commercial-intelligence",
}));
assert("non-matching category created 201", nonMatch.status === 201);
const nonMatchDetail = await liveRequest("GET", `/api/ai-tasks/${nonMatch.json.id}?facilityId=${FACILITY}`, null);
assert("non-matching category stays unassigned team", !nonMatchDetail.json?.assignedTeamChannelId);
assert(
  "non-matching category has no routed event",
  !(nonMatchDetail.json?.auditTrail ?? []).some((event) => event.eventType === "routed"),
);

removeClosureFixtures();
console.log("AI-TASK-SLA-1 routing rules verification passed.");

#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "AI-TASK-CENTER1-CLOSURE";
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
  console.error(`AI-TASK-CENTER-1 security/idempotency verification failed: ${message}`);
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
    title: "Closure AI task",
    summary: "Security closure task summary",
    category: "commercial-intelligence",
    priority: "medium",
    riskLevel: "medium",
    detectedIssue: "Detected issue for closure verification",
    reasoning: "Reasoning for closure verification",
    recommendation: {
      title: "Review recommendation",
      description: "Review recommendation description",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: {
      score: 0.82,
      explanation: "Closure verification confidence",
      modelVersion: "closure-1",
    },
    impact: {
      summary: "Closure verification impact",
    },
    sources: [{
      sourceType: "manual",
      sourceLabel: "Closure verifier",
      valueSummary: "Manual closure source",
    }],
    ...overrides,
  };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM ai_task_audit_events
    WHERE task_id IN (SELECT id FROM ai_tasks WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM ai_tasks WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running AI-TASK-CENTER-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/AiTasks/AiTaskEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/AiTasks/AiTaskEndpointHelpers.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/AiTasks/AiTaskPermissionKeys.cs");
const lifecycle = readRequired("packages/NevaxisZentra.Persistence/AiTasks/AiTaskLifecycle.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("create access helper", helpers.includes("ResolveCreateAccessAsync"));
assert("review access helper", helpers.includes("ResolveReviewAccessAsync"));
assert("ai-task.read key", permissions.includes("ai-task.read"));
assert("ai-task.create key", permissions.includes("ai-task.create"));
assert("ai-task.review key", permissions.includes("ai-task.review"));
assert("rejection note validation", lifecycle.includes("ValidateTransitionNote"));
forbidText(endpoints, "MapDelete", "ai task endpoints");
forbidText(endpoints, "execute", "ai task endpoints");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const tasksBeforeDeny = Number(db("SELECT COUNT(*) FROM ai_tasks"));
const auditBeforeDeny = Number(db("SELECT COUNT(*) FROM ai_task_audit_events"));

const noAuth = await fetch(`${BASE}/api/ai-tasks?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyCreate = await liveRequest(
  "POST",
  `/api/ai-tasks?facilityId=${FACILITY}`,
  sampleCreateBody(),
  actorHeaders(READONLY),
);
assert("readonly create 403", readonlyCreate.status === 403, `status=${readonlyCreate.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/ai-tasks?facilityId=${UNAUTH_FACILITY}`,
  null,
  actorHeaders(MANAGER),
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/ai-tasks?facilityId=${FACILITY}`,
  null,
  actorHeaders({
    issuer: "crevafactos-dev",
    subject: MANAGER.subject,
    tenant: "unknown-tenant-key",
  }),
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const tasksAfterDeny = Number(db("SELECT COUNT(*) FROM ai_tasks"));
const auditAfterDeny = Number(db("SELECT COUNT(*) FROM ai_task_audit_events"));
assert("denied requests created no tasks", tasksAfterDeny === tasksBeforeDeny, `${tasksBeforeDeny}→${tasksAfterDeny}`);
assert("denied requests created no audit rows", auditAfterDeny === auditBeforeDeny, `${auditBeforeDeny}→${auditAfterDeny}`);

const list = await liveRequest("GET", `/api/ai-tasks?facilityId=${FACILITY}`, null);
assert("authorized list 200", list.status === 200, list.text?.slice(0, 200));
assert("list capabilities", list.json?.capabilities?.canRead === true);

const createDraft = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody({
  title: `Closure draft ${RUN_SUFFIX}`,
}));
assert("authorized draft create 201", createDraft.status === 201, createDraft.text?.slice(0, 200));
const draftId = createDraft.json?.id;
assert("draft create returned id", Boolean(draftId));

const detail = await liveRequest("GET", `/api/ai-tasks/${draftId}?facilityId=${FACILITY}`, null);
assert("authorized detail 200", detail.status === 200, detail.text?.slice(0, 200));
assert("draft status", detail.json?.status === "draft");
assert("audit trail present", Array.isArray(detail.json?.auditTrail) && detail.json.auditTrail.length >= 1);

const idemKey = `${CLOSURE_PREFIX}-source-${RUN_SUFFIX}`;
const sourceBody = {
  ...sampleCreateBody({ title: `Closure source-linked ${RUN_SUFFIX}` }),
  sourceModule: "commercial-intelligence",
  sourceEntityId: `comparison-${RUN_SUFFIX}`,
  idempotencyKey: idemKey,
};
const firstSource = await liveRequest("POST", `/api/ai-tasks/source-linked?facilityId=${FACILITY}`, sourceBody);
assert("source-linked create 201", firstSource.status === 201, firstSource.text?.slice(0, 200));
const replaySource = await liveRequest("POST", `/api/ai-tasks/source-linked?facilityId=${FACILITY}`, sourceBody);
assert("source-linked replay 200", replaySource.status === 200, replaySource.text?.slice(0, 200));
assert("source-linked replay flag", replaySource.json?.idempotentReplay === true);
assert("source-linked same id", replaySource.json?.id === firstSource.json?.id);

const conflictSource = await liveRequest(
  "POST",
  `/api/ai-tasks/source-linked?facilityId=${FACILITY}`,
  { ...sourceBody, title: "Different payload title" },
);
assert("source-linked conflict 409", conflictSource.status === 409, `status=${conflictSource.status}`);

const lookup = await liveRequest(
  "GET",
  `/api/ai-tasks/by-idempotency/${encodeURIComponent(idemKey)}?facilityId=${FACILITY}`,
  null,
);
assert("lookup by idempotency 200", lookup.status === 200, lookup.text?.slice(0, 200));
assert("lookup same id", lookup.json?.id === firstSource.json?.id);

const invalidTransition = await liveRequest(
  "POST",
  `/api/ai-tasks/${draftId}/transitions?facilityId=${FACILITY}`,
  { expectedVersion: detail.json.version, targetStatus: "approved" },
);
assert("invalid transition 409", invalidTransition.status === 409, `status=${invalidTransition.status}`);

const submitReview = await liveRequest(
  "POST",
  `/api/ai-tasks/${draftId}/transitions?facilityId=${FACILITY}`,
  { expectedVersion: detail.json.version, targetStatus: "awaiting-review" },
);
assert("valid transition 200", submitReview.status === 200, submitReview.text?.slice(0, 200));

const reviewerCreateDenied = bootstrap.includes("EnsureAiTaskCreatePermissionAssignmentAsync");
assert("bootstrap create seed present", reviewerCreateDenied);
forbidText(bootstrap, "EnsureAiTaskCreatePermissionAssignmentAsync(\n            dbContext, tenant.Id, reviewerRole.Id", "bootstrap");

const rejectWithoutNote = await liveRequest(
  "POST",
  `/api/ai-tasks/${draftId}/transitions?facilityId=${FACILITY}`,
  { expectedVersion: submitReview.json.version, targetStatus: "rejected" },
);
assert("reject without note 400", rejectWithoutNote.status === 400, `status=${rejectWithoutNote.status}`);

const rejectWithNote = await liveRequest(
  "POST",
  `/api/ai-tasks/${draftId}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: submitReview.json.version,
    targetStatus: "rejected",
    note: "Kapanış doğrulaması için ret gerekçesi",
  },
);
assert("reject with note 200", rejectWithNote.status === 200, rejectWithNote.text?.slice(0, 200));

const reviewerApproveDraft = await liveRequest("POST", `/api/ai-tasks?facilityId=${FACILITY}`, sampleCreateBody({
  title: `Closure reviewer approve ${RUN_SUFFIX}`,
}));
assert("reviewer approve draft create 201", reviewerApproveDraft.status === 201, reviewerApproveDraft.text?.slice(0, 200));
const reviewerDraftDetail = await liveRequest(
  "GET",
  `/api/ai-tasks/${reviewerApproveDraft.json.id}?facilityId=${FACILITY}`,
  null,
);
const reviewerSubmit = await liveRequest(
  "POST",
  `/api/ai-tasks/${reviewerApproveDraft.json.id}/transitions?facilityId=${FACILITY}`,
  { expectedVersion: reviewerDraftDetail.json.version, targetStatus: "awaiting-review" },
);
const reviewerApprove = await liveRequest(
  "POST",
  `/api/ai-tasks/${reviewerApproveDraft.json.id}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: reviewerSubmit.json.version,
    targetStatus: "approved",
  },
  actorHeaders(READONLY),
);
assert("reviewer role can review", reviewerApprove.status === 200, `status=${reviewerApprove.status}`);

const auditCountAfterReject = Number(db(`
  SELECT COUNT(*) FROM ai_task_audit_events WHERE task_id = '${draftId}'
`));
assert("audit events append-only growth", auditCountAfterReject >= 3, `count=${auditCountAfterReject}`);

removeClosureFixtures();
console.log("AI-TASK-CENTER-1 security/idempotency verification passed.");

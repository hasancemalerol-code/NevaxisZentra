#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const FOLLOWUP_EXCEPTION = "bb000001-0077-4000-8000-000000000077";
const INSUFFICIENT_EXCEPTION = "bb000001-0078-4000-8000-000000000078";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const OPERATOR = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const DATA_REVIEW_IDEM_KEY = `production-follow-up-task:${INSUFFICIENT_EXCEPTION}:data-review-draft`;

function fail(message) {
  console.error(`PRODUCTION-AI-TASK-INTEGRATION-1 security/idempotency verification failed: ${message}`);
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

console.log("Running PRODUCTION-AI-TASK-INTEGRATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Production/ProductionFollowUpEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Production/ProductionFollowUpRepository.cs");
const presentation = readRequired("packages/CrevaFactOS.Persistence/Production/ProductionFollowUpPresentationBuilder.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/Production/ProductionFollowUpConstants.cs");

assert("task draft endpoint registered", endpoints.includes('MapPost("/follow-up/{exceptionId:guid}/task-draft"'));
assert("follow-up uses exception manage access", endpoints.includes("ResolveExceptionManageAccessAsync"));
assert("task draft uses ai create access", endpoints.includes("ResolveCreateAccessAsync"));
assert("repository link task method", repository.includes("LinkTaskAsync"));
assert("repository save changes on link", repository.includes("SaveChangesAsync"));
assert("idempotency key builder", presentation.includes("BuildTaskDraftIdempotencyKey"));
assert("data review purpose", constants.includes("TaskPurposeDataReviewDraft"));
assert("source module production", constants.includes('SourceModule = "production"'));
assert("no auto action disclaimer", constants.includes("Otomatik üretim emri, makine, stok veya kalite kararı uygulanmaz."));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/production/follow-up/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace capabilities authoritative", workspace.json?.capabilities?.canRead === true);
assert("workspace exceptions array", Array.isArray(workspace.json?.exceptions));
assert("pilot follow-up exception present", workspace.json?.exceptions?.some((row) => row.id === FOLLOWUP_EXCEPTION));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/production/follow-up/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const wrongFacility = await liveRequest("GET", `/api/production/follow-up/workspace?facilityId=${UNAUTH_FACILITY}`);
assert("wrong facility workspace rejected", wrongFacility.status === 403);

const followUpDetail = await liveRequest("GET", `/api/production/follow-up/${FOLLOWUP_EXCEPTION}?facilityId=${FACILITY}`);
assert("follow-up exception detail succeeds", followUpDetail.status === 200);
assert("follow-up exception not insufficient evidence", followUpDetail.json?.hasInsufficientEvidence === false);
if (followUpDetail.json?.linkedAiTaskId) {
  assert("follow-up exception already linked for replay", followUpDetail.json?.followUpStatus === "task-created");
} else {
  assert("follow-up exception can create task", followUpDetail.json?.canCreateTaskDraft === true);
  assert("production review purpose", followUpDetail.json?.taskDraftPurpose === "human-review-draft");
}

const insufficientDetail = await liveRequest("GET", `/api/production/follow-up/${INSUFFICIENT_EXCEPTION}?facilityId=${FACILITY}`);
assert("insufficient evidence detail succeeds", insufficientDetail.status === 200);
assert("insufficient evidence flagged", insufficientDetail.json?.hasInsufficientEvidence === true);
assert("data review purpose", insufficientDetail.json?.taskDraftPurpose === "data-review-draft");

const idemSeed = await liveRequest(
  "POST",
  `/api/ai-tasks/source-linked?facilityId=${FACILITY}`,
  {
    title: "Üretim veri incelemesi — seed",
    summary: "Idempotency seed",
    category: "live-production",
    priority: "medium",
    riskLevel: "medium",
    detectedIssue: "seed",
    reasoning: "seed",
    recommendation: {
      title: "Veri incelemesi",
      description: "seed",
      actionType: "review",
      requiresApproval: true,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: { score: 0.5, explanation: "seed", modelVersion: "production-1" },
    impact: { summary: "seed" },
    sources: [{ sourceType: "manual", sourceLabel: "seed", valueSummary: "seed" }],
    sourceModule: "production",
    sourceEntityId: INSUFFICIENT_EXCEPTION,
    idempotencyKey: DATA_REVIEW_IDEM_KEY,
    productionOrderId: "bb000001-0076-4000-8000-000000000076",
  },
);
assert("idempotency seed create succeeds", idemSeed.status === 201 || idemSeed.status === 200, idemSeed.text?.slice(0, 200));

const idemConflict = await liveRequest(
  "POST",
  `/api/ai-tasks/source-linked?facilityId=${FACILITY}`,
  {
    title: "Üretim veri incelemesi — conflicting payload",
    summary: "Different payload",
    category: "live-production",
    priority: "medium",
    riskLevel: "medium",
    detectedIssue: "conflict",
    reasoning: "conflict",
    recommendation: {
      title: "Veri incelemesi",
      description: "conflict",
      actionType: "review",
      requiresApproval: true,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: { score: 0.5, explanation: "conflict", modelVersion: "production-1" },
    impact: { summary: "conflict" },
    sources: [{ sourceType: "manual", sourceLabel: "conflict", valueSummary: "conflict" }],
    sourceModule: "production",
    sourceEntityId: INSUFFICIENT_EXCEPTION,
    idempotencyKey: DATA_REVIEW_IDEM_KEY,
    productionOrderId: "bb000001-0076-4000-8000-000000000076",
  },
);
assert("incompatible idempotency-key reuse conflicts", idemConflict.status === 409, `status=${idemConflict.status}`);

const dismissWithoutReason = await liveRequest(
  "POST",
  `/api/production/follow-up/${INSUFFICIENT_EXCEPTION}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json.version, targetFollowUpStatus: "dismissed" },
  actorHeaders(OPERATOR),
);
assert("dismissal without Turkish reason returns 400", dismissWithoutReason.status === 400, dismissWithoutReason.text?.slice(0, 200));

const invalidFollowUp = await liveRequest(
  "POST",
  `/api/production/follow-up/${INSUFFICIENT_EXCEPTION}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json.version, targetFollowUpStatus: "resolved" },
  actorHeaders(OPERATOR),
);
assert("invalid follow-up transition returns 409", invalidFollowUp.status === 409, invalidFollowUp.text?.slice(0, 200));

const readonlyTaskDraft = await liveRequest(
  "POST",
  `/api/production/follow-up/${FOLLOWUP_EXCEPTION}/task-draft?facilityId=${FACILITY}`,
  {},
  actorHeaders(READONLY),
);
assert("readonly cannot create task draft", readonlyTaskDraft.status === 403);

const firstDraft = await liveRequest(
  "POST",
  `/api/production/follow-up/${FOLLOWUP_EXCEPTION}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("manager can create task draft", firstDraft.status === 200, firstDraft.text);
assert("task id returned", Boolean(firstDraft.json?.taskId));
if (!followUpDetail.json?.linkedAiTaskId) {
  assert("exception linked after create", Boolean(firstDraft.json?.exception?.linkedAiTaskId));
  assert("follow-up status task-created", firstDraft.json?.exception?.followUpStatus === "task-created");
}

const secondDraft = await liveRequest(
  "POST",
  `/api/production/follow-up/${FOLLOWUP_EXCEPTION}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("idempotent replay succeeds", secondDraft.status === 200);
assert("same task id on replay", secondDraft.json?.taskId === firstDraft.json?.taskId);
assert("idempotent replay flag", secondDraft.json?.idempotentReplay === true);

const taskDetail = await liveRequest("GET", `/api/ai-tasks/${firstDraft.json.taskId}?facilityId=${FACILITY}`);
assert("linked ai task readable", taskDetail.status === 200);
assert("task source module production", taskDetail.json?.sourceModule === "production");
assert("task source entity id", taskDetail.json?.sourceEntityId === FOLLOWUP_EXCEPTION);

const exceptionAfterLink = await liveRequest("GET", `/api/production/follow-up/${FOLLOWUP_EXCEPTION}?facilityId=${FACILITY}`);
assert("follow-up audit events recorded", Array.isArray(exceptionAfterLink.json?.auditEvents) && exceptionAfterLink.json.auditEvents.length >= 1);
assert("audit event task-linked present", exceptionAfterLink.json.auditEvents.some((e) => e.eventType === "task-linked"));

console.log("PRODUCTION-AI-TASK-INTEGRATION-1 security/idempotency verification passed.");

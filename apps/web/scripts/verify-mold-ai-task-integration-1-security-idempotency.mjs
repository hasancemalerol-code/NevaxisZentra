#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const MAINTENANCE_MOLD = "bb000001-0070-4000-8000-000000000070";
const INSUFFICIENT_MOLD = "bb000001-0071-4000-8000-000000000071";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const OPERATOR = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const DATA_REVIEW_IDEM_KEY = `mold-follow-up-task:${INSUFFICIENT_MOLD}:data-review-draft`;

function fail(message) {
  console.error(`MOLD-AI-TASK-INTEGRATION-1 security/idempotency verification failed: ${message}`);
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

console.log("Running MOLD-AI-TASK-INTEGRATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Mold/MoldFollowUpEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Mold/MoldFollowUpRepository.cs");
const presentation = readRequired("packages/CrevaFactOS.Persistence/Mold/MoldFollowUpPresentationBuilder.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/Mold/MoldFollowUpConstants.cs");

assert("task draft endpoint registered", endpoints.includes('MapPost("/follow-up/{moldId:guid}/task-draft"'));
assert("follow-up uses mold write access", endpoints.includes("ResolveWriteAccessAsync"));
assert("task draft uses ai create access", endpoints.includes("ResolveCreateAccessAsync"));
assert("repository link task method", repository.includes("LinkTaskAsync"));
assert("repository save changes on link", repository.includes("SaveChangesAsync"));
assert("idempotency key builder", presentation.includes("BuildTaskDraftIdempotencyKey"));
assert("data review purpose", constants.includes("TaskPurposeDataReviewDraft"));
assert("source module mold", constants.includes('SourceModule = "mold"'));
assert("no auto action disclaimer", constants.includes("Otomatik bakım emri, kalıp yaşam döngüsü veya üretim kararı uygulanmaz"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/molds/follow-up/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace capabilities authoritative", workspace.json?.capabilities?.canRead === true);
assert("workspace molds array", Array.isArray(workspace.json?.molds));
assert("pilot maintenance mold present", workspace.json?.molds?.some((row) => row.id === MAINTENANCE_MOLD));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/molds/follow-up/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const wrongFacility = await liveRequest("GET", `/api/molds/follow-up/workspace?facilityId=${UNAUTH_FACILITY}`);
assert("wrong facility workspace rejected", wrongFacility.status === 403);

const maintenanceDetail = await liveRequest("GET", `/api/molds/follow-up/${MAINTENANCE_MOLD}?facilityId=${FACILITY}`);
assert("maintenance mold detail succeeds", maintenanceDetail.status === 200);
assert("maintenance mold not insufficient evidence", maintenanceDetail.json?.hasInsufficientEvidence === false);
if (maintenanceDetail.json?.linkedAiTaskId) {
  assert("maintenance mold already linked for replay", maintenanceDetail.json?.followUpStatus === "task-created");
} else {
  assert("maintenance mold can create task", maintenanceDetail.json?.canCreateTaskDraft === true);
  assert("technical review purpose", maintenanceDetail.json?.taskDraftPurpose === "human-review-draft");
}

const insufficientDetail = await liveRequest("GET", `/api/molds/follow-up/${INSUFFICIENT_MOLD}?facilityId=${FACILITY}`);
assert("insufficient evidence detail succeeds", insufficientDetail.status === 200);
assert("insufficient evidence flagged", insufficientDetail.json?.hasInsufficientEvidence === true);
assert("data review purpose", insufficientDetail.json?.taskDraftPurpose === "data-review-draft");

const idemSeed = await liveRequest(
  "POST",
  `/api/ai-tasks/source-linked?facilityId=${FACILITY}`,
  {
    title: "Kalıp veri incelemesi — seed",
    summary: "Idempotency seed",
    category: "maintenance",
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
    confidence: { score: 0.5, explanation: "seed", modelVersion: "mold-1" },
    impact: { summary: "seed" },
    sources: [{ sourceType: "manual", sourceLabel: "seed", valueSummary: "seed" }],
    sourceModule: "mold",
    sourceEntityId: INSUFFICIENT_MOLD,
    idempotencyKey: DATA_REVIEW_IDEM_KEY,
    moldId: INSUFFICIENT_MOLD,
  },
);
assert("idempotency seed create succeeds", idemSeed.status === 201 || idemSeed.status === 200, idemSeed.text?.slice(0, 200));

const idemConflict = await liveRequest(
  "POST",
  `/api/ai-tasks/source-linked?facilityId=${FACILITY}`,
  {
    title: "Kalıp veri incelemesi — conflicting payload",
    summary: "Different payload",
    category: "maintenance",
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
    confidence: { score: 0.5, explanation: "conflict", modelVersion: "mold-1" },
    impact: { summary: "conflict" },
    sources: [{ sourceType: "manual", sourceLabel: "conflict", valueSummary: "conflict" }],
    sourceModule: "mold",
    sourceEntityId: INSUFFICIENT_MOLD,
    idempotencyKey: DATA_REVIEW_IDEM_KEY,
    moldId: INSUFFICIENT_MOLD,
  },
);
assert("incompatible idempotency-key reuse conflicts", idemConflict.status === 409, `status=${idemConflict.status}`);

const dismissWithoutReason = await liveRequest(
  "POST",
  `/api/molds/follow-up/${INSUFFICIENT_MOLD}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json.version, targetFollowUpStatus: "dismissed" },
  actorHeaders(OPERATOR),
);
assert("dismissal without Turkish reason returns 400", dismissWithoutReason.status === 400, dismissWithoutReason.text?.slice(0, 200));

const invalidFollowUp = await liveRequest(
  "POST",
  `/api/molds/follow-up/${INSUFFICIENT_MOLD}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json.version, targetFollowUpStatus: "resolved" },
  actorHeaders(OPERATOR),
);
assert("invalid follow-up transition returns 409", invalidFollowUp.status === 409, invalidFollowUp.text?.slice(0, 200));

const taskDraftTarget = MAINTENANCE_MOLD;

const readonlyTaskDraft = await liveRequest(
  "POST",
  `/api/molds/follow-up/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
  actorHeaders(READONLY),
);
assert("readonly cannot create task draft", readonlyTaskDraft.status === 403);

const firstDraft = await liveRequest(
  "POST",
  `/api/molds/follow-up/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("manager can create task draft", firstDraft.status === 200, firstDraft.text);
assert("task id returned", Boolean(firstDraft.json?.taskId));
if (!maintenanceDetail.json?.linkedAiTaskId) {
  assert("mold linked after create", Boolean(firstDraft.json?.mold?.linkedAiTaskId));
  assert("follow-up status task-created", firstDraft.json?.mold?.followUpStatus === "task-created");
}

const secondDraft = await liveRequest(
  "POST",
  `/api/molds/follow-up/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("idempotent replay succeeds", secondDraft.status === 200);
assert("same task id on replay", secondDraft.json?.taskId === firstDraft.json?.taskId);
assert("idempotent replay flag", secondDraft.json?.idempotentReplay === true);

const taskDetail = await liveRequest("GET", `/api/ai-tasks/${firstDraft.json.taskId}?facilityId=${FACILITY}`);
assert("linked ai task readable", taskDetail.status === 200);
assert("task source module mold", taskDetail.json?.sourceModule === "mold");
assert("task source entity id", taskDetail.json?.sourceEntityId === MAINTENANCE_MOLD);

const maintenanceAfterLink = await liveRequest("GET", `/api/molds/follow-up/${MAINTENANCE_MOLD}?facilityId=${FACILITY}`);
assert("follow-up audit events recorded", Array.isArray(maintenanceAfterLink.json?.auditEvents) && maintenanceAfterLink.json.auditEvents.length >= 1);
assert("audit event task-linked present", maintenanceAfterLink.json.auditEvents.some((e) => e.eventType === "task-linked"));

console.log("MOLD-AI-TASK-INTEGRATION-1 security/idempotency verification passed.");

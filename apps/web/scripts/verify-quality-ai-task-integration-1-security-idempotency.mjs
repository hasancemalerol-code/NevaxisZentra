#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CONFIRMED_NCR = "bb000001-0063-4000-8000-000000000063";
const INSUFFICIENT_NCR = "bb000001-0064-4000-8000-000000000064";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`QUALITY-AI-TASK-INTEGRATION-1 security/idempotency verification failed: ${message}`);
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

console.log("Running QUALITY-AI-TASK-INTEGRATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Quality/QualityNcrFollowUpEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityNonconformanceRepository.cs");
const presentation = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityFollowUpPresentationBuilder.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityFollowUpConstants.cs");

assert("task draft endpoint registered", endpoints.includes('MapPost("/ncr-follow-up/{ncrId:guid}/task-draft"'));
assert("follow-up requires ncr manage permission", endpoints.includes("QualityPermissionKeys.NcrManage"));
assert("task draft uses ai create access", endpoints.includes("ResolveCreateAccessAsync"));
assert("repository link task method", repository.includes("LinkTaskAsync"));
assert("idempotency key builder", presentation.includes("BuildTaskDraftIdempotencyKey"));
assert("data review purpose", constants.includes("TaskPurposeDataReviewDraft"));
assert("source module quality", constants.includes('SourceModule = "quality"'));
assert("no auto action disclaimer", constants.includes("Otomatik üretim, stok veya kalite kararı uygulanmaz"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/quality/ncr-follow-up/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace capabilities authoritative", workspace.json?.capabilities?.canRead === true);
assert("workspace findings array", Array.isArray(workspace.json?.findings));
assert("pilot confirmed ncr present", workspace.json?.findings?.some((row) => row.id === CONFIRMED_NCR));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/quality/ncr-follow-up/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const wrongFacility = await liveRequest("GET", `/api/quality/ncr-follow-up/workspace?facilityId=${UNAUTH_FACILITY}`);
assert("wrong facility workspace rejected", wrongFacility.status === 403);

const confirmedDetail = await liveRequest("GET", `/api/quality/ncr-follow-up/${CONFIRMED_NCR}?facilityId=${FACILITY}`);
assert("confirmed ncr detail succeeds", confirmedDetail.status === 200);
assert("confirmed ncr not insufficient evidence", confirmedDetail.json?.hasInsufficientEvidence === false);
if (confirmedDetail.json?.linkedAiTaskId) {
  assert("confirmed ncr already linked for replay", confirmedDetail.json?.followUpStatus === "task-created");
} else {
  assert("confirmed ncr can create task", confirmedDetail.json?.canCreateTaskDraft === true);
}

const insufficientDetail = await liveRequest("GET", `/api/quality/ncr-follow-up/${INSUFFICIENT_NCR}?facilityId=${FACILITY}`);
assert("insufficient evidence detail succeeds", insufficientDetail.status === 200);
assert("insufficient evidence flagged", insufficientDetail.json?.hasInsufficientEvidence === true);
assert("data review purpose", insufficientDetail.json?.taskDraftPurpose === "data-review-draft");

const taskDraftTarget = insufficientDetail.json?.linkedAiTaskId ? CONFIRMED_NCR : INSUFFICIENT_NCR;

const readonlyTaskDraft = await liveRequest(
  "POST",
  `/api/quality/ncr-follow-up/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
  actorHeaders(READONLY),
);
assert("readonly cannot create task draft", readonlyTaskDraft.status === 403);

const firstDraft = await liveRequest(
  "POST",
  `/api/quality/ncr-follow-up/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("manager can create task draft", firstDraft.status === 200, firstDraft.text);
assert("task id returned", Boolean(firstDraft.json?.taskId));
if (!insufficientDetail.json?.linkedAiTaskId) {
  assert("finding linked after create", Boolean(firstDraft.json?.finding?.linkedAiTaskId));
  assert("follow-up status task-created", firstDraft.json?.finding?.followUpStatus === "task-created");
}

const secondDraft = await liveRequest(
  "POST",
  `/api/quality/ncr-follow-up/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("idempotent replay succeeds", secondDraft.status === 200);
assert("same task id on replay", secondDraft.json?.taskId === firstDraft.json?.taskId);
assert("idempotent replay flag", secondDraft.json?.idempotentReplay === true);

const replaySourceId = taskDraftTarget;
const taskDetail = await liveRequest("GET", `/api/ai-tasks/${firstDraft.json.taskId}?facilityId=${FACILITY}`);
assert("linked ai task readable", taskDetail.status === 200);
assert("task source module quality", taskDetail.json?.sourceModule === "quality");
assert("task source entity id", taskDetail.json?.sourceEntityId === replaySourceId);

console.log("QUALITY-AI-TASK-INTEGRATION-1 security/idempotency verification passed.");

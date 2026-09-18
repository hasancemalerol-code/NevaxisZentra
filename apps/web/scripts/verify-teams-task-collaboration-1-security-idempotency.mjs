#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const URETIM_CHANNEL = "bb000001-03d1-4000-8000-0000000003d1";
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const RUN_SUFFIX = Date.now();

function fail(message) {
  console.error(`TEAMS-TASK-COLLABORATION-1 security/idempotency verification failed: ${message}`);
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
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(OWNER)) {
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

console.log("Running TEAMS-TASK-COLLABORATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Teams/TeamsEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Teams/TeamsEndpointHelpers.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/Teams/TeamsRepository.cs");
const presentation = readRequired("packages/NevaxisZentra.Persistence/Teams/TeamsMessageTaskPresentationBuilder.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/Teams/TeamsConstants.cs");

assert("task draft endpoint", endpoints.includes("/task-draft"));
assert("message source context endpoint", endpoints.includes("/source-context"));
assert("ai-task create resolver", helpers.includes("ResolveAiTaskCreateAccessAsync"));
assert("teams source module contract", presentation.includes('SourceModule: TeamsConstants.TeamsSourceModule'));
assert("human review idempotency key", constants.includes("teams-message-task:"));
assert("link persisted after task success", repository.includes("TeamsMessageTaskLinks.Add"));
assert("failure returns before link insert path", repository.includes("if (!taskResult.Succeeded)"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/teams/messages/${URETIM_CHANNEL}/source-context?facilityId=${FACILITY}`, null, {});
assert("unauthenticated source-context 401", unauth.status === 401);

const unauthDraft = await liveRequest(
  "POST",
  `/api/teams/messages/00000000-0000-4000-8000-000000000001/task-draft?facilityId=${FACILITY}`,
  { title: "x", category: "maintenance", priority: "medium" },
  {},
);
assert("unauthenticated task-draft 401", unauthDraft.status === 401);

const channel = await liveRequest("GET", `/api/teams/channels/${URETIM_CHANNEL}?facilityId=${FACILITY}`);
assert("authorized channel detail", channel.status === 200);

const freshMessage = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  {
    body: `Teams task collaboration security ${RUN_SUFFIX}`,
    idempotencyKey: `teams-task-collab-${RUN_SUFFIX}`,
    mentionedMembershipIds: [],
  },
);
assert("fresh message posted", freshMessage.status === 200 || freshMessage.status === 201);
const messageId = freshMessage.json?.id;
assert("persisted message available", Boolean(messageId), freshMessage.text?.slice(0, 200));

const negativeContext = await liveRequest(
  "GET",
  `/api/teams/messages/${messageId}/source-context?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("non-member source-context denied", negativeContext.status === 403 || negativeContext.status === 404);

const reviewerDraftDenied = await liveRequest(
  "POST",
  `/api/teams/messages/${messageId}/task-draft?facilityId=${FACILITY}`,
  {
    title: `Reviewer denied ${RUN_SUFFIX}`,
    category: "maintenance",
    priority: "medium",
  },
  actorHeaders(REVIEWER),
);
assert("member without ai-task.create denied", reviewerDraftDenied.status === 403, reviewerDraftDenied.text?.slice(0, 200));

const blankTitle = await liveRequest(
  "POST",
  `/api/teams/messages/${messageId}/task-draft?facilityId=${FACILITY}`,
  { title: "   ", category: "maintenance", priority: "medium" },
);
assert("blank title 400", blankTitle.status === 400, blankTitle.text?.slice(0, 200));
assert("blank title turkish", blankTitle.json?.title?.includes("Görev başlığı") || blankTitle.text?.includes("Görev başlığı"));

const draftBody = {
  title: `Teams mesaj görev taslağı ${RUN_SUFFIX}`,
  category: "maintenance",
  priority: "high",
  humanNote: "İnsan incelemesi notu",
};

const firstDraft = await liveRequest(
  "POST",
  `/api/teams/messages/${messageId}/task-draft?facilityId=${FACILITY}`,
  draftBody,
);
assert("eligible member creates draft", firstDraft.status === 201 || firstDraft.status === 200, firstDraft.text?.slice(0, 300));
const taskId = firstDraft.json?.taskId;
assert("task id returned", Boolean(taskId));

const detail = await liveRequest("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("teams source on task detail", detail.status === 200
  && detail.json?.sourceModule === "teams"
  && detail.json?.sourceEntityId === messageId);

const replayDraft = await liveRequest(
  "POST",
  `/api/teams/messages/${messageId}/task-draft?facilityId=${FACILITY}`,
  draftBody,
);
assert("replay same task id", replayDraft.json?.taskId === taskId);
assert("replay flag", replayDraft.json?.idempotentReplay === true);

const conflictMessage = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  {
    body: `Teams task collaboration conflict ${RUN_SUFFIX}`,
    idempotencyKey: `teams-task-collab-conflict-${RUN_SUFFIX}`,
    mentionedMembershipIds: [],
  },
);
assert("conflict message posted", conflictMessage.status === 200 || conflictMessage.status === 201);
const conflictMessageId = conflictMessage.json?.id;
assert("conflict message id", Boolean(conflictMessageId));

const conflictKey = `teams-message-task:${conflictMessageId}:human-review-draft`;
const seededTask = await liveRequest(
  "POST",
  `/api/ai-tasks/source-linked?facilityId=${FACILITY}`,
  {
    title: `Seeded conflict task ${RUN_SUFFIX}`,
    summary: "Conflict seed",
    category: "maintenance",
    priority: "medium",
    riskLevel: "medium",
    detectedIssue: "seed",
    reasoning: "seed",
    recommendation: {
      title: "review",
      description: "seed",
      actionType: "review",
      requiresApproval: true,
      reversible: true,
      targetSystem: "crevafactos",
    },
    confidence: { score: 0.5, explanation: "seed", modelVersion: null },
    impact: {
      financialAmount: null,
      currency: null,
      productionQuantity: null,
      productionUnit: null,
      estimatedDelayMinutes: null,
      estimatedDowntimeMinutes: null,
      estimatedSavingsAmount: null,
      summary: "seed",
    },
    sources: [{
      sourceType: "teams",
      sourceId: conflictMessageId,
      sourceLabel: "Teams mesajı",
      observedAt: null,
      valueSummary: "conflict seed",
    }],
    sourceModule: "teams",
    sourceEntityId: conflictMessageId,
    idempotencyKey: conflictKey,
  },
);
assert("seed source-linked task", seededTask.status === 201 || seededTask.status === 200, seededTask.text?.slice(0, 200));

const conflictDraft = await liveRequest(
  "POST",
  `/api/teams/messages/${conflictMessageId}/task-draft?facilityId=${FACILITY}`,
  {
    title: `Different title ${RUN_SUFFIX}`,
    category: "maintenance",
    priority: "high",
  },
);
assert("incompatible idempotency 409", conflictDraft.status === 409, `status=${conflictDraft.status}`);

const sourceContext = await liveRequest(
  "GET",
  `/api/teams/messages/${messageId}/source-context?facilityId=${FACILITY}`,
);
assert("source context for member", sourceContext.status === 200 && sourceContext.json?.canOpen === true);
assert("deep link shape", sourceContext.json?.teamsDeepLinkPath?.includes(`channel=${URETIM_CHANNEL}`));
assert("deep link message param", sourceContext.json?.teamsDeepLinkPath?.includes(`message=${messageId}`));

const refreshedChannel = await liveRequest("GET", `/api/teams/channels/${URETIM_CHANNEL}?facilityId=${FACILITY}`);
const linkedMessage = refreshedChannel.json?.messages?.find((item) => item.id === messageId);
assert("linked task on message", Boolean(linkedMessage?.linkedTask?.taskId === taskId));

console.log("TEAMS-TASK-COLLABORATION-1 security/idempotency verification passed.");

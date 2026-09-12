#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const URETIM_CHANNEL = "bb000001-03d1-4000-8000-0000000003d1";
const TASK_CONTEXT_CHANNEL = "bb000001-03d7-4000-8000-0000000003d7";
const PILOT_TASK = "bb000001-03d0-4000-8000-0000000003d0";
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`TEAMS-FOUNDATION-1 security/idempotency verification failed: ${message}`);
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
    "X-NevaxisFactOS-Tenant": actor.tenant,
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

console.log("Running TEAMS-FOUNDATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Teams/TeamsEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/Teams/TeamsRepository.cs");
const fingerprint = readRequired("packages/NevaxisFactOS.Persistence/Teams/TeamsIdempotencyFingerprint.cs");

assert("channels list endpoint", endpoints.includes('MapGet("/", ListChannelsAsync)'));
assert("post message endpoint", endpoints.includes("/messages"));
assert("mark read endpoint", endpoints.includes("/read"));
assert("task context endpoint", endpoints.includes("/by-task/{taskId:guid}"));
assert("repository membership gate", repository.includes("LoadMembershipAsync"));
assert("non-member mention rejection", repository.includes("Yalnızca kanal üyeleri etiketlenebilir"));
assert("idempotency fingerprint", fingerprint.includes("ComputeMessageFingerprint"));
assert("append-only audit events", repository.includes("TeamsMessageAuditEvents.Add"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/teams/channels?facilityId=${FACILITY}`, null, {});
assert("unauthenticated 401", unauth.status === 401);

const negativeList = await liveRequest("GET", `/api/teams/channels?facilityId=${FACILITY}`, null, actorHeaders(NEGATIVE));
assert("non-member/no-permission list denied or empty", negativeList.status === 403 || (negativeList.status === 200 && (negativeList.json?.items?.length ?? 0) === 0));

const channels = await liveRequest("GET", `/api/teams/channels?facilityId=${FACILITY}`);
assert("authorized channel list", channels.status === 200);
assert("pilot uretim channel present", channels.json?.items?.some((item) => item.id === URETIM_CHANNEL));

const guessedChannel = await liveRequest(
  "GET",
  `/api/teams/channels/00000000-0000-4000-8000-000000009999?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("guessed channel blocked", guessedChannel.status === 403 || guessedChannel.status === 404);

const idempotencyKey = `teams-verify-${Date.now()}`;
const body = {
  body: "Teams güvenlik doğrulama mesajı",
  idempotencyKey,
  mentionedMembershipIds: [],
};
const firstPost = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  body,
);
assert("member post succeeds", firstPost.status === 200 || firstPost.status === 201, `status=${firstPost.status}`);
const firstId = firstPost.json?.id;
assert("posted message id returned", Boolean(firstId));

const replayPost = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  body,
);
assert("idempotent replay same message", replayPost.status === 200 && replayPost.json?.id === firstId);

const conflictPost = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  { ...body, body: "Farklı içerik" },
);
assert("idempotency conflict 409", conflictPost.status === 409);

const readonlyPost = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  { body: "Yetkisiz", idempotencyKey: `readonly-${Date.now()}`, mentionedMembershipIds: [] },
  actorHeaders(NEGATIVE),
);
assert("non-member post forbidden", readonlyPost.status === 403);

const taskContext = await liveRequest(
  "GET",
  `/api/teams/channels/by-task/${PILOT_TASK}?facilityId=${FACILITY}`,
);
assert("task context channel resolves", taskContext.status === 200 && taskContext.json?.channelId === TASK_CONTEXT_CHANNEL);

const negativeTaskContext = await liveRequest(
  "GET",
  `/api/teams/channels/by-task/${PILOT_TASK}?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("task context hidden from non-member", negativeTaskContext.status === 404 || negativeTaskContext.status === 403);

const markRead = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/read?facilityId=${FACILITY}`,
  { lastReadMessageId: firstId },
);
assert("mark read succeeds", markRead.status === 200);

console.log("TEAMS-FOUNDATION-1 security/idempotency verification passed.");

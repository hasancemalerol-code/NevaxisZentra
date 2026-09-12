#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";
const RUN_SUFFIX = Date.now();

function fail(message) {
  console.error(`TEAMS-NOTIFICATIONS-1 security verification failed: ${message}`);
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

console.log("Running TEAMS-NOTIFICATIONS-1 security verifier.");

const endpoints = readRequired("apps/api/Teams/TeamsNotificationEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/Teams/TeamsNotificationRepository.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260830153612_AddTeamsNotifications.cs");

assert("notifications route family", endpoints.includes("/api/teams/notifications"));
assert("mark read route", endpoints.includes("/read"));
assert("recipient fingerprint uniqueness", repository.includes("SourceEventFingerprint == fingerprint"));
assert("no self notification", repository.includes("recipientMembershipId == context.MembershipId"));
assert("source access validation", repository.includes("ResolveSourceAccessAsync"));
assert("append-only audit migration", migration.includes("teams_notification_audit_events"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/teams/notifications?facilityId=${FACILITY}`, null, {});
assert("unauthenticated list 401", unauth.status === 401);

const create = await liveRequest(
  "POST",
  `/api/teams/direct-messages?facilityId=${FACILITY}`,
  {
    recipientTenantMembershipId: REVIEWER_MEMBERSHIP,
    idempotencyKey: `teams-notify-dm-${RUN_SUFFIX}`,
  },
);
assert("conversation created for notification seed", create.status === 200 || create.status === 201, create.text?.slice(0, 200));
const conversationId = create.json?.id;
assert("conversation id", Boolean(conversationId));

const message = await liveRequest(
  "POST",
  `/api/teams/direct-messages/${conversationId}/messages?facilityId=${FACILITY}`,
  {
    body: `Bildirim doğrulama ${RUN_SUFFIX}`,
    idempotencyKey: `teams-notify-message-${RUN_SUFFIX}`,
  },
);
assert("direct message posted", message.status === 200, message.text?.slice(0, 200));

const reviewerList = await liveRequest(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  actorHeaders(REVIEWER),
);
assert("reviewer can list notifications", reviewerList.status === 200);
const reviewerItems = reviewerList.json?.items ?? [];
assert(
  "reviewer received direct-message notification",
  reviewerItems.some((item) => item.notificationType === "direct-message"),
);

const ownerList = await liveRequest(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  actorHeaders(OWNER),
);
assert("sender does not receive own DM notification", ownerList.status === 200);
assert(
  "sender list excludes current DM notification body",
  !(ownerList.json?.items ?? []).some((item) => (item.sourcePreview ?? "").includes(`Bildirim doğrulama ${RUN_SUFFIX}`)),
);

const notificationId = reviewerItems.find((item) => item.notificationType === "direct-message")?.id;
assert("notification id resolved", Boolean(notificationId));

const markRead = await liveRequest(
  "POST",
  `/api/teams/notifications/${notificationId}/read?facilityId=${FACILITY}`,
  {},
  actorHeaders(REVIEWER),
);
assert("recipient can mark read", markRead.status === 200, markRead.text?.slice(0, 200));

const markReplay = await liveRequest(
  "POST",
  `/api/teams/notifications/${notificationId}/read?facilityId=${FACILITY}`,
  {},
  actorHeaders(REVIEWER),
);
assert("mark read idempotent", markReplay.status === 200 && markReplay.json?.idempotentReplay === true);

const negativeList = await liveRequest(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("non-member list blocked or empty", negativeList.status === 403 || negativeList.status === 200);
if (negativeList.status === 200) {
  assert(
    "non-member cannot see reviewer notification",
    !(negativeList.json?.items ?? []).some((item) => item.id === notificationId),
  );
}

const negativeMark = await liveRequest(
  "POST",
  `/api/teams/notifications/${notificationId}/read?facilityId=${FACILITY}`,
  {},
  actorHeaders(NEGATIVE),
);
assert("non-recipient cannot mark read", negativeMark.status === 404 || negativeMark.status === 403);

const mutate = await liveRequest("DELETE", `/api/teams/notifications/${notificationId}?facilityId=${FACILITY}`, null, actorHeaders(REVIEWER));
assert("delete rejected", mutate.status === 404 || mutate.status === 405 || mutate.status === 401);

console.log("TEAMS-NOTIFICATIONS-1 security verification passed.");

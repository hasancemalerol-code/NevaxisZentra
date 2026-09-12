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
  console.error(`TEAMS-DIRECT-MESSAGES-1 security verification failed: ${message}`);
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

console.log("Running TEAMS-DIRECT-MESSAGES-1 security verifier.");

const endpoints = readRequired("apps/api/Teams/TeamsDirectMessageEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/Teams/TeamsDirectMessageRepository.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260830135508_AddTeamsDirectMessages.cs");

assert("direct route family", endpoints.includes("/api/teams/direct-messages"));
assert("recipient directory route", endpoints.includes("/recipients"));
assert("participant-only read", repository.includes("IsParticipantAsync"));
assert("facility membership validation", repository.includes("IsEligibleFacilityMemberAsync"));
assert("canonical pair", repository.includes("ParticipantLowMembershipId"));
assert("append-only messages", migration.includes("GRANT SELECT, INSERT ON TABLE teams_direct_messages"));
assert("no message delete grant", !migration.includes("DELETE ON TABLE teams_direct_messages"));
assert("direct permissions seeded", migration.includes("teams.direct.read"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/teams/direct-messages?facilityId=${FACILITY}`, null, {});
assert("unauthenticated list 401", unauth.status === 401);

const create = await liveRequest(
  "POST",
  `/api/teams/direct-messages?facilityId=${FACILITY}`,
  {
    recipientTenantMembershipId: REVIEWER_MEMBERSHIP,
    idempotencyKey: `teams-direct-create-${RUN_SUFFIX}`,
  },
);
assert("conversation created", create.status === 200 || create.status === 201, create.text?.slice(0, 200));
const conversationId = create.json?.id;
assert("conversation id", Boolean(conversationId));

const message = await liveRequest(
  "POST",
  `/api/teams/direct-messages/${conversationId}/messages?facilityId=${FACILITY}`,
  {
    body: `Özel mesaj güvenlik ${RUN_SUFFIX}`,
    idempotencyKey: `teams-direct-message-${RUN_SUFFIX}`,
  },
);
assert("message posted", message.status === 200, message.text?.slice(0, 200));

const reviewerList = await liveRequest(
  "GET",
  `/api/teams/direct-messages?facilityId=${FACILITY}`,
  null,
  actorHeaders(REVIEWER),
);
assert("reviewer sees conversation", reviewerList.status === 200);
assert(
  "reviewer conversation listed",
  (reviewerList.json?.items ?? []).some((item) => item.id === conversationId),
);

const negativeDetail = await liveRequest(
  "GET",
  `/api/teams/direct-messages/${conversationId}?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("non-participant cannot read conversation", negativeDetail.status === 404 || negativeDetail.status === 403);

const selfRecipient = await liveRequest(
  "POST",
  `/api/teams/direct-messages?facilityId=${FACILITY}`,
  {
    recipientTenantMembershipId: "bb000001-0003-4000-8000-000000000003",
    idempotencyKey: `teams-direct-self-${RUN_SUFFIX}`,
  },
);
assert("self-recipient rejected", selfRecipient.status === 400 || selfRecipient.status === 403 || selfRecipient.status === 404);

const mutate = await liveRequest("DELETE", `/api/teams/direct-messages/${conversationId}?facilityId=${FACILITY}`);
assert("delete rejected", mutate.status === 404 || mutate.status === 405 || mutate.status === 401);

console.log("TEAMS-DIRECT-MESSAGES-1 security verification passed.");

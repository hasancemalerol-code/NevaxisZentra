#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const URETIM_CHANNEL = "bb000001-03d1-4000-8000-0000000003d1";
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const RUN_SUFFIX = Date.now();

function fail(message) {
  console.error(`TEAMS-APPROVALS-1 security/idempotency verification failed: ${message}`);
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

console.log("Running TEAMS-APPROVALS-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Teams/TeamsApprovalEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Teams/TeamsEndpointHelpers.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Teams/TeamsApprovalRepository.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/Teams/TeamsConstants.cs");

assert("approvals route family", endpoints.includes("/api/teams/approvals"));
assert("create permission resolver", helpers.includes("ResolveApprovalCreateAccessAsync"));
assert("review permission resolver", helpers.includes("ResolveApprovalReviewAccessAsync"));
assert("self-approval blocked in repository", repository.includes("kendi isteğini karara bağlayamaz"));
assert("no task lifecycle mutation", !repository.includes("TransitionTaskAsync") && !repository.includes("CreateSourceLinkedTaskAsync"));
assert("disclaimer records decisions only", constants.includes("yalnızca insan kararı kaydeder"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/teams/approvals?facilityId=${FACILITY}`, null, {});
assert("unauthenticated list 401", unauth.status === 401);

const unauthCreate = await liveRequest(
  "POST",
  `/api/teams/approvals?facilityId=${FACILITY}`,
  { channelId: URETIM_CHANNEL, title: "x", rationale: "y", idempotencyKey: "z", reviewers: [] },
  {},
);
assert("unauthenticated create 401", unauthCreate.status === 401);

const message = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  {
    body: `Onay güvenlik mesajı ${RUN_SUFFIX}`,
    idempotencyKey: `teams-approval-sec-${RUN_SUFFIX}`,
    mentionedMembershipIds: [],
  },
);
assert("source message posted", message.status === 200 || message.status === 201);
const messageId = message.json?.id;
assert("source message id", Boolean(messageId));

const negativeCreate = await liveRequest(
  "POST",
  `/api/teams/approvals?facilityId=${FACILITY}`,
  {
    channelId: URETIM_CHANNEL,
    messageId,
    title: "blocked",
    rationale: "blocked",
    idempotencyKey: `blocked-${RUN_SUFFIX}`,
    reviewers: [{ tenantMembershipId: REVIEWER_MEMBERSHIP, role: "reviewer" }],
  },
  actorHeaders(NEGATIVE),
);
assert("non-member create denied", negativeCreate.status === 403);

const guessed = await liveRequest("GET", `/api/teams/approvals/00000000-0000-4000-8000-000000000099?facilityId=${FACILITY}`, null, actorHeaders(NEGATIVE));
assert("guessed request hidden", guessed.status === 403 || guessed.status === 404);
assert("guessed request no status leak", !String(guessed.text ?? "").includes("pending"));

const selfReviewer = await liveRequest(
  "POST",
  `/api/teams/approvals?facilityId=${FACILITY}`,
  {
    channelId: URETIM_CHANNEL,
    messageId,
    title: `Kendine onay ${RUN_SUFFIX}`,
    rationale: "İstek sahibi inceleyen olamaz.",
    idempotencyKey: `self-reviewer-${RUN_SUFFIX}`,
    reviewers: [{ tenantMembershipId: "bb000001-0003-4000-8000-000000000003", role: "reviewer" }],
  },
);
assert("requester cannot assign self", selfReviewer.status === 400, selfReviewer.text?.slice(0, 200));

const bothSources = await liveRequest(
  "POST",
  `/api/teams/approvals?facilityId=${FACILITY}`,
  {
    channelId: URETIM_CHANNEL,
    messageId,
    aiTaskId: "bb000001-03d0-4000-8000-0000000003d0",
    title: "çift kaynak",
    rationale: "geçersiz",
    idempotencyKey: `both-${RUN_SUFFIX}`,
    reviewers: [{ tenantMembershipId: REVIEWER_MEMBERSHIP, role: "reviewer" }],
  },
);
assert("exclusive source required", bothSources.status === 400);

const createBody = {
  channelId: URETIM_CHANNEL,
  messageId,
  title: `Operasyon onay isteği ${RUN_SUFFIX}`,
  rationale: "İnsan kararı için gerekçe.",
  idempotencyKey: `teams-approval-create-${RUN_SUFFIX}`,
  reviewers: [{ tenantMembershipId: REVIEWER_MEMBERSHIP, role: "reviewer" }],
};

const created = await liveRequest("POST", `/api/teams/approvals?facilityId=${FACILITY}`, createBody);
assert("eligible member creates request", created.status === 201 || created.status === 200, created.text?.slice(0, 300));
const requestId = created.json?.id;
assert("request id returned", Boolean(requestId));
assert("pending status", created.json?.status === "pending");
assert("disclaimer present", String(created.json?.disclaimer ?? "").includes("insan kararı"));

const replay = await liveRequest("POST", `/api/teams/approvals?facilityId=${FACILITY}`, createBody);
assert("idempotent replay same id", replay.json?.id === requestId);
assert("replay does not duplicate", replay.status === 200);

const conflict = await liveRequest(
  "POST",
  `/api/teams/approvals?facilityId=${FACILITY}`,
  { ...createBody, title: "Farklı başlık", rationale: "Farklı gerekçe" },
);
assert("incompatible idempotency 409", conflict.status === 409);

const ownerDecide = await liveRequest(
  "POST",
  `/api/teams/approvals/${requestId}/decisions?facilityId=${FACILITY}`,
  { decision: "approved", note: "kendi isteğim" },
);
assert("requester cannot decide own request", ownerDecide.status === 403);

const negativeDecide = await liveRequest(
  "POST",
  `/api/teams/approvals/${requestId}/decisions?facilityId=${FACILITY}`,
  { decision: "approved" },
  actorHeaders(NEGATIVE),
);
assert("non-member cannot decide", negativeDecide.status === 403);

const decided = await liveRequest(
  "POST",
  `/api/teams/approvals/${requestId}/decisions?facilityId=${FACILITY}`,
  { decision: "approved", note: "İnceleme tamam." },
  actorHeaders(REVIEWER),
);
assert("assigned reviewer decides", decided.status === 200, decided.text?.slice(0, 300));
assert("resolved approved", decided.json?.status === "approved");
assert("decision recorded", decided.json?.decision?.decision === "approved");

const secondDecision = await liveRequest(
  "POST",
  `/api/teams/approvals/${requestId}/decisions?facilityId=${FACILITY}`,
  { decision: "rejected" },
  actorHeaders(REVIEWER),
);
assert("resolved request cannot receive another decision", secondDecision.status === 400 || secondDecision.status === 409);

const cancelResolved = await liveRequest(
  "POST",
  `/api/teams/approvals/${requestId}/cancel?facilityId=${FACILITY}`,
  {},
);
assert("resolved request cannot cancel", cancelResolved.status === 400);

const detail = await liveRequest("GET", `/api/teams/approvals/${requestId}?facilityId=${FACILITY}`);
assert("member can read own request", detail.status === 200 && detail.json?.id === requestId);
assert("source can open", detail.json?.source?.canOpen === true);
assert("audit present", Array.isArray(detail.json?.auditEvents) && detail.json.auditEvents.length >= 2);

const list = await liveRequest("GET", `/api/teams/approvals?facilityId=${FACILITY}`);
assert("list includes created request", list.status === 200 && list.json?.items?.some((item) => item.id === requestId));

const negativeList = await liveRequest("GET", `/api/teams/approvals?facilityId=${FACILITY}`, null, actorHeaders(NEGATIVE));
assert("non-member list empty or denied", negativeList.status === 403 || (negativeList.status === 200 && (negativeList.json?.items?.length ?? 0) === 0));

console.log("TEAMS-APPROVALS-1 security/idempotency verification passed.");

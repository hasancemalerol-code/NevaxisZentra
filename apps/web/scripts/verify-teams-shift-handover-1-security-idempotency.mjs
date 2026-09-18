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
  console.error(`TEAMS-SHIFT-HANDOVER-1 security/idempotency verification failed: ${message}`);
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

function item(title, extras = {}) {
  return {
    itemKind: "attention",
    title,
    body: "Operasyon bağlamı kaydı.",
    messageId: extras.messageId ?? null,
    aiTaskId: extras.aiTaskId ?? null,
  };
}

console.log("Running TEAMS-SHIFT-HANDOVER-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Teams/TeamsHandoverEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Teams/TeamsEndpointHelpers.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/Teams/TeamsHandoverRepository.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/Teams/TeamsConstants.cs");

assert("handovers route family", endpoints.includes("/api/teams/handovers"));
assert("create permission resolver", helpers.includes("ResolveHandoverCreateAccessAsync"));
assert("acknowledge permission resolver", helpers.includes("ResolveHandoverAcknowledgeAccessAsync"));
assert("self-ack blocked in repository", repository.includes("kendi vardiya devrini teslim alamaz"));
assert("published immutable", repository.includes("Yayımlanmış vardiya devri değiştirilemez"));
assert("no task lifecycle mutation", !repository.includes("TransitionTaskAsync") && !repository.includes("CreateSourceLinkedTaskAsync"));
assert("disclaimer records context only", constants.includes("yalnızca operasyon bağlamı kaydeder"));
assert("no delete endpoint", !endpoints.includes("MapDelete"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/teams/handovers?facilityId=${FACILITY}`, null, {});
assert("unauthenticated list 401", unauth.status === 401);

const unauthCreate = await liveRequest(
  "POST",
  `/api/teams/handovers?facilityId=${FACILITY}`,
  {
    channelId: URETIM_CHANNEL,
    shiftDate: "2026-08-30",
    shiftType: "morning",
    summary: "x",
    safetyNote: "",
    idempotencyKey: "z",
    items: [item("x")],
  },
  {},
);
assert("unauthenticated create 401", unauthCreate.status === 401);

const message = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  {
    body: `Vardiya devri güvenlik mesajı ${RUN_SUFFIX}`,
    idempotencyKey: `teams-handover-sec-${RUN_SUFFIX}`,
    mentionedMembershipIds: [],
  },
);
assert("source message posted", message.status === 200 || message.status === 201);
const messageId = message.json?.id;
assert("source message id", Boolean(messageId));

const negativeCreate = await liveRequest(
  "POST",
  `/api/teams/handovers?facilityId=${FACILITY}`,
  {
    channelId: URETIM_CHANNEL,
    shiftDate: "2026-08-30",
    shiftType: "morning",
    summary: "blocked",
    safetyNote: "",
    idempotencyKey: `blocked-${RUN_SUFFIX}`,
    items: [item("blocked", { messageId })],
  },
  actorHeaders(NEGATIVE),
);
assert("non-member create denied", negativeCreate.status === 403);

const guessed = await liveRequest(
  "GET",
  `/api/teams/handovers/00000000-0000-4000-8000-000000000099?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("guessed handover hidden", guessed.status === 403 || guessed.status === 404);
assert("guessed handover no status leak", !String(guessed.text ?? "").includes("published") && !String(guessed.text ?? "").includes("draft"));

const bothSources = await liveRequest(
  "POST",
  `/api/teams/handovers?facilityId=${FACILITY}`,
  {
    channelId: URETIM_CHANNEL,
    shiftDate: "2026-08-30",
    shiftType: "morning",
    summary: "çift kaynak",
    safetyNote: "",
    idempotencyKey: `both-${RUN_SUFFIX}`,
    items: [item("çift kaynak", {
      messageId,
      aiTaskId: "bb000001-03d0-4000-8000-0000000003d0",
    })],
  },
);
assert("exclusive source required", bothSources.status === 400);

const createBody = {
  channelId: URETIM_CHANNEL,
  shiftDate: "2026-08-30",
  shiftType: "morning",
  summary: `Operasyon vardiya özeti ${RUN_SUFFIX}`,
  safetyNote: "Güvenlik notu.",
  idempotencyKey: `teams-handover-create-${RUN_SUFFIX}`,
  items: [item(`Dikkat ${RUN_SUFFIX}`, { messageId })],
};

const created = await liveRequest("POST", `/api/teams/handovers?facilityId=${FACILITY}`, createBody);
assert("eligible member creates handover", created.status === 201 || created.status === 200, created.text?.slice(0, 300));
const handoverId = created.json?.id;
assert("handover id returned", Boolean(handoverId));
assert("draft status", created.json?.status === "draft");
assert("disclaimer present", String(created.json?.disclaimer ?? "").includes("operasyon bağlamı"));

const replay = await liveRequest("POST", `/api/teams/handovers?facilityId=${FACILITY}`, createBody);
assert("idempotent replay same id", replay.json?.id === handoverId);
assert("replay does not duplicate", replay.status === 200);

const conflict = await liveRequest(
  "POST",
  `/api/teams/handovers?facilityId=${FACILITY}`,
  { ...createBody, summary: "Farklı özet", safetyNote: "Farklı not" },
);
assert("incompatible idempotency 409", conflict.status === 409);

const published = await liveRequest("POST", `/api/teams/handovers/${handoverId}/publish?facilityId=${FACILITY}`);
assert("author publishes own draft", published.status === 200, published.text?.slice(0, 300));
assert("published status", published.json?.status === "published");

const editPublished = await liveRequest(
  "POST",
  `/api/teams/handovers/${handoverId}?facilityId=${FACILITY}`,
  {
    shiftDate: "2026-08-30",
    shiftType: "evening",
    summary: "değiştirilemez",
    safetyNote: "",
    items: [item("değiştirilemez")],
  },
);
assert("published handover cannot be edited", editPublished.status === 400 || editPublished.status === 403);

const ownerAck = await liveRequest("POST", `/api/teams/handovers/${handoverId}/acknowledgements?facilityId=${FACILITY}`);
assert("author cannot acknowledge own handover", ownerAck.status === 403);

const negativeAck = await liveRequest(
  "POST",
  `/api/teams/handovers/${handoverId}/acknowledgements?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("non-member cannot acknowledge", negativeAck.status === 403);

const acknowledged = await liveRequest(
  "POST",
  `/api/teams/handovers/${handoverId}/acknowledgements?facilityId=${FACILITY}`,
  null,
  actorHeaders(REVIEWER),
);
assert("eligible member acknowledges", acknowledged.status === 200, acknowledged.text?.slice(0, 300));
assert("ack recorded", Array.isArray(acknowledged.json?.acknowledgements) && acknowledged.json.acknowledgements.length >= 1);

const secondAck = await liveRequest(
  "POST",
  `/api/teams/handovers/${handoverId}/acknowledgements?facilityId=${FACILITY}`,
  null,
  actorHeaders(REVIEWER),
);
assert("ack replay same member", secondAck.status === 200);
assert("ack not duplicated", (secondAck.json?.acknowledgements?.length ?? 0) === (acknowledged.json?.acknowledgements?.length ?? 0));

const detail = await liveRequest("GET", `/api/teams/handovers/${handoverId}?facilityId=${FACILITY}`);
assert("member can read own handover", detail.status === 200 && detail.json?.id === handoverId);
assert("audit present", Array.isArray(detail.json?.auditEvents) && detail.json.auditEvents.length >= 2);

const list = await liveRequest("GET", `/api/teams/handovers?facilityId=${FACILITY}`);
assert("list includes created handover", list.status === 200 && list.json?.items?.some((row) => row.id === handoverId));

const negativeList = await liveRequest("GET", `/api/teams/handovers?facilityId=${FACILITY}`, null, actorHeaders(NEGATIVE));
assert(
  "non-member list empty or denied",
  negativeList.status === 403 || (negativeList.status === 200 && (negativeList.json?.items?.length ?? 0) === 0),
);

console.log("TEAMS-SHIFT-HANDOVER-1 security/idempotency verification passed.");

#!/usr/bin/env node
import { createHmac } from "node:crypto";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:5299").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5199").replace(/\/$/, "");
const FACILITY = process.env.NEVAXIS_FACTOS_WHATSAPP_GOLDEN_FACILITY ?? "bb000001-0005-4000-8000-000000000005";
const TASK = process.env.NEVAXIS_FACTOS_WHATSAPP_GOLDEN_TASK ?? "bb000001-03d0-4000-8000-0000000003d0";
const APP_SECRET = process.env.NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__APP_SECRET ?? "whatsapp-test-app-secret";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: process.env.NEVAXIS_FACTOS_WHATSAPP_GOLDEN_TENANT ?? "omnichannel-closure-tenant-a",
};

function fail(message) {
  console.error(`WHATSAPP golden flow failed: ${message}`);
  process.exit(1);
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ACTOR.issuer,
    "X-crevafactos-dev-Subject": ACTOR.subject,
    "X-NevaxisFactOS-Tenant": ACTOR.tenant,
  };
}

async function api(method, path, body, extra = {}) {
  const res = await fetch(`${API}${path}`, {
    method,
    headers: { ...headers(), ...extra },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const json = res.ok ? await res.json() : null;
  return { status: res.status, json };
}

async function waitFor(predicate, timeoutMs = 90000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (await predicate()) return;
    await new Promise((r) => setTimeout(r, 500));
  }
  fail("timeout in golden flow");
}

function signWebhook(body) {
  const hash = createHmac("sha256", APP_SECRET).update(body).digest("hex");
  return `sha256=${hash}`;
}

async function postWebhook(payload) {
  const body = JSON.stringify(payload);
  const res = await fetch(`${API}/api/v1/integrations/whatsapp/webhook`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Hub-Signature-256": signWebhook(body),
    },
    body,
  });
  return res.status;
}

const channels = await api("GET", `/api/ai-task-delivery/channels?facilityId=${FACILITY}`);
if (channels.status !== 200) fail(`channel readiness HTTP ${channels.status}`);
const channelList = channels.json?.channels ?? channels.json?.items ?? [];
const whatsapp = channelList.find((c) => c.channelKey === "whatsapp");
if (!whatsapp?.available) {
  fail(`WhatsApp channel not ready (HTTP ${channels.status}, available=${whatsapp?.available ?? false})`);
}
console.log("WHATSAPP_CHANNEL_READINESS=PASS");

const issue = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "24h", scope: "read-only" });
if (issue.status !== 200) fail(`secure link issue HTTP ${issue.status}`);
const token = issue.json?.accessToken ?? issue.json?.rawToken;
const grantId = issue.json?.grant?.id;
if (!token || !grantId) fail("secure link issue");

const goldenKey = `whatsapp-golden-${Date.now()}`;
const secureUrl = `${WEB}/gorev#access=${encodeURIComponent(token)}`;
const enqueue = await api(
  "POST",
  `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`,
  {
    channel: "whatsapp",
    recipientKind: "external",
    maskedRecipient: "+90 5** *** ** 33",
    recipientE164: "+905551112233",
    consentStatus: "known",
    secureAccessGrantId: grantId,
    secureTaskUrl: secureUrl,
  },
  { "Idempotency-Key": goldenKey },
);
if (enqueue.status !== 200) fail(`enqueue status ${enqueue.status}`);

let deliveryId = null;
let providerMessageId = null;
await waitFor(async () => {
  const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === goldenKey);
  if (!item) return false;
  deliveryId = item.id;
  providerMessageId = item.providerMessageId;
  return item.status === "sent";
});

if (!providerMessageId) {
  fail("provider message id missing after send");
}

const ts = String(Math.floor(Date.now() / 1000));
const webhookPayload = {
  entry: [{
    changes: [{
      value: {
        statuses: [{
          id: providerMessageId,
          status: "delivered",
          timestamp: ts,
          conversation: { id: `conv-${deliveryId}` },
        }],
      },
    }],
  }],
};

const deliveredStatus = await postWebhook(webhookPayload);
if (deliveredStatus !== 200) fail(`delivered webhook ${deliveredStatus}`);

await waitFor(async () => {
  const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const item = (list.json?.items ?? []).find((d) => d.id === deliveryId);
  return item?.providerStatus === "delivered";
});

const readPayload = {
  entry: [{
    changes: [{
      value: {
        statuses: [{
          id: providerMessageId,
          status: "read",
          timestamp: String(Number(ts) + 5),
          conversation: { id: `conv-${deliveryId}` },
        }],
      },
    }],
  }],
};
const readStatus = await postWebhook(readPayload);
if (readStatus !== 200) fail(`read webhook ${readStatus}`);

await waitFor(async () => {
  const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const item = (list.json?.items ?? []).find((d) => d.id === deliveryId);
  return item?.providerStatus === "read";
});

const taskPage = await fetch(`${WEB}/ai-gorevleri/${TASK}`);
if (!taskPage.ok) fail(`task page ${taskPage.status}`);

console.log("WHATSAPP_GOLDEN_FLOW=PASS");

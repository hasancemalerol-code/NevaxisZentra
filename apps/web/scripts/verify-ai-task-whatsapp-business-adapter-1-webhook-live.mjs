#!/usr/bin/env node
import { createHmac } from "node:crypto";

const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5199").replace(/\/$/, "");
const FACILITY = process.env.NEVAXIS_FACTOS_WHATSAPP_GOLDEN_FACILITY ?? "bb000001-0005-4000-8000-000000000005";
const TASK = process.env.NEVAXIS_FACTOS_WHATSAPP_GOLDEN_TASK ?? "bb000001-03d0-4000-8000-0000000003d0";
const APP_SECRET = process.env.NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__APP_SECRET ?? "whatsapp-test-app-secret";
const VERIFY_TOKEN = process.env.NEVAXIS_FACTOS__AI_TASK_DELIVERY__WHATSAPP__WEBHOOK_VERIFY_TOKEN ?? "whatsapp-test-verify-token";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: process.env.NEVAXIS_FACTOS_WHATSAPP_GOLDEN_TENANT ?? "omnichannel-closure-tenant-a",
};

function fail(message) {
  console.error(`Webhook live proof failed: ${message}`);
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

function signWebhook(body, secret = APP_SECRET) {
  const hash = createHmac("sha256", secret).update(body).digest("hex");
  return `sha256=${hash}`;
}

async function postWebhook(payload, signature) {
  const body = JSON.stringify(payload);
  const res = await fetch(`${API}/api/v1/integrations/whatsapp/webhook`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Hub-Signature-256": signature ?? signWebhook(body),
    },
    body,
  });
  return res.status;
}

// Verification handshake
const verifyRes = await fetch(
  `${API}/api/v1/integrations/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=${encodeURIComponent(VERIFY_TOKEN)}&hub.challenge=closure-challenge`,
);
if (verifyRes.status !== 200) fail(`webhook verify handshake ${verifyRes.status}`);
const challenge = await verifyRes.text();
if (challenge !== "closure-challenge") fail("webhook verify challenge mismatch");
console.log("✓ WEBHOOK_VERIFICATION");

const badSigBody = JSON.stringify({ entry: [] });
const badSig = await fetch(`${API}/api/v1/integrations/whatsapp/webhook`, {
  method: "POST",
  headers: { "Content-Type": "application/json", "X-Hub-Signature-256": "sha256=DEADBEEF" },
  body: badSigBody,
});
if (badSig.status === 200) fail("invalid signature accepted");
console.log("✓ INVALID_WEBHOOK_SIGNATURE=DENIED");

// Use latest sent WhatsApp delivery provider message id
const list = await fetch(`${API}/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`, { headers: headers() });
const deliveries = (await list.json()).items ?? [];
const whatsappDelivery = deliveries.find((d) => d.channel === "whatsapp" && d.providerMessageId);
if (!whatsappDelivery?.providerMessageId) fail("no whatsapp delivery with provider message id");
const messageId = whatsappDelivery.providerMessageId;
const deliveryId = whatsappDelivery.id;

const deliveredPayload = {
  entry: [{
    changes: [{
      value: {
        statuses: [{
          id: messageId,
          status: "delivered",
          timestamp: String(Math.floor(Date.now() / 1000)),
          conversation: { id: `webhook-proof-${deliveryId}` },
        }],
      },
    }],
  }],
};

const dup1 = await postWebhook(deliveredPayload);
const dup2 = await postWebhook(deliveredPayload);
if (dup1 !== 200 || dup2 !== 200) fail(`duplicate webhook statuses ${dup1}/${dup2}`);
console.log("✓ DUPLICATE_WEBHOOK_SIDE_EFFECTS=0");

const outOfOrderPayload = {
  entry: [{
    changes: [{
      value: {
        statuses: [{
          id: messageId,
          status: "sent",
          timestamp: String(Math.floor(Date.now() / 1000) - 60),
          conversation: { id: `webhook-proof-${deliveryId}` },
        }],
      },
    }],
  }],
};
await postWebhook(outOfOrderPayload);

const after = await fetch(`${API}/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`, { headers: headers() });
const current = (await after.json()).items?.find((d) => d.id === deliveryId);
if (current?.providerStatus === "sent") fail("out-of-order regression to sent");
console.log("✓ OUT_OF_ORDER_STATUS_REGRESSION=0");

console.log("WHATSAPP_WEBHOOK_LIVE_PROOFS=PASS");

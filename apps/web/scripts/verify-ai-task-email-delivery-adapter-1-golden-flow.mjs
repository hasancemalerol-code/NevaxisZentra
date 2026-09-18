#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:5311").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5211").replace(/\/$/, "");
const SMTP_HTTP = (process.env.NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_URL ?? "http://127.0.0.1:19026").replace(/\/$/, "");
const CAPTURE_FILE = resolve(ROOT, "artifacts/email-closure/captured-messages.jsonl");
const FACILITY = process.env.NEVAXIS_FACTOS_EMAIL_GOLDEN_FACILITY ?? "bb000001-0005-4000-8000-000000000005";
const TASK = process.env.NEVAXIS_FACTOS_EMAIL_GOLDEN_TASK ?? "bb000001-03d0-4000-8000-0000000003d0";
const RECIPIENT = process.env.NEVAXIS_FACTOS_EMAIL_GOLDEN_RECIPIENT ?? "hakan@company.com";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: process.env.NEVAXIS_FACTOS_EMAIL_GOLDEN_TENANT ?? "omnichannel-closure-tenant-a",
};

function fail(message) {
  console.error(`EMAIL golden flow failed: ${message}`);
  process.exit(1);
}

function headers(tenant = ACTOR.tenant) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ACTOR.issuer,
    "X-crevafactos-dev-Subject": ACTOR.subject,
    "X-NevaxisZentra-Tenant": tenant,
  };
}

async function api(method, path, body, extra = {}, tenant = ACTOR.tenant) {
  const res = await fetch(`${API}${path}`, {
    method,
    headers: { ...headers(tenant), ...extra },
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

const channels = await api("GET", `/api/ai-task-delivery/channels?facilityId=${FACILITY}`);
if (channels.status !== 200) fail(`channel readiness HTTP ${channels.status}`);
const email = (channels.json?.channels ?? []).find((c) => c.channelKey === "email");
if (!email?.available) fail(`Email channel not ready: ${JSON.stringify(email)}`);
console.log("EMAIL_CHANNEL_READINESS_HTTP_STATUS=200");
console.log("EMAIL_CHANNEL_READINESS=PASS");

const taskBefore = await api("GET", `/api/ai-tasks/${TASK}?facilityId=${FACILITY}`);
if (taskBefore.status !== 200) fail(`task read HTTP ${taskBefore.status}`);
const statusBefore = taskBefore.json?.status ?? taskBefore.json?.task?.status;

const issue = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "24h", scope: "read-only" });
if (issue.status !== 200) fail(`secure link issue HTTP ${issue.status}`);
const token = issue.json?.accessToken;
const grantId = issue.json?.grant?.id;
if (!token || !grantId) fail("secure link issue missing token/grant");

const goldenKey = `email-golden-${Date.now()}`;
const secureUrl = `${WEB}/gorev#access=${encodeURIComponent(token)}`;
const enqueue = await api(
  "POST",
  `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`,
  {
    channel: "email",
    recipientKind: "external",
    maskedRecipient: "h***n@company.com",
    recipientEmail: RECIPIENT,
    recipientEligibility: "eligible",
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

if (!providerMessageId) fail("transport Message-ID missing after send");
console.log("TRANSPORT_MESSAGE_ID_PERSISTENCE=PASS");

const taskAfter = await api("GET", `/api/ai-tasks/${TASK}?facilityId=${FACILITY}`);
const statusAfter = taskAfter.json?.status ?? taskAfter.json?.task?.status;
if (statusBefore && statusAfter && statusBefore !== statusAfter) {
  fail(`task workflow changed: ${statusBefore} -> ${statusAfter}`);
}
console.log("TASK_WORKFLOW_UNCHANGED=PASS");

const publicResolve = await fetch(`${API}/api/public/ai-task-access/resolve`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ accessToken: token }),
});
if (!publicResolve.ok) fail(`secure /gorev resolve HTTP ${publicResolve.status}`);
console.log("SECURE_TASK_LINK=PASS");

let captured = null;
try {
  captured = await fetch(`${SMTP_HTTP}/last-captured`).then((r) => r.json());
} catch {
  try {
    const lines = readFileSync(CAPTURE_FILE, "utf8").trim().split("\n").filter(Boolean);
    captured = JSON.parse(lines[lines.length - 1]);
  } catch {
    fail("no captured SMTP message");
  }
}

if (!captured?.subject?.includes("crevafactos") || !captured?.subject?.includes("bildirimi")) fail("EMAIL_SUBJECT missing");
if (!captured?.from?.includes("closure.NevaxisZentra") && !captured?.from?.includes("crevafactos")) fail("EMAIL_FROM missing");
if (!captured?.toMasked && !captured?.rcptTo) fail("EMAIL_RECIPIENT missing");
const bodyBlob = `${captured?.plainTextBody ?? ""}\n${captured?.htmlBody ?? ""}`;
if (!captured?.plainTextBody?.includes("Factory Operations")) fail("EMAIL_PLAIN_TEXT_PART missing");
if (!captured?.htmlBody?.includes("Factory Operations")) fail("EMAIL_HTML_PART missing");
if (!bodyBlob.includes("gorev#access=[REDACTED]") && !bodyBlob.includes("/gorev")) fail("EMAIL_SECURE_CTA missing");
if (JSON.stringify(captured).match(/#access=[A-Za-z0-9._%-]{20,}/)) fail("RAW_SECURE_TOKEN in capture");

console.log("EMAIL_SUBJECT=PASS");
console.log("EMAIL_FROM=PASS");
console.log("EMAIL_RECIPIENT=PASS");
console.log("EMAIL_PLAIN_TEXT_PART=PASS");
console.log("EMAIL_HTML_PART=PASS");
console.log("EMAIL_SECURE_CTA=PASS");
console.log("EMAIL_GOLDEN_FLOW=PASS");

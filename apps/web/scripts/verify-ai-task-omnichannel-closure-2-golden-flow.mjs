#!/usr/bin/env node
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:5299").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5199").replace(/\/$/, "");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TASK = "bb000001-03d0-4000-8000-0000000003d0";
const ACTOR = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "omnichannel-closure-tenant-a" };

function fail(message) {
  console.error(`Golden flow failed: ${message}`);
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

async function waitFor(predicate, timeoutMs = 30000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (await predicate()) return;
    await new Promise((r) => setTimeout(r, 500));
  }
  fail("timeout in golden flow");
}

const taskPage = await fetch(`${WEB}/ai-gorevleri/${TASK}`);
if (!taskPage.ok) fail(`task page ${taskPage.status}`);

const issue = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "24h", scope: "read-only" });
const token = issue.json?.accessToken ?? issue.json?.rawToken;
const grantId = issue.json?.grant?.id;
if (!token || !grantId) fail("secure link issue");

const publicPage = await fetch(`${WEB}/gorev`);
if (!publicPage.ok) fail(`public page ${publicPage.status}`);
const publicResolve = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: token });
if (publicResolve.status !== 200) fail("public resolve");

const goldenKey = `closure-golden-flow-${Date.now()}`;
const enqueue = await api(
  "POST",
  `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`,
  {
    channel: "development-loopback",
    recipientKind: "user",
    recipientUserId: "bb000001-0002-4000-8000-000000000002",
  },
  { "Idempotency-Key": goldenKey },
);
if (enqueue.status !== 200) fail("enqueue");

await waitFor(async () => {
  const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === goldenKey);
  return item?.status === "sent";
}, 90000);

const revoke = await api("POST", `/api/ai-task-secure-access/${grantId}/revoke?facilityId=${FACILITY}`, { reason: "golden-flow" });
if (revoke.status !== 200) fail("revoke");
const denied = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: token });
if (denied.status === 200) fail("revoked link still resolves");

console.log("LIVE_FOUNDATION_FLOW=PASS");

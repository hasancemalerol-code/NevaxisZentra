#!/usr/bin/env node
/**
 * Omnichannel closure-2 live API proofs (no secrets in output).
 */
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const API = (process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5199").replace(/\/$/, "");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TASK = "bb000001-03d0-4000-8000-0000000003d0";
const TENANT_B = "omnichannel-closure-tenant-b";
const ACTOR = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "omnichannel-closure-tenant-a" };
const ACTOR_B = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: TENANT_B };
const OUT = join(ROOT, "artifacts/omnichannel-closure/live-closure-results.json");

const results = {
  generatedAtUtc: new Date().toISOString(),
  apiBase: API,
  proofs: {},
};

function fail(message) {
  console.error(`CLOSURE-2 proof failed: ${message}`);
  writeFileSync(OUT, JSON.stringify(results, null, 2));
  process.exit(1);
}

function pass(key, detail = {}) {
  results.proofs[key] = { status: "PASS", ...detail };
  console.log(`✓ ${key}`);
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function api(method, path, body, actor = ACTOR, extra = {}) {
  const res = await fetch(`${API}${path}`, {
    method,
    headers: { ...headers(actor), ...extra },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: res.status, json, text };
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

async function waitForDelivery(idempotencyKey, predicate, timeoutMs = 45000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
    const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === idempotencyKey);
    if (item && predicate(item)) return item;
    await sleep(500);
  }
  fail(`timeout waiting for delivery ${idempotencyKey}`);
}

async function main() {
  mkdirSync(dirname(OUT), { recursive: true });

  const cancelKey = `closure-cancel-${Date.now()}`;
  const cancelEnqueue = await api(
    "POST",
    `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`,
    {
      channel: "development-loopback",
      recipientKind: "user",
      recipientUserId: "bb000001-0002-4000-8000-000000000002",
    },
    ACTOR,
    { "Idempotency-Key": cancelKey },
  );
  if (cancelEnqueue.status !== 200) fail(`cancel enqueue ${cancelEnqueue.status}`);
  const cancelId = cancelEnqueue.json.id;
  const cancelRes = await api("POST", `/api/ai-task-deliveries/${cancelId}/cancel?facilityId=${FACILITY}`, {});
  if (cancelRes.status !== 200 || cancelRes.json.status !== "cancelled") fail("cancel API");
  await sleep(5000);
  const cancelDetail = await api("GET", `/api/ai-task-deliveries/${cancelId}?facilityId=${FACILITY}`);
  const cancelledDelivery = cancelDetail.json.delivery ?? cancelDetail.json;
  const cancelAttempts = cancelDetail.json.attempts ?? [];
  if ((cancelledDelivery.attemptCount ?? -1) !== 0) {
    fail(`cancel attempts ${cancelledDelivery.attemptCount}`);
  }
  if (cancelAttempts.length !== 0) fail(`cancel adapter invocations ${cancelAttempts.length}`);
  pass("cancel", { deliveryId: cancelId, status: cancelledDelivery.status, attemptCount: 0 });

  const deadKey = "closure-dead-letter-002";
  const deadEnqueue = await api(
    "POST",
    `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`,
    {
      channel: "development-loopback",
      recipientKind: "user",
      recipientUserId: "bb000001-0002-4000-8000-000000000002",
      deliveryProjectionJson: JSON.stringify({ fixture: "transient" }),
      maxAttempts: 3,
    },
    ACTOR,
    { "Idempotency-Key": deadKey },
  );
  if (deadEnqueue.status !== 200) fail(`dead-letter enqueue ${deadEnqueue.status}`);
  const deadDelivery = await waitForDelivery(deadKey, (d) => d.status === "dead-letter");
  if (deadDelivery.attemptCount !== 3) fail(`dead-letter attempt count ${deadDelivery.attemptCount}`);
  pass("deadLetter", { deliveryId: deadDelivery.id, attemptCount: deadDelivery.attemptCount });

  const issueExpiry = await api(
    "POST",
    `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`,
    { ttlPolicy: "closure-10s", scope: "read-only" },
  );
  if (issueExpiry.status !== 200 || !(issueExpiry.json.accessToken ?? issueExpiry.json.rawToken)) {
    fail("issue closure TTL grant");
  }
  const expiryToken = issueExpiry.json.accessToken ?? issueExpiry.json.rawToken;
  const validResolve = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: expiryToken });
  if (validResolve.status !== 200) fail("pre-expiry resolve");
  await sleep(12000);
  const expiredResolve = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: expiryToken });
  if (expiredResolve.status === 200) fail("expired token still valid");
  pass("expiry", { deniedStatus: expiredResolve.status });

  const issueRotate = await api(
    "POST",
    `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`,
    { ttlPolicy: "24h", scope: "read-only" },
  );
  const tokenA = issueRotate.json.accessToken ?? issueRotate.json.rawToken;
  const grantId = issueRotate.json.grant.id;
  const resolveA = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: tokenA });
  if (resolveA.status !== 200) fail("token A resolve");
  const rotate = await api(
    "POST",
    `/api/ai-task-secure-access/${grantId}/rotate?facilityId=${FACILITY}`,
    { ttlPolicy: "24h" },
  );
  if (rotate.status !== 200 || !(rotate.json.accessToken ?? rotate.json.rawToken)) fail("rotate");
  const tokenB = rotate.json.accessToken ?? rotate.json.rawToken;
  const deniedA = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: tokenA });
  const validB = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: tokenB });
  if (deniedA.status === 200) fail("rotated old token still valid");
  if (validB.status !== 200) fail("rotated new token invalid");
  pass("rotation", { oldDeniedStatus: deniedA.status, newValidStatus: validB.status });

  const crossTenant = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`, undefined, ACTOR_B);
  if (crossTenant.status !== 403) fail(`cross-tenant ${crossTenant.status}`);
  pass("crossTenant", { status: crossTenant.status });

  const tampered = `${tokenB.slice(0, -2)}xx`;
  const tamperRes = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: tampered });
  if (tamperRes.status === 200) fail("tampered token accepted");
  pass("tamperedToken", { status: tamperRes.status });

  const issueRevoke = await api(
    "POST",
    `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`,
    { ttlPolicy: "24h", scope: "read-only" },
  );
  const revokeToken = issueRevoke.json.accessToken ?? issueRevoke.json.rawToken;
  const revokeGrantId = issueRevoke.json.grant.id;
  if (issueRevoke.status !== 200 || !revokeToken || !revokeGrantId) fail("issue revoke grant");
  await api("POST", `/api/ai-task-secure-access/${revokeGrantId}/revoke?facilityId=${FACILITY}`, { reason: "closure" });
  const revokedResolve = await api("POST", "/api/public/ai-task-access/resolve", { accessToken: revokeToken });
  if (revokedResolve.status === 200) fail("revoked token accepted");
  pass("revokedLink", { status: revokedResolve.status });

  const restartKey = "closure-restart-002";
  const restartEnqueue = await api(
    "POST",
    `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`,
    {
      channel: "development-loopback",
      recipientKind: "user",
      recipientUserId: "bb000001-0002-4000-8000-000000000002",
    },
    ACTOR,
    { "Idempotency-Key": restartKey },
  );
  pass("restartDurabilitySeed", { deliveryId: restartEnqueue.json.id, idempotencyKey: restartKey });

  writeFileSync(OUT, JSON.stringify(results, null, 2));
  console.log(`Wrote ${OUT}`);
}

main().catch((e) => fail(e.message));

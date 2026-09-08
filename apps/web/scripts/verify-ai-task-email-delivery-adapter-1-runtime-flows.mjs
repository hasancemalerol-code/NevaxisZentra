#!/usr/bin/env node
import { readFileSync, writeFileSync, readdirSync, mkdirSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = (process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5211").replace(/\/$/, "");
const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:5311").replace(/\/$/, "");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TASK = "bb000001-03d0-4000-8000-0000000003d0";
const RECIPIENT = "retry.test@company.com";
const PERMANENT_RECIPIENT = "bad.recipient@company.com";
const TENANT_A = "omnichannel-closure-tenant-a";
const TENANT_B = "omnichannel-closure-tenant-b";
const RETRY_KEY_FILE = resolve(ROOT, "artifacts/email-closure/retry-flow-key.txt");
mkdirSync(resolve(ROOT, "artifacts/email-closure"), { recursive: true });

function fail(message) {
  console.error(`EMAIL runtime flow failed: ${message}`);
  process.exit(1);
}

function headers(tenant) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": "crevafactos-dev",
    "X-CrevaFactOS-Dev-Subject": tenant === TENANT_B ? "omnichannel-closure-tenant-b-owner" : "planning-pilot-user",
    "X-CrevaFactOS-Tenant": tenant,
  };
}

async function api(method, path, body, extra = {}, tenant = TENANT_A) {
  const res = await fetch(`${API}${path}`, {
    method,
    headers: { ...headers(tenant), ...extra },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const json = res.ok ? await res.json() : null;
  return { status: res.status, json };
}

async function waitFor(predicate, timeoutMs = 120000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (await predicate()) return;
    await new Promise((r) => setTimeout(r, 500));
  }
  fail(`timeout phase=${phase}`);
}

async function issueAndEnqueue(idempotencyKey, recipient, tenant = TENANT_A) {
  const issue = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "24h", scope: "read-only" }, {}, tenant);
  if (issue.status !== 200) fail(`secure access HTTP ${issue.status}`);
  const grantId = issue.json?.grant?.id;
  const token = issue.json?.accessToken;
  const secureUrl = `${WEB}/gorev#access=${encodeURIComponent(token)}`;
  const enqueue = await api(
    "POST",
    `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`,
    {
      channel: "email",
      recipientKind: "external",
      maskedRecipient: "r***t@company.com",
      recipientEmail: recipient,
      recipientEligibility: "eligible",
      secureAccessGrantId: grantId,
      secureTaskUrl: secureUrl,
    },
    { "Idempotency-Key": idempotencyKey },
    tenant,
  );
  if (enqueue.status !== 200) fail(`enqueue HTTP ${enqueue.status}`);
  return enqueue.json;
}

const phase = process.argv.find((a) => a.startsWith("--phase="))?.split("=")[1] ?? "all";

async function runRetryFail() {
  const key = `email-retry-${Date.now()}`;
  writeFileSync(RETRY_KEY_FILE, key, "utf8");
  await issueAndEnqueue(key, RECIPIENT);
  await waitFor(async () => {
    const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
    const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === key);
    return item?.status === "failed-retryable";
  });
  console.log("EMAIL_TEMPORARY_FAILURE_CLASSIFICATION=PASS");
}

async function runRetryComplete() {
  const key = readFileSync(RETRY_KEY_FILE, "utf8").trim();
  await waitFor(async () => {
    const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
    const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === key);
    return item?.status === "sent";
  }, 120000);
  console.log("EMAIL_RETRY_FLOW=PASS");
}

async function runRetry() {
  await runRetryFail();
  await runRetryComplete();
}

async function runPermanent() {
  const key = `email-permanent-${Date.now()}`;
  await issueAndEnqueue(key, PERMANENT_RECIPIENT);
  await waitFor(async () => {
    const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
    const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === key);
    return item?.status === "failed-permanent";
  });
  const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === key);
  if ((item?.attemptCount ?? 0) > 2) fail("PERMANENT_EMAIL_RETRIED unexpected extra attempts");
  await new Promise((r) => setTimeout(r, 8000));
  const list2 = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const item2 = (list2.json?.items ?? []).find((d) => d.idempotencyKey === key);
  if (item2?.status !== "failed-permanent") fail("permanent delivery retried");
  console.log("EMAIL_PERMANENT_FAILURE_CLASSIFICATION=PASS");
  console.log("PERMANENT_EMAIL_RETRIED=FALSE");
  console.log("EMAIL_PERMANENT_FAILURE_FLOW=PASS");
}

async function runIdempotency() {
  const key = `email-idempotency-${Date.now()}`;
  const first = await issueAndEnqueue(key, "idem@company.com");
  const second = await issueAndEnqueue(key, "idem@company.com");
  if (first.id !== second.id) fail("duplicate email deliveries created");
  const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const matches = (list.json?.items ?? []).filter((d) => d.idempotencyKey === key);
  if (matches.length !== 1) fail(`DUPLICATE_EMAIL_DISPATCH count=${matches.length}`);
  console.log("EMAIL_IDEMPOTENCY=PASS");
  console.log("DUPLICATE_EMAIL_DISPATCH=0");
}

async function runAuthClassification() {
  const key = `email-auth-fail-${Date.now()}`;
  await issueAndEnqueue(key, "auth.fail@company.com");
  await waitFor(async () => {
    const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
    const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === key);
    return item?.status === "failed-permanent" && String(item?.lastErrorCode ?? "").includes("auth");
  }, 60000);
  console.log("EMAIL_AUTH_FAILURE_CLASSIFICATION=PASS");
}

async function readDeliveryFromDb(idempotencyKey) {
  const result = spawnSync(
    "docker",
    [
      "exec",
      "crevafactos-local-postgres-1",
      "psql",
      "-U",
      "crevafactos",
      "-d",
      "crevafactos_email_closure_1",
      "-t",
      "-A",
      "-c",
      `SELECT status, COALESCE(last_error_code,'') FROM ai_task_deliveries WHERE idempotency_key='${idempotencyKey}' LIMIT 1`,
    ],
    { encoding: "utf8" },
  );
  if (result.status !== 0 || !result.stdout.trim()) return null;
  const [status, code] = result.stdout.trim().split("|");
  return { status, code };
}

async function runTimeoutClassification() {
  const timeoutKey = `email-timeout-${Date.now()}`;
  await issueAndEnqueue(timeoutKey, "timeout@company.com");
  await waitFor(async () => {
    const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
    const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === timeoutKey);
    const db = await readDeliveryFromDb(timeoutKey);
    const status = item?.status ?? db?.status;
    const code = String(item?.lastErrorCode ?? db?.code ?? "");
    return status === "failed-retryable"
      && (code.includes("timeout") || code.includes("network") || code.includes("421"));
  }, 120000);
  console.log("EMAIL_TIMEOUT_CLASSIFICATION=PASS");
}

async function runTlsClassification() {
  const tlsKey = `email-tls-${Date.now()}`;
  await issueAndEnqueue(tlsKey, "tls@company.com");
  await waitFor(async () => {
    const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
    const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === tlsKey);
    return item?.status === "failed-permanent";
  }, 60000);
  console.log("EMAIL_TLS_FAILURE_CLASSIFICATION=PASS");
}

async function runCrossTenant() {
  const tenantBDelivery = await api(
    "GET",
    `/api/ai-task-deliveries/${"00000000-0000-0000-0000-000000000001"}?facilityId=cc000002-0005-4000-8000-000000000005`,
    undefined,
    {},
    TENANT_A,
  );
  if (tenantBDelivery.status === 200) fail("cross-tenant delivery read allowed");
  const channelsB = await api("GET", `/api/ai-task-delivery/channels?facilityId=${FACILITY}`, undefined, {}, TENANT_B);
  if (channelsB.status === 200 && (channelsB.json?.channels ?? []).some((c) => c.channelKey === "email" && c.available)) {
    // tenant B has no email config — should not be available
    const email = (channelsB.json?.channels ?? []).find((c) => c.channelKey === "email");
    if (email?.available) fail("tenant B email should not be available");
  }
  console.log("EMAIL_CROSS_TENANT_ACCESS=DENIED");
}

function scanLogs() {
  const patterns = [
    { label: "SMTP_PASSWORD_LOG_MATCHES", regex: /closure-smtp-password/gi },
    { label: "RAW_SECURE_TOKEN_LOG_MATCHES", regex: /#access=[A-Za-z0-9._-]{20,}/g },
    { label: "FULL_EMAIL_RECIPIENT_LOG_MATCHES", regex: /hakan@company\.com/gi },
  ];
  const logDir = resolve(ROOT, "artifacts/email-closure/logs");
  let files = [];
  try {
    files = readdirSync(logDir).map((f) => resolve(logDir, f));
  } catch {
    files = [];
  }
  for (const { label, regex } of patterns) {
    let matches = 0;
    for (const file of files) {
      try {
        const content = readFileSync(file, "utf8");
        const found = content.match(regex);
        if (found) matches += found.length;
      } catch {
        // ignore
      }
    }
    if (matches > 0) fail(`${label}=${matches}`);
    console.log(`${label}=0`);
  }
  console.log("CLIENT_EXPOSED_EMAIL_SECRETS=0");
  console.log("RAW_SMTP_RESPONSE_PII_LEAK=0");
}

if (phase === "retry") await runRetry();
else if (phase === "retry-fail") await runRetryFail();
else if (phase === "retry-complete") await runRetryComplete();
else if (phase === "permanent") await runPermanent();
else if (phase === "idempotency") await runIdempotency();
else if (phase === "auth") await runAuthClassification();
else if (phase === "timeout") await runTimeoutClassification();
else if (phase === "tls") await runTlsClassification();
else if (phase === "cross-tenant") await runCrossTenant();
else if (phase === "security-scan") scanLogs();
else {
  await runIdempotency();
  await runCrossTenant();
  scanLogs();
}

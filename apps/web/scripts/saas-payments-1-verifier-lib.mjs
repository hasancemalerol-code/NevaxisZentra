#!/usr/bin/env node
import { createHmac, randomUUID } from "node:crypto";
import { createServer as createHttpServer } from "node:http";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

export const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
export const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
export const ORGANIZATION = "dev-pilot-tenant";
export const CLOSURE_PREFIX = "SAAS-PAYMENTS-1-CLOSURE";
export const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
export const DEV_OPEN_INVOICE_ID = "bb000003-00b1-4000-8000-0000000000b1";
export const DEV_OVERDUE_INVOICE_ID = "bb000003-00b3-4000-8000-0000000000b3";
export const MOCK_API_KEY = "mock-iyzico-api-key";
export const MOCK_SECRET_KEY = "mock-iyzico-secret-key";

const sessions = new Map();

export function fail(message) {
  console.error(`SAAS-PAYMENTS-1 verification failed: ${message}`);
  process.exit(1);
}

export function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

export function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

export function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

export function previewClosureFixtures() {
  try {
    const sessions = Number(db(`SELECT COUNT(*) FROM saas_billing_checkout_sessions WHERE conversation_id LIKE '${CLOSURE_PREFIX}%'`));
    const events = Number(db(`SELECT COUNT(*) FROM saas_billing_provider_events WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
    const payments = Number(db(`SELECT COUNT(*) FROM saas_billing_payment_attempts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
    console.log(`Preview ${CLOSURE_PREFIX}: sessions=${sessions}, events=${events}, payments=${payments}`);
    return { sessions, events, payments };
  } catch {
    console.log(`Preview ${CLOSURE_PREFIX}: checkout tables not migrated yet`);
    return { sessions: 0, events: 0, payments: 0 };
  }
}

export function removeClosureFixtures() {
  db(`
    DELETE FROM saas_billing_audit_events
    WHERE tenant_id = '${TENANT_ID}'
      AND entity_reference IN (
        SELECT id::text FROM saas_billing_payment_attempts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
      );
    DELETE FROM saas_billing_provider_events WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_billing_checkout_sessions
    WHERE conversation_id LIKE '${CLOSURE_PREFIX}%'
      OR payment_attempt_id IN (
        SELECT id FROM saas_billing_payment_attempts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
      );
    DELETE FROM saas_billing_idempotency_records
    WHERE tenant_id = '${TENANT_ID}'
      AND operation = 'billing.provider-webhook.process';
    DELETE FROM saas_billing_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_billing_payment_attempts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

export function computeWebhookSignature(body, secretKey = MOCK_SECRET_KEY) {
  const payload = JSON.parse(body);
  const key = secretKey
    + (payload.iyziEventType ?? "")
    + (payload.iyziPaymentId ?? payload.paymentId ?? "")
    + (payload.token ?? "")
    + (payload.paymentConversationId ?? "")
    + (payload.status ?? "");
  return createHmac("sha256", secretKey).update(key).digest("hex");
}

export function startMockIyzicoServer() {
  const server = createHttpServer((req, res) => {
    const chunks = [];
    req.on("data", (chunk) => chunks.push(chunk));
    req.on("end", () => {
      const body = Buffer.concat(chunks).toString("utf8");
      if (req.url?.includes("initialize")) {
        const token = `mock-checkout-token-${randomUUID()}`;
        const parsed = JSON.parse(body);
        sessions.set(token, {
          conversationId: parsed.conversationId,
          price: parsed.price,
          currency: parsed.currency,
          status: "SUCCESS",
        });
        res.writeHead(200, { "Content-Type": "application/json" });
        res.end(JSON.stringify({
          status: "success",
          token,
          paymentPageUrl: `http://127.0.0.1:18081/mock-checkout?token=${encodeURIComponent(token)}`,
        }));
        return;
      }

      if (req.url?.includes("detail")) {
        const parsed = JSON.parse(body);
        const session = sessions.get(parsed.token) ?? {
          conversationId: parsed.conversationId,
          price: "100.00",
          currency: "TRY",
          status: "SUCCESS",
        };
        res.writeHead(200, { "Content-Type": "application/json" });
        res.end(JSON.stringify({
          status: "success",
          paymentStatus: session.status,
          paidPrice: session.price,
          currency: session.currency,
          paymentId: "mock-payment-id",
        }));
        return;
      }

      res.writeHead(404);
      res.end();
    });
  });

  return new Promise((resolvePromise, rejectPromise) => {
    server.once("error", (error) => {
      if (error?.code === "EADDRINUSE") {
        resolvePromise({ close() {} });
        return;
      }
      rejectPromise(error);
    });
    server.listen(18081, "127.0.0.1", () => resolvePromise(server));
  });
}

export class CookieJar {
  constructor() {
    this.cookies = new Map();
  }

  ingest(response) {
    const setCookies = typeof response.headers.getSetCookie === "function"
      ? response.headers.getSetCookie()
      : [];
    for (const raw of setCookies) {
      const [pair] = raw.split(";");
      const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      this.cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
    }
  }

  get(name) {
    return this.cookies.get(name);
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

export async function api(method, path, body, jar, extraHeaders = {}) {
  const headers = { "Content-Type": "application/json", Accept: "application/json", ...extraHeaders };
  if (jar?.cookieHeader()) headers.Cookie = jar.cookieHeader();
  if (jar?.get("NevaxisFactOS.session") && jar.get("NevaxisFactOS.csrf") && method !== "GET") {
    headers["X-NevaxisFactOS-Csrf"] = jar.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

export function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    const value = TOTP_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA shared secret.");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const secretBytes = Buffer.from(output);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", secretBytes).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

export async function signInOwner() {
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "admin@demo.nevaxisfactos.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    deviceLabel: `${CLOSURE_PREFIX}-owner`,
  }, jar);
  if (signIn.status !== 200) fail(`Owner sign-in failed: ${signIn.status}`);
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
  if (verify.status !== 200) fail(`Owner MFA failed: ${verify.status}`);
  return jar;
}

export async function readProviderMode(jar) {
  const provider = await api("GET", "/api/saas-billing/provider-status", undefined, jar);
  return provider.json?.providerMode ?? provider.json?.provider?.providerMode ?? "not-configured";
}

export function assertNoSecretLeakage(raw) {
  const forbidden = [MOCK_SECRET_KEY, MOCK_API_KEY, "closure-token-", "cvv", "cardNumber"];
  const lower = raw.toLowerCase();
  for (const token of forbidden) {
    assert(`no leakage of ${token}`, !lower.includes(token.toLowerCase()));
  }
}

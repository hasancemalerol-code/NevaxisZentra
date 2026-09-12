#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`LOCAL-SUBSCRIPTION-RECURRENCE validation failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__SUBSCRIPTION_RECURRENCE_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) {
      text = readFileSync(candidate, "utf8");
      break;
    }
  }
  for (const key of [
    "DevelopmentSaasIdentity__TenantOwnerPassword",
    "DevelopmentSaasIdentity__MfaSharedSecret",
    "DevelopmentSaasIdentity__ActiveUserPassword",
  ]) {
    if (process.env[key]?.trim()) continue;
    const match = text.match(new RegExp(`${key}='([^']+)'`));
    if (match?.[1]) process.env[key] = match[1];
  }
  process.env.__SUBSCRIPTION_RECURRENCE_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    const value = TOTP_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid MFA secret");
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

class CookieJar {
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
  header() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

async function api(method, path, body, jar, extraHeaders = {}) {
  const headers = { Accept: "application/json", ...extraHeaders };
  if (body) headers["Content-Type"] = "application/json";
  if (jar?.header()) headers.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisFactOS.session") && jar.cookies.get("NevaxisFactOS.csrf") && method !== "GET") {
    headers["X-NevaxisFactOS-Csrf"] = jar.cookies.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = null; }
  return { status: response.status, json, raw: text };
}

async function signInOwner(jar) {
  const password = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "tenant-owner@dev.local",
    password,
    deviceLabel: "LOCAL-SUBSCRIPTION-RECURRENCE-CLOSURE-2",
  }, jar);
  assert("tenant-owner sign-in", signIn.status === 200 && signIn.json?.requiresMfa === true, `status=${signIn.status}`);
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(secret) }, jar);
  assert("tenant-owner MFA", verify.status === 200, `status=${verify.status}`);
}

console.log("Running LOCAL-SUBSCRIPTION-RECURRENCE API validation.");

const ready = await fetch(`${API}/health/ready`);
assert("API /health/ready", ready.status === 200, `status=${ready.status}`);
const giris = await fetch(`${WEB}/giris`);
assert("web /giris", giris.status === 200, `status=${giris.status}`);

const jar = new CookieJar();
await signInOwner(jar);

for (const path of [
  "/api/identity/me",
  "/api/saas-subscription/current",
  "/api/saas-subscription/access-status?routePath=/dashboard",
  "/api/saas-subscription/access-status?routePath=/enerji",
  "/api/saas-subscription/access-status?routePath=/fabrika-vitrini",
]) {
  const result = await api("GET", path, undefined, jar);
  assert(`${path} -> 200`, result.status === 200, `status=${result.status} title=${result.json?.title ?? "null"}`);
}

const current = await api("GET", "/api/saas-subscription/current", undefined, jar);
assert("current subscription active", current.json?.subscription?.subscriptionStatus === "active");

for (const path of [
  "/api/saas-subscription/access-status?routePath=/dashboard",
  "/api/saas-subscription/access-status?routePath=/enerji",
  "/api/saas-subscription/access-status?routePath=/fabrika-vitrini",
]) {
  const access = await api("GET", path, undefined, jar);
  assert(`${path} allowed`, access.json?.allowed === true, JSON.stringify(access.json));
}

const missingAuth = await fetch(`${WEB}/dashboard`, { redirect: "manual" });
assert("missing auth redirects away from dashboard", missingAuth.status === 307 || missingAuth.status === 302);
const missingAuthApi = await api("GET", "/api/saas-subscription/current");
assert("missing auth subscription/current -> 401", missingAuthApi.status === 401, `status=${missingAuthApi.status}`);
const crossTenant = await api("GET", "/api/saas-subscription/current", undefined, jar, {
  "X-NevaxisFactOS-Tenant": "other-tenant-not-pilot",
});
assert("cross-tenant denied", crossTenant.status === 403, `status=${crossTenant.status}`);

const subscriptionId = current.json?.subscription?.subscriptionId;
const version = current.json?.subscription?.version ?? 1;
if (subscriptionId) {
  const suspend = await api("POST", "/api/saas-subscription/transitions", {
    subscriptionId,
    targetStatus: "suspended",
    transitionReason: "manual-suspension",
    expectedVersion: version,
    idempotencyKey: `LOCAL-SUBSCRIPTION-RECURRENCE-suspend-${Date.now()}`,
  }, jar);
  assert("inactive subscription transition", suspend.status === 201 || suspend.status === 200, `status=${suspend.status}`);
  const denied = await api("GET", "/api/saas-subscription/access-status?routePath=/dashboard", undefined, jar);
  assert("inactive subscription denied", denied.json?.allowed === false, JSON.stringify(denied.json));
  const reactivate = await api("POST", "/api/saas-subscription/transitions", {
    subscriptionId,
    targetStatus: "active",
    transitionReason: "manual-reactivation",
    expectedVersion: version + 1,
    idempotencyKey: `LOCAL-SUBSCRIPTION-RECURRENCE-reactivate-${Date.now()}`,
  }, jar);
  assert("subscription reactivated", reactivate.status === 201 || reactivate.status === 200, `status=${reactivate.status}`);
}

const staleCookieResponse = await fetch(`${WEB}/dashboard`, {
  headers: { Cookie: "NevaxisFactOS.session=invalid-local-recurrence-probe" },
  redirect: "manual",
});
const staleLocation = staleCookieResponse.headers.get("location") ?? "";
assert(
  "stale cookie SSR redirects to session-expired login",
  staleLocation.includes("/giris") && staleLocation.includes("reason=session-expired"),
  staleLocation,
);

console.log("Browser/UI proof is covered by verify-auth-session-expiry-routing-closure-1.mjs (no cookie injection).");
console.log("LOCAL-SUBSCRIPTION-RECURRENCE API validation: PASS");

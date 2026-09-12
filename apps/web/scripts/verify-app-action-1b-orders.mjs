#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";

function fail(message) {
  console.error(`APP-ACTION-1B orders verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing required environment variable ${name}`);
  return value;
}

function read(relativePath) {
  return readFileSync(join(root, relativePath), "utf8");
}

function generateTotp(secret) {
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | alphabet.indexOf(character);
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
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

async function api(method, path, body, jar) {
  const headers = { Accept: "application/json" };
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
  return { status: response.status };
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, {
    headers: { Cookie: jar.header(), Accept: "text/html" },
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
}

async function signInOwner() {
  const jar = new CookieJar();
  const signInResult = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "admin@demo.nevaxisfactos.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    deviceLabel: "app-action-1b-orders",
  }, jar);
  if (signInResult.status !== 200) fail("owner sign-in failed");
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

const actionPanel = read("src/features/orders/components/order-draft-action-panel.tsx");
assert("approval service gated off", actionPanel.includes("ORDER_APPROVAL_SERVICE_READY = false"));
assert("ERP transfer service gated off", actionPanel.includes("ORDER_ERP_TRANSFER_SERVICE_READY = false"));
assert("approval unavailable reason is Turkish", actionPanel.includes("Onay gönderme servisi henüz kullanıma hazır değil."));
assert("ERP unavailable reason is Turkish", actionPanel.includes("ERP aktarım servisi henüz kullanıma hazır değil."));
assert("no fake ERP transfer handler", !/onClick=\{.*transfer/i.test(actionPanel));

const owner = await signInOwner();
const detail = await fetchWeb("/siparisler/order-draft-001", owner);
assert("order detail is 200", detail.status === 200);
assert("Onaya gönder label present", detail.html.includes("Onaya gönder"));
assert("ERP aktar label present", detail.html.includes("ERP"));
assert("approval action disabled honestly", /Onaya gönder[\s\S]*disabled/.test(detail.html) || detail.html.includes("Onay gönderme servisi henüz kullanıma hazır değil."));
assert("ERP action disabled honestly", /ERP[\s\S]*disabled/.test(detail.html) || detail.html.includes("ERP aktarım servisi henüz kullanıma hazır değil."));
assert("no fake transferred status claim in action panel", !/aktarıldı|transfer completed/i.test(detail.html));

console.log("APP-ACTION-1B orders verification passed.");

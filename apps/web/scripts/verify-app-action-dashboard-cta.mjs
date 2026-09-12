#!/usr/bin/env node
import { createHmac } from "node:crypto";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";

function fail(message) {
  console.error(`APP-ACTION dashboard CTA verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
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

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: "app-action-dashboard-cta",
  }, jar);
  if (result.status !== 200) fail("sign-in failed for persona");
  return jar;
}

async function signInOwner() {
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = await signIn("admin@demo.nevaxisfactos.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
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
  const totp = String(binaryCode % 1_000_000).padStart(6, "0");
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: totp }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

function hasMaintenanceQuickAction(html) {
  return /class="[^"]*locked-quick-action[^"]*"[^>]*href="\/bakim"|href="\/bakim"[^>]*class="[^"]*locked-quick-action[^"]*"/.test(html)
    || /locked-quick-action[\s\S]{0,240}Bakım Talebi/.test(html);
}

async function fetchDashboard(jar) {
  const response = await fetch(`${WEB}/dashboard`, {
    headers: { Cookie: jar.header(), Accept: "text/html" },
    redirect: "manual",
  });
  return { status: response.status, html: await response.text() };
}

const password = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const owner = await fetchDashboard(await signInOwner());
const machine = await fetchDashboard(await signIn("machine-responsible@dev.local", password));
const sales = await fetchDashboard(await signIn("materials-sales@dev.local", password));
const maintenance = await fetchDashboard(await signIn("maintenance-team@dev.local", password));

assert("owner dashboard loads", owner.status === 200);
assert("owner sees Bakım Talebi CTA", hasMaintenanceQuickAction(owner.html));
assert("maintenance-only sees Bakım Talebi CTA", hasMaintenanceQuickAction(maintenance.html));
assert("machine-responsible dashboard loads", machine.status === 200);
assert("machine-responsible does not see Bakım Talebi CTA", !hasMaintenanceQuickAction(machine.html));
assert("materials-only does not see Bakım Talebi CTA", !hasMaintenanceQuickAction(sales.html));
assert("unauthorized CTA is not an active /bakim quick action", !hasMaintenanceQuickAction(machine.html) && !hasMaintenanceQuickAction(sales.html));

console.log("APP-ACTION dashboard CTA verification passed.");

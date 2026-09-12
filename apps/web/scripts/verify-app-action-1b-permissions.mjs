#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const root = join(dirname(fileURLToPath(import.meta.url)), "..");

function fail(message) {
  console.error(`APP-ACTION-1B permission verification failed: ${message}`);
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
  let payload = null;
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }
  }
  return { status: response.status, payload };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: "app-action-1b-permissions",
  }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn(
    "tenant-owner@dev.local",
    requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  );
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, {
    headers: { Cookie: jar.header(), Accept: "text/html" },
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
}

const dashboardNav = read("src/components/dashboard/dashboard-navigation-action.tsx");
assert("denied dashboard navigation is hidden", dashboardNav.includes("return null"));

const password = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const owner = await signInOwner();
const machine = await signIn("machine-responsible@dev.local", password);
const materials = await signIn("materials-sales@dev.local", password);
const maintenance = await signIn("maintenance-team@dev.local", password);
const readOnly = await signIn("active-user@dev.local", password);

const ownerDashboard = await fetchWeb("/dashboard", owner);
assert("owner dashboard is 200", ownerDashboard.status === 200);

for (const [name, jar, path, expected] of [
  ["machine-responsible", machine, "/kaliplar", 403],
  ["materials-only", materials, "/vardiyalar", 403],
  ["maintenance-only", maintenance, "/proses-oee", 403],
]) {
  const page = await fetchWeb(path, jar);
  assert(`${name} direct route ${path} is ${expected}`, page.status === expected);
}

const ownerKaliplar = await fetchWeb("/kaliplar", owner);
const ownerVardiyalar = await fetchWeb("/vardiyalar", owner);
const ownerProses = await fetchWeb("/proses-oee", owner);
const ownerPlanlama = await fetchWeb("/bakim/planlama", owner);
assert("owner can open kaliplar", ownerKaliplar.status === 200);
assert("owner can open vardiyalar", ownerVardiyalar.status === 200);
assert("owner can open proses-oee", ownerProses.status === 200);
assert("owner can open bakim/planlama", ownerPlanlama.status === 200);

const machineDashboard = await fetchWeb("/dashboard", machine);
const materialsDashboard = await fetchWeb("/dashboard", materials);
const maintenanceDashboard = await fetchWeb("/dashboard", maintenance);
const readOnlyDashboard = await fetchWeb("/dashboard", readOnly);

for (const [name, result] of [
  ["machine-responsible", machineDashboard],
  ["materials-only", materialsDashboard],
  ["maintenance-only", maintenanceDashboard],
  ["normal-user", readOnlyDashboard],
]) {
  assert(`${name} dashboard is 200`, result.status === 200);
  assert(
    `${name} dashboard has no permission-denied prohibited cursor markup`,
    !/Üretim emirlerini görüntüleme yetkiniz yok/.test(result.html)
      && !/Bakım planını görüntüleme yetkiniz yok/.test(result.html),
  );
}

const readOnlyAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, readOnly);
assert("read-only effective-access API responds", readOnlyAccess.status === 200);

console.log("APP-ACTION-1B permission verification passed.");

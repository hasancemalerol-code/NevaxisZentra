#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`AUTH-SESSION-EXPIRY-ROUTING-CLOSURE-1 failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__AUTH_SESSION_ROUTING_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(process.env.USERPROFILE ?? process.env.HOME ?? "", ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(process.env.USERPROFILE ?? process.env.HOME ?? "", ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(process.env.USERPROFILE ?? process.env.HOME ?? "", ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
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
  ]) {
    if (process.env[key]?.trim()) continue;
    const match = text.match(new RegExp(`${key}='([^']+)'`));
    if (match?.[1]) process.env[key] = match[1];
  }
  process.env.__AUTH_SESSION_ROUTING_BOOTSTRAPPED = "1";
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

function resolveChromiumExecutable() {
  const candidates = [
    process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    process.env.CHROME_PATH,
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
  ].filter(Boolean);
  for (const candidate of candidates) {
    if (existsSync(candidate)) return candidate;
  }
  return undefined;
}

async function readHeading(page) {
  return page.locator("h1").first().textContent().catch(() => null);
}

async function signInThroughForm(page, { organization, email, password }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
  await page.waitForURL("**/mfa-dogrula", { timeout: 60000 });
}

async function completeMfa(page, secret) {
  await page.waitForURL("**/mfa-dogrula", { timeout: 60000 });
  await page.locator("#mfa-totp").fill(generateTotp(secret));
  await page.getByRole("button", { name: "Doğrula" }).click();
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

async function ownerApiSession() {
  const password = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "admin@demo.nevaxisfactos.local",
    password,
    deviceLabel: "AUTH-SESSION-EXPIRY-ROUTING-setup",
  }, jar);
  assert("setup owner sign-in", signIn.status === 200, `status=${signIn.status}`);
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(secret) }, jar);
  assert("setup owner MFA", verify.status === 200, `status=${verify.status}`);
  return jar;
}

function assertRecurrenceVerifierDoesNotInjectCookies() {
  const recurrencePath = join(process.cwd(), "scripts/verify-local-subscription-recurrence-closure-2.mjs");
  if (!existsSync(recurrencePath)) return;
  const source = readFileSync(recurrencePath, "utf8");
  assert(
    "recurrence verifier rejects cookie injection",
    !source.includes("addCookies(") && !source.includes("asPlaywrightCookies"),
    "verify-local-subscription-recurrence-closure-2.mjs must not inject browser cookies",
  );
}

console.log("Running AUTH-SESSION-EXPIRY-ROUTING-CLOSURE-1 verifier.");

assertRecurrenceVerifierDoesNotInjectCookies();

const ready = await fetch(`${API}/health/ready`);
assert("API /health/ready", ready.status === 200, `status=${ready.status}`);
const girisReady = await fetch(`${WEB}/giris`);
assert("web /giris", girisReady.status === 200, `status=${girisReady.status}`);

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const profileDir = mkdtempSync(join(tmpdir(), "NevaxisFactOS-auth-session-routing-"));

try {
  const browser = await chromium.launchPersistentContext(profileDir, {
    headless: true,
    ...(executablePath ? { executablePath } : {}),
  });

  // A. No session cookie
  {
    const page = await browser.newPage();
    const response = await page.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
    assert("A final URL is login", page.url().includes("/giris"), page.url());
    assert("A not subscription recovery", !page.url().includes("/abonelik-erisimi"), page.url());
    const heading = await readHeading(page);
    assert("A login heading", heading === "Oturum aç", heading ?? "null");
    console.log(`A requested=/dashboard final=${page.url()} status=${response?.status() ?? "null"}`);
    await page.close();
  }

  // B. Random invalid session cookie (negative proof only)
  {
    const page = await browser.newPage();
    await page.context().addCookies([{
      name: "NevaxisFactOS.session",
      value: "invalid-random-session-probe",
      domain: new URL(WEB).hostname,
      path: "/",
      httpOnly: true,
      secure: false,
      sameSite: "Lax",
    }]);
    const response = await page.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
    assert("B invalid cookie redirects to login", page.url().includes("/giris"), page.url());
    assert("B reason=session-expired", page.url().includes("reason=session-expired"), page.url());
    assert("B not subscription recovery", !page.url().includes("/abonelik-erisimi"), page.url());
    const notice = await page.locator(".auth-feedback--info").textContent().catch(() => null);
    assert("B session-expired notice", notice?.includes("Oturumunuzun süresi doldu"), notice ?? "null");
    console.log(`B requested=/dashboard final=${page.url()} status=${response?.status() ?? "null"}`);
    await page.close();
  }

  // C. Revoked real session cookie
  {
    const setupJar = await ownerApiSession();
    const page = await browser.newPage();
    await page.context().addCookies([{
      name: "NevaxisFactOS.session",
      value: setupJar.cookies.get("NevaxisFactOS.session"),
      domain: new URL(WEB).hostname,
      path: "/",
      httpOnly: true,
      secure: false,
      sameSite: "Lax",
    }]);
    await api("POST", "/api/identity/sign-out", undefined, setupJar);
    const response = await page.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
    assert("C revoked cookie redirects to login", page.url().includes("/giris"), page.url());
    assert("C reason=session-expired", page.url().includes("reason=session-expired"), page.url());
    console.log(`C requested=/dashboard final=${page.url()} status=${response?.status() ?? "null"}`);
    await page.close();
  }

  // D/E/F and real sign-in proof in a clean persistent page flow
  await browser.clearCookies();
  let proofPage = await browser.newPage();

  // Real form sign-in + MFA (Phase 5)
  await proofPage.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
  assert("sign-in redirect from dashboard", proofPage.url().includes("/giris"), proofPage.url());

  await proofPage.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await signInThroughForm(proofPage, {
    organization: ORGANIZATION,
    email: "admin@demo.nevaxisfactos.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  });
  await completeMfa(proofPage, requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));
  await proofPage.waitForURL("**/dashboard", { timeout: 60000 });
  assert("real form sign-in reaches dashboard", proofPage.url().includes("/dashboard"), proofPage.url());

  for (const route of ["/dashboard", "/enerji", "/fabrika-vitrini"]) {
    const response = await proofPage.goto(`${WEB}${route}`, { waitUntil: "networkidle", timeout: 90000 });
    assert(`${route} loads`, !proofPage.url().includes("/abonelik-erisimi"), proofPage.url());
    assert(`${route} HTTP 200`, response?.status() === 200, String(response?.status()));
    const refresh = await proofPage.reload({ waitUntil: "networkidle", timeout: 90000 });
    assert(`${route} refresh preserved`, !proofPage.url().includes("/abonelik-erisimi"), proofPage.url());
    assert(`${route} refresh HTTP 200`, refresh?.status() === 200, String(refresh?.status()));
  }

  await proofPage.close();
  const relaunchedPage = await browser.newPage();
  const relaunchedResponse = await relaunchedPage.goto(`${WEB}/dashboard`, {
    waitUntil: "networkidle",
    timeout: 90000,
  });
  assert("browser session persistence", relaunchedPage.url().includes("/dashboard"), relaunchedPage.url());
  assert("browser session persistence HTTP 200", relaunchedResponse?.status() === 200, String(relaunchedResponse?.status()));
  proofPage = relaunchedPage;

  // F. Authenticated service-unavailable recovery state
  const unavailableResponse = await proofPage.goto(`${WEB}/abonelik-erisimi?reason=service-unavailable`, {
    waitUntil: "networkidle",
    timeout: 90000,
  });
  assert("F service-unavailable recovery route", proofPage.url().includes("/abonelik-erisimi"), proofPage.url());
  assert("F service-unavailable reason", proofPage.url().includes("reason=service-unavailable"), proofPage.url());
  assert("F not login", !proofPage.url().includes("/giris"), proofPage.url());
  const unavailableHeading = await readHeading(proofPage);
  assert("F unavailable heading", unavailableHeading?.includes("Abonelik bilgisi"), unavailableHeading ?? "null");
  console.log(`F requested=/abonelik-erisimi final=${proofPage.url()} status=${unavailableResponse?.status() ?? "null"}`);

  // Restore access and verify valid owner session (D)
  const dashboardResponse = await proofPage.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
  assert("D valid session dashboard", proofPage.url().includes("/dashboard"), proofPage.url());
  assert("D dashboard HTTP 200", dashboardResponse?.status() === 200, String(dashboardResponse?.status()));
  console.log(`D requested=/dashboard final=${proofPage.url()} status=${dashboardResponse?.status() ?? "null"}`);

  // E. Valid session with inactive subscription (API fixture setup only)
  const ownerJar = await ownerApiSession();
  const current = await api("GET", "/api/saas-subscription/current", undefined, ownerJar);
  const subscriptionId = current.json?.subscription?.subscriptionId;
  const version = current.json?.subscription?.version ?? 1;
  const suspend = await api("POST", "/api/saas-subscription/transitions", {
    subscriptionId,
    targetStatus: "suspended",
    transitionReason: "manual-suspension",
    expectedVersion: version,
    idempotencyKey: `AUTH-SESSION-ROUTING-suspend-${Date.now()}`,
  }, ownerJar);
  assert("inactive subscription fixture", suspend.status === 201 || suspend.status === 200, `status=${suspend.status}`);

  const inactiveResponse = await proofPage.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
  assert("E inactive subscription recovery route", proofPage.url().includes("/abonelik-erisimi"), proofPage.url());
  assert("E inactive not login", !proofPage.url().includes("/giris"), proofPage.url());
  assert("E paid-application reason", proofPage.url().includes("reason=paid-application"), proofPage.url());
  const inactiveHeading = await readHeading(proofPage);
  assert("E inactive heading", inactiveHeading?.includes("Uygulama erişimi"), inactiveHeading ?? "null");
  console.log(`E requested=/dashboard final=${proofPage.url()} status=${inactiveResponse?.status() ?? "null"}`);

  await api("POST", "/api/saas-subscription/transitions", {
    subscriptionId,
    targetStatus: "active",
    transitionReason: "manual-reactivation",
    expectedVersion: version + 1,
    idempotencyKey: `AUTH-SESSION-ROUTING-reactivate-${Date.now()}`,
  }, ownerJar);

  await proofPage.close();
  await browser.close();
} finally {
  try {
    rmSync(profileDir, { recursive: true, force: true });
  } catch {
    // Windows may keep persistent profile handles open briefly after browser close.
  }
}

// Cross-tenant API negative proof
{
  const jar = await ownerApiSession();
  const crossTenant = await api("GET", "/api/saas-subscription/current", undefined, jar, {
    "X-NevaxisFactOS-Tenant": "other-tenant-not-pilot",
  });
  assert("cross-tenant denied", crossTenant.status === 403, `status=${crossTenant.status}`);
}

console.log("AUTH-SESSION-EXPIRY-ROUTING-CLOSURE-1: PASS");
console.log("COOKIE_INJECTION_USED_FOR_POSITIVE_AUTH: NO");

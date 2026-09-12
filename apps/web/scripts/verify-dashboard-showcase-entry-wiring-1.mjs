#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ORGANIZATION = "dev-pilot-tenant";
const DEV_SUBSCRIPTION_ID = "bb000002-00e0-4000-8000-0000000000e0";
const VIEWPORTS = [
  { width: 1600, height: 1000, label: "1600x1000" },
  { width: 1024, height: 900, label: "1024x900" },
];
const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;

function fail(message) {
  console.error(`DASHBOARD-SHOWCASE-ENTRY-WIRING-1 verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
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

function base32Decode(encoded) {
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  const normalized = encoded.trim().replace(/=+$/, "").toUpperCase();
  const output = [];
  let buffer = 0;
  let bitsLeft = 0;
  for (const character of normalized) {
    const value = alphabet.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA secret");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  return Buffer.from(output);
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", base32Decode(secret)).update(counterBytes).digest();
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

  get(name) {
    return this.cookies.get(name);
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

async function api(method, path, body, jar) {
  const headers = { Accept: "application/json", "Content-Type": "application/json" };
  if (jar?.cookieHeader()) headers.Cookie = jar.cookieHeader();
  if (jar?.get("NevaxisFactOS.session") && jar?.get("NevaxisFactOS.csrf") && method !== "GET") {
    headers["X-NevaxisFactOS-Csrf"] = jar.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${API}${path}`, {
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

async function signInThroughApi(email, password, mfaSecret) {
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: "dashboard-showcase-entry-wiring-1",
  }, jar);
  if (signIn.status !== 200) fail(`sign-in failed for ${email}: ${signIn.status}`);
  if (mfaSecret) {
    const verify = await api("POST", "/api/identity/mfa/verify", {
      totpCode: generateTotp(mfaSecret),
    }, jar);
    if (verify.status !== 200) fail(`MFA failed for ${email}`);
  }
  return jar;
}

async function signInThroughForm(page, { email, password, mfaSecret }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(ORGANIZATION);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
  if (mfaSecret) {
    await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
    await page.locator("#mfa-totp").fill(generateTotp(mfaSecret));
    await page.getByRole("button", { name: "Doğrula" }).click();
  }
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, {
    headers: { Cookie: jar.cookieHeader(), Accept: "text/html" },
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
}

async function assertDashboardShowcaseEntry(page, label) {
  await page.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
  assert(`${label} dashboard route loaded`, page.url().includes("/dashboard"));

  const link = page.locator('[data-dashboard-showcase-tv-link="true"]');
  await link.waitFor({ state: "visible", timeout: 15000 });
  assert(`${label} showcase TV button visible`, await link.isVisible());
  assert(`${label} button label`, (await link.innerText()).includes("TV Görünümünü Aç"));
  assert(`${label} href exactly /fabrika-vitrini`, await link.getAttribute("href") === "/fabrika-vitrini");
  assert(`${label} target _blank`, await link.getAttribute("target") === "_blank");
  assert(`${label} rel noopener noreferrer`, await link.getAttribute("rel") === "noopener noreferrer");
  assert(`${label} accessible name`, await link.getAttribute("aria-label") === "Fabrika vitrini TV görünümünü aç");

  const bodyText = await page.locator("body").innerText();
  assert(`${label} no raw UUID`, !UUID_PATTERN.test(bodyText));

  const overflow = await page.evaluate(() => ({
    horizontal: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
  }));
  assert(`${label} no horizontal overflow`, !overflow.horizontal);

  await link.focus();
  const focusVisible = await page.evaluate(() => {
    const active = document.activeElement;
    if (!active) return false;
    const style = window.getComputedStyle(active);
    return style.outlineStyle !== "none" || style.boxShadow !== "none";
  });
  assert(`${label} keyboard focus visible`, focusVisible);

  const destination = await fetchWeb("/fabrika-vitrini", await signInThroughApi(
    "admin@demo.nevaxisfactos.local",
    requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"),
  ));
  assert(`${label} destination HTTP 200`, destination.status === 200, `status=${destination.status}`);
}

console.log("Running DASHBOARD-SHOWCASE-ENTRY-WIRING-1 verifier.");

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
const activePassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");

const ownerJar = await signInThroughApi("admin@demo.nevaxisfactos.local", ownerPassword, mfaSharedSecret);
const ownerDashboard = await fetchWeb("/dashboard", ownerJar);
assert("tenant owner dashboard HTTP 200", ownerDashboard.status === 200, `status=${ownerDashboard.status}`);
assert("tenant owner dashboard includes showcase link", ownerDashboard.html.includes('data-dashboard-showcase-tv-link="true"'));
assert("tenant owner dashboard href", ownerDashboard.html.includes('href="/fabrika-vitrini"'));

const activeJar = await signInThroughApi("active-user@dev.local", activePassword);
const activeDashboard = await fetchWeb("/dashboard", activeJar);
assert("active user dashboard HTTP 200", activeDashboard.status === 200);
assert("active user sees showcase link", activeDashboard.html.includes('data-dashboard-showcase-tv-link="true"'));

async function transitionDevSubscription(jar, targetStatus, idempotencyKey, extras = {}) {
  const current = await api("GET", "/api/saas-subscription/current", undefined, jar);
  const subscription = current.json?.subscription;
  return api("POST", "/api/saas-subscription/transitions", {
    subscriptionId: subscription?.subscriptionId ?? DEV_SUBSCRIPTION_ID,
    targetStatus,
    transitionReason: extras.transitionReason ?? "manual-suspension",
    expectedVersion: subscription?.version ?? 1,
    idempotencyKey,
  }, jar);
}

async function ensureDevSubscriptionActive(jar, prefix) {
  const current = await api("GET", "/api/saas-subscription/current", undefined, jar);
  const status = current.json?.subscription?.subscriptionStatus;
  if (status === "active") return;
  const restore = await transitionDevSubscription(jar, "active", `${prefix}-restore-${Date.now()}`, {
    transitionReason: "manual-reactivation",
  });
  assert("subscription restored to active before probe", restore.status === 200 || restore.status === 201, restore.raw.slice(0, 180));
}

const probePrefix = `DASHBOARD-SHOWCASE-ENTRY-WIRING-1-${Date.now()}`;
await ensureDevSubscriptionActive(ownerJar, probePrefix);

const suspend = await transitionDevSubscription(ownerJar, "suspended", `${probePrefix}-suspend`);
const suspendedStatus = (await api("GET", "/api/saas-subscription/current", undefined, ownerJar)).json?.subscription?.subscriptionStatus;
assert(
  "subscription suspended for denied probe",
  suspend.status === 200 || suspend.status === 201 || suspendedStatus === "suspended",
  suspend.raw.slice(0, 180),
);

try {
  const deniedDashboard = await fetchWeb("/dashboard", ownerJar);
  assert(
    "denied subscription does not render active showcase link",
    !deniedDashboard.html.includes('data-dashboard-showcase-tv-link="true"'),
  );
} finally {
  await ensureDevSubscriptionActive(ownerJar, `${probePrefix}-cleanup`);
  assert("subscription restored to active", (await api("GET", "/api/saas-subscription/current", undefined, ownerJar)).json?.subscription?.subscriptionStatus === "active");
}

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const browser = await chromium.launch({
  headless: true,
  ...(executablePath ? { executablePath } : {}),
});

const consoleErrors = [];

try {
  for (const viewport of VIEWPORTS) {
    const context = await browser.newContext({ viewport });
    const page = await context.newPage();
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    page.on("pageerror", (err) => consoleErrors.push(String(err)));

    await signInThroughForm(page, {
      email: "admin@demo.nevaxisfactos.local",
      password: ownerPassword,
      mfaSecret: mfaSharedSecret,
    });
    await page.waitForURL("**/dashboard", { timeout: 30000 });
    await assertDashboardShowcaseEntry(page, viewport.label);
    await context.close();
  }

  const hydrationLike = consoleErrors.some((line) =>
    /hydration|serialization|Text content does not match|Only plain objects/i.test(line),
  );
  assert("no hydration/serialization/console errors", !hydrationLike, consoleErrors.join(" | "));
} finally {
  await browser.close();
}

console.log("DASHBOARD-SHOWCASE-ENTRY-WIRING-1 verification passed.");

#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-SUBSCRIPTION-1-CLOSURE";
const DEV_PLAN_VERSION_ID = "bb000001-00e2-4000-8000-0000000000e2";
const DEV_SUBSCRIPTION_ID = "bb000002-00e0-4000-8000-0000000000e0";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const SALES_MEMBER = "mem_bb000003021340008000000000000213";
const ADMIN_ACCESS_READ_PERMISSION_ID = "bb000003-0001-4000-8000-000000000001";
const VIEWPORTS = [
  { width: 1600, height: 1000 },
  { width: 1024, height: 900 },
];
const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
const INTERNAL_ENUM_PATTERN = /\b(PendingVerification|Locked|mfa_pending|lifecycle_status|grace-period|trialing)\b/;
const OPAQUE_TOKEN_PATTERN = /\b[a-f0-9]{40,}\b/i;
const DISABLED_PAYMENT_BUTTON = "Ödeme ve plan değiştirme akışı henüz kullanıma hazır değil.";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`SAAS-SUBSCRIPTION-1 functional-ui verification failed: ${message}`);
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

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function restoreDevSubscriptionCurrent() {
  db(`
    UPDATE saas_tenant_subscriptions
    SET is_current = false, updated_at_utc = NOW()
    WHERE tenant_id = '${TENANT_ID}' AND id <> '${DEV_SUBSCRIPTION_ID}' AND is_current = true;

    UPDATE saas_tenant_subscriptions
    SET is_current = true,
        subscription_status = 'active',
        commercial_plan_version_id = '${DEV_PLAN_VERSION_ID}',
        billing_cadence = 'monthly',
        grace_until_utc = NULL,
        suspended_at_utc = NULL,
        cancelled_at_utc = NULL,
        cancel_at_period_end = false,
        updated_at_utc = NOW()
    WHERE id = '${DEV_SUBSCRIPTION_ID}';
  `);
}

function removeClosureFixtures() {
  db(`
    DELETE FROM saas_subscription_idempotency_records
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM admin_access_idempotency_records
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM saas_tenant_subscription_transition_history
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';

    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference = '${SALES_MEMBER}')
      AND permission_definition_id IN ('${ADMIN_ACCESS_READ_PERMISSION_ID}', '10000000-0000-4000-8000-000000000072');
  `);
  restoreDevSubscriptionCurrent();
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    const value = TOTP_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid Base32 character in MFA secret");
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

  get(name) {
    return this.cookies.get(name);
  }

  cookieHeader() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

async function api(method, path, body, jar) {
  const headers = { "Content-Type": "application/json", Accept: "application/json" };
  if (jar?.cookieHeader()) headers.Cookie = jar.cookieHeader();
  if (jar?.get("NevaxisFactOS.session") && jar.get("NevaxisFactOS.csrf") && method !== "GET") {
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

async function signInOwnerJar(password, mfaSecret) {
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "tenant-owner@dev.local",
    password,
    deviceLabel: `${CLOSURE_PREFIX}-ui-owner`,
  }, jar);
  if (signIn.status !== 200) fail(`owner sign-in failed: status=${signIn.status}`);
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(mfaSecret) }, jar);
  if (verify.status !== 200) fail(`owner MFA failed: status=${verify.status}`);
  return jar;
}

async function transitionSubscription(jar, targetStatus, idempotencyKey, extras = {}) {
  const current = await api("GET", "/api/saas-subscription/current", undefined, jar);
  const subscription = current.json?.subscription;
  return api("POST", "/api/saas-subscription/transitions", {
    subscriptionId: subscription?.subscriptionId ?? DEV_SUBSCRIPTION_ID,
    targetStatus,
    transitionReason: extras.transitionReason ?? "manual-suspension",
    expectedVersion: subscription?.version ?? 1,
    cancelAtPeriodEnd: extras.cancelAtPeriodEnd ?? null,
    graceUntilUtc: extras.graceUntilUtc ?? null,
    idempotencyKey,
  }, jar);
}

function assertNoSensitivePageText(pageText, label) {
  assert(`${label} no raw UUID`, !UUID_PATTERN.test(pageText));
  assert(`${label} no internal enum leak`, !INTERNAL_ENUM_PATTERN.test(pageText));
  assert(`${label} no opaque token`, !OPAQUE_TOKEN_PATTERN.test(pageText));
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

async function signInThroughForm(page, { organization, email, password }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
}

async function assertNoHorizontalOverflow(page, label) {
  const overflow = await page.evaluate(() => ({
    doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    body: document.body.scrollWidth > document.body.clientWidth + 1,
  }));
  assert(`${label} no horizontal overflow`, !overflow.doc && !overflow.body);
}

async function exerciseBillingTab(page, personaLabel, expectedStatus = "Aktif") {
  await page.goto(`${WEB}/ayarlar`, { waitUntil: "networkidle", timeout: 90000 });
  assert(`${personaLabel} ayarlar heading`, await page.getByRole("heading", { name: "Ayarlar" }).isVisible());
  await page.getByRole("tab", { name: "Üyelik ve Faturalandırma" }).click();
  await page.waitForTimeout(400);
  assert(
    `${personaLabel} billing tab selected`,
    await page.getByRole("tab", { name: "Üyelik ve Faturalandırma" }).getAttribute("aria-selected") === "true",
  );
  assert(`${personaLabel} current subscription eyebrow`, await page.getByText("MEVCUT ABONELİK").isVisible());
  assert(`${personaLabel} subscription status label`, await page.getByText(expectedStatus).first().isVisible());
  assert(`${personaLabel} billing cadence label`, await page.getByText("Aylık").first().isVisible());
  assert(`${personaLabel} development catalog note`, await page.getByText("Geliştirme kataloğu").isVisible());
  const paymentButton = page.getByRole("button", { name: DISABLED_PAYMENT_BUTTON });
  assert(`${personaLabel} disabled payment button visible`, await paymentButton.isVisible());
  assert(`${personaLabel} disabled payment button state`, await paymentButton.isDisabled());
  const pageText = await page.locator("body").innerText();
  assertNoSensitivePageText(pageText, personaLabel);
  await assertNoHorizontalOverflow(page, personaLabel);
}

async function waitForSubscriptionDeniedRoute(page) {
  await page.waitForURL(
    (url) => url.pathname === "/abonelik-erisimi",
    { timeout: 60000 },
  );
  await page.locator("#subscription-access-denied-title").waitFor({
    state: "visible",
    timeout: 60000,
  });
}

async function exerciseSubscriptionDeniedPage(page, personaLabel, expectedHeadline) {
  if (!page.url().includes("/abonelik-erisimi")) {
    await page.goto(`${WEB}/abonelik-erisimi`, { waitUntil: "networkidle", timeout: 90000 });
  } else {
    await waitForSubscriptionDeniedRoute(page);
  }
  assert(
    `${personaLabel} denial headline`,
    await page.locator("#subscription-access-denied-title").filter({ hasText: expectedHeadline }).isVisible(),
  );
  assert(`${personaLabel} denial eyebrow`, await page.getByText("ABONELİK ERİŞİMİ").isVisible());
  const pageText = await page.locator("body").innerText();
  assertNoSensitivePageText(pageText, personaLabel);
  await assertNoHorizontalOverflow(page, personaLabel);
}

async function runBrowserVerification({
  ownerPassword,
  mfaSharedSecret,
  salesPassword,
  ownerJar,
}) {
  const ready = await fetch(`${WEB}/giris`, { redirect: "manual" });
  assert("web server reachable", ready.status === 200 || ready.status === 307 || ready.status === 308, `status=${ready.status}`);

  const { chromium } = await import("playwright");
  const executablePath = resolveChromiumExecutable();
  const browser = await chromium.launch({
    headless: true,
    ...(executablePath ? { executablePath } : {}),
  });

  const consoleErrors = [];

  try {
    for (const viewport of VIEWPORTS) {
      const label = `${viewport.width}x${viewport.height}`;

      const ownerContext = await browser.newContext({ viewport });
      const ownerPage = await ownerContext.newPage();
      ownerPage.on("console", (msg) => {
        if (msg.type() === "error") consoleErrors.push(msg.text());
      });
      ownerPage.on("pageerror", (err) => consoleErrors.push(String(err)));

      await signInThroughForm(ownerPage, {
        organization: ORGANIZATION,
        email: "tenant-owner@dev.local",
        password: ownerPassword,
      });
      await ownerPage.waitForURL("**/mfa-dogrula", { timeout: 30000 });
      await ownerPage.locator("#mfa-totp").fill(generateTotp(mfaSharedSecret));
      await ownerPage.getByRole("button", { name: "Doğrula" }).click();
      await ownerPage.waitForURL("**/dashboard", { timeout: 30000 });
      await exerciseBillingTab(ownerPage, `owner active ${label}`, "Aktif");
      await ownerContext.close();

      const suspend = await transitionSubscription(ownerJar, "suspended", `${CLOSURE_PREFIX}-ui-suspend-${viewport.width}`);
      assert(`API suspend for ${label}`, suspend.status === 201 || suspend.status === 200, suspend.raw.slice(0, 160));

      const suspendedContext = await browser.newContext({ viewport });
      const suspendedPage = await suspendedContext.newPage();
      suspendedPage.on("console", (msg) => {
        if (msg.type() === "error") consoleErrors.push(msg.text());
      });
      suspendedPage.on("pageerror", (err) => consoleErrors.push(String(err)));

      await signInThroughForm(suspendedPage, {
        organization: ORGANIZATION,
        email: "tenant-owner@dev.local",
        password: ownerPassword,
      });
      await suspendedPage.waitForURL("**/mfa-dogrula", { timeout: 30000 });
      await suspendedPage.locator("#mfa-totp").fill(generateTotp(mfaSharedSecret));
      await suspendedPage.getByRole("button", { name: "Doğrula" }).click();
      await waitForSubscriptionDeniedRoute(suspendedPage);
      await exerciseSubscriptionDeniedPage(
        suspendedPage,
        `owner suspended ${label}`,
        "Uygulama erişimi şu anda kullanılamıyor",
      );
      await suspendedPage.getByRole("link", { name: "Ayarlara git" }).click();
      await suspendedPage.waitForURL("**/ayarlar", { timeout: 30000 });
      await exerciseBillingTab(suspendedPage, `owner recovery ${label}`, "Askıda");
      await suspendedContext.close();

      const reactivate = await transitionSubscription(ownerJar, "active", `${CLOSURE_PREFIX}-ui-reactivate-${viewport.width}`, {
        transitionReason: "manual-reactivation",
      });
      assert(`API reactivate for ${label}`, reactivate.status === 201 || reactivate.status === 200, reactivate.raw.slice(0, 160));

      const moduleContext = await browser.newContext({ viewport });
      const modulePage = await moduleContext.newPage();
      modulePage.on("console", (msg) => {
        if (msg.type() === "error") consoleErrors.push(msg.text());
      });
      modulePage.on("pageerror", (err) => consoleErrors.push(String(err)));

      await signInThroughForm(modulePage, {
        organization: ORGANIZATION,
        email: "machine-responsible@dev.local",
        password: salesPassword,
      });
      await modulePage.waitForURL("**/dashboard", { timeout: 30000 });
      await modulePage.goto(`${WEB}/vardiyalar`, { waitUntil: "networkidle", timeout: 90000 });
      await waitForSubscriptionDeniedRoute(modulePage);
      await exerciseSubscriptionDeniedPage(
        modulePage,
        `module denied ${label}`,
        "Bu modüle erişim aboneliğinizde tanımlı değil",
      );
      await moduleContext.close();

      const restrictedContext = await browser.newContext({ viewport });
      const restrictedPage = await restrictedContext.newPage();
      restrictedPage.on("console", (msg) => {
        if (msg.type() === "error") consoleErrors.push(msg.text());
      });
      restrictedPage.on("pageerror", (err) => consoleErrors.push(String(err)));

      await signInThroughForm(restrictedPage, {
        organization: ORGANIZATION,
        email: "materials-sales@dev.local",
        password: salesPassword,
      });
      await restrictedPage.waitForURL("**/dashboard", { timeout: 30000 });
      await restrictedPage.goto(`${WEB}/ayarlar`, { waitUntil: "networkidle", timeout: 90000 });
      const restrictedText = await restrictedPage.locator("body").innerText();
      assert(
        `restricted ${label} ayarlar denied`,
        restrictedText.includes("Erişim reddedildi") || restrictedText.includes("Bu sayfayı görüntüleme yetkiniz yok."),
        restrictedText.slice(0, 200),
      );
      assertNoSensitivePageText(restrictedText, `restricted ${label}`);
      await assertNoHorizontalOverflow(restrictedPage, `restricted ${label}`);
      await restrictedContext.close();
    }

    const hydrationLike = consoleErrors.some((line) =>
      /hydration|serialization|Text content does not match|Only plain objects/i.test(line),
    );
    assert("no hydration/serialization/console errors", !hydrationLike, consoleErrors.join(" | "));
  } finally {
    await browser.close();
  }
}

console.log("Running SAAS-SUBSCRIPTION-1 functional-ui verifier.");
removeClosureFixtures();

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
const salesPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const ownerJar = await signInOwnerJar(ownerPassword, mfaSharedSecret);

await runBrowserVerification({
  ownerPassword,
  mfaSharedSecret,
  salesPassword,
  ownerJar,
});

removeClosureFixtures();
const activeStatus = db(`SELECT subscription_status FROM saas_tenant_subscriptions WHERE id = '${DEV_SUBSCRIPTION_ID}'`);
assert("dev subscription restored active", activeStatus === "active", activeStatus);

void ROOT;
console.log("SAAS-SUBSCRIPTION-1 functional-ui verification passed.");

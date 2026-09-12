#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";
import {
  assert,
  assertNoSecretLeakage,
  CLOSURE_PREFIX,
  DEV_OPEN_INVOICE_ID,
  MOCK_API_KEY,
  MOCK_SECRET_KEY,
  previewClosureFixtures,
  readProviderMode,
  removeClosureFixtures,
  signInOwner,
  api,
  requireEnv,
} from "./saas-payments-1-verifier-lib.mjs";

const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const VIEWPORTS = [
  { width: 1600, height: 1000, label: "1600x1000" },
  { width: 1024, height: 900, label: "1024x900" },
];
const SCREENSHOT_DIR = join(tmpdir(), "NevaxisFactOS-saas-payments-1");
const CARD_NOT_CONFIGURED = "Kartla ödeme sağlayıcısı henüz yapılandırılmadı.";
const CARD_SANDBOX = "Test ortamı — gerçek tahsilat yapılmaz";
const RETURN_VERIFY_MESSAGE = "Ödeme sonucu doğrulanıyor";
const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
const INTERNAL_ENUM_PATTERN = /\b(provider-not-configured|awaiting-manual-verification|not-configured|sandbox)\b/i;
const FORBIDDEN_UI = [MOCK_SECRET_KEY, MOCK_API_KEY, "closure-token-", "cvv", "cardNumber", "secret"];

function fail(message) {
  console.error(`SAAS-PAYMENTS-1 functional-ui verification failed: ${message}`);
  process.exit(1);
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

async function signInThroughForm(page, { organization, email, password, mfaSecret }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
  await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
  await page.locator("#mfa-totp").fill(generateTotp(mfaSecret));
  await page.getByRole("button", { name: "Doğrula" }).click();
  await page.waitForURL("**/dashboard", { timeout: 30000 });
}

async function assertNoHorizontalOverflow(page, label) {
  const overflow = await page.evaluate(() => ({
    doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    body: document.body.scrollWidth > document.body.clientWidth + 1,
  }));
  assert(`${label} no horizontal overflow`, !overflow.doc && !overflow.body);
}

function assertNoSensitivePageText(pageText, label) {
  assert(`${label} no raw UUID`, !UUID_PATTERN.test(pageText));
  assert(`${label} no internal enum leak`, !INTERNAL_ENUM_PATTERN.test(pageText));
  const lower = pageText.toLowerCase();
  for (const token of FORBIDDEN_UI) {
    assert(`${label} no leakage of ${token}`, !lower.includes(token.toLowerCase()));
  }
}

async function openBillingTab(page) {
  await page.goto(`${WEB}/ayarlar`, { waitUntil: "networkidle", timeout: 90000 });
  await page.getByRole("tab", { name: "Üyelik ve Faturalandırma" }).click();
  await page.waitForTimeout(500);
  await page.locator("#saas-admin-panel-billing").waitFor({ timeout: 15000 });
}

async function runViewportValidation(page, viewportLabel, mode, returnUrl) {
  await openBillingTab(page);
  const panelText = await page.locator("#saas-admin-panel-billing").innerText();
  assertNoSensitivePageText(panelText, viewportLabel);
  assert(`${viewportLabel} billing tab visible`, panelText.includes("Fatura kayıtları"));
  assert(`${viewportLabel} payment methods section`, panelText.includes("Ödeme yöntemleri"));

  if (mode === "not-configured") {
    assert(`${viewportLabel} not-configured message`, panelText.includes(CARD_NOT_CONFIGURED));
    const payButton = page.getByRole("button", { name: CARD_NOT_CONFIGURED });
    assert(`${viewportLabel} checkout button disabled`, await payButton.isDisabled());
    assert(`${viewportLabel} no card form`, (await page.locator("input[name*='card'], input[autocomplete='cc-number']").count()) === 0);
  } else {
    assert(`${viewportLabel} sandbox notice`, panelText.includes(CARD_SANDBOX));
    assert(`${viewportLabel} provider test mode`, /\bTest\b/.test(panelText));
    const openInvoiceRow = page.locator(".saas-admin-billing-invoice-row").filter({ hasText: /Açık|Gecikmiş/ }).first();
    if (await openInvoiceRow.isVisible()) {
      await openInvoiceRow.click();
      await page.waitForTimeout(300);
    }
    const payButton = page.getByRole("button", { name: "Kartla öde" });
    assert(`${viewportLabel} Kartla öde visible`, await payButton.isVisible());
    assert(`${viewportLabel} Kartla öde enabled for owner`, !(await payButton.isDisabled()));
    assert(`${viewportLabel} no card form in NevaxisFactOS`, (await page.locator("input[name*='card'], input[autocomplete='cc-number']").count()) === 0);
  }

  await assertNoHorizontalOverflow(page, viewportLabel);
  const screenshotPath = join(SCREENSHOT_DIR, `billing-${mode}-${viewportLabel.replace("x", "-")}.png`);
  await page.screenshot({ path: screenshotPath, fullPage: true });
  console.log(`✓ screenshot saved: ${screenshotPath}`);

  if (returnUrl) {
    await page.goto(returnUrl, { waitUntil: "networkidle", timeout: 90000 });
    const returnText = await page.locator("body").innerText();
    assert(`${viewportLabel} return verification message`, returnText.includes(RETURN_VERIFY_MESSAGE) || returnText.includes("Ödeme sonucu doğrulandı") || returnText.includes("İşlem tamamlanamadı"));
    assertNoSensitivePageText(returnText, `${viewportLabel} return`);
    assertNoSecretLeakage(returnText);
    const returnShot = join(SCREENSHOT_DIR, `return-${mode}-${viewportLabel.replace("x", "-")}.png`);
    await page.screenshot({ path: returnShot, fullPage: true });
    console.log(`✓ screenshot saved: ${returnShot}`);
  }
}

async function runReturnPageValidation({ ownerPassword, mfaSecret, returnUrl }) {
  const { chromium } = await import("playwright");
  const browser = await chromium.launch({
    headless: true,
    ...(resolveChromiumExecutable() ? { executablePath: resolveChromiumExecutable() } : {}),
  });
  try {
    const context = await browser.newContext({ viewport: { width: 1600, height: 1000 } });
    const page = await context.newPage();
    await signInThroughForm(page, {
      organization: ORGANIZATION,
      email: "tenant-owner@dev.local",
      password: ownerPassword,
      mfaSecret,
    });
    await page.goto(returnUrl, { waitUntil: "networkidle", timeout: 90000 });
    const returnText = await page.locator("body").innerText();
    assert("return verification message", returnText.includes(RETURN_VERIFY_MESSAGE) || returnText.includes("Ödeme sonucu doğrulandı") || returnText.includes("İşlem tamamlanamadı"));
    assertNoSensitivePageText(returnText, "return 1600x1000");
    assertNoSecretLeakage(returnText);
    const returnShot = join(SCREENSHOT_DIR, "return-sandbox-1600-1000.png");
    await page.screenshot({ path: returnShot, fullPage: true });
    console.log(`✓ screenshot saved: ${returnShot}`);
    await context.close();
  } finally {
    await browser.close();
  }
}

async function runBrowserValidation({ mode, ownerPassword, mfaSecret, returnUrl }) {
  const ready = await fetch(`${WEB}/giris`, { redirect: "manual" });
  assert("web server reachable", [200, 307, 308].includes(ready.status), `status=${ready.status}`);

  const { chromium } = await import("playwright");
  const browser = await chromium.launch({
    headless: true,
    ...(resolveChromiumExecutable() ? { executablePath: resolveChromiumExecutable() } : {}),
  });
  const consoleErrors = [];

  try {
    for (const viewport of VIEWPORTS) {
      const context = await browser.newContext({ viewport: { width: viewport.width, height: viewport.height } });
      const page = await context.newPage();
      page.on("console", (msg) => {
        if (msg.type() === "error") consoleErrors.push(msg.text());
      });
      page.on("pageerror", (err) => consoleErrors.push(String(err)));

      await signInThroughForm(page, {
        organization: ORGANIZATION,
        email: "tenant-owner@dev.local",
        password: ownerPassword,
        mfaSecret,
      });

      await runViewportValidation(page, viewport.label, mode, viewport.width === 1600 ? returnUrl : null);
      await context.close();
    }

    const hydrationLike = consoleErrors.some((line) =>
      /hydration|serialization|Text content does not match|Only plain objects/i.test(line),
    );
    assert("no hydration/serialization/console errors", !hydrationLike, consoleErrors.join(" | "));
  } finally {
    await browser.close();
  }
}

console.log("Running SAAS-PAYMENTS-1 functional-ui verifier.");
mkdirSync(SCREENSHOT_DIR, { recursive: true });
previewClosureFixtures();
removeClosureFixtures();

requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

const owner = await signInOwner();
const workspace = await api("GET", "/api/saas-billing/workspace", undefined, owner);
assert("billing workspace loads", workspace.status === 200, `status=${workspace.status}`);
assertNoSecretLeakage(workspace.raw);

const mode = await readProviderMode(owner);
assert("provider mode readable", mode === "not-configured" || mode === "sandbox", `mode=${mode}`);

let returnUrl = null;
if (mode === "sandbox") {
  await runBrowserValidation({ mode, ownerPassword, mfaSecret, returnUrl: null });

  const invoice = await api("GET", `/api/saas-billing/invoices/${DEV_OPEN_INVOICE_ID}`, undefined, owner);
  const init = await api("POST", "/api/saas-billing/payments/checkout/initialize", {
    invoiceId: DEV_OPEN_INVOICE_ID,
    invoiceExpectedVersion: invoice.json?.version ?? 1,
    idempotencyKey: `${CLOSURE_PREFIX}-ui-init`,
  }, owner);
  assert("hosted checkout initiation", [200, 201].includes(init.status), `status=${init.status}`);
  assert("hosted checkout url present", Boolean(init.json?.hostedCheckoutUrl));
  assertNoSecretLeakage(init.raw);
  const hostedUrl = init.json?.hostedCheckoutUrl ?? "";
  const tokenMatch = hostedUrl.match(/[?&]token=([^&]+)/);
  const token = tokenMatch ? decodeURIComponent(tokenMatch[1]) : (init.json?.checkoutSessionToken ?? init.json?.token);
  const paymentAttemptId = init.json?.paymentAttemptId;
  assert("payment attempt id present", Boolean(paymentAttemptId));
  returnUrl = `${WEB}/ayarlar/faturalandirma/odeme-donusu?paymentAttemptId=${encodeURIComponent(paymentAttemptId)}&token=${encodeURIComponent(token ?? "closure-ui-token")}`;
  await runReturnPageValidation({ ownerPassword, mfaSecret, returnUrl });
} else {
  await runBrowserValidation({ mode, ownerPassword, mfaSecret, returnUrl: null });
}

removeClosureFixtures();
const remaining = previewClosureFixtures();
assert("fixture cleanup sessions", remaining.sessions === 0, `count=${remaining.sessions}`);
assert("fixture cleanup events", remaining.events === 0, `count=${remaining.events}`);
assert("fixture cleanup payments", remaining.payments === 0, `count=${remaining.payments}`);

console.log(`SAAS-PAYMENTS-1 functional-ui verifier passed. Screenshots: ${SCREENSHOT_DIR}`);

#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-BILLING-1-CLOSURE";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const SUBSCRIPTION_ID = "bb000002-00e0-4000-8000-0000000000e0";
const DEV_BILLING_ACCOUNT_ID = "bb000003-00a0-4000-8000-0000000000a0";
const DEV_DRAFT_INVOICE_ID = "bb000003-00b0-4000-8000-0000000000b0";
const DEV_OPEN_INVOICE_ID = "bb000003-00b1-4000-8000-0000000000b1";
const DEV_PAID_INVOICE_ID = "bb000003-00b2-4000-8000-0000000000b2";
const DEV_OVERDUE_INVOICE_ID = "bb000003-00b3-4000-8000-0000000000b3";
const DEV_PAYMENT_AWAITING_ID = "bb000003-00c0-4000-8000-0000000000c0";
const DEV_INVOICE_IDS = [
  DEV_DRAFT_INVOICE_ID,
  DEV_OPEN_INVOICE_ID,
  DEV_PAID_INVOICE_ID,
  DEV_OVERDUE_INVOICE_ID,
];
const VIEWPORTS = [
  { width: 1600, height: 1000 },
  { width: 1024, height: 900 },
];
const CARD_PROVIDER_MESSAGE = "Kartla ödeme sağlayıcısı henüz yapılandırılmadı.";
const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
const INTERNAL_ENUM_PATTERN = /\b(provider-not-configured|awaiting-manual-verification|bank-transfer|enterprise-invoice)\b/;

function fail(message) {
  console.error(`SAAS-BILLING-1 functional-ui verification failed: ${message}`);
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
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function closureInvoiceIdsSubquery() {
  const devIds = DEV_INVOICE_IDS.map((id) => `'${id}'`).join(", ");
  return `SELECT id FROM saas_billing_invoices WHERE tenant_id = '${TENANT_ID}' AND id NOT IN (${devIds})`;
}

function restoreDevPilotBilling() {
  db(`
    UPDATE saas_billing_invoices
    SET invoice_status = 'draft', issued_at_utc = NULL, due_at_utc = NULL, paid_at_utc = NULL, voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_DRAFT_INVOICE_ID}';
    UPDATE saas_billing_invoices
    SET invoice_status = 'open', issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00', due_at_utc = TIMESTAMPTZ '2026-08-25 00:00:00+00', paid_at_utc = NULL, voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_OPEN_INVOICE_ID}';
    UPDATE saas_billing_invoices
    SET invoice_status = 'paid', issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00', due_at_utc = TIMESTAMPTZ '2026-08-25 00:00:00+00', paid_at_utc = TIMESTAMPTZ '2026-08-10 00:00:00+00', voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_PAID_INVOICE_ID}';
    UPDATE saas_billing_invoices
    SET invoice_status = 'overdue', issued_at_utc = TIMESTAMPTZ '2026-08-05 00:00:00+00', due_at_utc = TIMESTAMPTZ '2026-08-01 00:00:00+00', paid_at_utc = NULL, voided_at_utc = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_OVERDUE_INVOICE_ID}';
    UPDATE saas_billing_payment_attempts
    SET payment_status = 'awaiting-manual-verification', failure_code = NULL, version = 1, updated_at_utc = NOW()
    WHERE id = '${DEV_PAYMENT_AWAITING_ID}';
  `);
}

function removeClosureFixtures() {
  const closureInvoices = closureInvoiceIdsSubquery();
  db(`
    DELETE FROM saas_billing_audit_events
    WHERE tenant_id = '${TENANT_ID}'
      AND (
        entity_reference IN (SELECT id::text FROM saas_billing_invoices WHERE id IN (${closureInvoices}))
        OR entity_reference IN (SELECT id::text FROM saas_billing_payment_attempts WHERE invoice_id IN (${closureInvoices}))
      );
    DELETE FROM saas_billing_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_billing_payment_attempts
    WHERE invoice_id IN (${closureInvoices}) OR idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_billing_invoice_lines WHERE invoice_id IN (${closureInvoices});
    DELETE FROM saas_billing_invoices WHERE id IN (${closureInvoices});
  `);
  restoreDevPilotBilling();
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
  if (jar?.get("crevafactos.session") && jar.get("crevafactos.csrf") && method !== "GET") {
    headers["X-CrevaFactOS-Csrf"] = jar.get("crevafactos.csrf");
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

function assertNoSensitivePageText(pageText, label) {
  assert(`${label} no raw UUID`, !UUID_PATTERN.test(pageText));
  assert(`${label} no internal enum leak`, !INTERNAL_ENUM_PATTERN.test(pageText));
}

async function exerciseBillingTab(page, personaLabel) {
  await page.goto(`${WEB}/ayarlar`, { waitUntil: "networkidle", timeout: 90000 });
  await page.getByRole("tab", { name: "Üyelik ve Faturalandırma" }).click();
  await page.waitForTimeout(400);
  await page.getByText("Açık faturalar").waitFor({ timeout: 15000 });
  assert(`${personaLabel} billing tab selected`, await page.getByRole("tab", { name: "Üyelik ve Faturalandırma" }).getAttribute("aria-selected") === "true");

  const pageText = await page.locator("body").innerText();
  assert(`${personaLabel} Turkish KPI Açık faturalar`, pageText.includes("Açık faturalar"));
  assert(`${personaLabel} Turkish KPI Gecikmiş faturalar`, pageText.includes("Gecikmiş faturalar"));
  assert(`${personaLabel} Turkish KPI Doğrulama bekleyen`, pageText.includes("Doğrulama bekleyen"));
  assert(`${personaLabel} invoice KPI strip`, await page.locator(".saas-admin-billing-kpi-strip").isVisible());
  assert(`${personaLabel} card disabled message`, pageText.includes(CARD_PROVIDER_MESSAGE));
  assert(`${personaLabel} Fatura kayıtları section`, pageText.includes("Fatura kayıtları"));
  assert(`${personaLabel} Ödeme yöntemleri section`, pageText.includes("Ödeme yöntemleri"));
  assertNoSensitivePageText(pageText, personaLabel);
  await assertNoHorizontalOverflow(page, personaLabel);

  const invoiceRow = page.locator(".saas-admin-billing-invoice-row").first();
  if (await invoiceRow.isVisible()) {
    await invoiceRow.click();
    await page.waitForTimeout(300);
    assert(`${personaLabel} invoice detail visible`, await page.locator(".saas-admin-billing-invoice-detail").isVisible());
  }
}

async function runBrowserVerification({ ownerPassword, mfaSharedSecret }) {
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
      const context = await browser.newContext({ viewport });
      const page = await context.newPage();
      page.on("console", (msg) => {
        if (msg.type() === "error") consoleErrors.push(msg.text());
      });
      page.on("pageerror", (err) => consoleErrors.push(String(err)));

      await signInThroughForm(page, {
        organization: ORGANIZATION,
        email: "tenant-owner@dev.local",
        password: ownerPassword,
      });
      await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
      await page.locator("#mfa-totp").fill(generateTotp(mfaSharedSecret));
      await page.getByRole("button", { name: "Doğrula" }).click();
      await page.waitForURL("**/dashboard", { timeout: 30000 });

      await exerciseBillingTab(page, `owner ${viewport.width}x${viewport.height}`);
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

console.log("Running SAAS-BILLING-1 functional-ui verifier.");
removeClosureFixtures();

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

const ownerJar = new CookieJar();
const signIn = await api("POST", "/api/identity/sign-in", {
  organizationContext: ORGANIZATION,
  email: "tenant-owner@dev.local",
  password: ownerPassword,
  deviceLabel: `${CLOSURE_PREFIX}-ui-setup`,
}, ownerJar);
if (signIn.status !== 200) fail(`owner sign-in failed: status=${signIn.status}`);
const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(mfaSharedSecret) }, ownerJar);
if (verify.status !== 200) fail(`owner MFA failed: status=${verify.status}`);

const closureDraft = await api("POST", "/api/saas-billing/invoices", {
  subscriptionId: SUBSCRIPTION_ID,
  periodStartUtc: "2026-08-01T00:00:00.000Z",
  periodEndUtc: "2026-09-01T00:00:00.000Z",
  idempotencyKey: `${CLOSURE_PREFIX}-ui-draft`,
}, ownerJar);
if (closureDraft.status !== 201) {
  fail(`could not create closure draft for UI probe: status=${closureDraft.status} ${closureDraft.raw.slice(0, 160)}`);
}

await runBrowserVerification({ ownerPassword, mfaSharedSecret });

removeClosureFixtures();
const remainingInvoices = Number(db(`SELECT COUNT(*) FROM saas_billing_invoices WHERE id IN (${closureInvoiceIdsSubquery()})`));
const remainingIdempotency = Number(db(`SELECT COUNT(*) FROM saas_billing_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
assert("closure fixtures remaining", remainingInvoices === 0 && remainingIdempotency === 0);
const accountCount = Number(db(`SELECT COUNT(*) FROM saas_tenant_billing_accounts WHERE id = '${DEV_BILLING_ACCOUNT_ID}'`));
assert("dev billing account preserved", accountCount === 1, `count=${accountCount}`);

void ROOT;
console.log("SAAS-BILLING-1 functional-ui verification passed.");

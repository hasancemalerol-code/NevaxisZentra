#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-ADMIN-1-CLOSURE";
const VIEWPORTS = [
  { width: 1600, height: 1000, label: "1600x1000" },
  { width: 1024, height: 900, label: "1024x900" },
];
const SCREENSHOT_DIR = join(tmpdir(), "NevaxisFactOS-saas-admin-ui-1-premium");

const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
const CLOSURE_PATTERN = /SAAS-ADMIN-1-CLOSURE/i;

function fail(message) {
  console.error(`SAAS-ADMIN-UI-1 premium verification failed: ${message}`);
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

function previewClosureFixtures() {
  const audits = Number(db(`SELECT COUNT(*) FROM authorization_audit_events WHERE entity_reference ILIKE 'saas-admin-1-closure%' OR actor_reference LIKE '${CLOSURE_PREFIX}%'`));
  const invitations = Number(db(`SELECT COUNT(*) FROM saas_identity_invitations WHERE intended_email_normalized LIKE '${CLOSURE_PREFIX.toLowerCase()}%'`));
  const sessions = Number(db(`SELECT COUNT(*) FROM saas_identity_sessions WHERE device_label LIKE '${CLOSURE_PREFIX}%'`));
  console.log(`Preview ${CLOSURE_PREFIX} fixtures: audits=${audits}, invitations=${invitations}, sessions=${sessions}`);
  return { audits, invitations, sessions };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM tenant_membership_roles
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM facility_memberships
    WHERE tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM saas_identity_sessions
    WHERE device_label LIKE '${CLOSURE_PREFIX}%'
       OR tenant_membership_id IN (SELECT id FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM admin_access_idempotency_records WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM authorization_audit_events
    WHERE entity_reference ILIKE 'saas-admin-1-closure%' OR actor_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM saas_identity_invitations
    WHERE intended_email_normalized LIKE '${CLOSURE_PREFIX.toLowerCase()}%';
    DELETE FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM identity_subjects WHERE external_subject LIKE '${CLOSURE_PREFIX}%';
  `);
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

function assertNoSensitivePageText(pageText, label) {
  assert(`${label} no raw UUID`, !UUID_PATTERN.test(pageText));
  assert(`${label} no closure fixture names`, !CLOSURE_PATTERN.test(pageText));
}

async function assertNoHorizontalOverflow(page, label) {
  const overflow = await page.evaluate(() => ({
    doc: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    body: document.body.scrollWidth > document.body.clientWidth + 1,
  }));
  assert(`${label} no horizontal overflow`, !overflow.doc && !overflow.body);
}

async function exercisePremiumFlows(page, personaLabel, viewportLabel) {
  await page.goto(`${WEB}/ayarlar`, { waitUntil: "networkidle", timeout: 90000 });
  assert(`${personaLabel} executive eyebrow`, await page.getByText("YÖNETİM VE GÜVENLİK").isVisible());
  assert(`${personaLabel} executive title`, await page.getByRole("heading", { name: "Ayarlar" }).isVisible());
  assert(`${personaLabel} invite action`, await page.getByRole("button", { name: "Kullanıcı davet et" }).isVisible());
  assert(`${personaLabel} KPI strip`, await page.locator(".saas-admin-executive__kpis").isVisible());
  assert(`${personaLabel} tab rail`, await page.locator(".saas-admin-tab-rail").isVisible());

  let pageText = await page.locator("body").innerText();
  assertNoSensitivePageText(pageText, personaLabel);
  await assertNoHorizontalOverflow(page, `${personaLabel} overview`);

  await page.screenshot({ path: join(SCREENSHOT_DIR, `${viewportLabel}-overview.png`), fullPage: true });

  await page.getByRole("tab", { name: "Kullanıcılar" }).click();
  await page.waitForTimeout(300);
  assert(`${personaLabel} user search`, await page.getByLabel("Kullanıcı ara").isVisible());
  assert(`${personaLabel} pending invitations section`, await page.getByLabel("Bekleyen davetler").or(page.getByText("Bekleyen davet yok.")).first().isVisible());

  const firstRow = page.locator(".saas-admin-registry-row").first();
  if (await firstRow.isVisible()) {
    await firstRow.click();
    assert(`${personaLabel} user detail panel`, await page.locator("aside.saas-admin-detail-panel").isVisible());
    await page.screenshot({ path: join(SCREENSHOT_DIR, `${viewportLabel}-user-detail.png`), fullPage: true });
    await page.getByRole("button", { name: "Kullanıcı detayını kapat" }).click();
    await page.waitForTimeout(200);
    assert(`${personaLabel} detail closed`, !(await page.locator("aside.saas-admin-detail-panel").isVisible()));
  }

  await page.getByRole("button", { name: "Kullanıcı davet et" }).click();
  assert(`${personaLabel} invite dialog`, await page.getByRole("dialog").isVisible());
  await page.screenshot({ path: join(SCREENSHOT_DIR, `${viewportLabel}-invite-dialog.png`), fullPage: true });
  await page.getByRole("button", { name: "Vazgeç" }).click();

  await page.getByRole("tab", { name: "Roller ve Yetkiler" }).click();
  await page.waitForTimeout(300);
  const roleRow = page.locator(".saas-admin-registry-row").first();
  if (await roleRow.isVisible()) {
    await roleRow.click();
    await page.getByRole("button", { name: "Yetki matrisi" }).click();
    assert(`${personaLabel} role matrix dialog`, await page.getByRole("dialog").isVisible());
    await page.screenshot({ path: join(SCREENSHOT_DIR, `${viewportLabel}-role-matrix.png`), fullPage: true });
    await page.getByRole("button", { name: "Kapat" }).click();
  }

  for (const tab of ["Tesis Erişimi", "Oturumlar ve Güvenlik", "Denetim Kayıtları", "Üyelik ve Faturalandırma"]) {
    await page.getByRole("tab", { name: tab }).click();
    await page.waitForTimeout(250);
    await assertNoHorizontalOverflow(page, `${personaLabel} ${tab}`);
    pageText = await page.locator("body").innerText();
    assertNoSensitivePageText(pageText, `${personaLabel} ${tab}`);
  }

  await page.screenshot({ path: join(SCREENSHOT_DIR, `${viewportLabel}-billing.png`), fullPage: true });
}

console.log("Running SAAS-ADMIN-UI-1 premium verifier.");
mkdirSync(SCREENSHOT_DIR, { recursive: true });
console.log(`Screenshots directory (outside repository): ${SCREENSHOT_DIR}`);

const before = previewClosureFixtures();
removeClosureFixtures();
const afterCleanup = previewClosureFixtures();
assert("closure fixtures removed", afterCleanup.audits === 0 && afterCleanup.invitations === 0 && afterCleanup.sessions === 0,
  `audits=${afterCleanup.audits}, invitations=${afterCleanup.invitations}, sessions=${afterCleanup.sessions}`);
void before;

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

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
    const context = await browser.newContext({ viewport: { width: viewport.width, height: viewport.height } });
    const page = await context.newPage();
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    page.on("pageerror", (err) => consoleErrors.push(String(err)));

    await signInThroughForm(page, {
      organization: ORGANIZATION,
      email: "admin@demo.nevaxisfactos.local",
      password: ownerPassword,
    });
    await page.waitForURL("**/mfa-dogrula", { timeout: 30000 });
    await page.locator("#mfa-totp").fill(generateTotp(mfaSharedSecret));
    await page.getByRole("button", { name: "Doğrula" }).click();
    await page.waitForURL("**/dashboard", { timeout: 30000 });

    await exercisePremiumFlows(page, `owner ${viewport.label}`, viewport.label);
    await context.close();
  }

  const hydrationLike = consoleErrors.some((line) =>
    /hydration|serialization|Text content does not match|Only plain objects/i.test(line),
  );
  assert("no hydration/serialization/console errors", !hydrationLike, consoleErrors.join(" | "));
} finally {
  await browser.close();
}

void ROOT;
console.log("SAAS-ADMIN-UI-1 premium verification passed.");

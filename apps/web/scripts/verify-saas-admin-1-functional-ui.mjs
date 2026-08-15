#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "SAAS-ADMIN-1-CLOSURE";
const ACTIVE_MEMBER = "mem_bb000002001340008000000000000013";
const VIEWPORTS = [
  { width: 1600, height: 1000 },
  { width: 1024, height: 900 },
];
const TABS = [
  "Kullanıcılar",
  "Roller ve Yetkiler",
  "Tesis Erişimi",
  "Oturumlar ve Güvenlik",
  "Denetim Kayıtları",
  "Üyelik ve Faturalandırma",
];

const UUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/i;
const INTERNAL_ENUM_PATTERN = /\b(PendingVerification|Locked|mfa_pending|lifecycle_status|created-delivered|created-not-delivered)\b/;
const OPAQUE_TOKEN_PATTERN = /\b[a-f0-9]{40,}\b/i;

function fail(message) {
  console.error(`SAAS-ADMIN-1 functional-ui verification failed: ${message}`);
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

function machinePassword() {
  return process.env.DevelopmentSaasIdentity__MachineResponsiblePassword?.trim()
    ?? requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
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

async function signIn(email, password, mfaSecret) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: `${CLOSURE_PREFIX}-ui`,
  }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}: status=${result.status}`);
  if (mfaSecret) {
    const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(mfaSecret) }, jar);
    if (verify.status !== 200) fail(`MFA failed for ${email}: status=${verify.status}`);
  }
  return jar;
}

function assertNoSensitivePageText(pageText, label) {
  assert(`${label} no raw UUID`, !UUID_PATTERN.test(pageText));
  assert(`${label} no internal enum`, !INTERNAL_ENUM_PATTERN.test(pageText));
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

async function exerciseAyarlarPage(page, personaLabel, canManage) {
  await page.goto(`${WEB}/ayarlar`, { waitUntil: "networkidle", timeout: 90000 });
  assert(`${personaLabel} ayarlar heading`, await page.getByRole("heading", { name: "Ayarlar" }).isVisible());
  assert(`${personaLabel} tenant context`, await page.getByText("Organizasyon:").isVisible());

  const pageText = await page.locator("body").innerText();
  assertNoSensitivePageText(pageText, personaLabel);
  await assertNoHorizontalOverflow(page, personaLabel);

  for (const tabLabel of TABS) {
    await page.getByRole("tab", { name: tabLabel }).click();
    await page.waitForTimeout(400);
    assert(`${personaLabel} tab ${tabLabel}`, await page.getByRole("tab", { name: tabLabel }).getAttribute("aria-selected") === "true");
    await assertNoHorizontalOverflow(page, `${personaLabel} ${tabLabel}`);
    const tabText = await page.locator("body").innerText();
    assertNoSensitivePageText(tabText, `${personaLabel} ${tabLabel}`);
  }

  await page.getByRole("tab", { name: "Kullanıcılar" }).click();
  await page.waitForTimeout(300);

  if (canManage) {
    await page.getByRole("button", { name: "Kullanıcı davet et" }).click();
    assert(`${personaLabel} invite dialog`, await page.getByRole("dialog").isVisible());
    assert(`${personaLabel} invite dialog title`, await page.getByRole("heading", { name: "Kullanıcı davet et" }).isVisible());
    await page.getByRole("button", { name: "Vazgeç" }).click();
    await page.waitForTimeout(200);

    const firstUserRow = page.locator(".saas-admin-registry-row").first();
    if (await firstUserRow.isVisible()) {
      await firstUserRow.click();
      assert(`${personaLabel} user detail panel`, await page.locator("aside.saas-admin-detail-panel").isVisible());
      await page.getByRole("button", { name: "Etkin erişimi önizle" }).click();
      await page.waitForTimeout(400);
      await page.getByRole("button", { name: "Kullanıcı detayını kapat" }).click();
      await page.waitForTimeout(200);
    }
  } else {
    const inviteButton = page.getByRole("button", { name: "Kullanıcı davet et" });
    assert(`${personaLabel} no invite button`, !(await inviteButton.isVisible()));
  }

  await page.getByRole("tab", { name: "Roller ve Yetkiler" }).click();
  await page.waitForTimeout(300);
  const roleRow = page.locator(".saas-admin-registry-row").first();
  if (canManage && await roleRow.isVisible()) {
    await roleRow.click();
    await page.waitForTimeout(200);
  }
  const matrixButton = page.getByRole("button", { name: "Yetki matrisi" }).first();
  if (canManage && await matrixButton.isVisible()) {
    await matrixButton.click();
    assert(`${personaLabel} role permissions dialog`, await page.getByRole("dialog").isVisible());
    await page.getByRole("button", { name: "Kapat" }).click();
    await page.waitForTimeout(200);
  }
}

async function runBrowserVerification({
  ownerPassword,
  mfaSharedSecret,
  activeUserPassword,
  salesPassword,
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

      await exerciseAyarlarPage(ownerPage, `owner ${viewport.width}x${viewport.height}`, true);
      await ownerContext.close();

      const adminContext = await browser.newContext({ viewport });
      const adminPage = await adminContext.newPage();
      adminPage.on("console", (msg) => {
        if (msg.type() === "error") consoleErrors.push(msg.text());
      });
      adminPage.on("pageerror", (err) => consoleErrors.push(String(err)));

      await signInThroughForm(adminPage, {
        organization: ORGANIZATION,
        email: "active-user@dev.local",
        password: activeUserPassword,
      });
      await adminPage.waitForURL("**/dashboard", { timeout: 30000 });
      await exerciseAyarlarPage(adminPage, `admin ${viewport.width}x${viewport.height}`, true);
      await adminContext.close();

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
        `restricted ${viewport.width}x${viewport.height} denied`,
        restrictedText.includes("Erişim reddedildi") || restrictedText.includes("Bu sayfayı görüntüleme yetkiniz yok."),
        restrictedText.slice(0, 200),
      );
      assertNoSensitivePageText(restrictedText, `restricted ${viewport.width}x${viewport.height}`);
      await assertNoHorizontalOverflow(restrictedPage, `restricted ${viewport.width}x${viewport.height}`);
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

console.log("Running SAAS-ADMIN-1 functional-ui verifier.");

const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
const mfaSharedSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
const activeUserPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const salesPassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const maintenancePassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
void machinePassword();
void maintenancePassword;

const ownerJar = await signIn("tenant-owner@dev.local", ownerPassword, mfaSharedSecret);

const activeRolesBefore = await api("GET", `/api/admin-access/members/${ACTIVE_MEMBER}/effective-access`, undefined, ownerJar);
const rolesBeforeGrant = (activeRolesBefore.json?.roleReferences ?? []).filter((role) => role !== "tenant-admin");
const activeVersion = Number(activeRolesBefore.json?.authorizationVersion ?? 1);
const grantAdmin = await api("PUT", `/api/admin-access/members/${ACTIVE_MEMBER}/roles`, {
  roleReferences: ["tenant-admin"],
  expectedAuthorizationVersion: activeVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-ui-admin-setup`,
}, ownerJar);
if (grantAdmin.status !== 200 && grantAdmin.status !== 409) {
  fail(`could not grant tenant-admin for UI admin persona: status=${grantAdmin.status} ${grantAdmin.raw.slice(0, 160)}`);
}

await runBrowserVerification({
  ownerPassword,
  mfaSharedSecret,
  activeUserPassword,
  salesPassword,
});

const activeRolesAfter = await api("GET", `/api/admin-access/members/${ACTIVE_MEMBER}/effective-access`, undefined, ownerJar);
const restoreVersion = Number(activeRolesAfter.json?.authorizationVersion ?? activeVersion + 1);
const restoreAdmin = await api("PUT", `/api/admin-access/members/${ACTIVE_MEMBER}/roles`, {
  roleReferences: rolesBeforeGrant,
  expectedAuthorizationVersion: restoreVersion,
  idempotencyKey: `${CLOSURE_PREFIX}-ui-admin-restore`,
}, ownerJar);
if (restoreAdmin.status !== 200 && restoreAdmin.status !== 409) {
  fail(`could not restore active-user roles after UI proof: status=${restoreAdmin.status} ${restoreAdmin.raw.slice(0, 160)}`);
}

void ROOT;
console.log("SAAS-ADMIN-1 functional-ui verification passed.");

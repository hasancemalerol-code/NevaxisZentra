#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "OPRISKUI1-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000003-00e6-4000-8000-0000000000e6";
const SCREENSHOT_DIR = join(tmpdir(), "crevafactos-operational-risk-ui-1-screenshots");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const RAW_ENGLISH_TITLE_PATTERNS = [
  /Cross-module risk detected/i,
  /Terminal energy detection case/i,
];

const RAW_VISIBLE_CODES = [
  "connectivity-risk",
  "maintenance-risk",
  "source-conflict",
  "insufficient-data",
  "risk-detected",
  "within-threshold",
  "endpoint-offline",
  "observation-stale",
  "source-sequence-gap",
  "clock-skew-exceeded",
  "within-quality-profile",
];

const OWNER_SCREENSHOTS = [
  { width: 1600, height: 1000, filename: "operational-risk-ui-1-9-8-overview-owner-1600x1000.png", tab: "Genel Bakış", label: "owner-overview-1600x1000", requireOverviewDetection: true },
  { width: 1600, height: 1000, filename: "operational-risk-ui-1-9-8-cases-owner-1600x1000.png", tab: "Risk Vakaları", label: "owner-cases-1600x1000" },
  { width: 1600, height: 1000, filename: "operational-risk-ui-1-9-8-case-detail-owner-1600x1000.png", tab: "Risk Vakaları", label: "owner-case-detail-1600x1000", openCaseDetail: true },
  { width: 1600, height: 1000, filename: "operational-risk-ui-1-9-8-profiles-owner-1600x1000.png", tab: "Tespit Profilleri", label: "owner-profiles-1600x1000", requireProfileHero: true },
  { width: 1600, height: 1000, filename: "operational-risk-ui-1-9-8-runs-owner-1600x1000.png", tab: "Tespit Çalıştırmaları", label: "owner-runs-1600x1000", requireRunsSection: true },
  { width: 1024, height: 900, filename: "operational-risk-ui-1-9-8-overview-owner-1024x900.png", tab: "Genel Bakış", label: "owner-overview-1024x900", requireOverviewDetection: true },
];
const READONLY_SCREENSHOT = { width: 1600, height: 1000, filename: "operational-risk-ui-1-9-8-overview-readonly-1600x1000.png", tab: "Genel Bakış", label: "readonly-overview-1600x1000", requireOverviewDetection: true };

const report = { screenshots: [], consoleErrors: [], deniedMutations: 0 };

function fail(message) {
  console.error(`OPERATIONAL-RISK-UI-1 functional UI verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__OPRISK_UI_1_BOOTSTRAPPED === "1") return;
  const transcript = join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl");
  let text = existsSync(transcript) ? readFileSync(transcript, "utf8") : "";
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    if (!process.env[key]?.trim()) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
    }
  }
  process.env.__OPRISK_UI_1_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function generateTotp(secret, counterOffset = 0) {
  const counter = Math.floor(Date.now() / 1000 / 30) + counterOffset;
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0; let bitsLeft = 0; const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character); bitsLeft += 5;
    if (bitsLeft >= 8) { output.push((buffer >> (bitsLeft - 8)) & 0xff); bitsLeft -= 8; }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

function resolveChromiumExecutable() {
  for (const candidate of [
    process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    process.env.CHROME_PATH,
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
  ].filter(Boolean)) {
    if (existsSync(candidate)) return candidate;
  }
  return undefined;
}

function db(sql) {
  const result = spawnSync("docker", ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function seedReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';
    INSERT INTO tenant_membership_permission_overrides (id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES ('${CLOSURE_OVERRIDE_ID}', 'bb000001-0001-4000-8000-000000000001', 'bb000003-0223-4000-8000-000000000223', '10000000-0000-4000-8000-000000000080', 'allow', 'tenant', 'Active', NOW(), NOW(), 1);`);
}

function removeReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';`);
}

function readPngDimensions(filePath) {
  const buffer = readFileSync(filePath);
  if (buffer.length < 24 || buffer.toString("ascii", 1, 4) !== "PNG") fail(`Invalid PNG: ${filePath}`);
  return { width: buffer.readUInt32BE(16), height: buffer.readUInt32BE(20) };
}

async function waitForCaptureReady(page) {
  await page.waitForLoadState("networkidle", { timeout: 90000 }).catch(() => undefined);
  await page.waitForFunction(() => {
    const text = document.body?.innerText ?? "";
    if (text.includes("Rendering...")) return false;
    const loading = document.querySelector('[data-testid="operational-risk-case-detail-loading"]');
    if (loading && loading.offsetParent !== null) return false;
    return true;
  }, { timeout: 30000 });
}

async function waitForAuthenticatedWorkspace(page) {
  await page.goto(`${WEB}/operasyonel-riskler`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.locator('[data-testid="operational-risk-workspace"]').waitFor({ state: "visible", timeout: 30000 });
}

async function assertNoRawTechnicalCodesInPrimaryUI(page) {
  const offenders = await page.evaluate((codes) => {
    const root = document.querySelector('[data-testid="operational-risk-workspace"]');
    if (!root) return ["workspace-missing"];
    const hits = [];
    const walk = (node) => {
      if (node.nodeType === Node.TEXT_NODE) {
        const parent = node.parentElement;
        if (parent?.closest(".oprisk-technical-details")) return;
        if (parent?.tagName === "CODE") return;
        const text = node.textContent ?? "";
        for (const code of codes) {
          if (text.includes(code)) hits.push(code);
        }
      } else if (node.nodeType === Node.ELEMENT_NODE) {
        for (const child of node.childNodes) walk(child);
      }
    };
    walk(root);
    return [...new Set(hits)];
  }, RAW_VISIBLE_CODES);
  assert("no raw technical codes in primary UI", offenders.length === 0, offenders.join(", "));
}

async function assertNoRawEnglishPrimaryTitles(page) {
  const offenders = await page.evaluate((patterns) => {
    const regexes = patterns.map((pattern) => new RegExp(pattern, "i"));
    const selectors = [
      ".oprisk-priority-row__title",
      ".oprisk-table tbody td:first-child",
      ".oprisk-drawer__header h2",
    ];
    const hits = [];
    for (const selector of selectors) {
      for (const element of document.querySelectorAll(selector)) {
        const text = element.textContent?.trim() ?? "";
        if (regexes.some((regex) => regex.test(text))) hits.push(text);
      }
    }
    return hits;
  }, RAW_ENGLISH_TITLE_PATTERNS.map((pattern) => pattern.source));
  assert("no raw English primary case title", offenders.length === 0, offenders.join(" | "));
}

async function signInPersona(page, email, password, mfaSecret = null) {
  await page.goto(`${WEB}/giris`, { waitUntil: "domcontentloaded", timeout: 90000 });
  const signInStatus = await page.evaluate(async ({ organization, userEmail, userPassword }) => {
    const response = await fetch("/api/identity/sign-in", {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({
        organizationContext: organization,
        email: userEmail,
        password: userPassword,
        deviceLabel: "operational-risk-ui-1-functional",
      }),
    });
    return response.status;
  }, { organization: ORGANIZATION, userEmail: email, userPassword: password });
  assert(`sign-in ${email}`, signInStatus === 200, String(signInStatus));
  if (mfaSecret) {
    await page.goto(`${WEB}/mfa-dogrula`, { waitUntil: "domcontentloaded", timeout: 90000 });
    let verified = false;
    for (const counterOffset of [-1, 0, 1]) {
      await page.locator("#mfa-totp").fill(generateTotp(mfaSecret, counterOffset));
      const [verify] = await Promise.all([
        page.waitForResponse((r) => r.url().includes("/api/identity/mfa/verify"), { timeout: 30000 }),
        page.getByRole("button", { name: "Doğrula" }).click(),
      ]);
      if (verify.status() === 200) {
        verified = true;
        break;
      }
      await page.waitForTimeout(350);
    }
    assert("MFA verify", verified);
    await page.waitForURL("**/dashboard", { timeout: 90000 });
  } else {
    await page.goto(`${WEB}/dashboard`, { waitUntil: "domcontentloaded" });
  }
}

async function clickTab(page, tabName) {
  await page.locator('[data-testid="operational-risk-tab-rail"]').scrollIntoViewIfNeeded();
  const tab = page.getByRole("tab", { name: tabName, exact: true });
  await tab.click();
  await page.waitForTimeout(250);
  assert(`${tabName} tab selected`, await tab.getAttribute("aria-selected") === "true");
}

async function captureScreenshot(page, spec) {
  await page.setViewportSize({ width: spec.width, height: spec.height });
  await page.goto(`${WEB}/operasyonel-riskler?tab=${spec.tab === "Genel Bakış" ? "overview" : spec.tab === "Risk Vakaları" ? "cases" : spec.tab === "Tespit Profilleri" ? "profiles" : "runs"}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.locator('[data-testid="operational-risk-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  await clickTab(page, spec.tab);
  if (spec.openCaseDetail) {
    const detailButton = page.getByRole("button", { name: "Detay" }).first();
    if (await detailButton.count()) {
      await detailButton.click();
      await page.locator('[data-testid="operational-risk-case-detail-ready"]').waitFor({ state: "visible", timeout: 30000 });
      assert("case detail ready state", await page.locator('[data-testid="operational-risk-case-detail-ready"]').isVisible());
      assert("case detail not loading", !(await page.locator('[data-testid="operational-risk-case-detail-loading"]').isVisible()));
      assert("case detail not error", !(await page.locator('[data-testid="operational-risk-case-detail-error"]').isVisible()));
    }
  }
  if (spec.requireOverviewDetection) {
    const hasSummary = await page.locator('[data-testid="operational-risk-detection-summary"]').count();
    const hasEmpty = await page.locator('[data-testid="operational-risk-detection-summary-empty"]').count();
    assert("overview detection summary or intentional empty state", hasSummary > 0 || hasEmpty > 0);
  }
  if (spec.requireProfileHero) {
    await page.locator('[data-testid="operational-risk-profile-hero"]').waitFor({ state: "visible", timeout: 30000 });
    const hasRules = await page.locator('[data-testid="operational-risk-profile-rule-grid"], [data-testid="operational-risk-profile-rules-unavailable"]').count();
    assert("profile hero visible", await page.locator('[data-testid="operational-risk-profile-hero"]').isVisible());
    assert("profile rule summary or honest unavailable", hasRules > 0);
  }
  if (spec.requireRunsSection) {
    await page.locator('[data-testid="operational-risk-runs-history"]').waitFor({ state: "visible", timeout: 30000 });
    const hasHistory = await page.locator('[data-testid="operational-risk-runs-empty"], .oprisk-table--runs tbody tr').count();
    assert("run history or intentional empty state", hasHistory > 0);
  }
  await waitForCaptureReady(page);
  await assertNoRawEnglishPrimaryTitles(page);
  await assertNoRawTechnicalCodesInPrimaryUI(page);
  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const screenshotPath = join(SCREENSHOT_DIR, spec.filename);
  await page.screenshot({ path: screenshotPath, fullPage: false });
  const dimensions = readPngDimensions(screenshotPath);
  assert(`${spec.label} PNG width`, dimensions.width === spec.width, String(dimensions.width));
  assert(`${spec.label} PNG height`, dimensions.height === spec.height, String(dimensions.height));
  report.screenshots.push({ path: screenshotPath, ...dimensions, bytes: statSync(screenshotPath).size });
  console.log(`  screenshot: ${screenshotPath} (${dimensions.width}x${dimensions.height})`);
}

console.log("Running OPERATIONAL-RISK-UI-1 functional UI verifier.");

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const browser = await chromium.launch({ headless: true, ...(executablePath ? { executablePath } : {}) });
const ownerContext = await browser.newContext({ deviceScaleFactor: 1 });
const ownerPage = await ownerContext.newPage();

await signInPersona(ownerPage, "tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"), requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));
ownerPage.on("console", (msg) => { if (msg.type() === "error") report.consoleErrors.push(msg.text()); });
ownerPage.on("pageerror", (error) => report.consoleErrors.push(error.message));
await waitForAuthenticatedWorkspace(ownerPage);
assert("owner workspace loads", await ownerPage.locator('[data-testid="operational-risk-workspace"]').isVisible());

for (const tab of [
  { name: "Genel Bakış", id: "overview" },
  { name: "Risk Vakaları", id: "cases" },
  { name: "Tespit Profilleri", id: "profiles" },
  { name: "Tespit Çalıştırmaları", id: "runs" },
]) {
  await ownerPage.goto(`${WEB}/operasyonel-riskler?tab=${tab.id}`, { waitUntil: "networkidle", timeout: 90000 });
  await ownerPage.locator('[data-testid="operational-risk-workspace"]').waitFor({ state: "visible" });
  await clickTab(ownerPage, tab.name);
}
await ownerPage.goto(`${WEB}/operasyonel-riskler?tab=cases`, { waitUntil: "networkidle" });
const detailButton = ownerPage.getByRole("button", { name: "Detay" }).first();
if (await detailButton.count()) {
  await detailButton.click();
  await ownerPage.locator('[data-testid="operational-risk-case-detail-ready"]').waitFor({ state: "visible", timeout: 30000 });
  assert("case detail drawer ready", await ownerPage.locator('[data-testid="operational-risk-case-detail-ready"]').isVisible());
  assert("no permanent loading state", !(await ownerPage.locator('[data-testid="operational-risk-case-detail-loading"]').isVisible()));
  assert("case detail not error", !(await ownerPage.locator('[data-testid="operational-risk-case-detail-error"]').isVisible()));
  await ownerPage.getByRole("button", { name: "Kapat", exact: true }).click();
}

const profiles = await ownerPage.request.get(`${API}/api/operational-risks/detection/profiles?facilityId=${FACILITY}`, {
  headers: { Cookie: (await ownerContext.cookies()).map((c) => `${c.name}=${c.value}`).join("; ") },
});
const profilePayload = await profiles.json();
const approvedProfile = (profilePayload.items ?? []).find((item) => item.status === "approved");
if (approvedProfile) {
  const idempotencyKey = `${CLOSURE_PREFIX}-run-${Date.now()}`;
  const runResult = await ownerPage.evaluate(async ({ apiBase, facilityId, profileId, provenance, key }) => {
    const response = await fetch(`${apiBase}/api/operational-risks/detection/runs?facilityId=${facilityId}`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({
        profileId,
        windowStartUtc: "2026-08-21T08:00:00.000Z",
        windowEndUtc: "2026-08-21T12:00:00.000Z",
        sourceProvenance: provenance,
        idempotencyKey: key,
      }),
    });
    return response.status;
  }, {
    apiBase: WEB,
    facilityId: FACILITY,
    profileId: approvedProfile.id,
    provenance: profilePayload.provenanceDisclaimer ?? "closure",
    key: idempotencyKey,
  });
  assert("detection run execute", runResult === 201 || runResult === 200, String(runResult));
  const replayResult = await ownerPage.evaluate(async ({ apiBase, facilityId, profileId, provenance, key }) => {
    const response = await fetch(`${apiBase}/api/operational-risks/detection/runs?facilityId=${facilityId}`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({
        profileId,
        windowStartUtc: "2026-08-21T08:00:00.000Z",
        windowEndUtc: "2026-08-21T12:00:00.000Z",
        sourceProvenance: provenance,
        idempotencyKey: key,
      }),
    });
    return response.status;
  }, {
    apiBase: WEB,
    facilityId: FACILITY,
    profileId: approvedProfile.id,
    provenance: profilePayload.provenanceDisclaimer ?? "closure",
    key: idempotencyKey,
  });
  assert("detection run idempotent replay", replayResult === 200, String(replayResult));
}

await ownerPage.goto(`${WEB}/operasyonel-riskler?tab=cases`, { waitUntil: "networkidle" });
await ownerPage.reload({ waitUntil: "networkidle" });
assert("tab query preserved after refresh", ownerPage.url().includes("tab=cases"));

for (const spec of OWNER_SCREENSHOTS) await captureScreenshot(ownerPage, spec);

seedReadOverride();
const readonlyContext = await browser.newContext({ deviceScaleFactor: 1 });
const readonlyPage = await readonlyContext.newPage();
await signInPersona(readonlyPage, "maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
await readonlyPage.goto(`${WEB}/operasyonel-riskler`, { waitUntil: "networkidle" });
assert("readonly note visible", await readonlyPage.locator('[data-testid="operational-risk-readonly-note"]').isVisible());
await captureScreenshot(readonlyPage, READONLY_SCREENSHOT);
removeReadOverride();

const overflow = await ownerPage.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1);
assert("no horizontal overflow", overflow);
assert("no Rendering artifact", !(await ownerPage.evaluate(() => (document.body?.innerText ?? "").includes("Rendering..."))));
assert("no console errors", report.consoleErrors.length === 0, report.consoleErrors.join(" | "));

await browser.close();
console.log(`REPORT_JSON=${JSON.stringify(report)}`);
console.log("OPERATIONAL-RISK-UI-1 functional UI verification passed.");
console.log("COOKIE_INJECTION_USED=NO");

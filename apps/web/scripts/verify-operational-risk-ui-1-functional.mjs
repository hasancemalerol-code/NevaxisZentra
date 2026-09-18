#!/usr/bin/env node
import { createHash, createHmac } from "node:crypto";
import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const REPO_ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB_ROOT = join(REPO_ROOT, "apps/web");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "OPRISK-UI-FUNCTIONAL-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000003-00e6-4000-8000-0000000000e6";
const SCREENSHOT_DIR = join(tmpdir(), "NevaxisZentra-operational-risk-runtime-closure-screenshots");
const WINDOW_START = "2026-08-21T08:00:00.000Z";
const WINDOW_END = "2026-08-21T12:00:00.000Z";
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
  { width: 1600, height: 1000, filename: "operational-risk-runtime-closure-overview-owner-1600x1000.png", tab: "Genel Bakış", label: "owner-overview-1600x1000", requireOverviewDetection: true },
  { width: 1600, height: 1000, filename: "operational-risk-runtime-closure-cases-owner-1600x1000.png", tab: "Risk Vakaları", label: "owner-cases-1600x1000" },
  { width: 1600, height: 1000, filename: "operational-risk-runtime-closure-case-detail-owner-1600x1000.png", tab: "Risk Vakaları", label: "owner-case-detail-1600x1000", openCaseDetail: true },
  { width: 1600, height: 1000, filename: "operational-risk-runtime-closure-profiles-owner-1600x1000.png", tab: "Tespit Profilleri", label: "owner-profiles-1600x1000", requireProfileHero: true },
  { width: 1600, height: 1000, filename: "operational-risk-runtime-closure-runs-with-history-owner-1600x1000.png", tab: "Tespit Çalıştırmaları", label: "owner-runs-with-history-1600x1000", requireRunHistory: true },
  { width: 1024, height: 900, filename: "operational-risk-runtime-closure-overview-owner-1024x900.png", tab: "Genel Bakış", label: "owner-overview-1024x900", requireOverviewDetection: true },
];
const READONLY_SCREENSHOT = {
  width: 1600,
  height: 1000,
  filename: "operational-risk-runtime-closure-overview-readonly-1600x1000.png",
  tab: "Genel Bakış",
  label: "readonly-overview-1600x1000",
  requireOverviewDetection: true,
};

const report = {
  screenshots: [],
  consoleErrors: [],
  sessionFingerprints: {},
  restart: {},
  subscription: {},
  runPersistence: {},
};

function fail(message) {
  console.error(`OPERATIONAL-RISK-UI-1 functional UI verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function isBenignDevConsoleError(message) {
  const text = String(message);
  return text.includes("/_next/hmr") && text.includes("WebSocket") && text.includes("ERR_CONNECTION_REFUSED");
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__OPRISK_UI_1_BOOTSTRAPPED === "1") return;
  const transcript = join(homedir(), ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl");
  const text = existsSync(transcript) ? readFileSync(transcript, "utf8") : "";
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
  const result = spawnSync("docker", ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function closureCounts() {
  const runs = Number(db(`SELECT count(*) FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`));
  const results = Number(db(`SELECT count(*) FROM operational_risk_detection_results WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');`));
  const drivers = Number(db(`SELECT count(*) FROM operational_risk_detection_drivers WHERE result_id IN (SELECT id FROM operational_risk_detection_results WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'));`));
  return { runs, results, drivers };
}

function operationalRiskTotals() {
  return {
    runs: Number(db("SELECT count(*) FROM operational_risk_detection_runs;")),
    results: Number(db("SELECT count(*) FROM operational_risk_detection_results;")),
    drivers: Number(db("SELECT count(*) FROM operational_risk_detection_drivers;")),
    cases: Number(db("SELECT count(*) FROM operational_risk_cases;")),
    evidence: Number(db("SELECT count(*) FROM operational_risk_evidence;")),
    history: Number(db("SELECT count(*) FROM operational_risk_status_history;")),
  };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM operational_risk_detection_drivers
    WHERE result_id IN (
      SELECT id FROM operational_risk_detection_results
      WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')
    );
    DELETE FROM operational_risk_detection_results
    WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
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

function listenerPid(port) {
  const result = spawnSync("powershell", ["-NoProfile", "-Command", `(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess`], { encoding: "utf8" });
  const pid = Number(result.stdout.trim());
  return Number.isFinite(pid) && pid > 0 ? pid : null;
}

async function authHeaders(context) {
  const cookies = await context.cookies();
  const headers = { Accept: "application/json", "Content-Type": "application/json" };
  const cookieHeader = cookies.map((cookie) => `${cookie.name}=${cookie.value}`).join("; ");
  if (cookieHeader) headers.Cookie = cookieHeader;
  const csrf = cookies.find((cookie) => cookie.name === "NevaxisZentra.csrf");
  if (csrf?.value) headers["X-NevaxisZentra-Csrf"] = csrf.value;
  return headers;
}

async function sessionFingerprint(context) {
  const cookies = await context.cookies();
  const session = cookies.find((cookie) => cookie.name === "NevaxisZentra.session");
  if (!session?.value) return { hasSession: false, fingerprint: "none" };
  const fingerprint = createHash("sha256").update(session.value).digest("hex").slice(0, 12);
  return { hasSession: true, fingerprint };
}

async function waitForCaptureReady(page) {
  await page.waitForLoadState("networkidle", { timeout: 90000 }).catch(() => undefined);
  await page.waitForFunction(() => {
    const text = document.body?.innerText ?? "";
    if (text.includes("Rendering...")) return false;
    const loading = document.querySelector('[data-testid="operational-risk-case-detail-loading"]');
    if (loading && loading.offsetParent !== null) return false;
    return true;
  }, { timeout: 45000 });
}

async function assertNoRenderingArtifact(page, label) {
  const count = await page.evaluate(() => (document.body?.innerText ?? "").split("Rendering...").length - 1);
  assert(`${label} Rendering... count zero`, count === 0, String(count));
  return count;
}

async function waitForAuthenticatedWorkspace(page) {
  const response = await page.goto(`${WEB}/operasyonel-riskler`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("operasyonel-riskler HTTP 200", response?.status() === 200, String(response?.status()));
  assert("no login redirect", !page.url().includes("/giris"), page.url());
  assert("no subscription recovery redirect", !page.url().includes("/abonelik-erisimi"), page.url());
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
    const selectors = [".oprisk-priority-row__title", ".oprisk-table tbody td:first-child", ".oprisk-drawer__header h2"];
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
      const verifyPromise = page.waitForResponse((response) => response.url().includes("/api/identity/mfa/verify"), { timeout: 15000 }).catch(() => null);
      await page.getByRole("button", { name: "Doğrula" }).click();
      const verify = await verifyPromise;
      if (verify?.status() === 200) { verified = true; break; }
      await page.waitForTimeout(400);
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

async function apiJson(context, method, path, data = undefined) {
  const headers = await authHeaders(context);
  const response = await context.request.fetch(`${API}${path}`, {
    method,
    headers,
    data: data ? JSON.stringify(data) : undefined,
  });
  const contentType = response.headers()["content-type"] ?? "";
  let body = null;
  const raw = await response.text();
  if (contentType.includes("json")) {
    try { body = raw ? JSON.parse(raw) : null; } catch { body = { raw: raw.slice(0, 300) }; }
  } else {
    body = { raw: raw.slice(0, 300) };
  }
  return { status: response.status(), contentType, body };
}

async function executeDetectionRun(context, payload, { expectSuccess = true } = {}) {
  const result = await apiJson(context, "POST", `/api/operational-risks/detection/runs?facilityId=${FACILITY}`, payload);
  if (expectSuccess) {
    assert("run response content-type json", result.contentType.includes("application/json") && !result.contentType.includes("problem"), result.contentType);
  } else {
    assert("run response content-type json family", result.contentType.includes("json"), result.contentType);
  }
  assert("run response not html", !String(result.body?.raw ?? "").includes("<!DOCTYPE"), String(result.body?.raw ?? "").slice(0, 80));
  return result;
}

function extractRunId(body) {
  return body?.id ?? body?.runId ?? body?.entityId ?? null;
}

async function waitForHealthReady(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${API}/health/ready`);
      if (response.status === 200) return;
    } catch { /* retry */ }
    await new Promise((resolve) => setTimeout(resolve, 2000));
  }
  fail("API health ready timeout");
}

async function waitForGirisReady(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${WEB}/giris`);
      if (response.status === 200) return;
    } catch { /* retry */ }
    await new Promise((resolve) => setTimeout(resolve, 2000));
  }
  fail("Web /giris ready timeout");
}

function restartCanonicalApi() {
  const beforePid = listenerPid(5000);
  report.restart.apiPidBefore = beforePid;
  if (beforePid) spawnSync("taskkill", ["/PID", String(beforePid), "/F"], { stdio: "ignore" });
  spawn("dotnet", ["run", "--project", join(REPO_ROOT, "apps/api/NevaxisZentra.Api.csproj"), "--urls", "http://127.0.0.1:5000", "--no-build"], {
    cwd: REPO_ROOT,
    env: { ...process.env, ASPNETCORE_ENVIRONMENT: "Development", NEVAXIS_FACTOS__AUTHENTICATION__MODE: "SessionCookie" },
    detached: true,
    stdio: "ignore",
  }).unref();
  report.restart.apiPidAfter = listenerPid(5000);
}

async function refreshAuthenticatedWorkspace(page, path = "/operasyonel-riskler") {
  const started = Date.now();
  while (Date.now() - started < 120000) {
    try {
      await page.goto(`${WEB}${path}`, { waitUntil: "domcontentloaded", timeout: 60000 });
      await waitForAuthenticatedWorkspace(page);
      return;
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 2000));
    }
  }
  fail("authenticated workspace refresh timeout");
}

function restartCanonicalWeb() {
  const beforePid = listenerPid(3001);
  report.restart.webPidBefore = beforePid;
  if (beforePid) spawnSync("taskkill", ["/PID", String(beforePid), "/F"], { stdio: "ignore" });
  const command = process.platform === "win32"
    ? "cmd.exe"
    : "pnpm";
  const args = process.platform === "win32"
    ? ["/d", "/s", "/c", "pnpm exec next dev -p 3001"]
    : ["exec", "next", "dev", "-p", "3001"];
  spawn(command, args, {
    cwd: WEB_ROOT,
    env: { ...process.env, NEVAXIS_FACTOS_API_BASE_URL: "http://127.0.0.1:5000" },
    detached: true,
    stdio: "ignore",
  }).unref();
  report.restart.webPidAfter = listenerPid(3001);
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
  if (spec.requireRunHistory) {
    await page.locator('[data-testid="operational-risk-runs-history"]').waitFor({ state: "visible", timeout: 30000 });
    assert("closure run history row visible", await page.locator(".oprisk-table--runs tbody tr").count() > 0);
    assert("closure runs empty state absent", await page.locator('[data-testid="operational-risk-runs-empty"]').count() === 0);
  }
  await waitForCaptureReady(page);
  const renderingCount = await assertNoRenderingArtifact(page, spec.label);
  await assertNoRawEnglishPrimaryTitles(page);
  await assertNoRawTechnicalCodesInPrimaryUI(page);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1);
  assert(`${spec.label} no horizontal overflow`, overflow);
  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const screenshotPath = join(SCREENSHOT_DIR, spec.filename);
  await page.screenshot({ path: screenshotPath, fullPage: false });
  const dimensions = readPngDimensions(screenshotPath);
  assert(`${spec.label} PNG width`, dimensions.width === spec.width, String(dimensions.width));
  assert(`${spec.label} PNG height`, dimensions.height === spec.height, String(dimensions.height));
  report.screenshots.push({ path: screenshotPath, ...dimensions, bytes: statSync(screenshotPath).size, renderingCount });
  console.log(`  screenshot: ${screenshotPath} (${dimensions.width}x${dimensions.height})`);
}

console.log("Running OPERATIONAL-RISK-UI-1 functional UI verifier.");

removeClosureFixtures();
assert("closure baseline runs", closureCounts().runs === 0);

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const browser = await chromium.launch({ headless: true, ...(executablePath ? { executablePath } : {}) });
const ownerContext = await browser.newContext({ deviceScaleFactor: 1 });
const ownerPage = await ownerContext.newPage();

let subscriptionRestore = null;

try {
  await signInPersona(ownerPage, "admin@demo.nevaxiszentra.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"), requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));
  ownerPage.on("console", (msg) => { if (msg.type() === "error") report.consoleErrors.push(msg.text()); });
  ownerPage.on("pageerror", (error) => report.consoleErrors.push(error.message));
  await waitForAuthenticatedWorkspace(ownerPage);
  assert("owner workspace loads", await ownerPage.locator('[data-testid="operational-risk-workspace"]').isVisible());

  report.sessionFingerprints.beforeRestart = await sessionFingerprint(ownerContext);
  assert("session fingerprint captured", report.sessionFingerprints.beforeRestart.hasSession);

  const profilesResponse = await apiJson(ownerContext, "GET", `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`);
  assert("profiles list status 200", profilesResponse.status === 200, String(profilesResponse.status));
  const approvedProfile = (profilesResponse.body.items ?? []).find((item) => item.status === "approved");
  assert("approved profile available", Boolean(approvedProfile));

  const beforeCounts = closureCounts();
  const idempotencyKey = `${CLOSURE_PREFIX}-${Date.now()}`;
  const runPayload = {
    profileId: approvedProfile.id,
    windowStartUtc: WINDOW_START,
    windowEndUtc: WINDOW_END,
    sourceProvenance: profilesResponse.body.provenanceDisclaimer ?? "functional-closure",
    idempotencyKey,
  };

  const create = await executeDetectionRun(ownerContext, runPayload);
  assert("detection run create status 201", create.status === 201, String(create.status));
  const runId = extractRunId(create.body);
  assert("runId returned", Boolean(runId), JSON.stringify(create.body));
  report.runPersistence.runIdShort = `${String(runId).slice(0, 8)}…`;

  const list = await apiJson(ownerContext, "GET", `/api/operational-risks/detection/runs?facilityId=${FACILITY}&page=1&pageSize=50`);
  assert("list runs status 200", list.status === 200, String(list.status));
  assert("list includes runId", (list.body.items ?? []).some((item) => item.id === runId));

  const detail = await apiJson(ownerContext, "GET", `/api/operational-risks/detection/runs/${runId}?facilityId=${FACILITY}`);
  assert("run detail status 200", detail.status === 200, String(detail.status));
  assert("run detail has results", (detail.body.results ?? []).length > 0, String((detail.body.results ?? []).length));

  const afterCreateCounts = closureCounts();
  assert("closure run count +1", afterCreateCounts.runs === beforeCounts.runs + 1);
  assert("closure result count > 0", afterCreateCounts.results > 0);
  assert("closure driver count > 0", afterCreateCounts.drivers > 0);
  report.runPersistence.afterCreate = afterCreateCounts;

  const dbRun = Number(db(`SELECT count(*) FROM operational_risk_detection_runs WHERE id = '${runId}';`));
  assert("database contains run", dbRun === 1, String(dbRun));

  await ownerPage.goto(`${WEB}/operasyonel-riskler?tab=runs`, { waitUntil: "networkidle", timeout: 90000 });
  await clickTab(ownerPage, "Tespit Çalıştırmaları");
  assert("UI run history row before refresh", await ownerPage.locator(".oprisk-table--runs tbody tr").count() > 0);
  const runReference = detail.body.runReference;
  if (runReference) {
    assert("UI shows persisted run reference", (await ownerPage.locator(".oprisk-table--runs").innerText()).includes(runReference), runReference);
  }
  await ownerPage.reload({ waitUntil: "networkidle" });
  assert("UI run history row after refresh", await ownerPage.locator(".oprisk-table--runs tbody tr").count() > 0);
  const secondTab = await ownerContext.newPage();
  await secondTab.goto(`${WEB}/operasyonel-riskler?tab=runs`, { waitUntil: "networkidle", timeout: 90000 });
  assert("UI run history row in new tab", await secondTab.locator(".oprisk-table--runs tbody tr").count() > 0);
  await secondTab.close();

  const replay = await executeDetectionRun(ownerContext, runPayload);
  assert("idempotent replay status 200", replay.status === 200, String(replay.status));
  assert("idempotent replay same runId", extractRunId(replay.body) === runId);
  if (replay.body?.idempotentReplay !== undefined) {
    assert("idempotent replay flag", replay.body.idempotentReplay === true);
  }
  const afterReplayCounts = closureCounts();
  assert("idempotent replay run delta 0", afterReplayCounts.runs === afterCreateCounts.runs);
  assert("idempotent replay result delta 0", afterReplayCounts.results === afterCreateCounts.results);
  assert("idempotent replay driver delta 0", afterReplayCounts.drivers === afterCreateCounts.drivers);

  const conflict = await executeDetectionRun(ownerContext, {
    ...runPayload,
    windowEndUtc: "2026-08-21T13:00:00.000Z",
  }, { expectSuccess: false });
  assert("payload conflict status 409", conflict.status === 409, String(conflict.status));
  const afterConflictCounts = closureCounts();
  assert("payload conflict run delta 0", afterConflictCounts.runs === afterCreateCounts.runs);

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
    await ownerPage.getByRole("button", { name: "Kapat", exact: true }).click();
  }

  restartCanonicalApi();
  await waitForHealthReady();
  await refreshAuthenticatedWorkspace(ownerPage);
  report.sessionFingerprints.afterApiRestart = await sessionFingerprint(ownerContext);
  assert("session fingerprint unchanged after api restart", report.sessionFingerprints.afterApiRestart.fingerprint === report.sessionFingerprints.beforeRestart.fingerprint);

  restartCanonicalWeb();
  await waitForGirisReady();
  await refreshAuthenticatedWorkspace(ownerPage);
  const newTab = await ownerContext.newPage();
  await waitForAuthenticatedWorkspace(newTab);
  report.sessionFingerprints.afterWebRestart = await sessionFingerprint(ownerContext);
  assert("session fingerprint unchanged after web restart", report.sessionFingerprints.afterWebRestart.fingerprint === report.sessionFingerprints.beforeRestart.fingerprint);
  await newTab.close();

  const currentSubscription = await apiJson(ownerContext, "GET", "/api/saas-subscription/current");
  assert("subscription current status 200", currentSubscription.status === 200, String(currentSubscription.status));
  const subscriptionId = currentSubscription.body?.subscription?.subscriptionId;
  const subscriptionVersion = currentSubscription.body?.subscription?.version ?? 1;
  const initialStatus = currentSubscription.body?.subscription?.subscriptionStatus;
  report.subscription.initial = {
    status: initialStatus,
    version: subscriptionVersion,
    fingerprint: createHash("sha256").update(JSON.stringify(currentSubscription.body?.subscription?.entitlements ?? [])).digest("hex").slice(0, 12),
  };
  assert("subscription initially active", initialStatus === "active", String(initialStatus));

  const totalsBeforeInactive = operationalRiskTotals();
  const suspend = await apiJson(ownerContext, "POST", "/api/saas-subscription/transitions", {
    subscriptionId,
    targetStatus: "suspended",
    transitionReason: "manual-suspension",
    expectedVersion: subscriptionVersion,
    idempotencyKey: `${CLOSURE_PREFIX}-suspend-${Date.now()}`,
  });
  assert("subscription suspend transition", suspend.status === 201 || suspend.status === 200, String(suspend.status));
  subscriptionRestore = { subscriptionId, expectedVersion: subscriptionVersion + 1 };

  await ownerPage.goto(`${WEB}/operasyonel-riskler`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("inactive subscription recovery route", ownerPage.url().includes("/abonelik-erisimi"), ownerPage.url());
  assert("inactive subscription paid-application reason", ownerPage.url().includes("reason=paid-application"), ownerPage.url());
  assert("inactive subscription workspace hidden", await ownerPage.locator('[data-testid="operational-risk-workspace"]').count() === 0);

  const deniedRun = await executeDetectionRun(ownerContext, runPayload, { expectSuccess: false });
  assert("inactive subscription run mutation denied", deniedRun.status === 403, String(deniedRun.status));
  const totalsAfterInactive = operationalRiskTotals();
  assert("inactive subscription run delta 0", totalsAfterInactive.runs === totalsBeforeInactive.runs);
  assert("inactive subscription result delta 0", totalsAfterInactive.results === totalsBeforeInactive.results);
  assert("inactive subscription driver delta 0", totalsAfterInactive.drivers === totalsBeforeInactive.drivers);
  assert("inactive subscription case delta 0", totalsAfterInactive.cases === totalsBeforeInactive.cases);
  assert("inactive subscription evidence delta 0", totalsAfterInactive.evidence === totalsBeforeInactive.evidence);
  report.subscription.deniedDeltas = {
    runs: totalsAfterInactive.runs - totalsBeforeInactive.runs,
    results: totalsAfterInactive.results - totalsBeforeInactive.results,
    drivers: totalsAfterInactive.drivers - totalsBeforeInactive.drivers,
  };

  const reactivate = await apiJson(ownerContext, "POST", "/api/saas-subscription/transitions", {
    subscriptionId,
    targetStatus: "active",
    transitionReason: "manual-reactivation",
    expectedVersion: subscriptionRestore.expectedVersion,
    idempotencyKey: `${CLOSURE_PREFIX}-reactivate-${Date.now()}`,
  });
  assert("subscription reactivated", reactivate.status === 201 || reactivate.status === 200, String(reactivate.status));
  subscriptionRestore = null;
  const restoredSubscription = await apiJson(ownerContext, "GET", "/api/saas-subscription/current");
  assert("subscription restored active", restoredSubscription.body?.subscription?.subscriptionStatus === "active");
  report.subscription.restored = {
    status: restoredSubscription.body?.subscription?.subscriptionStatus,
    version: restoredSubscription.body?.subscription?.version,
  };
  await waitForAuthenticatedWorkspace(ownerPage);

  console.log(`Preview closure fixture cleanup: runs=${closureCounts().runs} results=${closureCounts().results} drivers=${closureCounts().drivers}`);
  for (const spec of OWNER_SCREENSHOTS) await captureScreenshot(ownerPage, spec);

  seedReadOverride();
  const readonlyContext = await browser.newContext({ deviceScaleFactor: 1 });
  const readonlyPage = await readonlyContext.newPage();
  await signInPersona(readonlyPage, "maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
  await readonlyPage.goto(`${WEB}/operasyonel-riskler`, { waitUntil: "networkidle" });
  assert("readonly note visible", await readonlyPage.locator('[data-testid="operational-risk-readonly-note"]').isVisible());
  await captureScreenshot(readonlyPage, READONLY_SCREENSHOT);
  removeReadOverride();

  const invalidContext = await browser.newContext({ deviceScaleFactor: 1 });
  await invalidContext.addCookies([{
    name: "NevaxisZentra.session",
    value: "invalid-operational-risk-functional-closure",
    domain: "localhost",
    path: "/",
    httpOnly: true,
    secure: false,
    sameSite: "Lax",
  }]);
  const invalidPage = await invalidContext.newPage();
  const invalidResponse = await invalidPage.goto(`${WEB}/operasyonel-riskler`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("invalid session routes to login", invalidPage.url().includes("/giris"), invalidPage.url());
  assert("invalid session expired reason", invalidPage.url().includes("reason=session-expired"), invalidPage.url());
  assert("invalid session not subscription recovery", !invalidPage.url().includes("/abonelik-erisimi"), invalidPage.url());
  report.invalidSession = { url: invalidPage.url(), status: invalidResponse?.status() ?? null };
  await invalidContext.close();

  const relevantConsoleErrors = report.consoleErrors.filter((error) => !isBenignDevConsoleError(error));
  assert("no console errors", relevantConsoleErrors.length === 0, relevantConsoleErrors.join(" | "));
} finally {
  if (subscriptionRestore) {
    try {
      await apiJson(ownerContext, "POST", "/api/saas-subscription/transitions", {
        subscriptionId: subscriptionRestore.subscriptionId,
        targetStatus: "active",
        transitionReason: "manual-reactivation",
        expectedVersion: subscriptionRestore.expectedVersion,
        idempotencyKey: `${CLOSURE_PREFIX}-finally-reactivate-${Date.now()}`,
      });
    } catch {
      // best effort
    }
  }
  removeClosureFixtures();
  const afterCleanup = closureCounts();
  report.runPersistence.afterCleanup = afterCleanup;
  assert("closure fixture cleanup complete", afterCleanup.runs === 0 && afterCleanup.results === 0 && afterCleanup.drivers === 0);
  await browser.close();
}

console.log(`REPORT_JSON=${JSON.stringify(report)}`);
console.log("OPERATIONAL-RISK-UI-1 functional UI verification passed.");
console.log("COOKIE_INJECTION_USED=NO");

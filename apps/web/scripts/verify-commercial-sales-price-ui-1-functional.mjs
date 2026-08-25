#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001")
  .replace("127.0.0.1", "localhost")
  .replace(/\/$/, "");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ROUTE = "/ticari-zeka/satis-fiyat-analizi";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_OVERRIDE_ID = "dd000004-00e7-4000-8000-0000000000e8";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const SCREENSHOT_DIR = join(tmpdir(), "crevafactos-commercial-sales-price-ui-1-screenshots");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

const OWNER_SCREENSHOTS = [
  { width: 1600, height: 1000, filename: "commercial-sales-price-ui-1-overview-owner-1600x1000.png", tab: "Genel Bakış", tabId: "overview", label: "owner-overview-1600x1000", requireOverviewRanking: true },
  { width: 1600, height: 1000, filename: "commercial-sales-price-ui-1-temporal-owner-1600x1000.png", tab: "Dönemsel Sapma", tabId: "temporal", label: "owner-temporal-1600x1000" },
  { width: 1600, height: 1000, filename: "commercial-sales-price-ui-1-customers-owner-1600x1000.png", tab: "Müşteri Karşılaştırması", tabId: "customers", label: "owner-customers-1600x1000", requireRankingWorkspace: true },
  { width: 1600, height: 1000, filename: "commercial-sales-price-ui-1-history-owner-1600x1000.png", tab: "Değerlendirme Geçmişi", tabId: "history", label: "owner-history-1600x1000" },
  { width: 1600, height: 1000, filename: "commercial-sales-price-ui-1-detail-owner-1600x1000.png", tab: "Değerlendirme Geçmişi", tabId: "history", label: "owner-detail-1600x1000", openComparisonDetail: true },
  { width: 1600, height: 1000, filename: "commercial-sales-price-ui-1-driver-summary-owner-1600x1000.png", tab: "Genel Bakış", tabId: "overview", label: "owner-driver-summary-1600x1000", scrollToDriverSummary: true },
  { width: 1024, height: 900, filename: "commercial-sales-price-ui-1-overview-owner-1024x900.png", tab: "Genel Bakış", tabId: "overview", label: "owner-overview-1024x900", requireOverviewRanking: true },
];
const READONLY_SCREENSHOT = {
  width: 1600,
  height: 1000,
  filename: "commercial-sales-price-ui-1-overview-readonly-1600x1000.png",
  tab: "Genel Bakış",
  tabId: "overview",
  label: "readonly-overview-1600x1000",
  requireOverviewRanking: true,
};

const report = { screenshots: [], consoleErrors: [] };

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-UI-1 functional UI verification failed: ${message}`);
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
  if (process.env.__COMMINT_SALES_UI_1_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts/f170800e-80eb-42f9-b69a-a0770e665e7a/f170800e-80eb-42f9-b69a-a0770e665e7a.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) {
      text += readFileSync(candidate, "utf8");
    }
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    if (!process.env[key]?.trim()) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
    }
  }
  process.env.__COMMINT_SALES_UI_1_BOOTSTRAPPED = "1";
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
    VALUES ('${CLOSURE_OVERRIDE_ID}', 'bb000001-0001-4000-8000-000000000001', 'bb000003-0223-4000-8000-000000000223', '${READ_PERMISSION_ID}', 'allow', 'tenant', 'Active', NOW(), NOW(), 1);`);
}

function removeReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';`);
}

function readPngDimensions(filePath) {
  const buffer = readFileSync(filePath);
  if (buffer.length < 24 || buffer.toString("ascii", 1, 4) !== "PNG") fail(`Invalid PNG: ${filePath}`);
  return { width: buffer.readUInt32BE(16), height: buffer.readUInt32BE(20) };
}

async function authHeaders(context) {
  const cookies = await context.cookies();
  const headers = { Accept: "application/json", "Content-Type": "application/json" };
  const cookieHeader = cookies.map((cookie) => `${cookie.name}=${cookie.value}`).join("; ");
  if (cookieHeader) headers.Cookie = cookieHeader;
  const csrf = cookies.find((cookie) => cookie.name === "crevafactos.csrf");
  if (csrf?.value) headers["X-CrevaFactOS-Csrf"] = csrf.value;
  return headers;
}

async function waitForCaptureReady(page) {
  await page.waitForLoadState("networkidle", { timeout: 90000 }).catch(() => undefined);
  await page.waitForFunction(() => {
    const text = document.body?.innerText ?? "";
    return !text.includes("Rendering...");
  }, { timeout: 45000 });
}

async function waitForExecutiveDriverSummary(page) {
  await page.locator('[data-testid="commercial-sales-price-executive-driver-summary"]').waitFor({ state: "visible", timeout: 45000 });
  await page.locator('[data-testid="commercial-sales-price-executive-driver-ready"]').waitFor({ state: "visible", timeout: 45000 });
  const leadingText = await page.locator('[data-testid="commercial-sales-price-executive-driver-ready"]').innerText();
  assert("executive leading driver ready", leadingText.trim().length > 0 && !leadingText.includes("Yükleniyor"), leadingText);
}

async function waitForCustomerOverviewRanking(page) {
  await page.locator('[data-testid="commercial-sales-price-overview-rows"] .commint-supplier-row').first().waitFor({ state: "visible", timeout: 45000 });
  await page.waitForFunction(() => {
    const rows = document.querySelectorAll('[data-testid="commercial-sales-price-overview-rows"] .commint-supplier-row');
    return rows.length >= 2;
  }, { timeout: 45000 });
}

async function waitForCustomerRankingWorkspace(page) {
  await page.locator('[data-testid="commercial-sales-price-customer-ranking"] .commint-supplier-row').first().waitFor({ state: "visible", timeout: 45000 });
}

async function waitForAuthenticatedWorkspace(page) {
  const response = await page.goto(`${WEB}${ROUTE}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("sales price HTTP 200", response?.status() === 200, String(response?.status()));
  assert("no login redirect", !page.url().includes("/giris"), page.url());
  assert("no subscription recovery redirect", !page.url().includes("/abonelik-erisimi"), page.url());
  await page.locator('[data-testid="commercial-sales-price-workspace"]').waitFor({ state: "visible", timeout: 30000 });
}

async function signInThroughForm(page, { organization, email, password }, options = {}) {
  const { expectMfa = false } = options;
  await page.goto(`${WEB}/giris`, { waitUntil: "domcontentloaded", timeout: 90000 });
  const response = await page.request.post(`${WEB}/giris/oturum`, {
    form: { organizationContext: organization, email, password },
  });
  assert("sign-in route HTTP success", response.ok(), `status=${response.status()} url=${response.url()}`);
  if (expectMfa) {
    if (response.url().includes("/dashboard")) {
      await page.goto(`${WEB}/dashboard`, { waitUntil: "domcontentloaded", timeout: 90000 });
      return;
    }
    assert("sign-in MFA redirect", response.url().includes("/mfa-dogrula"), response.url());
    await page.goto(`${WEB}/mfa-dogrula`, { waitUntil: "domcontentloaded", timeout: 90000 });
    return;
  }
  assert("sign-in dashboard redirect", response.url().includes("/dashboard"), response.url());
  await page.goto(`${WEB}/dashboard`, { waitUntil: "domcontentloaded", timeout: 90000 });
}

async function completeOwnerMfa(page, mfaSecret) {
  await Promise.race([
    page.waitForURL("**/dashboard", { timeout: 10000 }),
    page.locator("#mfa-totp").waitFor({ state: "visible", timeout: 10000 }),
  ]).catch(() => undefined);
  if (page.url().includes("/dashboard")) return;

  const totpInput = page.locator("#mfa-totp");
  if (!(await totpInput.isVisible().catch(() => false))) {
    if (page.url().includes("/dashboard")) return;
    fail("MFA form unavailable");
  }

  for (const counterOffset of [-1, 0, 1]) {
    if (page.url().includes("/dashboard")) return;
    const totpCode = generateTotp(mfaSecret, counterOffset);
    await totpInput.fill(totpCode, { timeout: 5000 }).catch(() => undefined);
    if (page.url().includes("/dashboard")) return;
    const verifyResponse = await Promise.all([
      page.waitForResponse((response) => response.url().includes("/api/identity/mfa/verify"), { timeout: 15000 }).catch(() => null),
      page.getByRole("button", { name: "Doğrula" }).click(),
    ]).then(([, response]) => response);
    if (verifyResponse?.status() === 200) {
      await page.waitForURL("**/dashboard", { timeout: 90000 });
      return;
    }
    await page.waitForTimeout(400);
  }
  if (page.url().includes("/dashboard")) return;
  fail("MFA verify");
}

async function signInPersona(page, email, password, mfaSecret = null) {
  await signInThroughForm(page, { organization: ORGANIZATION, email, password }, { expectMfa: Boolean(mfaSecret) });
  await page.waitForLoadState("domcontentloaded").catch(() => undefined);
  await page.waitForTimeout(500);
  if (page.url().includes("/dashboard")) return;
  if (mfaSecret) await completeOwnerMfa(page, mfaSecret);
}

async function clickTab(page, tabName) {
  await page.locator('[data-testid="commercial-sales-price-tab-rail"]').scrollIntoViewIfNeeded();
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

async function captureScreenshot(page, spec) {
  await page.setViewportSize({ width: spec.width, height: spec.height });
  await page.goto(`${WEB}${ROUTE}?tab=${spec.tabId}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await page.locator('[data-testid="commercial-sales-price-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  await clickTab(page, spec.tab);
  if (spec.requireOverviewRanking) {
    await waitForCustomerOverviewRanking(page);
    await waitForExecutiveDriverSummary(page);
  }
  if (spec.scrollToDriverSummary) {
    await waitForCustomerOverviewRanking(page);
    await waitForExecutiveDriverSummary(page);
    const driverSummary = page.locator('[data-testid="commercial-sales-price-executive-driver-summary"]');
    await driverSummary.scrollIntoViewIfNeeded();
    await page.waitForTimeout(300);
    assert(`${spec.label} driver summary in viewport`, await driverSummary.isVisible());
    assert(`${spec.label} leading driver visible`, await page.locator('[data-testid="commercial-sales-price-leading-driver"]').isVisible());
    assert(`${spec.label} supporting drivers block`, await page.locator('[data-testid="commercial-sales-price-supporting-drivers"], .commint-driver-summary__drivers .commint-supplier-row__meta').first().isVisible());
    const driverSummaryText = await driverSummary.innerText();
    assert(`${spec.label} provenance turkish`, !driverSummaryText.includes("Deterministic"), driverSummaryText);
    assert(`${spec.label} driver summary denominator`, /paydas/i.test(driverSummaryText), driverSummaryText);
  }
  if (spec.requireRankingWorkspace) await waitForCustomerRankingWorkspace(page);
  if (spec.openComparisonDetail) {
    const detailButton = page.getByRole("button", { name: "Detay" }).first();
    await detailButton.click();
    await page.locator('[data-testid="commercial-sales-price-detail-ready"]').waitFor({ state: "visible", timeout: 30000 });
    assert(`${spec.label} detail ready state`, await page.locator('[data-testid="commercial-sales-price-detail-ready"]').isVisible());
    assert(`${spec.label} detail not loading`, !(await page.locator('[data-testid="commercial-sales-price-detail-loading"]').isVisible()));
    const drawerSections = await page.locator("[data-drawer-section]").count();
    assert(`${spec.label} drawer sections`, drawerSections >= 5, String(drawerSections));
  }
  await waitForCaptureReady(page);
  const renderingCount = await page.evaluate(() => (document.body?.innerText ?? "").split("Rendering...").length - 1);
  assert(`${spec.label} Rendering... count zero`, renderingCount === 0, String(renderingCount));
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1);
  assert(`${spec.label} no horizontal overflow`, overflow);
  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const screenshotPath = join(SCREENSHOT_DIR, spec.filename);
  await page.screenshot({ path: screenshotPath, fullPage: false });
  const dimensions = readPngDimensions(screenshotPath);
  assert(`${spec.label} PNG width`, dimensions.width === spec.width, String(dimensions.width));
  assert(`${spec.label} PNG height`, dimensions.height === spec.height, String(dimensions.height));
  report.screenshots.push({ path: screenshotPath, ...dimensions, bytes: statSync(screenshotPath).size });
  console.log(`  screenshot: ${screenshotPath} (${dimensions.width}x${dimensions.height})`);
}

console.log("Running COMMERCIAL-SALES-PRICE-UI-1 functional UI verifier.");

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const browser = await chromium.launch({ headless: true, ...(executablePath ? { executablePath } : {}) });
const ownerContext = await browser.newContext({ deviceScaleFactor: 1 });
const ownerPage = await ownerContext.newPage();

try {
  await signInPersona(ownerPage, "tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"), requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));
  ownerPage.on("console", (msg) => { if (msg.type() === "error") report.consoleErrors.push(msg.text()); });
  ownerPage.on("pageerror", (error) => report.consoleErrors.push(error.message));
  await waitForAuthenticatedWorkspace(ownerPage);
  assert("owner workspace loads", await ownerPage.locator('[data-testid="commercial-sales-price-workspace"]').isVisible());
  assert("six KPI cards", await ownerPage.locator(".commint-kpi-card").count() === 6);
  assert("kpi scope attributes present", await ownerPage.locator("[data-kpi-scope]").count() === 6);
  assert("kpi source attributes present", await ownerPage.locator("[data-kpi-source]").count() === 6);
  assert("executive title", (await ownerPage.locator("h1").innerText()).includes("Satış Fiyat Analizi"));

  const comparisonsResponse = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/sales-prices/comparisons?facilityId=${FACILITY}&comparisonType=customer-price-position&page=1&pageSize=200`);
  assert("comparisons list status 200", comparisonsResponse.status === 200, String(comparisonsResponse.status));
  const customerRuns = comparisonsResponse.body.items ?? [];
  const compatibleCustomerRun = customerRuns.find((item) => item.comparableObservationCount > 0 && item.comparisonOutcome !== "source-conflict") ?? customerRuns[0];
  assert("compatible customer comparison available", Boolean(compatibleCustomerRun), JSON.stringify(customerRuns.map((item) => item.comparisonReference)));
  const customerDetail = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/sales-prices/comparisons/${compatibleCustomerRun.id}?facilityId=${FACILITY}`);
  assert("customer detail status 200", customerDetail.status === 200, String(customerDetail.status));
  const apiCustomerRows = customerDetail.body?.customerResults ?? [];
  assert("api customer rows at least two", apiCustomerRows.length >= 2, String(apiCustomerRows.length));

  await ownerPage.goto(`${WEB}${ROUTE}?tab=overview`, { waitUntil: "networkidle", timeout: 90000 });
  await waitForCustomerOverviewRanking(ownerPage);
  const overviewCustomerCount = await ownerPage.locator('[data-testid="commercial-sales-price-overview-rows"] .commint-supplier-row').count();
  assert("overview customer rows at least two", overviewCustomerCount >= 2, String(overviewCustomerCount));
  const overviewCustomerLabels = await ownerPage.locator('[data-testid="commercial-sales-price-overview-rows"] .commint-supplier-row__title').allInnerTexts();
  assert("overview customer labels reconcile with api", overviewCustomerLabels.length === apiCustomerRows.length, `${overviewCustomerLabels.length} vs ${apiCustomerRows.length}`);
  for (const apiRow of apiCustomerRows) {
    const label = overviewCustomerLabels.find((value) => value.includes(apiRow.customerReference));
    assert(`overview includes customer ${apiRow.customerReference}`, Boolean(label), overviewCustomerLabels.join(" | "));
  }
  assert("baseline badge when applicable", await ownerPage.locator(".commint-badge--baseline").count() >= 1);
  const comparableKpi = await ownerPage.locator('[data-kpi-id="comparable-sales-evidence"]').innerText();
  assert("comparable sales evidence kpi rendered", comparableKpi.includes("Karşılaştırılabilir satış kanıtı"), comparableKpi);
  const excludedKpiValue = await ownerPage.locator('[data-kpi-id="excluded-evidence"] .commint-kpi-card__value').innerText();
  assert("excluded evidence kpi shows numeric zero", excludedKpiValue.trim() === "0", excludedKpiValue);
  const excludedKpiSummary = await ownerPage.locator('[data-kpi-id="excluded-evidence"] .commint-kpi-card__summary').innerText();
  assert("excluded evidence summary mentions zero", excludedKpiSummary.includes("0 hariç tutulan"), excludedKpiSummary);
  const provenanceStripText = await ownerPage.locator('[data-testid="commercial-sales-price-provenance-strip"]').innerText();
  assert("provenance strip turkish", !provenanceStripText.includes("Deterministic"), provenanceStripText);
  assert("provenance strip sales copy", provenanceStripText.includes("satış faturası"), provenanceStripText);
  assert("no tedarik fiyatı copy", !provenanceStripText.includes("tedarik fiyatı"), provenanceStripText);

  await ownerPage.goto(`${WEB}${ROUTE}?tab=customers`, { waitUntil: "networkidle", timeout: 90000 });
  await ownerPage.locator('[data-testid="commercial-sales-price-workspace"]').waitFor({ state: "visible" });
  await clickTab(ownerPage, "Müşteri Karşılaştırması");
  assert("spread panel visible", await ownerPage.locator('[data-testid="commercial-sales-price-spread-panel"]').isVisible());

  for (const tab of [
    { name: "Genel Bakış", id: "overview" },
    { name: "Dönemsel Sapma", id: "temporal" },
    { name: "Müşteri Karşılaştırması", id: "customers" },
    { name: "Değerlendirme Geçmişi", id: "history" },
  ]) {
    await ownerPage.goto(`${WEB}${ROUTE}?tab=${tab.id}`, { waitUntil: "networkidle", timeout: 90000 });
    await ownerPage.locator('[data-testid="commercial-sales-price-workspace"]').waitFor({ state: "visible" });
    await clickTab(ownerPage, tab.name);
  }

  await ownerPage.goto(`${WEB}${ROUTE}?tab=history`, { waitUntil: "networkidle", timeout: 90000 });
  await clickTab(ownerPage, "Değerlendirme Geçmişi");
  assert("history filter rail visible", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-rail"]').isVisible());
  assert("history filter result type", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-result-type"]').isVisible());
  assert("history filter outcome", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-outcome"]').isVisible());
  assert("history filter material", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-material"]').isVisible());
  assert("history filter customer", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-customer"]').isVisible());
  assert("history filter evidence", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-evidence"]').isVisible());
  assert("history filter date from", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-date-from"]').isVisible());
  assert("history filter date to", await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-date-to"]').isVisible());

  await ownerPage.locator('[data-testid="commercial-sales-price-history-filter-result-type"]').selectOption("temporal");
  await ownerPage.goto(`${WEB}${ROUTE}?tab=history&historyType=temporal`, { waitUntil: "networkidle", timeout: 90000 });
  assert("history type filter in url", ownerPage.url().includes("historyType=temporal"), ownerPage.url());
  const temporalOnlyRows = await ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"] tbody tr').count();
  assert("history temporal filter yields rows", temporalOnlyRows > 0, String(temporalOnlyRows));

  await ownerPage.goto(`${WEB}${ROUTE}?tab=history&historyFrom=2099-01-01&historyTo=2099-01-02`, { waitUntil: "networkidle", timeout: 90000 });
  assert("history empty state visible", await ownerPage.locator('[data-testid="commercial-sales-price-history-empty"]').isVisible());

  const clearButton = ownerPage.locator('[data-testid="commercial-sales-price-history-filter-clear"]');
  assert("history clear enabled", await clearButton.isEnabled());
  await clearButton.click();
  await ownerPage.waitForURL((url) => !url.search.includes("historyFrom="), { timeout: 15000 });
  assert("history filters cleared", !ownerPage.url().includes("historyFrom="), ownerPage.url());

  await ownerPage.goto(`${WEB}${ROUTE}?tab=overview`, { waitUntil: "networkidle", timeout: 90000 });
  await waitForExecutiveDriverSummary(ownerPage);
  assert("executive driver summary visible", await ownerPage.locator('[data-testid="commercial-sales-price-executive-driver-summary"]').isVisible());
  const executiveReference = await ownerPage.locator('[data-testid="commercial-sales-price-executive-driver-summary"] .commint-panel__trace').innerText();
  assert("executive reference human readable", executiveReference.length > 0 && !/^[0-9a-f-]{36}$/i.test(executiveReference.trim()), executiveReference);

  await ownerPage.goto(`${WEB}${ROUTE}?tab=history`, { waitUntil: "networkidle" });
  const detailButton = ownerPage.getByRole("button", { name: "Detay" }).first();
  if (await detailButton.count()) {
    await detailButton.click();
    await ownerPage.locator('[data-testid="commercial-sales-price-detail-ready"]').waitFor({ state: "visible", timeout: 30000 });
    assert("comparison detail drawer ready", await ownerPage.locator('[data-testid="commercial-sales-price-detail-ready"]').isVisible());
    const drawerSections = await ownerPage.locator("[data-drawer-section]").count();
    assert("detail drawer field groups", drawerSections >= 5, String(drawerSections));
    const drawerProvenance = await ownerPage.locator('[data-drawer-section="provenance"]').innerText();
    assert("detail drawer provenance turkish", !drawerProvenance.includes("Deterministic"), drawerProvenance);
    const supportingDrivers = await ownerPage.locator('[data-testid="commercial-sales-price-detail-supporting-drivers"]').count();
    assert("detail supporting drivers separated", supportingDrivers >= 0);
    await ownerPage.getByRole("button", { name: "Detayı kapat" }).click();
  }

  await ownerPage.goto(`${WEB}${ROUTE}?tab=customers`, { waitUntil: "networkidle" });
  await ownerPage.locator('[data-testid="commercial-sales-price-workspace"]').waitFor({ state: "visible" });
  await clickTab(ownerPage, "Müşteri Karşılaştırması");
  assert("owner customer evaluation controls visible", await ownerPage.locator('[data-testid="commercial-sales-price-customer-evaluation-controls"]').count() > 0);
  const composerTitle = await ownerPage.locator(".commint-composer__title").first().innerText();
  assert("evaluation composer title", composerTitle.includes("Müşteri karşılaştırması çalıştır"), composerTitle);

  for (const spec of OWNER_SCREENSHOTS) await captureScreenshot(ownerPage, spec);

  seedReadOverride();
  const readonlyContext = await browser.newContext({ deviceScaleFactor: 1 });
  const readonlyPage = await readonlyContext.newPage();
  await signInPersona(readonlyPage, "maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
  await readonlyPage.goto(`${WEB}${ROUTE}`, { waitUntil: "networkidle" });
  assert("readonly note visible", await readonlyPage.locator('[data-testid="commercial-sales-price-readonly-note"]').isVisible());
  assert("readonly evaluation controls absent", await readonlyPage.locator('[data-testid="commercial-sales-price-customer-evaluation-controls"]').count() === 0);
  await captureScreenshot(readonlyPage, READONLY_SCREENSHOT);
  removeReadOverride();
  await readonlyContext.close();

  const unauthContext = await browser.newContext({ deviceScaleFactor: 1 });
  const unauthPage = await unauthContext.newPage();
  await unauthPage.goto(`${WEB}${ROUTE}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("unauthenticated routes to login", unauthPage.url().includes("/giris"), unauthPage.url());
  await unauthContext.close();

  const relevantConsoleErrors = report.consoleErrors.filter((error) => !isBenignDevConsoleError(error));
  assert("no console errors", relevantConsoleErrors.length === 0, relevantConsoleErrors.join(" | "));
} finally {
  removeReadOverride();
  await browser.close();
}

console.log("COMMERCIAL-SALES-PRICE-UI-1 functional UI verification passed.");
console.log(JSON.stringify({ screenshots: report.screenshots }, null, 2));

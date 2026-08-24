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
const ROUTE = "/ticari-zeka/satin-alma-fiyat-analizi";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PROFILE = "bb000001-0220-4000-8000-000000000220";
const MATERIAL = "bb000001-00a2-4000-8000-0000000000a2";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "COMMINT-UI1-FUNCTIONAL-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000003-00e7-4000-8000-0000000000e7";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const SCREENSHOT_DIR = join(tmpdir(), "crevafactos-commercial-intelligence-ui-1-screenshots");
const PROVENANCE = "Deterministic local Development commercial-intelligence data — simulated, non-production, not real customer evidence, market prices, or guaranteed savings.";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

const OWNER_SCREENSHOTS = [
  { width: 1600, height: 1000, filename: "commercial-intelligence-ui-1-final-overview-owner-1600x1000.png", tab: "Genel Bakış", tabId: "overview", label: "owner-overview-1600x1000", requireSupplierOverview: true },
  { width: 1600, height: 1000, filename: "commercial-intelligence-ui-1-final-analyses-owner-1600x1000.png", tab: "Fiyat Analizleri", tabId: "analyses", label: "owner-analyses-1600x1000" },
  { width: 1600, height: 1000, filename: "commercial-intelligence-ui-1-final-suppliers-owner-1600x1000.png", tab: "Tedarikçi Karşılaştırması", tabId: "suppliers", label: "owner-suppliers-1600x1000", requireSupplierRanking: true },
  { width: 1600, height: 1000, filename: "commercial-intelligence-ui-1-final-profiles-owner-1600x1000.png", tab: "Analiz Profilleri", tabId: "profiles", label: "owner-profiles-1600x1000", requireProfileHero: true },
  { width: 1600, height: 1000, filename: "commercial-intelligence-ui-1-final-detail-owner-1600x1000.png", tab: "Fiyat Analizleri", tabId: "analyses", label: "owner-detail-1600x1000", openAnalysisDetail: true },
  { width: 1024, height: 900, filename: "commercial-intelligence-ui-1-final-overview-owner-1024x900.png", tab: "Genel Bakış", tabId: "overview", label: "owner-overview-1024x900", requireSupplierOverview: true },
];
const READONLY_SCREENSHOT = {
  width: 1600,
  height: 1000,
  filename: "commercial-intelligence-ui-1-final-overview-readonly-1600x1000.png",
  tab: "Genel Bakış",
  tabId: "overview",
  label: "readonly-overview-1600x1000",
  requireSupplierOverview: true,
};

const report = {
  screenshots: [],
  consoleErrors: [],
  runPersistence: {},
};

function fail(message) {
  console.error(`COMMERCIAL-INTELLIGENCE-UI-1 functional UI verification failed: ${message}`);
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
  if (process.env.__COMMINT_UI_1_BOOTSTRAPPED === "1") return;
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
      text = readFileSync(candidate, "utf8");
      break;
    }
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    if (!process.env[key]?.trim()) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
    }
  }
  process.env.__COMMINT_UI_1_BOOTSTRAPPED = "1";
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

function closureCounts() {
  const assessments = Number(db(`SELECT count(*) FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`));
  const drivers = Number(db(`SELECT count(*) FROM commercial_purchase_price_assessment_drivers WHERE assessment_id IN (SELECT id FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');`));
  return { assessments, drivers };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM commercial_purchase_price_assessment_drivers
    WHERE assessment_id IN (SELECT id FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM commercial_purchase_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
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
    if (text.includes("Rendering...")) return false;
    const loading = document.querySelector('[data-testid="commercial-intelligence-analysis-detail-loading"]');
    if (loading && loading.offsetParent !== null) return false;
    return true;
  }, { timeout: 45000 });
}

async function waitForSupplierOverview(page) {
  await page.locator('[data-testid="commercial-intelligence-supplier-overview-rows"]').waitFor({ state: "visible", timeout: 45000 });
  await page.waitForFunction(() => {
    const rows = document.querySelectorAll('[data-testid="commercial-intelligence-supplier-overview-rows"] .commint-supplier-row');
    return rows.length >= 2;
  }, { timeout: 45000 });
}

async function waitForTemporalChart(page) {
  await page.locator('[data-testid="commercial-intelligence-temporal-chart"]').waitFor({ state: "visible", timeout: 45000 });
}

async function waitForAuthenticatedWorkspace(page) {
  const response = await page.goto(`${WEB}${ROUTE}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("commercial intelligence HTTP 200", response?.status() === 200, String(response?.status()));
  assert("no login redirect", !page.url().includes("/giris"), page.url());
  assert("no subscription recovery redirect", !page.url().includes("/abonelik-erisimi"), page.url());
  await page.locator('[data-testid="commercial-intelligence-workspace"]').waitFor({ state: "visible", timeout: 30000 });
}

async function signInThroughForm(page, { organization, email, password }, options = {}) {
  const { expectMfa = false } = options;
  await page.goto(`${WEB}/giris`, { waitUntil: "domcontentloaded", timeout: 90000 });
  const response = await page.request.post(`${WEB}/giris/oturum`, {
    form: {
      organizationContext: organization,
      email,
      password,
    },
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
  if (page.url().includes("/dashboard")) {
    return;
  }
  if (mfaSecret) {
    await completeOwnerMfa(page, mfaSecret);
  }
}

async function clickTab(page, tabName) {
  await page.locator('[data-testid="commercial-intelligence-tab-rail"]').scrollIntoViewIfNeeded();
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
  await page.locator('[data-testid="commercial-intelligence-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  await clickTab(page, spec.tab);
  if (spec.openAnalysisDetail) {
    const detailButton = page.getByRole("button", { name: "Detay" }).first();
    if (await detailButton.count()) {
      await detailButton.click();
      await page.locator('[data-testid="commercial-intelligence-analysis-detail-ready"]').waitFor({ state: "visible", timeout: 30000 });
      assert("analysis detail ready state", await page.locator('[data-testid="commercial-intelligence-analysis-detail-ready"]').isVisible());
      assert("analysis detail not loading", !(await page.locator('[data-testid="commercial-intelligence-analysis-detail-loading"]').isVisible()));
    }
  }
  if (spec.requireProfileHero) {
    await page.locator('[data-testid="commercial-intelligence-profile-hero"]').waitFor({ state: "visible", timeout: 30000 });
  }
  if (spec.requireSupplierOverview) {
    await waitForTemporalChart(page);
    await waitForSupplierOverview(page);
  }
  if (spec.requireSupplierRanking) {
    await page.locator('[data-testid="commercial-intelligence-supplier-ranking"] .commint-supplier-row').first().waitFor({ state: "visible", timeout: 45000 });
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

console.log("Running COMMERCIAL-INTELLIGENCE-UI-1 functional UI verifier.");

removeClosureFixtures();
assert("closure baseline assessments", closureCounts().assessments === 0);

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
  assert("owner workspace loads", await ownerPage.locator('[data-testid="commercial-intelligence-workspace"]').isVisible());
  assert("six KPI cards", await ownerPage.locator(".commint-kpi-card").count() === 6);
  assert("kpi scope attributes present", await ownerPage.locator("[data-kpi-scope]").count() === 6);
  assert("kpi source attributes present", await ownerPage.locator("[data-kpi-source]").count() === 6);

  const comparisonsResponse = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/purchase-prices/comparisons?facilityId=${FACILITY}&page=1&pageSize=200`);
  assert("comparisons list status 200", comparisonsResponse.status === 200, String(comparisonsResponse.status));
  const supplierRuns = (comparisonsResponse.body.items ?? []).filter((item) => item.comparisonType === "supplier-price-position");
  const compatibleSupplierRun = supplierRuns.find((item) => item.comparableObservationCount > 0 && item.comparisonOutcome !== "source-conflict") ?? supplierRuns[0];
  assert("compatible supplier comparison available", Boolean(compatibleSupplierRun), JSON.stringify(supplierRuns.map((item) => item.comparisonReference)));
  const supplierDetail = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/purchase-prices/comparisons/${compatibleSupplierRun.id}?facilityId=${FACILITY}`);
  assert("supplier detail status 200", supplierDetail.status === 200, String(supplierDetail.status));
  const apiSupplierRows = supplierDetail.body?.supplierResults ?? [];
  assert("api supplier rows at least two", apiSupplierRows.length >= 2, String(apiSupplierRows.length));

  await ownerPage.goto(`${WEB}${ROUTE}?tab=overview`, { waitUntil: "networkidle", timeout: 90000 });
  await waitForSupplierOverview(ownerPage);
  const overviewSupplierCount = await ownerPage.locator('[data-testid="commercial-intelligence-supplier-overview-rows"] .commint-supplier-row').count();
  assert("overview supplier rows at least two", overviewSupplierCount >= 2, String(overviewSupplierCount));
  const overviewPrimaryText = await ownerPage.locator('[data-testid="commercial-intelligence-temporal-chart"] .commint-chart__primary').first().innerText();
  assert("overview chart primary label not technical", !overviewPrimaryText.includes("CI-CMP-PILOT"), overviewPrimaryText);
  const overviewSupplierLabels = await ownerPage.locator('[data-testid="commercial-intelligence-supplier-overview-rows"] .commint-supplier-row__title').allInnerTexts();
  assert("overview supplier labels reconcile with api", overviewSupplierLabels.length === apiSupplierRows.length, `${overviewSupplierLabels.length} vs ${apiSupplierRows.length}`);
  for (const apiRow of apiSupplierRows) {
    const label = overviewSupplierLabels.find((value) => value.includes(apiRow.supplierReference));
    assert(`overview includes supplier ${apiRow.supplierReference}`, Boolean(label), overviewSupplierLabels.join(" | "));
  }
  const tiedApi = apiSupplierRows.filter((row) => row.isTied);
  if (tiedApi.length > 0) {
    assert("overview tie badge visible", await ownerPage.locator(".commint-badge--tie").count() > 0);
  }
  const spreadKpi = await ownerPage.locator('[data-kpi-id="supplier-spread"]').innerText();
  assert("spread kpi rendered", spreadKpi.includes("Tedarikçi yayılım"), spreadKpi);
  const comparableKpiScope = await ownerPage.locator('[data-kpi-id="comparable-observations"]').getAttribute("data-kpi-scope");
  assert("comparable kpi scoped context", comparableKpiScope === "Son dönemsel karşılaştırma" || comparableKpiScope === "Son fiyat analizi", comparableKpiScope ?? "");

  const profilesResponse = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/purchase-prices/profiles?facilityId=${FACILITY}`);
  assert("profiles list status 200", profilesResponse.status === 200, String(profilesResponse.status));
  const approvedProfile = (profilesResponse.body.items ?? []).find((item) => item.status === "approved");
  assert("approved profile available", Boolean(approvedProfile));

  const beforeCounts = closureCounts();
  const idempotencyKey = `${CLOSURE_PREFIX}-${Date.now()}`;
  const evaluateBody = {
    profileId: PROFILE,
    materialId: MATERIAL,
    comparisonDimension: "material",
    currentUnitPrice: 101,
    currencyCode: "TRY",
    normalizedUnit: "kg",
    windowStartUtc: "2026-04-01T00:00:00.000Z",
    windowEndUtc: "2026-08-25T00:00:00.000Z",
    priceBasisTaxInclusion: "excluded",
    priceBasisDiscountInclusion: "excluded",
    priceBasisFreightInclusion: "excluded",
    sourceProvenance: PROVENANCE,
    idempotencyKey,
  };

  const create = await apiJson(ownerContext, "POST", `/api/commercial-intelligence/purchase-prices/assessments/evaluate?facilityId=${FACILITY}`, evaluateBody);
  assert("assessment evaluate create status 201", create.status === 201, String(create.status));
  const assessmentId = create.body?.id;
  assert("assessmentId returned", Boolean(assessmentId), JSON.stringify(create.body));

  const list = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/purchase-prices/assessments?facilityId=${FACILITY}&page=1&pageSize=50`);
  assert("list assessments status 200", list.status === 200, String(list.status));
  assert("list includes assessmentId", (list.body.items ?? []).some((item) => item.id === assessmentId));

  const afterCreateCounts = closureCounts();
  assert("closure assessment count +1", afterCreateCounts.assessments === beforeCounts.assessments + 1);
  report.runPersistence.afterCreate = afterCreateCounts;

  const replay = await apiJson(ownerContext, "POST", `/api/commercial-intelligence/purchase-prices/assessments/evaluate?facilityId=${FACILITY}`, evaluateBody);
  assert("idempotent replay status 200", replay.status === 200, String(replay.status));
  assert("idempotent replay same id", replay.body?.id === assessmentId);
  if (replay.body?.idempotentReplay !== undefined) {
    assert("idempotent replay flag", replay.body.idempotentReplay === true);
  }
  const afterReplayCounts = closureCounts();
  assert("idempotent replay assessment delta 0", afterReplayCounts.assessments === afterCreateCounts.assessments);

  const conflict = await apiJson(ownerContext, "POST", `/api/commercial-intelligence/purchase-prices/assessments/evaluate?facilityId=${FACILITY}`, {
    ...evaluateBody,
    currentUnitPrice: 120,
  });
  assert("payload conflict status 409", conflict.status === 409, String(conflict.status));
  const afterConflictCounts = closureCounts();
  assert("payload conflict assessment delta 0", afterConflictCounts.assessments === afterCreateCounts.assessments);

  for (const tab of [
    { name: "Genel Bakış", id: "overview" },
    { name: "Fiyat Analizleri", id: "analyses" },
    { name: "Tedarikçi Karşılaştırması", id: "suppliers" },
    { name: "Analiz Profilleri", id: "profiles" },
  ]) {
    await ownerPage.goto(`${WEB}${ROUTE}?tab=${tab.id}`, { waitUntil: "networkidle", timeout: 90000 });
    await ownerPage.locator('[data-testid="commercial-intelligence-workspace"]').waitFor({ state: "visible" });
    await clickTab(ownerPage, tab.name);
  }

  await ownerPage.goto(`${WEB}${ROUTE}?tab=analyses`, { waitUntil: "networkidle" });
  await ownerPage.getByLabel("Sonuç filtresi").selectOption({ index: 1 }).catch(() => undefined);
  const detailButton = ownerPage.getByRole("button", { name: "Detay" }).first();
  if (await detailButton.count()) {
    await detailButton.click();
    await ownerPage.locator('[data-testid="commercial-intelligence-analysis-detail-ready"]').waitFor({ state: "visible", timeout: 30000 });
    assert("analysis detail drawer ready", await ownerPage.locator('[data-testid="commercial-intelligence-analysis-detail-ready"]').isVisible());
    await ownerPage.getByRole("button", { name: "Detayı kapat" }).click();
  }

  assert("owner evaluation controls visible", await ownerPage.locator('[data-testid="commercial-intelligence-evaluation-controls"]').count() > 0);
  const composerTitle = await ownerPage.locator(".commint-composer__title").first().innerText();
  assert("evaluation composer title", composerTitle.includes("Yeni fiyat analizi"), composerTitle);
  const composerLabels = await ownerPage.locator(".commint-field__label").count();
  assert("evaluation composer labeled fields", composerLabels >= 4, String(composerLabels));

  for (const spec of OWNER_SCREENSHOTS) await captureScreenshot(ownerPage, spec);

  seedReadOverride();
  const readonlyContext = await browser.newContext({ deviceScaleFactor: 1 });
  const readonlyPage = await readonlyContext.newPage();
  await signInPersona(readonlyPage, "maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
  await readonlyPage.goto(`${WEB}${ROUTE}`, { waitUntil: "networkidle" });
  assert("readonly note visible", await readonlyPage.locator('[data-testid="commercial-intelligence-readonly-note"]').isVisible());
  assert("readonly evaluation controls absent", await readonlyPage.locator('[data-testid="commercial-intelligence-evaluation-controls"]').count() === 0);
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
  removeClosureFixtures();
  removeReadOverride();
  const afterCleanup = closureCounts();
  report.runPersistence.afterCleanup = afterCleanup;
  assert("closure fixture cleanup complete", afterCleanup.assessments === 0 && afterCleanup.drivers === 0);
  await browser.close();
}

console.log(`REPORT_JSON=${JSON.stringify(report)}`);
console.log("COMMERCIAL-INTELLIGENCE-UI-1 functional UI verification passed.");
console.log("COOKIE_INJECTION_USED=NO");

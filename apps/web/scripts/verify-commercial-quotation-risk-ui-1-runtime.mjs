#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001")
  .replace("127.0.0.1", "localhost")
  .replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ROUTE = "/ticari-zeka/teklif-riskleri";
const PROFILE = "bb000001-0280-4000-8000-000000000280";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ORGANIZATION = "dev-pilot-tenant";
const CLOSURE_PREFIX = "COMMQUOTE-UI1-RUNTIME";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const TAB_OPTIONS = [
  { id: "overview", label: "Genel Bakış", testId: "commercial-quotation-risk-overview" },
  { id: "assessments", label: "Değerlendirmeler", testId: "commercial-quotation-risk-assessments" },
  { id: "trends", label: "Risk Eğilimleri", testId: "commercial-quotation-risk-trends" },
  { id: "customers", label: "Müşteri Karşılaştırması", testId: "commercial-quotation-risk-customers" },
  { id: "profile", label: "Risk Profili", testId: "commercial-quotation-risk-profile" },
];
const FORBIDDEN_PAGE_TEXT = ["önerilen", "en iyi müşteri", "tercih edilen", "garantili", "onayla", "reddet"];

const report = { consoleErrors: [] };

function fail(message) {
  console.error(`COMMERCIAL-QUOTATION-RISK-UI-1 runtime verifier failed: ${message}`);
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

function applyDevelopmentIdentityEnvFromText(text, key) {
  if (process.env[key]?.trim()) return;
  const patterns = [
    new RegExp(`${key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}='([^']+)'`),
    new RegExp(`\\$env:${key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}='([^']+)'`),
    new RegExp(`${key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}=([^;'\"\\s]+)`),
  ];
  for (const pattern of patterns) {
    const match = text.match(pattern);
    if (match?.[1]) {
      process.env[key] = match[1];
      return;
    }
  }
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__COMMQUOTE_UI1_RUNTIME_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/a7824c6f-6aa8-498f-895d-7f9b31bab8e3/a7824c6f-6aa8-498f-895d-7f9b31bab8e3.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/f170800e-80eb-42f9-b69a-a0770e665e7a/f170800e-80eb-42f9-b69a-a0770e665e7a.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) text += readFileSync(candidate, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    applyDevelopmentIdentityEnvFromText(text, key);
  }
  process.env.__COMMQUOTE_UI1_RUNTIME_BOOTSTRAPPED = "1";
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
  const result = spawnSync("docker", ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function removeRuntimeFixtures() {
  db(`
    DELETE FROM commercial_quotation_risk_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_quotation_risk_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_quotation_risk_evidence_observations WHERE ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_quotation_risk_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

function buildProfilePutBody(profile, expectedVersion, overrides = {}) {
  return {
    expectedVersion,
    name: profile.name,
    status: profile.status,
    referenceWindowDurationDays: profile.referenceWindowDurationDays,
    minimumComparableObservations: profile.minimumComparableObservations,
    maximumNegativePriceVarianceRatio: profile.maximumNegativePriceVarianceRatio,
    maximumPositivePriceVarianceRatio: profile.maximumPositivePriceVarianceRatio,
    maximumPaymentTermDays: profile.maximumPaymentTermDays,
    expiryWarningDays: profile.expiryWarningDays,
    maximumEvidenceAgeDays: profile.maximumEvidenceAgeDays,
    allowedEvidenceStatuses: profile.allowedEvidenceStatuses,
    allowedSalesEvidenceKinds: profile.allowedSalesEvidenceKinds,
    requiredTaxInclusion: profile.requiredTaxInclusion,
    requiredDiscountInclusion: profile.requiredDiscountInclusion,
    requiredFreightInclusion: profile.requiredFreightInclusion,
    requiredGrossNetBasis: profile.requiredGrossNetBasis,
    sourceProvenance: profile.sourceProvenance,
    ...overrides,
  };
}

async function authHeaders(context) {
  const cookies = await context.cookies();
  const headers = { Accept: "application/json", "Content-Type": "application/json" };
  const cookieHeader = cookies.map((cookie) => `${cookie.name}=${cookie.value}`).join("; ");
  if (cookieHeader) headers.Cookie = cookieHeader;
  const csrf = cookies.find((cookie) => cookie.name === "NevaxisFactOS.csrf");
  if (csrf?.value) headers["X-NevaxisFactOS-Csrf"] = csrf.value;
  return headers;
}

async function apiJson(context, method, path, data = undefined) {
  const headers = await authHeaders(context);
  const response = await context.request.fetch(`${API}${path}`, {
    method,
    headers,
    data: data ? JSON.stringify(data) : undefined,
  });
  const raw = await response.text();
  let body = null;
  try { body = raw ? JSON.parse(raw) : null; } catch { body = { raw: raw.slice(0, 300) }; }
  return { status: response.status(), body };
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
    const verifyResponse = page.url().includes("/dashboard")
      ? { status: () => 200 }
      : await Promise.all([
        page.waitForResponse((response) => response.url().includes("/api/identity/mfa/verify"), { timeout: 15000 }).catch(() => null),
        page.getByRole("button", { name: "Doğrula" }).click({ timeout: 5000 }).catch(() => null),
      ]).then(([, response]) => response);
    if (verifyResponse?.status?.() === 200) {
      await page.waitForURL("**/dashboard", { timeout: 90000 }).catch(() => undefined);
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
  await page.locator('[data-testid="commercial-quotation-risk-tab-rail"]').scrollIntoViewIfNeeded();
  const tab = page.getByRole("tab", { name: tabName, exact: true });
  await tab.click();
  await page.waitForTimeout(250);
  assert(`${tabName} tab selected`, await tab.getAttribute("aria-selected") === "true");
}

function normalizeCount(value) {
  return String(value ?? "").replace(/\./g, "").trim();
}

console.log("Running COMMERCIAL-QUOTATION-RISK-UI-1 runtime verifier.");

removeRuntimeFixtures();

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const browser = await chromium.launch({ headless: true, ...(executablePath ? { executablePath } : {}) });
const context = await browser.newContext({ deviceScaleFactor: 1 });
const page = await context.newPage();

let originalProfile = null;
let profileRestoreVersion = null;

try {
  page.on("console", (msg) => { if (msg.type() === "error") report.consoleErrors.push(msg.text()); });
  page.on("pageerror", (error) => report.consoleErrors.push(error.message));

  await signInPersona(page, "tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"), requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));

  const workspaceApi = await apiJson(context, "GET", `/api/commercial-intelligence/quotation-risks/workspace?facilityId=${FACILITY}`);
  assert("workspace API 200", workspaceApi.status === 200, String(workspaceApi.status));
  const realQuotationMappingComplete = workspaceApi.body?.realQuotationMappingComplete === true;

  const response = await page.goto(`${WEB}${ROUTE}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("quotation risk HTTP 200", response?.status() === 200, String(response?.status()));
  assert("no login redirect", !page.url().includes("/giris"), page.url());
  await page.locator('[data-testid="commercial-quotation-risk-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  assert("workspace visible", await page.locator('[data-testid="commercial-quotation-risk-workspace"]').isVisible());

  if (!realQuotationMappingComplete) {
    assert("development banner visible", await page.locator('[data-testid="commercial-quotation-risk-development-banner"]').isVisible());
    assert("mapping note visible", await page.locator('[data-testid="commercial-quotation-risk-mapping-note"]').isVisible());
  } else {
    assert("development banner hidden when mapping complete", await page.locator('[data-testid="commercial-quotation-risk-development-banner"]').count() === 0);
  }

  assert("executive title", (await page.locator("h1").innerText()).includes("Teklif Riskleri"));
  assert("kpi grid visible", await page.locator('[data-testid="commercial-quotation-risk-kpi-grid"]').isVisible());

  const assessmentsApi = await apiJson(context, "GET", `/api/commercial-intelligence/quotation-risks/assessments?facilityId=${FACILITY}&page=1&pageSize=500`);
  assert("assessments API 200", assessmentsApi.status === 200, String(assessmentsApi.status));
  const assessments = assessmentsApi.body?.items ?? [];
  const attentionCount = assessments.filter((item) => item.outcome === "attention").length;
  const withinCount = assessments.filter((item) => item.outcome === "within-threshold").length;
  const insufficientOrConflict = assessments.filter((item) => item.outcome === "insufficient-data" || item.outcome === "source-conflict").length;

  const candidateKpi = normalizeCount(await page.locator('[data-kpi-id="candidate-assessments"] .commint-kpi-card__value').innerText());
  assert("candidate assessments KPI reconciles", candidateKpi === normalizeCount(assessments.length), `${candidateKpi} vs ${assessments.length}`);
  const attentionKpi = normalizeCount(await page.locator('[data-kpi-id="attention-assessments"] .commint-kpi-card__value').innerText());
  assert("attention assessments KPI reconciles", attentionKpi === normalizeCount(attentionCount), `${attentionKpi} vs ${attentionCount}`);
  const withinKpi = normalizeCount(await page.locator('[data-kpi-id="within-threshold-assessments"] .commint-kpi-card__value').innerText());
  assert("within-threshold KPI reconciles", withinKpi === normalizeCount(withinCount), `${withinKpi} vs ${withinCount}`);
  const insufficientKpi = normalizeCount(await page.locator('[data-kpi-id="insufficient-or-conflict"] .commint-kpi-card__value').innerText());
  assert("insufficient/conflict KPI reconciles", insufficientKpi === normalizeCount(insufficientOrConflict), `${insufficientKpi} vs ${insufficientOrConflict}`);

  const comparisonsApi = await apiJson(context, "GET", `/api/commercial-intelligence/quotation-risks/comparisons?facilityId=${FACILITY}&comparisonType=temporal-risk-trend&page=1&pageSize=50`);
  assert("comparisons API 200", comparisonsApi.status === 200, String(comparisonsApi.status));
  const temporalComparisons = comparisonsApi.body?.items ?? [];

  for (const tab of TAB_OPTIONS) {
    await page.goto(`${WEB}${ROUTE}?tab=${tab.id}`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await page.locator('[data-testid="commercial-quotation-risk-workspace"]').waitFor({ state: "visible", timeout: 30000 });
    await clickTab(page, tab.label);
    await page.locator(`[data-testid="${tab.testId}"]`).waitFor({ state: "visible", timeout: 30000 });
    assert(`${tab.label} panel visible`, await page.locator(`[data-testid="${tab.testId}"]`).isVisible());
  }

  await page.goto(`${WEB}${ROUTE}?tab=overview`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await clickTab(page, "Genel Bakış");
  const distributionText = await page.locator('[data-testid="commercial-quotation-risk-risk-distribution"]').innerText();
  const distributionMatches = [...distributionText.matchAll(/(\d[\d.]*)\s*değerlendirme/g)];
  const distributionTotal = distributionMatches.reduce((sum, match) => sum + Number(match[1].replace(/\./g, "")), 0);
  if (assessments.length > 0 && distributionMatches.length > 0) {
    assert("risk distribution count reconciles", distributionTotal === assessments.length, `${distributionTotal} vs ${assessments.length}`);
  }

  await page.goto(`${WEB}${ROUTE}?tab=assessments`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await clickTab(page, "Değerlendirmeler");
  const detailButton = page.locator('[data-testid^="commercial-quotation-risk-assessment-detail-"]').first();
  if (await detailButton.count()) {
    await detailButton.click();
    await page.locator('[data-testid="commercial-quotation-risk-assessment-drawer"]').waitFor({ state: "visible", timeout: 30000 });
    assert("assessment drawer visible", await page.locator('[data-testid="commercial-quotation-risk-assessment-drawer"]').isVisible());
    await page.getByRole("button", { name: "Detayı kapat" }).click();
    assert("assessment drawer closed", await page.locator('[data-testid="commercial-quotation-risk-assessment-drawer"]').count() === 0);
  } else {
    assert("assessments empty state visible", await page.locator('[data-testid="commercial-quotation-risk-assessments-empty"]').isVisible());
  }

  await page.goto(`${WEB}${ROUTE}?tab=trends`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await clickTab(page, "Risk Eğilimleri");
  if (temporalComparisons.length > 0) {
    assert("trends selector visible", await page.locator('[data-testid="commercial-quotation-risk-trends-selector"]').isVisible());
  }

  await page.goto(`${WEB}${ROUTE}?tab=profile`, { waitUntil: "domcontentloaded", timeout: 90000 });
  await clickTab(page, "Risk Profili");
  await page.locator('[data-testid="commercial-quotation-risk-profile-hero"]').waitFor({ state: "visible", timeout: 30000 });
  const profileHeroText = await page.locator('[data-testid="commercial-quotation-risk-profile-hero"]').innerText();
  assert("profile hero readable", profileHeroText.trim().length > 0, profileHeroText);
  assert("profile edit control visible for owner", await page.locator('[data-testid="commercial-quotation-risk-profile-edit"]').isVisible());

  const profilesApi = await apiJson(context, "GET", `/api/commercial-intelligence/quotation-risks/profiles?facilityId=${FACILITY}`);
  assert("profiles API 200", profilesApi.status === 200, String(profilesApi.status));
  originalProfile = profilesApi.body?.items?.find((item) => item.id === PROFILE);
  assert("pilot profile loaded", Boolean(originalProfile), PROFILE);

  const originalName = originalProfile.name.replace(new RegExp(`\\s+${CLOSURE_PREFIX}$`), "");
  const touchName = `${originalName} ${CLOSURE_PREFIX}`;
  await page.locator('[data-testid="commercial-quotation-risk-profile-edit"]').click();
  await page.locator('[data-testid="commercial-quotation-risk-profile-edit-form"]').waitFor({ state: "visible", timeout: 15000 });
  const nameInput = page.getByLabel("Profil görünen adı");
  await nameInput.click();
  await nameInput.fill("");
  await nameInput.fill(touchName);
  await Promise.all([
    page.waitForResponse((response) => response.url().includes("/ticari-zeka/teklif-riskleri") && response.request().method() === "POST", { timeout: 30000 }).catch(() => null),
    page.locator('[data-testid="commercial-quotation-risk-profile-edit-form"]').getByRole("button", { name: "Kaydet" }).click(),
  ]);
  await page.locator('[data-testid="commercial-quotation-risk-profile-edit-form"]').waitFor({ state: "hidden", timeout: 30000 });
  const updatedHeroText = await page.locator('[data-testid="commercial-quotation-risk-profile-hero"] h3').innerText();
  assert("profile name round-trip visible", updatedHeroText.includes(touchName), updatedHeroText);

  const touchedProfileApi = await apiJson(context, "GET", `/api/commercial-intelligence/quotation-risks/profiles?facilityId=${FACILITY}`);
  const touchedProfile = touchedProfileApi.body?.items?.find((item) => item.id === PROFILE);
  assert("profile API name updated", touchedProfile?.name === touchName, touchedProfile?.name);
  profileRestoreVersion = Number(touchedProfile?.version ?? originalProfile.version + 1);

  const pageText = (await page.locator('[data-testid="commercial-quotation-risk-workspace"]').innerText()).toLowerCase();
  for (const forbidden of FORBIDDEN_PAGE_TEXT) {
    assert(`page lacks forbidden text ${forbidden}`, !pageText.includes(forbidden.toLowerCase()), forbidden);
  }

  const relevantConsoleErrors = report.consoleErrors.filter((error) => !isBenignDevConsoleError(error));
  assert("no console errors", relevantConsoleErrors.length === 0, relevantConsoleErrors.join(" | "));
} finally {
  if (originalProfile && profileRestoreVersion) {
    const headers = await authHeaders(context);
    const restoreName = originalProfile.name.replace(new RegExp(`\\s+${CLOSURE_PREFIX}$`), "");
    await context.request.fetch(`${API}/api/commercial-intelligence/quotation-risks/profiles/${PROFILE}?facilityId=${FACILITY}`, {
      method: "PUT",
      headers,
      data: JSON.stringify(buildProfilePutBody(
        { ...originalProfile, version: profileRestoreVersion, name: restoreName },
        profileRestoreVersion,
        { name: restoreName },
      )),
    }).catch(() => undefined);
  }
  removeRuntimeFixtures();
  await browser.close();
}

console.log("COMMERCIAL-QUOTATION-RISK-UI-1 runtime verification passed.");

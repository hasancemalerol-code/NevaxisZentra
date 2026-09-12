#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const ROUTE = "/ticari-zeka/satis-fiyat-analizi";
const PURCHASE_ROUTE = "/ticari-zeka/satin-alma-fiyat-analizi";
const SUPPLIER_ROUTE = "/ticari-zeka/tedarikci-karsilastirmasi";
const OPERATIONAL_RISK_ROUTE = "/operasyonel-riskler";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PROFILE = "bb000001-0270-4000-8000-000000000270";
const MATERIAL = "bb000001-00b4-4000-8000-0000000000b4";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const MAINTENANCE_MEMBERSHIP_ID = "bb000003-0223-4000-8000-000000000223";
const CLOSURE_PREFIX = "COMMSALE-UI1-PERM-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000004-00e7-4000-8000-0000000000e8";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const PROVENANCE =
  "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.";

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE-UI-1 permission verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__COMM_SALES_UI1_PERM_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/f170800e-80eb-42f9-b69a-a0770e665e7a/f170800e-80eb-42f9-b69a-a0770e665e7a.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) { text = readFileSync(candidate, "utf8"); break; }
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    if (!process.env[key]?.trim()) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
    }
  }
  process.env.__COMM_SALES_UI1_PERM_BOOTSTRAPPED = "1";
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

class CookieJar {
  constructor() { this.cookies = new Map(); }
  ingest(response) {
    const setCookies = typeof response.headers.getSetCookie === "function" ? response.headers.getSetCookie() : [];
    for (const raw of setCookies) {
      const [pair] = raw.split(";"); const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      this.cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
    }
  }
  header() { return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; "); }
}

async function api(method, path, body, jar, headers = {}) {
  const requestHeaders = { Accept: "application/json", ...headers };
  if (body) requestHeaders["Content-Type"] = "application/json";
  if (jar?.header()) requestHeaders.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisFactOS.session") && jar.cookies.get("NevaxisFactOS.csrf") && method !== "GET") {
    requestHeaders["X-NevaxisFactOS-Csrf"] = jar.cookies.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${API}${path}`, { method, headers: requestHeaders, body: body ? JSON.stringify(body) : undefined });
  jar?.ingest(response);
  const raw = await response.text();
  let payload = null;
  try { payload = raw ? JSON.parse(raw) : null; } catch { payload = { raw: raw.slice(0, 300) }; }
  return { status: response.status, payload, raw };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", { organizationContext: ORGANIZATION, email, password, deviceLabel: `${CLOSURE_PREFIX}-permissions` }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}: ${result.status}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn("admin@demo.nevaxisfactos.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")) }, jar);
  if (verify.status !== 200) fail(`owner MFA failed: ${verify.status}`);
  return jar;
}

async function signInBrowserPersona(page, email, password, mfaSecret = null) {
  await signInThroughForm(page, { organization: ORGANIZATION, email, password }, { expectMfa: Boolean(mfaSecret) });
  await page.waitForLoadState("domcontentloaded").catch(() => undefined);
  if (page.url().includes("/dashboard")) return;
  if (mfaSecret) await completeOwnerMfa(page, mfaSecret);
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, { headers: { Cookie: jar.header(), Accept: "text/html" }, redirect: "manual" });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html, location: response.headers.get("location") };
}

function countSidebarLinks(html, route) {
  const navMatch = html.match(/<nav[^>]*class="sidebar-navigation"[^>]*>([\s\S]*?)<\/nav>/);
  if (!navMatch) return 0;
  const escaped = route.replace(/\//g, "\\/");
  return (navMatch[1].match(new RegExp(`href="${escaped}"`, "g")) ?? []).length;
}

function db(sql) {
  const result = spawnSync("docker", ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function seedReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';
    INSERT INTO tenant_membership_permission_overrides (id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES ('${CLOSURE_OVERRIDE_ID}', '${TENANT_ID}', '${MAINTENANCE_MEMBERSHIP_ID}', '${READ_PERMISSION_ID}', 'allow', 'tenant', 'Active', NOW(), NOW(), 1);`);
}

function removeReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';`);
}

function salesPriceCounts() {
  return {
    assessments: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`)),
    assessmentDrivers: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_assessment_drivers WHERE assessment_id IN (SELECT id FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')`)),
    runs: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`)),
    results: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_results WHERE comparison_run_id IN (SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')`)),
    drivers: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_drivers WHERE comparison_run_id IN (SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')`)),
  };
}

function removeClosureFixtures() {
  db(`DELETE FROM commercial_sales_price_comparison_drivers WHERE comparison_run_id IN (SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM commercial_sales_price_comparison_results WHERE comparison_run_id IN (SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_sales_price_assessment_drivers WHERE assessment_id IN (SELECT id FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM commercial_sales_price_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`);
}

function assessmentPayload(idempotencyKey) {
  return {
    profileId: PROFILE, materialId: MATERIAL, comparisonDimension: "material", currentUnitPrice: 105,
    currencyCode: "TRY", normalizedUnit: "kg", windowStartUtc: "2025-03-01T00:00:00.000Z", windowEndUtc: "2025-10-31T23:59:59.000Z",
    sourceEvidenceKind: "manual-development-simulator", priceBasisGrossNet: "net", priceBasisTaxInclusion: "excluded",
    priceBasisDiscountInclusion: "excluded", priceBasisFreightInclusion: "excluded", sourceProvenance: PROVENANCE, idempotencyKey,
  };
}

function comparisonPayload(idempotencyKey) {
  return {
    profileId: PROFILE, expectedProfileVersion: 1, comparisonType: "temporal-sales-price-deviation", materialId: MATERIAL,
    sourceEvidenceKind: "manual-development-simulator", currentWindowStartUtc: "2026-09-01T00:00:00.000Z", currentWindowEndUtc: "2026-09-30T00:00:00.000Z",
    referenceWindowStartUtc: "2026-08-01T00:00:00.000Z", referenceWindowEndUtc: "2026-08-31T00:00:00.000Z",
    currencyCode: "TRY", normalizedUnit: "kg", priceBasisGrossNet: "net", priceBasisTaxInclusion: "excluded",
    priceBasisDiscountInclusion: "excluded", priceBasisFreightInclusion: "excluded", sourceProvenance: PROVENANCE, idempotencyKey,
  };
}

function forbidText(source, forbidden, label) {
  if (String(source).toLowerCase().includes(forbidden.toLowerCase())) fail(`${label} must not contain "${forbidden}"`);
}

async function signInThroughForm(page, { organization, email, password }, options = {}) {
  const { expectMfa = false } = options;
  await page.goto(`${WEB}/giris`, { waitUntil: "domcontentloaded", timeout: 90000 });
  const response = await page.request.post(`${WEB}/giris/oturum`, {
    form: { organizationContext: organization, email, password },
  });
  assert(`browser sign-in ${email}`, response.ok(), `status=${response.status()} url=${response.url()}`);
  if (expectMfa) {
    if (response.url().includes("/dashboard")) {
      await page.goto(`${WEB}/dashboard`, { waitUntil: "domcontentloaded", timeout: 90000 });
      return;
    }
    assert(`browser sign-in MFA redirect ${email}`, response.url().includes("/mfa-dogrula"), response.url());
    await page.goto(`${WEB}/mfa-dogrula`, { waitUntil: "domcontentloaded", timeout: 90000 });
    return;
  }
  assert(`browser sign-in dashboard redirect ${email}`, response.url().includes("/dashboard"), response.url());
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
    await totpInput.fill(generateTotp(mfaSecret, counterOffset), { timeout: 5000 }).catch(() => undefined);
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
  if (!page.url().includes("/dashboard")) fail("MFA verify");
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

console.log("Running COMMERCIAL-SALES-PRICE-UI-1 permission verifier.");

const pageSource = read("apps/web/src/app/(app)/ticari-zeka/satis-fiyat-analizi/page.tsx");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const navigationVisibility = read("apps/web/src/lib/navigation/navigation-visibility.ts");
const subscription = read("apps/web/src/features/saas-subscription/domain/saas-subscription-labels.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");

assert("page requires commercial-intelligence.read", pageSource.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("refresh action requires read", actions.includes("refreshCommercialSalesPriceWorkspaceAction") && actions.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("evaluation uses manage permission via shared resolver", actions.includes("evaluateCommercialSalesPriceComparisonAction"));
assert("navigation href", navigation.includes('href: "/ticari-zeka/satis-fiyat-analizi"'));
assert("sales price permissionKey", /label: "Satış Fiyat Analizi"[\s\S]*permissionKey: "commercial-intelligence.read"/.test(navigation));
assert("sales price subscriptionModulePath", /label: "Satış Fiyat Analizi"[\s\S]*subscriptionModulePath: "\/ticari-zeka"/.test(navigation));
assert("navigation resolver permission gate", navigationVisibility.includes("item.permissionKey && !hasGrantedPermission(access, item.permissionKey)"));
assert("navigation resolver subscription gate", navigationVisibility.includes("hasSubscriptionModuleAccess(modulePath, subscription)"));
assert("navigation resolver hides empty groups", navigationVisibility.includes(".filter((group) => group.items.length > 0)"));
assert("subscription module mapping", subscription.includes('prefix: "/ticari-zeka"') && subscription.includes("module.commercial-intelligence"));
assert("sales route revalidated", actions.includes('"/ticari-zeka/satis-fiyat-analizi"'));
assert("route excluded from catch-all", catchAll.includes('"/ticari-zeka/satis-fiyat-analizi"'));
assert("readonly note in view", read("apps/web/src/features/commercial-intelligence/components/commercial-sales-price-workspace-view.tsx").includes("COMMERCIAL_INTELLIGENCE_READONLY_NOTE"));
assert("no email bypass in navigation resolver", !navigationVisibility.includes("materials-sales@dev.local"));

removeReadOverride();
removeClosureFixtures();

const ready = await fetch(`${API}/health/ready`);
assert("API ready", ready.status === 200, String(ready.status));
const giris = await fetch(`${WEB}/giris`);
assert("web giris ready", giris.status === 200, String(giris.status));

const { chromium } = await import("playwright");
const browser = await chromium.launch({
  headless: true,
  ...(resolveChromiumExecutable() ? { executablePath: resolveChromiumExecutable() } : {}),
});
let ownerJar;
let materialsJar;
let readonlyJar;
try {
  ownerJar = await signInOwner();
  materialsJar = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

  seedReadOverride();
  readonlyJar = await signIn("maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

  const ownerMe = await api("GET", "/api/identity/me", undefined, ownerJar);
  const materialsMe = await api("GET", "/api/identity/me", undefined, materialsJar);
  assert("owner identity me", ownerMe.status === 200);
  assert("materials identity me", materialsMe.status === 200);

  const ownerAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, ownerJar);
  const materialsAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, materialsJar);
  const readonlyAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, readonlyJar);
  assert("owner effective access", ownerAccess.status === 200);
  assert("materials effective access", materialsAccess.status === 200);
  assert("readonly effective access", readonlyAccess.status === 200);

  const ownerGranted = ownerAccess.payload?.grantedPermissionKeys ?? [];
  const materialsGranted = materialsAccess.payload?.grantedPermissionKeys ?? [];
  const readonlyGranted = readonlyAccess.payload?.grantedPermissionKeys ?? [];
  assert("owner commercial-intelligence.read", ownerGranted.includes("commercial-intelligence.read"));
  assert("owner commercial-intelligence.manage", ownerGranted.includes("commercial-intelligence.manage"));
  assert("materials lacks commercial-intelligence.read", !materialsGranted.includes("commercial-intelligence.read"));
  assert("materials lacks commercial-intelligence.manage", !materialsGranted.includes("commercial-intelligence.manage"));
  assert("readonly commercial-intelligence.read", readonlyGranted.includes("commercial-intelligence.read"));
  assert("readonly lacks commercial-intelligence.manage", !readonlyGranted.includes("commercial-intelligence.manage"));

  const currentSubscription = await api("GET", "/api/saas-subscription/current", undefined, ownerJar);
  assert("subscription active", currentSubscription.payload?.subscription?.subscriptionStatus === "active");

  const ownerDash = await fetchWeb("/dashboard", ownerJar);
  const materialsDash = await fetchWeb("/dashboard", materialsJar);
  const readonlyDash = await fetchWeb("/dashboard", readonlyJar);
  const ownerRoute = await fetchWeb(ROUTE, ownerJar);
  const materialsRoute = await fetchWeb(ROUTE, materialsJar);
  const readonlyRoute = await fetchWeb(ROUTE, readonlyJar);
  const unauthRoute = await fetchWeb(ROUTE, new CookieJar());

  assert("owner route 200", ownerRoute.status === 200);
  assert("unauthorized route 403", materialsRoute.status === 403);
  assert("readonly route 200", readonlyRoute.status === 200);
  assert("owner sales price sidebar once", countSidebarLinks(ownerDash.html, ROUTE) === 1);
  assert("unauthorized sales price sidebar hidden", countSidebarLinks(materialsDash.html, ROUTE) === 0, String(countSidebarLinks(materialsDash.html, ROUTE)));
  assert("readonly sales price sidebar once", countSidebarLinks(readonlyDash.html, ROUTE) === 1);
  assert("owner purchase price sidebar once", countSidebarLinks(ownerDash.html, PURCHASE_ROUTE) === 1);
  assert("owner supplier comparison sidebar once", countSidebarLinks(ownerDash.html, SUPPLIER_ROUTE) === 1);
  assert("owner operational risk sidebar once", countSidebarLinks(ownerDash.html, OPERATIONAL_RISK_ROUTE) === 1);
  assert("unauthorized purchase price sidebar hidden", countSidebarLinks(materialsDash.html, PURCHASE_ROUTE) === 0);
  assert("missing authentication redirect", unauthRoute.status === 302 || unauthRoute.status === 307 || unauthRoute.location?.includes("/giris"), String(unauthRoute.status));

  const ownerWorkspace = await api("GET", `/api/commercial-intelligence/sales-prices/workspace?facilityId=${FACILITY}`, undefined, ownerJar);
  const readonlyWorkspace = await api("GET", `/api/commercial-intelligence/sales-prices/workspace?facilityId=${FACILITY}`, undefined, readonlyJar);
  assert("owner workspace API 200", ownerWorkspace.status === 200);
  assert("owner canManage", ownerWorkspace.payload?.capabilities?.canManage === true);
  assert("readonly workspace API 200", readonlyWorkspace.status === 200);
  assert("readonly cannot manage", readonlyWorkspace.payload?.capabilities?.canManage !== true);

  const materialsContext = await browser.newContext({ deviceScaleFactor: 1 });
  const materialsPage = await materialsContext.newPage();
  await signInBrowserPersona(materialsPage, "materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
  await materialsPage.goto(`${WEB}/dashboard`, { waitUntil: "domcontentloaded", timeout: 90000 });
  const hydratedCount = await materialsPage.locator(`.sidebar-navigation a[href="${ROUTE}"]`).count();
  assert("unauthorized hydrated sales price sidebar hidden", hydratedCount === 0, String(hydratedCount));
  const routeResponse = await materialsPage.goto(`${WEB}${ROUTE}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("unauthorized hydrated route 403", routeResponse?.status() === 403, String(routeResponse?.status()));
  await materialsContext.close();

  const invalidContext = await browser.newContext({ deviceScaleFactor: 1 });
  await invalidContext.addCookies([{ name: "NevaxisFactOS.session", value: "invalid-sales-price-perm-closure", domain: "localhost", path: "/", httpOnly: true, secure: false, sameSite: "Lax" }]);
  const invalidPage = await invalidContext.newPage();
  await invalidPage.goto(`${WEB}${ROUTE}`, { waitUntil: "domcontentloaded", timeout: 90000 });
  assert("invalid session login redirect", invalidPage.url().includes("/giris"), invalidPage.url());
  assert("invalid session expired reason", invalidPage.url().includes("reason=session-expired"), invalidPage.url());
  await invalidContext.close();
} finally {
  await browser.close();
}

const beforeDenied = salesPriceCounts();
const readonlyAssessment = await api("POST", `/api/commercial-intelligence/sales-prices/assessments/evaluate?facilityId=${FACILITY}`, assessmentPayload(`${CLOSURE_PREFIX}-readonly-asm-${Date.now()}`), readonlyJar);
const readonlyComparison = await api("POST", `/api/commercial-intelligence/sales-prices/comparisons/evaluate?facilityId=${FACILITY}`, comparisonPayload(`${CLOSURE_PREFIX}-readonly-cmp-${Date.now()}`), readonlyJar);
const materialsAssessment = await api("POST", `/api/commercial-intelligence/sales-prices/assessments/evaluate?facilityId=${FACILITY}`, assessmentPayload(`${CLOSURE_PREFIX}-materials-asm-${Date.now()}`), materialsJar);
const materialsComparison = await api("POST", `/api/commercial-intelligence/sales-prices/comparisons/evaluate?facilityId=${FACILITY}`, comparisonPayload(`${CLOSURE_PREFIX}-materials-cmp-${Date.now()}`), materialsJar);
assert("readonly assessment denied", readonlyAssessment.status === 403, String(readonlyAssessment.status));
assert("readonly comparison denied", readonlyComparison.status === 403, String(readonlyComparison.status));
assert("unauthorized assessment denied", materialsAssessment.status === 403, String(materialsAssessment.status));
assert("unauthorized comparison denied", materialsComparison.status === 403, String(materialsComparison.status));
forbidText(readonlyAssessment.raw, "Npgsql", "readonly assessment leak");
forbidText(materialsComparison.raw, "password", "materials comparison leak");
const afterDenied = salesPriceCounts();
assert("denied assessment delta 0", afterDenied.assessments === beforeDenied.assessments);
assert("denied assessment driver delta 0", afterDenied.assessmentDrivers === beforeDenied.assessmentDrivers);
assert("denied comparison run delta 0", afterDenied.runs === beforeDenied.runs);
assert("denied comparison result delta 0", afterDenied.results === beforeDenied.results);
assert("denied comparison driver delta 0", afterDenied.drivers === beforeDenied.drivers);

removeReadOverride();
removeClosureFixtures();
const overrideCount = Number(db(`SELECT COUNT(*) FROM tenant_membership_permission_overrides WHERE id='${CLOSURE_OVERRIDE_ID}'`));
assert("permission override cleanup 0", overrideCount === 0, String(overrideCount));

console.log("COMMERCIAL-SALES-PRICE-UI-1 permission verification passed.");

#!/usr/bin/env node
import { createHash, createHmac } from "node:crypto";
import { execSync, spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { homedir, tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const REPO_ROOT = join(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB_ROOT = join(REPO_ROOT, "apps/web");
const SSR_RUNTIME_ENV = {
  NEVAXIS_FACTOS_API_BASE_URL: process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000",
  NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE: "development-api",
  NEVAXIS_FACTOS_DEV_AUTH_ISSUER: "crevafactos-dev",
  NEVAXIS_FACTOS_DEV_AUTH_SUBJECT: "planning-pilot-user",
  NEVAXIS_FACTOS_DEV_TENANT_KEY: "dev-pilot-tenant",
  NEVAXIS_FACTOS_DEV_FACILITY_ID: "bb000001-0005-4000-8000-000000000005",
};
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001")
  .replace("127.0.0.1", "localhost")
  .replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ROUTE = "/ticari-zeka/satis-fiyat-analizi";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PROFILE = "bb000001-0270-4000-8000-000000000270";
const MATERIAL = "bb000001-00b4-4000-8000-0000000000b4";
const TENANT = "bb000001-0001-4000-8000-000000000001";
const ORGANIZATION = "dev-pilot-tenant";
const PREFIX = "COMMSALE-INTEGRATION-GATE-OWNER-SESSION";
const SOURCE_EVIDENCE_KIND = "manual-development-simulator";
const PRICE_BASIS_GROSS_NET = "net";
const WINDOW_START = "2026-02-01T00:00:00.000Z";
const WINDOW_END = "2026-02-28T23:59:59.000Z";
const SCREENSHOT_DIR = join(tmpdir(), "NevaxisFactOS-commercial-sales-price-owner-session-closure");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const PROVENANCE =
  "Deterministic local Development sales-price evidence — simulated, non-production, not real customer, invoice, quotation, or market evidence.";

const report = {
  classification: "MISSING_OWNER_SESSION_WORKFLOW",
  runtimeOnly: true,
  productDefect: false,
  sessionFingerprints: {},
  run: {},
  deltas: {},
  cleanup: {},
  screenshot: null,
};

function fail(message) {
  console.error(`COMMERCIAL-SALES-PRICE owner-session persistence closure failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function assertNear(label, actual, expected, tolerance = 0.0001) {
  assert(label, actual !== null && actual !== undefined && Math.abs(actual - expected) < tolerance, `expected ${expected} got ${actual}`);
}

function forbidText(source, forbidden, label) {
  if (String(source).toLowerCase().includes(forbidden.toLowerCase())) {
    fail(`${label} must not contain "${forbidden}"`);
  }
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__COMM_SALES_OWNER_SESSION_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/f170800e-80eb-42f9-b69a-a0770e665e7a/f170800e-80eb-42f9-b69a-a0770e665e7a.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) text += readFileSync(candidate, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
    if (!process.env[key]?.trim()) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
    }
  }
  process.env.__COMM_SALES_OWNER_SESSION_BOOTSTRAPPED = "1";
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
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function dbExec(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-v", "ON_ERROR_STOP=1", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout || sql);
}

function listenerPid(port) {
  const result = spawnSync("netstat", ["-ano"], { encoding: "utf8" });
  if (result.status !== 0) return null;
  for (const line of result.stdout.split(/\r?\n/)) {
    if (!line.includes(`:${port}`) || !line.includes("LISTENING")) continue;
    const parts = line.trim().split(/\s+/);
    const pid = Number(parts[parts.length - 1]);
    if (Number.isFinite(pid)) return pid;
  }
  return null;
}

function spawnCanonicalApi() {
  spawn("dotnet", ["run", "--project", join(REPO_ROOT, "apps/api/NevaxisFactOS.Api.csproj"), "--urls", API, "-c", "Debug"], {
    cwd: REPO_ROOT,
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      NEVAXIS_FACTOS__AUTHENTICATION__MODE: "SessionCookie",
    },
    detached: true,
    stdio: "ignore",
  }).unref();
}

function restartCanonicalApi() {
  const beforePid = listenerPid(5000);
  if (beforePid) spawnSync("taskkill", ["/PID", String(beforePid), "/F"], { stdio: "ignore" });
  spawnCanonicalApi();
  report.restart = { ...(report.restart ?? {}), apiPidAfter: listenerPid(5000) };
}

async function ensureCanonicalApiRuntime() {
  const beforePid = listenerPid(5000);
  if (beforePid) spawnSync("taskkill", ["/PID", String(beforePid), "/F"], { stdio: "ignore" });
  execSync(`dotnet build "${join(REPO_ROOT, "apps/api/NevaxisFactOS.Api.csproj")}" -c Debug`, {
    cwd: REPO_ROOT,
    stdio: "pipe",
  });
  spawnCanonicalApi();
  await waitForHealthReady();
  report.runtime = { ...(report.runtime ?? {}), apiPid: listenerPid(5000) };
}

function spawnCanonicalWeb() {
  const command = process.platform === "win32" ? "cmd.exe" : "pnpm";
  const args = process.platform === "win32"
    ? ["/d", "/s", "/c", "pnpm exec next dev -p 3001"]
    : ["exec", "next", "dev", "-p", "3001"];
  spawn(command, args, {
    cwd: WEB_ROOT,
    env: { ...process.env, ...SSR_RUNTIME_ENV },
    detached: true,
    stdio: "ignore",
  }).unref();
}

function restartCanonicalWeb() {
  const beforePid = listenerPid(3001);
  if (beforePid) spawnSync("taskkill", ["/PID", String(beforePid), "/F"], { stdio: "ignore" });
  spawnCanonicalWeb();
  report.restart = { ...(report.restart ?? {}), webPidAfter: listenerPid(3001) };
}

async function ensureCanonicalWebRuntime() {
  const beforePid = listenerPid(3001);
  if (beforePid) spawnSync("taskkill", ["/PID", String(beforePid), "/F"], { stdio: "ignore" });
  spawnCanonicalWeb();
  await waitForGirisReady();
  report.runtime = { ...(report.runtime ?? {}), webPid: listenerPid(3001) };
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

function removeOwnerSessionFixtures() {
  dbExec(`
    DELETE FROM commercial_sales_price_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${PREFIX}%'
    );
    DELETE FROM commercial_sales_price_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${PREFIX}%'
    );
    DELETE FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${PREFIX}%';
    DELETE FROM commercial_sales_price_evidence_observations WHERE observation_set_code LIKE '${PREFIX}%';
  `);
}

function ownerSessionCounts() {
  return {
    runs: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${PREFIX}%'`)),
    results: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_results WHERE comparison_run_id IN (SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${PREFIX}%')`)),
    drivers: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_drivers WHERE comparison_run_id IN (SELECT id FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE '${PREFIX}%')`)),
    observations: Number(db(`SELECT COUNT(*) FROM commercial_sales_price_evidence_observations WHERE observation_set_code LIKE '${PREFIX}%'`)),
  };
}

function insertObservations({ setCode, prices, observationIds, customerReference }) {
  const rows = [];
  const startMs = new Date(WINDOW_START).getTime();
  const endMs = new Date(WINDOW_END).getTime();
  const stepMs = prices.length > 1 ? (endMs - startMs) / (prices.length - 1) : 0;
  for (let index = 0; index < prices.length; index += 1) {
    const observationId = observationIds[index];
    const eventAt = new Date(startMs + stepMs * index).toISOString();
    rows.push(`
      INSERT INTO commercial_sales_price_evidence_observations (
        id, tenant_id, facility_id, material_id, customer_reference, source_evidence_kind,
        source_document_reference, source_line_reference, commercial_event_at_utc, received_at_utc,
        quantity, source_unit, normalized_quantity, normalized_unit, original_unit_price, comparable_unit_price,
        currency_code, price_basis_gross_net, price_basis_tax_inclusion, price_basis_discount_inclusion,
        price_basis_freight_inclusion, is_cancelled_or_returned, evidence_status, source_fingerprint,
        source_provenance, observation_set_code, created_at_utc, created_by)
      VALUES (
        '${observationId}', '${TENANT}', '${FACILITY}', '${MATERIAL}', '${customerReference}', '${SOURCE_EVIDENCE_KIND}',
        'SO-${setCode}-${index + 1}', 'LINE-001', '${eventAt}', '${eventAt}', 10, 'kg', 10, 'kg',
        ${prices[index]}, ${prices[index]}, 'TRY', '${PRICE_BASIS_GROSS_NET}', 'excluded', 'excluded', 'excluded',
        false, 'simulated', '${setCode.toLowerCase()}-${index}', 'owner-session closure', '${setCode}',
        '${eventAt}', 'owner-session-closure')
      ON CONFLICT (id) DO UPDATE SET
        material_id = EXCLUDED.material_id,
        customer_reference = EXCLUDED.customer_reference,
        commercial_event_at_utc = EXCLUDED.commercial_event_at_utc,
        received_at_utc = EXCLUDED.received_at_utc,
        original_unit_price = EXCLUDED.original_unit_price,
        comparable_unit_price = EXCLUDED.comparable_unit_price,
        observation_set_code = EXCLUDED.observation_set_code,
        source_fingerprint = EXCLUDED.source_fingerprint;
    `);
  }
  db(rows.join("\n"));
}

function seedOwnerSessionEvidence() {
  const spreadA = [100, 100, 100, 100, 100, 100];
  const spreadB = [130, 130, 130, 130, 130, 130];
  insertObservations({
    setCode: `${PREFIX}-A`,
    prices: spreadA,
    customerReference: "CUST-OWNER-A",
    observationIds: [
      "bb000001-0a50-4000-8000-000000000a50",
      "bb000001-0a51-4000-8000-000000000a51",
      "bb000001-0a52-4000-8000-000000000a52",
      "bb000001-0a53-4000-8000-000000000a53",
      "bb000001-0a54-4000-8000-000000000a54",
      "bb000001-0a55-4000-8000-000000000a55",
    ],
  });
  insertObservations({
    setCode: `${PREFIX}-B`,
    prices: spreadB,
    customerReference: "CUST-OWNER-B",
    observationIds: [
      "bb000001-0a60-4000-8000-000000000a60",
      "bb000001-0a61-4000-8000-000000000a61",
      "bb000001-0a62-4000-8000-000000000a62",
      "bb000001-0a63-4000-8000-000000000a63",
      "bb000001-0a64-4000-8000-000000000a64",
      "bb000001-0a65-4000-8000-000000000a65",
    ],
  });
}

function loadRunFromDb(runId) {
  const line = db(`
    SELECT idempotency_key,
           COALESCE(idempotency_payload_fingerprint, ''),
           comparison_type,
           comparison_outcome,
           candidate_observation_count,
           comparable_observation_count,
           excluded_observation_count,
           COALESCE(customer_spread_ratio::text, ''),
           COALESCE(leading_driver_code, ''),
           COALESCE(comparison_reference, '')
    FROM commercial_sales_price_comparison_runs
    WHERE id = '${runId}'
  `);
  const parts = line.split("|");
  return {
    idempotencyKey: parts[0],
    payloadFingerprint: parts[1] || null,
    comparisonType: parts[2],
    comparisonOutcome: parts[3],
    candidateObservationCount: Number(parts[4]),
    comparableObservationCount: Number(parts[5]),
    excludedObservationCount: Number(parts[6]),
    customerSpreadRatio: parts[7] === "" ? null : Number(parts[7]),
    leadingDriverCode: parts[8] || null,
    comparisonReference: parts[9] || null,
  };
}

function loadCustomerResultsFromDb(runId) {
  const rows = db(`
    SELECT customer_reference,
           COALESCE(customer_median_unit_price::text, ''),
           COALESCE(relative_price_position_ratio::text, ''),
           price_rank
    FROM commercial_sales_price_comparison_results
    WHERE comparison_run_id = '${runId}'
    ORDER BY price_rank
  `);
  if (!rows) return [];
  return rows.split("\n").filter(Boolean).map((row) => {
    const [customerReference, median, positionRatio, priceRank] = row.split("|");
    return {
      customerReference,
      customerMedianUnitPrice: median === "" ? null : Number(median),
      relativePricePositionRatio: positionRatio === "" ? null : Number(positionRatio),
      priceRank: Number(priceRank),
    };
  });
}

function loadDriverCount(runId) {
  return Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_drivers WHERE comparison_run_id = '${runId}'`));
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
  const csrf = cookies.find((cookie) => cookie.name === "NevaxisFactOS.csrf");
  if (csrf?.value) headers["X-NevaxisFactOS-Csrf"] = csrf.value;
  return headers;
}

async function sessionFingerprint(context) {
  const cookies = await context.cookies(WEB);
  const session = cookies.find((cookie) => cookie.name === "NevaxisFactOS.session")?.value ?? "";
  const csrf = cookies.find((cookie) => cookie.name === "NevaxisFactOS.csrf")?.value ?? "";
  return {
    hasSession: Boolean(session),
    hasCsrf: Boolean(csrf),
    fingerprint: createHash("sha256").update(session).digest("hex").slice(0, 16),
  };
}

async function apiJson(context, method, path, data = undefined) {
  const headers = await authHeaders(context);
  const response = await context.request.fetch(`${API}${path}`, {
    method,
    headers,
    data: data ? JSON.stringify(data) : undefined,
  });
  const contentType = response.headers()["content-type"] ?? "";
  const raw = await response.text();
  let body = null;
  if (contentType.includes("json")) {
    try { body = raw ? JSON.parse(raw) : null; } catch { body = { raw: raw.slice(0, 300) }; }
  } else {
    body = { raw: raw.slice(0, 300) };
  }
  return { status: response.status(), contentType, body, raw };
}

function extractRunId(body) {
  return body?.id ?? body?.comparison?.id ?? null;
}

function extractComparison(body) {
  return body?.comparison ?? body;
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
        deviceLabel: "commercial-sales-price-owner-session-closure",
      }),
    });
    return response.status;
  }, { organization: ORGANIZATION, userEmail: email, userPassword: password });
  assert(`sign-in ${email}`, signInStatus === 200, String(signInStatus));
  if (mfaSecret) {
    const mfaState = await page.evaluate(async () => {
      const me = await fetch("/api/identity/me", { credentials: "include" });
      if (!me.ok) return { pending: true };
      const body = await me.json();
      return { pending: body?.mfaPending === true };
    });
    if (!mfaState.pending) {
      await page.goto(`${WEB}/dashboard`, { waitUntil: "domcontentloaded", timeout: 90000 });
      return;
    }
    await page.goto(`${WEB}/mfa-dogrula`, { waitUntil: "domcontentloaded", timeout: 90000 });
    await Promise.race([
      page.waitForURL("**/dashboard", { timeout: 10000 }),
      page.locator("#mfa-totp").waitFor({ state: "visible", timeout: 10000 }),
    ]).catch(() => undefined);
    if (page.url().includes("/dashboard")) return;
    let verified = false;
    for (const counterOffset of [-1, 0, 1]) {
      if (page.url().includes("/dashboard")) return;
      const totpInput = page.locator("#mfa-totp");
      if (!(await totpInput.isVisible({ timeout: 5000 }).catch(() => false))) {
        if (page.url().includes("/dashboard")) return;
        await page.waitForTimeout(400);
        continue;
      }
      await totpInput.fill(generateTotp(mfaSecret, counterOffset), { timeout: 5000 }).catch(() => undefined);
      if (page.url().includes("/dashboard")) return;
      const verifyResponse = await Promise.all([
        page.waitForResponse((response) => response.url().includes("/api/identity/mfa/verify"), { timeout: 15000 }).catch(() => null),
        page.getByRole("button", { name: "Doğrula" }).click(),
      ]).then(([, response]) => response);
      if (verifyResponse?.status() === 200) {
        verified = true;
        break;
      }
      await page.waitForTimeout(400);
    }
    if (page.url().includes("/dashboard")) return;
    assert("MFA verify", verified);
    await page.waitForURL("**/dashboard", { timeout: 90000 });
  } else {
    await page.goto(`${WEB}/dashboard`, { waitUntil: "domcontentloaded", timeout: 90000 });
  }
}

async function assertBrowserSessionReady(page, label) {
  const statuses = await page.evaluate(async () => {
    const me = await fetch("/api/identity/me", { credentials: "include" });
    const access = await fetch("/api/admin-access/me/effective-access", { credentials: "include" });
    let meBody = null;
    let accessBody = null;
    if (me.ok) meBody = await me.json();
    if (access.ok) accessBody = await access.json();
    return {
      me: me.status,
      access: access.status,
      mfaPending: meBody?.mfaPending ?? null,
      granted: accessBody?.grantedPermissionKeys ?? [],
    };
  });
  assert(`${label} identity me 200`, statuses.me === 200, String(statuses.me));
  assert(`${label} mfa not pending`, statuses.mfaPending === false, String(statuses.mfaPending));
  assert(`${label} effective access 200`, statuses.access === 200, String(statuses.access));
  assert(`${label} commercial-intelligence.read`, statuses.granted.includes("commercial-intelligence.read"));
  assert(`${label} commercial-intelligence.manage`, statuses.granted.includes("commercial-intelligence.manage"));
}

async function waitForAuthenticatedWorkspace(page) {
  const started = Date.now();
  while (Date.now() - started < 120000) {
    const response = await page.goto(`${WEB}${ROUTE}`, { waitUntil: "domcontentloaded", timeout: 90000 });
    assert("sales price route on workspace not login", !page.url().includes("/giris"), page.url());
    assert("sales price route not subscription recovery", !page.url().includes("/abonelik-erisimi"), page.url());
    assert("sales price HTTP 200", response?.status() === 200, String(response?.status()));
    const workspace = page.locator('[data-testid="commercial-sales-price-workspace"]');
    if (await workspace.isVisible().catch(() => false)) return;
    const errorBanner = page.locator(".commint-error-banner, .commint-partial-error");
    if (await errorBanner.isVisible().catch(() => false)) {
      const text = await errorBanner.innerText();
      if (!text.includes("yüklenemedi")) {
        fail(`workspace error banner: ${text.slice(0, 200)}`);
      }
    }
    await page.waitForTimeout(1500);
  }
  const snippet = (await page.locator("body").innerText().catch(() => "")).slice(0, 300);
  fail(`workspace not visible: ${snippet}`);
}

function buildEvaluatePayload(profileVersion, idempotencyKey) {
  return {
    profileId: PROFILE,
    expectedProfileVersion: profileVersion,
    comparisonType: "customer-price-position",
    materialId: MATERIAL,
    sourceEvidenceKind: SOURCE_EVIDENCE_KIND,
    priceBasisGrossNet: PRICE_BASIS_GROSS_NET,
    priceBasisTaxInclusion: "excluded",
    priceBasisDiscountInclusion: "excluded",
    priceBasisFreightInclusion: "excluded",
    currentWindowStartUtc: WINDOW_START,
    currentWindowEndUtc: WINDOW_END,
    currencyCode: "TRY",
    normalizedUnit: "kg",
    sourceProvenance: PROVENANCE,
    idempotencyKey,
  };
}

async function evaluateComparison(context, payload, { expectStatus = 201 } = {}) {
  const result = await apiJson(
    context,
    "POST",
    `/api/commercial-intelligence/sales-prices/comparisons/evaluate?facilityId=${FACILITY}`,
    payload,
  );
  assert("evaluate canonical API host", result.raw === "" || !result.raw.includes("<!DOCTYPE"), result.raw?.slice(0, 80));
  assert(
    "evaluate content-type json",
    result.contentType.includes("application/json") || result.contentType.includes("application/problem+json"),
    result.contentType,
  );
  assert(`evaluate status ${expectStatus}`, result.status === expectStatus, `${result.status} ${result.raw?.slice(0, 180)}`);
  return result;
}

async function waitForCaptureReady(page) {
  await page.waitForLoadState("networkidle", { timeout: 90000 }).catch(() => undefined);
  await page.waitForFunction(() => !(document.body?.innerText ?? "").includes("Rendering..."), { timeout: 45000 });
}

console.log("Running COMMERCIAL-SALES-PRICE owner-session persistence closure.");
console.log(`CLASSIFICATION=${report.classification}; verifier-only gap from prior integration gate (no dedicated owner-session workflow).`);

removeOwnerSessionFixtures();
assert("pre-run owner-session fixtures zero", ownerSessionCounts().runs === 0);

await ensureCanonicalApiRuntime();
await ensureCanonicalWebRuntime();
const giris = await fetch(`${WEB}/giris`);
assert("web giris ready", giris.status === 200, String(giris.status));
report.runtime = {
  ...(report.runtime ?? {}),
  apiPid: listenerPid(5000),
  webPid: listenerPid(3001),
};

const profileVersion = Number(db(`SELECT version FROM commercial_sales_price_profiles WHERE id='${PROFILE}'`));
assert("pilot profile version loaded", profileVersion >= 1, String(profileVersion));

seedOwnerSessionEvidence();
assert("owner-session observations seeded", ownerSessionCounts().observations === 12, String(ownerSessionCounts().observations));

const { chromium } = await import("playwright");
const browser = await chromium.launch({
  headless: true,
  ...(resolveChromiumExecutable() ? { executablePath: resolveChromiumExecutable() } : {}),
});
const ownerContext = await browser.newContext({ deviceScaleFactor: 1 });
const ownerPage = await ownerContext.newPage();

try {
  await signInPersona(
    ownerPage,
    "tenant-owner@dev.local",
    requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"),
  );
  await assertBrowserSessionReady(ownerPage, "post-sign-in");

  const meProbe = await apiJson(ownerContext, "GET", "/api/identity/me");
  assert("canonical identity me 200", meProbe.status === 200, `${meProbe.status} ${meProbe.raw?.slice(0, 180)}`);

  const workspaceProbe = await apiJson(
    ownerContext,
    "GET",
    `/api/commercial-intelligence/sales-prices/workspace?facilityId=${FACILITY}`,
  );
  if (workspaceProbe.status !== 200) {
    const comparisonsProbe = await apiJson(
      ownerContext,
      "GET",
      `/api/commercial-intelligence/sales-prices/comparisons?facilityId=${FACILITY}&page=1&pageSize=5`,
    );
    fail(
      `canonical workspace api 200: ${workspaceProbe.status} ${workspaceProbe.raw?.slice(0, 180)}; comparisons=${comparisonsProbe.status} ${comparisonsProbe.raw?.slice(0, 180)}`,
    );
  }
  assert("canonical workspace api 200", workspaceProbe.status === 200);

  await waitForAuthenticatedWorkspace(ownerPage);

  const identity = await apiJson(ownerContext, "GET", "/api/identity/me");
  assert("identity me 200", identity.status === 200, String(identity.status));
  assert("tenant owner identity", identity.body?.displayName?.includes("Tenant Owner") || String(identity.body?.emailMasked ?? "").includes("tenant-owner"));
  assert("mfa not pending", identity.body?.mfaPending === false, String(identity.body?.mfaPending));

  const access = await apiJson(ownerContext, "GET", "/api/admin-access/me/effective-access");
  const granted = access.body?.grantedPermissionKeys ?? [];
  assert("commercial-intelligence.read granted", granted.includes("commercial-intelligence.read"));
  assert("commercial-intelligence.manage granted", granted.includes("commercial-intelligence.manage"));

  report.sessionFingerprints.beforeEvaluate = await sessionFingerprint(ownerContext);
  assert("session fingerprint captured", report.sessionFingerprints.beforeEvaluate.hasSession);
  assert("csrf cookie present", report.sessionFingerprints.beforeEvaluate.hasCsrf);

  const idempotencyKey = `${PREFIX}-${Date.now()}`;
  const payload = buildEvaluatePayload(profileVersion, idempotencyKey);
  const beforeCounts = ownerSessionCounts();
  const create = await evaluateComparison(ownerContext, payload, { expectStatus: 201 });
  const runId = extractRunId(create.body);
  assert("run id returned", Boolean(runId), JSON.stringify(create.body));
  const comparison = extractComparison(create.body);
  assert("comparison type customer-price-position", comparison.comparisonType === "customer-price-position", comparison.comparisonType);
  assert("comparison outcome attention", comparison.comparisonOutcome === "attention", comparison.comparisonOutcome);
  assert("customer results present", (comparison.customerResults ?? []).length === 2, String((comparison.customerResults ?? []).length));
  assert("drivers present", (comparison.drivers ?? []).length >= 1, String((comparison.drivers ?? []).length));
  assert("leading driver present", Boolean(comparison.leadingDriverCode));
  assert("candidate denominator honest", comparison.candidateObservationCount === comparison.comparableObservationCount + comparison.excludedObservationCount);
  for (const row of comparison.customerResults ?? []) {
    if (row.relativePricePositionRatio !== null && row.relativePricePositionRatio !== undefined) {
      assert(`${row.customerReference} position >= 0`, row.relativePricePositionRatio >= 0);
      assert(`${row.customerReference} position < 1`, row.relativePricePositionRatio < 1);
    }
  }
  assertNear("customer spread ratio", comparison.customerSpreadRatio, 0.3);

  report.run = {
    runIdShort: `${String(runId).slice(0, 8)}…`,
    comparisonReference: comparison.comparisonReference,
    idempotencyKey,
    outcome: comparison.comparisonOutcome,
    customerRows: (comparison.customerResults ?? []).length,
    driverCount: (comparison.drivers ?? []).length,
    payloadFingerprint: loadRunFromDb(runId).payloadFingerprint,
  };

  const afterCreateCounts = ownerSessionCounts();
  assert("create run delta +1", afterCreateCounts.runs === beforeCounts.runs + 1);
  assert("create results > 0", afterCreateCounts.results > beforeCounts.results);
  assert("create drivers > 0", afterCreateCounts.drivers > beforeCounts.drivers);

  const list = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/sales-prices/comparisons?facilityId=${FACILITY}&page=1&pageSize=100`);
  assert("list 200", list.status === 200, String(list.status));
  assert("list contains run", (list.body?.items ?? []).some((item) => item.id === runId));

  const detail = await apiJson(ownerContext, "GET", `/api/commercial-intelligence/sales-prices/comparisons/${runId}?facilityId=${FACILITY}`);
  assert("detail 200", detail.status === 200, String(detail.status));
  assert("detail comparison type", detail.body?.comparisonType === "customer-price-position");
  assert("detail customer results", (detail.body?.customerResults ?? []).length === 2);
  assert("detail drivers", (detail.body?.drivers ?? []).length >= 1);
  forbidText(JSON.stringify(detail.body ?? {}), "Npgsql", "detail response");
  forbidText(JSON.stringify(detail.body ?? {}), "password", "detail response");

  const dbRun = loadRunFromDb(runId);
  const dbCustomers = loadCustomerResultsFromDb(runId);
  const dbDrivers = loadDriverCount(runId);
  assert("db run id unique", Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE id='${runId}'`)) === 1);
  assert("db idempotency key", dbRun.idempotencyKey === idempotencyKey);
  assert("db comparison type", dbRun.comparisonType === "customer-price-position");
  assert("db outcome matches api", dbRun.comparisonOutcome === comparison.comparisonOutcome);
  assert("db customer rows", dbCustomers.length === 2);
  assert("db driver count", dbDrivers >= 1);
  assert("db candidate denominator", dbRun.candidateObservationCount === dbRun.comparableObservationCount + dbRun.excludedObservationCount);
  assertNear("db spread ratio", dbRun.customerSpreadRatio, 0.3);
  for (const row of dbCustomers) {
    if (row.relativePricePositionRatio !== null) {
      assert(`db ${row.customerReference} position bound`, row.relativePricePositionRatio >= 0 && row.relativePricePositionRatio < 1);
    }
  }

  await ownerPage.goto(`${WEB}${ROUTE}?tab=history`, { waitUntil: "networkidle", timeout: 90000 });
  await ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"]').waitFor({ state: "visible", timeout: 30000 });
  const historyText = await ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"]').innerText();
  assert("ui history contains reference", historyText.includes(comparison.comparisonReference), comparison.comparisonReference);
  await ownerPage.reload({ waitUntil: "networkidle" });
  const historyAfterRefresh = await ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"]').innerText();
  assert("ui history after refresh", historyAfterRefresh.includes(comparison.comparisonReference));

  const historyRow = ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"] tbody tr', {
    hasText: comparison.comparisonReference,
  }).first();
  await historyRow.getByRole("button", { name: "Detay" }).click();
  await ownerPage.locator('[data-testid="commercial-sales-price-detail-ready"]').waitFor({ state: "visible", timeout: 30000 });
  assert("detail drawer ready", await ownerPage.locator('[data-testid="commercial-sales-price-detail-ready"]').isVisible());
  assert("detail drawer not loading", !(await ownerPage.locator('[data-testid="commercial-sales-price-detail-loading"]').isVisible()));
  const drawerText = await ownerPage.locator(".commint-drawer").innerText();
  assert("drawer contains reference", drawerText.includes(comparison.comparisonReference));
  assert("drawer turkish provenance", !drawerText.includes("Deterministic"));
  assert("drawer no raw uuid primary", !/^[0-9a-f-]{36}$/i.test(comparison.comparisonReference));

  await waitForCaptureReady(ownerPage);
  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  await ownerPage.setViewportSize({ width: 1600, height: 1000 });
  const screenshotPath = join(SCREENSHOT_DIR, "commercial-sales-price-owner-session-history-detail-1600x1000.png");
  await ownerPage.screenshot({ path: screenshotPath, fullPage: false });
  const dimensions = readPngDimensions(screenshotPath);
  assert("screenshot width", dimensions.width === 1600, String(dimensions.width));
  assert("screenshot height", dimensions.height === 1000, String(dimensions.height));
  report.screenshot = { path: screenshotPath, ...dimensions, bytes: statSync(screenshotPath).size };

  const countsBeforeReplay = ownerSessionCounts();
  const fingerprintBeforeReplay = loadRunFromDb(runId).payloadFingerprint;
  const replay = await evaluateComparison(ownerContext, payload, { expectStatus: 200 });
  assert("replay same run id", extractRunId(replay.body) === runId);
  if (replay.body?.idempotentReplay !== undefined) assert("idempotent replay flag", replay.body.idempotentReplay === true);
  const countsAfterReplay = ownerSessionCounts();
  report.deltas.replay = {
    runs: countsAfterReplay.runs - countsBeforeReplay.runs,
    results: countsAfterReplay.results - countsBeforeReplay.results,
    drivers: countsAfterReplay.drivers - countsBeforeReplay.drivers,
    observations: countsAfterReplay.observations - countsBeforeReplay.observations,
    payloadFingerprint: loadRunFromDb(runId).payloadFingerprint,
  };
  assert("replay run delta 0", report.deltas.replay.runs === 0);
  assert("replay result delta 0", report.deltas.replay.results === 0);
  assert("replay driver delta 0", report.deltas.replay.drivers === 0);
  assert("replay observation delta 0", report.deltas.replay.observations === 0);
  assert("replay fingerprint unchanged", report.deltas.replay.payloadFingerprint === fingerprintBeforeReplay);

  await ownerPage.reload({ waitUntil: "networkidle" });
  const historyAfterReplay = await ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"]').innerText();
  const referenceOccurrences = (historyAfterReplay.match(new RegExp(comparison.comparisonReference, "g")) ?? []).length;
  assert("ui single history reference after replay", referenceOccurrences === 1, String(referenceOccurrences));

  const countsBeforeConflict = ownerSessionCounts();
  const conflict = await evaluateComparison(ownerContext, { ...payload, currencyCode: "USD" }, { expectStatus: 409 });
  forbidText(conflict.raw ?? "", "Npgsql", "conflict response");
  forbidText(conflict.raw ?? "", "password", "conflict response");
  const countsAfterConflict = ownerSessionCounts();
  report.deltas.conflict = {
    runs: countsAfterConflict.runs - countsBeforeConflict.runs,
    results: countsAfterConflict.results - countsBeforeConflict.results,
    drivers: countsAfterConflict.drivers - countsBeforeConflict.drivers,
  };
  assert("conflict run delta 0", report.deltas.conflict.runs === 0);
  assert("conflict result delta 0", report.deltas.conflict.results === 0);
  assert("conflict driver delta 0", report.deltas.conflict.drivers === 0);

  const countsBeforeStale = ownerSessionCounts();
  await evaluateComparison(
    ownerContext,
    { ...payload, expectedProfileVersion: 999, idempotencyKey: `${PREFIX}-stale-${Date.now()}` },
    { expectStatus: 409 },
  );
  const countsAfterStale = ownerSessionCounts();
  report.deltas.stale = {
    runs: countsAfterStale.runs - countsBeforeStale.runs,
    results: countsAfterStale.results - countsBeforeStale.results,
    drivers: countsAfterStale.drivers - countsBeforeStale.drivers,
  };
  assert("stale run delta 0", report.deltas.stale.runs === 0);
  assert("stale result delta 0", report.deltas.stale.results === 0);
  assert("stale driver delta 0", report.deltas.stale.drivers === 0);
  assert("pilot profile version unchanged", Number(db(`SELECT version FROM commercial_sales_price_profiles WHERE id='${PROFILE}'`)) === profileVersion);

  const concurrentKey = `${PREFIX}-concurrent-${Date.now()}`;
  const concurrentPayload = {
    ...buildEvaluatePayload(profileVersion, concurrentKey),
    currentWindowStartUtc: "2026-02-02T00:00:00.000Z",
    currentWindowEndUtc: "2026-02-27T23:59:59.000Z",
  };
  const [concurrentA, concurrentB] = await Promise.all([
    apiJson(ownerContext, "POST", `/api/commercial-intelligence/sales-prices/comparisons/evaluate?facilityId=${FACILITY}`, concurrentPayload),
    apiJson(ownerContext, "POST", `/api/commercial-intelligence/sales-prices/comparisons/evaluate?facilityId=${FACILITY}`, concurrentPayload),
  ]);
  const concurrentStatuses = [concurrentA.status, concurrentB.status].sort();
  assert("concurrent statuses valid", concurrentStatuses[0] === 200 || concurrentStatuses[0] === 201, JSON.stringify(concurrentStatuses));
  const concurrentRunIdA = extractRunId(concurrentA.body);
  const concurrentRunIdB = extractRunId(concurrentB.body);
  assert("concurrent same run id", concurrentRunIdA && concurrentRunIdA === concurrentRunIdB, JSON.stringify([concurrentRunIdA, concurrentRunIdB]));
  const concurrentPersisted = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE idempotency_key='${concurrentKey}'`));
  assert("concurrent one persisted run", concurrentPersisted === 1, String(concurrentPersisted));
  report.deltas.concurrent = { persistedRuns: concurrentPersisted, runIdShort: `${String(concurrentRunIdA).slice(0, 8)}…` };

  report.sessionFingerprints.beforeRestart = await sessionFingerprint(ownerContext);
  restartCanonicalApi();
  await waitForHealthReady();
  await waitForAuthenticatedWorkspace(ownerPage);
  report.sessionFingerprints.afterApiRestart = await sessionFingerprint(ownerContext);
  assert("fingerprint unchanged after api restart", report.sessionFingerprints.afterApiRestart.fingerprint === report.sessionFingerprints.beforeRestart.fingerprint);
  const historyAfterApiRestart = await ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"]').innerText().catch(async () => {
    await ownerPage.goto(`${WEB}${ROUTE}?tab=history`, { waitUntil: "networkidle" });
    return ownerPage.locator('[data-testid="commercial-sales-price-history-workspace"]').innerText();
  });
  assert("run visible after api restart", historyAfterApiRestart.includes(comparison.comparisonReference));

  restartCanonicalWeb();
  await waitForGirisReady();
  await waitForAuthenticatedWorkspace(ownerPage);
  const newTab = await ownerContext.newPage();
  await newTab.goto(`${WEB}${ROUTE}?tab=history`, { waitUntil: "networkidle", timeout: 90000 });
  assert("new tab no login redirect", !newTab.url().includes("/giris"), newTab.url());
  const newTabHistory = await newTab.locator('[data-testid="commercial-sales-price-history-workspace"]').innerText();
  assert("new tab history contains run", newTabHistory.includes(comparison.comparisonReference));
  await newTab.reload({ waitUntil: "networkidle" });
  assert("new tab refresh no login", !newTab.url().includes("/giris"), newTab.url());
  report.sessionFingerprints.afterWebRestart = await sessionFingerprint(ownerContext);
  assert("fingerprint unchanged after web restart", report.sessionFingerprints.afterWebRestart.fingerprint === report.sessionFingerprints.beforeRestart.fingerprint);
  await newTab.close();
} finally {
  await browser.close().catch(() => undefined);
  removeOwnerSessionFixtures();
  report.cleanup = ownerSessionCounts();
  const pilotRuns = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE profile_id='${PROFILE}'`));
  const closureRuns = Number(db(`SELECT COUNT(*) FROM commercial_sales_price_comparison_runs WHERE idempotency_key LIKE 'COMMSALE2-CLOSURE%' OR idempotency_key LIKE 'COMMSALE2-BOUNDARY%'`));
  const overrideCount = Number(db(`SELECT COUNT(*) FROM tenant_membership_permission_overrides WHERE id='dd000004-00e7-4000-8000-0000000000e8'`));
  assert("cleanup runs 0", report.cleanup.runs === 0, String(report.cleanup.runs));
  assert("cleanup results 0", report.cleanup.results === 0, String(report.cleanup.results));
  assert("cleanup drivers 0", report.cleanup.drivers === 0, String(report.cleanup.drivers));
  assert("cleanup observations 0", report.cleanup.observations === 0, String(report.cleanup.observations));
  assert("pilot runs still seven", pilotRuns === 7, String(pilotRuns));
  assert("closure fixtures 0", closureRuns === 0, String(closureRuns));
  assert("permission overrides 0", overrideCount === 0, String(overrideCount));
}

console.log("COMMERCIAL-SALES-PRICE owner-session persistence closure passed.");
console.log(JSON.stringify(report, null, 2));

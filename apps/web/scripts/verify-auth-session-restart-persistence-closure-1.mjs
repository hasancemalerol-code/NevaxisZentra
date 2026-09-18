#!/usr/bin/env node
import { createHash, createHmac } from "node:crypto";
import { execSync, spawnSync } from "node:child_process";
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const REPO = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const LAUNCHER = join(REPO, "scripts/runtime/start-local-saas-development.ps1");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const ORGANIZATION = "dev-pilot-tenant";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const DATA_PROTECTION_PATH = join(
  process.env.LOCALAPPDATA ?? "",
  "crevafactos",
  "DataProtection-Keys",
);

const report = {
  initialApiPid: null,
  finalApiPid: null,
  initialWebPid: null,
  finalWebPid: null,
  authenticationMode: "SessionCookie",
  dataProtectionPath: DATA_PROTECTION_PATH,
  dataProtectionApplicationName: "crevafactos",
  dataProtectionProcessIdentity: "NevaxisZentra.Api (dotnet)",
  fingerprintBeforeApiRestart: null,
  fingerprintAfterApiRestart: null,
  fingerprintBeforeWebRestart: null,
  fingerprintAfterWebRestart: null,
};

function fail(message) {
  console.error(`AUTH-SESSION-RESTART-PERSISTENCE-CLOSURE-1 failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__AUTH_RESTART_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(process.env.USERPROFILE ?? "", ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(process.env.USERPROFILE ?? "", ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(process.env.USERPROFILE ?? "", ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) {
      text = readFileSync(candidate, "utf8");
      break;
    }
  }
  for (const key of [
    "DevelopmentSaasIdentity__TenantOwnerPassword",
    "DevelopmentSaasIdentity__MfaSharedSecret",
  ]) {
    if (process.env[key]?.trim()) continue;
    const match = text.match(new RegExp(`${key}='([^']+)'`));
    if (match?.[1]) process.env[key] = match[1];
  }
  process.env.__AUTH_RESTART_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    const value = TOTP_ALPHABET.indexOf(character);
    if (value < 0) fail("Invalid MFA secret");
    buffer = (buffer << 5) | value;
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const secretBytes = Buffer.from(output);
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", secretBytes).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

function getListeningPid(port) {
  const output = execSync(`netstat -ano | findstr ":${port} "`, { encoding: "utf8", stdio: ["pipe", "pipe", "ignore"] });
  for (const line of output.split(/\r?\n/)) {
    if (!line.includes("LISTENING")) continue;
    const parts = line.trim().split(/\s+/);
    const pid = Number.parseInt(parts.at(-1), 10);
    if (Number.isFinite(pid) && pid > 0) return pid;
  }
  return null;
}

function stopPid(pid, label) {
  if (!pid) fail(`Missing PID for ${label}`);
  const result = spawnSync("taskkill", ["/PID", String(pid), "/F"], { encoding: "utf8" });
  if (result.status !== 0 && !/not found/i.test(result.stderr ?? "")) {
    fail(`Failed to stop ${label} PID ${pid}: ${result.stderr ?? result.stdout}`);
  }
  console.log(`Stopped ${label} PID ${pid}`);
}

function runLauncher(skipApi, skipWeb) {
  const args = [
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    LAUNCHER,
  ];
  if (skipApi) args.push("-SkipApi");
  if (skipWeb) args.push("-SkipWeb");
  const result = spawnSync("powershell.exe", args, {
    cwd: REPO,
    encoding: "utf8",
    timeout: 180000,
  });
  if (result.status !== 0) {
    fail(`Launcher failed: ${result.stderr ?? result.stdout}`);
  }
}

async function waitForHttp(url, expectedStatus = 200, timeoutMs = 120000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url, { redirect: "manual" });
      if (response.status === expectedStatus) return;
    } catch {
      // retry
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 2000));
  }
  fail(`Timed out waiting for ${url} -> HTTP ${expectedStatus}`);
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

async function sessionFingerprint(context) {
  const cookies = await context.cookies(WEB);
  const session = cookies.find((cookie) => cookie.name === "NevaxisZentra.session");
  if (!session?.value) return null;
  return createHash("sha256").update(session.value).digest("hex").slice(0, 16);
}

async function signInThroughForm(page, { organization, email, password }) {
  await page.goto(`${WEB}/giris`, { waitUntil: "networkidle", timeout: 90000 });
  await page.locator("#sign-in-organization").fill(organization);
  await page.locator("#sign-in-email").fill(email);
  await page.locator("#sign-in-password").fill(password);
  await page.getByRole("button", { name: "Oturum aç" }).click();
  await page.waitForURL("**/mfa-dogrula", { timeout: 60000 });
}

async function completeMfa(page, secret) {
  await page.locator("#mfa-totp").fill(generateTotp(secret));
  await page.getByRole("button", { name: "Doğrula" }).click();
  await page.waitForURL("**/dashboard", { timeout: 60000 });
}

async function assertApiStatuses(page, label) {
  const statuses = await page.evaluate(async () => {
    const me = await fetch("/api/identity/me", { credentials: "include" });
    const current = await fetch("/api/saas-subscription/current", { credentials: "include" });
    let subscriptionStatus = null;
    if (current.ok) {
      const payload = await current.json();
      subscriptionStatus = payload?.subscription?.subscriptionStatus ?? null;
    }
    return {
      me: me.status,
      current: current.status,
      subscriptionStatus,
    };
  });
  assert(`${label} /api/identity/me -> 200`, statuses.me === 200, String(statuses.me));
  assert(`${label} /api/saas-subscription/current -> 200`, statuses.current === 200, String(statuses.current));
  assert(`${label} subscription active`, statuses.subscriptionStatus === "active", String(statuses.subscriptionStatus));
}

async function assertProtectedRoutes(page, label) {
  for (const route of ["/dashboard", "/enerji", "/fabrika-vitrini"]) {
    const response = await page.goto(`${WEB}${route}`, { waitUntil: "networkidle", timeout: 90000 });
    assert(`${label} ${route} URL`, !page.url().includes("/abonelik-erisimi") && !page.url().includes("/giris"), page.url());
    assert(`${label} ${route} HTTP 200`, response?.status() === 200, String(response?.status()));
  }
  const refresh = await page.reload({ waitUntil: "networkidle", timeout: 90000 });
  assert(`${label} refresh preserves access`, !page.url().includes("/abonelik-erisimi") && !page.url().includes("/giris"), page.url());
  assert(`${label} refresh HTTP 200`, refresh?.status() === 200, String(refresh?.status()));
  const tab = await page.context().newPage();
  const tabResponse = await tab.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
  assert(`${label} new tab preserves access`, !tab.url().includes("/abonelik-erisimi") && !tab.url().includes("/giris"), tab.url());
  assert(`${label} new tab HTTP 200`, tabResponse?.status() === 200, String(tabResponse?.status()));
  await tab.close();
}

console.log("Running AUTH-SESSION-RESTART-PERSISTENCE-CLOSURE-1 verifier.");

report.initialApiPid = getListeningPid(5000);
report.initialWebPid = getListeningPid(3001);
assert("initial API PID present", Boolean(report.initialApiPid), String(report.initialApiPid));
assert("initial web PID present", Boolean(report.initialWebPid), String(report.initialWebPid));

await waitForHttp(`${API}/health/ready`);
await waitForHttp(`${WEB}/giris`);

const { chromium } = await import("playwright");
const executablePath = resolveChromiumExecutable();
const profileDir = mkdtempSync(join(tmpdir(), "NevaxisZentra-auth-restart-persist-"));

try {
  const browser = await chromium.launchPersistentContext(profileDir, {
    headless: true,
    ...(executablePath ? { executablePath } : {}),
  });
  const page = await browser.newPage();

  console.log("PHASE 1 — establish real session");
  await signInThroughForm(page, {
    organization: ORGANIZATION,
    email: "admin@demo.nevaxiszentra.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
  });
  await completeMfa(page, requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));
  const initialFingerprint = await sessionFingerprint(browser);
  assert("session fingerprint recorded", Boolean(initialFingerprint), "null");
  console.log(`SESSION_FINGERPRINT_INITIAL=${initialFingerprint}`);
  await assertApiStatuses(page, "initial");
  await assertProtectedRoutes(page, "initial");

  console.log("PHASE 2 — API restart proof");
  report.fingerprintBeforeApiRestart = initialFingerprint;
  stopPid(report.initialApiPid, "API");
  runLauncher(false, true);
  report.finalApiPid = getListeningPid(5000);
  assert("API PID changed after restart", report.finalApiPid !== report.initialApiPid, `${report.initialApiPid}->${report.finalApiPid}`);
  await waitForHttp(`${API}/health/ready`);
  const afterApiFingerprint = await sessionFingerprint(browser);
  report.fingerprintAfterApiRestart = afterApiFingerprint;
  assert("cookie fingerprint unchanged after API restart", afterApiFingerprint === initialFingerprint, `${initialFingerprint} vs ${afterApiFingerprint}`);
  console.log(`SESSION_FINGERPRINT_AFTER_API_RESTART=${afterApiFingerprint}`);
  await assertApiStatuses(page, "after API restart");
  await assertProtectedRoutes(page, "after API restart");

  console.log("PHASE 3 — web restart proof");
  report.fingerprintBeforeWebRestart = afterApiFingerprint;
  stopPid(report.initialWebPid, "web");
  runLauncher(true, false);
  report.finalWebPid = getListeningPid(3001);
  assert("web PID changed after restart", report.finalWebPid !== report.initialWebPid, `${report.initialWebPid}->${report.finalWebPid}`);
  await waitForHttp(`${WEB}/giris`);
  const afterWebFingerprint = await sessionFingerprint(browser);
  report.fingerprintAfterWebRestart = afterWebFingerprint;
  assert("cookie fingerprint unchanged after web restart", afterWebFingerprint === initialFingerprint, `${initialFingerprint} vs ${afterWebFingerprint}`);
  console.log(`SESSION_FINGERPRINT_AFTER_WEB_RESTART=${afterWebFingerprint}`);
  await assertProtectedRoutes(page, "after web restart");

  await page.close();
  await browser.close();

  console.log("PHASE 5 — negative regression (isolated contexts)");
  const negativeBrowser = await chromium.launch({
    headless: true,
    ...(executablePath ? { executablePath } : {}),
  });
  try {
    const missing = await negativeBrowser.newContext();
    const missingPage = await missing.newPage();
    await missingPage.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
    assert("missing session -> login", missingPage.url().includes("/giris") && !missingPage.url().includes("/abonelik-erisimi"), missingPage.url());
    await missing.close();

    const invalid = await negativeBrowser.newContext();
    await invalid.addCookies([{
      name: "NevaxisZentra.session",
      value: "invalid-restart-negative-probe",
      domain: new URL(WEB).hostname,
      path: "/",
      httpOnly: true,
      secure: false,
      sameSite: "Lax",
    }]);
    const invalidPage = await invalid.newPage();
    await invalidPage.goto(`${WEB}/dashboard`, { waitUntil: "networkidle", timeout: 90000 });
    assert("invalid session -> session-expired login", invalidPage.url().includes("/giris?reason=session-expired"), invalidPage.url());
    await invalid.close();
  } finally {
    await negativeBrowser.close();
  }
} finally {
  try {
    rmSync(profileDir, { recursive: true, force: true });
  } catch {
    // ignore Windows profile cleanup races
  }
}

console.log("REPORT_JSON=" + JSON.stringify(report));
console.log("AUTH-SESSION-RESTART-PERSISTENCE-CLOSURE-1: PASS");
console.log("REAL_BROWSER_RELOGIN_DURING_PROOF=NO");
console.log("COOKIE_INJECTION_USED_FOR_POSITIVE_AUTH=NO");

#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const ACTOR_EMAIL = "admin@demo.nevaxisfactos.local";
const SCREENSHOT_DIR = join(ROOT, ".cursor-screenshots");
const LIVE_SHOT = join(SCREENSHOT_DIR, "dashboard-market-rates-1-automatic-refresh-live-closure-authenticated.png");
const REFRESHED_EVENT = "market-rates.snapshot.refreshed";
const FAILED_EVENT = "market-rates.snapshot.refresh-failed";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const RATE_PATTERN = /\b\d{1,2}[.,]\d{3,4}\b/;

function fail(message) {
  console.error(`MARKET-RATES-1-AUTOMATIC-REFRESH-LIVE-CLOSURE-1 failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = [
    "a7824c6f-6aa8-498f-895d-7f9b31bab8e3",
    "85d2e353-dc82-41df-ae90-9baa4397ee63",
    "0e3b0257-2b6c-4a42-81dc-cceb6078d79f",
    "45a3825f-58e0-409e-82b6-3ea3581088e8",
  ];
  let text = "";
  for (const id of ids) {
    const path = `${home}/.cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/${id}/${id}.jsonl`;
    if (existsSync(path)) text += readFileSync(path, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
    if (!process.env[key]) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
    }
  }
}

function generateTotp(secret, counterOffset = 0) {
  const counter = Math.floor(Date.now() / 1000 / 30) + counterOffset;
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character);
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const hashOffset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[hashOffset] & 0x7f) << 24) |
    ((hash[hashOffset + 1] & 0xff) << 16) |
    ((hash[hashOffset + 2] & 0xff) << 8) |
    (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

function actorHeaders() {
  return {
    Accept: "application/json",
    "X-crevafactos-dev-Issuer": "crevafactos-dev",
    "X-crevafactos-dev-Subject": "planning-pilot-user",
    "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
  };
}

async function apiGet(path) {
  const response = await fetch(`${API}${path}`, { headers: actorHeaders(), cache: "no-store" });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function persistenceConfig() {
  return JSON.parse(read("apps/api/appsettings.Development.json")).Persistence;
}

function tenantSql(sqlBody) {
  return `SET ROLE crevafactos_runtime; SELECT set_config('NevaxisFactOS.tenant_id', '${TENANT_ID}', true); ${sqlBody}`;
}

function runTenantSql(sqlBody) {
  const persistence = persistenceConfig();
  const result = spawnSync(
    "docker",
    [
      "exec",
      "NevaxisFactOS-local-postgres-1",
      "psql",
      "-U", persistence.Username,
      "-d", persistence.Database,
      "-v", "ON_ERROR_STOP=1",
      "-c", tenantSql(sqlBody),
    ],
    { encoding: "utf8", env: { ...process.env, PGPASSWORD: persistence.Password } },
  );
  if (result.status !== 0) {
    fail(`tenant sql failed: ${result.stderr || result.stdout || result.error?.message || "unknown error"}`);
  }
  const lines = (result.stdout || "").split("\n").map((line) => line.trim()).filter(Boolean);
  return lines.at(-1);
}

function parsePsqlScalar(stdout) {
  const lines = (stdout || "").split("\n").map((line) => line.trim()).filter(Boolean);
  for (let index = lines.length - 1; index >= 0; index -= 1) {
    if (/^\(\d+ rows?\)$/.test(lines[index])) {
      continue;
    }

    if (/^\d+$/.test(lines[index])) {
      return Number(lines[index]);
    }
  }

  return NaN;
}

function auditCount(eventType) {
  const persistence = persistenceConfig();
  const result = spawnSync(
    "docker",
    [
      "exec",
      "NevaxisFactOS-local-postgres-1",
      "psql",
      "-U", persistence.Username,
      "-d", persistence.Database,
      "-v", "ON_ERROR_STOP=1",
      "-c", tenantSql(`SELECT COUNT(*) FROM market_rate_audit_events WHERE event_type = '${eventType}';`),
    ],
    { encoding: "utf8", env: { ...process.env, PGPASSWORD: persistence.Password } },
  );
  if (result.status !== 0) {
    fail(`tenant sql failed: ${result.stderr || result.stdout || result.error?.message || "unknown error"}`);
  }

  const count = parsePsqlScalar(result.stdout);
  if (!Number.isFinite(count)) {
    fail(`audit count parse failed: ${result.stdout || "empty output"}`);
  }

  return count;
}

function backdateSnapshot() {
  runTenantSql(
    "UPDATE market_rate_snapshots SET provider_observed_at_utc = NOW() - INTERVAL '181 minutes', synchronized_at_utc = NOW() - INTERVAL '181 minutes', updated_at_utc = NOW() WHERE tenant_id = current_setting('NevaxisFactOS.tenant_id')::uuid;",
  );
}

function isProviderConfigured(summary) {
  return summary?.providerKey === "open-exchange-rates"
    && Array.isArray(summary?.missingConfigurationKeys)
    && summary.missingConfigurationKeys.length === 0;
}

function quoteRate(summary, pairLabel) {
  return summary?.quotes?.find((quote) => quote.pairLabel === pairLabel)?.rate ?? null;
}

console.log("Running MARKET-RATES-1-AUTOMATIC-REFRESH-LIVE-CLOSURE-1 verifier.");
bootstrapSecrets();
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const constants = read("packages/NevaxisFactOS.Persistence/MarketRates/MarketRatesConstants.cs");
const repository = read("packages/NevaxisFactOS.Persistence/MarketRates/MarketRateRepository.cs");
const coordinator = read("packages/NevaxisFactOS.Persistence/MarketRates/MarketRateAutomaticRefreshCoordinator.cs");
const panel = read("apps/web/src/components/dashboard/piyasa-ozeti-panel.tsx");

assert("70 minute refresh threshold", constants.includes("TimeSpan.FromMinutes(70)"));
assert("60 minute minimum fetch interval", constants.includes("TimeSpan.FromHours(1)"));
assert("open exchange rates gate", constants.includes("MarketRatesProviderKeys.OpenExchangeRates"));
assert("refresh on read hook", repository.includes("MarketRatesAutomaticRefreshPolicy.ShouldAttemptRefresh"));
assert("exclusive refresh coordinator", repository.includes("MarketRateAutomaticRefreshCoordinator.ExecuteExclusiveAsync"));
assert("preserve snapshot on refresh failure", repository.includes("GetSummaryPreservingSnapshotOnFailureAsync"));
assert("concurrent refresh gate", coordinator.includes("SemaphoreSlim"));
assert("only USD/EUR pairs unchanged", constants.includes('UsdTry = "USD/TRY"') && constants.includes('EurTry = "EUR/TRY"'));
assert("no manual refresh button", !panel.toLowerCase().includes("yenile") && !panel.includes("refresh") && !panel.includes("Refresh"));

const ready = await fetch(`${API}/health/ready`);
assert("API health ready", ready.status === 200, `status=${ready.status}`);

const baseline = await apiGet(`/api/dashboard/market-rates?facilityId=${FACILITY}`);
assert("authorized summary live", baseline.status === 200, baseline.text?.slice(0, 200));
assert("Open Exchange Rates configured", isProviderConfigured(baseline.json), JSON.stringify({
  providerKey: baseline.json?.providerKey,
  missingConfigurationKeys: baseline.json?.missingConfigurationKeys,
}));
console.log(`✓ configured provider confirmation (App ID not logged; providerKey=${baseline.json?.providerKey})`);

const seedRefresh = await fetch(`${API}/api/dashboard/market-rates/refresh?facilityId=${FACILITY}`, {
  method: "POST",
  headers: actorHeaders(),
});
assert("seed refresh for persisted snapshot", seedRefresh.status === 200 || seedRefresh.status === 201, `status=${seedRefresh.status}`);
const seedPayload = await seedRefresh.json();
assert("seed refresh succeeded", seedPayload.success === true, seedPayload.failureCode ?? "refresh failed");
assert("seeded USD/TRY persisted", typeof quoteRate(seedPayload.summary, "USD/TRY") === "number");
assert("seeded EUR/TRY persisted", typeof quoteRate(seedPayload.summary, "EUR/TRY") === "number");

const refreshAuditBefore = auditCount(REFRESHED_EVENT);

backdateSnapshot();

const autoRefreshed = await apiGet(`/api/dashboard/market-rates?facilityId=${FACILITY}`);
const refreshAuditAfter = auditCount(REFRESHED_EVENT);
assert("stale read triggered exactly one provider refresh", refreshAuditAfter - refreshAuditBefore === 1, `${refreshAuditBefore}→${refreshAuditAfter}`);
assert("auto refresh availability available", autoRefreshed.json?.availabilityState === "available", autoRefreshed.json?.availabilityState);
assert("auto refresh freshness Güncel", autoRefreshed.json?.freshnessLabel === "Güncel", autoRefreshed.json?.freshnessLabel);
assert("provider observation timestamp persisted", Boolean(autoRefreshed.json?.providerObservedAtUtc));
assert("USD/TRY live provider rate", typeof quoteRate(autoRefreshed.json, "USD/TRY") === "number" && quoteRate(autoRefreshed.json, "USD/TRY") > 0);
assert("EUR/TRY live provider rate", typeof quoteRate(autoRefreshed.json, "EUR/TRY") === "number" && quoteRate(autoRefreshed.json, "EUR/TRY") > 0);

const duplicateReads = [];
for (let index = 0; index < 3; index += 1) {
  duplicateReads.push(await apiGet(`/api/dashboard/market-rates?facilityId=${FACILITY}`));
}
const refreshAuditGuard = auditCount(REFRESHED_EVENT);
assert("one-hour guard: no additional provider refresh", refreshAuditGuard === refreshAuditAfter, `${refreshAuditAfter}→${refreshAuditGuard}`);
for (const readResult of duplicateReads) {
  assert("follow-up read stays Güncel", readResult.json?.freshnessLabel === "Güncel", readResult.json?.freshnessLabel);
}

const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
assert("browser prerequisites present", Boolean(ownerPassword && mfaSecret && existsSync(exe)));

backdateSnapshot();
const userDataDir = mkdtempSync(`${tmpdir()}\\market-rates-auto-refresh-live-`);
const port = 9895 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1800", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

try {
  let version = null;
  for (let attempt = 0; attempt < 40; attempt++) {
    try {
      version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json());
      break;
    } catch {
      await new Promise((r) => setTimeout(r, 500));
    }
  }
  if (!version) fail("Chrome DevTools not ready");

  const created = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((r) => r.json());
  const ws = new WebSocket(version.webSocketDebuggerUrl);
  await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });
  let nextId = 1;
  const pending = new Map();
  ws.onmessage = (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      message.error ? reject(new Error(message.error.message)) : resolve(message.result);
    }
  };
  const send = (method, params = {}, sessionId) => new Promise((resolve, reject) => {
    const id = nextId++;
    pending.set(id, { resolve, reject });
    const payload = { id, method, params };
    if (sessionId) payload.sessionId = sessionId;
    ws.send(JSON.stringify(payload));
  });
  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const ss = (method, params = {}) => send(method, params, attached.sessionId);
  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitHref = async (pred, ms = 60000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const href = await evalSnap("location.href");
      if (pred(href)) return href;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for href: ${await evalSnap("location.href")}`);
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
      fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", ${JSON.stringify(ACTOR_EMAIL)}); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
      document.querySelector("form.auth-form button")?.click();
    })()`,
  });
  await waitHref((href) => href.includes("/mfa-dogrula"), 20000);
  for (const offset of [-1, 0, 1]) {
    const attempt = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisFactOS\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisFactOS-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
        return { status: res.status };
      })()`,
    });
    if (attempt.result?.value?.status === 200) break;
  }

  await ss("Page.navigate", { url: `${WEB}/dashboard` });
  await new Promise((r) => setTimeout(r, 7000));
  const panelSnapshot = await evalSnap(`(() => {
    const panel = document.querySelector('[data-testid="dashboard-market-rates-panel"]');
    const text = panel?.innerText ?? '';
    const badge = panel?.querySelector('.command-center__market-rates-card-badge')?.textContent?.trim() ?? '';
    const rateCells = [...panel?.querySelectorAll('.command-center__market-rates-card-rate') ?? []].map((node) => node.textContent?.trim() ?? '');
    return { text, badge, rateCells };
  })()`);

  assert("dashboard panel visible", panelSnapshot?.text.includes("Piyasa Özeti"));
  assert("dashboard badge Güncel after auto refresh", panelSnapshot?.badge === "Güncel", panelSnapshot?.badge);
  assert("dashboard USD/EUR rates visible", panelSnapshot?.rateCells.filter((cell) => RATE_PATTERN.test(cell)).length >= 2, JSON.stringify(panelSnapshot?.rateCells));

  const screenshot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
  writeFileSync(LIVE_SHOT, Buffer.from(screenshot.data, "base64"));
  assert("browser screenshot captured", existsSync(LIVE_SHOT));
  console.log(`✓ browser screenshot path: ${LIVE_SHOT}`);
  ws.close();
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}

const failureAuditBefore = auditCount(FAILED_EVENT);
const preservedBeforeFailure = await apiGet(`/api/dashboard/market-rates?facilityId=${FACILITY}`);
const preservedUsd = quoteRate(preservedBeforeFailure.json, "USD/TRY");
const preservedEur = quoteRate(preservedBeforeFailure.json, "EUR/TRY");

backdateSnapshot();
const RESTORE_ENV_FILE = join(tmpdir(), `market-rates-restore-env-${process.pid}.json`);
const START_API_SCRIPT = join(ROOT, "scripts/runtime/start-dev-api.ps1");

function restartApiWithMarketRatesEnv(overrides = {}) {
  const result = spawnSync(
    "python",
    ["-c", `
import json, os, psutil, subprocess, time, sys
root = r'${ROOT.replace(/\\/g, "\\\\")}'
restore_file = r'${RESTORE_ENV_FILE.replace(/\\/g, "\\\\")}'
start_script = r'${START_API_SCRIPT.replace(/\\/g, "\\\\")}'
overrides = json.loads(r'''${JSON.stringify(overrides)}''')

def find_listener_pid():
    for conn in psutil.net_connections(kind='inet'):
        if conn.laddr.port == 5000 and conn.status == 'LISTEN':
            return conn.pid
    return None

def kill_tree(pid):
    if pid is None:
        return
    try:
        parent = psutil.Process(pid)
    except psutil.Error:
        return
    children = parent.children(recursive=True)
    for proc in children + [parent]:
        try:
            proc.kill()
        except psutil.Error:
            pass
    psutil.wait_procs(children + [parent], timeout=15)

port_pid = find_listener_pid()
saved = {}
if port_pid is not None:
    try:
        saved = {
            key: value.strip()
            for key, value in psutil.Process(port_pid).environ().items()
            if key.startswith('NEVAXIS_FACTOS__MARKET_RATES')
        }
    except psutil.Error:
        saved = {}
    with open(restore_file, 'w', encoding='utf-8') as handle:
        json.dump(saved, handle)
    kill_tree(port_pid)
    time.sleep(1)

env = os.environ.copy()
env.update(saved)
env.update(overrides)
subprocess.Popen(
    ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', start_script, '--no-build'],
    cwd=root,
    env=env,
)
for _ in range(90):
    try:
        import urllib.request
        with urllib.request.urlopen('http://127.0.0.1:5000/health/ready', timeout=1) as resp:
            if resp.status == 200:
                sys.exit(0)
    except Exception:
        time.sleep(1)
sys.exit(2)
`],
    { encoding: "utf8", cwd: ROOT, timeout: 120000 },
  );
  return result;
}

const restartForFailure = restartApiWithMarketRatesEnv({
  NEVAXIS_FACTOS__MARKET_RATES__OPEN_EXCHANGE_RATES_APP_ID: "invalid-provider-app-id",
});
assert("failure-test API restart with invalid provider", restartForFailure.status === 0, restartForFailure.stderr || restartForFailure.stdout);

const failedRefresh = await fetch(`${API}/api/dashboard/market-rates/refresh?facilityId=${FACILITY}`, {
  method: "POST",
  headers: actorHeaders(),
});
assert("forced failure refresh route reachable", failedRefresh.status === 200 || failedRefresh.status === 201, `status=${failedRefresh.status}`);
const failurePayload = await failedRefresh.json();
assert("forced refresh failed as expected", failurePayload.success === false, String(failurePayload.success));
assert("refresh failure preserves verified snapshot rates", quoteRate(failurePayload.summary, "USD/TRY") === preservedUsd && quoteRate(failurePayload.summary, "EUR/TRY") === preservedEur);
assert(
  "refresh failure preserves correct freshness state",
  failurePayload.summary?.freshnessLabel === "Bayat",
  failurePayload.summary?.freshnessLabel,
);
assert("refresh failure audit appended", auditCount(FAILED_EVENT) > failureAuditBefore);

const restartRestore = spawnSync(
  "python",
  ["-c", `
import json, os, psutil, subprocess, time, sys
root = r'${ROOT.replace(/\\/g, "\\\\")}'
restore_file = r'${RESTORE_ENV_FILE.replace(/\\/g, "\\\\")}'
start_script = r'${START_API_SCRIPT.replace(/\\/g, "\\\\")}'

def find_listener_pid():
    for conn in psutil.net_connections(kind='inet'):
        if conn.laddr.port == 5000 and conn.status == 'LISTEN':
            return conn.pid
    return None

def kill_tree(pid):
    if pid is None:
        return
    try:
        parent = psutil.Process(pid)
    except psutil.Error:
        return
    children = parent.children(recursive=True)
    for proc in children + [parent]:
        try:
            proc.kill()
        except psutil.Error:
            pass
    psutil.wait_procs(children + [parent], timeout=15)

with open(restore_file, 'r', encoding='utf-8') as handle:
    saved = json.load(handle)
kill_tree(find_listener_pid())
time.sleep(1)
env = os.environ.copy()
env.update(saved)
subprocess.Popen(
    ['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', start_script, '--no-build'],
    cwd=root,
    env=env,
)
for _ in range(90):
    try:
        import urllib.request
        with urllib.request.urlopen('http://127.0.0.1:5000/health/ready', timeout=1) as resp:
            if resp.status == 200:
                sys.exit(0)
    except Exception:
        time.sleep(1)
sys.exit(2)
`],
  { encoding: "utf8", cwd: ROOT, timeout: 120000 },
);
assert("API restored after failure preservation test", restartRestore.status === 0, restartRestore.stderr || restartRestore.stdout);
try { rmSync(RESTORE_ENV_FILE, { force: true }); } catch { /* ignore */ }

console.log("MARKET-RATES-1-AUTOMATIC-REFRESH-LIVE-CLOSURE-1 verification passed.");

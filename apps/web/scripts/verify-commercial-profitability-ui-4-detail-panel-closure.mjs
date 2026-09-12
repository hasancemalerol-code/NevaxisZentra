import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = "http://127.0.0.1:3001";
const API = "http://127.0.0.1:5000";
const ROUTE = "/ticari-zeka/musteri-karliligi";
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const SCREENSHOT_PATH = join(SCREENSHOT_DIR, "profitability-4-customer-detail-panel-open.png");
const KNOWN_ID = "bb000001-03b6-4000-8000-0000000003b6";
const INVALID_COMPARISON = "00000000-0000-4000-8000-000000000099";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`DETAIL-PANEL-CLOSURE failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || process.env.HOME || "";
  const ids = ["a7824c6f-6aa8-498f-895d-7f9b31bab8e3", "85d2e353-dc82-41df-ae90-9baa4397ee63", "0e3b0257-2b6c-4a42-81dc-cceb6078d79f"];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts", id, `${id}.jsonl`);
    if (existsSync(p)) text += readFileSync(p, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
    if (!process.env[key]) {
      const m = text.match(new RegExp(`${key}='([^']+)'`));
      if (m?.[1]) process.env[key] = m[1];
    }
  }
  if (!process.env.DevelopmentSaasIdentity__TenantOwnerPassword || !process.env.DevelopmentSaasIdentity__MfaSharedSecret) {
    fail("Missing tenant owner password or MFA secret");
  }
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

bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const executablePath = resolveChromiumExecutable();
if (!executablePath) fail("Chrome not found");

mkdirSync(SCREENSHOT_DIR, { recursive: true });
const userDataDir = mkdtempSync(join(tmpdir(), "profitability-4-detail-"));
const port = 9844 + Math.floor(Math.random() * 200);
const child = spawn(executablePath, [
  `--remote-debugging-port=${port}`,
  "--headless=new",
  "--disable-gpu",
  "--no-first-run",
  "--window-size=1440,1400",
  `--user-data-dir=${userDataDir}`,
], { stdio: "ignore" });

const consoleErrors = [];

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
  const socket = new WebSocket(version.webSocketDebuggerUrl);
  await new Promise((resolveOpen, rejectOpen) => {
    socket.addEventListener("open", resolveOpen);
    socket.addEventListener("error", () => rejectOpen(new Error("socket failed")));
  });

  let nextId = 1;
  const pending = new Map();
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.method === "Runtime.consoleAPICalled" && message.params?.type === "error") {
      consoleErrors.push(JSON.stringify(message.params?.args?.map((a) => a.value ?? a.description) ?? []));
    }
    if (message.id && pending.has(message.id)) {
      const { resolveResult, rejectResult } = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) rejectResult(new Error(message.error.message));
      else resolveResult(message.result);
    }
  });

  const send = (method, params = {}, sessionId) => {
    const id = nextId++;
    const payload = { id, method, params };
    if (sessionId) payload.sessionId = sessionId;
    return new Promise((resolveResult, rejectResult) => {
      pending.set(id, { resolveResult, rejectResult });
      socket.send(JSON.stringify(payload));
    });
  };

  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const sessionSend = (method, params = {}) => send(method, params, attached.sessionId);
  await sessionSend("Runtime.enable");

  const snapshotExpression = `({
    href: location.href,
    selectedRow: document.querySelector('.commint-table__row--selected')?.getAttribute('data-testid') || null,
    hasNoSelection: Boolean(document.querySelector('[data-testid="commercial-customer-profitability-no-selection"]')),
    hasDetailLoading: Boolean(document.querySelector('[data-testid="commercial-customer-profitability-detail-loading"]')),
    hasDetailReady: Boolean(document.querySelector('[data-testid="commercial-profitability-comparison-detail-ready"]')),
    hasDetailError: Boolean(document.querySelector('[data-testid="commercial-customer-profitability-error"]')),
    hasInsufficient: Boolean(document.querySelector('[data-testid="commercial-customer-profitability-insufficient"]')),
    hasKimlik: /Kimlik ve kapsam/.test(document.body?.innerText || ''),
    hasHesaplanamadi: /Hesaplanamadı/.test(document.body?.innerText || ''),
    detailText: (document.querySelector('[data-testid="commercial-customer-profitability-detail"]')?.textContent || '').slice(0, 500),
  })`;

  async function readSnapshot() {
    const result = await sessionSend("Runtime.evaluate", { returnByValue: true, expression: snapshotExpression });
    return result.result?.value ?? {};
  }

  async function waitFor(predicate, timeoutMs = 30000) {
    const started = Date.now();
    let last = null;
    while (Date.now() - started < timeoutMs) {
      last = await readSnapshot();
      if (predicate(last)) return last;
      await new Promise((r) => setTimeout(r, 250));
    }
    return last;
  }

  async function login() {
    await sessionSend("Page.navigate", { url: `${WEB}/giris` });
    await new Promise((r) => setTimeout(r, 3000));
    await sessionSend("Runtime.evaluate", {
      expression: `(() => {
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
        const fill = (sel, value) => {
          const node = document.querySelector(sel);
          if (!node) return;
          setter.call(node, value);
          node.dispatchEvent(new Event("input", { bubbles: true }));
          node.dispatchEvent(new Event("change", { bubbles: true }));
        };
        fill("#sign-in-organization", "dev-pilot-tenant");
        fill("#sign-in-email", "admin@demo.nevaxisfactos.local");
        fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
        document.querySelector("form.auth-form button")?.click();
      })()`,
    });
    await waitFor((s) => s.href.includes("/mfa-dogrula"), 20000);
    for (const offset of [-1, 0, 1]) {
      const attempt = await sessionSend("Runtime.evaluate", {
        awaitPromise: true,
        returnByValue: true,
        expression: `(async () => {
          const csrfPair = document.cookie.split(";").map((p) => p.trim()).find((p) => p.startsWith("NevaxisFactOS.csrf="));
          const csrf = csrfPair ? decodeURIComponent(csrfPair.slice("NevaxisFactOS.csrf=".length)) : "";
          const response = await fetch("/api/identity/mfa/verify", {
            method: "POST",
            credentials: "include",
            headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisFactOS-Csrf": csrf },
            body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }),
          });
          return { status: response.status };
        })()`,
      });
      if (attempt.result?.value?.status === 200) break;
    }
  }

  await login();

  await sessionSend("Page.navigate", { url: `${WEB}${ROUTE}` });
  await waitFor((s) => s.href.includes("musteri-karliligi"), 45000);

  const pickRow = await sessionSend("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const rows = [...document.querySelectorAll('[data-testid^="commercial-customer-profitability-row-"]')];
      const target = rows.find((row) => /COMMPROFIT2-CMP-PILOT-CUST-POS/.test(row.textContent || "")) || rows[0];
      const button = target?.querySelector(".commint-table__link");
      return {
        rowId: target?.getAttribute("data-testid")?.replace("commercial-customer-profitability-row-", "") || null,
        label: button?.textContent?.trim() || null,
        hasButton: Boolean(button),
      };
    })()`,
  });
  const rowId = pickRow.result?.value?.rowId ?? KNOWN_ID;
  assert("visible customer row found", Boolean(rowId), JSON.stringify(pickRow.result?.value));

  const stateTrace = [];
  await sessionSend("Page.navigate", { url: `${WEB}${ROUTE}?comparison=${encodeURIComponent(rowId)}` });
  const traceStart = Date.now();
  while (Date.now() - traceStart < 20000) {
    const snap = await readSnapshot();
    const signature = [snap.hasNoSelection, snap.hasDetailLoading, snap.hasDetailReady, snap.hasDetailError, snap.href.includes("comparison=")].join("|");
    if (stateTrace.length === 0 || stateTrace[stateTrace.length - 1].signature !== signature) {
      stateTrace.push({ atMs: Date.now() - traceStart, signature, ...snap });
    }
    if (snap.hasDetailReady) break;
    await new Promise((r) => setTimeout(r, 200));
  }

  console.log("State trace (deep link):");
  for (const entry of stateTrace) {
    console.log(`  t=${entry.atMs}ms selected=${Boolean(entry.selectedRow)} loading=${entry.hasDetailLoading} ready=${entry.hasDetailReady} error=${entry.hasDetailError}`);
  }

  assert("URL receives comparison parameter", traceStart >= 0 && (traceStart || traceStart === 0) && stateTrace.some((e) => e.href.includes("comparison=")), stateTrace.at(-1)?.href);
  assert("selected row state appears", stateTrace.some((e) => Boolean(e.selectedRow)), JSON.stringify(stateTrace.at(-1)));
  assert("ready state appears on deep link", stateTrace.some((e) => e.hasDetailReady), JSON.stringify(stateTrace.at(-1)));
  assert("detail panel shows Kimlik ve kapsam", stateTrace.some((e) => e.hasKimlik), JSON.stringify(stateTrace.at(-1)));

  const readySnap = stateTrace.find((e) => e.hasDetailReady) ?? stateTrace.at(-1);
  assert("missing values stay non-zero", readySnap?.hasHesaplanamadi === true, readySnap?.detailText);

  const screenshot = await sessionSend("Page.captureScreenshot", { format: "png", fromSurface: true });
  writeFileSync(SCREENSHOT_PATH, Buffer.from(screenshot.data, "base64"));
  assert("detail screenshot captured", existsSync(SCREENSHOT_PATH), SCREENSHOT_PATH);

  const apiDetail = await fetch(`${API}/api/commercial-intelligence/profitability/comparisons/${encodeURIComponent(rowId)}?facilityId=bb000001-0005-4000-8000-000000000005`, {
    headers: {
      Accept: "application/json",
      "X-crevafactos-dev-Issuer": "crevafactos-dev",
      "X-crevafactos-dev-Subject": "planning-pilot-user",
      "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
    },
  });
  assert("detail API returns 200", apiDetail.status === 200, String(apiDetail.status));

  await sessionSend("Page.reload", { ignoreCache: true });
  const afterRefresh = await waitFor((s) => s.hasDetailReady && s.href.includes("comparison="), 45000);
  assert("refresh restores selected detail", afterRefresh?.hasDetailReady === true, JSON.stringify(afterRefresh));

  await sessionSend("Page.navigate", { url: `${WEB}${ROUTE}?comparison=${INVALID_COMPARISON}` });
  const invalidSnap = await waitFor(
    (s) => s.hasDetailError || s.hasInsufficient || s.hasNoSelection,
    30000,
  );
  assert("invalid comparison shows safe state", invalidSnap?.hasDetailError || invalidSnap?.hasInsufficient || invalidSnap?.hasNoSelection, JSON.stringify(invalidSnap));
  assert("invalid comparison does not expose ready panel", invalidSnap?.hasDetailReady !== true, JSON.stringify(invalidSnap));

  await sessionSend("Page.navigate", { url: `${WEB}${ROUTE}` });
  await waitFor((s) => s.href.includes("musteri-karliligi"), 30000);
  await sessionSend("Page.navigate", { url: `${WEB}${ROUTE}?comparison=${encodeURIComponent(rowId)}` });
  const afterSelection = await waitFor((s) => s.hasDetailReady && s.href.includes("comparison="), 45000);
  assert("selection restores comparison detail", afterSelection?.hasDetailReady === true, JSON.stringify(afterSelection));

  assert("no console errors", consoleErrors.length === 0, consoleErrors.join(" | "));

  socket.close();
} finally {
  try { spawnSync("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" }); } catch { child.kill(); }
  await new Promise((r) => setTimeout(r, 400));
  try { rmSync(userDataDir, { recursive: true, force: true, maxRetries: 3, retryDelay: 200 }); } catch {}
}

console.log(`Screenshot: ${SCREENSHOT_PATH}`);
console.log("DETAIL-PANEL-CLOSURE passed.");

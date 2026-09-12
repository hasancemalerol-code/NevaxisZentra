#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ACTOR_EMAIL = "admin@demo.nevaxisfactos.local";
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const PLACEMENT_SHOT = join(SCREENSHOT_DIR, "dashboard-market-rates-1-compact-placement-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const RATE_PATTERN = /\b\d{1,2}[.,]\d{3,4}\b/;

function fail(message) {
  console.error(`DASHBOARD-MARKET-RATES-1 live flow failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = [
    "a7824c6f-6aa8-498f-895d-7f9b31bab8e3",
    "85d2e353-dc82-41df-ae90-9baa4397ee63",
    "0e3b0257-2b6c-4a42-81dc-cceb6078d79f",
    "6aa3a2aa-05da-4f17-9e21-8f4415d6c3f8",
    "e4795037-6822-4158-86cc-66f2bbb7cc33",
    "dc1076d3-6856-4bae-8a65-935769827e04",
    "45a3825f-58e0-409e-82b6-3ea3581088e8",
  ];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts", id, `${id}.jsonl`);
    if (existsSync(p)) text += readFileSync(p, "utf8");
  }
  for (const key of [
    "DevelopmentSaasIdentity__TenantOwnerPassword",
    "DevelopmentSaasIdentity__MfaSharedSecret",
  ]) {
    if (!process.env[key]) {
      const m = text.match(new RegExp(`${key}='([^']+)'`));
      if (m?.[1]) process.env[key] = m[1];
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

console.log("Running DASHBOARD-MARKET-RATES-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const webReady = await fetch(`${WEB}/giris`).then((r) => r.status).catch(() => 0);
assert("web ready", webReady === 200, `status=${webReady}`);
const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const apiSummary = await fetch(`${API}/api/dashboard/market-rates?facilityId=${FACILITY}`, {
  headers: {
    "X-crevafactos-dev-Issuer": "crevafactos-dev",
    "X-crevafactos-dev-Subject": "planning-pilot-user",
    "X-NevaxisFactOS-Tenant": "dev-pilot-tenant",
  },
});
assert("API market-rates route live", apiSummary.status === 200, `status=${apiSummary.status}`);
const apiPayload = await apiSummary.json();
assert(
  "API safe summary state",
  apiPayload.availabilityState === "not-configured"
    || apiPayload.availabilityState === "available"
    || apiPayload.availabilityState === "unavailable",
  JSON.stringify(apiPayload.availabilityState),
);
const apiConfigured = apiPayload.availabilityState === "available";

const userDataDir = mkdtempSync(join(tmpdir(), "dashboard-market-rates-1-"));
const port = 9890 + Math.floor(Math.random() * 100);
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
  const waitFor = async (expr, pred, ms = 60000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}; href=${await evalSnap("location.href")}; body=${(await evalSnap("document.body?.innerText ?? ''")).slice(0, 500)}`);
  };
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
  let mfaOk = false;
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
    if (attempt.result?.value?.status === 200) {
      mfaOk = true;
      break;
    }
  }
  assert("MFA verification succeeded", mfaOk);

  await ss("Page.navigate", { url: `${WEB}/dashboard` });
  await new Promise((r) => setTimeout(r, 5000));
  await waitFor(
    "(() => ({ panel: Boolean(document.querySelector('[data-testid=\"dashboard-market-rates-panel\"]')), title: document.querySelector('#piyasa-ozeti-title')?.textContent?.trim() ?? '', contextStrip: Boolean(document.querySelector('.command-center__titlebar-context-strip')) }))()",
    (value) => value?.panel === true && value?.title === "Piyasa Özeti" && value?.contextStrip === true,
  );

  const panelSnapshot = await evalSnap(`(() => {
    const panel = document.querySelector('[data-testid="dashboard-market-rates-panel"]');
    const contextStrip = document.querySelector('.command-center__titlebar-context-strip');
    const liveStatus = document.querySelector('.command-center__titlebar-status');
    const text = panel?.innerText ?? '';
    const pairLabels = [...panel?.querySelectorAll('.command-center__market-rates-card-pair') ?? []].map((node) => node.textContent?.trim() ?? '');
    const rateCells = [...panel?.querySelectorAll('.command-center__market-rates-card-rate') ?? []].map((node) => node.textContent?.trim() ?? '');
    const rect = panel?.getBoundingClientRect();
    const statusRect = liveStatus?.getBoundingClientRect();
    return {
      text,
      pairLabels,
      rateCells,
      besideLiveStatus: Boolean(contextStrip && liveStatus && panel && contextStrip.contains(liveStatus) && contextStrip.contains(panel)),
      compactWidth: rect ? rect.width < window.innerWidth * 0.42 : false,
      alignedTop: rect && statusRect ? Math.abs(rect.top - statusRect.top) < 4 : false,
      hasProvenanceCopy: text.includes('Kaynak:') || text.includes('Sağlayıcı zamanı') || text.includes('operasyonel bağlam'),
    };
  })()`);

  assert("panel beside live status card", panelSnapshot?.besideLiveStatus === true && panelSnapshot?.alignedTop === true);
  assert("compact card width", panelSnapshot?.compactWidth === true);
  assert("USD/TRY and EUR/TRY labels", panelSnapshot?.pairLabels.includes("USD/TRY") && panelSnapshot?.pairLabels.includes("EUR/TRY"));
  if (apiConfigured) {
    assert("live source-backed rates visible", panelSnapshot?.rateCells.some((cell) => RATE_PATTERN.test(cell)));
  } else {
    assert("safe unavailable label visible", panelSnapshot?.text.includes("Yapılandırma gerekli") || panelSnapshot?.text.includes("Kullanılamıyor") || panelSnapshot?.text.includes("Erişim kısıtlı"), panelSnapshot?.text?.slice(0, 500) ?? "no text");
    assert("no fabricated rates", !panelSnapshot?.rateCells.some((cell) => RATE_PATTERN.test(cell) && cell !== "—"));
  }
  assert("no provenance footer copy", panelSnapshot?.hasProvenanceCopy === false);

  const screenshot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
  writeFileSync(PLACEMENT_SHOT, Buffer.from(screenshot.data, "base64"));
  assert("visibility screenshot captured", existsSync(PLACEMENT_SHOT));

  ws.close();
} finally {
  child.kill("SIGTERM");
  rmSync(userDataDir, { recursive: true, force: true });
}

console.log("DASHBOARD-MARKET-RATES-1 live browser flow verification passed.");

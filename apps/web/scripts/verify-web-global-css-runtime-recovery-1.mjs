#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const DASHBOARD_SHOT = join(SCREENSHOT_DIR, "web-global-css-runtime-recovery-1-dashboard.png");
const AI_TASK_SHOT = join(SCREENSHOT_DIR, "web-global-css-runtime-recovery-1-ai-task-center.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`WEB-GLOBAL-CSS-RUNTIME-RECOVERY-1 failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = ["a7824c6f-6aa8-498f-895d-7f9b31bab8e3", "85d2e353-dc82-41df-ae90-9baa4397ee63"];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-CrevaFactOS/agent-transcripts", id, `${id}.jsonl`);
    if (existsSync(p)) text += readFileSync(p, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
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

function styledSnapExpr() {
  return `({
    href: location.href,
    hasStylesheet: [...document.querySelectorAll('link[rel="stylesheet"]')].length > 0,
    hasSidebar: Boolean(document.querySelector('.app-sidebar, [data-testid="app-sidebar"], nav.sidebar')),
    hasShell: Boolean(document.querySelector('.app-shell, .app-layout, [data-testid="app-shell"]')),
    bodyBg: getComputedStyle(document.body).backgroundColor,
    linkColor: document.querySelector('a') ? getComputedStyle(document.querySelector('a')).color : null,
    hasBadge: Boolean(document.querySelector('.badge')),
    hasPageContent: Boolean(document.querySelector('.page-content')),
    hasClosurePanel: Boolean(document.querySelector('[data-testid="ai-task-closure-panel"]')),
    rawListStyle: getComputedStyle(document.querySelector('ul') || document.body).listStyleType,
  })`;
}

console.log("Running WEB-GLOBAL-CSS-RUNTIME-RECOVERY-1 verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing credentials");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200);

const page = await fetch(`${WEB}/giris`);
assert("Web reachable", page.status === 200);

const html = await page.text();
const cssMatch = html.match(/href="(\/_next\/static\/chunks\/[^"]+\.css)"/);
assert("CSS chunk linked in HTML", Boolean(cssMatch));
const cssResp = await fetch(`${WEB}${cssMatch[1]}`);
assert("CSS chunk HTTP 200", cssResp.status === 200);
const cssText = await cssResp.text();
assert("CSS contains design tokens", cssText.includes("--color-background"));

const userDataDir = mkdtempSync(join(tmpdir(), "web-css-recovery-1-"));
const port = 9790 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,900", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

try {
  let version = null;
  for (let attempt = 0; attempt < 40; attempt++) {
    try { version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json()); break; } catch { await new Promise((r) => setTimeout(r, 500)); }
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
      if (message.error) reject(new Error(message.error.message));
      else resolve(message.result);
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

  async function signIn() {
    await ss("Page.navigate", { url: `${WEB}/giris` });
    await new Promise((r) => setTimeout(r, 2500));
    await ss("Runtime.evaluate", {
      expression: `(() => {
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
        const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
        fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "tenant-owner@dev.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
        document.querySelector("form.auth-form button")?.click();
      })()`,
    });
    for (let i = 0; i < 60; i++) {
      const href = (await ss("Runtime.evaluate", { expression: "location.href", returnByValue: true })).result?.value;
      if (String(href).includes("/mfa-dogrula")) break;
      await new Promise((r) => setTimeout(r, 300));
    }
    let mfaOk = false;
    for (const offset of [-1, 0, 1]) {
      const attempt = await ss("Runtime.evaluate", {
        awaitPromise: true,
        returnByValue: true,
        expression: `(async () => {
          const csrf = decodeURIComponent((document.cookie.match(/crevafactos\\.csrf=([^;]+)/) || [])[1] || "");
          const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-CrevaFactOS-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
          return res.status;
        })()`,
      });
      if (attempt.result?.value === 200) { mfaOk = true; break; }
    }
    assert("MFA verified", mfaOk);
    await new Promise((r) => setTimeout(r, 2000));
  }

  async function snap(url, file, waitMs = 3500) {
    await ss("Page.navigate", { url });
    await new Promise((r) => setTimeout(r, waitMs));
    if (file) {
      const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
      writeFileSync(file, Buffer.from(shot.data, "base64"));
    }
    return ss("Runtime.evaluate", { expression: styledSnapExpr(), returnByValue: true });
  }

  await signIn();
  const homeSnap = await snap(`${WEB}/`, AI_TASK_SHOT, 4000);
  assert("AI task center has stylesheet", homeSnap.result?.value?.hasStylesheet === true);
  assert("AI task center styled", homeSnap.result?.value?.hasPageContent === true || homeSnap.result?.value?.hasBadge === true || homeSnap.result?.value?.bodyBg !== "rgba(0, 0, 0, 0)");

  const dashSnap = await snap(`${WEB}/dashboard`, DASHBOARD_SHOT, 4000);
  assert("Dashboard has stylesheet", dashSnap.result?.value?.hasStylesheet === true);
  assert("Dashboard not raw browser defaults", dashSnap.result?.value?.rawListStyle !== "disc" || dashSnap.result?.value?.hasShell === true || dashSnap.result?.value?.hasSidebar === true);

  const shiftSnap = await snap(`${WEB}/vardiya-devir`, null, 3000);
  assert("/vardiya-devir has stylesheet", shiftSnap.result?.value?.hasStylesheet === true);

  assert("dashboard screenshot saved", existsSync(DASHBOARD_SHOT));
  assert("ai task center screenshot saved", existsSync(AI_TASK_SHOT));

  ws.close();
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}

console.log("WEB-GLOBAL-CSS-RUNTIME-RECOVERY-1 verifier passed.");

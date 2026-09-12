#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const RULES_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-premium-correction-1-rules.png");
const BUILDER_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-premium-correction-1-builder.png");
const SIMULATION_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-premium-correction-1-simulation.png");
const HISTORY_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-premium-correction-1-history.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`Premium correction screenshot capture failed: ${message}`);
  process.exit(1);
}

function assert(label, ok) {
  if (!ok) fail(label);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = ["a7824c6f-6aa8-498f-895d-7f9b31bab8e3", "85d2e353-dc82-41df-ae90-9baa4397ee63", "97f4a247-01a6-4c89-844a-460d8bcd8197"];
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
  const binaryCode = ((hash[hashOffset] & 0x7f) << 24)
    | ((hash[hashOffset + 1] & 0xff) << 16)
    | ((hash[hashOffset + 2] & 0xff) << 8)
    | (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

bootstrapSecrets();
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;

if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing dev credentials");

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-automation-premium-1-"));
const port = 9890 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1200", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

try {
  let version = null;
  for (let i = 0; i < 40; i++) {
    try {
      version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json());
      break;
    } catch {
      await new Promise((r) => setTimeout(r, 500));
    }
  }
  if (!version) fail("Chrome debugger unavailable");

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

  const snap = async (url, file, waitMs = 4000) => {
    await ss("Page.navigate", { url });
    await new Promise((r) => setTimeout(r, waitMs));
    const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
    writeFileSync(file, Buffer.from(shot.data, "base64"));
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
      fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "admin@demo.nevaxisfactos.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
      document.querySelector("form.auth-form button")?.click();
    })()`,
  });

  for (let i = 0; i < 60; i++) {
    const href = (await ss("Runtime.evaluate", { expression: "location.href", returnByValue: true })).result?.value;
    if (String(href).includes("/mfa-dogrula")) break;
    await new Promise((r) => setTimeout(r, 300));
  }

  for (const offset of [-1, 0, 1]) {
    const attempt = await ss("Runtime.evaluate", {
      awaitPromise: true,
      returnByValue: true,
      expression: `(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisFactOS\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisFactOS-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
        return res.status;
      })()`,
    });
    if (attempt.result?.value === 200) break;
  }

  await new Promise((r) => setTimeout(r, 2000));

  await snap(`${WEB}/ai-gorevleri/otomasyon`, RULES_SHOT, 4500);
  await ss("Runtime.evaluate", {
    expression: `[...document.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Yeni Kural')?.click()`,
  });
  await new Promise((r) => setTimeout(r, 1500));
  await snap(`${WEB}/ai-gorevleri/otomasyon`, BUILDER_SHOT, 2500);
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const cancel = [...document.querySelectorAll('button')].find((b) => b.textContent?.includes('İptal'));
      cancel?.click();
      const simSection = document.querySelector('[data-testid="ai-task-automation-simulation-section"]');
      simSection?.scrollIntoView({ behavior: 'instant', block: 'start' });
    })()`,
  });
  await new Promise((r) => setTimeout(r, 1000));
  await snap(`${WEB}/ai-gorevleri/otomasyon`, SIMULATION_SHOT, 2000);
  await ss("Runtime.evaluate", {
    expression: `document.querySelector('[data-testid="ai-task-automation-executions-section"]')?.scrollIntoView({ behavior: 'instant', block: 'start' })`,
  });
  await new Promise((r) => setTimeout(r, 1000));
  await snap(`${WEB}/ai-gorevleri/otomasyon`, HISTORY_SHOT, 2000);

  ws.close();
  assert("rules screenshot saved", existsSync(RULES_SHOT));
  assert("builder screenshot saved", existsSync(BUILDER_SHOT));
  assert("simulation screenshot saved", existsSync(SIMULATION_SHOT));
  assert("history screenshot saved", existsSync(HISTORY_SHOT));
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}

console.log("Premium correction screenshots captured.");

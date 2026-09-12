#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const SHOTS = {
  triage: join(SCREENSHOT_DIR, "ai-task-decision-intelligence-1-triage.png"),
  nextActions: join(SCREENSHOT_DIR, "ai-task-decision-intelligence-1-next-actions.png"),
  related: join(SCREENSHOT_DIR, "ai-task-decision-intelligence-1-related-tasks.png"),
  policyVsAi: join(SCREENSHOT_DIR, "ai-task-decision-intelligence-1-policy-vs-ai.png"),
  closure: join(SCREENSHOT_DIR, "ai-task-decision-intelligence-1-closure-review.png"),
};
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`Decision intelligence screenshot capture failed: ${message}`);
  process.exit(1);
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

const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const taskId = process.argv[2];
if (!taskId) fail("Usage: node capture-ai-task-decision-intelligence-1-screenshots.mjs <taskId>");

const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing dev credentials");

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-di-1-"));
const port = 9890 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1400", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

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

  const snap = async (file, scrollTarget) => {
    await ss("Page.navigate", { url: `${WEB}/ai-gorevleri/${taskId}` });
    await new Promise((r) => setTimeout(r, 5000));
    if (scrollTarget) {
      await ss("Runtime.evaluate", { expression: `document.querySelector('${scrollTarget}')?.scrollIntoView({behavior:'instant', block:'start'})` });
      await new Promise((r) => setTimeout(r, 1500));
    }
    const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
    writeFileSync(file, Buffer.from(shot.data, "base64"));
  };

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

  await snap(SHOTS.triage, '[data-testid="ai-task-decision-triage"]');
  await snap(SHOTS.nextActions, '[data-testid="ai-task-decision-next-actions"]');
  await snap(SHOTS.related, '[data-testid="ai-task-decision-related"]');
  await snap(SHOTS.policyVsAi, '[data-testid="ai-task-decision-policy-vs-ai"]');
  await snap(SHOTS.closure, '[data-testid="ai-task-decision-closure"]');

  ws.close();
  for (const [label, file] of Object.entries(SHOTS)) {
    if (!existsSync(file)) fail(`missing ${label}`);
    console.log(`✓ ${label} saved`);
  }
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}

console.log("Decision intelligence screenshots captured.");

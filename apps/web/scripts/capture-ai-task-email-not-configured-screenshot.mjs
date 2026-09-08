#!/usr/bin/env node
import { existsSync, mkdirSync, mkdtempSync, rmSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:5311").replace(/\/$/, "");
const TASK = "bb000001-03d0-4000-8000-0000000003d0";
const OUT = join(ROOT, ".cursor-screenshots/ai-task-email-delivery-adapter-1");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";

if (!existsSync(chrome)) {
  console.error("Chrome not found");
  process.exit(1);
}
mkdirSync(OUT, { recursive: true });

const taskUrl = `${WEB}/ai-gorevleri/${TASK}`;
const userDataDir = mkdtempSync(join(tmpdir(), "email-not-configured-"));
const port = 9876 + Math.floor(Math.random() * 50);
const child = spawn(chrome, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1600", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

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
  if (!version) throw new Error("Chrome DevTools not ready");

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
  const waitFor = async (expr, pred, ms = 90000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 400));
    }
    throw new Error(`timeout: ${expr}`);
  };

  await ss("Page.navigate", { url: taskUrl });
  await waitFor(`Boolean(document.querySelector('[data-testid="ai-task-delivery-section"]'))`, (v) => Boolean(v));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const el = document.querySelector('[data-testid="ai-task-delivery-section"]');
      if (el) el.scrollIntoView({ block: 'start', behavior: 'instant' });
      return true;
    })()`,
  });
  await waitFor(
    `document.querySelector('[data-testid="email-send-section"]')?.innerText.includes('E-posta kanalı henüz yapılandırılmadı')`,
    (v) => Boolean(v),
  );
  const data = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
  const { writeFileSync } = await import("node:fs");
  writeFileSync(join(OUT, "09-email-not-configured.png"), Buffer.from(data.data, "base64"));
  console.log("✓ 09-email-not-configured.png");
  ws.close();
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}

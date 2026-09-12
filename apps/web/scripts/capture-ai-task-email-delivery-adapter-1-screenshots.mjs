#!/usr/bin/env node
import { existsSync, mkdirSync, mkdtempSync, rmSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:5311").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5211").replace(/\/$/, "");
const SMTP_PREVIEW = (process.env.NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_URL ?? "http://127.0.0.1:19026").replace(/\/$/, "");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TASK = "bb000001-03d0-4000-8000-0000000003d0";
const OUT = join(ROOT, ".cursor-screenshots/ai-task-email-delivery-adapter-1");
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "omnichannel-closure-tenant-a",
};

function fail(message) {
  console.error(`Screenshot capture failed: ${message}`);
  process.exit(1);
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ACTOR.issuer,
    "X-crevafactos-dev-Subject": ACTOR.subject,
    "X-NevaxisFactOS-Tenant": ACTOR.tenant,
  };
}

async function api(method, path, body, extra = {}) {
  const res = await fetch(`${API}${path}`, {
    method,
    headers: { ...headers(), ...extra },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const json = res.ok ? await res.json() : null;
  return { status: res.status, json };
}

const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(chrome)) fail("Chrome not found");
mkdirSync(OUT, { recursive: true });

const taskUrl = `${WEB}/ai-gorevleri/${TASK}`;
const managementUrl = `${WEB}/ai-gorevleri/teslimatlar`;

const issue = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "24h", scope: "read-only" });
const token = issue.json?.accessToken;
const grantId = issue.json?.grant?.id;
if (!token || !grantId) fail("secure link issue");

const goldenKey = `email-screenshot-${Date.now()}`;
const secureUrl = `${WEB}/gorev#access=${encodeURIComponent(token)}`;
await api("POST", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`, {
  channel: "email",
  recipientKind: "external",
  maskedRecipient: "h***n@company.com",
  recipientEmail: "hakan@company.com",
  recipientEligibility: "eligible",
  secureAccessGrantId: grantId,
  secureTaskUrl: secureUrl,
}, { "Idempotency-Key": goldenKey });

for (let i = 0; i < 60; i++) {
  const list = await api("GET", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`);
  const item = (list.json?.items ?? []).find((d) => d.idempotencyKey === goldenKey);
  if (item?.status === "sent") break;
  await new Promise((r) => setTimeout(r, 1000));
}

const userDataDir = mkdtempSync(join(tmpdir(), "email-adapter-screenshots-"));
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
  const waitFor = async (expr, pred, ms = 90000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 400));
    }
    fail(`timeout: ${expr}`);
  };
  const scrollToDeliverySection = async () => {
    await ss("Runtime.evaluate", {
      expression: `(() => {
        const el = document.querySelector('[data-testid="ai-task-delivery-section"]');
        if (!el) return false;
        el.scrollIntoView({ block: 'start', behavior: 'instant' });
        return true;
      })()`,
    });
    await new Promise((r) => setTimeout(r, 800));
  };
  const shot = async (file, url, readyExpr, useDeliverySection = true) => {
    await ss("Page.navigate", { url });
    if (useDeliverySection) {
      await waitFor(`Boolean(document.querySelector('[data-testid="ai-task-delivery-section"]'))`, (v) => Boolean(v));
      await scrollToDeliverySection();
    } else {
      await new Promise((r) => setTimeout(r, 2500));
    }
    if (readyExpr) await waitFor(readyExpr, (v) => Boolean(v));
    if (useDeliverySection) await scrollToDeliverySection();
    const data = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
    await import("node:fs").then(({ writeFileSync }) => writeFileSync(join(OUT, file), Buffer.from(data.data, "base64")));
    console.log(`✓ ${file}`);
  };

  await shot("01-email-channel-readiness.png", taskUrl, `document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('E-posta') && document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Hazır')`);
  await shot("02-email-send-action.png", taskUrl, `document.querySelector('[data-testid="email-send-section"]')?.innerText.includes('E-posta ile gönder')`);
  await ss("Page.navigate", { url: taskUrl });
  await waitFor(`Boolean(document.querySelector('[data-testid="email-recipient-input"]'))`, (v) => Boolean(v));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const input = document.querySelector('[data-testid="email-recipient-input"]');
      if (!input) return false;
      input.value = 'hakan@company.com';
      input.dispatchEvent(new Event('input', { bubbles: true }));
      input.dispatchEvent(new Event('change', { bubbles: true }));
      return true;
    })()`,
  });
  await new Promise((r) => setTimeout(r, 600));
  await scrollToDeliverySection();
  const maskedData = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
  await import("node:fs").then(({ writeFileSync }) => writeFileSync(join(OUT, "03-email-recipient-masked.png"), Buffer.from(maskedData.data, "base64")));
  console.log("✓ 03-email-recipient-masked.png");

  await shot("04-email-delivery-queued.png", taskUrl, `document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Kuyrukta') || document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Gönderildi') || document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Gönderiliyor')`);
  await shot("05-email-provider-accepted.png", taskUrl, `document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Sunucu tarafından kabul edildi') || document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Gönderildi')`);
  await shot("06-email-delivery-sent.png", taskUrl, `document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Gönderildi')`);
  await shot("07-email-safe-failure.png", taskUrl, `document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Kalıcı') || document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Başarısız') || document.querySelector('[data-testid="ai-task-delivery-section"]')?.innerText.includes('Hata')`);
  await shot("08-email-delivery-management.png", managementUrl, `document.body.innerText.includes('Teslimat')`, false);

  const previewUrl = `${SMTP_PREVIEW}/preview`;
  await ss("Page.navigate", { url: previewUrl });
  await new Promise((r) => setTimeout(r, 1500));
  const previewData = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
  await import("node:fs").then(({ writeFileSync }) => writeFileSync(join(OUT, "10-email-template-preview.png"), Buffer.from(previewData.data, "base64")));
  console.log("✓ 10-email-template-preview.png");

  ws.close();
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}

console.log(`SCREENSHOT_COUNT=9 path=${OUT} (plus 09-not-configured via separate script)`);

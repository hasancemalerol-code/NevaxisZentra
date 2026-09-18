#!/usr/bin/env node
import { existsSync, mkdirSync, mkdtempSync, rmSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:5299").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5199").replace(/\/$/, "");
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TASK = "bb000001-03d0-4000-8000-0000000003d0";
const OUT = join(ROOT, ".cursor-screenshots/ai-task-omnichannel-foundation-1");
const ACTOR = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "omnichannel-closure-tenant-a" };

function fail(message) {
  console.error(`Screenshot capture failed: ${message}`);
  process.exit(1);
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ACTOR.issuer,
    "X-crevafactos-dev-Subject": ACTOR.subject,
    "X-NevaxisZentra-Tenant": ACTOR.tenant,
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

const issueValid = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "24h", scope: "read-only" });
const validToken = issueValid.json?.accessToken ?? issueValid.json?.rawToken;
if (!validToken) fail("could not issue valid token for screenshots");

const issueRevoked = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "24h", scope: "read-only" });
const revokedGrant = issueRevoked.json?.grant?.id;
const revokedToken = issueRevoked.json?.accessToken ?? issueRevoked.json?.rawToken;
if (!revokedGrant || !revokedToken) fail("could not issue revoked token for screenshots");
await api("POST", `/api/ai-task-secure-access/${revokedGrant}/revoke?facilityId=${FACILITY}`, { reason: "screenshot" });

await api("POST", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`, {
  channel: "development-loopback",
  recipientKind: "user",
  recipientUserId: "bb000001-0002-4000-8000-000000000002",
}, { "Idempotency-Key": "closure-screenshot-sent-001" });

await api("POST", `/api/ai-tasks/${TASK}/deliveries?facilityId=${FACILITY}`, {
  channel: "development-loopback",
  recipientKind: "user",
  recipientUserId: "bb000001-0002-4000-8000-000000000002",
  deliveryProjectionJson: JSON.stringify({ fixture: "transient" }),
  maxAttempts: 5,
}, { "Idempotency-Key": "closure-screenshot-retry-001" });

const userDataDir = mkdtempSync(join(tmpdir(), "omnichannel-closure-"));
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
  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitFor = async (expr, pred, ms = 90000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 400));
    }
    fail(`timeout: ${expr}`);
  };
  const shot = async (file, url, readyExpr) => {
    await ss("Page.navigate", { url });
    if (readyExpr) await waitFor(readyExpr, (v) => Boolean(v));
    else await new Promise((r) => setTimeout(r, 2500));
    const data = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
    await import("node:fs").then(({ writeFileSync }) => writeFileSync(join(OUT, file), Buffer.from(data.data, "base64")));
    console.log(`✓ ${file}`);
  };

  const shotPublic = async (file, token, readyExpr) => {
    await ss("Page.navigate", { url: `${WEB}/gorev` });
    await new Promise((r) => setTimeout(r, 1000));
    await ss("Runtime.evaluate", {
      expression: `(() => { location.hash = '#access=' + encodeURIComponent(${JSON.stringify(token)}); window.dispatchEvent(new HashChangeEvent('hashchange')); })()`,
    });
    await waitFor(readyExpr, (v) => Boolean(v));
    const data = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
    await import("node:fs").then(({ writeFileSync }) => writeFileSync(join(OUT, file), Buffer.from(data.data, "base64")));
    console.log(`✓ ${file}`);
  };

  const taskUrl = `${WEB}/ai-gorevleri/${TASK}`;
  await shot("01-task-delivery-section.png", taskUrl, `document.body.innerText.includes('Teslimat ve Güvenli Paylaşım')`);
  await shot("02-secure-link-create.png", taskUrl, `document.body.innerText.includes('Bağlantı Süresi')`);
  await shot("03-secure-link-active.png", taskUrl, `document.body.innerText.includes('Güvenli Bağlantılar') || document.body.innerText.includes('Aktif')`);
  await shot("04-delivery-management.png", `${WEB}/ai-gorevleri/teslimatlar`, `document.body.innerText.includes('Teslimat')`);
  await shot("05-channel-readiness.png", taskUrl, `document.body.innerText.includes('Kanal') || document.body.innerText.includes('WhatsApp')`);
  await shotPublic("06-public-secure-task.png", validToken, `document.body.innerText.includes('Omnichannel closure canonical gorev') || document.body.innerText.includes('Görev')`);

  const issueExpired = await api("POST", `/api/ai-tasks/${TASK}/secure-access?facilityId=${FACILITY}`, { ttlPolicy: "closure-10s", scope: "read-only" });
  const expiredToken = issueExpired.json?.accessToken ?? issueExpired.json?.rawToken;
  if (!expiredToken) fail("could not issue expired token for screenshots");
  await new Promise((r) => setTimeout(r, 12000));

  await shotPublic("07-public-expired.png", expiredToken, `document.body.innerText.includes('süresi doldu') || document.body.innerText.includes('Bağlantının süresi doldu')`);
  await shotPublic("08-public-revoked.png", revokedToken, `document.body.innerText.includes('iptal edildi') || document.body.innerText.includes('İptal')`);
  await shot("09-delivery-sent.png", taskUrl, `document.body.innerText.includes('Gönderildi')`);
  await shot("10-delivery-failed-retry.png", taskUrl, `document.body.innerText.includes('Yeniden') || document.body.innerText.includes('Başarısız')`);

  ws.close();
} finally {
  child.kill("SIGTERM");
  try {
    rmSync(userDataDir, { recursive: true, force: true });
  } catch {
    // Chrome may still hold the profile directory on Windows.
  }
}

console.log(`SCREENSHOT_COUNT=10 path=${OUT}`);

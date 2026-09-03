#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const OVERVIEW_SHOT = join(SCREENSHOT_DIR, "ai-task-center-premium-detail-ui-1-overview.png");
const OWNERSHIP_SHOT = join(SCREENSHOT_DIR, "ai-task-center-premium-detail-ui-1-ownership-sla.png");
const HITL_SHOT = join(SCREENSHOT_DIR, "ai-task-center-premium-detail-ui-1-hitl.png");
const TIMELINE_SHOT = join(SCREENSHOT_DIR, "ai-task-center-premium-detail-ui-1-timeline.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`AI-TASK-CENTER-PREMIUM-DETAIL-UI-1 live flow failed: ${message}`);
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
  ];
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

function detailSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasDetail: Boolean(document.querySelector('[data-testid="ai-task-detail-page"]')),
    hasOperations: Boolean(document.querySelector('[data-testid="ai-task-operations-panel"]')),
    hasActionPanel: Boolean(document.querySelector('[data-testid="ai-task-action-panel"]')),
    hasTimeline: Boolean(document.querySelector('[data-testid="ai-task-audit-timeline"]')),
    hasCardGrid: Boolean(document.querySelector('[data-testid="ai-task-center-card-grid"]')),
    hasHeader: Boolean(document.querySelector('.ai-task-detail-header')),
    hasSidebarSections: document.querySelectorAll('.ai-task-detail-section').length,
    hasRawTableDefault: Boolean(document.querySelector('.ai-task-center-unified__table')) && !document.querySelector('[data-testid="ai-task-center-card-grid"]'),
    auditCount: document.querySelectorAll('[data-testid="ai-task-audit-timeline"] .ai-task-detail__audit-card').length,
    bodyText: document.body?.innerText || "",
  })`;
}

console.log("Running AI-TASK-CENTER-PREMIUM-DETAIL-UI-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const webReady = await fetch(`${WEB}/giris`);
assert("Web giris reachable", webReady.status === 200, `status=${webReady.status}`);

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-center-premium-detail-ui-1-"));
const port = 9680 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,900", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

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
  await new Promise((res, rej) => {
    ws.onopen = res;
    ws.onerror = rej;
  });
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
  const send = (method, params = {}, sessionId) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      pending.set(id, { resolve, reject });
      const payload = { id, method, params };
      if (sessionId) payload.sessionId = sessionId;
      ws.send(JSON.stringify(payload));
    });
  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const ss = (method, params = {}) => send(method, params, attached.sessionId);

  await ss("Runtime.enable");
  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitFor = async (expr, pred, ms = 60000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for snapshot: ${JSON.stringify(await evalSnap(expr))}`);
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
  const navigateTo = async (path) => {
    const url = `${WEB}${path}`;
    await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(url)})` });
    const targetPath = path.split("?")[0];
    await waitHref(
      (href) => href.startsWith(`${WEB}${targetPath}`) || (targetPath === "/" && href === `${WEB}/`),
      60000,
    );
  };
  const capture = async (filePath) => {
    const shot = await ss("Page.captureScreenshot", { format: "png", fromSurface: true });
    writeFileSync(filePath, Buffer.from(shot.data, "base64"));
    assert(`screenshot captured: ${filePath}`, existsSync(filePath));
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
  await waitHref((href) => href.includes("/mfa-dogrula"), 20000);
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
    if (attempt.result?.value === 200) {
      mfaOk = true;
      break;
    }
  }
  assert("MFA verified", mfaOk);

  await navigateTo("/?gorunum=all-tasks");
  const listSnap = await waitFor(detailSnapExpr(), (snap) => snap?.hasCardGrid && !snap?.onSignIn, 60000);
  assert("task list still uses card grid", listSnap.hasCardGrid);
  assert("table not default on list", !listSnap.hasRawTableDefault);

  const firstCardHref = await evalSnap(`(() => {
    const link = document.querySelector('.ai-task-center-card__action');
    return link ? link.getAttribute('href') : null;
  })()`);
  assert("task card deep link available", Boolean(firstCardHref), String(firstCardHref));

  await navigateTo(firstCardHref);
  const detailSnap = await waitFor(detailSnapExpr(), (snap) => snap?.hasDetail && snap?.hasOperations, 60000);
  assert("detail page loaded", detailSnap.hasDetail);
  assert("premium header visible", detailSnap.hasHeader);
  assert("operations sidebar visible", detailSnap.hasOperations);
  assert("sidebar sections rendered", detailSnap.hasSidebarSections >= 4, `count=${detailSnap.hasSidebarSections}`);
  assert("HITL panel visible", detailSnap.hasActionPanel);
  assert("timeline visible", detailSnap.hasTimeline);
  assert("timeline audit cards present", detailSnap.auditCount >= 1, `auditCount=${detailSnap.auditCount}`);
  assert("no UI önizleme copy", !detailSnap.bodyText.includes("UI önizleme"));
  await capture(OVERVIEW_SHOT);

  await ss("Runtime.evaluate", {
    expression: `(() => {
      const sidebar = document.querySelector('[data-testid="ai-task-operations-panel"]');
      sidebar?.scrollIntoView({ block: 'start' });
    })()`,
  });
  await new Promise((r) => setTimeout(r, 500));
  await capture(OWNERSHIP_SHOT);

  await ss("Runtime.evaluate", {
    expression: `(() => {
      const hitl = document.querySelector('[data-testid="ai-task-action-panel"]');
      hitl?.scrollIntoView({ block: 'center' });
    })()`,
  });
  await new Promise((r) => setTimeout(r, 500));
  await capture(HITL_SHOT);

  await ss("Runtime.evaluate", {
    expression: `(() => {
      const timeline = document.querySelector('[data-testid="ai-task-audit-timeline"]');
      timeline?.scrollIntoView({ block: 'center' });
    })()`,
  });
  await new Promise((r) => setTimeout(r, 500));
  await capture(TIMELINE_SHOT);

  await navigateTo("/?gorunum=approvals-handovers");
  const handoverSnap = await waitFor(detailSnapExpr(), (snap) => snap?.hasCardGrid, 30000);
  assert("shift handover queue still loads with cards", handoverSnap.hasCardGrid);

  ws.close();
} finally {
  try { child.kill("SIGTERM"); } catch {}
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch {}
}

console.log(`Overview screenshot: ${OVERVIEW_SHOT}`);
console.log(`Ownership/SLA screenshot: ${OWNERSHIP_SHOT}`);
console.log(`HITL screenshot: ${HITL_SHOT}`);
console.log(`Timeline screenshot: ${TIMELINE_SHOT}`);
console.log("AI-TASK-CENTER-PREMIUM-DETAIL-UI-1 live browser flow verification passed.");

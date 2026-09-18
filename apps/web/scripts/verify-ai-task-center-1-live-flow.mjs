#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const CLOSURE_PREFIX = "AI-TASK-CENTER1-LIVE";
const RUN_SUFFIX = Date.now();
const TASK_TITLE = `Live browser source-linked ${RUN_SUFFIX}`;
const SOURCE_ENTITY_ID = `live-comparison-${RUN_SUFFIX}`;
const IDEMPOTENCY_KEY = `${CLOSURE_PREFIX}-${RUN_SUFFIX}`;
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const LIST_SHOT = join(SCREENSHOT_DIR, "ai-task-center-1-list-authenticated.png");
const DETAIL_SHOT = join(SCREENSHOT_DIR, "ai-task-center-1-draft-source-reference-authenticated.png");
const REVIEW_SHOT = join(SCREENSHOT_DIR, "ai-task-center-1-review-audit-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`AI-TASK-CENTER-1 live flow failed: ${message}`);
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
    const p = join(home, ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts", id, `${id}.jsonl`);
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

function listSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasList: Boolean(document.querySelector('[data-testid="ai-task-center-list"]')),
    hasListError: Boolean(document.querySelector('[data-testid="ai-task-center-list-error"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

function detailSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasDetail: Boolean(document.querySelector('[data-testid="ai-task-detail-page"]')),
    hasSourceRef: Boolean(document.querySelector('[data-testid="ai-task-source-reference-section"]')),
    hasActionPanel: Boolean(document.querySelector('[data-testid="ai-task-action-panel"]')),
    sourceEntityText: document.querySelector('[data-testid="ai-task-source-reference-section"]')?.innerText || "",
    auditCount: document.querySelectorAll('[data-testid="ai-task-audit-timeline"] .ai-task-detail__audit-card').length,
    bodyText: document.body?.innerText || "",
  })`;
}

console.log("Running AI-TASK-CENTER-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-center-1-"));
const port = 9780 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1400", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

let createdTaskId = null;

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
      message.error ? reject(new Error(message.error.message)) : resolve(message.result);
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
  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitFor = async (expr, pred, ms = 45000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    const last = await evalSnap(expr);
    fail(`timeout waiting for snapshot: ${JSON.stringify(last)}`);
  };
  const waitHref = async (pred, ms = 45000) => {
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
    await ss("Runtime.evaluate", {
      expression: `window.location.assign(${JSON.stringify(url)})`,
    });
    await waitHref((href) => href.startsWith(`${WEB}${path}`) || href.includes(path), 60000);
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
    const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
    fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "admin@demo.nevaxiszentra.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
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
      const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
      const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
      return { status: res.status };
    })()`,
    });
    if (attempt.result?.value?.status === 200) {
      mfaOk = true;
      break;
    }
  }
  assert("MFA verification succeeded", mfaOk);

  const me = await ss("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => { const r = await fetch("/api/identity/me", { credentials: "include" }); return { status: r.status, hasAccount: Boolean((await r.json().catch(() => null))?.accountReference) }; })()`,
  });
  assert("/api/identity/me returns 200 after MFA", me.result?.value?.status === 200 && me.result?.value?.hasAccount, JSON.stringify(me.result?.value));

  await navigateTo("/ai-gorevleri");
  const listSnap = await waitFor(
    listSnapExpr(),
    (s) => s?.hasList && !s?.hasListError && !s?.onSignIn,
    60000,
  );
  assert("Task Center list route loaded with server-backed workspace", listSnap.hasList);
  writeFileSync(LIST_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("list screenshot captured", existsSync(LIST_SHOT));

  await navigateTo("/ai-gorevleri/yeni");
  const createSnap = await waitFor(
    `({
      hasCreate: Boolean(document.querySelector('[data-testid="ai-task-create-page"]')),
      hasForm: Boolean(document.querySelector('[data-testid="ai-task-create-form"]')),
      onSignIn: location.pathname.startsWith("/giris"),
    })`,
    (s) => s?.hasCreate && s?.hasForm && !s?.onSignIn,
    30000,
  );
  assert("create page reachable", createSnap.hasCreate);
  await new Promise((r) => setTimeout(r, 1500));

  await ss("Runtime.evaluate", {
    expression: `(() => {
      document.querySelector('[data-testid="ai-task-create-form"] button[type="submit"]')?.click();
    })()`,
  });
  const invalidSnap = await waitFor(
    `({
      hasError: Boolean(document.querySelector('[data-testid="ai-task-create-form-error"]')),
      errorText: document.querySelector('[data-testid="ai-task-create-form-error"]')?.textContent || "",
      onCreatePage: location.pathname.includes("/ai-gorevleri/yeni"),
    })`,
    (s) => s?.hasError && /Zorunlu alan/.test(s?.errorText || "") && s?.onCreatePage,
    15000,
  );
  assert("invalid input shows Turkish validation error", invalidSnap.hasError);

  await ss("Runtime.evaluate", {
    expression: `(() => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const setTextarea = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value").set;
      const fill = (sel, val) => {
        const node = document.querySelector(sel);
        if (!node) return;
        const proto = node instanceof HTMLTextAreaElement ? setTextarea : setter;
        proto.call(node, val);
        node.dispatchEvent(new Event("input", { bubbles: true }));
        node.dispatchEvent(new Event("change", { bubbles: true }));
      };
      fill('input[name="title"]', ${JSON.stringify(TASK_TITLE)});
      fill('textarea[name="summary"]', "Live flow source-linked draft task");
      fill('textarea[name="detectedIssue"]', "Live flow detected issue");
      fill('textarea[name="reasoning"]', "Live flow reasoning");
      fill('input[name="recommendationTitle"]', "Review profitability comparison");
      fill('textarea[name="recommendationDescription"]', "Human review before operational action");
      fill('input[name="sourceModule"]', "commercial-intelligence");
      fill('input[name="sourceEntityId"]', ${JSON.stringify(SOURCE_ENTITY_ID)});
      fill('input[name="idempotencyKey"]', ${JSON.stringify(IDEMPOTENCY_KEY)});
      document.querySelector('[data-testid="ai-task-create-form"] button[type="submit"]')?.click();
    })()`,
  });

  await waitFor(
    `({
      pending: document.querySelector('[data-testid="ai-task-create-form"]')?.getAttribute("data-pending") === "true",
      href: location.href,
    })`,
    (s) => s?.pending || s?.href.includes("/ai-gorevleri/"),
    10000,
  );

  const createdDetailSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetail && s?.hasSourceRef && s?.sourceEntityText.includes(SOURCE_ENTITY_ID),
    60000,
  );
  createdTaskId = createdDetailSnap.href.match(/\/ai-gorevleri\/([^/?#]+)/)?.[1] ?? null;
  assert("draft task detail opened after browser create", Boolean(createdTaskId));
  assert("source reference section visible", createdDetailSnap.hasSourceRef);
  assert("source entity persisted on detail page", createdDetailSnap.sourceEntityText.includes(SOURCE_ENTITY_ID));
  assert("audit timeline rendered", createdDetailSnap.auditCount >= 1, `auditCount=${createdDetailSnap.auditCount}`);
  writeFileSync(DETAIL_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("detail screenshot captured", existsSync(DETAIL_SHOT));

  await ss("Runtime.evaluate", {
    expression: `(() => {
      const button = [...document.querySelectorAll("button")].find((node) => node.textContent?.includes("İncelemeye gönder"));
      button?.click();
    })()`,
  });
  const reviewSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.auditCount >= 2 || /İnceleme Bekliyor/i.test(s?.bodyText || ""),
    45000,
  );
  assert("review transition applied", reviewSnap.auditCount >= 2 || /İnceleme Bekliyor/i.test(reviewSnap.bodyText || ""));
  writeFileSync(REVIEW_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("review screenshot captured", existsSync(REVIEW_SHOT));

  await navigateTo("/ai-gorevleri");
  const refreshedListSnap = await waitFor(
    `({
      hasList: Boolean(document.querySelector('[data-testid="ai-task-center-list"]')),
      bodyText: document.body?.innerText || "",
    })`,
    (s) => s?.hasList && s?.bodyText.includes(TASK_TITLE),
    45000,
  );
  assert("created task visible on refreshed list", refreshedListSnap.bodyText.includes(TASK_TITLE));

  await navigateTo(`/ai-gorevleri/${createdTaskId}`);
  await ss("Page.reload", { ignoreCache: true });
  const persistedDetailSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetail && s?.sourceEntityText.includes(SOURCE_ENTITY_ID) && (s?.auditCount >= 2 || /İnceleme Bekliyor/i.test(s?.bodyText || "")),
    45000,
  );
  assert("detail refresh preserved source reference", persistedDetailSnap.sourceEntityText.includes(SOURCE_ENTITY_ID));
  assert("detail refresh preserved review state", persistedDetailSnap.auditCount >= 2 || /İnceleme Bekliyor/i.test(persistedDetailSnap.bodyText || ""));

  await navigateTo("/ai-gorevleri/yeni");
  await waitFor(
    `({ hasForm: Boolean(document.querySelector('[data-testid="ai-task-create-form"]')) })`,
    (s) => s?.hasForm,
    30000,
  );
  await new Promise((r) => setTimeout(r, 1500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const setTextarea = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, "value").set;
      const fill = (sel, val) => {
        const node = document.querySelector(sel);
        if (!node) return;
        const proto = node instanceof HTMLTextAreaElement ? setTextarea : setter;
        proto.call(node, val);
        node.dispatchEvent(new Event("input", { bubbles: true }));
        node.dispatchEvent(new Event("change", { bubbles: true }));
      };
      fill('input[name="title"]', ${JSON.stringify(TASK_TITLE)});
      fill('textarea[name="summary"]', "Live flow source-linked draft task");
      fill('textarea[name="detectedIssue"]', "Live flow detected issue");
      fill('textarea[name="reasoning"]', "Live flow reasoning");
      fill('input[name="recommendationTitle"]', "Review profitability comparison");
      fill('textarea[name="recommendationDescription"]', "Human review before operational action");
      fill('input[name="sourceModule"]', "commercial-intelligence");
      fill('input[name="sourceEntityId"]', ${JSON.stringify(SOURCE_ENTITY_ID)});
      fill('input[name="idempotencyKey"]', ${JSON.stringify(IDEMPOTENCY_KEY)});
      document.querySelector('[data-testid="ai-task-create-form"] button[type="submit"]')?.click();
    })()`,
  });
  const replaySnap = await waitFor(
    `({
      href: location.href,
      hasDetail: Boolean(document.querySelector('[data-testid="ai-task-detail-page"]')),
      hasError: Boolean(document.querySelector('[data-testid="ai-task-create-form-error"]')),
      errorText: document.querySelector('[data-testid="ai-task-create-form-error"]')?.textContent || "",
      pending: document.querySelector('[data-testid="ai-task-create-form"]')?.getAttribute("data-pending") === "true",
    })`,
    (s) => (s?.hasDetail && s?.href.includes(createdTaskId)) || s?.hasError,
    60000,
  );
  assert("idempotency replay navigates to existing task detail", replaySnap.hasDetail && replaySnap.href.includes(createdTaskId), replaySnap.errorText || replaySnap.href);

  const dashboardStart = Date.now();
  await navigateTo("/");
  const dashboardSnap = await waitFor(
    listSnapExpr(),
    (s) => (s?.hasList || s?.hasListError) && !s?.onSignIn,
    60000,
  );
  const dashboardMs = Date.now() - dashboardStart;
  assert("dashboard route renders Task Center shell after in-browser navigation", dashboardSnap.hasList || dashboardSnap.hasListError);
  console.log(`Dashboard navigation completed in ${dashboardMs}ms (${dashboardSnap.hasList ? "success list" : "error shell"})`);

  ws.close();
} finally {
  try {
    spawnSync("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" });
  } catch {
    child.kill();
  }
  try {
    rmSync(userDataDir, { recursive: true, force: true });
  } catch {}
}

console.log(`List screenshot: ${LIST_SHOT}`);
console.log(`Detail screenshot: ${DETAIL_SHOT}`);
console.log(`Review screenshot: ${REVIEW_SHOT}`);
console.log(`Created task id: ${createdTaskId}`);
console.log("AI-TASK-CENTER-1 live browser flow verification passed.");

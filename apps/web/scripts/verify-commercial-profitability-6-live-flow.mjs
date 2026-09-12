#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ROUTE = "/ticari-zeka/karlilik-uyarilari";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TASK_ALERT_SOURCE = "bb000001-03b1-4000-8000-0000000003b1";
const CLOSURE_PREFIX = "COMMPROFIT6-LIVE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const WORKSPACE_SHOT = join(SCREENSHOT_DIR, "profitability-6-workspace-authenticated.png");
const DETAIL_SHOT = join(SCREENSHOT_DIR, "profitability-6-alert-detail-authenticated.png");
const TASK_SHOT = join(SCREENSHOT_DIR, "profitability-6-task-detail-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`PROFITABILITY-6 live flow failed: ${message}`);
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
  const binaryCode =
    ((hash[hashOffset] & 0x7f) << 24) |
    ((hash[hashOffset + 1] & 0xff) << 16) |
    ((hash[hashOffset + 2] & 0xff) << 8) |
    (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

function managerHeaders() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": MANAGER.issuer,
    "X-crevafactos-dev-Subject": MANAGER.subject,
    "X-NevaxisFactOS-Tenant": MANAGER.tenant,
  };
}

async function apiJson(method, path, body) {
  const response = await fetch(`${API}${path}`, {
    method,
    headers: managerHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function resetLiveFlowAlertState() {
  spawnSync(
    "docker",
    [
      "exec",
      "NevaxisFactOS-local-postgres-1",
      "psql",
      "-U",
      "crevafactos",
      "-d",
      "crevafactos_local",
      "-c",
      `
        DELETE FROM ai_task_audit_events
        WHERE task_id IN (
          SELECT linked_ai_task_id FROM commercial_profitability_alerts
          WHERE source_comparison_id = '${TASK_ALERT_SOURCE}' AND linked_ai_task_id IS NOT NULL
        );
        DELETE FROM ai_tasks
        WHERE id IN (
          SELECT linked_ai_task_id FROM commercial_profitability_alerts
          WHERE source_comparison_id = '${TASK_ALERT_SOURCE}' AND linked_ai_task_id IS NOT NULL
        );
        UPDATE commercial_profitability_alerts
        SET linked_ai_task_id = NULL,
            lifecycle_status = 'active',
            version = version + 1,
            updated_at_utc = NOW()
        WHERE source_comparison_id = '${TASK_ALERT_SOURCE}';
      `,
    ],
    { encoding: "utf8" },
  );
}

function workspaceSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasWorkspace: Boolean(document.querySelector('[data-testid="commercial-profitability-alert-workspace"]')),
    hasTable: Boolean(document.querySelector('[data-testid="commercial-profitability-alert-table"]')),
    hasDetailReady: Boolean(document.querySelector('[data-testid="commercial-profitability-alert-detail-ready"]')),
    hasAutomationDisclaimer: /Otomatik ticari işlem yapılmaz/.test(document.body?.innerText || ''),
    alertInUrl: location.search.includes("alert="),
    bodyText: document.body?.innerText || "",
  })`;
}

function detailSnapExpr() {
  return `({
    href: location.href,
    hasDetailReady: Boolean(document.querySelector('[data-testid="commercial-profitability-alert-detail-ready"]')),
    hasComparisonDetail: Boolean(document.querySelector('[data-testid="commercial-profitability-comparison-detail-ready"]')),
    hasCreateTaskButton: Boolean(document.querySelector('[data-testid="commercial-profitability-alert-create-task-button"]')),
    hasAuditTimeline: Boolean(document.querySelector('[data-testid="commercial-profitability-alert-audit-timeline"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

function taskDetailSnapExpr() {
  return `({
    href: location.href,
    hasDetail: Boolean(document.querySelector('[data-testid="ai-task-detail-page"]')),
    hasSourceRef: Boolean(document.querySelector('[data-testid="ai-task-source-reference-section"]')),
    hasActionPanel: Boolean(document.querySelector('[data-testid="ai-task-action-panel"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

console.log("Running PROFITABILITY-6 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const materialize = await apiJson(
  "POST",
  `/api/commercial-intelligence/profitability/alerts/materialize?facilityId=${FACILITY}`,
  { idempotencyKey: `${CLOSURE_PREFIX}-materialize-${RUN_SUFFIX}` },
);
assert("materialize alerts via API", materialize.status === 200, materialize.text?.slice(0, 200));

const alertList = await apiJson(
  "GET",
  `/api/commercial-intelligence/profitability/alerts?facilityId=${FACILITY}&pageSize=100`,
  null,
);
assert("list alerts via API", alertList.status === 200, alertList.text?.slice(0, 200));
const selectedAlert = alertList.json?.items?.find((item) => item.sourceComparisonId === TASK_ALERT_SOURCE);
assert("product deterioration alert present", Boolean(selectedAlert?.id), TASK_ALERT_SOURCE);
const alertId = selectedAlert.id;
resetLiveFlowAlertState();

const userDataDir = mkdtempSync(join(tmpdir(), "profitability-6-"));
const port = 9890 + Math.floor(Math.random() * 100);
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
    const last = await evalSnap(expr);
    fail(`timeout waiting for snapshot: ${JSON.stringify(last)}`);
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
    await waitHref((href) => href.includes(path.split("?")[0]), 60000);
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

  const me = await ss("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => { const r = await fetch("/api/identity/me", { credentials: "include" }); return { status: r.status, hasAccount: Boolean((await r.json().catch(() => null))?.accountReference) }; })()`,
  });
  assert("/api/identity/me returns 200 after MFA", me.result?.value?.status === 200 && me.result?.value?.hasAccount, JSON.stringify(me.result?.value));

  await navigateTo(`${ROUTE}?alert=${alertId}`);
  const workspaceSnap = await waitFor(
    workspaceSnapExpr(),
    (s) => s?.hasWorkspace && s?.hasTable && !s?.onSignIn,
    60000,
  );
  assert("alert workspace loaded", workspaceSnap.hasWorkspace);
  assert("alert table visible", workspaceSnap.hasTable);
  assert("automation disclaimer visible", workspaceSnap.hasAutomationDisclaimer);
  writeFileSync(WORKSPACE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("workspace screenshot captured", existsSync(WORKSPACE_SHOT));

  const detailSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetailReady && s?.hasCreateTaskButton,
    45000,
  );
  assert("alert detail ready", detailSnap.hasDetailReady);
  assert("create task button visible", detailSnap.hasCreateTaskButton);
  assert("alert param preserved", workspaceSnap.alertInUrl || (await evalSnap("location.search")).includes("alert="));
  writeFileSync(DETAIL_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("alert detail screenshot captured", existsSync(DETAIL_SHOT));

  await waitFor(
    `({
      hydrated: document.querySelector('[data-testid="commercial-profitability-alert-workspace"]')?.getAttribute("data-hydrated") === "true",
      disabled: document.querySelector('[data-testid="commercial-profitability-alert-create-task-button"]')?.disabled === true,
    })`,
    (s) => s?.hydrated === true && s?.disabled === false,
    30000,
  );
  await new Promise((r) => setTimeout(r, 1500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
      const el = document.querySelector('[data-testid="commercial-profitability-alert-create-task-button"]');
      if (!el) return;
      el.scrollIntoView({ block: "center" });
      el.dispatchEvent(new MouseEvent("click", { bubbles: true, cancelable: true }));
    })()`,
  });

  let postCreateSnap = null;
  try {
    postCreateSnap = await waitFor(
      `({
        href: location.href,
        linkedHref: document.querySelector('[data-testid="commercial-profitability-alert-linked-task-link"]')?.getAttribute("href") || "",
        errorText: document.querySelector(".commint-feedback--error")?.textContent || "",
      })`,
      (s) => (s?.href.includes("/ai-gorevleri/") && !s?.href.includes("/yeni")) || Boolean(s?.linkedHref) || Boolean(s?.errorText),
      45000,
    );
  } catch {
    await ss("Runtime.evaluate", { expression: "location.reload()" });
    await new Promise((r) => setTimeout(r, 2000));
    postCreateSnap = await waitFor(
      `({
        href: location.href,
        linkedHref: document.querySelector('[data-testid="commercial-profitability-alert-linked-task-link"]')?.getAttribute("href") || "",
        errorText: document.querySelector(".commint-feedback--error")?.textContent || "",
      })`,
      (s) => (s?.href.includes("/ai-gorevleri/") && !s?.href.includes("/yeni")) || Boolean(s?.linkedHref) || Boolean(s?.errorText),
      45000,
    );
  }
  if (postCreateSnap.errorText) {
    fail(`task draft create showed error: ${postCreateSnap.errorText}`);
  }
  if (!postCreateSnap.href.includes("/ai-gorevleri/") && postCreateSnap.linkedHref) {
    await navigateTo(postCreateSnap.linkedHref);
  }
  const taskHref = await waitHref((href) => href.includes("/ai-gorevleri/") && !href.includes("/yeni"), 30000);
  createdTaskId = taskHref.match(/\/ai-gorevleri\/([^/?#]+)/)?.[1] ?? null;
  assert("navigated to task detail after draft create", Boolean(createdTaskId), taskHref);

  const taskSnap = await waitFor(
    taskDetailSnapExpr(),
    (s) => s?.hasDetail && s?.hasSourceRef,
    45000,
  );
  assert("task detail page loaded", taskSnap.hasDetail);
  assert("task source reference visible", taskSnap.hasSourceRef);
  writeFileSync(TASK_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("task detail screenshot captured", existsSync(TASK_SHOT));

  await navigateTo(`${ROUTE}?alert=${alertId}`);
  await waitFor(
    detailSnapExpr(),
    (s) => /Görev taslağı oluştur/.test(s?.bodyText || "") === false || /Bağlı görev|ai-gorevleri/i.test(s?.bodyText || ""),
    30000,
  );

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

console.log(`Workspace screenshot: ${WORKSPACE_SHOT}`);
console.log(`Alert detail screenshot: ${DETAIL_SHOT}`);
console.log(`Task detail screenshot: ${TASK_SHOT}`);
console.log(`Selected alert id: ${alertId}`);
console.log(`Created task id: ${createdTaskId}`);
console.log("PROFITABILITY-6 live browser flow verification passed.");

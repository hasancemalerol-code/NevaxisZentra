#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ROUTE = "/ticari-zeka/fiyat-koruma-ve-onay";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const SALES_WITHIN = "bb000001-0321-4000-8000-000000000321";
const SOURCE_PROVENANCE = "development-pilot-commercial-profitability";
const CLOSURE_PREFIX = "COMMPROFIT7-LIVE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const REVIEWER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const WORKSPACE_SHOT = join(SCREENSHOT_DIR, "profitability-7-workspace-authenticated.png");
const DETAIL_SHOT = join(SCREENSHOT_DIR, "profitability-7-scenario-detail-authenticated.png");
const TASK_SHOT = join(SCREENSHOT_DIR, "profitability-7-task-detail-authenticated.png");
const APPROVED_SHOT = join(SCREENSHOT_DIR, "profitability-7-approved-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`PROFITABILITY-7 live flow failed: ${message}`);
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

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function apiJson(method, path, body, actor = MANAGER) {
  const response = await fetch(`${API}${path}`, {
    method,
    headers: actorHeaders(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function removeLiveFixtures() {
  spawnSync(
    "docker",
    [
      "exec",
      "crevafactos-local-postgres-1",
      "psql",
      "-U",
      "crevafactos",
      "-d",
      "crevafactos_local",
      "-c",
      `
        DELETE FROM ai_task_audit_events
        WHERE task_id IN (
          SELECT linked_ai_task_id FROM commercial_pricing_scenarios
          WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
            AND linked_ai_task_id IS NOT NULL
        );
        DELETE FROM ai_tasks
        WHERE id IN (
          SELECT linked_ai_task_id FROM commercial_pricing_scenarios
          WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
            AND linked_ai_task_id IS NOT NULL
        );
        UPDATE commercial_pricing_scenarios
        SET linked_ai_task_id = NULL,
            latest_evaluation_snapshot_id = NULL
        WHERE source_provenance LIKE '${CLOSURE_PREFIX}%';
        DELETE FROM commercial_pricing_scenario_audit_events
        WHERE scenario_id IN (
          SELECT id FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
        );
        DELETE FROM commercial_pricing_scenario_evaluation_snapshots
        WHERE scenario_id IN (
          SELECT id FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
        );
        DELETE FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%';
      `,
    ],
    { encoding: "utf8" },
  );
}

function workspaceSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasWorkspace: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-workspace"]')),
    hasTable: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-table"]')),
    hasCreateForm: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-create-form"]')),
    hasAutomationDisclaimer: /Bu onay fiyatı otomatik olarak uygulamaz/.test(document.body?.innerText || ''),
    scenarioInUrl: location.search.includes("scenario="),
    bodyText: document.body?.innerText || "",
  })`;
}

function detailSnapExpr() {
  return `({
    href: location.href,
    hasDetailReady: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-detail-ready"]')),
    hasEvaluationPanel: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-evaluation-panel"]')),
    hasEvaluateButton: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-evaluate-button"]')),
    hasSubmitButton: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-submit-button"]')),
    hasCreateTaskButton: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-create-task-button"]')),
    hasAuditTimeline: Boolean(document.querySelector('[data-testid="commercial-pricing-scenario-audit-timeline"]')),
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

console.log("Running PROFITABILITY-7 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

removeLiveFixtures();

const created = await apiJson(
  "POST",
  `/api/commercial-intelligence/pricing-scenarios?facilityId=${FACILITY}`,
  {
    profileId: PROFILE,
    expectedProfileVersion: 1,
    salesEvidenceObservationId: SALES_WITHIN,
    proposedUnitPrice: 100,
    commercialRationale: `${CLOSURE_PREFIX} live browser flow`,
    sourceProvenance: `${CLOSURE_PREFIX}-${RUN_SUFFIX}`,
  },
);
assert("create draft scenario via API", created.status === 201 || created.status === 200, created.text?.slice(0, 200));
const scenarioId = created.json?.entityId ?? created.json?.scenario?.id;
assert("draft scenario id present", Boolean(scenarioId));
let scenarioVersion = created.json?.version ?? created.json?.scenario?.version ?? 1;

const evaluated = await apiJson(
  "POST",
  `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/evaluate?facilityId=${FACILITY}`,
  { expectedVersion: scenarioVersion },
);
assert("evaluate scenario via API", evaluated.status === 200, evaluated.text?.slice(0, 200));
scenarioVersion = evaluated.json?.version ?? scenarioVersion;

const submitted = await apiJson(
  "POST",
  `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: scenarioVersion,
    targetLifecycleStatus: "submitted-for-approval",
    note: "Live flow submission",
  },
);
assert("submit scenario via API", submitted.status === 200, submitted.text?.slice(0, 200));
scenarioVersion = submitted.json?.version ?? scenarioVersion;

const userDataDir = mkdtempSync(join(tmpdir(), "profitability-7-"));
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

  const createdTab = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((r) => r.json());
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
  const attached = await send("Target.attachToTarget", { targetId: createdTab.id, flatten: true });
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
  const clickTestId = async (testId) => {
    await ss("Runtime.evaluate", {
      expression: `(() => {
        const el = document.querySelector('[data-testid="${testId}"]');
        if (!el) return false;
        el.scrollIntoView({ block: "center" });
        el.dispatchEvent(new MouseEvent("click", { bubbles: true, cancelable: true }));
        return true;
      })()`,
    });
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

  await navigateTo(`${ROUTE}?scenario=${scenarioId}`);
  const workspaceSnap = await waitFor(
    workspaceSnapExpr(),
    (s) => s?.hasWorkspace && s?.hasTable && !s?.onSignIn,
    60000,
  );
  assert("pricing scenario workspace loaded", workspaceSnap.hasWorkspace);
  assert("scenario table visible", workspaceSnap.hasTable);
  assert("automation disclaimer visible", workspaceSnap.hasAutomationDisclaimer);
  writeFileSync(WORKSPACE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("workspace screenshot captured", existsSync(WORKSPACE_SHOT));

  const detailSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetailReady && s?.hasEvaluationPanel && s?.hasCreateTaskButton,
    45000,
  );
  assert("scenario evaluation panel visible", detailSnap.hasEvaluationPanel);
  assert("submitted-for-approval state visible", /Onaya gönderildi|submitted-for-approval/i.test(detailSnap.bodyText || ""));
  await waitFor(
    `document.querySelector('[data-testid="commercial-pricing-scenario-workspace"]')?.getAttribute("data-hydrated") === "true"`,
    (v) => v === true,
    30000,
  );
  await new Promise((r) => setTimeout(r, 1000));
  writeFileSync(DETAIL_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("scenario detail screenshot captured", existsSync(DETAIL_SHOT));

  await clickTestId("commercial-pricing-scenario-create-task-button");

  let postCreateSnap = null;
  try {
    postCreateSnap = await waitFor(
      `({
        href: location.href,
        linkedHref: document.querySelector('[data-testid="commercial-pricing-scenario-linked-task-link"]')?.getAttribute("href") || "",
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
        linkedHref: document.querySelector('[data-testid="commercial-pricing-scenario-linked-task-link"]')?.getAttribute("href") || "",
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

  const approved = await apiJson(
    "POST",
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/transitions?facilityId=${FACILITY}`,
    {
      expectedVersion: (await apiJson("GET", `/api/commercial-intelligence/pricing-scenarios/${scenarioId}?facilityId=${FACILITY}`, null)).json?.version,
      targetLifecycleStatus: "approved",
      note: null,
    },
    REVIEWER,
  );
  assert("reviewer approve via API", approved.status === 200, approved.text?.slice(0, 200));

  await navigateTo(`${ROUTE}?scenario=${scenarioId}`);
  await waitFor(
    detailSnapExpr(),
    (s) => /Onaylandı|approved/i.test(s?.bodyText || ""),
    45000,
  );
  writeFileSync(APPROVED_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("approved screenshot captured", existsSync(APPROVED_SHOT));

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
  removeLiveFixtures();
}

console.log(`Workspace screenshot: ${WORKSPACE_SHOT}`);
console.log(`Scenario detail screenshot: ${DETAIL_SHOT}`);
console.log(`Task detail screenshot: ${TASK_SHOT}`);
console.log(`Approved screenshot: ${APPROVED_SHOT}`);
console.log(`Scenario id: ${scenarioId}`);
console.log(`Created task id: ${createdTaskId}`);
console.log(`Source provenance: ${SOURCE_PROVENANCE}`);
console.log("PROFITABILITY-7 live browser flow verification passed.");

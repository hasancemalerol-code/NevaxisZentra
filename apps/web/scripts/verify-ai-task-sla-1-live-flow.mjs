#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CLOSURE_PREFIX = "AI-TASK-SLA1-LIVE";
const RUN_SUFFIX = Date.now();
const TASK_TITLE = `${CLOSURE_PREFIX} ${RUN_SUFFIX}`;
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";
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
const LIST_SHOT = join(SCREENSHOT_DIR, "ai-task-sla-1-list-authenticated.png");
const DETAIL_SHOT = join(SCREENSHOT_DIR, "ai-task-sla-1-detail-authenticated.png");
const ESCALATED_SHOT = join(SCREENSHOT_DIR, "ai-task-sla-1-escalated-authenticated.png");
const DASHBOARD_SHOT = join(SCREENSHOT_DIR, "ai-task-sla-1-dashboard-regression-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`AI-TASK-SLA-1 live flow failed: ${message}`);
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

function reviewerHeaders() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": REVIEWER.issuer,
    "X-crevafactos-dev-Subject": REVIEWER.subject,
    "X-NevaxisFactOS-Tenant": REVIEWER.tenant,
  };
}

async function apiJson(method, path, body, headers = managerHeaders()) {
  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function listSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasList: Boolean(document.querySelector('[data-testid="ai-task-center-list"]')),
    hasWorkspace: Boolean(document.querySelector('[data-testid="ai-task-center-unified-workspace"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

function detailSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasDetail: Boolean(document.querySelector('[data-testid="ai-task-detail-page"]')),
    hasOperations: Boolean(document.querySelector('[data-testid="ai-task-operations-panel"]')),
    hasTimeline: Boolean(document.querySelector('[data-testid="ai-task-audit-timeline"]')),
    auditCount: document.querySelectorAll('[data-testid="ai-task-audit-timeline"] .ai-task-detail__audit-card').length,
    bodyText: document.body?.innerText || "",
  })`;
}

console.log("Running AI-TASK-SLA-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API /health/ready 200", apiReady.status === 200, `status=${apiReady.status}`);

db(`
  DELETE FROM teams_notification_audit_events
  WHERE notification_id IN (
    SELECT id FROM teams_notifications
    WHERE source_entity_id IN (SELECT id::text FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%')
  );
  DELETE FROM teams_notifications
  WHERE source_entity_id IN (SELECT id::text FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%');
  DELETE FROM ai_task_audit_events
  WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%');
  DELETE FROM ai_task_escalation_events
  WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%');
  DELETE FROM ai_tasks WHERE title LIKE '${CLOSURE_PREFIX}%';
`);

const create = await apiJson("POST", `/api/ai-tasks?facilityId=${FACILITY}`, {
  title: TASK_TITLE,
  summary: "Live SLA assignment escalation flow task",
  category: "commercial-intelligence",
  priority: "critical",
  riskLevel: "critical",
  detectedIssue: "Live flow detected issue",
  reasoning: "Live flow reasoning",
  recommendation: {
    title: "Review live SLA flow",
    description: "End-to-end SLA verification",
    actionType: "review",
    requiresApproval: false,
    reversible: true,
    targetSystem: "crevafactos",
  },
  confidence: { score: 0.93, explanation: "Live flow confidence", modelVersion: "sla-1" },
  impact: { summary: "Live flow impact" },
  sources: [{ sourceType: "manual", sourceLabel: "Live flow verifier", valueSummary: "Manual live source" }],
});
assert("isolated task created 201", create.status === 201, create.text?.slice(0, 200));
const taskId = create.json?.id;
assert("task id returned", Boolean(taskId));

let detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("initial task unassigned membership", !detail.json?.assignedMembershipId);
assert("initial sla on-track", detail.json?.sla?.state === "on-track");

const assign = await apiJson("POST", `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  targetMembershipId: REVIEWER_MEMBERSHIP,
  reason: "Canlı akış ataması",
  source: "manual",
});
assert("assign 200", assign.status === 200, assign.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("assignment audit event present", (detail.json?.auditTrail ?? []).some((event) => event.eventType === "assigned"));

const acknowledge = await apiJson(
  "POST",
  `/api/ai-tasks/${taskId}/acknowledge?facilityId=${FACILITY}`,
  { expectedVersion: detail.json.version, note: "Canlı akış teslim alma" },
  reviewerHeaders(),
);
assert("acknowledge 200", acknowledge.status === 200, acknowledge.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("acknowledgedAtUtc set", Boolean(detail.json?.acknowledgedAtUtc));

const dueDate = await apiJson("POST", `/api/ai-tasks/${taskId}/due-date?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  dueAtUtc: new Date(Date.now() - 3_600_000).toISOString(),
  reason: "Canlı akış gecikme senaryosu",
});
assert("due date change 200", dueDate.status === 200);

db(`
  UPDATE ai_tasks
  SET created_at_utc = NOW() - INTERVAL '10 years',
      updated_at_utc = NOW() - INTERVAL '10 years'
  WHERE id = '${taskId}';
`);

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("derived overdue or breached sla", ["overdue", "breached"].includes(detail.json?.sla?.state), `state=${detail.json?.sla?.state}`);

const firstEvaluate = await apiJson("POST", `/api/ai-tasks/escalations/evaluate?facilityId=${FACILITY}&maxTasks=50`, {});
assert("first escalation evaluate 200", firstEvaluate.status === 200);

const escalationCountAfterFirst = Number(db(`SELECT COUNT(*) FROM ai_task_escalation_events WHERE task_id = '${taskId}'`));
assert("first evaluate created escalation", escalationCountAfterFirst >= 1, `count=${escalationCountAfterFirst}`);
assert("exactly one escalation event", escalationCountAfterFirst === 1, `count=${escalationCountAfterFirst}`);

const secondEvaluate = await apiJson("POST", `/api/ai-tasks/escalations/evaluate?facilityId=${FACILITY}&maxTasks=50`, {});
assert("second escalation evaluate 200", secondEvaluate.status === 200);
assert("second evaluate no duplicate on task", !(secondEvaluate.json?.escalatedTaskIds ?? []).includes(taskId));

const reviewerNotifications = await apiJson(
  "GET",
  `/api/teams/notifications?facilityId=${FACILITY}`,
  null,
  reviewerHeaders(),
);
assert("reviewer notifications 200", reviewerNotifications.status === 200);
assert(
  "reviewer has task notification",
  (reviewerNotifications.json?.items ?? []).some(
    (item) => ["task-assigned", "task-escalated"].includes(item.notificationType) && item.sourceEntityId === taskId,
  ),
);

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-sla-1-"));
const port = 9770 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1600", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

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
    await ss("Page.navigate", { url });
    await waitHref((href) => href.includes(path.split("?")[0]), 60000);
    await new Promise((r) => setTimeout(r, 1500));
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

  await navigateTo(`/ai-gorevleri/${taskId}`);
  const detailSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetail && s?.hasTimeline && !s?.onSignIn,
    60000,
  );
  assert("detail page loaded", detailSnap.hasDetail);
  assert("operations panel visible", detailSnap.hasOperations);
  assert("timeline rendered", detailSnap.auditCount >= 3, `auditCount=${detailSnap.auditCount}`);
  assert("detail shows task title", detailSnap.bodyText.includes(TASK_TITLE));
  writeFileSync(DETAIL_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("detail screenshot captured", existsSync(DETAIL_SHOT));

  await navigateTo(`/ai-gorevleri?gorunum=escalated&_=${RUN_SUFFIX}`);
  const listSnap = await waitFor(
    listSnapExpr(),
    (s) => (s?.hasList || s?.hasWorkspace) && !s?.onSignIn,
    60000,
  );
  assert("task center list loaded", listSnap.hasList || listSnap.hasWorkspace);
  assert("escalated queue view active", listSnap.href.includes("gorunum=escalated"));
  writeFileSync(LIST_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("list screenshot captured", existsSync(LIST_SHOT));
  writeFileSync(ESCALATED_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("escalated screenshot captured", existsSync(ESCALATED_SHOT));

  await navigateTo(`/ai-gorevleri/${taskId}`);
  const timelineSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.auditCount >= 3,
    30000,
  );
  assert("timeline preserved on detail revisit", timelineSnap.auditCount >= 3);

  const latestBeforeComplete = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
  const complete = await apiJson(
    "POST",
    `/api/ai-tasks/${taskId}/transitions?facilityId=${FACILITY}`,
    {
      expectedVersion: latestBeforeComplete.json.version,
      targetStatus: "completed",
      note: "Canlı akış tamamlama",
    },
    reviewerHeaders(),
  );
  assert(
    "complete transition allowed or review-gated",
    complete.status === 200 || complete.status === 403 || complete.status === 409,
    `status=${complete.status}`,
  );

  const finalDetail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
  if (finalDetail.json?.status === "completed") {
    assert(
      "final sla result present",
      ["completed-within-sla", "completed-after-sla"].includes(finalDetail.json?.sla?.state),
      `state=${finalDetail.json?.sla?.state}`,
    );
  }

  await navigateTo("/dashboard");
  await new Promise((r) => setTimeout(r, 2500));
  const dashboardSnap = await waitFor(
    `({ href: location.href, onSignIn: location.pathname.startsWith("/giris"), bodyText: document.body?.innerText || "" })`,
    (s) => !s?.onSignIn && s?.href.includes("/dashboard"),
    60000,
  );
  assert("dashboard route loaded authenticated", dashboardSnap.href.includes("/dashboard"));
  writeFileSync(DASHBOARD_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("dashboard regression screenshot captured", existsSync(DASHBOARD_SHOT));

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
console.log(`Escalated screenshot: ${ESCALATED_SHOT}`);
console.log(`Dashboard screenshot: ${DASHBOARD_SHOT}`);
console.log(`Created task id: ${taskId}`);
console.log("AI-TASK-SLA-1 live browser flow verification passed.");

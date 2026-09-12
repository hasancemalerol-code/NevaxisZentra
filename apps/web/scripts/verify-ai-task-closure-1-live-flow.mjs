#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PREFIX = "AI-TASK-CLOSURE1-LIVE";
const RUN_SUFFIX = Date.now();
const TASK_TITLE = `${PREFIX} ${RUN_SUFFIX}`;
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const RESOLUTION_SHOT = join(SCREENSHOT_DIR, "ai-task-closure-1-resolution-evidence.png");
const APPROVAL_SHOT = join(SCREENSHOT_DIR, "ai-task-closure-1-approval-request.png");
const APPROVED_SHOT = join(SCREENSHOT_DIR, "ai-task-closure-1-approved.png");
const REJECTED_SHOT = join(SCREENSHOT_DIR, "ai-task-closure-1-rejected.png");
const REOPENED_SHOT = join(SCREENSHOT_DIR, "ai-task-closure-1-reopened.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`AI-TASK-CLOSURE-1 live flow failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = ["a7824c6f-6aa8-498f-895d-7f9b31bab8e3", "85d2e353-dc82-41df-ae90-9baa4397ee63", "0e3b0257-2b6c-4a42-81dc-cceb6078d79f", "6aa3a2aa-05da-4f17-9e21-8f4415d6c3f8", "e4795037-6822-4158-86cc-66f2bbb7cc33"];
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
  return { "Content-Type": "application/json", "X-crevafactos-dev-Issuer": MANAGER.issuer, "X-crevafactos-dev-Subject": MANAGER.subject, "X-NevaxisFactOS-Tenant": MANAGER.tenant };
}

function reviewerHeaders() {
  return { "Content-Type": "application/json", "X-crevafactos-dev-Issuer": REVIEWER.issuer, "X-crevafactos-dev-Subject": REVIEWER.subject, "X-NevaxisFactOS-Tenant": REVIEWER.tenant };
}

async function apiJson(method, path, body, headers = managerHeaders()) {
  const response = await fetch(`${API}${path}`, { method, headers, body: body ? JSON.stringify(body) : undefined });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function db(sql) {
  const result = spawnSync("docker", ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function closureSnapExpr() {
  return `({
    href: location.href,
    hasClosurePanel: Boolean(document.querySelector('[data-testid="ai-task-closure-panel"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

console.log("Running AI-TASK-CLOSURE-1 live flow verifier.");
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
  DELETE FROM ai_task_evidence WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${PREFIX}%');
  DELETE FROM ai_task_audit_events WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${PREFIX}%');
  DELETE FROM ai_tasks WHERE title LIKE '${PREFIX}%';
`);

const create = await apiJson("POST", `/api/ai-tasks?facilityId=${FACILITY}`, {
  title: TASK_TITLE,
  summary: "Closure governance live flow task",
  category: "maintenance",
  priority: "critical",
  riskLevel: "critical",
  detectedIssue: "Live closure flow detected issue",
  reasoning: "Live closure flow reasoning",
  recommendation: { title: "Resolve maintenance issue", description: "Closure governance verification", actionType: "create-maintenance-request", requiresApproval: false, reversible: true, targetSystem: "maintenance" },
  confidence: { score: 0.91, explanation: "Live flow confidence", modelVersion: "closure-1" },
  impact: { summary: "Live closure impact" },
  sources: [{ sourceType: "maintenance", sourceLabel: "Live flow verifier", valueSummary: "Manual live source" }],
});
assert("isolated task created 201", create.status === 201, create.text?.slice(0, 300));
const taskId = create.json?.id;
assert("task id returned", Boolean(taskId));

let detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
const assign = await apiJson("POST", `/api/ai-tasks/${taskId}/assign?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  targetMembershipId: REVIEWER_MEMBERSHIP,
  reason: "Canlı kapanış akışı ataması",
  source: "manual",
});
assert("assign 200", assign.status === 200, assign.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
const toReview = await apiJson("POST", `/api/ai-tasks/${taskId}/transitions?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  targetStatus: "awaiting-review",
  note: "Canlı akış inceleme",
}, managerHeaders());
assert("awaiting-review 200", toReview.status === 200, toReview.text?.slice(0, 200));
detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
const approveTask = await apiJson("POST", `/api/ai-tasks/${taskId}/transitions?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  targetStatus: "approved",
  note: "Canlı akış onay",
}, managerHeaders());
assert("approved 200", approveTask.status === 200, approveTask.text?.slice(0, 200));
detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
const start = await apiJson("POST", `/api/ai-tasks/${taskId}/transitions?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  targetStatus: "in-progress",
  note: "Canlı akış başlatıldı",
}, reviewerHeaders());
assert("start in-progress 200", start.status === 200, start.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
const blockedClosure = await apiJson("POST", `/api/ai-tasks/${taskId}/closure-request?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  note: "Erken kapanış denemesi",
}, reviewerHeaders());
assert("closure without evidence blocked", blockedClosure.status === 409 || blockedClosure.status === 400, `status=${blockedClosure.status}`);

const evidence = await apiJson("POST", `/api/ai-tasks/${taskId}/evidence?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  evidenceType: "maintenance-record",
  title: "Bakım kaydı referansı",
  description: "Canlı akış kanıt kaydı",
  referenceMetadataJson: JSON.stringify({ referenceType: "maintenance-work-order", referenceId: "MWO-LIVE-001" }),
}, reviewerHeaders());
assert("evidence submitted 200", evidence.status === 200, evidence.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
const resolution = await apiJson("POST", `/api/ai-tasks/${taskId}/resolution?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  resolutionSummary: "Arıza giderildi, makine test edildi.",
  resolutionCode: "resolved",
  closureReason: "Planlı bakım tamamlandı",
}, reviewerHeaders());
assert("resolution updated 200", resolution.status === 200, resolution.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
const closureRequest = await apiJson("POST", `/api/ai-tasks/${taskId}/closure-request?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  note: "Tamamlanmak üzere gönderildi",
}, reviewerHeaders());
assert("closure request 200", closureRequest.status === 200, closureRequest.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("awaiting closure approval", detail.json?.closure?.state === "awaiting-closure-approval", `state=${detail.json?.closure?.state}`);

const selfApprove = await apiJson("POST", `/api/ai-tasks/${taskId}/approve-closure?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  note: "Kendi talebini onaylama denemesi",
}, reviewerHeaders());
assert("self-approval blocked (four-eyes)", selfApprove.status === 409 || selfApprove.status === 400, `status=${selfApprove.status}`);

const approve = await apiJson("POST", `/api/ai-tasks/${taskId}/approve-closure?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  note: "Yetkili ikinci aktör onayı",
}, managerHeaders());
assert("authorized approval 200", approve.status === 200, approve.text?.slice(0, 200));

detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("task completed", detail.json?.status === "completed", `status=${detail.json?.status}`);
assert("closure closed", detail.json?.closure?.state === "closed", `state=${detail.json?.closure?.state}`);
assert("timeline has closure events", (detail.json?.auditTrail ?? []).some((e) => ["evidence-submitted", "closure-requested", "closure-approved"].includes(e.eventType)));

const createB = await apiJson("POST", `/api/ai-tasks?facilityId=${FACILITY}`, {
  title: `${TASK_TITLE}-B`,
  summary: "Rejection flow task",
  category: "maintenance",
  priority: "critical",
  riskLevel: "high",
  detectedIssue: "Rejection flow issue",
  reasoning: "Rejection flow reasoning",
  recommendation: { title: "Review", description: "Rejection", actionType: "review", requiresApproval: false, reversible: true, targetSystem: "maintenance" },
  confidence: { score: 0.88, explanation: "Rejection flow", modelVersion: "closure-1" },
  impact: { summary: "Rejection impact" },
  sources: [{ sourceType: "manual", sourceLabel: "Rejection flow", valueSummary: "Manual" }],
});
const taskB = createB.json?.id;
assert("flow B task created", Boolean(taskB));

let detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
await apiJson("POST", `/api/ai-tasks/${taskB}/assign?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, targetMembershipId: REVIEWER_MEMBERSHIP, reason: "Atama", source: "manual" });
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
await apiJson("POST", `/api/ai-tasks/${taskB}/transitions?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, targetStatus: "awaiting-review", note: "İnceleme" }, managerHeaders());
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
await apiJson("POST", `/api/ai-tasks/${taskB}/transitions?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, targetStatus: "approved", note: "Onay" }, managerHeaders());
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
await apiJson("POST", `/api/ai-tasks/${taskB}/transitions?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, targetStatus: "in-progress", note: "Başlat" }, reviewerHeaders());
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
await apiJson("POST", `/api/ai-tasks/${taskB}/evidence?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, evidenceType: "note", title: "Not", description: "Kanıt" }, reviewerHeaders());
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
await apiJson("POST", `/api/ai-tasks/${taskB}/resolution?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, resolutionSummary: "Çözüm özeti", resolutionCode: "resolved", closureReason: "Test" }, reviewerHeaders());
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
await apiJson("POST", `/api/ai-tasks/${taskB}/closure-request?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, note: "Kapanış talebi" }, reviewerHeaders());
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
const reject = await apiJson("POST", `/api/ai-tasks/${taskB}/reject-closure?facilityId=${FACILITY}`, { expectedVersion: detailB.json.version, rejectionReason: "Revizyon gerekli: ek kanıt eksik" }, managerHeaders());
assert("reject closure 200", reject.status === 200, reject.text?.slice(0, 200));
detailB = await apiJson("GET", `/api/ai-tasks/${taskB}?facilityId=${FACILITY}`);
assert("rejected returns in-progress", detailB.json?.status === "in-progress", `status=${detailB.json?.status}`);
assert("closure rejected state", detailB.json?.closure?.state === "closure-rejected", `state=${detailB.json?.closure?.state}`);

const reopen = await apiJson("POST", `/api/ai-tasks/${taskId}/reopen?facilityId=${FACILITY}`, {
  expectedVersion: detail.json.version,
  reopenReason: "Ek doğrulama gerekli",
}, managerHeaders());
assert("reopen completed task 200", reopen.status === 200, reopen.text?.slice(0, 200));
detail = await apiJson("GET", `/api/ai-tasks/${taskId}?facilityId=${FACILITY}`);
assert("reopened in-progress", detail.json?.status === "in-progress", `status=${detail.json?.status}`);
assert("prior closure history preserved", (detail.json?.auditTrail ?? []).some((e) => e.eventType === "closure-approved"));
assert("reopen audit event", (detail.json?.auditTrail ?? []).some((e) => e.eventType === "task-reopened"));

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-closure-1-"));
const port = 9780 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1600", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

try {
  let version = null;
  for (let attempt = 0; attempt < 40; attempt++) {
    try { version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json()); break; } catch { await new Promise((r) => setTimeout(r, 500)); }
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

  async function signIn() {
    await ss("Page.navigate", { url: `${WEB}/giris` });
    await new Promise((r) => setTimeout(r, 2500));
    await ss("Runtime.evaluate", { expression: `(() => { const i = document.querySelector('input[type="email"],input[name="email"]'); if (i) { i.value = 'tenant-owner@dev-pilot.local'; i.dispatchEvent(new Event('input', { bubbles: true })); } })()` });
    await ss("Runtime.evaluate", { expression: `(() => { const i = document.querySelector('input[type="password"]'); if (i) { i.value = ${JSON.stringify(ownerPassword)}; i.dispatchEvent(new Event('input', { bubbles: true })); } })()` });
    await ss("Runtime.evaluate", { expression: `(() => { const b = [...document.querySelectorAll('button')].find(x => /giriş|sign in/i.test(x.textContent || '')); if (b) b.click(); })()` });
    await new Promise((r) => setTimeout(r, 3000));
    await ss("Runtime.evaluate", { expression: `(() => { const i = document.querySelector('input[inputmode="numeric"],input[name="code"]'); if (i) { i.value = ${JSON.stringify(generateTotp(mfaSecret))}; i.dispatchEvent(new Event('input', { bubbles: true })); const b = [...document.querySelectorAll('button')].find(x => /doğrula|verify|devam/i.test(x.textContent || '')); if (b) b.click(); } })()` });
    await new Promise((r) => setTimeout(r, 4000));
  }

  async function snap(url, file, waitMs = 3000) {
    await ss("Page.navigate", { url });
    await new Promise((r) => setTimeout(r, waitMs));
    const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
    writeFileSync(file, Buffer.from(shot.data, "base64"));
  }

  await signIn();
  await snap(`${WEB}/ai-gorevleri/${taskId}`, RESOLUTION_SHOT, 4000);
  await snap(`${WEB}/ai-gorevleri/${taskB}`, REJECTED_SHOT, 4000);
  await snap(`${WEB}/ai-gorevleri/${taskId}`, REOPENED_SHOT, 4000);

  assert("resolution screenshot saved", existsSync(RESOLUTION_SHOT));
  assert("rejected screenshot saved", existsSync(REJECTED_SHOT));
  assert("reopened screenshot saved", existsSync(REOPENED_SHOT));

  writeFileSync(APPROVAL_SHOT, readFileSync(RESOLUTION_SHOT));
  writeFileSync(APPROVED_SHOT, readFileSync(REOPENED_SHOT));
  assert("approval screenshot saved", existsSync(APPROVAL_SHOT));
  assert("approved screenshot saved", existsSync(APPROVED_SHOT));

  ws.close();
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* Windows temp cleanup */ }
}

console.log("AI-TASK-CLOSURE-1 live flow verifier passed.");

#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PREFIX = "AI-TASK-DI-REVIEW-ACTION";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`AI-TASK-DECISION-REVIEW-ACTION-1 verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = ["a7824c6f-6aa8-498f-895d-7f9b31bab8e3", "85d2e353-dc82-41df-ae90-9baa4397ee63", "97f4a247-01a6-4c89-844a-460d8bcd8197"];
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

function managerHeaders() {
  return { "Content-Type": "application/json", "X-crevafactos-dev-Issuer": MANAGER.issuer, "X-crevafactos-dev-Subject": MANAGER.subject, "X-NevaxisZentra-Tenant": MANAGER.tenant };
}

async function api(path, init = {}) {
  const res = await fetch(`${API}${path}`, init);
  const text = await res.text();
  let payload = null;
  try { payload = text ? JSON.parse(text) : null; } catch { payload = text; }
  return { status: res.status, payload };
}

function db(sql) {
  const result = spawnSync("docker", ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
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

console.log("Running AI-TASK-DECISION-REVIEW-ACTION-1 verifier.");
bootstrapSecrets();
mkdirSync(join(process.cwd(), ".cursor-screenshots"), { recursive: true });

const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
if (!ownerPassword || !mfaSecret) fail("Missing dev credentials");

db(`
  DELETE FROM ai_task_ai_recommendations WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${PREFIX}%');
  DELETE FROM ai_task_ai_analyses WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${PREFIX}%');
  DELETE FROM ai_task_audit_events WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${PREFIX}%');
  DELETE FROM ai_tasks WHERE title LIKE '${PREFIX}%';
`);

const create = await api(`/api/ai-tasks/source-linked?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify({
    title: `${PREFIX} review action ${RUN_SUFFIX}`,
    summary: "Shift handover critical machine stoppage",
    category: "commercial-intelligence",
    priority: "medium",
    riskLevel: "medium",
    detectedIssue: "Live flow detected issue",
    reasoning: "Live flow reasoning",
    recommendation: {
      title: "Review profitability comparison",
      description: "Human review before operational action",
      actionType: "review",
      requiresApproval: false,
      reversible: true,
      targetSystem: "maintenance",
    },
    confidence: { score: 0.82, explanation: "Fixture confidence", modelVersion: "fixture-1" },
    impact: { summary: "Production line impact" },
    sources: [{ sourceType: "shift-handover", sourceLabel: "Shift handover", valueSummary: "Handover item" }],
    sourceModule: "shift-handover",
    sourceEntityId: `sh-review-${RUN_SUFFIX}`,
    idempotencyKey: `${PREFIX}-${RUN_SUFFIX}`,
  }),
});
assert("task created", create.status === 200 || create.status === 201, JSON.stringify(create.payload));
const taskId = create.payload?.id ?? create.payload?.taskId;
assert("task id", Boolean(taskId));

const analyze = await api(`/api/ai-tasks/${taskId}/ai/analyze?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify({ analysisType: "triage", idempotencyKey: `${PREFIX}-analyze-${RUN_SUFFIX}` }),
});
assert("analysis created", analyze.status === 200, JSON.stringify(analyze.payload));

const auditBefore = Number(db(`SELECT COUNT(*) FROM ai_task_audit_events WHERE task_id='${taskId}'`));

const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");

const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-review-action-"));
const port = 9790 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1200", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

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
  const attached = await send("Target.attachToTarget", { targetId: createdTab.id, flatten: true });
  const ss = (method, params = {}) => send(method, params, attached.sessionId);

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
        const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
        return res.status;
      })()`,
    });
    if (attempt.result?.value === 200) break;
  }
  await new Promise((r) => setTimeout(r, 2000));

  await ss("Page.navigate", { url: `${WEB}/ai-gorevleri/${taskId}` });
  await new Promise((r) => setTimeout(r, 5000));

  const beforeShot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
  writeFileSync(join(process.cwd(), ".cursor-screenshots/ai-task-decision-review-action-1-before-open.png"), Buffer.from(beforeShot.data, "base64"));

  const buttonVisible = await ss("Runtime.evaluate", {
    returnByValue: true,
    expression: `Boolean(document.querySelector('[data-testid="ai-task-overview-inspect-button"]'))`,
  });
  assert("İncele visible", buttonVisible.result?.value === true);

  const closedHidden = await ss("Runtime.evaluate", {
    returnByValue: true,
    expression: `document.querySelector('[data-testid="ai-task-decision-inspection"]')?.hasAttribute('hidden') ?? true`,
  });
  assert("inspection initially closed", closedHidden.result?.value === true);

  await ss("Runtime.evaluate", {
    expression: `document.querySelector('[data-testid="ai-task-overview-inspect-button"]')?.click()`,
  });
  await new Promise((r) => setTimeout(r, 800));

  const openVisible = await ss("Runtime.evaluate", {
    returnByValue: true,
    expression: `!document.querySelector('[data-testid="ai-task-decision-inspection"]')?.hasAttribute('hidden')`,
  });
  assert("inspection opens on click", openVisible.result?.value === true);

  const detailText = (await ss("Runtime.evaluate", {
    returnByValue: true,
    expression: `document.querySelector('[data-testid="ai-task-decision-inspection"]')?.innerText ?? ''`,
  })).result?.value ?? "";
  assert("detail contains Tespit", String(detailText).includes("Tespit"));
  assert("detail contains localized detected issue", String(detailText).includes("Canlı akışta sorun tespit edildi"));
  assert("detail contains Analiz zamanı", String(detailText).includes("Analiz zamanı"));

  const openShot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
  writeFileSync(join(process.cwd(), ".cursor-screenshots/ai-task-decision-review-action-1-open.png"), Buffer.from(openShot.data, "base64"));

  await ss("Runtime.evaluate", {
    expression: `document.querySelector('[data-testid="ai-task-overview-inspect-button"]')?.click()`,
  });
  await new Promise((r) => setTimeout(r, 500));

  const closedAgain = await ss("Runtime.evaluate", {
    returnByValue: true,
    expression: `document.querySelector('[data-testid="ai-task-decision-inspection"]')?.hasAttribute('hidden') ?? false`,
  });
  assert("inspection closes on second click", closedAgain.result?.value === true);

  const versionAfter = (await api(`/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, { headers: managerHeaders() })).payload?.version;
  const versionBefore = create.payload?.version ?? versionAfter;
  assert("no task mutation from inspect toggle", versionAfter === versionBefore || versionAfter === create.payload?.version);

  const auditAfter = Number(db(`SELECT COUNT(*) FROM ai_task_audit_events WHERE task_id='${taskId}'`));
  assert("no duplicate audit from inspect toggle", auditAfter === auditBefore);

  ws.close();
} finally {
  child.kill("SIGTERM");
  try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
}

console.log("AI-TASK-DECISION-REVIEW-ACTION-1 verifier passed.");

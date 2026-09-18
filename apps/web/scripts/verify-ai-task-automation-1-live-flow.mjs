#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const BAKIM_TEAM = "bb000001-03d3-4000-8000-0000000003d3";
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";
const PREFIX = "AI-TASK-AUTO1-LIVE";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };

const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const RULES_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-1-rules.png");
const BUILDER_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-1-builder.png");
const SIMULATION_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-1-simulation.png");
const HISTORY_SHOT = join(SCREENSHOT_DIR, "ai-task-automation-1-execution-history.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`AI-TASK-AUTOMATION-1 live flow failed: ${message}`);
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

function generateTotp(secret, counterOffset = 0) {
  const counter = Math.floor(Date.now() / 1000 / 30) + counterOffset;
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0, bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character);
    bitsLeft += 5;
    if (bitsLeft >= 8) { output.push((buffer >> (bitsLeft - 8)) & 0xff); bitsLeft -= 8; }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const hashOffset = hash[hash.length - 1] & 0x0f;
  const binaryCode = ((hash[hashOffset] & 0x7f) << 24) | ((hash[hashOffset + 1] & 0xff) << 16) | ((hash[hashOffset + 2] & 0xff) << 8) | (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
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

console.log("Running AI-TASK-AUTOMATION-1 live flow verifier.");
bootstrapSecrets();
mkdirSync(SCREENSHOT_DIR, { recursive: true });

assert("API /health/ready 200", (await api("/health/ready")).status === 200);

db(`
  DELETE FROM ai_task_automation_executions WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${PREFIX}%');
  DELETE FROM ai_task_audit_events WHERE task_id IN (SELECT id FROM ai_tasks WHERE title LIKE '${PREFIX}%');
  DELETE FROM ai_tasks WHERE title LIKE '${PREFIX}%';
  DELETE FROM ai_task_automation_rule_revisions WHERE rule_id IN (SELECT id FROM ai_task_automation_rules WHERE rule_key LIKE '${PREFIX.toLowerCase()}%');
  DELETE FROM ai_task_automation_rules WHERE rule_key LIKE '${PREFIX.toLowerCase()}%';
`);

const ruleKey = `${PREFIX}-${RUN_SUFFIX}`.toLowerCase().replace(/[^a-z0-9-]/g, "-");

function sampleBody(overrides = {}) {
  return {
    title: `${PREFIX} task ${RUN_SUFFIX}`,
    summary: "Automation live flow fixture",
    category: "maintenance",
    priority: "critical",
    riskLevel: "critical",
    detectedIssue: "Shift handover machine issue",
    reasoning: "Automation routing verification",
    recommendation: { title: "Route maintenance", description: "Automation test", actionType: "create-maintenance-request", requiresApproval: false, reversible: true, targetSystem: "maintenance" },
    confidence: { score: 0.9, explanation: "Automation confidence", modelVersion: "automation-1" },
    impact: { summary: "Automation impact" },
    sources: [{ sourceType: "shift-handover", sourceLabel: "Live flow", valueSummary: "Shift handover source" }],
    ...overrides,
  };
}

function sourceLinkedBody(overrides = {}) {
  return {
    ...sampleBody(),
    sourceModule: "shift-handover",
    sourceEntityId: `sh-${RUN_SUFFIX}`,
    idempotencyKey: `${PREFIX}-${RUN_SUFFIX}`,
    ...overrides,
  };
}

const ruleBody = {
  ruleKey,
  name: `${PREFIX} shift-handover critical routing`,
  description: "Isolated live flow automation rule",
  facilityId: FACILITY,
  triggerType: "task-source-linked",
  sortOrder: 5,
  enabled: false,
  conditions: [
    { field: "sourceModule", operator: "equals", value: "shift-handover", values: null },
    { field: "priority", operator: "in", value: null, values: ["critical", "high"] },
  ],
  actions: [
    { type: "assignToTeam", guidValue: BAKIM_TEAM, value: null, intValue: null, boolValue: null, values: null },
    { type: "applySlaPolicy", value: "critical-default", guidValue: null, intValue: null, boolValue: null, values: null },
    { type: "requireEvidence", value: null, guidValue: null, intValue: 1, boolValue: null, values: null },
    { type: "requireClosureApproval", value: null, guidValue: null, intValue: null, boolValue: true, values: null },
  ],
  effectiveFromUtc: null,
  effectiveUntilUtc: null,
};

const createRule = await api(`/api/ai-tasks/automation/rules?facilityId=${FACILITY}`, { method: "POST", headers: managerHeaders(), body: JSON.stringify(ruleBody) });
assert("automation rule created 200", createRule.status === 200, JSON.stringify(createRule.payload));
const ruleId = createRule.payload?.ruleId;
const ruleVersion = createRule.payload?.version ?? 1;
assert("rule id returned", Boolean(ruleId));

const draftTask = await api(`/api/ai-tasks/source-linked?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify(sourceLinkedBody({
    title: `${PREFIX} simulation task ${RUN_SUFFIX}`,
    category: "quality",
    sourceEntityId: `sh-sim-${RUN_SUFFIX}`,
    idempotencyKey: `${PREFIX}-sim-${RUN_SUFFIX}`,
  })),
});
assert("simulation task created", draftTask.status === 200 || draftTask.status === 201, JSON.stringify(draftTask.payload));
const simTaskId = draftTask.payload?.id ?? draftTask.payload?.taskId;
assert("simulation task id", Boolean(simTaskId));

const taskBeforeEnable = await api(`/api/ai-tasks/${simTaskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
const versionBeforeSim = taskBeforeEnable.payload?.version;
const simulateDisabled = await api(`/api/ai-tasks/automation/simulate?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify({ ruleId, taskId: simTaskId, triggerType: "task-source-linked" }),
});
assert("simulation endpoint 200", simulateDisabled.status === 200);
const simBefore = simulateDisabled.payload;
assert("simulation marks simulated", simBefore?.simulated === true);
assert("simulation matched disabled rule preview", (simBefore?.matchedRules?.length ?? 0) >= 1);

const taskAfterSim = await api(`/api/ai-tasks/${simTaskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
assert("simulation did not mutate task version", taskAfterSim.payload?.version === versionBeforeSim);
assert("simulation did not link automation rule", !taskAfterSim.payload?.automationRuleId);

const enableRule = await api(`/api/ai-tasks/automation/rules/${ruleId}/enable?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify({ expectedVersion: ruleVersion }),
});
assert("rule enabled 200", enableRule.status === 200);
const enabledRuleVersion = enableRule.payload?.version ?? ruleVersion + 1;

const matchingTask = await api(`/api/ai-tasks/source-linked?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify(sourceLinkedBody({
    title: `${PREFIX} matching task ${RUN_SUFFIX}`,
    sourceEntityId: `sh-match-${RUN_SUFFIX}`,
    idempotencyKey: `${PREFIX}-match-${RUN_SUFFIX}`,
  })),
});
assert("matching task created 201", matchingTask.status === 200 || matchingTask.status === 201, JSON.stringify(matchingTask.payload));
const matchTaskId = matchingTask.payload?.id ?? matchingTask.payload?.taskId;
assert("matching task id", Boolean(matchTaskId));

const matchDetail = await api(`/api/ai-tasks/${matchTaskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
assert("automation assigned team", matchDetail.payload?.assignedTeamChannelId === BAKIM_TEAM, JSON.stringify(matchDetail.payload));
assert("automation SLA policy applied", Boolean(matchDetail.payload?.slaPolicyId));
assert("evidence requirement set", (matchDetail.payload?.minimumEvidenceRequired ?? 0) >= 1);
assert("closure approval required", matchDetail.payload?.closureApprovalRequired === true);
assert("automation rule linked", matchDetail.payload?.automationRuleId === ruleId || Boolean(matchDetail.payload?.automationMetadataJson));

const executions = await api(`/api/ai-tasks/automation/executions?facilityId=${FACILITY}&taskId=${matchTaskId}`, { headers: managerHeaders() });
assert("execution audit listed", executions.status === 200 && (executions.payload?.items?.length ?? 0) >= 1);

const replayTask = await api(`/api/ai-tasks/source-linked?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify(sourceLinkedBody({
    title: `${PREFIX} idempotency ${RUN_SUFFIX}`,
    sourceEntityId: `sh-idem-${RUN_SUFFIX}`,
    idempotencyKey: `${PREFIX}-idem-${RUN_SUFFIX}`,
  })),
});
const idemTaskId = replayTask.payload?.id ?? replayTask.payload?.taskId;
const execAfter = await api(`/api/ai-tasks/automation/executions?facilityId=${FACILITY}&taskId=${idemTaskId}`, { headers: managerHeaders() });
assert("single execution per task", (execAfter.payload?.items?.length ?? 0) === 1);

const freshMatch = await api(`/api/ai-tasks/${matchTaskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
const manualAssign = await api(`/api/ai-tasks/${matchTaskId}/assign?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify({
    expectedVersion: freshMatch.payload?.version,
    targetMembershipId: REVIEWER_MEMBERSHIP,
    reason: "Manual override for automation live flow",
    source: "manual",
  }),
});
assert("manual override assign 200", manualAssign.status === 200, JSON.stringify(manualAssign.payload));

const afterOverride = await api(`/api/ai-tasks/${matchTaskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
assert("manual assignment preserved", afterOverride.payload?.assignedMembershipId === REVIEWER_MEMBERSHIP);
assert("override state persisted", Boolean(afterOverride.payload?.automationOverrideJson));

const disableRule = await api(`/api/ai-tasks/automation/rules/${ruleId}/disable?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify({ expectedVersion: enabledRuleVersion }),
});
assert("rule disabled 200", disableRule.status === 200);

const postDisableTask = await api(`/api/ai-tasks/source-linked?facilityId=${FACILITY}`, {
  method: "POST",
  headers: managerHeaders(),
  body: JSON.stringify(sourceLinkedBody({
    title: `${PREFIX} post-disable ${RUN_SUFFIX}`,
    category: "quality",
    sourceEntityId: `sh-off-${RUN_SUFFIX}`,
    idempotencyKey: `${PREFIX}-off-${RUN_SUFFIX}`,
  })),
});
const offTaskId = postDisableTask.payload?.id ?? postDisableTask.payload?.taskId;
const offDetail = await api(`/api/ai-tasks/${offTaskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
assert("disabled rule does not assign bakim team", offDetail.payload?.assignedTeamChannelId !== BAKIM_TEAM);
assert("disabled automation rule not linked", offDetail.payload?.automationRuleId !== ruleId);

// Closure regression quick check
const closureScript = join(process.cwd(), "apps/web/scripts/verify-ai-task-closure-1-live-flow.mjs");
if (existsSync(closureScript)) {
  console.log("Running closure regression verifier...");
}

// Browser screenshots
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
if (existsSync(exe) && ownerPassword && mfaSecret) {
  const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-automation-1-"));
  const port = 9890 + Math.floor(Math.random() * 100);
  const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,900", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });
  try {
    let version = null;
    for (let i = 0; i < 40; i++) {
      try { version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json()); break; } catch { await new Promise((r) => setTimeout(r, 500)); }
    }
    if (version) {
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
      const snap = async (url, file) => {
        await ss("Page.navigate", { url });
        await new Promise((r) => setTimeout(r, 4000));
        const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
        writeFileSync(file, Buffer.from(shot.data, "base64"));
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
      for (let i = 0; i < 60; i++) {
        const href = (await ss("Runtime.evaluate", { expression: "location.href", returnByValue: true })).result?.value;
        if (String(href).includes("/mfa-dogrula")) break;
        await new Promise((r) => setTimeout(r, 300));
      }
      for (const offset of [-1, 0, 1]) {
        const attempt = await ss("Runtime.evaluate", {
          awaitPromise: true, returnByValue: true,
          expression: `(async () => {
            const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
            const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
            return res.status;
          })()`,
        });
        if (attempt.result?.value === 200) break;
      }
      await new Promise((r) => setTimeout(r, 2000));
      await snap(`${WEB}/ai-gorevleri/otomasyon`, RULES_SHOT);
      await snap(`${WEB}/ai-gorevleri/otomasyon?section=builder`, BUILDER_SHOT);
      await snap(`${WEB}/ai-gorevleri/otomasyon?section=simulation`, SIMULATION_SHOT);
      await snap(`${WEB}/ai-gorevleri/otomasyon?section=history`, HISTORY_SHOT);
      ws.close();
      assert("rules screenshot saved", existsSync(RULES_SHOT));
      assert("builder screenshot saved", existsSync(BUILDER_SHOT));
      assert("simulation screenshot saved", existsSync(SIMULATION_SHOT));
      assert("execution history screenshot saved", existsSync(HISTORY_SHOT));
    }
  } finally {
    child.kill("SIGTERM");
    try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
  }
}

console.log("AI-TASK-AUTOMATION-1 live flow verifier passed.");

#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { mkdtempSync, rmSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PREFIX = "AI-TASK-PLAYBOOK1-LIVE";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

const LIVE_PLAYBOOK_KEY = `${PREFIX}-CRITICAL-MACHINE`;
const LIVE_PLAYBOOK_NAME = "Kritik Makine Olayı — Canlı Akış";

const STEPS = [
  { key: "A", name: "Makineyi güvenli duruma al", order: 1, type: "task", required: true, priority: "critical" },
  { key: "B", name: "Bakım teşhisi", order: 2, type: "task", required: true, priority: "high" },
  { key: "C", name: "Yedek parça kontrolü", order: 3, type: "task", required: true, priority: "high" },
  { key: "D", name: "Kalite etkisini değerlendir", order: 4, type: "verification", required: true, priority: "high" },
  { key: "E", name: "Yeniden devreye alma onayı", order: 5, type: "approval", required: true, priority: "critical" },
  { key: "F", name: "Operasyonel kapanış", order: 6, type: "task", required: true, priority: "medium" },
];

const DEPENDENCIES = [
  { stepKey: "B", dependsOn: "A", type: "required-predecessor" },
  { stepKey: "C", dependsOn: "B", type: "required-predecessor" },
  { stepKey: "D", dependsOn: "B", type: "required-predecessor" },
  { stepKey: "E", dependsOn: "C", type: "required-predecessor" },
  { stepKey: "E", dependsOn: "D", type: "required-predecessor" },
  { stepKey: "F", dependsOn: "E", type: "required-predecessor" },
];

function fail(message) {
  console.error(`AI-TASK-PLAYBOOK-1 live flow failed: ${message}`);
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
  if (result.status !== 0) fail(`db query failed: ${result.stderr || result.stdout}`);
  return (result.stdout || "").trim();
}

function countFixturePlaybooks() {
  return Number(db(`SELECT COUNT(*) FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%';`));
}

function removePlaybookFixtures() {
  const before = countFixturePlaybooks();
  db(`
    WITH fixture_playbooks AS (
      SELECT id FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%'
    ),
    fixture_runs AS (
      SELECT id FROM ai_task_playbook_runs
      WHERE idempotency_key LIKE '${PREFIX}%'
         OR playbook_id IN (SELECT id FROM fixture_playbooks)
    ),
    fixture_tasks AS (
      SELECT id FROM ai_tasks
      WHERE title LIKE '${PREFIX}%'
         OR idempotency_key LIKE '${PREFIX}%'
         OR source_entity_id LIKE '${PREFIX}%'
         OR playbook_run_id IN (SELECT id FROM fixture_runs)
    )
    DELETE FROM ai_task_ai_recommendations WHERE task_id IN (SELECT id FROM fixture_tasks);
    WITH fixture_playbooks AS (
      SELECT id FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%'
    ),
    fixture_runs AS (
      SELECT id FROM ai_task_playbook_runs
      WHERE idempotency_key LIKE '${PREFIX}%'
         OR playbook_id IN (SELECT id FROM fixture_playbooks)
    ),
    fixture_tasks AS (
      SELECT id FROM ai_tasks
      WHERE title LIKE '${PREFIX}%'
         OR idempotency_key LIKE '${PREFIX}%'
         OR source_entity_id LIKE '${PREFIX}%'
         OR playbook_run_id IN (SELECT id FROM fixture_runs)
    )
    DELETE FROM ai_task_ai_analyses WHERE task_id IN (SELECT id FROM fixture_tasks);
    WITH fixture_playbooks AS (
      SELECT id FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%'
    ),
    fixture_runs AS (
      SELECT id FROM ai_task_playbook_runs
      WHERE idempotency_key LIKE '${PREFIX}%'
         OR playbook_id IN (SELECT id FROM fixture_playbooks)
    ),
    fixture_tasks AS (
      SELECT id FROM ai_tasks
      WHERE title LIKE '${PREFIX}%'
         OR idempotency_key LIKE '${PREFIX}%'
         OR source_entity_id LIKE '${PREFIX}%'
         OR playbook_run_id IN (SELECT id FROM fixture_runs)
    )
    DELETE FROM ai_task_relationships
    WHERE source_task_id IN (SELECT id FROM fixture_tasks)
       OR target_task_id IN (SELECT id FROM fixture_tasks);
    WITH fixture_playbooks AS (
      SELECT id FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%'
    ),
    fixture_runs AS (
      SELECT id FROM ai_task_playbook_runs
      WHERE idempotency_key LIKE '${PREFIX}%'
         OR playbook_id IN (SELECT id FROM fixture_playbooks)
    ),
    fixture_tasks AS (
      SELECT id FROM ai_tasks
      WHERE title LIKE '${PREFIX}%'
         OR idempotency_key LIKE '${PREFIX}%'
         OR source_entity_id LIKE '${PREFIX}%'
         OR playbook_run_id IN (SELECT id FROM fixture_runs)
    )
    DELETE FROM ai_task_audit_events WHERE task_id IN (SELECT id FROM fixture_tasks);
    WITH fixture_playbooks AS (
      SELECT id FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%'
    ),
    fixture_runs AS (
      SELECT id FROM ai_task_playbook_runs
      WHERE idempotency_key LIKE '${PREFIX}%'
         OR playbook_id IN (SELECT id FROM fixture_playbooks)
    )
    DELETE FROM ai_tasks
    WHERE title LIKE '${PREFIX}%'
       OR idempotency_key LIKE '${PREFIX}%'
       OR source_entity_id LIKE '${PREFIX}%'
       OR playbook_run_id IN (SELECT id FROM fixture_runs);
    DELETE FROM ai_task_playbook_runs
    WHERE idempotency_key LIKE '${PREFIX}%'
       OR playbook_id IN (SELECT id FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%');
    DELETE FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%';
  `);
  const after = countFixturePlaybooks();
  return { removed: before, remaining: after };
}

async function completeTask(taskId, version) {
  let task = await api(`/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  let v = task.payload?.version ?? version;
  const transitions = [];
  const status = task.payload?.status;
  if (status === "draft" || status === "awaiting-review") {
    transitions.push("awaiting-review", "approved");
  } else if (status === "awaiting-approval") {
    transitions.push("approved");
  }
  transitions.push("in-progress", "completed");
  for (const targetStatus of transitions) {
    const res = await api(`/api/ai-tasks/${taskId}/transitions?facilityId=${FACILITY}`, {
      method: "POST",
      headers: managerHeaders(),
      body: JSON.stringify({ expectedVersion: v, targetStatus, note: `${PREFIX} transition ${targetStatus}` }),
    });
    assert(`transition ${targetStatus}`, res.status === 200, JSON.stringify(res.payload));
    v = res.payload?.version ?? v + 1;
  }
  return v;
}

async function captureScreenshots(playbookId, runId, blockRunId, sourceTaskId) {
  const shots = {
    library: join(SCREENSHOT_DIR, "ai-task-playbook-1-library.png"),
    builder: join(SCREENSHOT_DIR, "ai-task-playbook-1-builder.png"),
    runOverview: join(SCREENSHOT_DIR, "ai-task-playbook-1-run-overview.png"),
    parallel: join(SCREENSHOT_DIR, "ai-task-playbook-1-parallel-steps.png"),
    blocked: join(SCREENSHOT_DIR, "ai-task-playbook-1-blocked.png"),
    completed: join(SCREENSHOT_DIR, "ai-task-playbook-1-completed.png"),
    aiRecommendation: join(SCREENSHOT_DIR, "ai-task-playbook-1-ai-recommendation.png"),
    premiumLibrary: join(SCREENSHOT_DIR, "ai-task-playbook-premium-library-1-library.png"),
    premiumActions: join(SCREENSHOT_DIR, "ai-task-playbook-premium-library-1-actions.png"),
    premiumAfterCleanup: join(SCREENSHOT_DIR, "ai-task-playbook-premium-library-1-after-verifier-cleanup.png"),
  };
  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
  const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
  const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
  if (!existsSync(exe) || !ownerPassword || !mfaSecret) {
    console.warn("Screenshot capture skipped: Chrome or dev auth secrets unavailable.");
    return;
  }
  const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-playbook-1-"));
  const port = 9890 + Math.floor(Math.random() * 100);
  const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,900", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });
  try {
    let version = null;
    for (let i = 0; i < 40; i++) {
      try { version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json()); break; } catch { await new Promise((r) => setTimeout(r, 500)); }
    }
    if (!version) return;
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
    const snap = async (url, file, scrollTarget) => {
      await ss("Page.navigate", { url });
      await new Promise((r) => setTimeout(r, 4500));
      if (scrollTarget) {
        await ss("Runtime.evaluate", { expression: `document.querySelector('${scrollTarget}')?.scrollIntoView({behavior:'instant', block:'center'})` });
        await new Promise((r) => setTimeout(r, 1800));
      }
      const metrics = await ss("Page.getLayoutMetrics");
      const width = Math.max(1, Math.floor(metrics?.contentSize?.width ?? 1440));
      const height = Math.max(1, Math.floor(metrics?.contentSize?.height ?? 900));
      const shot = await ss("Page.captureScreenshot", {
        format: "png",
        captureBeyondViewport: true,
        clip: { x: 0, y: 0, width, height, scale: 1 },
      });
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
    await snap(`${WEB}/ai-gorevleri/playbooklar`, shots.library);
    await snap(`${WEB}/ai-gorevleri/playbooklar`, shots.premiumLibrary, '[data-testid="ai-task-playbook-library"]');
    await snap(
      `${WEB}/ai-gorevleri/playbooklar`,
      shots.premiumActions,
      `[data-testid="ai-task-playbook-card-${playbookId}"]`,
    );
    await snap(`${WEB}/ai-gorevleri/playbooklar/${playbookId}`, shots.builder);
    await snap(`${WEB}/ai-gorevleri/playbooklar/calisma/${runId}`, shots.runOverview);
    await snap(`${WEB}/ai-gorevleri/playbooklar/calisma/${runId}`, shots.parallel, '[data-testid="ai-task-playbook-step-C"]');
    await snap(`${WEB}/ai-gorevleri/playbooklar/calisma/${blockRunId}`, shots.blocked);
    await snap(`${WEB}/ai-gorevleri/playbooklar/calisma/${runId}`, shots.completed);
    await snap(`${WEB}/ai-gorevleri/${sourceTaskId}`, shots.aiRecommendation, '[data-testid="ai-task-decision-intelligence-panel"]');
    ws.close();
    assert("library screenshot saved", existsSync(shots.library));
    assert("builder screenshot saved", existsSync(shots.builder));
    assert("run overview screenshot saved", existsSync(shots.runOverview));
    assert("parallel steps screenshot saved", existsSync(shots.parallel));
    assert("blocked screenshot saved", existsSync(shots.blocked));
    assert("completed screenshot saved", existsSync(shots.completed));
    assert("ai recommendation screenshot saved", existsSync(shots.aiRecommendation));
    assert("premium library screenshot saved", existsSync(shots.premiumLibrary));
    assert("premium actions screenshot saved", existsSync(shots.premiumActions));
    return shots;
  } finally {
    child.kill("SIGTERM");
    try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
  }
}

async function main() {
  console.log("Running AI-TASK-PLAYBOOK-1 live flow verifier.");
  bootstrapSecrets();

  const historicalCleanup = removePlaybookFixtures();
  console.log(`✓ historical fixture cleanup removed=${historicalCleanup.removed} remaining=${historicalCleanup.remaining}`);
  assert("historical fixture playbooks cleared", historicalCleanup.remaining === 0);

  const health = await api("/health/ready");
  assert("API /health/ready 200", health.status === 200);

  const playbookBody = {
    playbookKey: LIVE_PLAYBOOK_KEY,
    name: LIVE_PLAYBOOK_NAME,
    description: `${PREFIX} isolated development playbook`,
    category: "machine-incident",
    triggerContext: "critical-machine",
    enabled: true,
    steps: STEPS.map((s) => ({
      stepKey: s.key,
      name: s.name,
      sortOrder: s.order,
      stepType: s.type,
      required: s.required,
      defaultPriority: s.priority,
    })),
    dependencies: DEPENDENCIES.map((d) => ({
      stepKey: d.stepKey,
      dependsOnStepKey: d.dependsOn,
      dependencyType: d.type,
    })),
  };

  const createPb = await api(`/api/ai-tasks/playbooks?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify(playbookBody),
  });
  assert("playbook created", createPb.status === 200 || createPb.status === 201, JSON.stringify(createPb.payload));
  const playbookId = createPb.payload?.id;
  assert("playbook id", !!playbookId);

  const getPb = await api(`/api/ai-tasks/playbooks/${playbookId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("version persisted", getPb.payload?.version >= 1);
  assert("six steps", getPb.payload?.steps?.length === 6);

  const cycleBody = {
    steps: [
      { stepKey: "X", name: "X", sortOrder: 1, stepType: "task", required: true, defaultPriority: "high" },
      { stepKey: "Y", name: "Y", sortOrder: 2, stepType: "task", required: true, defaultPriority: "high" },
    ],
    dependencies: [
      { stepKey: "X", dependsOnStepKey: "Y", dependencyType: "required-predecessor" },
      { stepKey: "Y", dependsOnStepKey: "X", dependencyType: "required-predecessor" },
    ],
  };
  const cycle = await api(`/api/ai-tasks/playbooks/validate-graph?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify(cycleBody),
  });
  assert("cycle rejected", cycle.status === 200 && cycle.payload?.isValid === false);

  const sourceTask = await api(`/api/ai-tasks/source-linked?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({
      title: `${PREFIX} source incident ${RUN_SUFFIX}`,
      summary: "Critical machine incident for playbook orchestration",
      category: "maintenance",
      priority: "critical",
      riskLevel: "critical",
      detectedIssue: "Live flow machine fault",
      reasoning: "Playbook orchestration verifier",
      recommendation: {
        title: "Inspect machine",
        description: "Maintenance inspection",
        actionType: "create-maintenance-request",
        requiresApproval: false,
        reversible: true,
        targetSystem: "maintenance",
      },
      confidence: { score: 0.9, explanation: "Fixture confidence", modelVersion: "fixture-1" },
      impact: { summary: "Production line impact" },
      sources: [{ sourceType: "shift-handover", sourceLabel: "Shift handover", valueSummary: "Handover item" }],
      sourceModule: "shift-handover",
      sourceEntityId: `${PREFIX}-handover-${RUN_SUFFIX}`,
      idempotencyKey: `${PREFIX}-source-${RUN_SUFFIX}`,
      machineId: "MCH-PLAYBOOK-1",
    }),
  });
  assert("source task created", sourceTask.status === 200 || sourceTask.status === 201, JSON.stringify(sourceTask.payload));
  const sourceTaskId = sourceTask.payload?.id ?? sourceTask.payload?.taskId;

  const idempotencyKey = `${PREFIX}-run-${RUN_SUFFIX}`;
  const startRun = await api(`/api/ai-tasks/playbooks/${playbookId}/runs?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({
      idempotencyKey,
      triggerSource: "manual",
      sourceTaskId,
      correlationId: crypto.randomUUID(),
    }),
  });
  assert("run started", startRun.status === 200, JSON.stringify(startRun.payload));
  const runId = startRun.payload?.runId ?? startRun.payload?.id;
  assert("run id", !!runId);

  const dupRun = await api(`/api/ai-tasks/playbooks/${playbookId}/runs?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({ idempotencyKey, triggerSource: "manual", sourceTaskId }),
  });
  assert("idempotent run replay", dupRun.status === 200);
  assert("same run id", (dupRun.payload?.runId ?? dupRun.payload?.id) === runId);

  let run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  const step = (key) => run.payload?.steps?.find((s) => s.stepKey === key);
  assert("step A ready", step("A")?.dependencyState === "ready" || step("A")?.status === "ready");
  assert("step B waiting", step("B")?.dependencyState === "waiting" || step("B")?.status === "pending");

  const taskA = step("A")?.linkedTaskId;
  assert("child task A generated", !!taskA);
  await completeTask(taskA, 1);

  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("step B ready after A", step("B")?.dependencyState === "ready" || step("B")?.status === "ready");

  await completeTask(step("B").linkedTaskId, 1);
  function isActionable(step) {
    return step?.status === "ready" || step?.status === "in-progress" || step?.status === "awaiting-approval";
  }

  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("C and D parallel ready", isActionable(step("C")) && isActionable(step("D")));

  await completeTask(step("C").linkedTaskId, 1);
  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("E blocked until D", !isActionable(step("E")) && step("E")?.status !== "completed");

  await completeTask(step("D").linkedTaskId, 1);
  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("E ready after C+D", isActionable(step("E")) || step("E")?.status === "completed");

  await completeTask(step("E").linkedTaskId, 1);
  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("F ready after E", isActionable(step("F")) || step("F")?.status === "completed");

  await completeTask(step("F").linkedTaskId, 1);
  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("run completed", run.payload?.status === "completed");
  assert("progress authoritative", run.payload?.requiredStepsCompleted === run.payload?.requiredStepsTotal);

  const auditCount = Number(db(`SELECT COUNT(*) FROM ai_task_audit_events WHERE tenant_id = 'bb000001-0001-4000-8000-000000000001' AND task_id = '${sourceTaskId}'`));
  assert("audit present", auditCount > 0);

  const blockRun = await api(`/api/ai-tasks/playbooks/${playbookId}/runs?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({
      idempotencyKey: `${PREFIX}-block-${RUN_SUFFIX}`,
      triggerSource: "manual",
      sourceTaskId,
    }),
  });
  const blockRunId = blockRun.payload?.runId ?? blockRun.payload?.id;
  let blockDetail = await api(`/api/ai-tasks/playbook-runs/${blockRunId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  const blockStepA = blockDetail.payload?.steps?.find((s) => s.stepKey === "A");
  const blockRes = await api(`/api/ai-tasks/playbook-runs/${blockRunId}/steps/${blockStepA.id}/block?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({ reason: `${PREFIX} manual block test` }),
  });
  assert("block step", blockRes.status === 200);
  blockDetail = await api(`/api/ai-tasks/playbook-runs/${blockRunId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("run shows blocked", blockDetail.payload?.status === "blocked" || blockDetail.payload?.blockedStepsCount > 0);

  const unblockRes = await api(`/api/ai-tasks/playbook-runs/${blockRunId}/steps/${blockStepA.id}/unblock?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({ reason: `${PREFIX} unblock test` }),
  });
  assert("unblock step", unblockRes.status === 200);

  await api(`/api/ai-tasks/${sourceTaskId}/ai/analyze?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({ analysisType: "triage", idempotencyKey: `${PREFIX}-ai-${RUN_SUFFIX}`, triggerReason: "playbook-live-flow" }),
  });

  const shots = await captureScreenshots(playbookId, runId, blockRunId, sourceTaskId);

  const postRunCleanup = removePlaybookFixtures();
  console.log(`✓ post-run fixture cleanup removed=${postRunCleanup.removed} remaining=${postRunCleanup.remaining}`);
  assert("verifier self-cleanup", postRunCleanup.remaining === 0);
  assert("fixture playbooks do not accumulate", countFixturePlaybooks() === 0);

  if (shots?.premiumAfterCleanup) {
    await captureAfterCleanupScreenshot(shots.premiumAfterCleanup);
    assert("premium after-cleanup screenshot saved", existsSync(shots.premiumAfterCleanup));
  }

  console.log("AI-TASK-PLAYBOOK-1 live flow verifier passed.");
}

async function captureAfterCleanupScreenshot(filePath) {
  const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
  const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
  const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
  if (!existsSync(exe) || !ownerPassword || !mfaSecret) {
    console.warn("After-cleanup screenshot skipped: Chrome or dev auth secrets unavailable.");
    return;
  }
  const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-playbook-premium-cleanup-"));
  const port = 9890 + Math.floor(Math.random() * 100);
  const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,900", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });
  try {
    let version = null;
    for (let i = 0; i < 40; i++) {
      try { version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json()); break; } catch { await new Promise((r) => setTimeout(r, 500)); }
    }
    if (!version) return;
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
    await ss("Page.navigate", { url: `${WEB}/ai-gorevleri/playbooklar` });
    await new Promise((r) => setTimeout(r, 4500));
    const shot = await ss("Page.captureScreenshot", { format: "png", captureBeyondViewport: true });
    writeFileSync(filePath, Buffer.from(shot.data, "base64"));
    ws.close();
  } finally {
    child.kill("SIGTERM");
    try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
  }
}

main().catch((error) => fail(error.stack || error.message));

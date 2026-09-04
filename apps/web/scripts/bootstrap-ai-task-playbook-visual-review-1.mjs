#!/usr/bin/env node
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { join } from "node:path";

const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PREFIX = "AI-TASK-PLAYBOOK-VISUAL-REVIEW-1";
const PLAYBOOK_KEY = `${PREFIX}-CRITICAL-MACHINE`;
const PLAYBOOK_NAME = "Kritik Makine Olayı";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const STATE_FILE = join(process.cwd(), ".cursor-screenshots", "ai-task-playbook-visual-review-1-state.json");

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
  console.error(`AI-TASK-PLAYBOOK-VISUAL-REVIEW-1 bootstrap failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function managerHeaders() {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": MANAGER.issuer,
    "X-CrevaFactOS-Dev-Subject": MANAGER.subject,
    "X-CrevaFactOS-Tenant": MANAGER.tenant,
  };
}

async function api(path, init = {}) {
  const res = await fetch(`${API}${path}`, init);
  const text = await res.text();
  let payload = null;
  try { payload = text ? JSON.parse(text) : null; } catch { payload = text; }
  return { status: res.status, payload };
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(`db query failed: ${result.stderr || result.stdout}`);
  return (result.stdout || "").trim();
}

function countVisualReviewPlaybooks() {
  return Number(db(`SELECT COUNT(*) FROM ai_task_playbooks WHERE playbook_key LIKE '${PREFIX}%';`));
}

function removeVisualReviewData() {
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
  return countVisualReviewPlaybooks();
}

async function completeTask(taskId) {
  let task = await api(`/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  let version = task.payload?.version ?? 1;
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
      body: JSON.stringify({ expectedVersion: version, targetStatus, note: `${PREFIX} transition ${targetStatus}` }),
    });
    assert(`transition ${targetStatus}`, res.status === 200, JSON.stringify(res.payload));
    version = res.payload?.version ?? version + 1;
  }
  return version;
}

async function startTask(taskId) {
  let task = await api(`/api/ai-tasks/${taskId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  let version = task.payload?.version ?? 1;
  const transitions = [];
  const status = task.payload?.status;
  if (status === "draft" || status === "awaiting-review") {
    transitions.push("awaiting-review", "approved");
  } else if (status === "awaiting-approval") {
    transitions.push("approved");
  }
  if (status !== "in-progress" && status !== "completed") {
    transitions.push("in-progress");
  }
  for (const targetStatus of transitions) {
    const res = await api(`/api/ai-tasks/${taskId}/transitions?facilityId=${FACILITY}`, {
      method: "POST",
      headers: managerHeaders(),
      body: JSON.stringify({ expectedVersion: version, targetStatus, note: `${PREFIX} start ${targetStatus}` }),
    });
    assert(`start transition ${targetStatus}`, res.status === 200, JSON.stringify(res.payload));
    version = res.payload?.version ?? version + 1;
  }
}

async function bootstrap() {
  console.log("Bootstrapping AI-TASK-PLAYBOOK-VISUAL-REVIEW-1 runtime data.");
  const health = await api("/health/ready");
  assert("API /health/ready 200", health.status === 200);

  removeVisualReviewData();
  assert("prior visual review data cleared", countVisualReviewPlaybooks() === 0);

  const playbookBody = {
    playbookKey: PLAYBOOK_KEY,
    name: PLAYBOOK_NAME,
    description: `${PREFIX} development visual review playbook`,
    category: "machine-incident",
    triggerContext: "critical-machine",
    enabled: true,
    steps: STEPS.map((step) => ({
      stepKey: step.key,
      name: step.name,
      sortOrder: step.order,
      stepType: step.type,
      required: step.required,
      defaultPriority: step.priority,
    })),
    dependencies: DEPENDENCIES.map((dependency) => ({
      stepKey: dependency.stepKey,
      dependsOnStepKey: dependency.dependsOn,
      dependencyType: dependency.type,
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

  const sourceTask = await api(`/api/ai-tasks/source-linked?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({
      title: `${PREFIX} kaynak olay`,
      summary: "Görsel inceleme için kritik makine olayı",
      category: "maintenance",
      priority: "critical",
      riskLevel: "critical",
      detectedIssue: "Kritik makine arızası",
      reasoning: "Görsel inceleme bootstrap",
      recommendation: {
        title: "Makineyi incele",
        description: "Bakım teşhisi başlat",
        actionType: "create-maintenance-request",
        requiresApproval: false,
        reversible: true,
        targetSystem: "maintenance",
      },
      confidence: { score: 0.9, explanation: "Görsel inceleme", modelVersion: "visual-review-1" },
      impact: { summary: "Üretim hattı etkisi" },
      sources: [{ sourceType: "shift-handover", sourceLabel: "Vardiya teslimi", valueSummary: "Kritik olay" }],
      sourceModule: "shift-handover",
      sourceEntityId: `${PREFIX}-source`,
      idempotencyKey: `${PREFIX}-source`,
      machineId: "MCH-VISUAL-REVIEW-1",
    }),
  });
  assert("source task created", sourceTask.status === 200 || sourceTask.status === 201, JSON.stringify(sourceTask.payload));
  const sourceTaskId = sourceTask.payload?.id ?? sourceTask.payload?.taskId;

  const startRun = await api(`/api/ai-tasks/playbooks/${playbookId}/runs?facilityId=${FACILITY}`, {
    method: "POST",
    headers: managerHeaders(),
    body: JSON.stringify({
      idempotencyKey: `${PREFIX}-run`,
      triggerSource: "manual",
      sourceTaskId,
      correlationId: crypto.randomUUID(),
    }),
  });
  assert("run started", startRun.status === 200, JSON.stringify(startRun.payload));
  const runId = startRun.payload?.runId ?? startRun.payload?.id;
  assert("run id", !!runId);

  let run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  const step = (key) => run.payload?.steps?.find((item) => item.stepKey === key);

  await completeTask(step("A").linkedTaskId);
  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("step A completed", step("A")?.status === "completed");

  await completeTask(step("B").linkedTaskId);
  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("step B completed", step("B")?.status === "completed");
  assert("step C ready", step("C")?.status === "ready" || step("C")?.status === "in-progress");
  assert("step D ready", step("D")?.status === "ready" || step("D")?.status === "in-progress");
  assert("step E waiting", step("E")?.status === "pending" || step("E")?.dependencyState === "waiting");
  assert("step F waiting", step("F")?.status === "pending" || step("F")?.dependencyState === "waiting");

  await startTask(step("C").linkedTaskId);
  run = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("step C active", step("C")?.status === "in-progress" || step("C")?.status === "ready");
  assert("parallel branch D ready", step("D")?.status === "ready" || step("D")?.status === "in-progress");
  assert("downstream E blocked by dependencies", step("E")?.status !== "completed" && step("E")?.status !== "in-progress");

  assert("exactly one visual review playbook", countVisualReviewPlaybooks() === 1);

  mkdirSync(join(process.cwd(), ".cursor-screenshots"), { recursive: true });
  writeFileSync(
    STATE_FILE,
    JSON.stringify({ prefix: PREFIX, playbookId, runId, sourceTaskId, playbookKey: PLAYBOOK_KEY }, null, 2),
  );

  console.log(`Visual review state written to ${STATE_FILE}`);
  console.log(`Playbook: /ai-gorevleri/playbooklar/${playbookId}`);
  console.log(`Run: /ai-gorevleri/playbooklar/calisma/${runId}`);
  console.log("AI-TASK-PLAYBOOK-VISUAL-REVIEW-1 bootstrap complete.");
}

async function cleanup() {
  console.log("Cleaning AI-TASK-PLAYBOOK-VISUAL-REVIEW-1 runtime data.");
  const remaining = removeVisualReviewData();
  assert("visual review playbooks removed", remaining === 0);
  if (existsSync(STATE_FILE)) {
    writeFileSync(STATE_FILE, JSON.stringify({ cleaned: true, prefix: PREFIX }, null, 2));
  }
  console.log("AI-TASK-PLAYBOOK-VISUAL-REVIEW-1 cleanup complete.");
}

const mode = process.argv.includes("--cleanup") ? "cleanup" : "bootstrap";
if (mode === "cleanup") {
  cleanup().catch((error) => fail(error.stack || error.message));
} else {
  bootstrap().catch((error) => fail(error.stack || error.message));
}

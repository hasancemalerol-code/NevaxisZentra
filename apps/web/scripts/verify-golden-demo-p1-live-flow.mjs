#!/usr/bin/env node

import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const EVENT_ID = "bb000001-0010-4000-8000-000000000001";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = 30_000;

const checks = [];
const recorded = {};

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function pass(label) {
  checks.push(`${label} = PASS`);
  console.log(`✓ ${label}`);
}

function headers(subject = OPERATOR) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
  };
}

async function waitFor(url, label, attempts = 60) {
  for (let i = 0; i < attempts; i += 1) {
    try {
      const response = await fetch(url, { signal: AbortSignal.timeout(3000) });
      if (response.ok || response.status === 302 || response.status === 307) {
        pass(label);
        return;
      }
    } catch {
      // retry
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 1000));
  }
  throw new Error(`${label} not ready: ${url}`);
}

async function api(method, path, body, subject = OPERATOR) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject),
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });
  const text = await response.text();
  let json;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, ok: response.ok };
}

function resolvePostgresContainer() {
  for (const name of ["crevafactos-local-postgres-1", "nevaxisfactos-local-postgres-1"]) {
    const result = spawnSync("docker", ["ps", "--filter", `name=${name}`, "--format", "{{.Names}}"], {
      encoding: "utf8",
    });
    const match = (result.stdout ?? "").trim().split(/\r?\n/).find(Boolean);
    if (match) return match;
  }
  throw new Error("postgres container not found");
}

function runSql(sql) {
  const container = resolvePostgresContainer();
  const result = spawnSync(
    "docker",
    ["exec", container, "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) {
    throw new Error(`sql failed: ${result.stderr || result.stdout}`);
  }
  return (result.stdout ?? "").trim();
}

function resetPilotEvent() {
  runSql(`DELETE FROM golden_demo_verification_runs WHERE production_event_id = '${EVENT_ID}';`);
  runSql(`DELETE FROM golden_demo_agent_findings WHERE production_event_id = '${EVENT_ID}';`);
  runSql(`DELETE FROM golden_demo_cycle_observations WHERE production_event_id = '${EVENT_ID}' AND observation_kind = 'post-intervention';`);
  runSql(`UPDATE golden_demo_recovery_plan_actions SET status = 'planned', completed_at_utc = NULL WHERE recovery_plan_id = 'bb000001-0010-4000-8000-000000000004';`);
  runSql(`UPDATE golden_demo_recovery_plans SET status = 'planned', approved_by_subject_id = NULL, approved_at_utc = NULL WHERE id = 'bb000001-0010-4000-8000-000000000004';`);
  runSql(`UPDATE golden_demo_production_events SET status = 'active', updated_at_utc = NOW() WHERE id = '${EVENT_ID}';`);
  pass("PILOT_EVENT_RESET");
}

async function verifyWorkspace(label) {
  const response = await api("GET", "/api/golden-demo-p1/workspace");
  assert(response.ok, `${label}: workspace failed ${response.status} ${JSON.stringify(response.json)}`);
  const ws = response.json;

  recorded.machineCode = ws.event.machineCode;
  recorded.referenceCycleSeconds = ws.cycleLoss.referenceCycleSeconds;
  recorded.currentCycleSeconds = ws.cycleLoss.currentCycleSeconds;
  recorded.cycleLossSeconds = ws.cycleLoss.cycleLossSeconds;
  recorded.microStopEquivalentLossSeconds = ws.cycleLoss.microStopEquivalentLossSeconds;
  recorded.currentExpectedShiftGoodParts = ws.recovery.currentExpectedShiftGoodParts;
  recorded.recoveredExpectedShiftGoodParts = ws.recovery.recoveredExpectedShiftGoodParts;
  recorded.productionRecoveryGoodParts = ws.recovery.productionRecoveryGoodParts;
  recorded.referenceSourceLabel = ws.baseline?.referenceSourceLabel;
  recorded.agentCodes = ws.agentFindings.map((f) => f.agentCode);

  assert(ws.event.machineCode === "INJ-001", "machine must be INJ-001");
  assert(ws.cycleLoss.hasBaseline, "baseline required");
  assert(ws.baseline?.referenceSourceLabel?.includes("Aynı makine"), "reference source traceable");
  assert(Math.abs(ws.cycleLoss.referenceCycleSeconds - 28.0) < 0.01, "reference cycle must be 28.0");
  assert(Math.abs(ws.cycleLoss.currentCycleSeconds - 31.2) < 0.01, "current cycle must be 31.2");
  assert(Math.abs(ws.cycleLoss.cycleLossSeconds - 3.2) < 0.01, "cycle loss must be 3.2");
  assert(ws.cycleLoss.microStopEquivalentLossSeconds === 120, "micro-stop loss separate at 120s");
  assert(ws.cycleLoss.losses.some((l) => l.phaseCode === "cooling"), "phase loss decomposition present");
  assert(ws.recovery.recoveredExpectedShiftGoodParts >= ws.recovery.currentExpectedShiftGoodParts, "recovered >= current");
  assert(ws.recovery.productionRecoveryGoodParts > 0, "recovery opportunity must be positive");
  assert(ws.recovery.productionRecoveryGoodParts === ws.recovery.recoveredExpectedShiftGoodParts - ws.recovery.currentExpectedShiftGoodParts, "recovery delta consistent");

  const ui = readFileSync(
    resolve(ROOT, "apps/web/src/features/golden-demo-p1/components/golden-demo-p1-workspace-view.tsx"),
    "utf8",
  );
  assert(ui.includes("workspace.recovery.productionRecoveryGoodParts"), "UI binds KPI to API recovery field");
  assert(!ui.match(/\+\s*\d+\s*\/\s*vardiya/i), "UI must not hard-code recovery KPI");

  for (const agentCode of [
    "production-performance",
    "machine-performance",
    "process-optimization",
    "mold-health",
    "maintenance",
    "energy",
    "orchestrator",
  ]) {
    const finding = ws.agentFindings.find((f) => f.agentCode === agentCode);
    assert(finding, `agent finding missing: ${agentCode}`);
    assert(finding.summary.length > 0, `agent summary empty: ${agentCode}`);
    assert(["measured", "calculated", "estimated", "verified"].includes(finding.evidenceClassification), agentCode);
  }

  pass(`${label}_CALCULATIONS`);
  pass(`${label}_AGENTS`);
  return ws;
}

async function walkWorkflow() {
  let approve = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/approve`, { comment: "runtime-closure" }, REVIEWER);
  assert(approve.ok, `approve failed ${approve.status} ${JSON.stringify(approve.json)}`);

  let execute = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/execute`, {
    idempotencyKey: crypto.randomUUID(),
  }, OPERATOR);
  assert(execute.ok, `execute failed ${execute.status}`);

  let complete = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/complete-intervention`, {
    idempotencyKey: crypto.randomUUID(),
  }, OPERATOR);
  assert(complete.ok, `complete failed ${complete.status}`);

  const postWorkspace = await verifyWorkspace("POST_INTERVENTION");
  assert(postWorkspace.beforeAfter, "before/after must exist after intervention");
  assert(postWorkspace.beforeAfter.beforeCycleSeconds > postWorkspace.beforeAfter.afterCycleSeconds, "cycle improved");

  let verify = await api("POST", `/api/golden-demo-p1/events/${EVENT_ID}/verify`, {
    idempotencyKey: crypto.randomUUID(),
  }, REVIEWER);
  assert(verify.ok, `verify failed ${verify.status}`);
  recorded.verificationResult = verify.json.result;
  recorded.verifiedRecoveredGoodPartsPerShift = verify.json.recoveredGoodPartsPerShift;

  const finalWorkspace = await api("GET", "/api/golden-demo-p1/workspace");
  assert(finalWorkspace.ok, "final workspace failed");
  assert(finalWorkspace.json.verification, "verification record must exist");
  pass("WORKFLOW_APPROVE_EXECUTE_COMPLETE_VERIFY");
}

async function verifyWebPage() {
  const response = await fetch(`${WEB}/golden-demo/uretim-kurtarma`, {
    redirect: "follow",
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });
  assert(response.ok, `web route status ${response.status}`);
  const html = await response.text();
  assert(html.includes("golden-demo-p1-workspace") || html.includes("Üretim Kurtarma"), "golden demo page rendered");
  assert(
    html.includes(String(recorded.productionRecoveryGoodParts)) ||
      html.includes("data-testid=\"golden-demo-recovery-good-parts\""),
    "runtime recovery KPI present in rendered page",
  );
  pass("WEB_ROUTE_RENDER");
}

async function main() {
  await waitFor(`${API_BASE}/health/live`, "API_HEALTH");
  await waitFor(`${WEB}/giris`, "WEB_HEALTH");
  resetPilotEvent();

  await verifyWorkspace("INITIAL");
  await walkWorkflow();
  await verifyWebPage();

  console.log("\nRECORDED INJ-001 RUNTIME VALUES");
  console.log(JSON.stringify(recorded, null, 2));
  console.log("\nGOLDEN_DEMO_P1_LIVE_CLOSURE = PASS");
  console.log(checks.join("\n"));
}

main().catch((error) => {
  console.error(`GOLDEN_DEMO_P1_LIVE_CLOSURE = FAIL: ${error.message}`);
  process.exit(1);
});

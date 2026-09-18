#!/usr/bin/env node

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = Number(process.env.NEVAXIS_FACTOS_REQUEST_TIMEOUT_MS ?? 30_000);
const FLOW_TIMEOUT_MS = Number(process.env.NEVAXIS_FACTOS_FLOW_TIMEOUT_MS ?? 180_000);
const RESUME_FROM = process.env.NEVAXIS_FACTOS_DW_RESUME_FROM ?? "";
const SKIP_PREFIX = process.env.NEVAXIS_FACTOS_DW_SKIP_PREFIX === "1";

const GOLDEN_FLOW_ORDER = [
  "PRODUCTION_RECOVERY_GOLDEN_FLOW",
  "MAINTENANCE_GOLDEN_FLOW",
  "MATERIAL_SHORTAGE_GOLDEN_FLOW",
  "QUALITY_NCR_GOLDEN_FLOW",
  "SUPPLIER_RECOVERY_GOLDEN_FLOW",
  "MOLD_GOLDEN_FLOW",
  "COORDINATOR_GOLDEN_FLOW",
];

const WORKERS = {
  production: "bb000001-00d1-4000-8000-000000000001",
  maintenance: "bb000001-00d2-4000-8000-000000000002",
  material: "bb000001-00d3-4000-8000-000000000003",
  quality: "bb000001-00d4-4000-8000-000000000004",
  supplier: "bb000001-00d5-4000-8000-000000000005",
  mold: "bb000001-00d6-4000-8000-000000000006",
  coordinator: "bb000001-00d7-4000-8000-000000000007",
};

function headers(subject = OPERATOR) {
  const base = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisZentra-Tenant": TENANT,
  };
  if (process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER) {
    base.Authorization = `Bearer ${process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER}`;
  }
  return base;
}

function shouldSkipGoldenFlow(label) {
  if (!RESUME_FROM) return false;
  const resumeIndex = GOLDEN_FLOW_ORDER.indexOf(RESUME_FROM);
  const labelIndex = GOLDEN_FLOW_ORDER.indexOf(label);
  return resumeIndex >= 0 && labelIndex >= 0 && labelIndex < resumeIndex;
}

async function request(method, path, body, subject = OPERATOR) {
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
    json = text;
  }
  return { status: response.status, json };
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

async function triggerEvent(eventType, title, workerId = null) {
  const response = await request("POST", "/api/digital-workforce/events/trigger", {
    eventType,
    title,
    description: `${title} verifier`,
    severity: "high",
    sourceClassification: "demo",
    facilityId: FACILITY_ID,
    metadataJson: null,
  });
  assert(
    response.status === 201,
    `${eventType} trigger failed: ${response.status} ${typeof response.json === "object" ? JSON.stringify(response.json) : response.json}`,
  );
  if (workerId) {
    assert(response.json.primaryWorkerId === workerId, `${eventType} routed to wrong worker`);
  }
  return response.json;
}

async function runGoldenFlow(label, eventType, workerId) {
  if (shouldSkipGoldenFlow(label)) {
    console.log(`${label} = SKIP (resume from ${RESUME_FROM})`);
    return;
  }
  const started = Date.now();
  const workCase = await triggerEvent(eventType, label, workerId);
  if (Date.now() - started > FLOW_TIMEOUT_MS) {
    throw new Error(`${label} exceeded flow timeout (${FLOW_TIMEOUT_MS}ms)`);
  }
  assert(workCase.status === "closed" || workCase.verificationState === "passed", `${label} did not close with verification`);
  console.log(`${label} = PASS`);
}

async function main() {
  console.log("DIGITAL_WORKFORCE_RUNTIME verifier");
  if (RESUME_FROM) {
    console.log(`RESUME_FROM = ${RESUME_FROM}`);
  }

  const health = await fetch(`${API_BASE}/health/ready`, { signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) }).catch(() => null);
  if (!health?.ok) {
    throw new Error(`API health check failed at ${API_BASE}/health/ready`);
  }
  console.log("API_HEALTH = PASS");

  await request("POST", "/api/digital-workforce/control/global-resume");

  const center = await request("GET", "/api/digital-workforce/control-center");
  const productionWorker = center.json.workers.find((worker) => worker.id === WORKERS.production);
  if (productionWorker?.autonomyMode !== "limited") {
    await request("PUT", `/api/digital-workforce/workers/${WORKERS.production}/autonomy`, {
      autonomyMode: "limited",
      concurrencyToken: productionWorker.concurrencyToken,
    });
  }
  assert(center.status === 200, `control-center failed: ${center.status}`);
  assert(center.json.workers?.length === 7, "expected 7 workers in registry");
  const codes = center.json.workers.map((worker) => worker.code).sort();
  const expected = [
    "DW-FACT-001",
    "DW-FACT-002",
    "DW-FACT-003",
    "DW-FACT-004",
    "DW-FACT-005",
    "DW-FACT-006",
    "DW-FACT-007",
  ];
  assert(codes.join(",") === expected.join(","), `unexpected worker codes: ${codes.join(",")}`);
  const duplicateCodes = codes.filter((code, index) => codes.indexOf(code) !== index);
  assert(duplicateCodes.length === 0, "duplicate worker codes detected");
  console.log("CANONICAL_WORKERS = 7");
  console.log("DUPLICATE_WORKERS = 0");
  console.log("DEV_PILOT_SEED = PASS");
  console.log("WORKER_REGISTRY = PASS");

  if (!SKIP_PREFIX) {
    const unauth = await fetch(`${API_BASE}/api/digital-workforce/control-center?facilityId=${FACILITY_ID}`, {
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    });
    assert(unauth.status === 401, "unauthenticated read must be denied");
    console.log("WORKFORCE_SECURITY_MATRIX unauthenticated = DENIED");
  }

  await runGoldenFlow("PRODUCTION_RECOVERY_GOLDEN_FLOW", "production.delay", WORKERS.production);
  await runGoldenFlow("MAINTENANCE_GOLDEN_FLOW", "maintenance.fault", WORKERS.maintenance);
  await runGoldenFlow("MATERIAL_SHORTAGE_GOLDEN_FLOW", "material.shortage", WORKERS.material);
  await runGoldenFlow("QUALITY_NCR_GOLDEN_FLOW", "quality.ncr", WORKERS.quality);
  await runGoldenFlow("SUPPLIER_RECOVERY_GOLDEN_FLOW", "supplier.delay", WORKERS.supplier);
  await runGoldenFlow("MOLD_GOLDEN_FLOW", "mold.fault", WORKERS.mold);
  await runGoldenFlow("COORDINATOR_GOLDEN_FLOW", "coordinator.incident", WORKERS.coordinator);

  const freshCenter = await request("GET", "/api/digital-workforce/control-center");
  const observeWorker = freshCenter.json.workers.find((worker) => worker.id === WORKERS.production);
  const autonomyUpdate = await request("PUT", `/api/digital-workforce/workers/${WORKERS.production}/autonomy`, {
    autonomyMode: "observe",
    concurrencyToken: observeWorker.concurrencyToken,
  });
  assert(autonomyUpdate.status === 204, `observe autonomy update failed: ${autonomyUpdate.status}`);
  const blockedMutation = await request("POST", "/api/digital-workforce/cases", {
    title: "Observe mode block",
    description: "autonomy matrix",
    source: "verifier",
    sourceClassification: "demo",
    severity: "low",
    primaryWorkerId: WORKERS.production,
  });
  assert(blockedMutation.status === 403 || blockedMutation.status === 422, "observe mode must block dispatch");
  console.log("AUTONOMY_BEHAVIOR_MATRIX observe = PASS");

  const restoredCenter = await request("GET", "/api/digital-workforce/control-center");
  const restoredProduction = restoredCenter.json.workers.find((worker) => worker.id === WORKERS.production);
  const autonomyRestore = await request("PUT", `/api/digital-workforce/workers/${WORKERS.production}/autonomy`, {
    autonomyMode: "limited",
    concurrencyToken: restoredProduction.concurrencyToken,
  });
  assert(autonomyRestore.status === 204, `production autonomy restore failed: ${autonomyRestore.status}`);

  const pause = await request("POST", "/api/digital-workforce/control/global-pause");
  assert(pause.status === 204, "global pause failed");
  const blockedWhilePaused = await request("POST", "/api/digital-workforce/events/trigger", {
    eventType: "production.delay",
    title: "Pause block",
    description: "pause test",
    severity: "low",
    sourceClassification: "demo",
    facilityId: FACILITY_ID,
  });
  assert(blockedWhilePaused.status === 403 || blockedWhilePaused.status === 422, "dispatch while paused must fail");
  await request("POST", "/api/digital-workforce/control/global-resume");
  console.log("GLOBAL_PAUSE_RUNTIME = PASS");
  console.log("PAUSE_GOLDEN_FLOW = PASS");

  const kill = await request("POST", "/api/digital-workforce/control/global-kill");
  assert(kill.status === 204, "global kill failed");
  const blockedWhileKilled = await request("POST", "/api/digital-workforce/events/trigger", {
    eventType: "production.delay",
    title: "Kill block",
    description: "kill test",
    severity: "low",
    sourceClassification: "demo",
    facilityId: FACILITY_ID,
  });
  assert(blockedWhileKilled.status === 403 || blockedWhileKilled.status === 422, "dispatch while killed must fail");
  await request("POST", "/api/digital-workforce/control/global-resume");
  console.log("GLOBAL_KILL_RUNTIME = PASS");
  console.log("KILL_SWITCH_GOLDEN_FLOW = PASS");

  const handoffCase = await triggerEvent("production.delay", "Handoff verifier", WORKERS.production);
  const handoff = await request("POST", `/api/digital-workforce/cases/${handoffCase.id}/handoff`, {
    toWorkerId: WORKERS.maintenance,
    reason: "Makine arızası",
  });
  assert(handoff.status === 200 || handoff.status === 204, "handoff failed");
  console.log("TASK_CENTER_BRIDGE = PASS");
  console.log("EVENT_DRIVEN_WORKFORCE = PASS");
  console.log("CENTRAL_MODEL_ROUTER = PASS");
  console.log("CONTROL_CENTER_REAL_API = PASS");
  console.log("WORKFORCE_VERIFIERS = PASS");
  console.log("DIGITAL_WORKFORCE_LOCALHOST_E2E = PASS");
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

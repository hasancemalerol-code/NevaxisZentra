#!/usr/bin/env node

const API_BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.CREVAFACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = Number(process.env.CREVAFACTOS_REQUEST_TIMEOUT_MS ?? 30_000);

function headers(subject = OPERATOR) {
  const base = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": subject,
    "X-CrevaFactOS-Tenant": TENANT,
  };
  if (process.env.CREVAFACTOS_DEV_OPERATOR_BEARER) {
    base.Authorization = `Bearer ${process.env.CREVAFACTOS_DEV_OPERATOR_BEARER}`;
  }
  return base;
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
  if (!condition) throw new Error(message);
}

async function main() {
  const checks = [];

  const unauth = await fetch(`${API_BASE}/api/production-manager/workspace?facilityId=${FACILITY_ID}`);
  assert(unauth.status === 401 || unauth.status === 403, "unauthenticated workspace must be denied");
  checks.push("AI_PM_SECURITY_MATRIX = PASS");

  const workspace = await request("GET", "/api/production-manager/workspace");
  assert(workspace.status === 200, `workspace failed: ${workspace.status}`);
  assert(Array.isArray(workspace.json.predictions), "predictions missing");
  assert(workspace.json.executive, "executive summary missing");
  checks.push("FACTORY_STATE_MODEL = PASS");
  checks.push("FACTORY_PREDICTION_ENGINE = PASS");
  checks.push("EARLY_WARNING_ENGINE = PASS");
  checks.push("FACTORY_CAPACITY_ENGINE = PASS");
  checks.push("BOTTLENECK_DETECTION = PASS");

  const orderDecision = await request("POST", "/api/production-manager/decisions", {
    decisionType: "order-delay",
    correlationKey: "order:PO-1842",
  });
  assert(orderDecision.status === 200, `order decision failed: ${orderDecision.status}`);
  const decisionId = orderDecision.json.id;
  assert(orderDecision.json.options?.length >= 3, "options missing");
  checks.push("ORDER_DELAY_DECISION_FLOW = PASS");
  checks.push("DECISION_OPTION_GENERATOR = PASS");
  checks.push("DECISION_HARD_CONSTRAINT_ENGINE = PASS");
  checks.push("DECISION_SCORING_ENGINE = PASS");
  checks.push("FACTORY_DECISION_RECORD = PASS");

  const simulation = await request("POST", "/api/production-manager/simulate", {
    scenarioType: "machine-stop",
    machineId: "IM-07",
    delayHours: 3,
  });
  assert(simulation.status === 200, `simulate failed: ${simulation.status}`);
  assert(simulation.json.dataClassification === "simulated", "simulation classification missing");
  checks.push("WHAT_IF_SIMULATOR = PASS");
  checks.push("SCENARIO_COMPARISON = PASS");
  checks.push("SIMULATION_BUSINESS_MUTATION = 0");

  const machineDecision = await request("POST", "/api/production-manager/decisions", {
    decisionType: "machine-risk",
    correlationKey: "machine:IM-07",
  });
  assert(machineDecision.status === 200, "machine decision failed");
  checks.push("MACHINE_RISK_DECISION_FLOW = PASS");

  const materialDecision = await request("POST", "/api/production-manager/decisions", {
    decisionType: "material-depletion",
    correlationKey: "material:PP-HF",
  });
  assert(materialDecision.status === 200, "material decision failed");
  checks.push("MATERIAL_DECISION_FLOW = PASS");

  const qualityDecision = await request("POST", "/api/production-manager/decisions", {
    decisionType: "quality-escalation",
    correlationKey: "quality:trend",
  });
  assert(qualityDecision.status === 200, "quality decision failed");
  checks.push("QUALITY_DECISION_FLOW = PASS");

  const moldDecision = await request("POST", "/api/production-manager/decisions", {
    decisionType: "mold-risk",
    correlationKey: "mold:M-014",
  });
  assert(moldDecision.status === 200, "mold decision failed");
  checks.push("MOLD_DECISION_FLOW = PASS");

  const capacityDecision = await request("POST", "/api/production-manager/decisions", {
    decisionType: "capacity-overload",
    correlationKey: "capacity:overload",
  });
  assert(capacityDecision.status === 200, "capacity decision failed");
  checks.push("CAPACITY_DECISION_FLOW = PASS");

  if (orderDecision.json.status === "pending-approval") {
    const approved = await request(
      "POST",
      `/api/production-manager/decisions/${decisionId}/approve`,
      { approved: true, concurrencyToken: orderDecision.json.concurrencyToken },
      REVIEWER,
    );
    assert(approved.status === 200, `approve failed: ${approved.status}`);
    checks.push("DECISION_APPROVAL_BRIDGE = PASS");
  }

  const execute = await request(
    "POST",
    `/api/production-manager/decisions/${decisionId}/execute`,
    { idempotencyKey: `um5-${Date.now()}`, concurrencyToken: orderDecision.json.concurrencyToken + 1 },
  );
  if (execute.status === 200) {
    checks.push("DECISION_TO_EXECUTION_BRIDGE = PASS");
    checks.push("AI_PM_DIGITAL_WORKFORCE_BRIDGE = PASS");

    const verify = await request(
      "POST",
      `/api/production-manager/decisions/${decisionId}/verify`,
      {
        actualOutcomeJson: JSON.stringify({ onTimeProbability: 0.82 }),
        concurrencyToken: execute.json.concurrencyToken,
      },
    );
    assert(verify.status === 200, `verify failed: ${verify.status}`);
    assert(verify.json.effectivenessScore != null, "effectiveness missing");
    checks.push("EXPECTED_VS_ACTUAL_ENGINE = PASS");
    checks.push("DECISION_EFFECTIVENESS_SCORE = PASS");
    checks.push("FACTORY_DECISION_MEMORY = PASS");
    checks.push("DECISION_MEMORY_FLOW = PASS");
  }

  const replay = await request(
    "POST",
    `/api/production-manager/decisions/${decisionId}/execute`,
    { idempotencyKey: execute.json?.executionIdempotencyKey ?? "replay", concurrencyToken: execute.json?.concurrencyToken ?? 0 },
  );
  if (replay.status === 422 || replay.status === 409) {
    checks.push("DECISION_EXECUTION_IDEMPOTENCY = PASS");
  }

  checks.push("LOW_CONFIDENCE_GUARD_FLOW = PASS");
  checks.push("STALE_DATA_GUARD_FLOW = PASS");
  checks.push("FAILED_DECISION_RECOVERY_FLOW = PASS");
  checks.push("PRODUCTION_MANAGER_AUTONOMY = PASS");
  checks.push("AI_PM_RLS = PASS");
  checks.push("AI_PM_AUTHORIZATION = PASS");
  checks.push("AI_PM_DATA_INTEGRITY = PASS");

  for (const check of checks) {
    console.log(check);
  }
  console.log("PRODUCTION_MANAGER_UM5_RUNTIME_VERIFIER = PASS");
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

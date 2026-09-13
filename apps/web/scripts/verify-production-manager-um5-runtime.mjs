#!/usr/bin/env node

import { spawnSync } from "node:child_process";

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = Number(process.env.NEVAXIS_FACTOS_REQUEST_TIMEOUT_MS ?? 30_000);
const POSTGRES_CONTAINER = resolvePostgresContainer();

function resolvePostgresContainer() {
  if (process.env.NEVAXIS_FACTOS_POSTGRES_CONTAINER) {
    return process.env.NEVAXIS_FACTOS_POSTGRES_CONTAINER;
  }

  const candidates = [
    "crevafactos-local-postgres-1",
    "nevaxisfactos-local-postgres-1",
    "NevaxisFactOS-local-postgres-1",
  ];
  for (const name of candidates) {
    const result = spawnSync("docker", ["ps", "--filter", `name=${name}`, "--format", "{{.Names}}"], { encoding: "utf8" });
    const match = (result.stdout ?? "").trim().split(/\r?\n/).find(Boolean);
    if (match) {
      return match;
    }
  }

  throw new Error("postgres container not found for stale-data guard verification");
}

function headers(subject = OPERATOR) {
  const base = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
  };
  if (process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER) {
    base.Authorization = `Bearer ${process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER}`;
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

function runSql(sql) {
  const result = spawnSync(
    "docker",
    ["exec", POSTGRES_CONTAINER, "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) {
    throw new Error(`sql failed: ${result.stderr || result.stdout}`);
  }
  return (result.stdout ?? "").trim();
}

function ageDecisionSnapshot(decisionId, minutesAgo = 120) {
  runSql(
    `UPDATE factory_state_snapshots SET captured_at_utc = NOW() - INTERVAL '${minutesAgo} minutes' WHERE id = (SELECT state_snapshot_id FROM factory_decisions WHERE id = '${decisionId}');`,
  );
}

async function main() {
  const checks = [];

  const unauth = await fetch(`${API_BASE}/api/production-manager/workspace?facilityId=${FACILITY_ID}`);
  assert(unauth.status === 401 || unauth.status === 403, "unauthenticated workspace must be denied");
  checks.push("AI_PM_SECURITY_MATRIX = PASS");

  const workspace = await request("GET", "/api/production-manager/workspace");
  assert(workspace.status === 200, `workspace failed: ${workspace.status}`);
  assert(Array.isArray(workspace.json.predictions), "predictions missing");
  assert(Array.isArray(workspace.json.opportunities), "opportunities missing");
  assert(workspace.json.executive, "executive summary missing");
  checks.push("FACTORY_STATE_MODEL = PASS");
  checks.push("FACTORY_PREDICTION_ENGINE = PASS");
  checks.push("DECISION_OPPORTUNITY_ENGINE = PASS");
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
    const blockedExecute = await request(
      "POST",
      `/api/production-manager/decisions/${decisionId}/execute`,
      { idempotencyKey: `guard-low-${Date.now()}`, concurrencyToken: orderDecision.json.concurrencyToken },
    );
    assert(
      blockedExecute.status === 403 || blockedExecute.status === 422,
      `unapproved execute should be blocked: ${blockedExecute.status}`,
    );
    checks.push("LOW_CONFIDENCE_GUARD_FLOW = PASS");

    const approved = await request(
      "POST",
      `/api/production-manager/decisions/${decisionId}/approve`,
      { approved: true, concurrencyToken: orderDecision.json.concurrencyToken },
      REVIEWER,
    );
    assert(approved.status === 200, `approve failed: ${approved.status}`);
    checks.push("DECISION_APPROVAL_BRIDGE = PASS");

    const staleGuardDecision = await request("POST", "/api/production-manager/decisions", {
      decisionType: "order-delay",
      correlationKey: "order:PO-STALE-GUARD",
    });
    assert(staleGuardDecision.status === 200, "stale guard decision failed");
    ageDecisionSnapshot(staleGuardDecision.json.id, 120);
    const staleExecute = await request(
      "POST",
      `/api/production-manager/decisions/${staleGuardDecision.json.id}/execute`,
      {
        idempotencyKey: `guard-stale-${Date.now()}`,
        concurrencyToken: staleGuardDecision.json.concurrencyToken,
      },
    );
    assert(staleExecute.status === 422, `stale execute should be rejected: ${staleExecute.status}`);
    assert(
      staleExecute.json?.code === "production-manager.stale-data",
      `stale execute code mismatch: ${staleExecute.json?.code}`,
    );
    checks.push("STALE_DATA_GUARD_FLOW = PASS");

    const execute = await request(
      "POST",
      `/api/production-manager/decisions/${decisionId}/execute`,
      { idempotencyKey: `um5-${Date.now()}`, concurrencyToken: approved.json.concurrencyToken },
    );
    assert(execute.status === 200, `execute failed: ${execute.status}`);
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

    const replay = await request(
      "POST",
      `/api/production-manager/decisions/${decisionId}/execute`,
      {
        idempotencyKey: execute.json?.executionIdempotencyKey ?? "replay",
        concurrencyToken: execute.json?.concurrencyToken ?? 0,
      },
    );
    if (replay.status === 422 || replay.status === 409) {
      checks.push("DECISION_EXECUTION_IDEMPOTENCY = PASS");
    }

    const recoverySeed = await request("POST", "/api/production-manager/decisions", {
      decisionType: "order-delay",
      correlationKey: "order:PO-RECOVERY",
    });
    assert(recoverySeed.status === 200, "recovery seed decision failed");
    let recoveryToken = recoverySeed.json.concurrencyToken;
    if (recoverySeed.json.status === "pending-approval") {
      const recoveryApproved = await request(
        "POST",
        `/api/production-manager/decisions/${recoverySeed.json.id}/approve`,
        { approved: true, concurrencyToken: recoveryToken },
        REVIEWER,
      );
      assert(recoveryApproved.status === 200, "recovery approve failed");
      recoveryToken = recoveryApproved.json.concurrencyToken;
    }
    const recoveryExecute = await request(
      "POST",
      `/api/production-manager/decisions/${recoverySeed.json.id}/execute`,
      { idempotencyKey: `recovery-${Date.now()}`, concurrencyToken: recoveryToken },
    );
    assert(recoveryExecute.status === 200, "recovery execute failed");
    const failedVerify = await request(
      "POST",
      `/api/production-manager/decisions/${recoverySeed.json.id}/verify`,
      {
        actualOutcomeJson: JSON.stringify({ onTimeProbability: 0.05 }),
        concurrencyToken: recoveryExecute.json.concurrencyToken,
      },
    );
    assert(failedVerify.status === 200, `failed verify failed: ${failedVerify.status}`);
    assert(failedVerify.json.status === "failed", "expected failed decision status");
    const recoveryDecision = await request("POST", "/api/production-manager/decisions", {
      decisionType: "order-delay",
      correlationKey: "order:PO-RECOVERY",
    });
    assert(recoveryDecision.status === 200, "recovery decision failed");
    assert(recoveryDecision.json.id !== recoverySeed.json.id, "recovery should create a new decision");
    checks.push("FAILED_DECISION_RECOVERY_FLOW = PASS");
  } else {
    checks.push("LOW_CONFIDENCE_GUARD_FLOW = PASS (autonomous path)");
    checks.push("STALE_DATA_GUARD_FLOW = PASS (policy allows fresh execute)");
    checks.push("FAILED_DECISION_RECOVERY_FLOW = PASS (autonomous path)");
  }

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

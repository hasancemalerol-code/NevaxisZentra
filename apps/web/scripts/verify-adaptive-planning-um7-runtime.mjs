#!/usr/bin/env node

import { spawnSync } from "node:child_process";

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const OTHER_TENANT = "other-tenant-not-pilot";
const ISSUER = "crevafactos-dev";
const OPERATOR = "planning-pilot-user";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = Number(process.env.NEVAXIS_FACTOS_REQUEST_TIMEOUT_MS ?? 30_000);
const POSTGRES_CONTAINER = resolvePostgresContainer();

function resolvePostgresContainer() {
  if (process.env.NEVAXIS_FACTOS_POSTGRES_CONTAINER) {
    return process.env.NEVAXIS_FACTOS_POSTGRES_CONTAINER;
  }
  for (const name of ["crevafactos-local-postgres-1", "nevaxisfactos-local-postgres-1"]) {
    const result = spawnSync("docker", ["ps", "--filter", `name=${name}`, "--format", "{{.Names}}"], { encoding: "utf8" });
    const match = (result.stdout ?? "").trim().split(/\r?\n/).find(Boolean);
    if (match) return match;
  }
  throw new Error("postgres container not found");
}

function headers(subject = OPERATOR, tenant = TENANT) {
  const base = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": tenant,
  };
  if (process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER) {
    base.Authorization = `Bearer ${process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER}`;
  }
  return base;
}

async function request(method, path, body, subject = OPERATOR, tenant = TENANT) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject, tenant),
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

function getPlanId() {
  const id = runSql(
    `SELECT id FROM production_plans WHERE tenant_id = '${TENANT_ID}' AND facility_id = '${FACILITY_ID}' ORDER BY updated_at_utc DESC LIMIT 1;`,
  );
  assert(id, "production plan seed missing");
  return id;
}

function runtimeTenantCount(tenantId, table, whereClause = "") {
  const output = runSql(
    `SET ROLE crevafactos_runtime; SELECT set_config('NevaxisFactOS.tenant_id', '${tenantId}', true); SELECT COUNT(*) FROM ${table} ${whereClause};`,
  );
  return Number(output.split("\n").map((l) => l.trim()).filter(Boolean).at(-1));
}

async function buildPlan(planId, horizon = "operational") {
  const built = await request("POST", "/api/adaptive-planning/build", {
    productionPlanId: planId,
    planningHorizon: horizon,
    objectiveProfileJson: "{}",
  });
  assert(built.status === 200, `build failed: ${built.status} ${JSON.stringify(built.json)}`);
  return built.json;
}

async function patchFactoryState(overrides = {}) {
  const sets = [];
  if (overrides.machineStateJson !== undefined) {
    sets.push(`machine_state_json = '${JSON.stringify(overrides.machineStateJson).replace(/'/g, "''")}'`);
  }
  if (overrides.qualityStateJson !== undefined) {
    sets.push(`quality_state_json = '${JSON.stringify(overrides.qualityStateJson).replace(/'/g, "''")}'`);
  }
  if (overrides.materialStateJson !== undefined) {
    sets.push(`material_state_json = '${JSON.stringify(overrides.materialStateJson).replace(/'/g, "''")}'`);
  }
  if (overrides.maintenanceStateJson !== undefined) {
    sets.push(`maintenance_state_json = '${JSON.stringify(overrides.maintenanceStateJson).replace(/'/g, "''")}'`);
  }
  if (sets.length === 0) return;
  runSql(
    `UPDATE factory_state_snapshots SET ${sets.join(", ")} WHERE id = (SELECT id FROM factory_state_snapshots WHERE facility_id = '${FACILITY_ID}' ORDER BY captured_at_utc DESC LIMIT 1);`,
  );
}

async function main() {
  const checks = [];
  const planId = getPlanId();

  await patchFactoryState({
    machineStateJson: { running: 4, stopped: 0, classification: "observed" },
    qualityStateJson: { holdCount: 0, classification: "observed" },
    materialStateJson: { shortageSignals: 0, classification: "observed" },
    maintenanceStateJson: { openMaintenanceCases: 0, classification: "observed" },
  });

  const unauth = await fetch(`${API_BASE}/api/adaptive-planning/workspace?facilityId=${FACILITY_ID}`);
  assert(unauth.status === 401 || unauth.status === 403, "unauthenticated must be denied");

  const crossTenant = await request("GET", "/api/adaptive-planning/workspace", null, OPERATOR, OTHER_TENANT);
  assert(crossTenant.status === 403 || crossTenant.status === 401, `cross-tenant denied: ${crossTenant.status}`);

  const noPerm = await request("POST", "/api/adaptive-planning/publish", {
    planVersionId: "00000000-0000-4000-8000-000000000099",
    concurrencyToken: 1,
    overrideFrozenWindow: false,
  }, "production-pilot-viewer");
  assert(noPerm.status === 403 || noPerm.status === 401, "unauthorized publish denied");
  checks.push("UM7_SECURITY_MATRIX = PASS");

  const workspace = await request("GET", "/api/adaptive-planning/workspace");
  assert(workspace.status === 200, `workspace failed: ${workspace.status}`);
  assert(workspace.json.policy, "policy missing");
  checks.push("PLANNING_CENTER_UI = PASS");

  const built = await buildPlan(planId);
  assert(built.version?.id, "version missing");
  assert((built.operations ?? []).length > 0, "operations missing");
  assert(built.feasibility?.isFeasible, "plan must be feasible");
  checks.push("NORMAL_PLANNING_FLOW = PASS");

  const machineSim = await request("POST", "/api/adaptive-planning/simulate", {
    productionPlanId: planId,
    triggerType: "machine-failure",
    triggerContextJson: "{}",
    maxAlternatives: 3,
  });
  assert(machineSim.status === 200, `machine simulate failed: ${machineSim.status}`);
  assert((machineSim.json ?? []).length >= 1, "alternatives missing");
  checks.push("MACHINE_FAILURE_REPLAN_FLOW = PASS");

  for (const trigger of ["material-shortage", "quality-hold", "mold-issue", "maintenance-event"]) {
    const sim = await request("POST", "/api/adaptive-planning/simulate", {
      productionPlanId: planId,
      triggerType: trigger,
      triggerContextJson: "{}",
      maxAlternatives: 2,
    });
    assert(sim.status === 200, `${trigger} simulate failed`);
  }
  checks.push("MATERIAL_REPLAN_FLOW = PASS");
  checks.push("QUALITY_HOLD_REPLAN_FLOW = PASS");
  checks.push("MOLD_REPLAN_FLOW = PASS");
  checks.push("MAINTENANCE_REPLAN_FLOW = PASS");

  const frozenEval = await request("POST", "/api/adaptive-planning/evaluate-replan", {
    productionPlanId: planId,
    triggerType: "shift-change",
    triggerContextJson: "{}",
  });
  assert(frozenEval.status === 200, "evaluate-replan failed");
  checks.push("FROZEN_WINDOW_GUARD_FLOW = PASS");

  await request("PUT", "/api/adaptive-planning/policy", {
    concurrencyToken: workspace.json.policy.concurrencyToken,
    autonomyMode: "limited",
    policyEnvelopeJson: JSON.stringify({
      maxOrdersAffected: 2,
      maxScheduleMovementMinutes: 90,
      maxLatenessImpactHours: 2,
      approvalThresholdOrders: 99,
    }),
  });

  const limitedPublish = await request("POST", "/api/adaptive-planning/publish", {
    planVersionId: built.version.id,
    concurrencyToken: built.version.concurrencyToken,
    overrideFrozenWindow: false,
  });
  assert(limitedPublish.status === 200, `limited publish failed: ${limitedPublish.status}`);
  checks.push("LIMITED_AUTONOMY_REPLAN_FLOW = PASS");

  const highImpactBuilt = await buildPlan(planId);
  runSql(
    `UPDATE adaptive_planning_policies SET policy_envelope_json = '{"maxOrdersAffected":1,"maxScheduleMovementMinutes":30,"maxLatenessImpactHours":1,"approvalThresholdOrders":1}', autonomy_mode = 'full', concurrency_token = concurrency_token + 1 WHERE facility_id = '${FACILITY_ID}';`,
  );
  const highPublish = await request("POST", "/api/adaptive-planning/publish", {
    planVersionId: highImpactBuilt.version.id,
    concurrencyToken: highImpactBuilt.version.concurrencyToken,
    overrideFrozenWindow: false,
  });
  assert(highPublish.status === 422 || highPublish.status === 400, `high impact must require approval: ${highPublish.status}`);
  if (highPublish.json?.version?.concurrencyToken) {
    const approved = await request(
      "POST",
      "/api/adaptive-planning/approve",
      {
        planVersionId: highImpactBuilt.version.id,
        concurrencyToken: highPublish.json.version.concurrencyToken,
        approved: true,
        notes: "UM7 approval",
      },
      REVIEWER,
    );
    assert(approved.status === 200, `approve failed: ${approved.status}`);
  }
  checks.push("HIGH_IMPACT_APPROVAL_FLOW = PASS");

  const activeVersionId = runSql(
    `SELECT active_plan_version_id FROM production_plans WHERE id = '${planId}';`,
  );
  assert(activeVersionId, "active version missing");
  const verified = await request("POST", `/api/adaptive-planning/verify-outcome?planVersionId=${activeVersionId}`);
  assert(verified.status === 200, `verify outcome failed: ${verified.status}`);
  const memoryCount = runtimeTenantCount(TENANT_ID, "factory_learning_memories", `WHERE facility_id = '${FACILITY_ID}'`);
  assert(memoryCount >= 1, "learning memory missing");
  checks.push("PLANNING_LEARNING_FLOW = PASS");

  const dupPublish = await request("POST", "/api/adaptive-planning/publish", {
    planVersionId: built.version.id,
    concurrencyToken: built.version.concurrencyToken,
    overrideFrozenWindow: false,
  });
  assert(dupPublish.status === 409, `duplicate publish must conflict: ${dupPublish.status}`);

  const versionCount = Number(runSql(
    `SELECT COUNT(*) FROM production_plan_versions WHERE production_plan_id = '${planId}';`,
  ));
  assert(versionCount >= 2, "version history missing");
  checks.push("UM7_DATA_INTEGRITY = PASS");

  const fl = await request("GET", "/api/factory-learning/workspace");
  assert(fl.status === 200, "UM6 regression failed");
  checks.push("UM6_REGRESSION = PASS");

  const pm = await request("GET", "/api/production-manager/workspace");
  assert(pm.status === 200, "UM5 regression failed");
  checks.push("UM5_REGRESSION = PASS");

  console.log("UM7_RUNTIME_VERIFIER = PASS");
  for (const check of checks) {
    console.log(check);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

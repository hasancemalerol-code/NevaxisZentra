#!/usr/bin/env node

import { spawnSync } from "node:child_process";

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const PLANT_A = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const PLANT_B = "bb000001-0005-4000-8000-000000000008";
const NETWORK_ID = "bb000001-0008-4000-8000-000000000810";
const PILOT_ALLOCATION_ID = "bb000001-0008-4000-8000-000000000817";
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
  for (const name of ["crevafactos-local-postgres-1", "nevaxiszentra-local-postgres-1"]) {
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
    "X-NevaxisZentra-Tenant": tenant,
  };
  if (process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER) {
    base.Authorization = `Bearer ${process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER}`;
  }
  return base;
}

async function request(method, path, body, subject = OPERATOR, tenant = TENANT, facilityId = PLANT_A) {
  const url = `${API_BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facilityId}`;
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

function runtimeTenantCount(tenantId, table, whereClause = "") {
  const output = runSql(
    `SET ROLE crevafactos_runtime; SELECT set_config('NevaxisZentra.tenant_id', '${tenantId}', true); SELECT COUNT(*) FROM ${table} ${whereClause};`,
  );
  return Number(output.split("\n").map((l) => l.trim()).filter(Boolean).at(-1));
}

function resetPilotAllocation() {
  runSql(
    `DELETE FROM factory_decisions WHERE execution_idempotency_key = 'network-allocation:${PILOT_ALLOCATION_ID.replace(/-/g, "")}';`,
  );
  runSql(
    `UPDATE network_allocations SET status = 'draft', published_at_utc = NULL, verified_at_utc = NULL, actual_impact_json = '{}', factory_decision_id = NULL, concurrency_token = 1, updated_at_utc = NOW() WHERE id = '${PILOT_ALLOCATION_ID}';`,
  );
  runSql(
    `UPDATE network_allocation_items SET status = 'planned', source_plan_version_id = NULL, destination_plan_version_id = NULL WHERE allocation_id = '${PILOT_ALLOCATION_ID}';`,
  );
}

function ensureFactoryStateSnapshot(facilityId) {
  const existing = Number(
    runSql(
      `SELECT COUNT(*) FROM factory_state_snapshots WHERE tenant_id = '${TENANT_ID}' AND facility_id = '${facilityId}';`,
    ),
  );
  if (existing > 0) return;

  const snapshotId =
    facilityId === PLANT_A
      ? "bb000001-0008-4000-8000-000000000820"
      : "bb000001-0008-4000-8000-000000000821";
  const freshIso = new Date().toISOString();
  runSql(
    `INSERT INTO factory_state_snapshots (
      id, tenant_id, facility_id, captured_at_utc, production_state_json, capacity_state_json,
      machine_state_json, material_state_json, quality_state_json, maintenance_state_json,
      mold_state_json, supplier_state_json, shift_state_json, active_cases_json, constraints_json, freshness_json)
    VALUES (
      '${snapshotId}', '${TENANT_ID}', '${facilityId}', NOW(),
      '{"activeOrderCount":3,"classification":"observed"}',
      '{"utilizationPercent":72,"classification":"observed"}',
      '{"running":4,"stopped":0,"classification":"observed"}',
      '{"shortageSignals":0,"classification":"observed"}',
      '{"holdCount":0,"classification":"observed"}',
      '{"openMaintenanceCases":0,"classification":"observed"}',
      '{"moldCases":0,"classification":"observed"}',
      '{"supplierCases":0,"classification":"observed"}',
      '{"classification":"observed"}',
      '[]',
      '{"erpScopeCreep":0}',
      '{"production":"${freshIso}"}');`,
  );
}

async function patchFactoryState(facilityId, overrides = {}) {
  ensureFactoryStateSnapshot(facilityId);
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
  if (overrides.moldStateJson !== undefined) {
    sets.push(`mold_state_json = '${JSON.stringify(overrides.moldStateJson).replace(/'/g, "''")}'`);
  }
  if (overrides.capacityStateJson !== undefined) {
    sets.push(`capacity_state_json = '${JSON.stringify(overrides.capacityStateJson).replace(/'/g, "''")}'`);
  }
  if (overrides.freshnessJson !== undefined) {
    sets.push(`freshness_json = '${JSON.stringify(overrides.freshnessJson).replace(/'/g, "''")}'`);
  }
  if (sets.length === 0) return;
  runSql(
    `UPDATE factory_state_snapshots SET ${sets.join(", ")} WHERE id = (SELECT id FROM factory_state_snapshots WHERE facility_id = '${facilityId}' ORDER BY captured_at_utc DESC LIMIT 1);`,
  );
}

async function restoreHealthyNetworkState() {
  await patchFactoryState(PLANT_A, {
    machineStateJson: { running: 4, stopped: 0, classification: "observed" },
    qualityStateJson: { holdCount: 0, classification: "observed" },
    materialStateJson: { shortageSignals: 0, classification: "observed" },
    maintenanceStateJson: { openMaintenanceCases: 0, classification: "observed" },
    moldStateJson: { moldCases: 0, classification: "observed" },
    capacityStateJson: { utilizationPercent: 91, classification: "observed" },
    freshnessJson: { production: new Date().toISOString() },
  });
  await patchFactoryState(PLANT_B, {
    machineStateJson: { running: 3, stopped: 0, classification: "observed" },
    qualityStateJson: { holdCount: 0, classification: "observed" },
    materialStateJson: { shortageSignals: 0, classification: "observed" },
    maintenanceStateJson: { openMaintenanceCases: 0, classification: "observed" },
    moldStateJson: { moldCases: 0, classification: "observed" },
    capacityStateJson: { utilizationPercent: 58, classification: "observed" },
    freshnessJson: { production: new Date().toISOString() },
  });
}

async function simulate(actionType, orderId = "PO-1842", quantity = 240) {
  const result = await request("POST", "/api/factory-network/simulate", {
    networkId: NETWORK_ID,
    sourceFacilityId: PLANT_A,
    destinationFacilityId: PLANT_B,
    orderId,
    quantity,
    actionType,
    maxScenarios: 3,
  });
  assert(result.status === 200, `simulate ${actionType} failed: ${result.status} ${JSON.stringify(result.json)}`);
  return result.json ?? [];
}

async function main() {
  const checks = [];
  resetPilotAllocation();
  await restoreHealthyNetworkState();

  const unauth = await fetch(`${API_BASE}/api/factory-network/workspace?facilityId=${PLANT_A}`);
  assert(unauth.status === 401 || unauth.status === 403, "unauthenticated must be denied");

  const crossTenant = await request("GET", `/api/factory-network/workspace?networkId=${NETWORK_ID}`, null, OPERATOR, OTHER_TENANT);
  assert(crossTenant.status === 403 || crossTenant.status === 401, `cross-tenant denied: ${crossTenant.status}`);

  const noPerm = await request(
    "POST",
    "/api/factory-network/execute",
    { allocationId: PILOT_ALLOCATION_ID, concurrencyToken: 1, overrideStaleGuard: false },
    "production-pilot-viewer",
  );
  assert(noPerm.status === 403 || noPerm.status === 401, "unauthorized execute denied");
  checks.push("UM8_SECURITY_MATRIX = PASS");

  const workspace = await request("GET", `/api/factory-network/workspace?networkId=${NETWORK_ID}`);
  assert(workspace.status === 200, `workspace failed: ${workspace.status}`);
  assert(workspace.json.policy, "policy missing");
  assert((workspace.json.capacityMap ?? []).length >= 2, "capacity map missing plants");
  assert((workspace.json.opportunities ?? []).length >= 1, "load balancing opportunity missing");
  checks.push("NETWORK_LOAD_BALANCING_FLOW = PASS");

  await patchFactoryState(PLANT_A, {
    machineStateJson: { running: 1, stopped: 3, fault: 1, classification: "observed" },
    capacityStateJson: { utilizationPercent: 96, classification: "observed" },
  });
  const failoverEval = await request("POST", "/api/factory-network/evaluate-allocation", {
    networkId: NETWORK_ID,
    triggerType: "machine-failure",
    triggerContextJson: "{}",
  });
  assert(failoverEval.status === 200, `failover evaluate failed: ${failoverEval.status}`);
  const failoverSim = await simulate("move-full-order");
  assert(failoverSim.length >= 1, "failover simulation missing");
  await restoreHealthyNetworkState();
  checks.push("FACTORY_FAILOVER_FLOW = PASS");

  await patchFactoryState(PLANT_B, {
    materialStateJson: { shortageSignals: 3, shortage: true, classification: "observed" },
  });
  const materialSim = await simulate("move-full-order", "PO-1901", 120);
  const materialMove = materialSim.find((row) => row.scenario?.actionType === "move-full-order");
  assert(!materialMove || materialMove.feasibility?.isFeasible === false, "material constraint must block transfer");
  await restoreHealthyNetworkState();
  checks.push("NETWORK_MATERIAL_CONSTRAINT_FLOW = PASS");

  const moldSim = await simulate("move-full-order", "PO-1901", 120);
  const moldMove = moldSim.find((row) => row.scenario?.actionType === "move-full-order");
  assert(moldMove, "mold scenario missing");
  assert(
    (moldMove.feasibility?.softWarnings ?? []).some((w) => /kalıp/i.test(w)),
    "mold compatibility warning missing",
  );
  checks.push("NETWORK_MOLD_FLOW = PASS");

  await patchFactoryState(PLANT_B, {
    qualityStateJson: { holdCount: 2, hold: true, classification: "observed" },
  });
  const qualitySim = await simulate("move-full-order");
  const qualityMove = qualitySim.find((row) => row.scenario?.actionType === "move-full-order");
  assert(!qualityMove || qualityMove.feasibility?.isFeasible === false, "quality hold must block transfer");
  await restoreHealthyNetworkState();
  checks.push("NETWORK_QUALITY_GUARD_FLOW = PASS");

  const splitSim = await simulate("split-order");
  assert(splitSim.some((row) => row.scenario?.actionType === "split-order"), "split-order scenario missing");
  checks.push("NETWORK_ORDER_SPLIT_FLOW = PASS");

  const staleIso = new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString();
  await patchFactoryState(PLANT_A, { freshnessJson: { production: staleIso } });
  await patchFactoryState(PLANT_B, { freshnessJson: { production: staleIso } });
  resetPilotAllocation();
  const staleExecute = await request("POST", "/api/factory-network/execute", {
    allocationId: PILOT_ALLOCATION_ID,
    concurrencyToken: 1,
    overrideStaleGuard: false,
  });
  assert(staleExecute.status === 422, `stale execute must fail: ${staleExecute.status}`);
  await restoreHealthyNetworkState();
  checks.push("NETWORK_STALE_DATA_FLOW = PASS");

  resetPilotAllocation();
  await request("PUT", `/api/factory-network/policy?networkId=${NETWORK_ID}`, {
    concurrencyToken: workspace.json.policy.concurrencyToken,
    autonomyMode: "limited",
    policyEnvelopeJson: JSON.stringify({
      approvalThresholdOrders: 2,
      maxTransferQuantity: 500,
      prohibitedPairs: [],
    }),
  });
  const limitedExecute = await request("POST", "/api/factory-network/execute", {
    allocationId: PILOT_ALLOCATION_ID,
    concurrencyToken: 1,
    overrideStaleGuard: false,
  });
  assert(limitedExecute.status === 200, `limited execute failed: ${limitedExecute.status}`);
  assert(limitedExecute.json?.allocation?.status === "published", "limited autonomy must publish");
  checks.push("NETWORK_LIMITED_AUTONOMY_FLOW = PASS");

  resetPilotAllocation();
  runSql(
    `UPDATE factory_network_policies SET policy_envelope_json = '{"approvalThresholdOrders":1,"maxTransferQuantity":100,"prohibitedPairs":[]}', autonomy_mode = 'full', concurrency_token = concurrency_token + 1 WHERE network_id = '${NETWORK_ID}';`,
  );
  const highImpactExecute = await request("POST", "/api/factory-network/execute", {
    allocationId: PILOT_ALLOCATION_ID,
    concurrencyToken: 1,
    overrideStaleGuard: false,
  });
  assert(highImpactExecute.status === 422, `high impact must require approval: ${highImpactExecute.status}`);
  const pendingToken = highImpactExecute.json?.allocation?.concurrencyToken ?? 2;
  const approved = await request(
    "POST",
    "/api/factory-network/approve",
    {
      allocationId: PILOT_ALLOCATION_ID,
      concurrencyToken: pendingToken,
      approved: true,
      notes: "UM8 approval",
    },
    REVIEWER,
  );
  assert(approved.status === 200, `approve failed: ${approved.status}`);
  checks.push("NETWORK_HIGH_IMPACT_APPROVAL_FLOW = PASS");

  const verified = await request("POST", `/api/factory-network/verify-outcome?allocationId=${PILOT_ALLOCATION_ID}`);
  assert(verified.status === 200, `verify outcome failed: ${verified.status}`);
  const memoryCount = runtimeTenantCount(
    TENANT_ID,
    "factory_learning_memories",
    `WHERE facility_id IN ('${PLANT_A}', '${PLANT_B}')`,
  );
  assert(memoryCount >= 1, "learning memory missing");
  checks.push("NETWORK_LEARNING_FLOW = PASS");

  const dupExecute = await request("POST", "/api/factory-network/execute", {
    allocationId: PILOT_ALLOCATION_ID,
    concurrencyToken: 1,
    overrideStaleGuard: true,
  });
  assert(dupExecute.status === 409, `duplicate execute must conflict: ${dupExecute.status}`);

  const conservation = runSql(
    `SELECT source_quantity + destination_quantity = total_quantity FROM network_allocation_items WHERE allocation_id = '${PILOT_ALLOCATION_ID}';`,
  );
  assert(conservation === "t", "allocation item quantity conservation violated");
  const allocationCount = Number(
    runSql(`SELECT COUNT(*) FROM network_allocations WHERE network_id = '${NETWORK_ID}';`),
  );
  assert(allocationCount >= 1, "allocation history missing");
  checks.push("UM8_DATA_INTEGRITY = PASS");

  const ap = await request("GET", "/api/adaptive-planning/workspace");
  assert(ap.status === 200, "UM7 regression failed");
  checks.push("UM7_REGRESSION = PASS");

  const fl = await request("GET", "/api/factory-learning/workspace");
  assert(fl.status === 200, "UM6 regression failed");
  checks.push("UM6_REGRESSION = PASS");

  console.log("UM8_RUNTIME_VERIFIER = PASS");
  for (const check of checks) {
    console.log(check);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

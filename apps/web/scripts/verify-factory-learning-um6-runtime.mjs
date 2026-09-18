#!/usr/bin/env node

import { spawnSync } from "node:child_process";

const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const OTHER_TENANT = "other-tenant-not-pilot";
const OTHER_TENANT_ID = "11111111-1111-4111-8111-111111111111";
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
    "nevaxiszentra-local-postgres-1",
    "NevaxisZentra-local-postgres-1",
  ];
  for (const name of candidates) {
    const result = spawnSync("docker", ["ps", "--filter", `name=${name}`, "--format", "{{.Names}}"], { encoding: "utf8" });
    const match = (result.stdout ?? "").trim().split(/\r?\n/).find(Boolean);
    if (match) {
      return match;
    }
  }

  throw new Error("postgres container not found for factory-learning verification");
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

function sqlJson(value) {
  return JSON.stringify(value).replace(/'/g, "''");
}

function patchLatestSnapshot(overrides = {}) {
  const sets = [];
  if (overrides.productionStateJson !== undefined) {
    sets.push(`production_state_json = '${sqlJson(overrides.productionStateJson)}'`);
  }
  if (overrides.machineStateJson !== undefined) {
    sets.push(`machine_state_json = '${sqlJson(overrides.machineStateJson)}'`);
  }
  if (overrides.qualityStateJson !== undefined) {
    sets.push(`quality_state_json = '${sqlJson(overrides.qualityStateJson)}'`);
  }
  if (overrides.materialStateJson !== undefined) {
    sets.push(`material_state_json = '${sqlJson(overrides.materialStateJson)}'`);
  }
  if (overrides.supplierStateJson !== undefined) {
    sets.push(`supplier_state_json = '${sqlJson(overrides.supplierStateJson)}'`);
  }
  if (overrides.moldStateJson !== undefined) {
    sets.push(`mold_state_json = '${sqlJson(overrides.moldStateJson)}'`);
  }
  if (overrides.shiftStateJson !== undefined) {
    sets.push(`shift_state_json = '${sqlJson(overrides.shiftStateJson)}'`);
  }
  if (sets.length === 0) {
    throw new Error("patchLatestSnapshot requires at least one state override");
  }

  runSql(
    `UPDATE factory_state_snapshots SET ${sets.join(", ")} WHERE id = (SELECT id FROM factory_state_snapshots WHERE facility_id = '${FACILITY_ID}' ORDER BY captured_at_utc DESC LIMIT 1);`,
  );
}

async function refreshLosses() {
  const refreshed = await request("POST", "/api/factory-learning/refresh");
  assert(refreshed.status === 200, `refresh failed: ${refreshed.status}`);
  return refreshed.json;
}

async function seedRecurringLoss(patchFn, refreshCount = 2, correlationKey = null) {
  patchFn();
  let workspace = null;
  for (let i = 0; i < refreshCount; i++) {
    workspace = await refreshLosses();
  }
  if (correlationKey) {
    runSql(
      `UPDATE factory_losses SET recurrence_count = GREATEST(recurrence_count, 2), last_seen_at_utc = NOW() WHERE facility_id = '${FACILITY_ID}' AND correlation_key = '${correlationKey}';`,
    );
  } else {
    runSql(
      `UPDATE factory_losses SET recurrence_count = GREATEST(recurrence_count, 2), last_seen_at_utc = NOW() WHERE facility_id = '${FACILITY_ID}';`,
    );
  }
  workspace = await refreshLosses();
  if ((workspace.patterns ?? []).length === 0) {
    runSql(
      `UPDATE factory_losses SET recurrence_count = GREATEST(recurrence_count, 2), last_seen_at_utc = NOW() WHERE facility_id = '${FACILITY_ID}';`,
    );
    workspace = await refreshLosses();
  }
  return workspace;
}

function findOpportunity(workspace, matcher) {
  const opportunities = workspace.opportunities ?? [];
  const match = opportunities.find(matcher) ?? opportunities[0];
  assert(match, `opportunity not found among ${opportunities.length} candidates`);
  return match;
}

async function createExperiment(opportunity, experimentType, title, options = {}) {
  const {
    baselineMinutes = 42,
    experimentMinutes = 34,
    guardrailsJson = '["quality-regression"]',
    hypothesis = "Bounded operational change reduces recurring loss",
  } = options;

  const created = await request("POST", "/api/factory-learning/experiments", {
    opportunityId: opportunity.id,
    experimentType,
    title,
    hypothesis,
    scopeJson: JSON.stringify({ machine: "IM-07", flow: title }),
    baselineJson: JSON.stringify({ durationMinutes: baselineMinutes }),
    targetJson: JSON.stringify({ durationMinutes: experimentMinutes }),
    guardrailsJson,
    stopConditionsJson: "[]",
  });
  assert(created.status === 200, `create experiment failed: ${created.status}`);
  return created.json;
}

async function approveExperiment(experiment) {
  if (experiment.status !== "approval-pending") {
    return experiment;
  }

  const approved = await request(
    "POST",
    `/api/factory-learning/experiments/${experiment.id}/approve`,
    { approved: true, concurrencyToken: experiment.concurrencyToken },
    REVIEWER,
  );
  assert(approved.status === 200, `approve failed: ${approved.status}`);
  return approved.json;
}

async function startExperiment(experiment) {
  const started = await request("POST", `/api/factory-learning/experiments/${experiment.id}/start`, {
    concurrencyToken: experiment.concurrencyToken,
  });
  assert(started.status === 200, `start failed: ${started.status}`);
  return started.json;
}

async function completeExperiment(experiment, measurements) {
  const completed = await request("POST", `/api/factory-learning/experiments/${experiment.id}/complete`, {
    concurrencyToken: experiment.concurrencyToken,
    measurements,
  });
  assert(completed.status === 200, `complete failed: ${completed.status}`);
  return completed.json;
}

async function verifyExperiment(experiment, outcome = "") {
  const verified = await request("POST", `/api/factory-learning/experiments/${experiment.id}/verify`, {
    concurrencyToken: experiment.concurrencyToken,
    outcome,
  });
  assert(verified.status === 200, `verify failed: ${verified.status} ${JSON.stringify(verified.json)}`);
  return verified.json;
}

async function runSuccessExperimentFlow(opportunity, experimentType, title, options = {}) {
  let experiment = await createExperiment(opportunity, experimentType, title, options);
  experiment = await approveExperiment(experiment);
  experiment = await startExperiment(experiment);
  experiment = await completeExperiment(experiment, [
    {
      phase: "experiment",
      metricKey: "duration-minutes",
      metricValue: options.experimentMinutes ?? 34,
      unit: "minutes",
      dataClassification: "measured",
      evidenceJson: "[]",
    },
    ...(options.extraMeasurements ?? []),
  ]);
  return verifyExperiment(experiment, options.verifyOutcome ?? "");
}

function runtimeTenantCount(tenantId, table, whereClause = "") {
  const output = runSql(
    `SET ROLE crevafactos_runtime; SELECT set_config('NevaxisZentra.tenant_id', '${tenantId}', true); SELECT COUNT(*) FROM ${table} ${whereClause};`,
  );
  const lines = output.split("\n").map((line) => line.trim()).filter(Boolean);
  return Number(lines.at(-1));
}

async function main() {
  const checks = [];

  const unauth = await fetch(`${API_BASE}/api/factory-learning/workspace?facilityId=${FACILITY_ID}`);
  assert(unauth.status === 401 || unauth.status === 403, "unauthenticated workspace must be denied");

  const crossTenant = await request("GET", "/api/factory-learning/workspace", null, OPERATOR, OTHER_TENANT);
  assert(crossTenant.status === 403 || crossTenant.status === 401, `cross-tenant must be denied: ${crossTenant.status}`);
  checks.push("UM6_SECURITY_MATRIX = PASS");

  const pmWorkspace = await request("GET", "/api/production-manager/workspace");
  assert(pmWorkspace.status === 200, `production-manager workspace failed: ${pmWorkspace.status}`);
  assert(pmWorkspace.json.executive, "production-manager state capture missing");

  let workspace = await refreshLosses();
  assert(Array.isArray(workspace.losses), "losses missing");
  assert(Array.isArray(workspace.patterns), "patterns missing");
  assert(Array.isArray(workspace.opportunities), "opportunities missing");
  assert(workspace.commandSummary, "command summary missing");

  const machineWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      machineStateJson: { stopped: 2, fault: 1, classification: "observed" },
    });
  }, 2, "downtime:IM-07");
  const machineOpportunity = findOpportunity(
    machineWorkspace,
    (o) => o.domain === "maintenance" || /bakım/i.test(o.title),
  );
  const machineResult = await runSuccessExperimentFlow(
    machineOpportunity,
    "maintenance",
    "UM6 machine maintenance interval experiment",
    { baselineMinutes: 42, experimentMinutes: 34 },
  );
  assert(machineResult.experiment.status === "verified", `machine experiment status: ${machineResult.experiment.status}`);
  assert(machineResult.experiment.outcome === "success", `machine outcome: ${machineResult.experiment.outcome}`);
  assert(machineResult.verifiedImprovement, "machine verified improvement missing");
  checks.push("MACHINE_IMPROVEMENT_FLOW = PASS");

  const qualityWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      qualityStateJson: { scrap: 3, holdCount: 1, classification: "observed" },
    });
  }, 2, "scrap:facility");
  const qualityOpportunity = findOpportunity(qualityWorkspace, (o) => /fire|kalite|scrap/i.test(`${o.title} ${o.problemSummary}`));
  const qualityResult = await runSuccessExperimentFlow(
    qualityOpportunity,
    "quality",
    "UM6 quality scrap reduction experiment",
    {
      extraMeasurements: [
        {
          phase: "baseline",
          metricKey: "scrap-rate",
          metricValue: 0.04,
          unit: "ratio",
          dataClassification: "measured",
          evidenceJson: "[]",
        },
        {
          phase: "experiment",
          metricKey: "scrap-rate",
          metricValue: 0.02,
          unit: "ratio",
          dataClassification: "measured",
          evidenceJson: "[]",
        },
      ],
    },
  );
  assert(qualityResult.experiment.outcome === "success", `quality outcome: ${qualityResult.experiment.outcome}`);
  checks.push("QUALITY_IMPROVEMENT_FLOW = PASS");

  const changeoverWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      productionStateJson: { changeoverMinutes: 42, highChangeover: true, classification: "observed" },
    });
  }, 2, "changeover:Line-2");
  const changeoverOpportunity = findOpportunity(changeoverWorkspace, (o) => /setup|changeover|değişim/i.test(o.title));
  const changeoverResult = await runSuccessExperimentFlow(
    changeoverOpportunity,
    "process",
    "UM6 changeover setup experiment",
  );
  assert(changeoverResult.experiment.outcome === "success", `changeover outcome: ${changeoverResult.experiment.outcome}`);
  checks.push("CHANGEOVER_IMPROVEMENT_FLOW = PASS");

  const materialWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      materialStateJson: { shortageSignals: 2, classification: "observed" },
    });
  }, 2, "material-shortage:PP-HF");
  const materialOpportunity = findOpportunity(materialWorkspace, (o) => /malzeme/i.test(o.title));
  const materialResult = await runSuccessExperimentFlow(
    materialOpportunity,
    "material",
    "UM6 material threshold experiment",
  );
  assert(materialResult.experiment.outcome === "success", `material outcome: ${materialResult.experiment.outcome}`);
  checks.push("MATERIAL_IMPROVEMENT_FLOW = PASS");

  const supplierWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      supplierStateJson: { delay: true, supplierCases: 2, classification: "observed" },
    });
  }, 2, "supplier-delay:facility");
  const supplierOpportunity = findOpportunity(supplierWorkspace, (o) => /tedarik/i.test(o.title));
  const supplierResult = await runSuccessExperimentFlow(
    supplierOpportunity,
    "supplier",
    "UM6 supplier strategy experiment",
  );
  assert(supplierResult.experiment.outcome === "success", `supplier outcome: ${supplierResult.experiment.outcome}`);
  checks.push("SUPPLIER_IMPROVEMENT_FLOW = PASS");

  const moldWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      moldStateJson: { moldCases: 2, classification: "observed" },
    });
  }, 2, "mold-unavailability:M-014");
  const moldOpportunity = findOpportunity(moldWorkspace, (o) => /kalıp/i.test(o.title));
  const moldResult = await runSuccessExperimentFlow(
    moldOpportunity,
    "mold",
    "UM6 mold maintenance experiment",
  );
  assert(moldResult.experiment.outcome === "success", `mold outcome: ${moldResult.experiment.outcome}`);
  checks.push("MOLD_IMPROVEMENT_FLOW = PASS");

  const shiftWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      shiftStateJson: { carryover: 2, pending: true, incomplete: true, classification: "observed" },
    });
  }, 2, "shift-handover-loss:facility");
  const shiftOpportunity = findOpportunity(shiftWorkspace, (o) => /vardiya/i.test(o.title));
  const shiftResult = await runSuccessExperimentFlow(
    shiftOpportunity,
    "shift",
    "UM6 shift handover experiment",
  );
  assert(shiftResult.experiment.outcome === "success", `shift outcome: ${shiftResult.experiment.outcome}`);
  checks.push("SHIFT_IMPROVEMENT_FLOW = PASS");

  const failedWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      machineStateJson: { stopped: 1, classification: "observed" },
    });
  }, 2, "downtime:IM-07");
  const failedOpportunity = findOpportunity(
    failedWorkspace,
    (o) => o.domain === "maintenance" || /bakım|iyileştirme/i.test(o.title),
  );
  let failedExperiment = await createExperiment(
    failedOpportunity,
    "maintenance",
    "UM6 guardrail breach experiment",
    { guardrailsJson: '["quality-regression"]' },
  );
  failedExperiment = await approveExperiment(failedExperiment);
  failedExperiment = await startExperiment(failedExperiment);
  failedExperiment = await completeExperiment(failedExperiment, [
    {
      phase: "experiment",
      metricKey: "duration-minutes",
      metricValue: 34,
      unit: "minutes",
      dataClassification: "measured",
      evidenceJson: "[]",
    },
    {
      phase: "experiment",
      metricKey: "scrap-rate",
      metricValue: 0.12,
      unit: "ratio",
      dataClassification: "measured",
      evidenceJson: "[]",
    },
  ]);
  const failedVerify = await verifyExperiment(failedExperiment, "");
  assert(failedVerify.experiment.status === "failed", `failed experiment status: ${failedVerify.experiment.status}`);
  assert(failedVerify.experiment.outcome === "negative-effect", `failed outcome: ${failedVerify.experiment.outcome}`);
  checks.push("FAILED_EXPERIMENT_FLOW = PASS");

  const inconclusiveWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      productionStateJson: { behindScheduleCount: 2, classification: "observed" },
    });
  }, 2, "planning-loss:PO-1842");
  const inconclusiveOpportunity = findOpportunity(inconclusiveWorkspace, () => true);
  let inconclusiveExperiment = await createExperiment(
    inconclusiveOpportunity,
    "planning",
    "UM6 inconclusive experiment",
    { guardrailsJson: "[]" },
  );
  inconclusiveExperiment = await approveExperiment(inconclusiveExperiment);
  inconclusiveExperiment = await startExperiment(inconclusiveExperiment);
  inconclusiveExperiment = await completeExperiment(inconclusiveExperiment, []);
  const inconclusiveVerify = await verifyExperiment(inconclusiveExperiment, "");
  assert(
    inconclusiveVerify.experiment.status === "inconclusive",
    `inconclusive status: ${inconclusiveVerify.experiment.status}`,
  );
  assert(
    inconclusiveVerify.experiment.outcome === "inconclusive",
    `inconclusive outcome: ${inconclusiveVerify.experiment.outcome}`,
  );
  checks.push("INCONCLUSIVE_EXPERIMENT_FLOW = PASS");

  const sustainabilityWorkspace = await seedRecurringLoss(() => {
    patchLatestSnapshot({
      machineStateJson: { stopped: 2, classification: "observed" },
    });
  }, 2, "downtime:IM-07");
  const sustainabilityOpportunity = findOpportunity(
    sustainabilityWorkspace,
    (o) => o.domain === "maintenance" || /bakım/i.test(o.title),
  );
  const sustainabilityResult = await runSuccessExperimentFlow(
    sustainabilityOpportunity,
    "maintenance",
    "UM6 sustainability regression seed experiment",
  );
  assert(sustainabilityResult.experiment.outcome === "success", "sustainability seed must succeed");

  const sustainabilityCheck = await request("POST", "/api/factory-learning/sustainability/check");
  assert(sustainabilityCheck.status === 200, `sustainability check failed: ${sustainabilityCheck.status}`);
  assert(
    (sustainabilityCheck.json.commandSummary?.regressingImprovementCount ?? 0) > 0,
    "regressing improvement count should be positive after sustainability check",
  );
  checks.push("SUSTAINABILITY_REGRESSION_FLOW = PASS");

  workspace = await request("GET", "/api/factory-learning/workspace");
  assert(workspace.status === 200, "final workspace read failed");
  assert(workspace.json.paretoRows?.length >= 0, "pareto rows missing");
  assert(workspace.json.learningMemories?.length >= 1, "learning memories missing");

  const lossCount = runSql(`SELECT COUNT(*) FROM factory_losses WHERE facility_id = '${FACILITY_ID}' AND tenant_id = '${TENANT_ID}';`);
  assert(Number(lossCount) > 0, "factory losses must exist for facility tenant");
  checks.push("UM6_DATA_INTEGRITY = PASS");

  for (const table of ["factory_losses", "factory_experiments", "verified_improvements"]) {
    const rls = runSql(`SELECT relrowsecurity, relforcerowsecurity FROM pg_class WHERE relname = '${table}';`);
    assert(rls === "t|t", `${table} RLS must be enabled`);
  }

  const visibleRows = runtimeTenantCount(TENANT_ID, "factory_losses", `WHERE facility_id = '${FACILITY_ID}'`);
  assert(visibleRows > 0, "runtime role must see tenant losses");
  const hiddenRows = runtimeTenantCount(OTHER_TENANT_ID, "factory_losses", `WHERE tenant_id = '${TENANT_ID}'`);
  assert(hiddenRows === 0, "wrong tenant must not see pilot losses");
  checks.push("UM6_RLS = PASS");

  const unauthorizedRefresh = await request("POST", "/api/factory-learning/refresh", null, "quality-negative-test-user");
  assert(
    unauthorizedRefresh.status === 403 || unauthorizedRefresh.status === 401,
    `unauthorized refresh must be denied: ${unauthorizedRefresh.status}`,
  );
  checks.push("UM6_AUTHORIZATION = PASS");

  for (const check of checks) {
    console.log(check);
  }
  console.log("FACTORY_LEARNING_UM6_RUNTIME_VERIFIER = PASS");
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

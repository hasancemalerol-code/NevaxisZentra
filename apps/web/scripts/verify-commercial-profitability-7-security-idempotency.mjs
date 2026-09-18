#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const SALES_WITHIN = "bb000001-0321-4000-8000-000000000321";
const SALES_BELOW = "bb000001-0323-4000-8000-000000000323";
const SALES_MISSING_COST = "bb000001-0326-4000-8000-000000000326";
const SOURCE_PROVENANCE = "development-pilot-commercial-profitability";
const CLOSURE_PREFIX = "COMMPROFIT7-CLOSURE";
const RUN_SUFFIX = Date.now();

const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const REVIEWER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-7 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(MANAGER)) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM ai_task_audit_events
    WHERE task_id IN (
      SELECT linked_ai_task_id FROM commercial_pricing_scenarios
      WHERE linked_ai_task_id IS NOT NULL
        AND source_provenance LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM ai_tasks
    WHERE id IN (
      SELECT linked_ai_task_id FROM commercial_pricing_scenarios
      WHERE linked_ai_task_id IS NOT NULL
        AND source_provenance LIKE '${CLOSURE_PREFIX}%'
    );
    UPDATE commercial_pricing_scenarios
    SET linked_ai_task_id = NULL,
        latest_evaluation_snapshot_id = NULL
    WHERE source_provenance LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_pricing_scenario_audit_events
    WHERE scenario_id IN (
      SELECT id FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_pricing_scenario_evaluation_snapshots
    WHERE scenario_id IN (
      SELECT id FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%';
  `);
}

function scenarioCount() {
  return Number(db(`SELECT COUNT(*) FROM commercial_pricing_scenarios WHERE source_provenance LIKE '${CLOSURE_PREFIX}%'`));
}

function snapshotCount(scenarioId) {
  return Number(db(`
    SELECT COUNT(*) FROM commercial_pricing_scenario_evaluation_snapshots
    WHERE scenario_id = '${scenarioId}'
  `));
}

function snapshotFingerprint(scenarioId) {
  return db(`
    SELECT guardrail_result || '|' || proposed_unit_price::text || '|' || evaluation_sequence::text
    FROM commercial_pricing_scenario_evaluation_snapshots
    WHERE scenario_id = '${scenarioId}'
    ORDER BY evaluation_sequence ASC
    LIMIT 1
  `);
}

function taskCountForScenario(scenarioId) {
  return Number(db(`
    SELECT COUNT(*) FROM ai_tasks
    WHERE id IN (
      SELECT linked_ai_task_id FROM commercial_pricing_scenarios
      WHERE id = '${scenarioId}' AND linked_ai_task_id IS NOT NULL
    )
  `));
}

async function createScenario(salesEvidenceId, proposedUnitPrice, label, headers = actorHeaders(MANAGER)) {
  return liveRequest(
    "POST",
    `/api/commercial-intelligence/pricing-scenarios?facilityId=${FACILITY}`,
    {
      profileId: PROFILE,
      expectedProfileVersion: 1,
      salesEvidenceObservationId: salesEvidenceId,
      proposedUnitPrice,
      commercialRationale: `${CLOSURE_PREFIX} ${label}`,
      sourceProvenance: `${CLOSURE_PREFIX}-${RUN_SUFFIX}`,
    },
    headers,
  );
}

async function evaluateScenario(scenarioId, expectedVersion, headers = actorHeaders(MANAGER)) {
  return liveRequest(
    "POST",
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/evaluate?facilityId=${FACILITY}`,
    { expectedVersion },
    headers,
  );
}

async function transitionScenario(scenarioId, expectedVersion, targetLifecycleStatus, note, headers = actorHeaders(MANAGER)) {
  return liveRequest(
    "POST",
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/transitions?facilityId=${FACILITY}`,
    { expectedVersion, targetLifecycleStatus, note },
    headers,
  );
}

console.log("Running COMMERCIAL-PROFITABILITY-7 security/idempotency verifier.");

const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialPricingScenarioEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/CommercialIntelligence/CommercialIntelligenceEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260828143401_AddCommercialPricingScenariosFoundation.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligencePermissionKeys.cs");
const lifecycle = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialPricingScenarioLifecycle.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialPricingScenarioRepository.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");
const profitabilitySeed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialProfitabilitySeed.cs");
const apiCommercialDir = [
  "apps/api/CommercialIntelligence/CommercialPricingScenarioEndpointRouteBuilderExtensions.cs",
  "apps/api/CommercialIntelligence/CommercialIntelligenceEndpointHelpers.cs",
].map(readRequired).join("\n");

assert("pricing scenario endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("workspace endpoint", endpoints.includes('MapGet("/workspace"'));
assert("list endpoint", endpoints.includes('MapGet("/", ListScenariosAsync'));
assert("detail endpoint", endpoints.includes('MapGet("/{scenarioId:guid}"'));
assert("create endpoint", endpoints.includes('MapPost("/", CreateScenarioAsync'));
assert("update endpoint", endpoints.includes('MapPut("/{scenarioId:guid}"'));
assert("evaluate endpoint", endpoints.includes('MapPost("/{scenarioId:guid}/evaluate"'));
assert("transition endpoint", endpoints.includes('MapPost("/{scenarioId:guid}/transitions"'));
assert("task draft endpoint", endpoints.includes('MapPost("/{scenarioId:guid}/task-draft"'));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("manage access helper", helpers.includes("ResolveManageAccessAsync"));
assert("review access helper", endpoints.includes("ResolveReviewAccessAsync"));
assert("commercial-intelligence.read key", permissions.includes("commercial-intelligence.read"));
assert("commercial-intelligence.manage key", permissions.includes("commercial-intelligence.manage"));
assert("rejection note validation", lifecycle.includes("ValidateTransitionNote"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("scenarios update grant", migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE commercial_pricing_scenarios"));
assert("snapshots append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_pricing_scenario_evaluation_snapshots"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_pricing_scenario_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE commercial_pricing_scenarios", "scenarios table");
forbidText(migration, "GRANT DELETE ON TABLE commercial_pricing_scenario_evaluation_snapshots", "snapshots table");
forbidText(migration, "GRANT DELETE ON TABLE commercial_pricing_scenario_audit_events", "audit table");
forbidText(migration, "GRANT UPDATE ON TABLE commercial_pricing_scenario_evaluation_snapshots", "snapshots table");
forbidText(endpoints, "MapDelete", "pricing scenario endpoints");
forbidText(repository, "CommercialPricingScenarioEvaluationSnapshots.Update", "snapshot repository");
forbidText(repository, ".Update(snapshot", "snapshot repository update call");
forbidText(apiCommercialDir, "ErpWriteback", "commercial intelligence API");
forbidText(apiCommercialDir, "IErpWritebackAdapter", "commercial intelligence API");
forbidText(apiCommercialDir, "erp_writeback", "commercial intelligence API");
forbidText(repository, "ErpWriteback", "pricing scenario repository");
forbidText(repository, "ApplyPrice", "pricing scenario repository");
assert("bootstrap profitability seed", bootstrap.includes("EnsureCommercialProfitabilityPilotRecordsAsync"));
assert("profitability seed within scenario", profitabilitySeed.includes("commprofit1-pilot-within-threshold"));
assert("profitability seed missing cost scenario", profitabilitySeed.includes("commprofit1-pilot-missing-energy"));

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(REVIEWER);
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const scenariosBeforeDeny = scenarioCount();

const noAuth = await fetch(`${BASE}/api/commercial-intelligence/pricing-scenarios/workspace?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyCreate = await createScenario(SALES_WITHIN, 100, "readonly-create-deny", readonlyHeaders);
assert("readonly create 403", readonlyCreate.status === 403, `status=${readonlyCreate.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios?facilityId=${UNAUTH_FACILITY}`,
  null,
  managerHeaders,
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios/workspace?facilityId=${FACILITY}`,
  null,
  crossTenantHeaders,
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

assert("denied requests created no scenarios", scenarioCount() === scenariosBeforeDeny, `${scenariosBeforeDeny}→${scenarioCount()}`);

const workspace = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios/workspace?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("authorized workspace 200", workspace.status === 200, workspace.text?.slice(0, 200));
assert("workspace capabilities", workspace.json?.capabilities?.canRead === true);
assert("workspace manage capability", workspace.json?.capabilities?.canManage === true);
assert("automation disclaimer", workspace.json?.automationDisclaimer === "Bu onay fiyatı otomatik olarak uygulamaz.");

const readonlyWorkspace = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios/workspace?facilityId=${FACILITY}`,
  null,
  readonlyHeaders,
);
assert("readonly workspace 200", readonlyWorkspace.status === 200, readonlyWorkspace.text?.slice(0, 200));
assert("readonly cannot manage", readonlyWorkspace.json?.capabilities?.canManage === false);
assert("readonly can review", readonlyWorkspace.json?.capabilities?.canReview === true);

const concurrencyCreate = await createScenario(SALES_WITHIN, 100, "concurrency");
const concurrencyScenarioId = concurrencyCreate.json?.entityId ?? concurrencyCreate.json?.scenario?.id;
const staleEvaluate = await evaluateScenario(concurrencyScenarioId, 999);
assert("optimistic concurrency evaluate 409", staleEvaluate.status === 409, `status=${staleEvaluate.status}`);

const guardrailCases = [
  { label: "passes", salesEvidenceId: SALES_WITHIN, proposedUnitPrice: 100, expectedGuardrail: "passes", expectedLifecycle: "eligible-for-approval" },
  { label: "needs-review", salesEvidenceId: SALES_WITHIN, proposedUnitPrice: 80, expectedGuardrail: "needs-review", expectedLifecycle: "needs-review" },
  { label: "blocked", salesEvidenceId: SALES_WITHIN, proposedUnitPrice: 50, expectedGuardrail: "blocked", expectedLifecycle: "blocked" },
  { label: "insufficient-data", salesEvidenceId: SALES_MISSING_COST, proposedUnitPrice: 100, expectedGuardrail: "insufficient-data", expectedLifecycle: "insufficient-data" },
  { label: "needs-review-below-material", salesEvidenceId: SALES_BELOW, proposedUnitPrice: 100, expectedGuardrail: "needs-review", expectedLifecycle: "needs-review" },
];

const createdScenarioIds = [];

for (const testCase of guardrailCases) {
  const created = await createScenario(testCase.salesEvidenceId, testCase.proposedUnitPrice, testCase.label);
  assert(`${testCase.label} create 201/200`, created.status === 201 || created.status === 200, created.text?.slice(0, 200));
  const scenarioId = created.json?.entityId ?? created.json?.scenario?.id;
  assert(`${testCase.label} scenario id`, Boolean(scenarioId));
  createdScenarioIds.push(scenarioId);

  const evaluated = await evaluateScenario(scenarioId, created.json?.version ?? 1);
  assert(`${testCase.label} evaluate 200`, evaluated.status === 200, evaluated.text?.slice(0, 200));
  assert(`${testCase.label} guardrail`, evaluated.json?.scenario?.guardrailResult === testCase.expectedGuardrail, evaluated.json?.scenario?.guardrailResult);
  assert(`${testCase.label} lifecycle`, evaluated.json?.scenario?.lifecycleStatus === testCase.expectedLifecycle, evaluated.json?.scenario?.lifecycleStatus);
  assert(`${testCase.label} evaluation snapshot`, Boolean(evaluated.json?.scenario?.latestEvaluation?.id));
}

const passesScenarioId = createdScenarioIds[0];
const passesDetail = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios/${passesScenarioId}?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("passes detail 200", passesDetail.status === 200);

const submitted = await transitionScenario(
  passesScenarioId,
  passesDetail.json.version,
  "submitted-for-approval",
  null,
);
assert("submit for approval 200", submitted.status === 200, submitted.text?.slice(0, 200));
assert("submitted lifecycle", submitted.json?.scenario?.lifecycleStatus === "submitted-for-approval");

const firstSnapshotFingerprint = snapshotFingerprint(passesScenarioId);
assert("first snapshot persisted", Boolean(firstSnapshotFingerprint));

const blockedScenarioId = createdScenarioIds[2];
const blockedDetail = await liveRequest(
  "GET",
  `/api/commercial-intelligence/pricing-scenarios/${blockedScenarioId}?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
const blockedSubmit = await transitionScenario(
  blockedScenarioId,
  blockedDetail.json.version,
  "submitted-for-approval",
  null,
);
assert("blocked submit 400", blockedSubmit.status === 400, `status=${blockedSubmit.status}`);

const rejectWithoutNote = await transitionScenario(
  passesScenarioId,
  submitted.json?.scenario?.version ?? submitted.json?.version,
  "rejected",
  null,
  readonlyHeaders,
);
assert("reject without note 400", rejectWithoutNote.status === 400, `status=${rejectWithoutNote.status}`);

const rejectWithNote = await transitionScenario(
  passesScenarioId,
  submitted.json?.scenario?.version ?? submitted.json?.version,
  "rejected",
  "Kapanış doğrulaması için reddetme gerekçesi",
  readonlyHeaders,
);
assert("reviewer reject with note 200", rejectWithNote.status === 200, rejectWithNote.text?.slice(0, 200));
assert("rejected lifecycle", rejectWithNote.json?.scenario?.lifecycleStatus === "rejected");

const lifecycleFlowCreate = await createScenario(SALES_WITHIN, 100, "lifecycle-flow");
const lifecycleScenarioId = lifecycleFlowCreate.json?.entityId ?? lifecycleFlowCreate.json?.scenario?.id;
const lifecycleEvaluate = await evaluateScenario(lifecycleScenarioId, lifecycleFlowCreate.json?.version ?? 1);
const lifecycleSubmit = await transitionScenario(
  lifecycleScenarioId,
  lifecycleEvaluate.json?.scenario?.version ?? lifecycleEvaluate.json?.version,
  "submitted-for-approval",
  null,
);
const lifecycleApprove = await transitionScenario(
  lifecycleScenarioId,
  lifecycleSubmit.json?.scenario?.version ?? lifecycleSubmit.json?.version,
  "approved",
  null,
  readonlyHeaders,
);
assert("reviewer approve 200", lifecycleApprove.status === 200, lifecycleApprove.text?.slice(0, 200));
assert("approved lifecycle", lifecycleApprove.json?.scenario?.lifecycleStatus === "approved");

const readonlyEvaluate = await evaluateScenario(lifecycleScenarioId, 1, readonlyHeaders);
assert("readonly evaluate 403", readonlyEvaluate.status === 403, `status=${readonlyEvaluate.status}`);

const taskFlowCreate = await createScenario(SALES_WITHIN, 80, "task-idempotency");
const taskScenarioId = taskFlowCreate.json?.entityId ?? taskFlowCreate.json?.scenario?.id;
const taskEvaluate = await evaluateScenario(taskScenarioId, taskFlowCreate.json?.version ?? 1);
assert("task flow evaluate 200", taskEvaluate.status === 200);

const tasksBeforeDraft = taskCountForScenario(taskScenarioId);
const firstTaskDraft = await liveRequest(
  "POST",
  `/api/commercial-intelligence/pricing-scenarios/${taskScenarioId}/task-draft?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("task draft create 200", firstTaskDraft.status === 200, firstTaskDraft.text?.slice(0, 200));
const taskId = firstTaskDraft.json?.taskId;
assert("task draft returned task id", Boolean(taskId));

const replayTaskDraft = await liveRequest(
  "POST",
  `/api/commercial-intelligence/pricing-scenarios/${taskScenarioId}/task-draft?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("task draft replay 200", replayTaskDraft.status === 200, replayTaskDraft.text?.slice(0, 200));
assert("task draft replay flag", replayTaskDraft.json?.idempotentReplay === true);
assert("task draft same id", replayTaskDraft.json?.taskId === taskId);

const tasksAfterDraft = taskCountForScenario(taskScenarioId);
assert("task draft created one task", tasksAfterDraft === tasksBeforeDraft + 1, `${tasksBeforeDraft}→${tasksAfterDraft}`);
assert("task draft replay created no extra task", tasksAfterDraft === tasksBeforeDraft + 1);

const snapshotAfterReplay = snapshotFingerprint(passesScenarioId);
assert("evaluation snapshot immutable", snapshotAfterReplay === firstSnapshotFingerprint, `${firstSnapshotFingerprint}→${snapshotAfterReplay}`);
assert("passes scenario snapshot count", snapshotCount(passesScenarioId) === 1, `count=${snapshotCount(passesScenarioId)}`);

removeClosureFixtures();
const closureScenarios = scenarioCount();
assert("closure scenarios removed", closureScenarios === 0, `count=${closureScenarios}`);

console.log("COMMERCIAL-PROFITABILITY-7 security/idempotency verification passed.");

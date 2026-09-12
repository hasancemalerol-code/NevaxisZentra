#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "COMMPROFIT6-CLOSURE";
const RUN_SUFFIX = Date.now();

const PILOT_CATEGORY_SOURCES = {
  "product-contribution-deterioration": "bb000001-03b1-4000-8000-0000000003b1",
  "customer-contribution-deterioration": "bb000001-03b6-4000-8000-0000000003b6",
  "material-contribution-deterioration": "bb000001-03b7-4000-8000-0000000003b7",
  "cost-mix-deterioration": "bb000001-03b4-4000-8000-0000000003b4",
  "data-review": "bb000001-03bb-4000-8000-0000000003bb",
};

const DISMISS_ALERT_SOURCE = "bb000001-03b9-4000-8000-0000000003b9";
const TASK_ALERT_SOURCE = "bb000001-03b1-4000-8000-0000000003b1";

const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-6 security/idempotency verification failed: ${message}`);
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
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
    "X-NevaxisFactOS-Tenant": actor.tenant,
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
      SELECT linked_ai_task_id FROM commercial_profitability_alerts
      WHERE linked_ai_task_id IS NOT NULL
        AND source_comparison_id IN (
          '${TASK_ALERT_SOURCE}',
          '${DISMISS_ALERT_SOURCE}'
        )
    );
    DELETE FROM ai_tasks
    WHERE id IN (
      SELECT linked_ai_task_id FROM commercial_profitability_alerts
      WHERE linked_ai_task_id IS NOT NULL
        AND source_comparison_id = '${TASK_ALERT_SOURCE}'
    );
    DELETE FROM commercial_profitability_alert_audit_events
    WHERE alert_id IN (
      SELECT id FROM commercial_profitability_alerts
      WHERE source_comparison_id IN ('${TASK_ALERT_SOURCE}', '${DISMISS_ALERT_SOURCE}')
    );
    DELETE FROM commercial_profitability_alerts
    WHERE source_comparison_id IN ('${TASK_ALERT_SOURCE}', '${DISMISS_ALERT_SOURCE}');
  `);
}

function alertCount() {
  return Number(db("SELECT COUNT(*) FROM commercial_profitability_alerts"));
}

function auditCount() {
  return Number(db("SELECT COUNT(*) FROM commercial_profitability_alert_audit_events"));
}

function taskCountForAlertSource(sourceComparisonId) {
  return Number(db(`
    SELECT COUNT(*) FROM ai_tasks
    WHERE id IN (
      SELECT linked_ai_task_id FROM commercial_profitability_alerts
      WHERE source_comparison_id = '${sourceComparisonId}' AND linked_ai_task_id IS NOT NULL
    )
  `));
}

function duplicateActiveAlertCount() {
  return Number(db(`
    SELECT COUNT(*) FROM (
      SELECT tenant_id, facility_id, source_comparison_id, alert_category, COUNT(*) AS c
      FROM commercial_profitability_alerts
      WHERE lifecycle_status = 'active'
      GROUP BY tenant_id, facility_id, source_comparison_id, alert_category
      HAVING COUNT(*) > 1
    ) dup
  `));
}

async function materialize(headers = actorHeaders(MANAGER)) {
  return liveRequest(
    "POST",
    `/api/commercial-intelligence/profitability/alerts/materialize?facilityId=${FACILITY}`,
    { idempotencyKey: `${CLOSURE_PREFIX}-materialize-${RUN_SUFFIX}` },
    headers,
  );
}

async function findAlertIdBySourceComparison(sourceComparisonId) {
  const list = await liveRequest(
    "GET",
    `/api/commercial-intelligence/profitability/alerts?facilityId=${FACILITY}&pageSize=100`,
    null,
  );
  const match = list.json?.items?.find((item) => item.sourceComparisonId === sourceComparisonId);
  return match?.id ?? null;
}

console.log("Running COMMERCIAL-PROFITABILITY-6 security/idempotency verifier.");

const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialProfitabilityAlertEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/CommercialIntelligence/CommercialIntelligenceEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260828122926_AddCommercialProfitabilityAlertsFoundation.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialIntelligencePermissionKeys.cs");
const lifecycle = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityAlertLifecycle.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");
const comparisonSeed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilityComparisonSeed.cs");

assert("alert endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("workspace endpoint", endpoints.includes('MapGet("/workspace"'));
assert("list endpoint", endpoints.includes('MapGet("/", ListAlertsAsync'));
assert("detail endpoint", endpoints.includes('MapGet("/{alertId:guid}"'));
assert("materialize endpoint", endpoints.includes('MapPost("/materialize"'));
assert("transition endpoint", endpoints.includes('MapPost("/{alertId:guid}/transitions"'));
assert("task draft endpoint", endpoints.includes('MapPost("/{alertId:guid}/task-draft"'));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("manage access helper", helpers.includes("ResolveManageAccessAsync"));
assert("commercial-intelligence.read key", permissions.includes("commercial-intelligence.read"));
assert("commercial-intelligence.manage key", permissions.includes("commercial-intelligence.manage"));
assert("dismissal note validation", lifecycle.includes("ValidateTransitionNote"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("alerts update grant", migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE commercial_profitability_alerts"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_profitability_alert_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE commercial_profitability_alerts", "alerts table");
forbidText(migration, "GRANT DELETE ON TABLE commercial_profitability_alert_audit_events", "audit table");
forbidText(endpoints, "MapDelete", "profitability alert endpoints");
assert("bootstrap comparison seed", bootstrap.includes("EnsureCommercialProfitabilityComparisonPilotRecordsAsync"));
assert("comparison seed deterioration scenario", comparisonSeed.includes("commprofit2-pilot-deterioration"));
assert("comparison seed source conflict", comparisonSeed.includes("commprofit2-pilot-source-conflict"));

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(READONLY);
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const alertsBeforeDeny = alertCount();
const auditBeforeDeny = auditCount();

const noAuth = await fetch(`${BASE}/api/commercial-intelligence/profitability/alerts/workspace?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyMaterialize = await materialize(readonlyHeaders);
assert("readonly materialize 403", readonlyMaterialize.status === 403, `status=${readonlyMaterialize.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts?facilityId=${UNAUTH_FACILITY}`,
  null,
  managerHeaders,
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/workspace?facilityId=${FACILITY}`,
  null,
  crossTenantHeaders,
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const alertsAfterDeny = alertCount();
const auditAfterDeny = auditCount();
assert("denied requests created no alerts", alertsAfterDeny === alertsBeforeDeny, `${alertsBeforeDeny}→${alertsAfterDeny}`);
assert("denied requests created no audit rows", auditAfterDeny === auditBeforeDeny, `${auditBeforeDeny}→${auditAfterDeny}`);

const workspace = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/workspace?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("authorized workspace 200", workspace.status === 200, workspace.text?.slice(0, 200));
assert("workspace capabilities", workspace.json?.capabilities?.canRead === true);
assert("automation disclaimer", workspace.json?.automationDisclaimer === "Otomatik ticari işlem yapılmaz.");

const firstMaterialize = await materialize(managerHeaders);
assert("authorized materialize 200", firstMaterialize.status === 200, firstMaterialize.text?.slice(0, 200));
forbidText(firstMaterialize.text ?? "", "Npgsql", "materialize response");
forbidText(firstMaterialize.text ?? "", "password", "materialize response");
assert("materialize created alerts", (firstMaterialize.json?.materialization?.createdCount ?? 0) >= 1);

const replayMaterialize = await materialize(managerHeaders);
assert("materialize replay 200", replayMaterialize.status === 200, replayMaterialize.text?.slice(0, 200));
assert(
  "materialize replay skips duplicates",
  (replayMaterialize.json?.materialization?.skippedCount ?? 0) >= (firstMaterialize.json?.materialization?.createdCount ?? 1),
  JSON.stringify(replayMaterialize.json?.materialization),
);
assert("materialize replay created zero", replayMaterialize.json?.materialization?.createdCount === 0);

const alertsAfterMaterialize = alertCount();
const countAfterFirst = alertsBeforeDeny + (firstMaterialize.json?.materialization?.createdCount ?? 0);
assert("alert row count stable after replay", alertsAfterMaterialize === countAfterFirst, `${countAfterFirst} vs ${alertsAfterMaterialize}`);
assert("no duplicate active alerts", duplicateActiveAlertCount() === 0, `count=${duplicateActiveAlertCount()}`);

for (const [category, sourceComparisonId] of Object.entries(PILOT_CATEGORY_SOURCES)) {
  const alertId = await findAlertIdBySourceComparison(sourceComparisonId);
  assert(`pilot category ${category}`, Boolean(alertId), sourceComparisonId);
  const detail = await liveRequest(
    "GET",
    `/api/commercial-intelligence/profitability/alerts/${alertId}?facilityId=${FACILITY}`,
    null,
    managerHeaders,
  );
  assert(`${category} detail 200`, detail.status === 200, detail.text?.slice(0, 200));
  assert(`${category} category matches`, detail.json?.alertCategory === category, detail.json?.alertCategory);
}

const dismissAlertId = await findAlertIdBySourceComparison(DISMISS_ALERT_SOURCE);
assert("dismiss candidate alert present", Boolean(dismissAlertId));
const dismissDetail = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/${dismissAlertId}?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("dismiss candidate detail 200", dismissDetail.status === 200);

const dismissWithoutNote = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/alerts/${dismissAlertId}/transitions?facilityId=${FACILITY}`,
  { expectedVersion: dismissDetail.json.version, targetLifecycleStatus: "dismissed" },
  managerHeaders,
);
assert("dismiss without note 400", dismissWithoutNote.status === 400, `status=${dismissWithoutNote.status}`);

const dismissWithNote = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/alerts/${dismissAlertId}/transitions?facilityId=${FACILITY}`,
  {
    expectedVersion: dismissDetail.json.version,
    targetLifecycleStatus: "dismissed",
    note: "Kapanış doğrulaması için reddetme gerekçesi",
  },
  managerHeaders,
);
assert("dismiss with note 200", dismissWithNote.status === 200, dismissWithNote.text?.slice(0, 200));
assert("dismissed lifecycle", dismissWithNote.json?.alert?.lifecycleStatus === "dismissed");

const taskAlertId = await findAlertIdBySourceComparison(TASK_ALERT_SOURCE);
assert("task draft candidate alert present", Boolean(taskAlertId));
const taskAlertDetail = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/alerts/${taskAlertId}?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("task alert detail 200", taskAlertDetail.status === 200);

const tasksBeforeDraft = taskCountForAlertSource(TASK_ALERT_SOURCE);
const firstTaskDraft = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/alerts/${taskAlertId}/task-draft?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("task draft create 200", firstTaskDraft.status === 200, firstTaskDraft.text?.slice(0, 200));
const taskId = firstTaskDraft.json?.taskId;
assert("task draft returned task id", Boolean(taskId));
assert("alert linked task-created", firstTaskDraft.json?.alert?.lifecycleStatus === "task-created");
assert("alert linked ai task id", firstTaskDraft.json?.alert?.linkedAiTaskId === taskId);

const replayTaskDraft = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/alerts/${taskAlertId}/task-draft?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("task draft replay 200", replayTaskDraft.status === 200, replayTaskDraft.text?.slice(0, 200));
assert("task draft replay flag", replayTaskDraft.json?.idempotentReplay === true);
assert("task draft same id", replayTaskDraft.json?.taskId === taskId);

const tasksAfterDraft = taskCountForAlertSource(TASK_ALERT_SOURCE);
assert("task draft created one task", tasksAfterDraft === tasksBeforeDraft + 1, `${tasksBeforeDraft}→${tasksAfterDraft}`);
assert("task draft replay created no extra task", tasksAfterDraft === tasksBeforeDraft + 1);

const readonlyTransition = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/alerts/${taskAlertId}/transitions?facilityId=${FACILITY}`,
  { expectedVersion: replayTaskDraft.json?.alert?.version ?? 2, targetLifecycleStatus: "resolved" },
  readonlyHeaders,
);
assert("readonly transition 403", readonlyTransition.status === 403, `status=${readonlyTransition.status}`);

const auditCountForDismiss = Number(db(`
  SELECT COUNT(*) FROM commercial_profitability_alert_audit_events WHERE alert_id = '${dismissAlertId}'
`));
assert("dismiss audit events appended", auditCountForDismiss >= 2, `count=${auditCountForDismiss}`);

removeClosureFixtures();
const closureAlerts = Number(db(`
  SELECT COUNT(*) FROM commercial_profitability_alerts
  WHERE source_comparison_id IN ('${TASK_ALERT_SOURCE}', '${DISMISS_ALERT_SOURCE}')
`));
assert("closure alerts removed", closureAlerts === 0, `count=${closureAlerts}`);

console.log("COMMERCIAL-PROFITABILITY-6 security/idempotency verification passed.");

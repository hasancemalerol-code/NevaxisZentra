#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MIGRATION_ID = "20260828122926_AddCommercialProfitabilityAlertsFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const CLOSURE_PREFIX = "COMMPROFIT6-CLOSURE";
const PROFILE = "bb000001-0310-4000-8000-000000000310";

const PILOT_RUN_IDS = [
  "bb000001-03b0-4000-8000-0000000003b0",
  "bb000001-03b1-4000-8000-0000000003b1",
  "bb000001-03b2-4000-8000-0000000003b2",
  "bb000001-03b3-4000-8000-0000000003b3",
  "bb000001-03b4-4000-8000-0000000003b4",
  "bb000001-03b5-4000-8000-0000000003b5",
  "bb000001-03b6-4000-8000-0000000003b6",
  "bb000001-03b7-4000-8000-0000000003b7",
  "bb000001-03b8-4000-8000-0000000003b8",
  "bb000001-03b9-4000-8000-0000000003b9",
  "bb000001-03ba-4000-8000-0000000003ba",
  "bb000001-03bb-4000-8000-0000000003bb",
];

const RLS_TABLES = [
  "commercial_profitability_alerts",
  "commercial_profitability_alert_audit_events",
];

const ALERTS_TABLE = "commercial_profitability_alerts";
const AUDIT_TABLE = "commercial_profitability_alert_audit_events";

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-6 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function tablePrivileges(grantee, tableName) {
  return db(`
    SELECT COALESCE(string_agg(DISTINCT privilege_type, ',' ORDER BY privilege_type), '')
    FROM information_schema.table_privileges
    WHERE grantee = '${grantee}' AND table_name = '${tableName}'
  `);
}

function readMigrationConnectionEnv() {
  const appsettingsPath = resolve(ROOT, "apps/api/appsettings.Development.json");
  const appsettings = JSON.parse(readFileSync(appsettingsPath, "utf8"));
  const persistence = appsettings.Persistence ?? {};
  return {
    ...process.env,
    CREVAFACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    CREVAFACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5432),
    CREVAFACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    CREVAFACTOS__MIGRATIONS__USERNAME: "crevafactos",
    CREVAFACTOS__MIGRATIONS__PASSWORD: process.env.CREVAFACTOS__MIGRATIONS__PASSWORD
      ?? process.env.CREVAFACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    CREVAFACTOS__MIGRATIONS__SSL_MODE: "Disable",
    CREVAFACTOS__MIGRATIONS__CONNECTION_TIMEOUT_SECONDS: "15",
    CREVAFACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

function runMigrationApply() {
  const migrationProject = resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj");
  return spawnSync(
    "dotnet",
    [
      "tool",
      "run",
      "dotnet-ef",
      "--",
      "database",
      "update",
      "--project",
      migrationProject,
      "--startup-project",
      migrationProject,
      "--context",
      "CrevaFactOSDbContext",
    ],
    {
      cwd: ROOT,
      encoding: "utf8",
      env: readMigrationConnectionEnv(),
    },
  );
}

function removeClosureFixtures() {
  db(`
    DELETE FROM commercial_profitability_alert_audit_events
    WHERE alert_id IN (
      SELECT id FROM commercial_profitability_alerts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_profitability_alerts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

function pilotSnapshot() {
  const runIdList = PILOT_RUN_IDS.map((id) => `'${id}'`).join(", ");
  return {
    profileCount: Number(db(`SELECT COUNT(*) FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`)),
    pilotRunCount: Number(db(`SELECT COUNT(*) FROM commercial_profitability_comparison_runs WHERE id IN (${runIdList})`)),
    pilotResultCount: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_comparison_results
      WHERE comparison_run_id IN (${runIdList})
    `)),
    pilotDriverCount: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_comparison_drivers
      WHERE comparison_run_id IN (${runIdList})
    `)),
    duplicateActiveAlerts: Number(db(`
      SELECT COUNT(*) FROM (
        SELECT tenant_id, facility_id, source_comparison_id, alert_category, COUNT(*) AS c
        FROM commercial_profitability_alerts
        WHERE lifecycle_status = 'active'
        GROUP BY tenant_id, facility_id, source_comparison_id, alert_category
        HAVING COUNT(*) > 1
      ) dup
    `)),
    duplicateIdempotency: Number(db(`
      SELECT COUNT(*) FROM (
        SELECT tenant_id, idempotency_key, COUNT(*) AS c
        FROM commercial_profitability_alerts
        WHERE idempotency_key IS NOT NULL
        GROUP BY tenant_id, idempotency_key
        HAVING COUNT(*) > 1
      ) dup
    `)),
  };
}

function assertPilotStable(baseline, current, label) {
  assert(`${label} pilot profile count stable`, current.profileCount === baseline.profileCount, `${baseline.profileCount}→${current.profileCount}`);
  assert(`${label} pilot run count stable`, current.pilotRunCount === baseline.pilotRunCount, `${baseline.pilotRunCount}→${current.pilotRunCount}`);
  assert(`${label} pilot result count stable`, current.pilotResultCount === baseline.pilotResultCount, `${baseline.pilotResultCount}→${current.pilotResultCount}`);
  assert(`${label} pilot driver count stable`, current.pilotDriverCount === baseline.pilotDriverCount, `${baseline.pilotDriverCount}→${current.pilotDriverCount}`);
  assert(`${label} no duplicate active alerts`, current.duplicateActiveAlerts === 0, `count=${current.duplicateActiveAlerts}`);
  assert(`${label} no duplicate idempotency rows`, current.duplicateIdempotency === 0, `count=${current.duplicateIdempotency}`);
}

function getListeningPid(port) {
  const result = spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess`,
  ], { encoding: "utf8" });
  const pid = Number(result.stdout.trim());
  return Number.isFinite(pid) && pid > 0 ? pid : null;
}

function stopApi() {
  const pid = getListeningPid(5000);
  if (!pid) return;
  spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Stop-Process -Id ${pid} -Force -ErrorAction SilentlyContinue`,
  ], { encoding: "utf8" });
}

function startApi() {
  spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Start-Process -WindowStyle Hidden -WorkingDirectory '${ROOT}' -FilePath 'dotnet' -ArgumentList 'run','--project','apps/api/CrevaFactOS.Api.csproj'`,
  ], { encoding: "utf8" });
}

async function waitForApiReady(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${BASE}/health/ready`);
      if (response.status === 200) return true;
    } catch {
      // retry
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 500));
  }
  return false;
}

async function restartApiCycle(label) {
  stopApi();
  await new Promise((resolveDelay) => setTimeout(resolveDelay, 2000));
  startApi();
  const ready = await waitForApiReady();
  assert(`${label} API ready`, ready, `base=${BASE}`);
}

console.log("Running COMMERCIAL-PROFITABILITY-6 migration verifier.");

const migration = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/CommercialIntelligence/CommercialProfitabilityAlertConstants.cs");
const comparisonSeed = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilityComparisonSeed.cs");

assert("migration source present", migration.includes("AddCommercialProfitabilityAlertsFoundation"));
assert("designer present", designer.includes("AddCommercialProfitabilityAlertsFoundation"));
assert("snapshot includes alerts table", snapshot.includes('ToTable("commercial_profitability_alerts"'));
assert("snapshot includes alert audit table", snapshot.includes('ToTable("commercial_profitability_alert_audit_events"'));
assert("rule set version constant", constants.includes('COMMERCIAL-PROFITABILITY-6.0'));
assert("five alert categories", constants.includes("product-contribution-deterioration")
  && constants.includes("customer-contribution-deterioration")
  && constants.includes("material-contribution-deterioration")
  && constants.includes("cost-mix-deterioration")
  && constants.includes("data-review"));
assert("comparison seed twelve scenarios", comparisonSeed.includes("commprofit2-pilot-source-conflict"));
assert("active comparison category unique index", migration.includes("ux_commprofit6_alerts_active_comparison_category"));
assert("tenant idempotency unique index", migration.includes("ux_commprofit6_alerts_tenant_idempotency"));
assert("source comparison FK restrict", migration.includes("onDelete: ReferentialAction.Restrict"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row present", historyCount === 1, `count=${historyCount}`);

const categoryConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commprofit6_alerts_category'
`);
assert(
  "alert category check constraint",
  categoryConstraint.includes("product-contribution-deterioration")
    && categoryConstraint.includes("data-review"),
  categoryConstraint,
);

const severityConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commprofit6_alerts_severity'
`);
assert(
  "alert severity check constraint",
  severityConstraint.includes("'critical'") && severityConstraint.includes("'low'"),
  severityConstraint,
);

const lifecycleConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commprofit6_alerts_lifecycle'
`);
assert(
  "alert lifecycle check constraint",
  lifecycleConstraint.includes("'task-created'") && lifecycleConstraint.includes("'dismissed'"),
  lifecycleConstraint,
);

const alertsPrivileges = tablePrivileges("crevafactos_runtime", ALERTS_TABLE);
forbidText(alertsPrivileges, "DELETE", `${ALERTS_TABLE} runtime privileges`);
assert(`${ALERTS_TABLE} SELECT grant`, alertsPrivileges.includes("SELECT"), alertsPrivileges);
assert(`${ALERTS_TABLE} INSERT grant`, alertsPrivileges.includes("INSERT"), alertsPrivileges);
assert(`${ALERTS_TABLE} UPDATE grant`, alertsPrivileges.includes("UPDATE"), alertsPrivileges);

const auditPrivileges = tablePrivileges("crevafactos_runtime", AUDIT_TABLE);
forbidText(auditPrivileges, "DELETE", `${AUDIT_TABLE} runtime privileges`);
forbidText(auditPrivileges, "UPDATE", `${AUDIT_TABLE} runtime privileges`);
assert(`${AUDIT_TABLE} SELECT grant`, auditPrivileges.includes("SELECT"), auditPrivileges);
assert(`${AUDIT_TABLE} INSERT grant`, auditPrivileges.includes("INSERT"), auditPrivileges);

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

const orphanAuditEvents = Number(db(`
  SELECT COUNT(*) FROM commercial_profitability_alert_audit_events ae
  LEFT JOIN commercial_profitability_alerts alert ON alert.id = ae.alert_id
  WHERE alert.id IS NULL
`));
const orphanAlerts = Number(db(`
  SELECT COUNT(*) FROM commercial_profitability_alerts alert
  LEFT JOIN commercial_profitability_comparison_runs run ON run.id = alert.source_comparison_id
  WHERE run.id IS NULL
`));
assert("no orphan alert audit events", orphanAuditEvents === 0, `count=${orphanAuditEvents}`);
assert("no orphan alerts", orphanAlerts === 0, `count=${orphanAlerts}`);

const duplicateActiveAlerts = Number(db(`
  SELECT COUNT(*) FROM (
    SELECT tenant_id, facility_id, source_comparison_id, alert_category, COUNT(*) AS c
    FROM commercial_profitability_alerts
    WHERE lifecycle_status = 'active'
    GROUP BY tenant_id, facility_id, source_comparison_id, alert_category
    HAVING COUNT(*) > 1
  ) dup
`));
assert("no duplicate active alerts", duplicateActiveAlerts === 0, `count=${duplicateActiveAlerts}`);

const duplicateIdempotency = Number(db(`
  SELECT COUNT(*) FROM (
    SELECT tenant_id, idempotency_key, COUNT(*) AS c
    FROM commercial_profitability_alerts
    WHERE idempotency_key IS NOT NULL
    GROUP BY tenant_id, idempotency_key
    HAVING COUNT(*) > 1
  ) dup
`));
assert("no duplicate idempotency rows", duplicateIdempotency === 0, `count=${duplicateIdempotency}`);

const historyBefore = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
const applyResult = runMigrationApply();
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("second migration apply succeeds", applyResult.status === 0, applyOutput.slice(-500));
assert(
  "second migration apply is no-op",
  /No migrations were applied|already up to date|Done\./i.test(applyOutput),
  applyOutput.slice(-500),
);
const historyAfter = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("second apply did not duplicate history", historyAfter === historyBefore, `${historyBefore}→${historyAfter}`);

const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`));
if (pilotProfileCount === 0) {
  fail("Commercial profitability pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);

const baselinePilot = pilotSnapshot();
assert("twelve pilot comparison runs", baselinePilot.pilotRunCount === 12, `count=${baselinePilot.pilotRunCount}`);
assert("pilot comparison results seeded", baselinePilot.pilotResultCount >= 12, `count=${baselinePilot.pilotResultCount}`);
assert("pilot comparison drivers seeded", baselinePilot.pilotDriverCount >= 12, `count=${baselinePilot.pilotDriverCount}`);

const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot pilot preserved", iotCount === 1, `count=${iotCount}`);

try {
  const initiallyReady = await waitForApiReady(5000);
  if (initiallyReady) {
    await restartApiCycle("first");
    assertPilotStable(baselinePilot, pilotSnapshot(), "after first restart");
    await restartApiCycle("second");
    assertPilotStable(baselinePilot, pilotSnapshot(), "after second restart");
  } else {
    fail("API not ready for pilot seed stability checks — start API with DevelopmentPilot enabled on port 5000");
  }
} finally {
  removeClosureFixtures();
  const closureAlerts = Number(db(`SELECT COUNT(*) FROM commercial_profitability_alerts WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  assert("closure alerts removed", closureAlerts === 0, `count=${closureAlerts}`);
}

console.log("COMMERCIAL-PROFITABILITY-6 migration verification passed.");

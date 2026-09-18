#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260821210657_AddIotPlcIndustrialConnectivityFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const IOT_PLC_READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000078";
const IOT_PLC_MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000079";
const PILOT_ENDPOINT_OPC = "bb000001-01c0-4000-8000-0000000001c0";

const RLS_TABLES = [
  "iot_plc_endpoints",
  "iot_plc_signals",
  "iot_plc_signal_observations",
  "iot_plc_connection_health_observations",
  "iot_plc_audit_events",
];

const APPEND_ONLY_TABLES = [
  "iot_plc_signal_observations",
  "iot_plc_connection_health_observations",
  "iot_plc_audit_events",
];

function fail(message) {
  console.error(`IOT-PLC-1 migration verification failed: ${message}`);
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
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
    NEVAXIS_FACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    NEVAXIS_FACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5432),
    NEVAXIS_FACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    NEVAXIS_FACTOS__MIGRATIONS__USERNAME: "crevafactos",
    NEVAXIS_FACTOS__MIGRATIONS__PASSWORD: process.env.NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
      ?? process.env.NEVAXIS_FACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE: "Disable",
    NEVAXIS_FACTOS__MIGRATIONS__CONNECTION_TIMEOUT_SECONDS: "15",
    NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

function runMigrationApply() {
  const migrationProject = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj");
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
      "NevaxisZentraDbContext",
    ],
    {
      cwd: ROOT,
      encoding: "utf8",
      env: readMigrationConnectionEnv(),
    },
  );
}

console.log("Running IOT-PLC-1 migration verifier.");

const migration = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotIotPlcSeed.cs");

assert("migration source present", migration.includes("AddIotPlcIndustrialConnectivityFoundation"));
assert("designer present", designer.includes("AddIotPlcIndustrialConnectivityFoundation"));
assert("snapshot includes endpoints", snapshot.includes('ToTable("iot_plc_endpoints"'));
assert("snapshot includes signal observations", snapshot.includes('ToTable("iot_plc_signal_observations"'));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const migrationHistoryCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("migration history row exactly one", migrationHistoryCount === 1, `count=${migrationHistoryCount}`);

const requiredIndexes = [
  "ux_iot_plc_endpoints_tenant_facility_code",
  "ux_iot_plc_signals_endpoint_code",
  "ux_iot_plc_signal_observations_tenant_idempotency",
  "ux_iot_plc_signal_observations_signal_source_ref",
  "ux_iot_plc_signal_observations_signal_observed_at",
  "ux_iot_plc_connection_health_tenant_idempotency",
];
for (const indexName of requiredIndexes) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_indexes WHERE indexname = '${indexName}'`));
  assert(`index ${indexName}`, count === 1, `count=${count}`);
}

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
}

const endpointsPrivileges = tablePrivileges("crevafactos_runtime", "iot_plc_endpoints");
assert("endpoints SELECT grant", endpointsPrivileges.includes("SELECT"), endpointsPrivileges);
assert("endpoints INSERT grant", endpointsPrivileges.includes("INSERT"), endpointsPrivileges);
assert("endpoints UPDATE grant", endpointsPrivileges.includes("UPDATE"), endpointsPrivileges);
forbidText(endpointsPrivileges, "DELETE", "endpoints runtime privileges");

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

const readPermission = db(`SELECT key FROM permission_definitions WHERE id = '${IOT_PLC_READ_PERMISSION_ID}'`);
const managePermission = db(`SELECT key FROM permission_definitions WHERE id = '${IOT_PLC_MANAGE_PERMISSION_ID}'`);
assert("iot-plc.read permission seeded", readPermission === "iot-plc.read", readPermission);
assert("iot-plc.manage permission seeded", managePermission === "iot-plc.manage", managePermission);

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

assert("bootstrap seed references pilot endpoint", bootstrap.includes("IotPlcPilotEndpointOpcId"));
const pilotEndpointCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = '${PILOT_ENDPOINT_OPC}'`));
if (pilotEndpointCount === 0) {
  fail("IoT PLC pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
const pilotEndpointTotal = Number(db(`
  SELECT COUNT(*) FROM iot_plc_endpoints
  WHERE id IN (
    'bb000001-01c0-4000-8000-0000000001c0',
    'bb000001-01c1-4000-8000-0000000001c1',
    'bb000001-01c2-4000-8000-0000000001c2'
  )
`));
assert("bootstrap pilot endpoints = 3", pilotEndpointTotal === 3, `count=${pilotEndpointTotal}`);

const duplicateRestartEndpoints = Number(db(`
  SELECT COUNT(*) FROM iot_plc_endpoints
  WHERE endpoint_code = 'EP-OPC-INJ-LINE'
`));
assert("restart duplicate endpoints = 1", duplicateRestartEndpoints === 1, `count=${duplicateRestartEndpoints}`);

console.log("IOT-PLC-1 migration verification passed.");

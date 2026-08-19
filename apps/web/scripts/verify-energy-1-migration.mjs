#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260819140015_AddEnergyMeasurementFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const ENERGY_READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000076";
const ENERGY_MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000077";
const PILOT_POINT_INJ1 = "bb000001-0160-4000-8000-000000000160";

const RLS_TABLES = [
  "energy_measurement_points",
  "energy_tariff_snapshots",
  "energy_readings",
  "energy_consumption_windows",
  "energy_allocations",
  "energy_audit_events",
];

const APPEND_ONLY_TABLES = [
  "energy_readings",
  "energy_consumption_windows",
  "energy_allocations",
  "energy_audit_events",
];

function fail(message) {
  console.error(`ENERGY-1 migration verification failed: ${message}`);
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

console.log("Running ENERGY-1 migration verifier.");

const migration = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotEnergySeed.cs");

assert("migration source present", migration.includes("AddEnergyMeasurementFoundation"));
assert("designer present", designer.includes("AddEnergyMeasurementFoundation"));
assert("snapshot includes measurement points", snapshot.includes('ToTable("energy_measurement_points"'));
assert("snapshot includes consumption windows", snapshot.includes('ToTable("energy_consumption_windows"'));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const migrationDiscovered = migration.includes("AddEnergyMeasurementFoundation");
assert("migration discovered exactly once", migrationDiscovered);

const migrationFileCount = Number(db(`
  SELECT COUNT(*) FROM pg_catalog.pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
  WHERE n.nspname = 'public' AND c.relname = 'energy_measurement_points'
`));
assert("energy tables present after apply", migrationFileCount === 1);

const migrationHistoryCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("migration history row exactly one", migrationHistoryCount === 1, `count=${migrationHistoryCount}`);

const requiredIndexes = [
  "ux_energy_measurement_points_tenant_facility_code",
  "ux_energy_readings_tenant_idempotency",
  "ux_energy_consumption_windows_point_window_ruleset",
  "ux_energy_consumption_windows_tenant_evaluate_idempotency",
];
for (const indexName of requiredIndexes) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_indexes WHERE indexname = '${indexName}'`));
  assert(`index ${indexName}`, count === 1, `count=${count}`);
}

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
}

const pointsPrivileges = tablePrivileges("crevafactos_runtime", "energy_measurement_points");
assert("measurement points SELECT grant", pointsPrivileges.includes("SELECT"), pointsPrivileges);
assert("measurement points INSERT grant", pointsPrivileges.includes("INSERT"), pointsPrivileges);
assert("measurement points UPDATE grant", pointsPrivileges.includes("UPDATE"), pointsPrivileges);
forbidText(pointsPrivileges, "DELETE", "measurement points runtime privileges");

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

const readPermission = db(`SELECT key FROM permission_definitions WHERE id = '${ENERGY_READ_PERMISSION_ID}'`);
const managePermission = db(`SELECT key FROM permission_definitions WHERE id = '${ENERGY_MANAGE_PERMISSION_ID}'`);
assert("energy.read permission seeded", readPermission === "energy.read", readPermission);
assert("energy.manage permission seeded", managePermission === "energy.manage", managePermission);

const orphanReadings = Number(db(`
  SELECT COUNT(*) FROM energy_readings r
  WHERE NOT EXISTS (
    SELECT 1 FROM energy_measurement_points p WHERE p.id = r.measurement_point_id
  )
`));
assert("no orphan energy readings", orphanReadings === 0, `count=${orphanReadings}`);

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

assert("bootstrap seed references pilot point", bootstrap.includes("EnergyPilotPointInj1Id"));
const pilotPointCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = '${PILOT_POINT_INJ1}'`));
if (pilotPointCount === 0) {
  fail("energy pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
const pilotPointTotal = Number(db(`
  SELECT COUNT(*) FROM energy_measurement_points
  WHERE id IN (
    'bb000001-0160-4000-8000-000000000160',
    'bb000001-0161-4000-8000-000000000161',
    'bb000001-0162-4000-8000-000000000162',
    'bb000001-0163-4000-8000-000000000163',
    'bb000001-0164-4000-8000-000000000164',
    'bb000001-0165-4000-8000-000000000165'
  )
`));
assert("bootstrap pilot measurement points = 6", pilotPointTotal === 6, `count=${pilotPointTotal}`);

const duplicateRestartPoints = Number(db(`
  SELECT COUNT(*) FROM energy_measurement_points
  WHERE point_code = 'EM-INJ-001'
`));
assert("restart duplicate points = 1", duplicateRestartPoints === 1, `count=${duplicateRestartPoints}`);

console.log("ENERGY-1 migration verification passed.");

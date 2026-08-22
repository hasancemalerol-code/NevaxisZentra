#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260821213240_AddIotPlcSignalQualityConnectivityIntelligence";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const PILOT_PROFILE = "bb000001-01e0-4000-8000-0000000001e0";

const RLS_TABLES = [
  "iot_plc_quality_profiles",
  "iot_plc_connectivity_assessments",
  "iot_plc_signal_quality_assessments",
  "iot_plc_intelligence_drivers",
];

const APPEND_ONLY_TABLES = [
  "iot_plc_connectivity_assessments",
  "iot_plc_signal_quality_assessments",
  "iot_plc_intelligence_drivers",
];

function fail(message) {
  console.error(`IOT-PLC-2 migration verification failed: ${message}`);
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
    ["tool", "run", "dotnet-ef", "--", "database", "update", "--project", migrationProject, "--startup-project", migrationProject, "--context", "CrevaFactOSDbContext"],
    { cwd: ROOT, encoding: "utf8", env: readMigrationConnectionEnv() },
  );
}

console.log("Running IOT-PLC-2 migration verifier.");

const migration = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const seed = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotIotPlcIntelligenceSeed.cs");

assert("migration source present", migration.includes("AddIotPlcSignalQualityConnectivityIntelligence"));
assert("designer present", designer.includes("AddIotPlcSignalQualityConnectivityIntelligence"));
assert("snapshot includes intelligence tables", snapshot.includes("iot_plc_quality_profiles"));
assert("RLS in migration", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("grants in migration", migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE iot_plc_quality_profiles"));
assert("bootstrap seed wired", readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs").includes("DevelopmentPilotIotPlcIntelligenceSeed"));
assert("seed idempotent profile", seed.includes("IgnoreQueryFilters"));

const countBefore = Number(db(`SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${MIGRATION_ID}'`));
const firstApply = runMigrationApply();
assert("first migration apply succeeds", firstApply.status === 0, firstApply.stderr || firstApply.stdout);
const countAfterFirst = Number(db(`SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${MIGRATION_ID}'`));
assert("migration appears exactly once after first apply", countAfterFirst === 1, `before=${countBefore} after=${countAfterFirst}`);

const secondApply = runMigrationApply();
assert("second migration apply succeeds", secondApply.status === 0, secondApply.stderr || secondApply.stdout);
const countAfterSecond = Number(db(`SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${MIGRATION_ID}'`));
assert("second apply is no-op", countAfterSecond === 1);

for (const table of RLS_TABLES) {
  const rls = db(`SELECT relrowsecurity, relforcerowsecurity FROM pg_class WHERE relname = '${table}'`);
  assert(`${table} RLS enabled`, rls.includes("t|t"), rls);
}

assert("profiles grants", tablePrivileges("crevafactos_runtime", "iot_plc_quality_profiles").includes("SELECT")
  && tablePrivileges("crevafactos_runtime", "iot_plc_quality_profiles").includes("INSERT")
  && tablePrivileges("crevafactos_runtime", "iot_plc_quality_profiles").includes("UPDATE"));

for (const table of APPEND_ONLY_TABLES) {
  const privs = tablePrivileges("crevafactos_runtime", table);
  assert(`${table} append-only grants`, privs.includes("SELECT") && privs.includes("INSERT") && !privs.includes("UPDATE"), privs);
  assert(`${table} no delete grant`, !privs.includes("DELETE"), privs);
}

const profileCount = Number(db(`SELECT COUNT(*) FROM iot_plc_quality_profiles WHERE id = '${PILOT_PROFILE}'`));
assert("bootstrap profile idempotent", profileCount === 1, `count=${profileCount}`);

console.log("IOT-PLC-2 migration verifier passed.");

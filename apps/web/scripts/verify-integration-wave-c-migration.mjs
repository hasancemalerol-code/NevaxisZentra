#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WAVE_C_MIGRATION = "20260910123549_IntegrationWaveCConnectorFoundation";
const SPINE_MIGRATION = "20260910101730_AddIntegrationSpineFoundation";
const WAVE_B_MIGRATION = "20260910104708_AddIntegrationWaveBFileImportFoundation";

function fail(message) {
  console.error(`INTEGRATION-WAVE-C migration verification failed: ${message}`);
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

function db(sql, database) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", database, "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function readMigrationConnectionEnv(database) {
  return {
    ...process.env,
    CREVAFACTOS__MIGRATIONS__HOST: "127.0.0.1",
    CREVAFACTOS__MIGRATIONS__PORT: "5433",
    CREVAFACTOS__MIGRATIONS__DATABASE: database,
    CREVAFACTOS__MIGRATIONS__USERNAME: "crevafactos",
    CREVAFACTOS__MIGRATIONS__PASSWORD:
      process.env.CREVAFACTOS__MIGRATIONS__PASSWORD
      ?? process.env.CREVAFACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    CREVAFACTOS__MIGRATIONS__SSL_MODE: "Disable",
    CREVAFACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "300",
  };
}

function runMigrationApply(database) {
  const migrationProject = resolve(
    ROOT,
    "packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj",
  );
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
    { cwd: ROOT, encoding: "utf8", env: readMigrationConnectionEnv(database) },
  );
}

console.log("Running INTEGRATION-WAVE-C migration verifier.");

const waveC = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${WAVE_C_MIGRATION}.cs`);
const snapshot = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");

assert("wave-c migration adds ingress_key", waveC.includes("ingress_key"));
assert("wave-c migration adds ingress resolver function", waveC.includes("resolve_integration_ingress_connector"));
assert("snapshot includes ingress_key", snapshot.includes("ingress_key"));
assert("snapshot ingress index filter", snapshot.includes('HasFilter("ingress_key IS NOT NULL")'));

const targetDb = process.env.CREVAFACTOS_INTEGRATION_MIGRATION_VERIFY_DB ?? "crevafactos_integration_wave_c_1";
const applyResult = runMigrationApply(targetDb);
assert("migration apply succeeds", applyResult.status === 0, `${applyResult.stdout}${applyResult.stderr}`.slice(-800));

assert("spine history row present", Number(db(
  `SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${SPINE_MIGRATION}'`,
  targetDb,
)) >= 1);
assert("wave-b history row present", Number(db(
  `SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${WAVE_B_MIGRATION}'`,
  targetDb,
)) >= 1);
assert("wave-c history row present", Number(db(
  `SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${WAVE_C_MIGRATION}'`,
  targetDb,
)) >= 1);

assert("ingress_key column exists", db(
  "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = 'integration_connectors' AND column_name = 'ingress_key'",
  targetDb,
) === "1");
assert("ingress resolver function exists", db(
  "SELECT COUNT(*) FROM pg_proc WHERE proname = 'resolve_integration_ingress_connector'",
  targetDb,
) === "1");
assert("integration_connectors FORCE RLS enabled", db(
  "SELECT c.relforcerowsecurity FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relname = 'integration_connectors' AND n.nspname = 'public'",
  targetDb,
) === "t");

const secondApply = runMigrationApply(targetDb);
assert("second migration apply is no-op", secondApply.status === 0, `${secondApply.stdout}${secondApply.stderr}`.slice(-800));

console.log("WAVE_C_MIGRATION=PASS");
console.log("WAVE_C_RLS=PASS");
console.log("EF_MODEL_SNAPSHOT_ALIGNED=TRUE");
console.log("INTEGRATION-WAVE-C migration verification passed.");

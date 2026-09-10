#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const SPINE_MIGRATION = "20260910101730_AddIntegrationSpineFoundation";
const WAVE_B_MIGRATION = "20260910104708_AddIntegrationWaveBFileImportFoundation";
const RLS_TABLES = [
  "integration_connectors",
  "integration_mapping_versions",
  "integration_sync_runs",
  "integration_ingestion_quarantine",
  "integration_source_identities",
];

function fail(message) {
  console.error(`INTEGRATION-WAVE-B migration verification failed: ${message}`);
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

console.log("Running INTEGRATION-WAVE-B migration verifier.");

const spine = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${SPINE_MIGRATION}.cs`);
const waveB = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${WAVE_B_MIGRATION}.cs`);

assert("spine migration creates integration_connectors", spine.includes("integration_connectors"));
assert("spine migration seeds integration.read", spine.includes("integration.read"));
assert("spine migration FORCE RLS", spine.includes("FORCE ROW LEVEL SECURITY"));
assert("wave-b migration adds mapping schema fingerprint", waveB.includes("source_schema_fingerprint"));
assert("wave-b migration adds import idempotency key", waveB.includes("import_idempotency_key"));
assert("wave-b migration adds content fingerprint", waveB.includes("content_fingerprint"));

const targetDb = process.env.CREVAFACTOS_INTEGRATION_MIGRATION_VERIFY_DB ?? "crevafactos_integration_wave_b_1";
const applyResult = runMigrationApply(targetDb);
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("migration apply succeeds", applyResult.status === 0, applyOutput.slice(-800));

const historySpine = Number(db(
  `SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${SPINE_MIGRATION}'`,
  targetDb,
));
const historyWaveB = Number(db(
  `SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${WAVE_B_MIGRATION}'`,
  targetDb,
));
assert("spine history row present", historySpine >= 1, `count=${historySpine}`);
assert("wave-b history row present", historyWaveB >= 1, `count=${historyWaveB}`);

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}'
      AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `, targetDb));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);

  const tenantColumn = Number(db(`
    SELECT COUNT(*) FROM information_schema.columns
    WHERE table_name = '${tableName}' AND column_name = 'tenant_id'
  `, targetDb));
  assert(`${tableName} tenant_id column`, tenantColumn === 1);
}

assert("integration.read permission", db(
  `SELECT COUNT(*) FROM permission_definitions WHERE key = 'integration.read'`,
  targetDb,
) === "1");
assert("integration.run permission", db(
  `SELECT COUNT(*) FROM permission_definitions WHERE key = 'integration.run'`,
  targetDb,
) === "1");
assert("integration.mapping.manage permission", db(
  `SELECT COUNT(*) FROM permission_definitions WHERE key = 'integration.mapping.manage'`,
  targetDb,
) === "1");

const secondApply = runMigrationApply(targetDb);
assert("second migration apply is no-op", secondApply.status === 0, `${secondApply.stdout}${secondApply.stderr}`.slice(-400));

console.log("WAVE_B_MIGRATION_RUNTIME=PASS");
console.log("WAVE_B_RLS_SCHEMA=PASS");
console.log("INTEGRATION-WAVE-B migration verification passed.");

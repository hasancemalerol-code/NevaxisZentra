#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260908120000_AddAiTaskEmailDeliveryAdapterFoundation";
const RLS_TABLES = ["ai_task_email_channel_configurations"];

function fail(message) {
  console.error(`EMAIL-DELIVERY-ADAPTER-1 migration verification failed: ${message}`);
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
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", database, "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function readMigrationConnectionEnv(database) {
  return {
    ...process.env,
    NEVAXIS_FACTOS__MIGRATIONS__HOST: "127.0.0.1",
    NEVAXIS_FACTOS__MIGRATIONS__PORT: "5433",
    NEVAXIS_FACTOS__MIGRATIONS__DATABASE: database,
    NEVAXIS_FACTOS__MIGRATIONS__USERNAME: "crevafactos",
    NEVAXIS_FACTOS__MIGRATIONS__PASSWORD: process.env.NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
      ?? process.env.NEVAXIS_FACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE: "Disable",
    NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "300",
  };
}

function runMigrationApply(database) {
  const migrationProject = resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj");
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
      "NevaxisFactOSDbContext",
    ],
    { cwd: ROOT, encoding: "utf8", env: readMigrationConnectionEnv(database) },
  );
}

console.log("Running EMAIL-DELIVERY-ADAPTER-1 migration verifier.");

const migration = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.cs`);
assert("email channel config table", migration.includes("ai_task_email_channel_configurations"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("email send permission", migration.includes("ai-task.delivery.email.send"));

const targetDb = process.env.NEVAXIS_FACTOS_EMAIL_MIGRATION_VERIFY_DB ?? "NEVAXIS_FACTOS_email_closure_1";
const applyResult = runMigrationApply(targetDb);
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("migration apply succeeds", applyResult.status === 0, applyOutput.slice(-800));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`, targetDb));
assert("history row present", historyCount >= 1, `count=${historyCount}`);

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `, targetDb));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

assert("email view permission", db(`SELECT COUNT(*) FROM permission_definitions WHERE key = 'ai-task.delivery.email.view'`, targetDb) === "1");
assert("email send permission", db(`SELECT COUNT(*) FROM permission_definitions WHERE key = 'ai-task.delivery.email.send'`, targetDb) === "1");
assert("email configure permission", db(`SELECT COUNT(*) FROM permission_definitions WHERE key = 'ai-task.delivery.email.configure'`, targetDb) === "1");

console.log("EMAIL_MIGRATION_PRESENT=PASS");
console.log("EMAIL_RLS_VERIFICATION=PASS");
console.log("EMAIL-DELIVERY-ADAPTER-1 migration verification passed.");

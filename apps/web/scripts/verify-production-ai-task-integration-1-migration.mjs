#!/usr/bin/env node
import { readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260829145324_AddProductionFollowUpTaskIntegration";

function fail(message) {
  console.error(`PRODUCTION-AI-TASK-INTEGRATION-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
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

function readMigrationConnectionEnv() {
  const appsettings = JSON.parse(read("apps/api/appsettings.Development.json"));
  const persistence = appsettings.Persistence ?? {};
  return {
    ...process.env,
    NEVAXIS_FACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    NEVAXIS_FACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5433),
    NEVAXIS_FACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    NEVAXIS_FACTOS__MIGRATIONS__USERNAME: "crevafactos",
    NEVAXIS_FACTOS__MIGRATIONS__PASSWORD: process.env.NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
      ?? process.env.NEVAXIS_FACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE: "Disable",
    NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

console.log("Running PRODUCTION-AI-TASK-INTEGRATION-1 migration verifier.");

const migrationsDir = resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/Migrations");
const migrationFiles = readdirSync(migrationsDir);
assert("migration class file exists", migrationFiles.includes(`${MIGRATION_ID}.cs`));
assert("migration designer file exists", migrationFiles.includes(`${MIGRATION_ID}.Designer.cs`));

const migration = read(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.cs`);
const designer = read(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.Designer.cs`);
const entityConfig = read("packages/NevaxisFactOS.Persistence/Configurations/ProductionExecutionExceptionEntityConfiguration.cs");
const snapshot = read("packages/NevaxisFactOS.Persistence.Migrations/Migrations/NevaxisFactOSDbContextModelSnapshot.cs");

assert("follow_up_status column on exceptions", migration.includes("follow_up_status") && migration.includes("production_execution_exceptions"));
assert("linked_ai_task_id column on exceptions", migration.includes("linked_ai_task_id"));
assert("audit events table", migration.includes("production_follow_up_audit_events"));
assert("follow-up backfill sql", migration.includes("requires-review"));
assert("audit rls enable", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("audit force rls", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("audit grants", migration.includes("GRANT SELECT, INSERT ON TABLE production_follow_up_audit_events"));
assert("audit rls select policy", migration.includes("rls_pfae_tenant_select"));
assert("audit rls insert policy", migration.includes("rls_pfae_tenant_insert"));
assert("entity config linked task", entityConfig.includes("LinkedAiTaskId"));
assert("entity config follow up status", entityConfig.includes("FollowUpStatus"));
assert("entity config audit table", entityConfig.includes("production_follow_up_audit_events"));
assert("designer references audit entity", designer.includes("ProductionFollowUpAuditEvent"));
assert("snapshot audit table", snapshot.includes("production_follow_up_audit_events"));

const applyResult = spawnSync(
  "dotnet",
  [
    "tool", "run", "dotnet-ef", "--",
    "database", "update",
    "--project", resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj"),
    "--startup-project", resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj"),
    "--context", "NevaxisFactOSDbContext",
  ],
  { cwd: ROOT, encoding: "utf8", env: readMigrationConnectionEnv() },
);
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("migration apply succeeds", applyResult.status === 0, applyOutput.slice(-500));

const historyCount = Number(db(`SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${MIGRATION_ID}'`));
assert("migration history recorded", historyCount === 1);

const auditTable = db("SELECT to_regclass('public.production_follow_up_audit_events')");
assert("audit table exists", auditTable === "production_follow_up_audit_events");

console.log("PRODUCTION-AI-TASK-INTEGRATION-1 migration verification passed.");

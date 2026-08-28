#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260828094900_AddAiTaskCenterFoundation";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000090";
const CREATE_PERMISSION_ID = "10000000-0000-4000-8000-000000000091";
const REVIEW_PERMISSION_ID = "10000000-0000-4000-8000-000000000092";

const RLS_TABLES = ["ai_tasks", "ai_task_audit_events"];
const APPEND_ONLY_TABLES = ["ai_task_audit_events"];

function fail(message) {
  console.error(`AI-TASK-CENTER-1 migration verification failed: ${message}`);
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

console.log("Running AI-TASK-CENTER-1 migration verifier.");

const migration = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.cs`);
const bootstrap = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("migration creates ai_tasks", migration.includes('name: "ai_tasks"'));
assert("migration creates ai_task_audit_events", migration.includes('name: "ai_task_audit_events"'));
assert("tenant idempotency unique index", migration.includes("ux_ai_tasks_tenant_idempotency"));
assert("tenant reference unique index", migration.includes("ux_ai_tasks_tenant_reference"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE ai_task_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE ai_task_audit_events", "audit events migration");

const applyResult = runMigrationApply();
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("migration apply succeeds", applyResult.status === 0, applyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row exactly once", historyCount === 1, `count=${historyCount}`);

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
  forbidText(privileges, "UPDATE", `${tableName} runtime privileges`);
}

const taskPrivileges = tablePrivileges("crevafactos_runtime", "ai_tasks");
assert("tasks SELECT grant", taskPrivileges.includes("SELECT"), taskPrivileges);
assert("tasks INSERT grant", taskPrivileges.includes("INSERT"), taskPrivileges);
assert("tasks UPDATE grant", taskPrivileges.includes("UPDATE"), taskPrivileges);
forbidText(taskPrivileges, "DELETE", "tasks runtime privileges");

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

assert("ai-task.read permission seeded", db(`SELECT key FROM permission_definitions WHERE id = '${READ_PERMISSION_ID}'`) === "ai-task.read");
assert("ai-task.create permission seeded", db(`SELECT key FROM permission_definitions WHERE id = '${CREATE_PERMISSION_ID}'`) === "ai-task.create");
assert("ai-task.review permission seeded", db(`SELECT key FROM permission_definitions WHERE id = '${REVIEW_PERMISSION_ID}'`) === "ai-task.review");

const historyBefore = historyCount;
const secondApply = runMigrationApply();
const secondOutput = `${secondApply.stdout}${secondApply.stderr}`;
assert("second migration apply succeeds", secondApply.status === 0, secondOutput.slice(-500));
assert(
  "second migration apply is no-op",
  /No migrations were applied|already up to date|Done\./i.test(secondOutput),
  secondOutput.slice(-500),
);
const historyAfter = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("second apply did not duplicate history", historyAfter === historyBefore, `${historyBefore}→${historyAfter}`);

assert("bootstrap read seed", bootstrap.includes("EnsureAiTaskReadPermissionAssignmentAsync"));
assert("bootstrap create seed", bootstrap.includes("EnsureAiTaskCreatePermissionAssignmentAsync"));
assert("bootstrap review seed", bootstrap.includes("EnsureAiTaskReviewPermissionAssignmentAsync"));

console.log("AI-TASK-CENTER-1 migration verification passed.");

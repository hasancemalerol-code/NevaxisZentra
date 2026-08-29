#!/usr/bin/env node
import { readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260829191337_AddProcurementDeliveryRiskFoundation";

function fail(message) {
  console.error(`PROCUREMENT-RISK-FOUNDATION-1 migration verification failed: ${message}`);
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
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
    CREVAFACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    CREVAFACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5433),
    CREVAFACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    CREVAFACTOS__MIGRATIONS__USERNAME: "crevafactos",
    CREVAFACTOS__MIGRATIONS__PASSWORD: process.env.CREVAFACTOS__MIGRATIONS__PASSWORD
      ?? process.env.CREVAFACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    CREVAFACTOS__MIGRATIONS__SSL_MODE: "Disable",
    CREVAFACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

console.log("Running PROCUREMENT-RISK-FOUNDATION-1 migration verifier.");

const migrationsDir = resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/Migrations");
const migrationFiles = readdirSync(migrationsDir);
assert("migration class file exists", migrationFiles.includes(`${MIGRATION_ID}.cs`));
assert("migration designer file exists", migrationFiles.includes(`${MIGRATION_ID}.Designer.cs`));

const migration = read(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.cs`);
const designer = read(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.Designer.cs`);
const entityConfig = read("packages/CrevaFactOS.Persistence/Configurations/ProcurementDeliveryRiskEntityConfigurations.cs");
const snapshot = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");

assert("procurement_delivery_risk_assessments table", migration.includes("procurement_delivery_risk_assessments"));
assert("result column", migration.includes("result"));
assert("input_fingerprint column", migration.includes("input_fingerprint"));
assert("idempotency_key column", migration.includes("idempotency_key"));
assert("is_current index", migration.includes("is_current = TRUE"));
assert("entity config table mapping", entityConfig.includes("procurement_delivery_risk_assessments"));
assert("entity config result column", entityConfig.includes("Result") && entityConfig.includes("result"));
assert("designer references table", designer.includes("procurement_delivery_risk_assessments"));
assert("snapshot references table", snapshot.includes("procurement_delivery_risk_assessments"));
assert("RLS SQL present", migration.includes("ENABLE ROW LEVEL SECURITY") && migration.includes("GRANT SELECT, INSERT, UPDATE"));

const applyResult = spawnSync(
  "dotnet",
  [
    "tool", "run", "dotnet-ef", "--",
    "database", "update",
    "--project", resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj"),
    "--startup-project", resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj"),
    "--context", "CrevaFactOSDbContext",
  ],
  { cwd: ROOT, encoding: "utf8", env: readMigrationConnectionEnv() },
);
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("migration apply succeeds", applyResult.status === 0, applyOutput.slice(-500));

const historyCount = Number(db(`SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${MIGRATION_ID}'`));
assert("migration history recorded", historyCount === 1);

const tableExists = Number(db(`
SELECT COUNT(*) FROM information_schema.tables
WHERE table_schema = 'public' AND table_name = 'procurement_delivery_risk_assessments'
`));
assert("table exists in database", tableExists === 1);

const rlsEnabled = db(`
SELECT relrowsecurity::text || ',' || relforcerowsecurity::text
FROM pg_class
WHERE relname = 'procurement_delivery_risk_assessments'
`);
assert("RLS enabled and forced", rlsEnabled === "t,t" || rlsEnabled === "true,true");

const policyCount = Number(db(`
SELECT COUNT(*) FROM pg_policies
WHERE schemaname = 'public' AND tablename = 'procurement_delivery_risk_assessments'
`));
assert("tenant RLS policies exist", policyCount >= 3);

console.log("PROCUREMENT-RISK-FOUNDATION-1 migration verification passed.");

#!/usr/bin/env node
import { readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260829172531_AddInventoryAvailabilityRiskFoundation";

function fail(message) {
  console.error(`INVENTORY-RISK-FOUNDATION-1 migration verification failed: ${message}`);
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

console.log("Running INVENTORY-RISK-FOUNDATION-1 migration verifier.");

const migrationsDir = resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/Migrations");
const migrationFiles = readdirSync(migrationsDir);
assert("migration class file exists", migrationFiles.includes(`${MIGRATION_ID}.cs`));
assert("migration designer file exists", migrationFiles.includes(`${MIGRATION_ID}.Designer.cs`));

const migration = read(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.cs`);
const designer = read(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.Designer.cs`);
const entityConfig = read("packages/CrevaFactOS.Persistence/Configurations/MaterialRequirementEntityConfigurations.cs");
const snapshot = read("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");

assert("erp_available_quantity column", migration.includes("erp_available_quantity") && migration.includes("material_requirement_assessments"));
assert("erp_reserved_quantity column", migration.includes("erp_reserved_quantity"));
assert("shortage_quantity column", migration.includes("shortage_quantity"));
assert("available_after_reservation_quantity column", migration.includes("available_after_reservation_quantity"));
assert("source_freshness column", migration.includes("source_freshness"));
assert("assessment_type status index", migration.includes("assessment_type") && migration.includes("status") && migration.includes("CreateIndex"));
assert("entity config erp available quantity", entityConfig.includes("ErpAvailableQuantity") && entityConfig.includes("erp_available_quantity"));
assert("entity config erp reserved quantity", entityConfig.includes("ErpReservedQuantity"));
assert("entity config shortage quantity", entityConfig.includes("ShortageQuantity"));
assert("entity config available after reservation", entityConfig.includes("AvailableAfterReservationQuantity"));
assert("entity config source freshness", entityConfig.includes("SourceFreshness"));
assert("entity config assessment status index", entityConfig.includes("AssessmentType") && entityConfig.includes("Status") && entityConfig.includes("EvaluatedAtUtc"));
assert("designer references availability columns", designer.includes("erp_available_quantity") && designer.includes("source_freshness"));
assert("snapshot availability columns", snapshot.includes("erp_available_quantity") && snapshot.includes("shortage_quantity") && snapshot.includes("source_freshness"));

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

const columnNames = db(`
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'material_requirement_assessments'
  AND column_name IN (
    'erp_available_quantity',
    'erp_reserved_quantity',
    'shortage_quantity',
    'available_after_reservation_quantity',
    'source_freshness'
  )
ORDER BY column_name
`).split("\n").filter(Boolean);
assert("availability risk columns exist in database", columnNames.length === 5, columnNames.join(", "));

const indexCount = Number(db(`
SELECT COUNT(*)
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename = 'material_requirement_assessments'
  AND indexdef ILIKE '%assessment_type%'
  AND indexdef ILIKE '%status%'
`));
assert("assessment_type+status index exists", indexCount >= 1);

console.log("INVENTORY-RISK-FOUNDATION-1 migration verification passed.");

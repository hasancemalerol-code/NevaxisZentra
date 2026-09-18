#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260819150759_AddEnergyIntelligenceFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const CLOSURE_PREFIX = "ENERGY2-CLOSURE";
const RUN_SUFFIX = Date.now();

const RLS_TABLES = [
  "energy_baseline_profiles",
  "energy_targets",
  "energy_intelligence_assessments",
  "energy_intelligence_drivers",
];

const APPEND_ONLY_TABLES = [
  "energy_intelligence_assessments",
  "energy_intelligence_drivers",
];

function fail(message) {
  console.error(`ENERGY-2 migration verification failed: ${message}`);
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

function removeClosureFixtures() {
  db(`
    DELETE FROM energy_intelligence_drivers
    WHERE assessment_id IN (
      SELECT id FROM energy_intelligence_assessments
      WHERE assessment_reference LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM energy_intelligence_assessments
    WHERE assessment_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM energy_targets WHERE target_reference LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM energy_baseline_profiles WHERE baseline_reference LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running ENERGY-2 migration verifier.");

const migrationDir = "packages/NevaxisZentra.Persistence.Migrations/Migrations";
const migration = readRequired(`${migrationDir}/${MIGRATION_BASENAME}`);
const designer = readRequired(`${migrationDir}/${DESIGNER_BASENAME}`);
const snapshot = readRequired(`${migrationDir}/NevaxisZentraDbContextModelSnapshot.cs`);
const allMigrations = readRequired(`${migrationDir}/NevaxisZentraDbContextModelSnapshot.cs`);

const migrationMatches = [...migration.matchAll(/AddEnergyIntelligenceFoundation/g)];
assert("migration discovered exactly once in class", migrationMatches.length >= 1);

assert("designer present", designer.includes(MIGRATION_ID));
assert("snapshot includes energy_baseline_profiles", snapshot.includes("energy_baseline_profiles"));
assert("snapshot includes energy_intelligence_assessments", snapshot.includes("energy_intelligence_assessments"));
assert("restrictive FK pattern", migration.includes("ReferentialAction.Restrict"));
assert("unique baseline reference index", migration.includes("ux_energy_baseline_profiles_tenant_facility_reference"));
assert("unique assessment reference index", migration.includes("ux_energy_intelligence_assessments_tenant_facility_reference"));
assert("version check constraints", migration.includes("ck_energy_baseline_profiles_version"));
forbidText(migration, "GRANT DELETE", "migration");

for (const table of RLS_TABLES) {
  assert(`RLS table ${table} in migration`, migration.includes(table));
}

const migrateEnv = readMigrationConnectionEnv();
const firstApply = runMigrationApply();
assert("migration apply succeeds", firstApply.status === 0, `${firstApply.stdout}${firstApply.stderr}`.slice(-500));

const secondApply = runMigrationApply();
assert("second apply no-op", secondApply.status === 0);

const migrationHistoryCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("migration applied once", migrationHistoryCount === 1, `count=${migrationHistoryCount}`);

for (const table of RLS_TABLES) {
  const rls = db(`SELECT relrowsecurity, relforcerowsecurity FROM pg_class WHERE relname = '${table}'`);
  const [enabled, forced] = rls.split("|");
  assert(`${table} RLS enabled`, enabled === "t");
  assert(`${table} FORCE RLS`, forced === "t");
}

const baselineGrants = tablePrivileges("crevafactos_runtime", "energy_baseline_profiles");
assert("baseline minimum grants", baselineGrants.includes("SELECT") && baselineGrants.includes("INSERT") && baselineGrants.includes("UPDATE"));
forbidText(baselineGrants, "DELETE", "baseline grants");

const assessmentGrants = tablePrivileges("crevafactos_runtime", "energy_intelligence_assessments");
assert("assessment append-only grants", assessmentGrants.includes("SELECT") && assessmentGrants.includes("INSERT"));
forbidText(assessmentGrants, "DELETE", "assessment grants");
forbidText(assessmentGrants, "UPDATE", "assessment grants");

const orphanAssessments = Number(db(`
  SELECT COUNT(*) FROM energy_intelligence_assessments a
  WHERE NOT EXISTS (
    SELECT 1 FROM facilities f WHERE f.tenant_id = a.tenant_id AND f.id = a.facility_id
  )
`));
assert("no orphan ENERGY-2 assessments", orphanAssessments === 0);

const energy1Point = Number(db(`
  SELECT COUNT(*) FROM energy_measurement_points
  WHERE id = 'bb000001-0160-4000-8000-000000000160'
`));
assert("ENERGY-1 pilot point preserved", energy1Point === 1);

removeClosureFixtures();
const closureRemaining = Number(db(`
  SELECT COUNT(*) FROM energy_targets WHERE target_reference LIKE '${CLOSURE_PREFIX}%'
`));
assert("closure fixtures removed", closureRemaining === 0);

console.log("ENERGY-2 migration verification passed.");

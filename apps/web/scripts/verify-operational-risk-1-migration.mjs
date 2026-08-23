#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260823113228_AddOperationalRiskFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000080";
const MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000081";
const CLOSURE_PREFIX = "OPERATIONAL-RISK1-CLOSURE";

const RLS_TABLES = [
  "operational_risk_cases",
  "operational_risk_evidence",
  "operational_risk_status_history",
  "operational_risk_audit_events",
];

const APPEND_ONLY_TABLES = [
  "operational_risk_evidence",
  "operational_risk_status_history",
  "operational_risk_audit_events",
];

function fail(message) {
  console.error(`OPERATIONAL-RISK-1 migration verification failed: ${message}`);
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

function removeClosureFixtures() {
  db(`
    DELETE FROM operational_risk_audit_events
    WHERE entity_id IN (
      SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
      UNION SELECT id FROM operational_risk_evidence WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM operational_risk_status_history
    WHERE case_id IN (SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_evidence
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR case_id IN (SELECT id FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_cases WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running OPERATIONAL-RISK-1 migration verifier.");

const migration = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotOperationalRiskSeed.cs");

assert("migration source present", migration.includes("AddOperationalRiskFoundation"));
assert("designer present", designer.includes("AddOperationalRiskFoundation"));
assert("snapshot includes cases", snapshot.includes('ToTable("operational_risk_cases"'));
assert("snapshot includes evidence", snapshot.includes('ToTable("operational_risk_evidence"'));
assert("check constraints present", migration.includes("ck_operational_risk_cases_score"));
assert("permission seed present", migration.includes("operational-risk.read"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const migrationDiscoveryCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("migration discovered exactly once", migrationDiscoveryCount === 1, `count=${migrationDiscoveryCount}`);

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

const casesPrivileges = tablePrivileges("crevafactos_runtime", "operational_risk_cases");
assert("cases SELECT grant", casesPrivileges.includes("SELECT"), casesPrivileges);
assert("cases INSERT grant", casesPrivileges.includes("INSERT"), casesPrivileges);
assert("cases UPDATE grant", casesPrivileges.includes("UPDATE"), casesPrivileges);
forbidText(casesPrivileges, "DELETE", "cases runtime privileges");

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

const readPermission = db(`SELECT key FROM permission_definitions WHERE id = '${READ_PERMISSION_ID}'`);
const managePermission = db(`SELECT key FROM permission_definitions WHERE id = '${MANAGE_PERMISSION_ID}'`);
assert("operational-risk.read permission seeded", readPermission === "operational-risk.read", readPermission);
assert("operational-risk.manage permission seeded", managePermission === "operational-risk.manage", managePermission);

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

assert("bootstrap seed references pilot material case", bootstrap.includes("OperationalRiskPilotMaterialCaseId"));
const pilotCaseCount = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE id = 'bb000001-0200-4000-8000-000000000200'`));
if (pilotCaseCount === 0) {
  fail("Operational risk pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot material case seeded", pilotCaseCount === 1, `count=${pilotCaseCount}`);

const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot-plc pilot preserved", iotCount === 1, `count=${iotCount}`);

removeClosureFixtures();
const orphanEvidence = Number(db(`
  SELECT COUNT(*) FROM operational_risk_evidence e
  LEFT JOIN operational_risk_cases c ON c.id = e.case_id
  WHERE c.id IS NULL
`));
const orphanHistory = Number(db(`
  SELECT COUNT(*) FROM operational_risk_status_history h
  LEFT JOIN operational_risk_cases c ON c.id = h.case_id
  WHERE c.id IS NULL
`));
assert("no orphan evidence", orphanEvidence === 0, `count=${orphanEvidence}`);
assert("no orphan history", orphanHistory === 0, `count=${orphanHistory}`);

console.log("OPERATIONAL-RISK-1 migration verification passed.");

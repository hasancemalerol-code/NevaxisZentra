#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260814051728_AddSaasCommercialPlanAndEntitlementFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const DEV_PLAN_ID = "bb000001-00e0-4000-8000-0000000000e0";
const DEV_PLAN_CODE = "dev-pilot-local-commercial";
const PERMISSION_ID = "10000000-0000-4000-8000-000000000071";

const CATALOG_TABLES = [
  "saas_commercial_plans",
  "saas_commercial_plan_versions",
  "saas_commercial_price_components",
  "saas_commercial_entitlement_definitions",
  "saas_commercial_plan_version_entitlements",
  "saas_commercial_payment_method_capabilities",
  "saas_commercial_grace_period_policies",
];

function fail(message) {
  console.error(`SAAS-COMMERCIAL-1 migration verification failed: ${message}`);
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

function runSecondMigrationApply() {
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

console.log("Running SAAS-COMMERCIAL-1 migration verifier.");

const migration = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotSaasCommercialSeed.cs");

assert("migration source present", migration.includes("AddSaasCommercialPlanAndEntitlementFoundation"));
assert("designer present", designer.includes("AddSaasCommercialPlanAndEntitlementFoundation"));
assert("snapshot includes saas commercial plans", snapshot.includes('ToTable("saas_commercial_plans"'));
assert("snapshot includes saas commercial audit events", snapshot.includes('ToTable("saas_commercial_audit_events"'));

const migrationHistoryCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("migration history row exactly one", migrationHistoryCount === 1, `count=${migrationHistoryCount}`);

const requiredConstraints = [
  "ex_sc_plan_versions_no_overlap",
  "ck_sc_plans_lifecycle_state",
  "ck_sc_plans_availability_state",
  "ck_sc_plan_versions_billing_cadence",
  "ck_sc_plan_version_entitlements_semantics",
  "ck_sc_payment_method_capabilities_method_type",
];
for (const constraintName of requiredConstraints) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_constraint WHERE conname = '${constraintName}'`));
  assert(`constraint ${constraintName}`, count === 1, `count=${count}`);
}

const requiredIndexes = [
  "ix_saas_commercial_plans_plan_code_normalized",
  "ix_saas_commercial_entitlement_definitions_code_normalized",
  "ix_saas_commercial_plan_versions_plan_version",
  "ix_saas_commercial_plan_version_entitlements_version_definition",
  "ix_saas_commercial_payment_method_capabilities_version_type",
  "ix_saas_commercial_price_components_version_type",
  "ix_saas_commercial_grace_period_policies_plan_version",
];
for (const indexName of requiredIndexes) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_indexes WHERE indexname = '${indexName}'`));
  assert(`index ${indexName}`, count === 1, `count=${count}`);
}

for (const tableName of CATALOG_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  assert(`${tableName} runtime SELECT grant`, privileges.includes("SELECT"), privileges || "(none)");
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
  forbidText(privileges, "INSERT", `${tableName} catalog runtime privileges`);
  forbidText(privileges, "UPDATE", `${tableName} catalog runtime privileges`);
}

const auditPrivileges = tablePrivileges("crevafactos_runtime", "saas_commercial_audit_events");
assert("audit runtime SELECT grant", auditPrivileges.includes("SELECT"), auditPrivileges);
assert("audit runtime INSERT grant", auditPrivileges.includes("INSERT"), auditPrivileges);
forbidText(auditPrivileges, "DELETE", "audit runtime privileges");
forbidText(auditPrivileges, "UPDATE", "audit runtime privileges");

assert("migration declares catalog SELECT grants", migration.includes("GRANT SELECT ON TABLE saas_commercial_plans TO crevafactos_runtime"));
assert("migration declares audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE saas_commercial_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE saas_commercial_plans", "migration");
forbidText(migration, "GRANT DELETE ON TABLE saas_commercial_audit_events", "migration");

const permissionRow = db(`
  SELECT key FROM permission_definitions WHERE id = '${PERMISSION_ID}'
`);
assert("saas-commercial.read permission seeded", permissionRow === "saas-commercial.read", permissionRow);

const historyBefore = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
const applyResult = runSecondMigrationApply();
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
assert("second migration apply did not duplicate history", historyAfter === historyBefore, `${historyBefore}→${historyAfter}`);

assert("bootstrap seed references dev plan code", bootstrap.includes(DEV_PLAN_CODE));
const devPlanCount = Number(db(`SELECT COUNT(*) FROM saas_commercial_plans WHERE id = '${DEV_PLAN_ID}'`));
if (devPlanCount === 0) {
  fail("dev catalog not bootstrapped — start the API with DevelopmentPilot enabled before bootstrap count checks");
}
const devVersionCount = Number(db(`SELECT COUNT(*) FROM saas_commercial_plan_versions WHERE plan_id = '${DEV_PLAN_ID}'`));
const devEntitlementDefCount = Number(db("SELECT COUNT(*) FROM saas_commercial_entitlement_definitions"));
const devGrantCount = Number(db(`
  SELECT COUNT(*) FROM saas_commercial_plan_version_entitlements e
  JOIN saas_commercial_plan_versions v ON v.id = e.plan_version_id
  WHERE v.plan_id = '${DEV_PLAN_ID}'
`));
const devPaymentMethodCount = Number(db(`
  SELECT COUNT(*) FROM saas_commercial_payment_method_capabilities c
  JOIN saas_commercial_plan_versions v ON v.id = c.plan_version_id
  WHERE v.plan_id = '${DEV_PLAN_ID}'
`));
const devGracePolicyCount = Number(db(`
  SELECT COUNT(*) FROM saas_commercial_grace_period_policies g
  JOIN saas_commercial_plan_versions v ON v.id = g.plan_version_id
  WHERE v.plan_id = '${DEV_PLAN_ID}'
`));
const devPriceComponentCount = Number(db(`
  SELECT COUNT(*) FROM saas_commercial_price_components c
  JOIN saas_commercial_plan_versions v ON v.id = c.plan_version_id
  WHERE v.plan_id = '${DEV_PLAN_ID}'
`));

assert("bootstrap dev catalog plan count = 1", devPlanCount === 1, `count=${devPlanCount}`);
assert("bootstrap dev catalog version count = 2", devVersionCount === 2, `count=${devVersionCount}`);
assert("bootstrap entitlement definition count = 4", devEntitlementDefCount === 4, `count=${devEntitlementDefCount}`);
assert("bootstrap dev entitlement grant count = 8", devGrantCount === 8, `count=${devGrantCount}`);
assert("bootstrap dev payment method count = 6", devPaymentMethodCount === 6, `count=${devPaymentMethodCount}`);
assert("bootstrap dev grace policy count = 2", devGracePolicyCount === 2, `count=${devGracePolicyCount}`);
assert("bootstrap dev price component count = 14", devPriceComponentCount === 14, `count=${devPriceComponentCount}`);

const monthlyCadenceCount = Number(db(`
  SELECT COUNT(*) FROM saas_commercial_plan_versions
  WHERE plan_id = '${DEV_PLAN_ID}' AND billing_cadence = 'monthly'
`));
const annualCadenceCount = Number(db(`
  SELECT COUNT(*) FROM saas_commercial_plan_versions
  WHERE plan_id = '${DEV_PLAN_ID}' AND billing_cadence = 'annual'
`));
assert("bootstrap dev monthly cadence count = 1", monthlyCadenceCount === 1, `count=${monthlyCadenceCount}`);
assert("bootstrap dev annual cadence count = 1", annualCadenceCount === 1, `count=${annualCadenceCount}`);

console.log("SAAS-COMMERCIAL-1 migration verification passed.");

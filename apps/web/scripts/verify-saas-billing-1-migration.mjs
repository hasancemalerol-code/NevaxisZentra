#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260815061939_AddSaasBillingAccountInvoicePaymentFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const BILLING_READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000074";
const BILLING_MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000075";
const DEV_BILLING_ACCOUNT_ID = "bb000003-00a0-4000-8000-0000000000a0";

const RLS_TABLES = [
  "saas_tenant_billing_accounts",
  "saas_billing_invoices",
  "saas_billing_invoice_lines",
  "saas_billing_payment_attempts",
  "saas_billing_idempotency_records",
  "saas_billing_audit_events",
];

const APPEND_ONLY_TABLES = [
  "saas_billing_invoices",
  "saas_billing_payment_attempts",
  "saas_billing_audit_events",
];

function fail(message) {
  console.error(`SAAS-BILLING-1 migration verification failed: ${message}`);
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

console.log("Running SAAS-BILLING-1 migration verifier.");

const migration = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotSaasBillingSeed.cs");

assert("migration source present", migration.includes("AddSaasBillingAccountInvoicePaymentFoundation"));
assert("designer present", designer.includes("AddSaasBillingAccountInvoicePaymentFoundation"));
assert("snapshot includes billing accounts", snapshot.includes('ToTable("saas_tenant_billing_accounts"'));
assert("snapshot includes billing invoices", snapshot.includes('ToTable("saas_billing_invoices"'));
assert("snapshot includes billing payment attempts", snapshot.includes('ToTable("saas_billing_payment_attempts"'));

const firstApplyResult = runSecondMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("initial migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const migrationHistoryCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("migration history row exactly one", migrationHistoryCount === 1, `count=${migrationHistoryCount}`);

const requiredConstraints = [
  "ck_saas_billing_invoices_status",
  "ck_saas_billing_payment_attempts_method",
  "ck_saas_billing_payment_attempts_status",
];
for (const constraintName of requiredConstraints) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_constraint WHERE conname = '${constraintName}'`));
  assert(`constraint ${constraintName}`, count === 1, `count=${count}`);
}

const requiredIndexes = [
  "ux_saas_billing_invoices_tenant_number",
  "ux_saas_billing_invoices_idempotency",
  "ux_saas_billing_payment_attempts_idempotency",
  "ux_saas_billing_idempotency_key",
  "ux_saas_tenant_billing_accounts_tenant",
];
for (const indexName of requiredIndexes) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_indexes WHERE indexname = '${indexName}'`));
  assert(`index ${indexName}`, count === 1, `count=${count}`);
}

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
}

const invoicePrivileges = tablePrivileges("crevafactos_runtime", "saas_billing_invoices");
assert("invoices runtime SELECT grant", invoicePrivileges.includes("SELECT"), invoicePrivileges);
assert("invoices runtime INSERT grant", invoicePrivileges.includes("INSERT"), invoicePrivileges);
assert("invoices runtime UPDATE grant", invoicePrivileges.includes("UPDATE"), invoicePrivileges);

const paymentPrivileges = tablePrivileges("crevafactos_runtime", "saas_billing_payment_attempts");
assert("payments runtime SELECT grant", paymentPrivileges.includes("SELECT"), paymentPrivileges);
assert("payments runtime INSERT grant", paymentPrivileges.includes("INSERT"), paymentPrivileges);
assert("payments runtime UPDATE grant", paymentPrivileges.includes("UPDATE"), paymentPrivileges);

const auditPrivileges = tablePrivileges("crevafactos_runtime", "saas_billing_audit_events");
assert("audit runtime SELECT grant", auditPrivileges.includes("SELECT"), auditPrivileges);
assert("audit runtime INSERT grant", auditPrivileges.includes("INSERT"), auditPrivileges);
forbidText(auditPrivileges, "DELETE", "audit runtime privileges");
forbidText(auditPrivileges, "UPDATE", "audit runtime privileges");

assert("migration declares invoice grants", migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE saas_billing_invoices"));
assert("migration declares payment grants", migration.includes("GRANT SELECT, INSERT, UPDATE ON TABLE saas_billing_payment_attempts"));
assert("migration declares audit append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE saas_billing_audit_events"));
forbidText(migration, "GRANT DELETE ON TABLE saas_billing_invoices", "migration");
forbidText(migration, "GRANT DELETE ON TABLE saas_billing_payment_attempts", "migration");
forbidText(migration, "GRANT DELETE ON TABLE saas_billing_audit_events", "migration");

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
  const policies = Number(db(`
    SELECT COUNT(*) FROM pg_policies WHERE schemaname = 'public' AND tablename = '${tableName}'
  `));
  assert(`${tableName} tenant policies present`, policies >= 2, `count=${policies}`);
}

const readPermission = db(`SELECT key FROM permission_definitions WHERE id = '${BILLING_READ_PERMISSION_ID}'`);
const managePermission = db(`SELECT key FROM permission_definitions WHERE id = '${BILLING_MANAGE_PERMISSION_ID}'`);
assert("billing.read permission seeded", readPermission === "billing.read", readPermission);
assert("billing.manage permission seeded", managePermission === "billing.manage", managePermission);

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

assert("bootstrap seed references dev billing account", bootstrap.includes("DevPilotBillingAccountId"));
const devAccountCount = Number(db(`SELECT COUNT(*) FROM saas_tenant_billing_accounts WHERE id = '${DEV_BILLING_ACCOUNT_ID}'`));
if (devAccountCount === 0) {
  fail("dev billing not bootstrapped — start the API with DevelopmentPilot enabled before bootstrap count checks");
}
const devInvoiceCount = Number(db(`SELECT COUNT(*) FROM saas_billing_invoices WHERE billing_account_id = '${DEV_BILLING_ACCOUNT_ID}'`));
const devPaymentCount = Number(db(`
  SELECT COUNT(*) FROM saas_billing_payment_attempts p
  JOIN saas_billing_invoices i ON i.id = p.invoice_id
  WHERE i.billing_account_id = '${DEV_BILLING_ACCOUNT_ID}'
`));
assert("bootstrap dev billing account count = 1", devAccountCount === 1, `count=${devAccountCount}`);
assert("bootstrap dev invoice count = 4", devInvoiceCount === 4, `count=${devInvoiceCount}`);
assert("bootstrap dev payment attempt count = 2", devPaymentCount === 2, `count=${devPaymentCount}`);

console.log("SAAS-BILLING-1 migration verification passed.");

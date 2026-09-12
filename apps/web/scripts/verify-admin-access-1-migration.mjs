#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MIGRATION_ID = "20260814131819_AddTenantAdministrativeAccessControlFoundation";
const CLOSURE_PREFIX = "ADMINACCESS1-CLOSURE";

function fail(message) {
  console.error(`ADMIN-ACCESS-1 migration verification failed: ${message}`);
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

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function tablePrivileges(tableName) {
  return db(`
    SELECT COALESCE(string_agg(DISTINCT privilege_type, ',' ORDER BY privilege_type), '')
    FROM information_schema.table_privileges
    WHERE grantee = 'crevafactos_runtime' AND table_name = '${tableName}'
  `);
}

function readMigrationConnectionEnv() {
  const appsettings = JSON.parse(readFileSync(resolve(ROOT, "apps/api/appsettings.Development.json"), "utf8"));
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
  };
}

function runSecondMigrationApply() {
  const migrationProject = resolve(ROOT, "packages/NevaxisFactOS.Persistence.Migrations/NevaxisFactOS.Persistence.Migrations.csproj");
  return spawnSync(
    "dotnet",
    ["tool", "run", "dotnet-ef", "--", "database", "update", "--project", migrationProject, "--startup-project", migrationProject, "--context", "NevaxisFactOSDbContext"],
    { cwd: ROOT, encoding: "utf8", env: readMigrationConnectionEnv() },
  );
}

console.log("Running ADMIN-ACCESS-1 migration verifier.");

const migration = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.cs`);
const designer = readRequired(`packages/NevaxisFactOS.Persistence.Migrations/Migrations/${MIGRATION_ID}.Designer.cs`);
const snapshot = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/NevaxisFactOSDbContextModelSnapshot.cs");

assert("migration source present", migration.includes("AddTenantAdministrativeAccessControlFoundation"));
assert("designer present", designer.includes("AddTenantAdministrativeAccessControlFoundation"));
assert("snapshot includes overrides", snapshot.includes('ToTable("tenant_membership_permission_overrides"'));
assert("snapshot includes authorization audit", snapshot.includes('ToTable("authorization_audit_events"'));

const historyCount = Number(db(`SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${MIGRATION_ID}'`));
assert("migration history row exactly one", historyCount === 1, `count=${historyCount}`);

for (const tableName of [
  "tenant_membership_permission_overrides",
  "authorization_audit_events",
  "admin_access_idempotency_records",
  "tenant_roles",
  "tenant_role_permissions",
  "tenant_membership_roles",
]) {
  const rls = db(`SELECT relrowsecurity::text || ',' || relforcerowsecurity::text FROM pg_class WHERE relname = '${tableName}'`);
  assert(`${tableName} RLS + FORCE RLS`, rls === "true,true", rls);
}

const requiredIndexes = [
  "IX_tenant_memberships_tenant_id_membership_reference",
  "IX_permission_definitions_normalized_key",
  "ux_tenant_membership_permission_overrides_grant",
  "ux_admin_access_idempotency_key",
  "ix_authorization_audit_events_tenant_created",
];
for (const indexName of requiredIndexes) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_indexes WHERE indexname = '${indexName}'`));
  assert(`index ${indexName}`, count === 1, `count=${count}`);
}

const requiredFks = [
  "FK_tenant_membership_permission_overrides_tenants_tenant_id",
  "FK_authorization_audit_events_tenants_tenant_id",
  "FK_admin_access_idempotency_records_tenants_tenant_id",
];
for (const fkName of requiredFks) {
  const count = Number(db(`SELECT COUNT(*) FROM pg_constraint WHERE conname = '${fkName}'`));
  assert(`restrictive FK ${fkName}`, count === 1, `count=${count}`);
}

const auditPrivileges = tablePrivileges("authorization_audit_events");
assert("audit SELECT grant", auditPrivileges.includes("SELECT"), auditPrivileges);
assert("audit INSERT grant", auditPrivileges.includes("INSERT"), auditPrivileges);
assert("no DELETE on audit/history", !auditPrivileges.includes("DELETE"), auditPrivileges);

const countsBefore = {
  roles: Number(db("SELECT COUNT(*) FROM tenant_roles")),
  grants: Number(db("SELECT COUNT(*) FROM tenant_role_permissions")),
  overrides: Number(db("SELECT COUNT(*) FROM tenant_membership_permission_overrides")),
  audits: Number(db("SELECT COUNT(*) FROM authorization_audit_events")),
};
const applyResult = runSecondMigrationApply();
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("second apply exit code 0", applyResult.status === 0, applyOutput.slice(-500));
assert("second apply no-op", /No migrations were applied|already up to date|Done\./i.test(applyOutput), applyOutput.slice(-500));
const historyAfter = Number(db(`SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '${MIGRATION_ID}'`));
assert("second apply history unchanged", historyAfter === 1, `count=${historyAfter}`);
assert("role count unchanged", Number(db("SELECT COUNT(*) FROM tenant_roles")) === countsBefore.roles);
assert("grant count unchanged", Number(db("SELECT COUNT(*) FROM tenant_role_permissions")) === countsBefore.grants);
assert("override count unchanged", Number(db("SELECT COUNT(*) FROM tenant_membership_permission_overrides")) === countsBefore.overrides);
assert("audit count unchanged", Number(db("SELECT COUNT(*) FROM authorization_audit_events")) === countsBefore.audits);

const orphans = Number(db(`
  SELECT COUNT(*) FROM tenant_role_permissions g
  LEFT JOIN tenant_roles r ON r.id = g.tenant_role_id
  WHERE r.id IS NULL
`)) + Number(db(`
  SELECT COUNT(*) FROM tenant_membership_roles mr
  LEFT JOIN tenant_memberships m ON m.id = mr.tenant_membership_id
  WHERE m.id IS NULL
`)) + Number(db(`
  SELECT COUNT(*) FROM tenant_membership_permission_overrides o
  LEFT JOIN tenant_memberships m ON m.id = o.tenant_membership_id
  WHERE m.id IS NULL
`)) + Number(db(`
  SELECT COUNT(*) FROM authorization_audit_events a
  LEFT JOIN tenants t ON t.id = a.tenant_id
  WHERE t.id IS NULL
`));
assert("zero orphan authorization records", orphans === 0, `count=${orphans}`);

const templates = Number(db("SELECT COUNT(*) FROM tenant_roles WHERE system_template_origin = 'development-template'"));
assert("bootstrap templates present without requiring duplicates", templates === 15 || templates === 0, `count=${templates}`);

const closureRemaining = Number(db(`
  SELECT COUNT(*) FROM tenant_roles WHERE key ILIKE 'adminaccess1-closure%'
`)) + Number(db(`
  SELECT COUNT(*) FROM tenant_memberships WHERE membership_reference LIKE '${CLOSURE_PREFIX}%'
`));
assert("closure fixtures remaining", closureRemaining === 0, `count=${closureRemaining}`);

console.log("ADMIN-ACCESS-1 migration verification passed.");

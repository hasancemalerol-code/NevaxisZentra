#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const TENANT_OWNER_ROLE_ID = "bb000003-0101-4000-8000-000000000101";
const DEV_MONTHLY_VERSION_ID = "bb000001-00e2-4000-8000-0000000000e2";
const DEV_SUBSCRIPTION_ID = "bb000002-00e0-4000-8000-0000000000e0";

const MODULE_ENTITLEMENTS = [
  "module.dashboard",
  "module.teams",
  "module.orders",
  "module.planning",
  "module.production",
  "module.export",
  "module.shift-workforce",
  "module.process-oee",
  "module.connected-machines",
  "module.mold",
  "module.material",
  "module.maintenance",
  "module.quality",
  "module.energy",
  "module.iot-plc",
  "module.reporting",
  "module.erp-integration",
  "module.commercial-intelligence",
  "module.operational-risk",
  "module.ai-task-center",
];

function fail(message) {
  console.error(`SALES-DEMO bootstrap failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout || "psql failed");
  return result.stdout.trim();
}

function sqlQuote(value) {
  return `'${value.replace(/'/g, "''")}'`;
}

function ensurePermissions() {
  const inserted = db(`
    WITH missing AS (
      SELECT pd.id
      FROM permission_definitions pd
      WHERE pd.key NOT LIKE 'platform.%'
        AND NOT EXISTS (
        SELECT 1
        FROM tenant_role_permissions trp
        WHERE trp.tenant_id = '${TENANT_ID}'::uuid
          AND trp.tenant_role_id = '${TENANT_OWNER_ROLE_ID}'::uuid
          AND trp.permission_definition_id = pd.id
      )
    )
    INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
    SELECT '${TENANT_ID}'::uuid, '${TENANT_OWNER_ROLE_ID}'::uuid, missing.id
    FROM missing
    RETURNING permission_definition_id;
  `);
  const count = inserted ? inserted.split("\n").filter(Boolean).length : 0;
  assert(`tenant-owner permissions backfilled (${count} new assignments)`, true);
  const total = db(`
    SELECT COUNT(*)
    FROM tenant_role_permissions trp
    INNER JOIN permission_definitions pd ON pd.id = trp.permission_definition_id
  WHERE trp.tenant_id = '${TENANT_ID}'::uuid
    AND trp.tenant_role_id = '${TENANT_OWNER_ROLE_ID}'::uuid
  `);
  console.log(`  tenant-owner permission assignments total=${total}`);
}

function ensureSubscriptionActive() {
  db(`
    UPDATE saas_tenant_subscriptions
    SET is_current = false, updated_at_utc = NOW()
    WHERE tenant_id = '${TENANT_ID}'::uuid AND id <> '${DEV_SUBSCRIPTION_ID}'::uuid AND is_current = true;

    UPDATE saas_tenant_subscriptions
    SET is_current = true,
        subscription_status = 'active',
        commercial_plan_version_id = '${DEV_MONTHLY_VERSION_ID}'::uuid,
        grace_until_utc = NULL,
        suspended_at_utc = NULL,
        cancelled_at_utc = NULL,
        cancel_at_period_end = false,
        updated_at_utc = NOW()
    WHERE id = '${DEV_SUBSCRIPTION_ID}'::uuid;
  `);
  assert("dev pilot subscription active on monthly plan", true);
}

function ensureModuleEntitlements() {
  for (const code of MODULE_ENTITLEMENTS) {
    const normalized = code.toLowerCase();
    const label = code.replace("module.", "Modül — ");
    db(`
      INSERT INTO saas_commercial_entitlement_definitions (
        id, code, code_normalized, display_name_tr, description_tr, value_kind, created_at_utc
      )
      SELECT
        gen_random_uuid(),
        ${sqlQuote(code)},
        ${sqlQuote(normalized)},
        ${sqlQuote(`Satış demo — ${label}`)},
        'Yerel geliştirme pilotu satış demosu için modül erişimi.',
        'module-access',
        NOW()
      WHERE NOT EXISTS (
        SELECT 1 FROM saas_commercial_entitlement_definitions WHERE code_normalized = ${sqlQuote(normalized)}
      );
    `);

    db(`
      INSERT INTO saas_commercial_plan_version_entitlements (
        id, plan_version_id, entitlement_definition_id, semantics, boolean_value, integer_limit_value
      )
      SELECT
        gen_random_uuid(),
        '${DEV_MONTHLY_VERSION_ID}'::uuid,
        d.id,
        'unlimited',
        true,
        NULL
      FROM saas_commercial_entitlement_definitions d
      WHERE d.code_normalized = ${sqlQuote(normalized)}
        AND NOT EXISTS (
          SELECT 1
          FROM saas_commercial_plan_version_entitlements g
          WHERE g.plan_version_id = '${DEV_MONTHLY_VERSION_ID}'::uuid
            AND g.entitlement_definition_id = d.id
        );

      UPDATE saas_commercial_plan_version_entitlements g
      SET semantics = 'unlimited', boolean_value = true, integer_limit_value = NULL
      FROM saas_commercial_entitlement_definitions d
      WHERE g.plan_version_id = '${DEV_MONTHLY_VERSION_ID}'::uuid
        AND g.entitlement_definition_id = d.id
        AND d.code_normalized = ${sqlQuote(normalized)};
    `);
  }
  assert(`module entitlements ensured (${MODULE_ENTITLEMENTS.length})`, true);
}

console.log("Bootstrapping Development Pilot Tenant sales-demo full access profile.");
assert("postgres container reachable", spawnSync("docker", ["ps", "--filter", "name=crevafactos-local-postgres-1", "--format", "{{.Names}}"], { encoding: "utf8" }).stdout.includes("crevafactos-local-postgres-1"));
ensureSubscriptionActive();
ensureModuleEntitlements();
ensurePermissions();
console.log("SALES-DEMO bootstrap complete.");

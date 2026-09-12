#!/usr/bin/env node
/**
 * verify-production-setup.mjs
 *
 * Verifies structural correctness of the PRODUCTION-3L setup/changeover feature:
 * entity, migration, RLS, constraints, API endpoints, web panel, audit/outbox taxonomy.
 */

import fs from "node:fs";
import path from "node:path";

const ROOT = path.resolve(process.cwd());
const PASS = "\u2705";
const FAIL = "\u274c";

let failures = 0;

function check(label, condition, detail) {
  if (condition) {
    console.log(`${PASS} ${label}`);
  } else {
    console.error(`${FAIL} ${label}${detail ? ` — ${detail}` : ""}`);
    failures++;
  }
}

function readText(relPath) {
  try {
    return fs.readFileSync(path.join(ROOT, relPath), "utf8");
  } catch {
    return null;
  }
}

// ── 1. ENTITY ─────────────────────────────────────────────────────────────────

const entity = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionSetupSession.cs",
);

check("ProductionSetupSession.cs exists", entity !== null);
check("Entity has SetupType", entity?.includes("public string SetupType") ?? false);
check("Entity has StartedAtUtc", entity?.includes("public DateTimeOffset StartedAtUtc") ?? false);
check("Entity has CompletedAtUtc nullable", entity?.includes("public DateTimeOffset? CompletedAtUtc") ?? false);
check("Entity has DurationSeconds nullable", entity?.includes("public long? DurationSeconds") ?? false);
check("Entity has PreviousMachineReference", entity?.includes("PreviousMachineReference") ?? false);
check("Entity has Version", entity?.includes("public uint Version") ?? false);

// ── 2. ENTITY CONFIGURATION ──────────────────────────────────────────────────

const config = readText(
  "packages/NevaxisFactOS.Persistence/Configurations/ProductionSetupSessionEntityConfiguration.cs",
);

check("ProductionSetupSessionEntityConfiguration.cs exists", config !== null);
check("Config maps to production_setup_sessions table", config?.includes("production_setup_sessions") ?? false);
check("Config has index on ProductionOrderId", config?.includes("ProductionOrderId") ?? false);

// ── 3. PERMISSION KEYS ───────────────────────────────────────────────────────

const permKeys = readText(
  "packages/NevaxisFactOS.Persistence/Authorization/ProductionExecutionPermissionKeys.cs",
);

check(
  "production.execution.setup permission key exists",
  permKeys?.includes("production.execution.setup") ?? false,
);

// ── 4. AUDIT/OUTBOX TAXONOMY ─────────────────────────────────────────────────

const auditActions = readText(
  "packages/NevaxisFactOS.Persistence/Audit/AuditActions.cs",
);
check(
  "AuditActions contains production.order.setup.start",
  auditActions?.includes("production.order.setup.start") ?? false,
);
check(
  "AuditActions contains production.order.setup.complete",
  auditActions?.includes("production.order.setup.complete") ?? false,
);

const outbox = readText(
  "packages/NevaxisFactOS.Persistence/Outbox/OutboxMessageTypes.cs",
);
check(
  "OutboxMessageTypes contains production-order-setup-started",
  outbox?.includes("production-order-setup-started") ?? false,
);
check(
  "OutboxMessageTypes contains production-order-setup-completed",
  outbox?.includes("production-order-setup-completed") ?? false,
);

// ── 5. MIGRATION ──────────────────────────────────────────────────────────────

const migration = readText(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260808100000_AddProductionExecutionOperationsAndCycles.cs",
);

check("3L migration file exists", migration !== null);
check("Migration creates production_setup_sessions table", migration?.includes("CREATE TABLE IF NOT EXISTS production_setup_sessions") ?? false);
check("Migration adds partial unique index for active setup", migration?.includes("uix_production_setup_sessions_active") ?? false);
check("Migration enables RLS on production_setup_sessions", migration?.includes("production_setup_sessions ENABLE ROW LEVEL SECURITY") ?? false);
check("Migration grants SELECT/INSERT/UPDATE on production_setup_sessions", migration?.includes("GRANT SELECT, INSERT, UPDATE ON TABLE production_setup_sessions") ?? false);
check("Migration has ck_production_setup_sessions_type constraint", migration?.includes("ck_production_setup_sessions_type") ?? false);
check("Migration validates allowed setup types", migration?.includes("initial-setup") ?? false);
check("Migration adds setup permission definitions", migration?.includes("production.execution.setup") ?? false);

// ── 6. REPOSITORY ─────────────────────────────────────────────────────────────

const repo = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOperationsRepository.cs",
);

check("Repo implements StartSetupSessionAsync", repo?.includes("StartSetupSessionAsync") ?? false);
check("Repo implements CompleteSetupSessionAsync", repo?.includes("CompleteSetupSessionAsync") ?? false);
check("Repo implements ListSetupSessionsAsync", repo?.includes("ListSetupSessionsAsync") ?? false);
check("Repo checks active setup uniqueness", repo?.includes("ActiveSetupExists") ?? false);

// ── 7. API ENDPOINTS ──────────────────────────────────────────────────────────

const api = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check("API registers POST setups/start endpoint", api?.includes("setups/start") ?? false);
check("API registers POST setups/{id}/complete endpoint", (api?.includes("setups/{setupId") ?? false) || (api?.includes("setups/{setupSession") ?? false));
check("API registers GET setups endpoint", api?.includes("/setups") ?? false);
check("API requires production.execution.setup permission", api?.includes("ProductionExecutionPermissionKeys.Setup") ?? false);

// ── 8. WEB DOMAIN TYPES ───────────────────────────────────────────────────────

const ops = readText(
  "apps/web/src/features/production-execution/domain/production-execution-operations.ts",
);

check("Domain defines ProductionSetupSessionSummary", ops?.includes("ProductionSetupSessionSummary") ?? false);
check("Domain defines SETUP_TYPES", ops?.includes("SETUP_TYPES") ?? false);
check("Domain has getSetupTypeLabel helper", ops?.includes("getSetupTypeLabel") ?? false);

// ── 9. WEB PANEL ──────────────────────────────────────────────────────────────

const panel = readText(
  "apps/web/src/features/production-execution/components/production-setup-panel.tsx",
);

check("production-setup-panel.tsx exists", panel !== null);
check("Panel has ProductionSetupPanel component", panel?.includes("export function ProductionSetupPanel") ?? false);
check("Panel shows SETUP & DEĞİŞİM label", panel?.includes("SETUP & DEĞİŞİM") ?? false);
check("Panel has Aktif Setup display", panel?.includes("Aktif Setup") ?? false);
check("Panel imports start/complete server actions", panel?.includes("startSetupSessionAction") ?? false);

// ── 10. WEB SERVER BRIDGE ─────────────────────────────────────────────────────

const writer = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check("Writer has startSetupSessionAction", writer?.includes("export async function startSetupSessionAction") ?? false);
check("Writer has completeSetupSessionAction", writer?.includes("export async function completeSetupSessionAction") ?? false);

// ── Summary ───────────────────────────────────────────────────────────────────

console.log();
if (failures === 0) {
  console.log(`${PASS} All setup verifications passed.`);
} else {
  console.error(`${FAIL} ${failures} setup verification(s) failed.`);
  process.exit(1);
}

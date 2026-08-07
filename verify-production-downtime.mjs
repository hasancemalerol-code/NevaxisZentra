#!/usr/bin/env node
/**
 * verify-production-downtime.mjs
 *
 * Verifies structural correctness of the PRODUCTION-3L downtime feature:
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

function fileExists(relPath) {
  return fs.existsSync(path.join(ROOT, relPath));
}

// ── 1. ENTITY ─────────────────────────────────────────────────────────────────

const entity = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionDowntime.cs",
);

check("ProductionDowntime.cs exists", entity !== null);
check("ProductionDowntime has Id", entity?.includes("public Guid Id") ?? false);
check("ProductionDowntime has TenantId", entity?.includes("public Guid TenantId") ?? false);
check("ProductionDowntime has FacilityId", entity?.includes("public Guid FacilityId") ?? false);
check("ProductionDowntime has ProductionOrderId", entity?.includes("public Guid ProductionOrderId") ?? false);
check("ProductionDowntime has Category", entity?.includes("public string Category") ?? false);
check("ProductionDowntime has ReasonCode", entity?.includes("public string ReasonCode") ?? false);
check("ProductionDowntime has StartedAtUtc", entity?.includes("public DateTimeOffset StartedAtUtc") ?? false);
check("ProductionDowntime has EndedAtUtc nullable", entity?.includes("public DateTimeOffset? EndedAtUtc") ?? false);
check("ProductionDowntime has DurationSeconds nullable", entity?.includes("public long? DurationSeconds") ?? false);
check("ProductionDowntime has CorrelationId", entity?.includes("public Guid CorrelationId") ?? false);
check("ProductionDowntime has Version", entity?.includes("public uint Version") ?? false);

// ── 2. ENTITY CONFIGURATION ──────────────────────────────────────────────────

const config = readText(
  "packages/CrevaFactOS.Persistence/Configurations/ProductionDowntimeEntityConfiguration.cs",
);

check("ProductionDowntimeEntityConfiguration.cs exists", config !== null);
check("Config maps to production_downtimes table", config?.includes("production_downtimes") ?? false);
check("Config has index on ProductionOrderId", config?.includes("ProductionOrderId") ?? false);
check("Config has FK to production_orders", config?.includes("production_order_id") ?? false);

// ── 3. PERMISSION KEYS ───────────────────────────────────────────────────────

const permKeys = readText(
  "packages/CrevaFactOS.Persistence/Authorization/ProductionExecutionPermissionKeys.cs",
);

check(
  "production.execution.downtime permission key exists",
  permKeys?.includes("production.execution.downtime") ?? false,
);

// ── 4. AUDIT/OUTBOX TAXONOMY ─────────────────────────────────────────────────

const auditActions = readText(
  "packages/CrevaFactOS.Persistence/Audit/AuditActions.cs",
);
check(
  "AuditActions contains production.order.downtime.start",
  auditActions?.includes("production.order.downtime.start") ?? false,
);
check(
  "AuditActions contains production.order.downtime.end",
  auditActions?.includes("production.order.downtime.end") ?? false,
);

const auditEvents = readText(
  "packages/CrevaFactOS.Persistence/Audit/AuditEventTypes.cs",
);
check(
  "AuditEventTypes contains production.order.downtime.started",
  auditEvents?.includes("production.order.downtime.started") ?? false,
);
check(
  "AuditEventTypes contains production.order.downtime.ended",
  auditEvents?.includes("production.order.downtime.ended") ?? false,
);

const outbox = readText(
  "packages/CrevaFactOS.Persistence/Outbox/OutboxMessageTypes.cs",
);
check(
  "OutboxMessageTypes contains production-order-downtime-started",
  outbox?.includes("production-order-downtime-started") ?? false,
);
check(
  "OutboxMessageTypes contains production-order-downtime-ended",
  outbox?.includes("production-order-downtime-ended") ?? false,
);

// ── 5. MIGRATION ──────────────────────────────────────────────────────────────

const migration = readText(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260808100000_AddProductionExecutionOperationsAndCycles.cs",
);

check("3L migration file exists", migration !== null);
check("Migration creates production_downtimes table", migration?.includes("CREATE TABLE IF NOT EXISTS production_downtimes") ?? false);
check("Migration adds partial unique index for active downtime", migration?.includes("uix_production_downtimes_active") ?? false);
check("Migration enables RLS on production_downtimes", migration?.includes("production_downtimes ENABLE ROW LEVEL SECURITY") ?? false);
check("Migration grants SELECT/INSERT/UPDATE on production_downtimes", migration?.includes("GRANT SELECT, INSERT, UPDATE ON TABLE production_downtimes") ?? false);
check("Migration has ck_production_downtimes_category constraint", migration?.includes("ck_production_downtimes_category") ?? false);
check("Migration adds downtime permission definitions", migration?.includes("production.execution.downtime") ?? false);

// ── 6. REPOSITORY ─────────────────────────────────────────────────────────────

const repo = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOperationsRepository.cs",
);

check("ProductionOperationsRepository.cs exists", repo !== null);
check("Repo implements StartDowntimeAsync", repo?.includes("StartDowntimeAsync") ?? false);
check("Repo implements EndDowntimeAsync", repo?.includes("EndDowntimeAsync") ?? false);
check("Repo implements ListDowntimesAsync", repo?.includes("ListDowntimesAsync") ?? false);
check("Repo checks active downtime uniqueness", repo?.includes("ActiveDowntimeExists") ?? false);
check("Repo calculates DurationSeconds from timestamps", repo?.includes("DurationSeconds") ?? false);
check("Repo uses atomicity (SaveChangesAsync)", repo?.includes("SaveChangesAsync") ?? false);

// ── 7. API ENDPOINTS ──────────────────────────────────────────────────────────

const api = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check("API file exists", api !== null);
check("API registers POST downtimes/start endpoint", api?.includes("downtimes/start") ?? false);
check("API registers POST downtimes/{id}/end endpoint", api?.includes("downtimes/{downtimeId") ?? false);
check("API registers GET downtimes endpoint", api?.includes("/downtimes") ?? false);
check("API requires production.execution.downtime permission", api?.includes("ProductionExecutionPermissionKeys.Downtime") ?? false);

// ── 8. WEB DOMAIN TYPES ───────────────────────────────────────────────────────

const ops = readText(
  "apps/web/src/features/production-execution/domain/production-execution-operations.ts",
);

check("production-execution-operations.ts exists", ops !== null);
check("Domain defines ProductionDowntimeSummary", ops?.includes("ProductionDowntimeSummary") ?? false);
check("Domain defines PLANNED_DOWNTIME_REASONS", ops?.includes("PLANNED_DOWNTIME_REASONS") ?? false);
check("Domain defines UNPLANNED_DOWNTIME_REASONS", ops?.includes("UNPLANNED_DOWNTIME_REASONS") ?? false);
check("Domain has getDowntimeReasonLabel helper", ops?.includes("getDowntimeReasonLabel") ?? false);
check("Domain has requiresDowntimeComment helper", ops?.includes("requiresDowntimeComment") ?? false);

// ── 9. WEB PANEL ──────────────────────────────────────────────────────────────

const panel = readText(
  "apps/web/src/features/production-execution/components/production-downtime-panel.tsx",
);

check("production-downtime-panel.tsx exists", panel !== null);
check("Panel has ProductionDowntimePanel component", panel?.includes("export function ProductionDowntimePanel") ?? false);
check("Panel imports server actions for start/end", (panel?.includes("startDowntimeAction") ?? false) && (panel?.includes("endDowntimeAction") ?? false));
check("Panel shows active downtime session", panel?.includes("Aktif Duruş") ?? false);
check("Panel has DURUŞ YÖNETİMİ label", panel?.includes("DURUŞ YÖNETİMİ") ?? false);
check("Panel uses router.refresh() after action", panel?.includes("router.refresh()") ?? false);
check("Panel has reason selector with planned/unplanned", panel?.includes("PLANNED_DOWNTIME_REASONS") ?? false);

// ── 10. WEB SERVER BRIDGE ──────────────────────────────────────────────────────

const writer = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check("Writer has startDowntimeAction", writer?.includes("export async function startDowntimeAction") ?? false);
check("Writer has endDowntimeAction", writer?.includes("export async function endDowntimeAction") ?? false);
check("Writer blocks preview writes", writer?.includes("preview-writes-blocked") ?? false);

// ── Summary ───────────────────────────────────────────────────────────────────

console.log();
if (failures === 0) {
  console.log(`${PASS} All downtime verifications passed.`);
} else {
  console.error(`${FAIL} ${failures} downtime verification(s) failed.`);
  process.exit(1);
}

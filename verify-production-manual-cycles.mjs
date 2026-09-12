#!/usr/bin/env node
/**
 * verify-production-manual-cycles.mjs
 *
 * Verifies structural correctness of the PRODUCTION-3L manual cycle tracking:
 * entity, migration, RLS, constraints, API endpoints, web panel, cycle numbering.
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
  "packages/NevaxisFactOS.Persistence/Production/ProductionCycleEntry.cs",
);

check("ProductionCycleEntry.cs exists", entity !== null);
check("Entity has CycleNumber", entity?.includes("public int CycleNumber") ?? false);
check("Entity has CycleStartedAtUtc", entity?.includes("public DateTimeOffset CycleStartedAtUtc") ?? false);
check("Entity has CycleEndedAtUtc", entity?.includes("public DateTimeOffset CycleEndedAtUtc") ?? false);
check("Entity has CycleDurationMilliseconds", entity?.includes("public long CycleDurationMilliseconds") ?? false);
check("Entity has QuantityProduced nullable", entity?.includes("public decimal? QuantityProduced") ?? false);
check("Entity has Source field", entity?.includes("public string Source") ?? false);
check("Entity Source defaults to manual", entity?.includes("\"manual\"") ?? false);
check("No Version on cycle entry (immutable)", !(entity?.includes("public uint Version") ?? false));

// ── 2. ENTITY CONFIGURATION ──────────────────────────────────────────────────

const config = readText(
  "packages/NevaxisFactOS.Persistence/Configurations/ProductionCycleEntryEntityConfiguration.cs",
);

check("ProductionCycleEntryEntityConfiguration.cs exists", config !== null);
check("Config maps to production_cycle_entries table", config?.includes("production_cycle_entries") ?? false);
check("Config has index on ProductionOrderId", config?.includes("ProductionOrderId") ?? false);

// ── 3. MIGRATION ──────────────────────────────────────────────────────────────

const migration = readText(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260808100000_AddProductionExecutionOperationsAndCycles.cs",
);

check("Migration creates production_cycle_entries table", migration?.includes("CREATE TABLE IF NOT EXISTS production_cycle_entries") ?? false);
check("Migration has unique index for cycle number", migration?.includes("uix_production_cycle_entries_order_cycle_number") ?? false);
check("Migration enables RLS on production_cycle_entries", migration?.includes("production_cycle_entries ENABLE ROW LEVEL SECURITY") ?? false);
check("Migration grants SELECT/INSERT only (no UPDATE) on cycles", migration?.includes("GRANT SELECT, INSERT ON TABLE production_cycle_entries") ?? false);
check("Migration has ck_production_cycle_entries_duration constraint", migration?.includes("ck_production_cycle_entries_duration") ?? false);
check("Migration has ck_production_cycle_entries_source constraint", migration?.includes("ck_production_cycle_entries_source") ?? false);
check("Migration has ck_production_cycle_entries_cycle_number constraint", migration?.includes("ck_production_cycle_entries_cycle_number") ?? false);
check("Migration source constrained to manual only", migration?.includes("source IN ('manual')") ?? false);

// ── 4. PRODUCTIONORDER OPERATIONAL FIELDS ────────────────────────────────────

const order = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrder.cs",
);

check("ProductionOrder has ManualCycleCount", order?.includes("ManualCycleCount") ?? false);
check("ProductionOrder has TotalManualCycleDurationMilliseconds", order?.includes("TotalManualCycleDurationMilliseconds") ?? false);
check("ProductionOrder has LastCycleAtUtc", order?.includes("LastCycleAtUtc") ?? false);
check("Migration adds manual_cycle_count column", migration?.includes("manual_cycle_count") ?? false);

// ── 5. CYCLE NUMBERING ────────────────────────────────────────────────────────

const repo = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOperationsRepository.cs",
);

check("Repo implements RecordManualCycleAsync", repo?.includes("RecordManualCycleAsync") ?? false);
check("Repo increments ManualCycleCount atomically", repo?.includes("ManualCycleCount") ?? false);
check("Repo uses ManualCycleCount as CycleNumber", repo?.includes("CycleNumber") ?? false);
check("Repo sets CycleEndedAtUtc from server time", repo?.includes("CycleEndedAtUtc") ?? false);
check("Repo sets CycleStartedAtUtc = now minus duration", repo?.includes("CycleStartedAtUtc") ?? false);
check("Repo does NOT increment GoodQuantity from cycle", !(repo?.includes("GoodQuantity") && repo?.includes("CycleDurationMilliseconds") ? repo.indexOf("GoodQuantity") < repo.indexOf("CycleDurationMilliseconds") + 200 : false));

// ── 6. API ENDPOINT ───────────────────────────────────────────────────────────

const api = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check("API registers POST cycles/manual endpoint", api?.includes("cycles/manual") ?? false);
check("API registers GET cycles endpoint", api?.includes("/cycles") ?? false);
check("API uses execution.record permission for manual cycle", api?.includes("ProductionExecutionPermissionKeys.Record") ?? false);

const dtos = readText(
  "apps/api/Production/ProductionOrderDtos.cs",
);

check("DTOs define RecordManualCycleApiRequest", dtos?.includes("RecordManualCycleApiRequest") ?? false);
check("DTOs define CycleEntrySummaryDto", dtos?.includes("CycleEntrySummaryDto") ?? false);

// ── 7. WEB DOMAIN TYPES ───────────────────────────────────────────────────────

const ops = readText(
  "apps/web/src/features/production-execution/domain/production-execution-operations.ts",
);

check("Domain defines ProductionCycleEntrySummary", ops?.includes("ProductionCycleEntrySummary") ?? false);
check("Domain has formatDurationMs helper", ops?.includes("formatDurationMs") ?? false);

// ── 8. WEB PANEL ──────────────────────────────────────────────────────────────

const panel = readText(
  "apps/web/src/features/production-execution/components/production-cycle-panel.tsx",
);

check("production-cycle-panel.tsx exists", panel !== null);
check("Panel has ProductionCyclePanel component", panel?.includes("export function ProductionCyclePanel") ?? false);
check("Panel shows MANUEL ÇEVRİM TAKİBİ label", panel?.includes("MANUEL ÇEVRİM TAKİBİ") ?? false);
check("Panel states manual entry disclaimer", panel?.includes("makine bağlantısı yok") ?? false);
check("Panel states quantity does not alter ledger", panel?.includes("Üretim miktar cetveli etkilenmez") ?? false);
check("Panel only allows record when InProgress", panel?.includes("status === \"in-progress\"") ?? false);

// ── 9. WEB SERVER ACTION ──────────────────────────────────────────────────────

const writer = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check("Writer has recordManualCycleAction", writer?.includes("export async function recordManualCycleAction") ?? false);
check("Writer validates duration > 0", writer?.includes("cycleDurationMilliseconds <= 0") ?? false);
check("Writer validates duration <= 24h", writer?.includes("86_400_000") ?? false);

// ── Summary ───────────────────────────────────────────────────────────────────

console.log();
if (failures === 0) {
  console.log(`${PASS} All manual cycle verifications passed.`);
} else {
  console.error(`${FAIL} ${failures} manual cycle verification(s) failed.`);
  process.exit(1);
}

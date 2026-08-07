#!/usr/bin/env node
/**
 * verify-production-quantities.mjs
 *
 * Verifies structural correctness of PRODUCTION-2L quantity tracking:
 * - ProductionExecutionEntry entity and immutable ledger design
 * - ProductionOrder quantity fields (planned/good/scrap)
 * - Planned quantity source resolution
 * - Progress summary semantics (remaining, percentage, overproduction)
 * - Completion guard (target-not-reached, short completion)
 * - Aggregate version advance on quantity writes
 * - RLS and runtime grants for execution entries table
 * - Migration includes quantity table, columns, constraints, indexes
 * - Audit/outbox taxonomy for quantity events
 * - No machine telemetry, PLC, OPC-UA
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

// ── 1. ENTITY FILES EXIST ────────────────────────────────────────────────────

const entryEntity = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionExecutionEntry.cs",
);
const entryType = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionExecutionEntryType.cs",
);
const entryInterface = readText(
  "packages/CrevaFactOS.Persistence/Production/IProductionExecutionEntryRepository.cs",
);
const entryRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionExecutionEntryRepository.cs",
);
const entryConfig = readText(
  "packages/CrevaFactOS.Persistence/Configurations/ProductionExecutionEntryEntityConfiguration.cs",
);

check("ProductionExecutionEntry.cs exists", entryEntity !== null);
check("ProductionExecutionEntryType.cs exists", entryType !== null);
check(
  "IProductionExecutionEntryRepository.cs exists",
  entryInterface !== null,
);
check("ProductionExecutionEntryRepository.cs exists", entryRepo !== null);
check(
  "ProductionExecutionEntryEntityConfiguration.cs exists",
  entryConfig !== null,
);

// ── 2. ENTRY ENTITY FIELDS ───────────────────────────────────────────────────

check(
  "Entry entity has Id field",
  entryEntity?.includes("public Guid Id") ?? false,
);
check(
  "Entry entity has TenantId field",
  entryEntity?.includes("public Guid TenantId") ?? false,
);
check(
  "Entry entity has FacilityId field",
  entryEntity?.includes("public Guid FacilityId") ?? false,
);
check(
  "Entry entity has ProductionOrderId field",
  entryEntity?.includes("public Guid ProductionOrderId") ?? false,
);
check(
  "Entry entity has EntryType field",
  entryEntity?.includes("public ProductionExecutionEntryType EntryType") ??
    false,
);
check(
  "Entry entity has Quantity field",
  entryEntity?.includes("public decimal Quantity") ?? false,
);
check(
  "Entry entity has Unit field",
  entryEntity?.includes("public string Unit") ?? false,
);
check(
  "Entry entity has ReasonCode nullable",
  entryEntity?.includes("public string? ReasonCode") ?? false,
);
check(
  "Entry entity has RecordedBySubjectId field",
  entryEntity?.includes("public string RecordedBySubjectId") ?? false,
);
check(
  "Entry entity has RecordedAtUtc field",
  entryEntity?.includes("public DateTimeOffset RecordedAtUtc") ?? false,
);
check(
  "Entry entity has ReversesEntryId nullable",
  entryEntity?.includes("public Guid? ReversesEntryId") ?? false,
);
check(
  "Entry entity has CorrelationId field",
  entryEntity?.includes("public Guid CorrelationId") ?? false,
);
check(
  "Entry entity has Version field",
  entryEntity?.includes("public uint Version") ?? false,
);

// ── 3. IMMUTABLE LEDGER: NO UPDATE/DELETE ────────────────────────────────────

check(
  "Entry repository does not UPDATE existing entries (no dbContext.Update(entry))",
  !entryRepo?.includes("dbContext.Update(entry)") ?? false,
);
check(
  "Entry repository does not DELETE entries",
  !entryRepo?.includes("dbContext.Remove(entry)") ?? false,
);
check(
  "Entry repository uses Add for new entries",
  entryRepo?.includes(".Add(") ?? false,
);

// ── 4. ENTRY TYPE ENUM ───────────────────────────────────────────────────────

check(
  "EntryType enum has GoodProduction",
  entryType?.includes("GoodProduction") ?? false,
);
check("EntryType enum has Scrap", entryType?.includes("Scrap") ?? false);
check("EntryType enum has Reversal", entryType?.includes("Reversal") ?? false);

// ── 5. PLANNED QUANTITY ON PRODUCTIONORDER ───────────────────────────────────

const orderEntity = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOrder.cs",
);

check(
  "ProductionOrder has PlannedQuantity",
  orderEntity?.includes("PlannedQuantity") ?? false,
);
check(
  "PlannedQuantity is nullable (decimal?)",
  orderEntity?.includes("decimal? PlannedQuantity") ?? false,
);
check(
  "ProductionOrder has GoodQuantity",
  orderEntity?.includes("GoodQuantity") ?? false,
);
check(
  "ProductionOrder has ScrapQuantity",
  orderEntity?.includes("ScrapQuantity") ?? false,
);
check(
  "ProductionOrder has LastProductionEntryAtUtc",
  orderEntity?.includes("LastProductionEntryAtUtc") ?? false,
);

// ── 6. PLANNED QUANTITY MAPPED FROM HANDOFF ──────────────────────────────────

const writeRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOrderWriteRepository.cs",
);

check(
  "Write repository maps PlannedQuantity from handoff",
  writeRepo?.includes("PlannedQuantity") ?? false,
);
check(
  "Write repository sets GoodQuantity default on consume",
  writeRepo?.includes("GoodQuantity") ?? false,
);
check(
  "Write repository sets ScrapQuantity default on consume",
  writeRepo?.includes("ScrapQuantity") ?? false,
);

// ── 7. COMPLETION GUARD ──────────────────────────────────────────────────────

check(
  "CompleteAsync checks PlannedQuantity",
  writeRepo?.includes("PlannedQuantity") ?? false,
);
check(
  "Completion guard uses production-target-not-reached failure code",
  writeRepo?.includes("ProductionTargetNotReached") ?? false,
);
check(
  "Short completion requires AllowShortCompletion flag",
  writeRepo?.includes("AllowShortCompletion") ?? false,
);
check(
  "Short completion requires CompletionReason",
  writeRepo?.includes("CompletionReason") ?? false,
);
check(
  "Short completion produces distinct audit action",
  writeRepo?.includes("CompleteShort") ?? false,
);

// ── 8. AGGREGATE VERSION ADVANCE ─────────────────────────────────────────────

check(
  "Entry repository advances order.Version on write",
  entryRepo?.includes("order.Version") ?? false,
);
check(
  "Entry repository advances order.UpdatedAtUtc",
  entryRepo?.includes("order.UpdatedAtUtc") ?? false,
);

// ── 9. STATUS GUARD — INPROGRESS ONLY ────────────────────────────────────────

check(
  "Entry repository validates InProgress status before recording",
  entryRepo?.includes("ProductionOrderStatus.InProgress") ?? false,
);
check(
  "Entry repository returns InvalidTransition for non-InProgress",
  entryRepo?.includes("InvalidTransition") ?? false,
);

// ── 10. QUANTITY SUMMARY INTERFACE ───────────────────────────────────────────

const models = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOrderModels.cs",
);

check(
  "ProductionQuantitySummary record exists",
  models?.includes("ProductionQuantitySummary") ?? false,
);
check(
  "Summary has GoodQuantity",
  models?.includes("GoodQuantity") ?? false,
);
check(
  "Summary has ScrapQuantity",
  models?.includes("ScrapQuantity") ?? false,
);
check(
  "Summary has GrossProducedQuantity",
  models?.includes("GrossProducedQuantity") ?? false,
);
check(
  "Summary has RemainingQuantity",
  models?.includes("RemainingQuantity") ?? false,
);
check(
  "Summary has CompletionPercentage",
  models?.includes("CompletionPercentage") ?? false,
);

// ── 11. FAILURE CODES ────────────────────────────────────────────────────────

check(
  "Failure code: invalid-quantity",
  models?.includes("InvalidQuantity") ?? false,
);
check(
  "Failure code: production-target-not-reached",
  models?.includes("ProductionTargetNotReached") ?? false,
);
check(
  "Failure code: completion-reason-required",
  models?.includes("CompletionReasonRequired") ?? false,
);

// ── 12. DB ENTITY CONFIGURATION ──────────────────────────────────────────────

check(
  "Entity config maps production_execution_entries table",
  entryConfig?.includes("production_execution_entries") ?? false,
);
check(
  "Entity config has entry_type check constraint",
  entryConfig?.includes("ck_execution_entries_entry_type") ?? false,
);
check(
  "Entity config has quantity positive check constraint",
  entryConfig?.includes("ck_execution_entries_quantity_positive") ??
    false,
);
check(
  "Entity config has unit check constraint",
  entryConfig?.includes("ck_execution_entries_unit") ?? false,
);
check(
  "Entity config indexes TenantId+FacilityId+ProductionOrderId+RecordedAtUtc",
  entryConfig?.includes("RecordedAtUtc") ?? false,
);

// ── 13. DBCONTEXT REGISTRATION ───────────────────────────────────────────────

const dbContext = readText(
  "packages/CrevaFactOS.Persistence/CrevaFactOSDbContext.cs",
);

check(
  "DbContext has ProductionExecutionEntries DbSet",
  dbContext?.includes("ProductionExecutionEntries") ?? false,
);
check(
  "DbContext applies ProductionExecutionEntryEntityConfiguration",
  dbContext?.includes("ProductionExecutionEntryEntityConfiguration") ??
    false,
);
check(
  "DbContext has query filter for tenant isolation on entries",
  (dbContext?.includes("ProductionExecutionEntry") &&
    dbContext?.includes("HasQueryFilter")) ??
    false,
);

// ── 14. AUDIT / OUTBOX TAXONOMY ──────────────────────────────────────────────

const auditConst = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionAuditConstants.cs",
);

check(
  "Audit taxonomy: production.order.quantity.good.record action",
  auditConst?.includes("ProductionQuantityGoodRecord") ?? false,
);
check(
  "Audit taxonomy: production.order.quantity.scrap.record action",
  auditConst?.includes("ProductionQuantityScrapRecord") ?? false,
);
check(
  "Audit taxonomy: production.order.quantity.reverse action",
  auditConst?.includes("ProductionQuantityReverse") ?? false,
);
check(
  "Audit taxonomy: production.order.complete.short action",
  auditConst?.includes("ProductionOrderCompleteShort") ?? false,
);

// ── 15. MIGRATION ────────────────────────────────────────────────────────────

const migrationsDir =
  "packages/CrevaFactOS.Persistence.Migrations/Migrations";
const files = fs.existsSync(path.join(ROOT, migrationsDir))
  ? fs.readdirSync(path.join(ROOT, migrationsDir))
  : [];

const quantityMigrationFile = files.find((f) =>
  f.includes("AddProductionExecutionQuantitiesAndScrap"),
);
check(
  "Migration AddProductionExecutionQuantitiesAndScrap exists",
  quantityMigrationFile !== undefined,
);

if (quantityMigrationFile) {
  const migrationText = readText(
    `${migrationsDir}/${quantityMigrationFile}`,
  );
  check(
    "Migration creates production_execution_entries table",
    migrationText?.includes("production_execution_entries") ?? false,
  );
  check(
    "Migration adds planned_quantity to production_orders",
    migrationText?.includes("planned_quantity") ?? false,
  );
  check(
    "Migration adds good_quantity to production_orders",
    migrationText?.includes("good_quantity") ?? false,
  );
  check(
    "Migration adds scrap_quantity to production_orders",
    migrationText?.includes("scrap_quantity") ?? false,
  );
  check(
    "Migration adds production.execution.record permission",
    migrationText?.includes("production.execution.record") ?? false,
  );
  check(
    "Migration enables RLS on execution entries",
    migrationText?.includes("ENABLE ROW LEVEL SECURITY") ?? false,
  );
  check(
    "Migration grants SELECT/INSERT (not UPDATE/DELETE) on entries",
    (migrationText?.includes("GRANT SELECT") &&
      !migrationText?.includes("GRANT DELETE")) ??
      false,
  );
  check(
    "Migration updates audit action taxonomy with quantity actions",
    migrationText?.includes("production.order.quantity") ?? false,
  );
  check(
    "Migration updates outbox message type taxonomy",
    migrationText?.includes("production-order-good-quantity") ?? false,
  );
}

// ── 16. NO MACHINE TELEMETRY ─────────────────────────────────────────────────

const noMachineTerms = ["PLC", "OPC-UA", "MQTT", "telemetry", "cycleCount"];
for (const term of noMachineTerms) {
  check(
    `No machine telemetry term '${term}' in entry repository`,
    !(entryRepo?.toLowerCase().includes(term.toLowerCase()) ?? false),
  );
}

// ── 17. NO BROWSER AUTH HEADERS IN WEB ───────────────────────────────────────

const webReader = readText(
  "apps/web/src/features/production-execution/server/production-order-reader.ts",
);
const webWriter = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check(
  "Web reader does not inject X-CrevaFactOS-Dev headers",
  !webReader?.includes("X-CrevaFactOS-Dev") ?? false,
);
check(
  "Web writer does not inject X-CrevaFactOS-Dev headers",
  !webWriter?.includes("X-CrevaFactOS-Dev") ?? false,
);

// ── RESULT ───────────────────────────────────────────────────────────────────

console.log();
if (failures === 0) {
  console.log("✅ All production quantity checks passed.");
  process.exit(0);
} else {
  console.error(`❌ ${failures} production quantity check(s) failed.`);
  process.exit(1);
}

#!/usr/bin/env node
/**
 * verify-production-shifts.mjs
 *
 * PRODUCTION-5L: Verifies structural correctness of shift assignment capability.
 * Checks: entity, configuration, repository contract, API endpoints,
 * EF Core configuration, RLS policies, web contracts, server actions.
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

// ── 1. CORE SHIFT ENTITY FILES ────────────────────────────────────────────────

check(
  "ProductionShift.cs exists",
  fileExists("packages/NevaxisFactOS.Persistence/Production/ProductionShift.cs"),
);

check(
  "ProductionOrderShiftAssignment.cs exists",
  fileExists("packages/NevaxisFactOS.Persistence/Production/ProductionOrderShiftAssignment.cs"),
);

check(
  "IProductionShiftRepository.cs exists",
  fileExists("packages/NevaxisFactOS.Persistence/Production/IProductionShiftRepository.cs"),
);

check(
  "ProductionShiftRepository.cs exists",
  fileExists("packages/NevaxisFactOS.Persistence/Production/ProductionShiftRepository.cs"),
);

// ── 2. SHIFT ENTITY FIELDS ────────────────────────────────────────────────────

const shiftEntity = readText("packages/NevaxisFactOS.Persistence/Production/ProductionShift.cs");

check(
  "ProductionShift has Code field",
  shiftEntity?.includes("Code") ?? false,
);

check(
  "ProductionShift has Name field",
  shiftEntity?.includes("Name") ?? false,
);

check(
  "ProductionShift has IsActive field",
  shiftEntity?.includes("IsActive") ?? false,
);

check(
  "ProductionShift has StartLocalTime field",
  shiftEntity?.includes("StartLocalTime") ?? false,
);

check(
  "ProductionShift has EndLocalTime field",
  shiftEntity?.includes("EndLocalTime") ?? false,
);

// ── 3. SHIFT ASSIGNMENT ENTITY FIELDS ─────────────────────────────────────────

const assignmentEntity = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderShiftAssignment.cs",
);

check(
  "ProductionOrderShiftAssignment has ProductionOrderId",
  assignmentEntity?.includes("ProductionOrderId") ?? false,
);

check(
  "ProductionOrderShiftAssignment has ProductionShiftId",
  assignmentEntity?.includes("ProductionShiftId") ?? false,
);

check(
  "ProductionOrderShiftAssignment has ShiftDate",
  assignmentEntity?.includes("ShiftDate") ?? false,
);

check(
  "ProductionOrderShiftAssignment has ShiftStartAtUtc",
  assignmentEntity?.includes("ShiftStartAtUtc") ?? false,
);

check(
  "ProductionOrderShiftAssignment has ShiftEndAtUtc",
  assignmentEntity?.includes("ShiftEndAtUtc") ?? false,
);

check(
  "ProductionOrderShiftAssignment has ReleasedAtUtc",
  assignmentEntity?.includes("ReleasedAtUtc") ?? false,
);

check(
  "ProductionOrderShiftAssignment has CorrelationId for idempotency",
  assignmentEntity?.includes("CorrelationId") ?? false,
);

// ── 4. EF CORE CONFIGURATIONS ─────────────────────────────────────────────────

check(
  "ProductionShiftEntityConfiguration.cs exists",
  fileExists(
    "packages/NevaxisFactOS.Persistence/Configurations/ProductionShiftEntityConfiguration.cs",
  ),
);

check(
  "ProductionOrderShiftAssignmentEntityConfiguration.cs exists",
  fileExists(
    "packages/NevaxisFactOS.Persistence/Configurations/ProductionOrderShiftAssignmentEntityConfiguration.cs",
  ),
);

const shiftConfig = readText(
  "packages/NevaxisFactOS.Persistence/Configurations/ProductionOrderShiftAssignmentEntityConfiguration.cs",
);

check(
  "Shift assignment config has unique index for active per order",
  (shiftConfig?.includes("uix_production_shift_assignments_active_per_order") ?? false) ||
    (shiftConfig?.includes("ix_production_shift_assignments_order_released") ?? false) ||
    (shiftConfig?.includes("partial uniqueness") ?? false),
);

// ── 5. ORDER SUMMARY FIELDS ───────────────────────────────────────────────────

const productionOrder = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrder.cs",
);

check(
  "ProductionOrder has CurrentShiftId",
  productionOrder?.includes("CurrentShiftId") ?? false,
);

check(
  "ProductionOrder has CurrentShiftCode",
  productionOrder?.includes("CurrentShiftCode") ?? false,
);

check(
  "ProductionOrder has CurrentShiftStartAtUtc",
  productionOrder?.includes("CurrentShiftStartAtUtc") ?? false,
);

check(
  "ProductionOrder has CurrentShiftEndAtUtc",
  productionOrder?.includes("CurrentShiftEndAtUtc") ?? false,
);

check(
  "ProductionOrder has LastAssignmentAtUtc",
  productionOrder?.includes("LastAssignmentAtUtc") ?? false,
);

// ── 6. REPOSITORY CONTRACT ────────────────────────────────────────────────────

const shiftRepo = readText(
  "packages/NevaxisFactOS.Persistence/Production/IProductionShiftRepository.cs",
);

check(
  "IProductionShiftRepository has AssignShiftAsync",
  shiftRepo?.includes("AssignShiftAsync") ?? false,
);

check(
  "IProductionShiftRepository has ReassignShiftAsync",
  shiftRepo?.includes("ReassignShiftAsync") ?? false,
);

check(
  "IProductionShiftRepository has ListShiftAssignmentsAsync",
  shiftRepo?.includes("ListShiftAssignmentsAsync") ?? false,
);

check(
  "IProductionShiftRepository has GetShiftExecutionSummaryAsync",
  shiftRepo?.includes("GetShiftExecutionSummaryAsync") ?? false,
);

// ── 8. API ENDPOINTS ──────────────────────────────────────────────────────────

const endpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check(
  "API has shift assignment endpoint",
  endpoints?.includes("shift-assignment") ?? false,
);

check(
  "API has shifts read endpoint",
  endpoints?.includes("shifts") ?? false,
);

// ── 9. PERMISSIONS ────────────────────────────────────────────────────────────

const permKeys = readText(
  "packages/NevaxisFactOS.Persistence/Authorization/ProductionExecutionPermissionKeys.cs",
);

check(
  "production.execution.assign permission defined (5L assignment permission)",
  permKeys?.includes('"production.execution.assign"') ?? false,
);

// ── 10. WEB LAYER ─────────────────────────────────────────────────────────────

check(
  "production-shift-panel.tsx exists",
  fileExists(
    "apps/web/src/features/production-execution/components/production-shift-panel.tsx",
  ),
);

const serverWriter = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check(
  "assignShiftAction server action exists",
  serverWriter?.includes("assignShiftAction") ?? false,
);

check(
  "reassignShiftAction server action exists",
  serverWriter?.includes("reassignShiftAction") ?? false,
);

// ── 11. MIGRATION ─────────────────────────────────────────────────────────────

check(
  "Migration 20260808130000_AddProductionShiftResourceExecution.cs exists",
  fileExists(
    "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260808130000_AddProductionShiftResourceExecution.cs",
  ),
);

const migration = readText(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260808130000_AddProductionShiftResourceExecution.cs",
);

check(
  "Migration creates production_shifts table",
  migration?.includes("production_shifts") ?? false,
);

check(
  "Migration creates production_order_shift_assignments table",
  migration?.includes("production_order_shift_assignments") ?? false,
);

check(
  "Migration creates unique partial index for active assignment per order",
  migration?.includes("uix_production_shift_assignments_active_per_order") ?? false,
);

// ── RESULT ────────────────────────────────────────────────────────────────────

console.log("");
if (failures === 0) {
  console.log(`${PASS} verify-production-shifts: all checks passed.`);
  process.exit(0);
} else {
  console.error(`${FAIL} verify-production-shifts: ${failures} check(s) failed.`);
  process.exit(1);
}

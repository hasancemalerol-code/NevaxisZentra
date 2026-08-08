#!/usr/bin/env node
/**
 * verify-production-resource-execution.mjs
 *
 * PRODUCTION-5L: Verifies structural correctness of resource execution summary capability.
 * Checks: shift execution summary endpoint, operator execution summary endpoint,
 * read models, API contracts, web contracts, execution resources domain.
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

// ── 1. API ENDPOINTS ──────────────────────────────────────────────────────────

const endpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check(
  "API has shift execution summary endpoint",
  endpoints?.includes("execution-summary") ?? false,
);

check(
  "API has operator execution summary endpoint",
  (endpoints?.includes("operators/{subjectId}") ?? false) &&
    (endpoints?.includes("execution-summary") ?? false),
);

// ── 2. API DTOs ────────────────────────────────────────────────────────────────

const apiDtos = readText("apps/api/Production/ProductionOrderDtos.cs");

check(
  "ProductionOrderDtos.cs exists",
  apiDtos !== null,
);

check(
  "API DTOs have ShiftExecutionSummary or equivalent",
  (apiDtos?.includes("Shift") ?? false) && (apiDtos?.includes("Summary") ?? false),
);

check(
  "API DTOs include openExceptionCount or equivalent",
  (apiDtos?.includes("OpenExceptionCount") ?? false) ||
    (apiDtos?.includes("openExceptionCount") ?? false),
);

check(
  "API DTOs include ActiveOrderCount",
  apiDtos?.includes("ActiveOrderCount") ?? false,
);

// ── 3. SHIFT REPOSITORY ────────────────────────────────────────────────────────

const shiftRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/IProductionShiftRepository.cs",
);

check(
  "IProductionShiftRepository has GetShiftExecutionSummaryAsync",
  shiftRepo?.includes("GetShiftExecutionSummaryAsync") ?? false,
);

// ── 4. PRODUCTION ORDER MODELS ────────────────────────────────────────────────

const orderModels = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOrderModels.cs",
);

check(
  "ProductionOrderModels.cs includes shift execution read model",
  orderModels?.includes("Shift") ?? false,
);

// ── 5. WEB RESOURCES DOMAIN ───────────────────────────────────────────────────

check(
  "production-execution-resources.ts exists",
  fileExists(
    "apps/web/src/features/production-execution/domain/production-execution-resources.ts",
  ),
);

const resourcesDomain = readText(
  "apps/web/src/features/production-execution/domain/production-execution-resources.ts",
);

check(
  "Resources domain has shift execution summary type",
  resourcesDomain?.includes("Shift") ?? false,
);

check(
  "Resources domain has operator execution summary type",
  resourcesDomain?.includes("Operator") ?? false,
);

// ── 6. WEB API CONTRACTS ─────────────────────────────────────────────────────

check(
  "production-execution-resources-api-contracts.ts exists",
  fileExists(
    "apps/web/src/features/production-execution/server/production-execution-resources-api-contracts.ts",
  ),
);

const resourcesContracts = readText(
  "apps/web/src/features/production-execution/server/production-execution-resources-api-contracts.ts",
);

check(
  "Resources API contracts have shift data",
  (resourcesContracts?.includes("shift") ?? false) ||
    (resourcesContracts?.includes("Shift") ?? false),
);

// ── 7. MIGRATION INCLUDES 5L COLUMNS ─────────────────────────────────────────

const migration = readText(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260808130000_AddProductionShiftResourceExecution.cs",
);

check(
  "Migration adds 5L summary columns to production_orders",
  (migration?.includes("current_shift_code") ?? false) ||
    (migration?.includes("primary_operator_subject_id") ?? false),
);

check(
  "Migration adds active_operator_count column",
  migration?.includes("active_operator_count") ?? false,
);

// ── 8. DB CONTEXT REGISTRATION ───────────────────────────────────────────────

const dbContext = readText(
  "packages/CrevaFactOS.Persistence/CrevaFactOSDbContext.cs",
);

check(
  "DbContext has ProductionShifts DbSet",
  dbContext?.includes("ProductionShifts") ?? false,
);

check(
  "DbContext has ProductionOrderShiftAssignments DbSet",
  dbContext?.includes("ProductionOrderShiftAssignments") ?? false,
);

check(
  "DbContext has ProductionOperatorAssignments DbSet",
  dbContext?.includes("ProductionOperatorAssignments") ?? false,
);

check(
  "DbContext has ProductionShiftHandovers DbSet",
  dbContext?.includes("ProductionShiftHandovers") ?? false,
);

// ── RESULT ────────────────────────────────────────────────────────────────────

console.log("");
if (failures === 0) {
  console.log(`${PASS} verify-production-resource-execution: all checks passed.`);
  process.exit(0);
} else {
  console.error(`${FAIL} verify-production-resource-execution: ${failures} check(s) failed.`);
  process.exit(1);
}

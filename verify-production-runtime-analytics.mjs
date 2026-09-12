#!/usr/bin/env node
/**
 * verify-production-runtime-analytics.mjs
 *
 * Verifies structural correctness of the PRODUCTION-3L runtime analytics:
 * ProductionRuntimeSummary model, derived fields, API endpoint, web component,
 * net running time semantics, fallback from denormalized order fields.
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

// ── 1. PERSISTENCE MODEL ──────────────────────────────────────────────────────

const models = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderModels.cs",
);

check("ProductionOrderModels.cs exists", models !== null);
check("Models define ProductionRuntimeSummary", models?.includes("ProductionRuntimeSummary") ?? false);
check("Models have CurrentElapsedSeconds", models?.includes("CurrentElapsedSeconds") ?? false);
check("Models have NetRunningSeconds", models?.includes("NetRunningSeconds") ?? false);
check("Models have TotalDowntimeSeconds", models?.includes("TotalDowntimeSeconds") ?? false);
check("Models have TotalSetupSeconds", models?.includes("TotalSetupSeconds") ?? false);
check("Models have ManualCycleCount", models?.includes("ManualCycleCount") ?? false);
check("Models have AverageManualCycleMilliseconds", models?.includes("AverageManualCycleMilliseconds") ?? false);
check("Models have LastCycleAtUtc", models?.includes("LastCycleAtUtc") ?? false);
check("Models have ActiveDowntime", models?.includes("ActiveDowntime") ?? false);
check("Models have ActiveSetup", models?.includes("ActiveSetup") ?? false);

// ── 2. RUNTIME SUMMARY CALCULATION ───────────────────────────────────────────

const repo = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOperationsRepository.cs",
);

check("Repo implements GetRuntimeSummaryAsync", repo?.includes("GetRuntimeSummaryAsync") ?? false);
check("Repo calculates elapsed time from ActualStartAtUtc", repo?.includes("ActualStartAtUtc") ?? false);
check("Repo calculates net running seconds", (repo?.includes("NetRunningSeconds") ?? false) || (repo?.includes("netRunningSeconds") ?? false));
check("Repo includes active downtime in summary", repo?.includes("ActiveDowntime") ?? false);
check("Repo includes active setup in summary", repo?.includes("ActiveSetup") ?? false);

// ── 3. PRODUCTIONORDER DENORMALIZED FIELDS ────────────────────────────────────

const order = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrder.cs",
);

check("ProductionOrder has TotalDowntimeSeconds", order?.includes("TotalDowntimeSeconds") ?? false);
check("ProductionOrder has TotalSetupSeconds", order?.includes("TotalSetupSeconds") ?? false);
check("ProductionOrder has ManualCycleCount", order?.includes("ManualCycleCount") ?? false);
check("ProductionOrder has TotalManualCycleDurationMilliseconds", order?.includes("TotalManualCycleDurationMilliseconds") ?? false);

const migration = readText(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260808100000_AddProductionExecutionOperationsAndCycles.cs",
);
check("Migration adds total_downtime_seconds column", migration?.includes("total_downtime_seconds") ?? false);
check("Migration adds total_setup_seconds column", migration?.includes("total_setup_seconds") ?? false);
check("Migration adds manual_cycle_count column", migration?.includes("manual_cycle_count") ?? false);
check("Migration adds total_manual_cycle_duration_ms column", migration?.includes("total_manual_cycle_duration_ms") ?? false);

// ── 4. API ENDPOINT ───────────────────────────────────────────────────────────

const api = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check("API registers GET runtime-summary endpoint", api?.includes("runtime-summary") ?? false);

const dtos = readText(
  "apps/api/Production/ProductionOrderDtos.cs",
);

check("DTOs define ProductionRuntimeSummaryDto", dtos?.includes("ProductionRuntimeSummaryDto") ?? false);
check("RuntimeSummaryDto has NetRunningSeconds", dtos?.includes("NetRunningSeconds") ?? false);
check("RuntimeSummaryDto has AverageManualCycleMilliseconds", dtos?.includes("AverageManualCycleMilliseconds") ?? false);

// ── 5. WEB DOMAIN TYPES ───────────────────────────────────────────────────────

const ops = readText(
  "apps/web/src/features/production-execution/domain/production-execution-operations.ts",
);

check("Domain defines ProductionRuntimeSummary", ops?.includes("ProductionRuntimeSummary") ?? false);
check("Domain has formatDuration helper", ops?.includes("export function formatDuration") ?? false);
check("Domain has formatDurationMs helper", ops?.includes("export function formatDurationMs") ?? false);
check("Domain has formatUtcShort helper", ops?.includes("export function formatUtcShort") ?? false);

// ── 6. WEB API CONTRACTS ──────────────────────────────────────────────────────

const contracts = readText(
  "apps/web/src/features/production-execution/server/production-execution-operations-api-contracts.ts",
);

check("Operations API contracts file exists", contracts !== null);
check("Contracts have parseRuntimeSummaryApiResponse", contracts?.includes("parseRuntimeSummaryApiResponse") ?? false);
check("Contracts validate currentElapsedSeconds", contracts?.includes("currentElapsedSeconds") ?? false);
check("Contracts validate netRunningSeconds", contracts?.includes("netRunningSeconds") ?? false);
check("Contracts parse nested activeDowntime", contracts?.includes("activeDowntime") ?? false);
check("Contracts parse nested activeSetup", contracts?.includes("activeSetup") ?? false);

// ── 7. WEB COMPONENT ─────────────────────────────────────────────────────────

const analytics = readText(
  "apps/web/src/features/production-execution/components/production-runtime-analytics.tsx",
);

check("production-runtime-analytics.tsx exists", analytics !== null);
check("Component has ProductionRuntimeAnalytics", analytics?.includes("export function ProductionRuntimeAnalytics") ?? false);
check("Component shows Geçen Süre tile", analytics?.includes("Geçen Süre") ?? false);
check("Component shows Net Çalışma tile", analytics?.includes("Net Çalışma") ?? false);
check("Component shows Toplam Duruş tile", analytics?.includes("Toplam Duruş") ?? false);
check("Component shows Setup Süresi tile", analytics?.includes("Setup Süresi") ?? false);
check("Component shows Manuel Çevrim tile", analytics?.includes("Manuel Çevrim") ?? false);
check("Component shows Ort. Çevrim tile", analytics?.includes("Ort. Çevrim") ?? false);
check("Component does not mention OEE", !(analytics?.includes("OEE") ?? false));

// ── 8. DETAIL PAGE INTEGRATION ───────────────────────────────────────────────

const page = readText(
  "apps/web/src/app/(app)/uretim/[orderId]/page.tsx",
);

check("Detail page exists", page !== null);
check("Page imports ProductionRuntimeAnalytics", page?.includes("ProductionRuntimeAnalytics") ?? false);
check("Page imports readRuntimeSummary", page?.includes("readRuntimeSummary") ?? false);
check("Page passes runtimeSummary to analytics component", page?.includes("runtimeSummary") ?? false);
check("Page shows ops panels (downtime/setup/cycle)", page?.includes("production-order-detail-page__ops-panels") ?? false);
check("Page has fallback summary from denormalized order fields", page?.includes("totalDowntimeSeconds") ?? false);

// ── 9. NO OEE CLAIMS ─────────────────────────────────────────────────────────

check(
  "Analytics component does not claim OEE",
  !(analytics?.includes("OEE") ?? false),
  "Component must not use OEE terminology",
);

// ── Summary ───────────────────────────────────────────────────────────────────

console.log();
if (failures === 0) {
  console.log(`${PASS} All runtime analytics verifications passed.`);
} else {
  console.error(`${FAIL} ${failures} runtime analytics verification(s) failed.`);
  process.exit(1);
}

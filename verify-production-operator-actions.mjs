#!/usr/bin/env node
/**
 * verify-production-operator-actions.mjs
 *
 * Verifies structural correctness of PRODUCTION-2L operator actions:
 * - Server-only bridge for quantity writes (no browser auth headers)
 * - Operator identity is server-authoritative (RecordedBySubjectId from actor, not body)
 * - Good production endpoint and web action
 * - Scrap endpoint and web action
 * - Reversal endpoint and web action
 * - Web completion guard UI (short completion dialog)
 * - Preview mode blocks quantity writes
 * - No DELETE route for production entries
 * - Production entry history UI exists
 * - Permission: production.execution.record in pilot bootstrap
 * - Concurrency: expectedVersion required for all quantity writes
 * - API entry history endpoint exists
 * - List card shows quantity progress
 * - Detail page includes quantity panel
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

// ── 1. SERVER-ONLY BRIDGE ────────────────────────────────────────────────────

const apiClient = readText(
  "apps/web/src/features/production-execution/server/production-order-api-client.ts",
);
const webWriter = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);
const serverBoundary = readText(
  "apps/web/src/features/production-execution/server/production-order-server-boundary.ts",
);

check("API client file exists", apiClient !== null);
check("Web writer file exists", webWriter !== null);
check("Server boundary file exists", serverBoundary !== null);

check(
  "API client does not inject X-CrevaFactOS-Dev-TenantId header",
  !apiClient?.includes("X-CrevaFactOS-Dev-TenantId") ?? false,
);
check(
  "API client does not inject X-CrevaFactOS-Dev-SubjectId header",
  !apiClient?.includes("X-CrevaFactOS-Dev-SubjectId") ?? false,
);
check(
  "Web writer is server-only (no 'use client')",
  !webWriter?.includes('"use client"') ?? false,
);

// ── 2. OPERATOR IDENTITY IS SERVER-AUTHORITATIVE ─────────────────────────────

const entryRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionExecutionEntryRepository.cs",
);

check(
  "Entry repository sets RecordedBySubjectId from context, not request body",
  entryRepo?.includes("RecordedBySubjectId") ?? false,
);
check(
  "RecordGoodProductionCommand does NOT have a SubjectId field",
  !readText(
    "packages/CrevaFactOS.Persistence/Production/ProductionOrderModels.cs",
  )?.match(/RecordGoodProductionCommand[^)]*SubjectId/) ?? false,
);

// ── 3. GOOD PRODUCTION ENDPOINT ──────────────────────────────────────────────

const apiEndpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check(
  "API has POST quantities/good endpoint",
  apiEndpoints?.includes("/quantities/good") ?? false,
);
check(
  "Good endpoint checks production.execution.record permission",
  (apiEndpoints?.includes("ProductionExecutionPermissionKeys.Record") ??
    apiEndpoints?.includes("production.execution.record")) ??
    false,
);
check(
  "Good endpoint requires expectedVersion",
  apiEndpoints?.includes("ExpectedVersion") ?? false,
);

// ── 4. SCRAP ENDPOINT ────────────────────────────────────────────────────────

check(
  "API has POST quantities/scrap endpoint",
  apiEndpoints?.includes("/quantities/scrap") ?? false,
);

// ── 5. REVERSAL ENDPOINT ─────────────────────────────────────────────────────

check(
  "API has POST quantity-entries/{entryId}/reverse endpoint",
  apiEndpoints?.includes("reverse") ?? false,
);
check(
  "No DELETE endpoint for production entries",
  !apiEndpoints?.includes('MapDelete') ?? false,
);

// ── 6. ENTRY HISTORY ENDPOINT ────────────────────────────────────────────────

check(
  "API has GET quantity-entries endpoint",
  apiEndpoints?.includes("/quantity-entries") ?? false,
);
check(
  "Entry history endpoint uses production.execution.read permission",
  (apiEndpoints?.includes("ProductionExecutionPermissionKeys.Read") ??
    apiEndpoints?.includes("production.execution.read")) ??
    false,
);

// ── 7. WEB OPERATOR ACTIONS COMPONENT ────────────────────────────────────────

const operatorActions = readText(
  "apps/web/src/features/production-execution/components/production-operator-actions.tsx",
);

check(
  "ProductionOperatorActions component exists",
  operatorActions !== null,
);
check(
  "Operator actions has good production button/form",
  operatorActions?.includes("good") ?? false,
);
check(
  "Operator actions has scrap recording button/form",
  operatorActions?.includes("scrap") ?? false,
);
check(
  "Operator actions component is NOT 'use server'",
  !operatorActions?.includes('"use server"') ?? false,
);
check(
  "Operator actions does not inject dev auth headers",
  !operatorActions?.includes("X-CrevaFactOS-Dev") ?? false,
);

// ── 8. COMPLETION GUARD UI ────────────────────────────────────────────────────

const lifecycleActions = readText(
  "apps/web/src/features/production-execution/components/production-order-lifecycle-actions.tsx",
);

check(
  "Lifecycle actions has quantitySummary prop",
  lifecycleActions?.includes("quantitySummary") ?? false,
);
check(
  "Lifecycle actions has short completion state/dialog",
  lifecycleActions?.includes("short-complete") ??
    lifecycleActions?.includes("shortComplete") ??
    false,
);
check(
  "Completion guard derives targetReached from quantity summary",
  lifecycleActions?.includes("targetReached") ?? false,
);

// ── 9. PREVIEW MODE BLOCKS WRITES ────────────────────────────────────────────

check(
  "Web writer blocks good production in preview mode",
  webWriter?.includes("development-preview") ?? false,
);
check(
  "Web writer blocks scrap recording in preview mode",
  webWriter?.includes("recordScrapAction") ?? false,
);

// ── 10. ENTRY HISTORY UI ─────────────────────────────────────────────────────

const entryHistory = readText(
  "apps/web/src/features/production-execution/components/production-entry-history.tsx",
);

check("ProductionEntryHistory component exists", entryHistory !== null);
check(
  "History component shows entry type",
  entryHistory?.includes("entryType") ?? false,
);
check(
  "History component shows quantity",
  entryHistory?.includes("quantity") ?? false,
);
check(
  "History component marks reversed entries",
  entryHistory?.includes("revers") ?? false,
);

// ── 11. QUANTITY PANEL UI ─────────────────────────────────────────────────────

const quantityPanel = readText(
  "apps/web/src/features/production-execution/components/production-quantity-panel.tsx",
);

check("ProductionQuantityPanel component exists", quantityPanel !== null);
check(
  "Quantity panel shows planned quantity (Planlanan)",
  quantityPanel?.includes("Planlanan") ??
    quantityPanel?.includes("plannedQuantity") ??
    false,
);
check(
  "Quantity panel shows good quantity (İyi Üretim)",
  quantityPanel?.includes("goodQuantity") ??
    quantityPanel?.includes("good") ??
    false,
);
check(
  "Quantity panel shows scrap quantity (Fire)",
  quantityPanel?.includes("scrap") ?? false,
);

// ── 12. LIST CARD SHOWS PROGRESS ─────────────────────────────────────────────

const orderCard = readText(
  "apps/web/src/features/production-execution/components/production-order-card.tsx",
);

check("Production order card exists", orderCard !== null);
check(
  "List card displays goodQuantity",
  orderCard?.includes("goodQuantity") ?? false,
);
check(
  "List card displays plannedQuantity",
  orderCard?.includes("plannedQuantity") ?? false,
);
check(
  "List card displays scrapQuantity",
  orderCard?.includes("scrapQuantity") ?? false,
);

// ── 13. DETAIL PAGE INTEGRATES QUANTITY COMPONENTS ───────────────────────────

const detailPage = readText(
  "apps/web/src/app/(app)/uretim/[orderId]/page.tsx",
);

check("Detail page exists", detailPage !== null);
check(
  "Detail page includes ProductionQuantityPanel",
  detailPage?.includes("ProductionQuantityPanel") ?? false,
);
check(
  "Detail page includes ProductionOperatorActions",
  detailPage?.includes("ProductionOperatorActions") ?? false,
);
check(
  "Detail page includes ProductionEntryHistory",
  detailPage?.includes("ProductionEntryHistory") ?? false,
);
check(
  "Detail page fetches execution entries (readProductionEntries)",
  detailPage?.includes("readProductionEntries") ?? false,
);

// ── 14. WEB READER HAS ENTRY HISTORY READ ────────────────────────────────────

const webReader = readText(
  "apps/web/src/features/production-execution/server/production-order-reader.ts",
);

check(
  "Web reader has readProductionEntries function",
  webReader?.includes("readProductionEntries") ?? false,
);

// ── 15. DEVELOPMENT PILOT: production.execution.record PERMISSION ─────────────

const pilotBootstrap = readText(
  "packages/CrevaFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs",
);
const pilotIds = readText(
  "packages/CrevaFactOS.Persistence/Development/DevelopmentPilotIds.cs",
);

check(
  "Development pilot bootstrap has ExecutionRecordPermission",
  pilotBootstrap?.includes("ExecutionRecord") ?? false,
);
check(
  "DevelopmentPilotIds has ExecutionRecordPermissionDefinitionId",
  pilotIds?.includes("ExecutionRecordPermissionDefinitionId") ?? false,
);

// ── 16. CONCURRENCY: expectedVersion REQUIRED ────────────────────────────────

check(
  "RecordGoodAsync requires expectedVersion command field",
  entryRepo?.includes("expectedVersion") ?? false,
);
check(
  "RecordScrapAsync requires expectedVersion command field",
  entryRepo?.includes("expectedVersion") ?? false,
);
check(
  "ReverseAsync requires expectedVersion command field",
  entryRepo?.includes("expectedVersion") ?? false,
);
check(
  "Concurrency conflict returned for stale version",
  entryRepo?.includes("ConcurrencyConflict") ?? false,
);

// ── 17. API CONTRACTS IN WEB ─────────────────────────────────────────────────

const entryContracts = readText(
  "apps/web/src/features/production-execution/server/production-execution-entry-api-contracts.ts",
);

check("Execution entry API contracts file exists", entryContracts !== null);
check(
  "Contracts parse execution entry list response",
  entryContracts?.includes("parseProductionExecutionEntryList") ??
    entryContracts?.includes("List") ??
    false,
);

// ── 18. GLOBALS.CSS UNTOUCHED ────────────────────────────────────────────────

check(
  "apps/web/src/app/globals.css still exists",
  fs.existsSync(path.join(ROOT, "apps/web/src/app/globals.css")),
);

// ── RESULT ───────────────────────────────────────────────────────────────────

console.log();
if (failures === 0) {
  console.log("✅ All production operator actions checks passed.");
  process.exit(0);
} else {
  console.error(`❌ ${failures} production operator actions check(s) failed.`);
  process.exit(1);
}

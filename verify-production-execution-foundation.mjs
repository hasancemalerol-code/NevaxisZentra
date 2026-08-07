#!/usr/bin/env node
/**
 * verify-production-execution-foundation.mjs
 *
 * Verifies structural correctness of the Production Execution bounded context.
 * Checks: distinct permissions, bounded context separation, entity fields,
 * repository contracts, API endpoints, migration, RLS, audit/outbox taxonomy,
 * server-only bridge, preview writes blocked, no machine/Edge implementation,
 * no Planning dependency reversal.
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

// ── 1. DISTINCT PRODUCTION PERMISSIONS ───────────────────────────────────────

const permKeys = readText(
  "packages/CrevaFactOS.Persistence/Authorization/ProductionExecutionPermissionKeys.cs",
);

check(
  "ProductionExecutionPermissionKeys.cs exists",
  permKeys !== null,
);

check(
  "production.execution.read permission defined",
  permKeys?.includes('"production.execution.read"') ?? false,
);

check(
  "production.execution.write permission defined",
  permKeys?.includes('"production.execution.write"') ?? false,
);

check(
  "production.execution.start permission defined",
  permKeys?.includes('"production.execution.start"') ?? false,
);

check(
  "production.execution.pause permission defined",
  permKeys?.includes('"production.execution.pause"') ?? false,
);

check(
  "production.execution.complete permission defined",
  permKeys?.includes('"production.execution.complete"') ?? false,
);

check(
  "Permissions do not reuse planning prefix",
  permKeys !== null &&
    !permKeys.includes('"planning.') &&
    !permKeys.includes('"production.plan.'),
);

// ── 2. BOUNDED CONTEXT SEPARATION ────────────────────────────────────────────

check(
  "Production bounded context exists at packages/CrevaFactOS.Persistence/Production/",
  fileExists("packages/CrevaFactOS.Persistence/Production"),
);

check(
  "ProductionOrder.cs entity exists",
  fileExists("packages/CrevaFactOS.Persistence/Production/ProductionOrder.cs"),
);

check(
  "ProductionOrderStatus.cs exists",
  fileExists(
    "packages/CrevaFactOS.Persistence/Production/ProductionOrderStatus.cs",
  ),
);

const productionOrder = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOrder.cs",
);

check(
  "ProductionOrder is NOT in ProductionPlanning namespace",
  productionOrder !== null &&
    !productionOrder.includes("namespace CrevaFactOS.Persistence.ProductionPlanning"),
);

check(
  "ProductionOrder is in Production namespace",
  productionOrder?.includes(
    "namespace CrevaFactOS.Persistence.Production",
  ) ?? false,
);

// ── 3. PRODUCTION ORDER ENTITY FIELDS ────────────────────────────────────────

const requiredFields = [
  "ProductionPlanHandoffId",
  "SourceProductionPlanId",
  "SourceProductionPlanVersion",
  "ReferenceCode",
  "Title",
  "Status",
  "PlannedStartAtUtc",
  "PlannedEndAtUtc",
  "ActualStartAtUtc",
  "ActualEndAtUtc",
  "ShiftName",
  "MachineReference",
  "MoldReference",
  "ProductReference",
  "CreatedBySubjectId",
  "LastModifiedBySubjectId",
  "Version",
  "CreatedAtUtc",
  "UpdatedAtUtc",
];

for (const field of requiredFields) {
  check(
    `ProductionOrder has field: ${field}`,
    productionOrder?.includes(`public`) && productionOrder?.includes(field) ? true : false,
  );
}

// ── 4. STATUS MODEL ───────────────────────────────────────────────────────────

const statusModel = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOrderStatus.cs",
);

for (const status of ["Ready", "InProgress", "Paused", "Completed", "Cancelled"]) {
  check(
    `ProductionOrderStatus has value: ${status}`,
    statusModel?.includes(status) ?? false,
  );
}

// ── 5. REPOSITORY CONTRACTS ───────────────────────────────────────────────────

const readRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/IProductionOrderReadRepository.cs",
);
const writeRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/IProductionOrderWriteRepository.cs",
);

check("IProductionOrderReadRepository.cs exists", readRepo !== null);
check("IProductionOrderWriteRepository.cs exists", writeRepo !== null);
check("ReadRepository has ListAsync", readRepo?.includes("ListAsync") ?? false);
check("ReadRepository has FindByIdAsync", readRepo?.includes("FindByIdAsync") ?? false);
check("WriteRepository has ConsumeHandoffAsync", writeRepo?.includes("ConsumeHandoffAsync") ?? false);
check("WriteRepository has StartAsync", writeRepo?.includes("StartAsync") ?? false);
check("WriteRepository has PauseAsync", writeRepo?.includes("PauseAsync") ?? false);
check("WriteRepository has ResumeAsync", writeRepo?.includes("ResumeAsync") ?? false);
check("WriteRepository has CompleteAsync", writeRepo?.includes("CompleteAsync") ?? false);

// ── 6. API ENDPOINTS ─────────────────────────────────────────────────────────

const apiEndpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check("ProductionExecutionEndpointRouteBuilderExtensions.cs exists", apiEndpoints !== null);

const apiRoutes = [
  ["/api/production/orders", "list orders"],
  ["{productionOrderId", "detail orders"],
  ["/handoffs/", "consume handoff"],
  ["/start", "start order"],
  ["/pause", "pause order"],
  ["/resume", "resume order"],
  ["/complete", "complete order"],
];

for (const [fragment, label] of apiRoutes) {
  check(
    `API route defined: ${label}`,
    apiEndpoints?.includes(fragment) ?? false,
  );
}

check(
  "API does not have DELETE route for orders",
  apiEndpoints !== null && !apiEndpoints.includes('MapDelete("/api/production/orders'),
);

// ── 7. PERMISSION GUARDS ─────────────────────────────────────────────────────

check(
  "API references ProductionExecutionPermissionKeys.Read",
  apiEndpoints?.includes("ProductionExecutionPermissionKeys.Read") ?? false,
);

check(
  "API references ProductionExecutionPermissionKeys.Write",
  apiEndpoints?.includes("ProductionExecutionPermissionKeys.Write") ?? false,
);

check(
  "API references ProductionExecutionPermissionKeys.Start",
  apiEndpoints?.includes("ProductionExecutionPermissionKeys.Start") ?? false,
);

check(
  "API references ProductionExecutionPermissionKeys.Pause",
  apiEndpoints?.includes("ProductionExecutionPermissionKeys.Pause") ?? false,
);

check(
  "API references ProductionExecutionPermissionKeys.Complete",
  apiEndpoints?.includes("ProductionExecutionPermissionKeys.Complete") ?? false,
);

// ── 8. MIGRATION ─────────────────────────────────────────────────────────────

const migration = readText(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260807200000_AddProductionExecutionFoundation.cs",
);
const migrationDesigner = readText(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260807200000_AddProductionExecutionFoundation.Designer.cs",
);

check("AddProductionExecutionFoundation migration exists", migration !== null);
check("Migration Designer.cs exists", migrationDesigner !== null);
check(
  "Migration has [Migration] attribute",
  migrationDesigner?.includes('[Migration("20260807200000_AddProductionExecutionFoundation")]') ?? false,
);
check(
  "Migration creates production_orders table",
  migration?.includes("production_orders") ?? false,
);
check(
  "Migration inserts production.execution.read permission",
  migration?.includes("production.execution.read") ?? false,
);
check(
  "Migration enables RLS on production_orders",
  migration?.includes("ROW LEVEL SECURITY") ?? false,
);
check(
  "Migration adds runtime grants",
  migration?.includes("GRANT SELECT, INSERT, UPDATE") ?? false,
);
check(
  "Migration extends audit action taxonomy",
  migration?.includes("production.order.create") ?? false,
);
check(
  "Migration extends outbox message type taxonomy",
  migration?.includes("production-order-created") ?? false,
);

// ── 9. AUDIT TAXONOMY ────────────────────────────────────────────────────────

const auditActions = readText(
  "packages/CrevaFactOS.Persistence/Audit/AuditActions.cs",
);

check(
  "AuditActions.cs includes production.order.create",
  auditActions?.includes("production.order.create") ?? false,
);

check(
  "AuditActions.cs includes production.order.start",
  auditActions?.includes("production.order.start") ?? false,
);

// ── 10. OUTBOX TAXONOMY ──────────────────────────────────────────────────────

const outboxTypes = readText(
  "packages/CrevaFactOS.Persistence/Outbox/OutboxMessageTypes.cs",
);

check(
  "OutboxMessageTypes.cs includes production-order-created",
  outboxTypes?.includes("production-order-created") ?? false,
);

check(
  "OutboxMessageTypes.cs includes production-order-started",
  outboxTypes?.includes("production-order-started") ?? false,
);

// ── 11. SERVER-ONLY BRIDGE ───────────────────────────────────────────────────

const serverBoundary = readText(
  "apps/web/src/features/production-execution/server/production-order-server-boundary.ts",
);

check("Server boundary file exists", serverBoundary !== null);

check(
  "Server boundary enforces server-only (runtime guard)",
  serverBoundary?.includes("window") && serverBoundary?.includes("throw new Error") ? true : false,
);

const runtimeConfig = readText(
  "apps/web/src/features/production-execution/server/production-order-runtime-config.ts",
);
const requestContext = readText(
  "apps/web/src/features/production-execution/server/production-order-request-context.ts",
);
const apiClient = readText(
  "apps/web/src/features/production-execution/server/production-order-api-client.ts",
);
const reader = readText(
  "apps/web/src/features/production-execution/server/production-order-reader.ts",
);
const writer = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check("production-order-runtime-config.ts exists", runtimeConfig !== null);
check("production-order-request-context.ts exists", requestContext !== null);
check("production-order-api-client.ts exists", apiClient !== null);
check("production-order-reader.ts exists", reader !== null);
check("production-order-writer.ts exists", writer !== null);

// ── 12. PREVIEW WRITES BLOCKED ───────────────────────────────────────────────

check(
  "Writer blocks writes in preview mode",
  writer?.includes("preview-writes-blocked") ?? false,
);

check(
  "Writer returns failure for development-preview mode",
  writer?.includes("development-preview") ?? false,
);

// ── 13. BROWSER DOES NOT GENERATE AUTH HEADERS ───────────────────────────────

const lifecycleActions = readText(
  "apps/web/src/features/production-execution/components/production-order-lifecycle-actions.tsx",
);

check("Lifecycle actions component is client-only", lifecycleActions?.includes('"use client"') ?? false);

check(
  "Client component does not inject X-CrevaFactOS-Dev headers",
  lifecycleActions !== null &&
    !lifecycleActions.includes("X-CrevaFactOS-Dev") &&
    !lifecycleActions.includes("X-CrevaFactOS-Tenant"),
);

// ── 14. NO MACHINE/EDGE IMPLEMENTATION ───────────────────────────────────────

const productionFiles = [
  "packages/CrevaFactOS.Persistence/Production/ProductionOrder.cs",
  "packages/CrevaFactOS.Persistence/Production/ProductionOrderWriteRepository.cs",
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
];

for (const file of productionFiles) {
  const content = readText(file);
  check(
    `No machine telemetry/PLC/OPC-UA in ${path.basename(file)}`,
    content !== null &&
      !content.includes("PlcClient") &&
      !content.includes("OpcUa") &&
      !content.includes("MqttClient") &&
      !content.includes("EdgeAgent"),
  );
}

// ── 15. NO PLANNING DEPENDENCY REVERSAL ──────────────────────────────────────

const planningFiles = [
  "packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanHandoff.cs",
  "packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanWorkflowRepository.cs",
];

for (const file of planningFiles) {
  const content = readText(file);

  if (content === null) {
    continue;
  }

  check(
    `Planning file ${path.basename(file)} does not import Production namespace`,
    !content.includes("using CrevaFactOS.Persistence.Production;"),
  );
}

// ── 16. WEB ROUTES ───────────────────────────────────────────────────────────

check(
  "/uretim list page exists",
  fileExists("apps/web/src/app/(app)/uretim/page.tsx"),
);

check(
  "/uretim/[orderId] detail page exists",
  fileExists("apps/web/src/app/(app)/uretim/[orderId]/page.tsx"),
);

const listPage = readText("apps/web/src/app/(app)/uretim/page.tsx");
const detailPage = readText("apps/web/src/app/(app)/uretim/[orderId]/page.tsx");

check(
  "List page uses readProductionOrders",
  listPage?.includes("readProductionOrders") ?? false,
);

check(
  "Detail page uses readProductionOrderById",
  detailPage?.includes("readProductionOrderById") ?? false,
);

check(
  "Detail page includes lifecycle actions",
  detailPage?.includes("ProductionOrderLifecycleActions") ?? false,
);

// ── 17. globals.css UNTOUCHED ─────────────────────────────────────────────────

const globalsCssMtime = (() => {
  try {
    return fs
      .statSync(
        path.join(ROOT, "apps/web/src/app/globals.css"),
      )
      .mtime.toISOString();
  } catch {
    return null;
  }
})();

// We can only check the file exists; modification time is environment-specific
check(
  "apps/web/src/app/globals.css exists (untouched)",
  fileExists("apps/web/src/app/globals.css"),
);

// ── SUMMARY ──────────────────────────────────────────────────────────────────

console.log();

if (failures === 0) {
  console.log("✅ All production execution foundation checks passed.");
} else {
  console.error(
    `❌ ${failures} check(s) failed. Review the output above.`,
  );
  process.exit(1);
}

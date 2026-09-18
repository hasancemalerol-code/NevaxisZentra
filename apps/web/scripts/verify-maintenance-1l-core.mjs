import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Maintenance 1L core verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

console.log("Running maintenance 1L core verifier.");

const endpoints = readRequired("apps/api/Maintenance/MaintenanceEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Maintenance/MaintenanceEndpointHelpers.cs");
const lifecycle = readRequired("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceLifecycleEngine.cs");
const requestRepo = readRequired("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceRequestRepository.cs");
const workOrderRepo = readRequired("packages/NevaxisZentra.Persistence/Maintenance/MaintenanceWorkOrderRepository.cs");
const program = readRequired("apps/api/Program.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809170000_AddMaintenanceWorkOrderCore.cs",
);
const bakimPage = readRequired("apps/web/src/app/(app)/bakim/page.tsx");
const dashboardView = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-dashboard-view.tsx",
);
const bakimUi = `${bakimPage}\n${dashboardView}`;

requireText(endpoints, "MapGroup(\"/api/maintenance\")", "maintenance endpoints");
requireText(endpoints, "/requests", "maintenance endpoints");
requireText(endpoints, "/work-orders/{workOrderId:guid}/complete", "maintenance endpoints");
requireText(endpoints, "ResolveExecuteAccessAsync", "maintenance endpoints");
requireText(helpers, "MaintenancePermissionKeys.Execute", "MaintenanceEndpointHelpers");
requireText(program, "AddNevaxisZentraMaintenance", "Program.cs");
requireText(program, "MapMaintenanceEndpoints", "Program.cs");

requireText(lifecycle, "CanTransitionWorkOrder", "MaintenanceLifecycleEngine");
requireText(lifecycle, "IsTerminalWorkOrderStatus", "MaintenanceLifecycleEngine");
forbidText(lifecycle, "OpenAI", "MaintenanceLifecycleEngine");
forbidText(lifecycle, "Mqtt", "MaintenanceLifecycleEngine");
forbidText(lifecycle, "OpcUa", "MaintenanceLifecycleEngine");

requireText(requestRepo, "MaintenanceOutboxMessageTypes.RequestCreated", "MaintenanceRequestRepository");
requireText(workOrderRepo, "MaintenanceFailureCodes.CompletionNoteRequired", "MaintenanceWorkOrderRepository");
requireText(workOrderRepo, "MaintenanceOutboxMessageTypes.WorkOrderCompleted", "MaintenanceWorkOrderRepository");
forbidText(workOrderRepo, "ProductionDowntimes", "MaintenanceWorkOrderRepository");
forbidText(workOrderRepo, "ProductionOrders.Update", "MaintenanceWorkOrderRepository");
forbidText(requestRepo, "DELETE", "MaintenanceRequestRepository");

forbidText(endpoints, "MapDelete", "maintenance endpoints");
forbidText(migration, "GRANT DELETE", "migration");

requireText(migration, "maintenance_requests", "migration");
requireText(migration, "maintenance_work_orders", "migration");
requireText(migration, "preventive_maintenance_plans", "migration");
requireText(migration, "maintenance.read", "migration");
requireText(migration, "maintenance.execute", "migration");
requireText(migration, "ROW LEVEL SECURITY", "migration");

requireText(bakimUi, "Bakım &amp; Arıza", "bakim page");
requireText(bakimUi, "Açık Arızalar", "bakim page");
requireText(bakimUi, "Aktif Bakım İşleri", "bakim page");
requireText(bakimUi, "Periyodik Bakım Planları", "bakim page");

console.log("Maintenance 1L core verification passed.");

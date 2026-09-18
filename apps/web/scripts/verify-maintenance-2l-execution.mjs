import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Maintenance 2L execution verification failed: ${message}`);
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

console.log("Running maintenance 2L execution verifier.");

const executionEndpoints = readRequired(
  "apps/api/Maintenance/MaintenanceExecutionEndpointRouteBuilderExtensions.cs",
);
const executionRepo = readRequired(
  "packages/NevaxisZentra.Persistence/Maintenance/MaintenanceExecutionRepository.cs",
);
const dueCalculator = readRequired(
  "packages/NevaxisZentra.Persistence/Maintenance/MaintenanceDueDateCalculator.cs",
);
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809180000_AddMaintenanceExecutionAndPreventiveScheduling.cs",
);
const workOrderRepo = readRequired(
  "packages/NevaxisZentra.Persistence/Maintenance/MaintenanceWorkOrderRepository.cs",
);
const executionPanels = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-work-order-execution-panels.tsx",
);
const planningView = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-planning-view.tsx",
);

requireText(executionEndpoints, "/work-orders/{workOrderId:guid}/notes", "execution endpoints");
requireText(executionEndpoints, "/work-orders/{workOrderId:guid}/timeline", "execution endpoints");
requireText(executionEndpoints, "/preventive-plans/{planId:guid}/generate-next-due", "execution endpoints");
requireText(executionEndpoints, "/preventive-dues/{dueId:guid}/create-work-order", "execution endpoints");
requireText(executionEndpoints, "/work-orders/{workOrderId:guid}/parts", "execution endpoints");
requireText(executionEndpoints, "/work-orders/{workOrderId:guid}/cost-summary", "execution endpoints");
requireText(executionEndpoints, "/assets/{assetReference}/history", "execution endpoints");
forbidText(executionEndpoints, "MapDelete", "execution endpoints");
forbidText(executionEndpoints, "MapPut", "execution endpoints");

requireText(executionRepo, "MaintenanceWorkNote", "execution repository");
requireText(executionRepo, "PreventiveMaintenanceDue", "execution repository");
requireText(executionRepo, "MaintenancePartUsage", "execution repository");
requireText(executionRepo, "GetWorkOrderTimelineAsync", "execution repository");
requireText(executionRepo, "GenerateNextDueAsync", "execution repository");
requireText(executionRepo, "CreateWorkOrderFromDueAsync", "execution repository");
requireText(executionRepo, "string.IsNullOrWhiteSpace(command.Text)", "execution repository — empty note rejected");
requireText(executionRepo, "command.Quantity <= 0", "execution repository — quantity validation");
requireText(executionRepo, "WorkOrderClosed", "execution repository — completed WO part block");
requireText(executionRepo, "WorkOrderCreated && due.WorkOrderId.HasValue", "execution repository — duplicate idempotent");
requireText(executionRepo, "MaintenanceDueDateCalculator.CalculateNextDueAtUtc", "execution repository");

requireText(dueCalculator, "AddMonths", "due calculator — calendar month semantics");
requireText(dueCalculator, "AddDays", "due calculator");

requireText(migration, "maintenance_work_notes", "migration");
requireText(migration, "preventive_maintenance_dues", "migration");
requireText(migration, "maintenance_part_usages", "migration");
requireText(migration, "maintenance.worknote.created", "migration");
requireText(migration, "maintenance-preventive-due-generated", "migration");
requireText(migration, "GRANT SELECT, INSERT ON TABLE maintenance_work_notes", "migration — append-only notes");
requireText(migration, "GRANT SELECT, INSERT ON TABLE maintenance_part_usages", "migration — append-only parts");
forbidText(migration, "GRANT DELETE", "migration");

forbidText(executionRepo, "Inventory", "execution repository — no ERP stock");
forbidText(executionRepo, "Warehouse", "execution repository");
forbidText(executionRepo, "StockMovement", "execution repository");
forbidText(executionRepo, "ProductionOrders.Update", "execution repository");
forbidText(executionRepo, "OpenAI", "execution repository");
forbidText(executionRepo, "Mqtt", "execution repository");
forbidText(executionRepo, "OpcUa", "execution repository");

requireText(workOrderRepo, "AsNoTracking()", "work order repo — production read-only");
requireText(workOrderRepo, "PreventiveDueId", "work order repo — due completion hook");

requireText(executionPanels, "Bu kayıt bakım kullanım bilgisidir; stok hareketi oluşturmaz.", "execution panels");
requireText(executionPanels, "Bakım Notları", "execution panels");
requireText(executionPanels, "Kullanılan Parçalar", "execution panels");
requireText(executionPanels, "Maliyet Özeti", "execution panels");
requireText(executionPanels, "Zaman Çizelgesi", "execution panels");

requireText(planningView, "Gecikenler", "planning view");
requireText(planningView, "Yaklaşanlar", "planning view");
requireText(planningView, "Planlar", "planning view");

console.log("Maintenance 2L execution verification passed.");

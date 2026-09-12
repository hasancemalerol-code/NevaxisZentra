import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const TENANT = process.env.NEVAXIS_FACTOS_TENANT ?? "dev-pilot-tenant";
const OPERATOR = process.env.NEVAXIS_FACTOS_SUBJECT ?? "production-pilot-operator";
const ISSUER = "crevafactos-dev";
const FACILITY = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";

function fail(message) {
  console.error(`Maintenance integration boundaries verification failed: ${message}`);
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

function headers(subject = OPERATOR, extra = {}) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
    ...extra,
  };
}

console.log("Running maintenance integration boundaries verifier.");

const workOrderRepo = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceWorkOrderRepository.cs");
const requestRepo = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceRequestRepository.cs");
const executionRepo = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceExecutionRepository.cs");
const reliabilityRepo = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceReliabilityRepository.cs");
const lifecycle = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceLifecycleEngine.cs");
const reportingEndpoints = readRequired(
  "apps/api/Maintenance/MaintenanceReportingEndpointRouteBuilderExtensions.cs",
);
const dashboardView = readRequired(
  "apps/web/src/features/maintenance/components/maintenance-dashboard-view.tsx",
);
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const globalsCss = readRequired("apps/web/src/app/globals.css");

requireText(workOrderRepo, "MaintenanceWorkOrder", "maintenance ownership");
requireText(requestRepo, "MaintenanceRequest", "maintenance ownership");
requireText(executionRepo, "MaintenanceWorkNote", "maintenance ownership");
requireText(executionRepo, "PreventiveMaintenanceDue", "maintenance ownership");
requireText(executionRepo, "MaintenancePartUsage", "maintenance ownership");
requireText(reliabilityRepo, "MaintenanceAssetRiskProjectionReadModel", "maintenance ownership");

forbidText(workOrderRepo, "ProductionOrders.Update", "work order repository");
forbidText(workOrderRepo, "ProductionDowntimes", "work order repository");
forbidText(requestRepo, "ProductionOrders.Update", "request repository");
forbidText(executionRepo, "Inventory", "execution repository");
forbidText(executionRepo, "Warehouse", "execution repository");
forbidText(executionRepo, "StockMovement", "execution repository");
forbidText(executionRepo, "PurchaseOrder", "execution repository");
forbidText(executionRepo, "Payroll", "execution repository");
forbidText(executionRepo, "EmployeeScore", "execution repository");

forbidText(lifecycle, "Mqtt", "lifecycle engine");
forbidText(lifecycle, "OpcUa", "lifecycle engine");
forbidText(lifecycle, "Modbus", "lifecycle engine");
forbidText(lifecycle, "OpenAI", "lifecycle engine");
forbidText(reliabilityRepo, "predictive maintenance", "reliability repository");
forbidText(reliabilityRepo, "remaining useful life", "reliability repository");
forbidText(reliabilityRepo, "EmployeeScore", "reliability repository");
forbidText(reliabilityRepo, "technicianRanking", "reliability repository");

requireText(reliabilityRepo, "MaintenanceAssetRiskProjectionReadModel", "risk projection read model");
requireText(reliabilityRepo, "ConditionStatus", "condition status");
requireText(reliabilityRepo, "EvaluateConditionStatusWithReasons", "condition reasons");

requireText(dashboardView, "MaintenanceAssetCell", "UI-1 asset cells");
requireText(dashboardView, "MaintenanceTableActionGroup", "UI-1 action groups");
forbidText(dashboardView, "Bakım Emrini Aç", "UI-1 concatenated action text");
forbidText(dashboardView, 'className="maintenance-table__action"', "UI-1 legacy inline action");

forbidText(nav, "Bakım Planlama", "primary navigation unchanged");
forbidText(globalsCss, "maintenance-table-action-group", "globals.css untouched");

if (dashboardView.includes("assignedTechnicianSubjectId")) {
  console.log("Technician display classification: A — acceptable developer/pilot subject reference display.");
}

console.log("Running maintenance integration boundaries live checks.");

try {
  const ordersResponse = await fetch(`${BASE}/api/production/orders?facilityId=${FACILITY}`, {
    headers: headers(),
  });
  if (ordersResponse.status !== 200) {
    fail(`production order list must return HTTP 200, got ${ordersResponse.status}.`);
  }
  const orders = await ordersResponse.json();
  if (!Array.isArray(orders) || orders.length === 0) {
    fail("production non-mutation proof requires at least one production order.");
  }
  const productionOrder = orders[0];
  const productionOrderId = productionOrder.id ?? productionOrder.productionOrderId;
  if (!productionOrderId) {
    fail("production order list must expose an order id.");
  }

  const beforeResponse = await fetch(`${BASE}/api/production/orders/${productionOrderId}?facilityId=${FACILITY}`, {
    headers: headers(),
  });
  if (beforeResponse.status !== 200) {
    fail(`production order read must return HTTP 200, got ${beforeResponse.status}.`);
  }
  const before = await beforeResponse.json();
  const beforeSnapshot = {
    status: before.status,
    version: before.version,
    goodQuantity: before.goodQuantity,
    scrapQuantity: before.scrapQuantity,
  };

  const dateTo = new Date().toISOString();
  const dateFrom = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString();
  const maintenanceOps = [
    fetch(`${BASE}/api/maintenance/reporting/overview?facilityId=${FACILITY}&dateFrom=${encodeURIComponent(dateFrom)}&dateTo=${encodeURIComponent(dateTo)}`, { headers: headers() }),
    fetch(`${BASE}/api/maintenance/assets/IM-120/history?facilityId=${FACILITY}`, { headers: headers() }),
    fetch(`${BASE}/api/maintenance/work-orders?facilityId=${FACILITY}`, { headers: headers() }),
  ];
  const maintenanceResults = await Promise.all(maintenanceOps);
  if (maintenanceResults.some((response) => response.status !== 200)) {
    fail("representative maintenance reads/reporting must succeed during production non-mutation probe.");
  }

  const afterResponse = await fetch(`${BASE}/api/production/orders/${productionOrderId}?facilityId=${FACILITY}`, {
    headers: headers(),
  });
  const after = await afterResponse.json();
  if (
    after.status !== beforeSnapshot.status
    || after.version !== beforeSnapshot.version
    || after.goodQuantity !== beforeSnapshot.goodQuantity
    || after.scrapQuantity !== beforeSnapshot.scrapQuantity
  ) {
    fail("maintenance operations must not mutate production order state.");
  }

  const postReporting = await fetch(`${BASE}/api/maintenance/reporting/overview?facilityId=${FACILITY}`, {
    method: "POST",
    headers: headers(),
  });
  if (postReporting.status !== 405) {
    fail(`reporting POST must return HTTP 405, got ${postReporting.status}.`);
  }

  const deleteReporting = await fetch(`${BASE}/api/maintenance/reporting/overview?facilityId=${FACILITY}`, {
    method: "DELETE",
    headers: headers(),
  });
  if (deleteReporting.status !== 405) {
    fail(`reporting DELETE must return HTTP 405, got ${deleteReporting.status}.`);
  }

  const bakimHtml = await fetch(`${WEB}/bakim`).then((response) => response.text());
  forbidText(bakimHtml, "IM-120Makine", "live /bakim asset concatenation");
  forbidText(bakimHtml, "CHILLER-01Yardımcı", "live /bakim preventive concatenation");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP/stock boundary: static code proof only — no inventory subsystem writes in Maintenance repositories.");
console.log("Maintenance integration boundaries verification passed.");

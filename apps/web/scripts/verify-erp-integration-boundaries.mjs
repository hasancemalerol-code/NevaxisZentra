import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.CREVAFACTOS_API_BASE ?? "http://127.0.0.1:5000";
const WEB = process.env.CREVAFACTOS_WEB_BASE ?? "http://127.0.0.1:3001";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP integration boundaries verification failed: ${message}`);
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

function headers(subject = OPERATOR) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": subject,
    "X-CrevaFactOS-Tenant": TENANT,
  };
}

async function api(method, path, body) {
  const url = path.includes("facilityId=")
    ? `${BASE}${path}`
    : `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY}`;
  const response = await fetch(url, {
    method,
    headers: headers(),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = null;
  }
  return { status: response.status, json, text };
}

console.log("Running ERP integration boundaries verifier.");

const entities = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpEntities.cs");
const syncRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpSyncRepository.cs");
const inventoryRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpInventoryRepository.cs");
const productionRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpProductionRepository.cs");
const writebackRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpWritebackRepository.cs");
const mappingRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpMappingRepository.cs");
const adapterContract = readRequired("packages/CrevaFactOS.Persistence/Erp/IErpAdapter.cs");
const adapters = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpAdapters.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const workflowRepo = readRequired("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanWorkflowRepository.cs");
const writePlanRepo = readRequired("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanWriteRepository.cs");
const globals = readRequired("apps/web/src/app/globals.css");
const sidebar = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const productionView = readRequired("apps/web/src/features/erp/components/erp-production-view.tsx");
const inventoryView = readRequired("apps/web/src/features/erp/components/erp-inventory-view.tsx");
const purchasingView = readRequired("apps/web/src/features/erp/components/erp-purchasing-view.tsx");
const writebackActions = readRequired("apps/web/src/features/erp/components/erp-writeback-approval-actions.tsx");
const qualityContext = readRequired("packages/CrevaFactOS.Persistence/Quality/ProductionOrderContextReader.cs");
const maintenanceWorkOrder = readRequired("packages/CrevaFactOS.Persistence/Maintenance/MaintenanceWorkOrderRepository.cs");

requireText(entities, "External ERP remains system of record", "ERP SoR boundary");
requireText(entities, "Does not create or mutate internal Production orders", "production projection boundary");
requireText(inventoryRepo, "MaterialAvailabilityFreshnessNote", "material availability is projection-based");
requireText(inventoryRepo, "ERP stok projeksiyonuna dayalı bilgi", "not MRP labeling");

forbidText(syncRepo, "dbContext.ProductionOrders", "sync must not mutate internal Production");
forbidText(productionRepo, "dbContext.ProductionOrders", "production repo must not mutate internal Production");
forbidText(inventoryRepo, "StockLedger", "no stock ledger ownership");
forbidText(inventoryRepo, "StockMovement", "no stock movement ownership");
forbidText(inventoryRepo, "Mrp", "no MRP");
forbidText(syncRepo, "Accounting", "no accounting ownership");
forbidText(syncRepo, "PurchaseOrder.Create", "no purchasing creation");
forbidText(writebackRepo, "OpenAI", "no AI approval");
forbidText(writebackRepo, "AutoApprove", "no autonomous writeback");
forbidText(mappingRepo, "OpenAI", "no AI matching");
forbidText(mappingRepo, "Embedding", "no fuzzy auto-match");
forbidText(workflowRepo, "ProductionOrders.Update", "Planning ownership preserved");
forbidText(writePlanRepo, "dbContext.ProductionOrders", "Planning write preserved");
forbidText(qualityContext, "ProductionOrders.Update", "Quality must not mutate production via ERP");
forbidText(maintenanceWorkOrder, "ErpConnectionProfiles", "Maintenance must not own ERP");

forbidText(adapterContract, "ExecuteSql", "no arbitrary SQL contract");
forbidText(adapterContract, "ExecuteCommand", "no arbitrary command contract");
forbidText(endpoints, "MapPost(\"/production-orders", "no production mutation API");
forbidText(endpoints, "MapPost(\"/inventory", "no inventory mutation API");
forbidText(endpoints, "MapPost(\"/purchase-orders", "no purchasing mutation API");

for (const repo of [syncRepo, inventoryRepo, productionRepo, mappingRepo, writebackRepo]) {
  forbidText(repo, "Logo.Tiger", "no Logo leakage outside adapter");
  forbidText(repo, "Mikro.", "no Mikro leakage outside adapter");
  forbidText(repo, "Netsis.", "no Netsis leakage outside adapter");
}

requireText(adapters, "class MikroErpAdapterPlaceholder", "Mikro placeholder");
requireText(adapters, "class NetsisErpAdapterPlaceholder", "Netsis placeholder");
const logoAdapter = readRequired("packages/CrevaFactOS.Persistence/Erp/Logo/LogoErpAdapter.cs");
requireText(logoAdapter, "class LogoErpAdapter", "Logo adapter");
forbidText(adapters, "fallback to simulator", "no simulator fallback");
forbidText(writebackRepo, "Resolve(profile.Provider) ?? Resolve(\"simulator\")", "no simulator fallback");

requireText(productionView, "salt okunur", "production read-only UI");
requireText(inventoryView, "salt okunur", "inventory read-only UI");
requireText(purchasingView, "salt okunur", "purchasing read-only UI");
requireText(writebackActions, "Onayla", "approve distinct");
requireText(writebackActions, "Uygula", "execute distinct");
forbidText(writebackActions, "Onayla ve Uygula", "no combined approve execute");

forbidText(globals, "erp-workspace", "globals.css untouched");
forbidText(sidebar, "Stok & Depolar", "main sidebar unchanged");

console.log("Running ERP integration boundaries live checks.");

try {
  const internalBefore = await fetch(`${BASE}/api/production/orders?facilityId=${FACILITY}&limit=100`, {
    headers: headers(),
  });
  if (internalBefore.status !== 200) fail(`internal production list expected 200 got ${internalBefore.status}`);
  const internalOrdersBefore = await internalBefore.json();
  const internalCountBefore = Array.isArray(internalOrdersBefore) ? internalOrdersBefore.length : 0;
  if (internalCountBefore < 1) fail("internal production baseline count required");

  const internalSnapshotById = Object.fromEntries(
    internalOrdersBefore.map((order) => {
      const id = order.id ?? order.productionOrderId;
      return [
        id,
        {
          status: order.status,
          version: order.version,
          goodQuantity: order.goodQuantity,
          scrapQuantity: order.scrapQuantity,
        },
      ];
    }),
  );

  const planningBefore = await fetch(`${BASE}/api/production-planning/plans?facilityId=${FACILITY}`, {
    headers: headers(),
  });
  const planningBodyBefore = planningBefore.status === 200 ? await planningBefore.json() : [];
  const planningCountBefore = Array.isArray(planningBodyBefore) ? planningBodyBefore.length : 0;

  await api("POST", `/api/erp/connections/${CONNECTION}/sync/customers`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/products`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/orders`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/warehouses`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/purchasing`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/production`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/documents`);

  const internalAfter = await fetch(`${BASE}/api/production/orders?facilityId=${FACILITY}&limit=100`, {
    headers: headers(),
  });
  const internalOrdersAfter = await internalAfter.json();
  const internalCountAfter = Array.isArray(internalOrdersAfter) ? internalOrdersAfter.length : 0;
  if (internalCountAfter !== internalCountBefore) {
    fail(`ERP sync must not mutate internal Production count ${internalCountBefore} -> ${internalCountAfter}`);
  }

  for (const order of internalOrdersAfter) {
    const id = order.id ?? order.productionOrderId;
    const before = internalSnapshotById[id];
    if (!before) fail(`unexpected internal production order appeared: ${id}`);
    if (
      order.status !== before.status
      || order.version !== before.version
      || order.goodQuantity !== before.goodQuantity
      || order.scrapQuantity !== before.scrapQuantity
    ) {
      fail(`ERP sync must not mutate internal Production order ${id}`);
    }
  }

  const planningAfter = await fetch(`${BASE}/api/production-planning/plans?facilityId=${FACILITY}`, {
    headers: headers(),
  });
  const planningBodyAfter = planningAfter.status === 200 ? await planningAfter.json() : [];
  const planningCountAfter = Array.isArray(planningBodyAfter) ? planningBodyAfter.length : 0;
  if (planningCountAfter !== planningCountBefore) {
    fail(`ERP sync must not mutate Planning count ${planningCountBefore} -> ${planningCountAfter}`);
  }

  const orders = await api("GET", "/api/erp/orders");
  const order = orders.json?.find((row) => row.externalId === "ERP-ORD-001");
  if (order?.id) {
    const readiness = await api("GET", `/api/erp/orders/${order.id}/readiness`);
    if (readiness.json?.readyForOperationalUse === true) {
      const stillSame = await fetch(`${BASE}/api/production/orders?facilityId=${FACILITY}&limit=100`, {
        headers: headers(),
      });
      const stillOrders = stillSame.status === 200 ? await stillSame.json() : [];
      const stillCount = Array.isArray(stillOrders) ? stillOrders.length : 0;
      if (stillCount !== internalCountBefore) {
        fail("readiness true must not create internal Production");
      }
    }
  }

  const erpRoute = await fetch(`${WEB}/erp-entegrasyonlari/stok`);
  if (erpRoute.status !== 200) fail("inventory UI boundary route expected 200");
  const html = await erpRoute.text();
  if (html.includes("MRP") || html.includes("Material Requirements Planning")) {
    fail("UI must not claim MRP");
  }

  console.log(
    `ERP integration boundaries live checks passed: internalProduction=${internalCountBefore}, planning=${planningCountBefore}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP integration boundaries verification passed.");

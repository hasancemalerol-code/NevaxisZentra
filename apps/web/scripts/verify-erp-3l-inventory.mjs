import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-3L inventory verification failed: ${message}`);
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
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
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

console.log("Running ERP-3L inventory verifier.");

const adapter = readRequired("packages/NevaxisFactOS.Persistence/Erp/IErpAdapter.cs");
const adapters = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpAdapters.cs");
const entities = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpEntities.cs");
const inventoryRepo = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpInventoryRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const migration = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260809220000_AddErpInventoryWarehouseAndPurchasingReadModels.cs",
);

requireText(adapter, "ReadWarehousesAsync", "IErpAdapter");
requireText(adapter, "ReadInventoryAsync", "IErpAdapter");
requireText(entities, "WarehousesRead = \"warehouses.read\"", "ErpCapabilities");
requireText(entities, "InventoryRead = \"inventory.read\"", "ErpCapabilities");
requireText(entities, "class CanonicalErpWarehouse", "CanonicalErpWarehouse");
requireText(entities, "class CanonicalErpInventoryBalance", "CanonicalErpInventoryBalance");
requireText(entities, "External ERP inventory balance projection", "inventory projection boundary");
requireText(adapters, "ERP-WH-001", "simulator warehouse");
requireText(adapters, "ERP-PRD-002", "simulator inventory product");
requireText(inventoryRepo, "SyncWarehousesAsync", "ErpInventoryRepository");
requireText(inventoryRepo, "SyncInventoryAsync", "ErpInventoryRepository");
requireText(inventoryRepo, "ListInventoryAvailabilityAsync", "ErpInventoryRepository");
requireText(inventoryRepo, "GetOrderMaterialAvailabilityAsync", "ErpInventoryRepository");
requireText(endpoints, "sync/warehouses", "warehouse sync endpoint");
requireText(endpoints, "sync/inventory", "inventory sync endpoint");
requireText(endpoints, "/inventory/availability", "inventory availability endpoint");
requireText(endpoints, "material-availability", "material availability endpoint");
requireText(migration, "erp_warehouses", "migration");
requireText(migration, "erp_inventory_balances", "migration");

forbidText(inventoryRepo, "ProductionOrders", "inventory repo must not mutate production");
forbidText(inventoryRepo, "StockLedger", "no stock ledger");
forbidText(inventoryRepo, "Mrp", "no MRP");
forbidText(endpoints, "MapPost(\"/inventory", "no inventory mutation endpoints");

const inventoryEntitySection = entities.slice(
  entities.indexOf("class CanonicalErpInventoryBalance"),
  entities.indexOf("/// External ERP purchase order projection"),
);
forbidText(inventoryEntitySection, "SupplierName", "CanonicalErpInventoryBalance");

console.log("Running ERP-3L inventory live smoke.");

try {
  let r = await api("GET", "/api/erp/connections");
  if (r.status !== 200) fail(`connections readable expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/customers`);
  if (r.status !== 200) fail(`customer sync regression expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/customers`);
  if (r.status !== 200) fail(`customer sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`customer sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/products`);
  if (r.status !== 200) fail(`product sync regression expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/products`);
  if (r.status !== 200) fail(`product sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`product sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/orders`);
  if (r.status !== 200) fail(`order sync regression expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/orders`);
  if (r.status !== 200) fail(`order sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`order sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/warehouses`);
  if (r.status !== 200) fail(`warehouse sync #1 expected 200 got ${r.status}`);
  const whSync1Created = r.json?.recordsCreated ?? 0;
  if (whSync1Created < 1) {
    r = await api("GET", "/api/erp/warehouses");
    if (r.status !== 200 || !Array.isArray(r.json)) fail("warehouses list expected 200 array for warm-db check");
    if (!r.json.find((row) => row.externalId === "ERP-WH-001")) {
      fail(`warehouse sync #1 must create at least 1 warehouse (got ${whSync1Created})`);
    }
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/warehouses`);
  if (r.status !== 200) fail(`warehouse sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`warehouse sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);
  if (r.status !== 200) fail(`inventory sync #1 expected 200 got ${r.status}`);
  const invSync1Created = r.json?.recordsCreated ?? 0;
  if (invSync1Created < 1) {
    r = await api("GET", "/api/erp/inventory");
    if (r.status !== 200 || !Array.isArray(r.json)) fail("inventory list expected 200 array for warm-db check");
    const warmBalance = r.json.find(
      (row) => row.externalProductId === "ERP-PRD-002" && row.externalWarehouseId === "ERP-WH-001",
    );
    if (!warmBalance) {
      fail(`inventory sync #1 must create at least 1 balance (got ${invSync1Created})`);
    }
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);
  if (r.status !== 200) fail(`inventory sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`inventory sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("GET", "/api/erp/warehouses");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("warehouses list expected 200 array");
  const warehouse = r.json.find((row) => row.externalId === "ERP-WH-001");
  if (!warehouse) fail("ERP-WH-001 not found after sync");

  r = await api("GET", "/api/erp/inventory");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("inventory list expected 200 array");
  const balance = r.json.find(
    (row) => row.externalProductId === "ERP-PRD-002" && row.externalWarehouseId === "ERP-WH-001",
  );
  if (!balance) fail("ERP-PRD-002 inventory balance not found");
  if (Number(balance.quantityAvailable) !== 2500) {
    fail(`ERP-PRD-002 available must be 2500 got ${balance.quantityAvailable}`);
  }

  r = await api("GET", "/api/erp/inventory/availability");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("inventory availability expected 200 array");
  const availability = r.json.find((row) => row.externalProductId === "ERP-PRD-002");
  if (!availability) fail("ERP-PRD-002 availability row missing");
  if (Number(availability.totalAvailable) !== 2500) {
    fail(`ERP-PRD-002 totalAvailable must be 2500 got ${availability.totalAvailable}`);
  }
  if (!Array.isArray(availability.warehouseBreakdown) || availability.warehouseBreakdown.length < 1) {
    fail("availability warehouse breakdown required");
  }

  r = await api("GET", "/api/erp/orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("orders list expected 200 array");
  const order = r.json.find((row) => row.externalId === "ERP-ORD-001");
  if (!order) fail("ERP-ORD-001 required for material availability");

  r = await api("GET", `/api/erp/orders/${order.id}/material-availability`);
  if (r.status !== 200) fail("material availability expected 200");
  if (r.json?.freshnessNote !== "ERP stok projeksiyonuna dayalı bilgi") {
    fail("material availability freshness note missing");
  }
  const materialLine = (r.json?.lines ?? []).find((line) => line.externalLineId === "ERP-ORD-001-L2");
  if (!materialLine) fail("ERP-ORD-001-L2 material line missing");
  if (materialLine.status !== "insufficient") {
    fail(`ERP-ORD-001-L2 status must be insufficient got ${materialLine.status}`);
  }
  if (Number(materialLine.shortageQuantity) !== 2500) {
    fail(`ERP-ORD-001-L2 shortage must be 2500 got ${materialLine.shortageQuantity}`);
  }

  console.log(
    `ERP-3L inventory live smoke passed: whSync1Created=${whSync1Created}, invSync1Created=${invSync1Created}, availability=${availability.totalAvailable}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-3L inventory verification passed.");

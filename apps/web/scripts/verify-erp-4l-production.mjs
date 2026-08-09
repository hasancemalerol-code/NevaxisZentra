import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.CREVAFACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-4L production verification failed: ${message}`);
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

console.log("Running ERP-4L production verifier.");

const adapter = readRequired("packages/CrevaFactOS.Persistence/Erp/IErpAdapter.cs");
const adapters = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpAdapters.cs");
const entities = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpEntities.cs");
const productionRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpProductionRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260809230000_AddErpProductionDocumentsAndWritebackFoundation.cs",
);

requireText(adapter, "ReadProductionOrdersAsync", "IErpAdapter");
requireText(entities, "ProductionRead = \"production.read\"", "ErpCapabilities");
requireText(entities, "class CanonicalErpProductionOrder", "CanonicalErpProductionOrder");
requireText(
  entities,
  "Does not create or mutate internal Production orders",
  "production projection boundary",
);
requireText(adapters, "ERP-MO-001", "simulator production order");
requireText(productionRepo, "SyncProductionAsync", "ErpProductionRepository");
requireText(productionRepo, "ListProductionOrdersAsync", "ErpProductionRepository");
requireText(endpoints, "sync/production", "production sync endpoint");
requireText(endpoints, "/production-orders", "production orders endpoint");
requireText(migration, "erp_production_orders", "migration");

forbidText(productionRepo, "dbContext.ProductionOrders", "production repo must not mutate internal production");
forbidText(productionRepo, "StockLedger", "no stock ledger");
forbidText(productionRepo, "Mrp", "no MRP");
forbidText(endpoints, "MapPost(\"/production-orders", "no production mutation endpoints");

console.log("Running ERP-4L production live smoke.");

try {
  let r = await api("GET", "/api/erp/connections");
  if (r.status !== 200) fail(`connections readable expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/customers`);
  if (r.status !== 200) fail(`customer sync regression expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/products`);
  if (r.status !== 200) fail(`product sync regression expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/orders`);
  if (r.status !== 200) fail(`order sync regression expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/production`);
  if (r.status !== 200) fail(`production sync #1 expected 200 got ${r.status}`);
  const prodSync1Created = r.json?.recordsCreated ?? 0;
  if (prodSync1Created < 1) {
    r = await api("GET", "/api/erp/production-orders");
    if (r.status !== 200 || !Array.isArray(r.json)) {
      fail("production orders list expected 200 array for warm-db check");
    }
    if (!r.json.find((row) => row.externalId === "ERP-MO-001")) {
      fail(`production sync #1 must create at least 1 order (got ${prodSync1Created})`);
    }
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/production`);
  if (r.status !== 200) fail(`production sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`production sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("GET", "/api/erp/production-orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("production orders list expected 200 array");
  const productionOrder = r.json.find((row) => row.externalId === "ERP-MO-001");
  if (!productionOrder) fail("ERP-MO-001 not found after sync");

  if (Number(productionOrder.plannedQuantity) !== 10000) {
    fail(`ERP-MO-001 planned must be 10000 got ${productionOrder.plannedQuantity}`);
  }
  if (Number(productionOrder.completedQuantity) !== 4000) {
    fail(`ERP-MO-001 completed must be 4000 got ${productionOrder.completedQuantity}`);
  }
  if (Number(productionOrder.scrapQuantity) !== 100) {
    fail(`ERP-MO-001 scrap must be 100 got ${productionOrder.scrapQuantity}`);
  }
  if (productionOrder.status !== "in-progress") {
    fail(`ERP-MO-001 status must be in-progress got ${productionOrder.status}`);
  }

  r = await api("GET", `/api/erp/production-orders/${productionOrder.id}`);
  if (r.status !== 200) fail("production order detail expected 200");
  if (r.json?.externalId !== "ERP-MO-001") fail("production order detail externalId mismatch");
  if (Number(r.json?.plannedQuantity) !== 10000) {
    fail(`production order detail planned must be 10000 got ${r.json?.plannedQuantity}`);
  }

  console.log(
    `ERP-4L production live smoke passed: prodSync1Created=${prodSync1Created}, status=${productionOrder.status}, planned=${productionOrder.plannedQuantity}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-4L production verification passed.");

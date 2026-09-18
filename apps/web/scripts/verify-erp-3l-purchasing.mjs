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
  console.error(`ERP-3L purchasing verification failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": TENANT,
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

console.log("Running ERP-3L purchasing verifier.");

const adapter = readRequired("packages/NevaxisZentra.Persistence/Erp/IErpAdapter.cs");
const adapters = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpAdapters.cs");
const entities = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpEntities.cs");
const inventoryRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpInventoryRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809220000_AddErpInventoryWarehouseAndPurchasingReadModels.cs",
);

requireText(adapter, "ReadPurchaseOrdersAsync", "IErpAdapter");
requireText(entities, "PurchasingRead = \"purchasing.read\"", "ErpCapabilities");
requireText(entities, "class CanonicalErpPurchaseOrder", "CanonicalErpPurchaseOrder");
requireText(entities, "No purchasing writeback", "purchase order projection boundary");
requireText(adapters, "ERP-PO-001", "simulator purchase order");
requireText(adapters, "ERP-PRD-002", "simulator PO line product");
requireText(inventoryRepo, "SyncPurchasingAsync", "ErpInventoryRepository");
requireText(inventoryRepo, "ErpCoverageStatuses.Covered", "purchase coverage logic");
requireText(endpoints, "sync/purchasing", "purchasing sync endpoint");
requireText(endpoints, "/purchase-orders", "purchase order list endpoint");
requireText(migration, "erp_purchase_orders", "migration");
requireText(migration, "erp_purchase_order_lines", "migration");

forbidText(inventoryRepo, "WritePurchase", "no purchasing writeback");
forbidText(endpoints, "MapPost(\"/purchase-orders", "no PO mutation endpoints");

console.log("Running ERP-3L purchasing live smoke.");

try {
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/orders`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);

  let r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/purchasing`);
  if (r.status !== 200) fail(`purchasing sync #1 expected 200 got ${r.status}`);
  const poSync1Created = r.json?.recordsCreated ?? 0;
  if (poSync1Created < 1) {
    r = await api("GET", "/api/erp/purchase-orders");
    if (r.status !== 200 || !Array.isArray(r.json)) fail("purchase orders list expected 200 array for warm-db check");
    if (!r.json.find((row) => row.externalId === "ERP-PO-001")) {
      fail(`purchasing sync #1 must create at least 1 PO (got ${poSync1Created})`);
    }
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/purchasing`);
  if (r.status !== 200) fail(`purchasing sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`purchasing sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("GET", "/api/erp/purchase-orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("purchase orders list expected 200 array");
  const po = r.json.find((row) => row.externalId === "ERP-PO-001");
  if (!po) fail("ERP-PO-001 not found after sync");

  r = await api("GET", `/api/erp/purchase-orders/${po.id}`);
  if (r.status !== 200) fail("purchase order detail expected 200");
  const poLine = (r.json?.lines ?? []).find((line) => line.externalProductId === "ERP-PRD-002");
  if (!poLine) fail("ERP-PO-001 ERP-PRD-002 line missing");
  if (Number(poLine.openQuantity) !== 5000) {
    fail(`open PO quantity must be 5000 got ${poLine.openQuantity}`);
  }

  r = await api("GET", "/api/erp/orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("orders list expected 200 array");
  const order = r.json.find((row) => row.externalId === "ERP-ORD-001");
  if (!order) fail("ERP-ORD-001 required for coverage cross-check");

  r = await api("GET", `/api/erp/orders/${order.id}/material-availability`);
  if (r.status !== 200) fail("material availability expected 200");
  const materialLine = (r.json?.lines ?? []).find((line) => line.externalProductId === "ERP-PRD-002");
  if (!materialLine) fail("ERP-PRD-002 material availability line missing");
  if (Number(materialLine.shortageQuantity) !== 2500) {
    fail(`ERP-PRD-002 shortage must be 2500 got ${materialLine.shortageQuantity}`);
  }
  const coverage = materialLine.purchaseCoverage;
  if (!coverage) fail("purchase coverage required for ERP-PRD-002 shortage");
  if (Number(coverage.openPurchaseQuantity) !== 5000) {
    fail(`open purchase coverage must be 5000 got ${coverage.openPurchaseQuantity}`);
  }
  if (coverage.coverageStatus !== "covered") {
    fail(`coverage status must be covered got ${coverage.coverageStatus}`);
  }

  console.log(
    `ERP-3L purchasing live smoke passed: poSync1Created=${poSync1Created}, shortage=2500, openPo=5000, covered=${coverage.coverageStatus}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-3L purchasing verification passed.");

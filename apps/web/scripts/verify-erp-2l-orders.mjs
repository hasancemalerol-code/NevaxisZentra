import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.CREVAFACTOS_API_BASE ?? "http://localhost:5000";
const WEB = process.env.CREVAFACTOS_WEB_BASE ?? "http://localhost:3001";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";
const CUSTOMER_TARGET = "bb000001-0091-4000-8000-000000000091";
const PRODUCT_TARGET_1 = "bb000001-0092-4000-8000-000000000092";
const PRODUCT_TARGET_2 = "bb000001-0093-4000-8000-000000000093";

function fail(message) {
  console.error(`ERP-2L orders verification failed: ${message}`);
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

console.log("Running ERP-2L orders verifier.");

const adapter = readRequired("packages/CrevaFactOS.Persistence/Erp/IErpAdapter.cs");
const adapters = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpAdapters.cs");
const entities = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpEntities.cs");
const syncRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpSyncRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260809210000_AddErpOrderInboundAndReconciliation.cs",
);

requireText(adapter, "ReadOrdersAsync", "IErpAdapter");
requireText(entities, "OrdersRead = \"orders.read\"", "ErpCapabilities");
requireText(entities, "class CanonicalErpOrder", "CanonicalErpOrder");
requireText(entities, "class CanonicalErpOrderLine", "CanonicalErpOrderLine");
requireText(adapters, "ERP-ORD-001", "simulator order");
requireText(adapters, "ERP-ORD-001-L1", "simulator order line");
requireText(syncRepo, "SyncOrdersAsync", "ErpSyncRepository");
requireText(endpoints, "sync/orders", "order sync endpoint");
requireText(endpoints, "/orders", "order list endpoint");
requireText(migration, "erp_orders", "migration");
requireText(migration, "erp_order_lines", "migration");

console.log("Running ERP-2L orders live smoke.");

try {
  let r = await api("GET", "/api/erp/connections");
  if (r.status !== 200) fail(`connections readable expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/customers`);
  if (r.status !== 200) fail(`customer sync regression expected 200 got ${r.status}`);
  const custSync1Created = r.json?.recordsCreated ?? -1;

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
  if (r.status !== 200) fail(`order sync #1 expected 200 got ${r.status}`);
  const orderSync1Created = r.json?.recordsCreated ?? 0;
  const orderSync1Lines = r.json?.recordsRead ?? 0;
  if (orderSync1Created < 1) fail(`order sync #1 must create at least 1 order`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/orders`);
  if (r.status !== 200) fail(`order sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`order sync #2 created orders must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("GET", "/api/erp/orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("orders list expected 200 array");
  const finalOrderCount = r.json.length;
  if (finalOrderCount < 1) fail("final order count must be >= 1");

  const order = r.json.find((row) => row.externalId === "ERP-ORD-001");
  if (!order) fail("ERP-ORD-001 not found after sync");
  if (order.externalId !== "ERP-ORD-001") fail("external id not preserved");

  r = await api("GET", `/api/erp/orders/${order.id}`);
  if (r.status !== 200) fail("order detail expected 200");
  const lineCount = r.json?.lines?.length ?? 0;
  if (lineCount < 2) fail(`expected at least 2 lines got ${lineCount}`);

  r = await api("GET", `/api/erp/orders/${order.id}/readiness`);
  if (r.status !== 200) fail("readiness expected 200");
  if (r.json?.readyForOperationalUse !== false) {
    fail("readiness before mapping must be false");
  }

  const blocking = (r.json?.blockingReasons ?? []).map((b) => b.code);
  if (!blocking.some((c) => c === "customer-unmapped" || c === "product-unmapped")) {
    fail(`blocking reasons must include customer-unmapped or product-unmapped got ${blocking.join(",")}`);
  }

  r = await api("GET", "/api/erp/reconciliation/customers");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("customer reconciliation expected 200");
  const custRow = r.json.find((row) => row.externalId === "ERP-CUST-001");
  if (!custRow?.mappingId) fail("customer mapping row missing");

  r = await api("POST", `/api/erp/mappings/${custRow.mappingId}/confirm`, {
    facilityId: FACILITY,
    targetEntityId: CUSTOMER_TARGET,
    expectedVersion: custRow.mappingVersion ?? 1,
  });
  if (r.status !== 200) fail(`customer mapping confirm expected 200 got ${r.status}`);

  r = await api("GET", "/api/erp/reconciliation/products");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("product reconciliation expected 200");
  const prod1 = r.json.find((row) => row.externalId === "ERP-PRD-001");
  const prod2 = r.json.find((row) => row.externalId === "ERP-PRD-002");
  if (!prod1?.mappingId || !prod2?.mappingId) fail("product mapping rows missing");

  r = await api("POST", `/api/erp/mappings/${prod1.mappingId}/confirm`, {
    facilityId: FACILITY,
    targetEntityId: PRODUCT_TARGET_1,
    expectedVersion: prod1.mappingVersion ?? 1,
  });
  if (r.status !== 200) fail(`product1 mapping confirm expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/mappings/${prod2.mappingId}/confirm`, {
    facilityId: FACILITY,
    targetEntityId: PRODUCT_TARGET_2,
    expectedVersion: prod2.mappingVersion ?? 1,
  });
  if (r.status !== 200) fail(`product2 mapping confirm expected 200 got ${r.status}`);

  r = await api("GET", `/api/erp/orders/${order.id}/readiness`);
  if (r.status !== 200) fail("readiness after mapping expected 200");
  if (r.json?.readyForOperationalUse !== true) {
    fail(`readiness after mapping must be true got ${r.json?.readyForOperationalUse}`);
  }

  r = await fetch(`${WEB}/erp-entegrasyonlari/siparisler`);
  if (r.status !== 200) fail(`orders UI expected 200 got ${r.status}`);

  r = await fetch(`${WEB}/erp-entegrasyonlari/siparisler/${order.id}`);
  if (r.status !== 200) fail(`order detail UI expected 200 got ${r.status}`);

  console.log(
    `ERP-2L orders live smoke passed: custSync1Created=${custSync1Created}, orderSync1Created=${orderSync1Created}, finalOrders=${finalOrderCount}, finalLines=${lineCount}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-2L orders verification passed.");

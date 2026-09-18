import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTHORIZED_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const NEGATIVE = "quality-negative-test-user";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-3L security verification failed: ${message}`);
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

async function api(method, path, body, subject = OPERATOR, facilityId = FACILITY) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facilityId}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject),
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

function assertNoSupplierLeakage(payload, label) {
  const serialized = JSON.stringify(payload ?? {});
  if (serialized.includes("supplierName") || serialized.includes("externalSupplierId")) {
    fail(`${label} must not expose supplier fields`);
  }
}

console.log("Running ERP-3L security verifier.");

const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Erp/ErpEndpointHelpers.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809220000_AddErpInventoryWarehouseAndPurchasingReadModels.cs",
);
const inventoryRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpInventoryRepository.cs");
const entities = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpEntities.cs");
const adapters = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpAdapters.cs");

requireText(endpoints, "ErpPermissionKeys.Sync", "inventory sync permission");
requireText(endpoints, "ResolveReadAccessAsync", "inventory read permission");
requireText(endpoints, "sync/inventory", "inventory sync route");
requireText(endpoints, "sync/purchasing", "purchasing sync route");
requireText(helpers, "Status401Unauthorized", "401 mapping");
requireText(helpers, "Status403Forbidden", "403 mapping");
requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration RLS");
requireText(migration, "erp_inventory_balances", "inventory migration");
requireText(migration, "erp_purchase_orders", "purchase order migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE erp_inventory_balances", "inventory grants");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE erp_purchase_orders", "purchase order grants");

forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "erp endpoints");
forbidText(inventoryRepo, "Logo.", "no Logo leakage outside adapter");
forbidText(inventoryRepo, "Mikro.", "no Mikro leakage outside adapter");
forbidText(inventoryRepo, "Netsis.", "no Netsis leakage outside adapter");
forbidText(adapters, "Logo.Tiger", "no vendor SDK leakage in adapters");

const inventoryEntitySection = entities.slice(
  entities.indexOf("class CanonicalErpInventoryBalance"),
  entities.indexOf("/// External ERP purchase order projection"),
);
forbidText(inventoryEntitySection, "SupplierName", "inventory entity");

console.log("Running ERP-3L security live checks.");

try {
  let r = await fetch(`${BASE}/api/erp/inventory?facilityId=${FACILITY}`);
  if (r.status !== 401) fail(`401 expected without auth got ${r.status}`);

  r = await api("GET", "/api/erp/inventory", null, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.read on inventory expected 403 got ${r.status}`);

  r = await api("GET", "/api/erp/purchase-orders", null, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.read on purchase orders expected 403 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`, null, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.sync on inventory expected 403 got ${r.status}`);

  r = await api("GET", "/api/erp/inventory", null, OPERATOR, UNAUTHORIZED_FACILITY);
  if (r.status !== 403) fail(`wrong facility on inventory expected 403 got ${r.status}`);

  const crossTenant = await fetch(`${BASE}/api/erp/inventory/availability?facilityId=${FACILITY}`, {
    headers: {
      ...headers(OPERATOR),
      "X-NevaxisZentra-Tenant": "other-tenant-not-pilot",
    },
  });
  if (crossTenant.status !== 403) {
    fail(`cross-tenant isolation expected 403 got ${crossTenant.status}`);
  }

  await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/purchasing`);

  r = await api("GET", "/api/erp/inventory");
  if (r.status !== 200) fail("authorized inventory read expected 200");
  assertNoSupplierLeakage(r.json, "inventory list");

  r = await api("GET", "/api/erp/inventory/availability");
  if (r.status !== 200) fail("inventory availability read expected 200");
  assertNoSupplierLeakage(r.json, "inventory availability");

  r = await api("GET", "/api/erp/warehouses");
  if (r.status !== 200) fail("warehouses read expected 200");
  assertNoSupplierLeakage(r.json, "warehouses list");

  r = await api("GET", "/api/erp/purchase-orders");
  if (r.status !== 200) fail("purchase orders read expected 200");
  const po = r.json?.find((row) => row.externalId === "ERP-PO-001");
  if (!po?.supplierName) fail("purchase order supplierName expected on PO endpoint");

  const deleteInventory = await fetch(
    `${BASE}/api/erp/inventory/f0000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`,
    { method: "DELETE", headers: headers() },
  );
  if (deleteInventory.status !== 405 && deleteInventory.status !== 404) {
    fail(`DELETE inventory unavailable expected 405/404 got ${deleteInventory.status}`);
  }

  const deletePo = await fetch(
    `${BASE}/api/erp/purchase-orders/f0000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`,
    { method: "DELETE", headers: headers() },
  );
  if (deletePo.status !== 405 && deletePo.status !== 404) {
    fail(`DELETE purchase order unavailable expected 405/404 got ${deletePo.status}`);
  }

  r = await api("GET", "/api/erp/sync-reliability");
  if (r.status !== 200) fail("sync reliability readback expected 200");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-3L security verification passed.");

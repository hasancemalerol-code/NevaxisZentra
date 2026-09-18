import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MIKRO_CONNECTION = "bb000001-0092-4000-8000-000000000092";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-MIKRO-1 adapter verification failed: ${message}`);
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

function headers() {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": OPERATOR,
    "X-NevaxisZentra-Tenant": TENANT,
  };
}

async function api(method, path, body) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY}`;
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

console.log("Running ERP-MIKRO-1 adapter verifier.");

const mikroAdapter = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroErpAdapter.cs");
const mikroTransport = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/IMikroReadTransport.cs");
const mikroFixture = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroReadTransportTestFixture.cs");
const mikroMapper = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroSourceRecords.cs");
const adapters = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpAdapters.cs");
const syncRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpSyncRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const entities = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpEntities.cs");

requireText(mikroAdapter, "class MikroErpAdapter", "MikroErpAdapter");
forbidText(adapters, "class MikroErpAdapterPlaceholder", "placeholder removed");
requireText(mikroAdapter, "ProviderKey => ErpProviderKeys.Mikro", "provider key mikro");
requireText(mikroTransport, "interface IMikroReadTransport", "transport abstraction");
requireText(mikroAdapter, "ErpCapabilities.CustomersRead", "customers.read");
requireText(mikroAdapter, "ErpCapabilities.ProductsRead", "products.read");
forbidText(mikroAdapter, "ErpCapabilities.OrdersRead", "orders unsupported");
forbidText(mikroAdapter, "ErpCapabilities.InventoryRead", "inventory unsupported");
forbidText(mikroAdapter, "IErpWritebackAdapter", "no writeback");
requireText(mikroMapper, "MikroCustomerMapper", "customer mapping");
requireText(mikroMapper, "MikroProductMapper", "product mapping");
requireText(mikroMapper, "ErpExternalCustomerRecord", "canonical customer mapping");
requireText(mikroMapper, "ErpExternalProductRecord", "canonical product mapping");
requireText(mikroTransport, "MikroReadPageRequest", "pagination request");
const mikroReadPage = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroReadPage.cs");
requireText(mikroReadPage, "ContinuationToken", "pagination cursor");
requireText(mikroAdapter, "CancelAfter", "timeout support");
requireText(mikroAdapter, "CancellationToken", "cancellation support");
requireText(mikroFixture, "MIKRO-TEST-CUST-001", "test customer fixture");
requireText(mikroFixture, "MIKRO-TEST-PRD-001", "test product fixture");
requireText(mikroFixture, "Mikro Test Fixture", "test fixture naming");
forbidText(mikroFixture, "Mikro Simulator", "not a simulator");
forbidText(mikroAdapter, "CARI_", "no guessed Mikro tables");
forbidText(mikroAdapter, "fallback to simulator", "no simulator fallback");
forbidText(mikroAdapter, "LogoErpAdapter", "no Logo fallback");
requireText(endpoints, "AddSingleton<IErpAdapter, MikroErpAdapter>", "Mikro adapter DI");
requireText(syncRepo, "ReadCustomersAsync", "existing canonical sync path");
requireText(entities, "Mikro = \"mikro\"", "provider key constant");

console.log("Running ERP-MIKRO-1 adapter live checks.");

try {
  const connections = await api("GET", "/api/erp/connections");
  if (connections.status !== 200 || !Array.isArray(connections.json)) {
    fail(`connections list expected 200 got ${connections.status}`);
  }

  const mikroConnection = connections.json.find((row) => row.id === MIKRO_CONNECTION);
  if (!mikroConnection) {
    fail("development Mikro test connection missing");
  }

  if (mikroConnection.provider !== "mikro") {
    fail("Mikro test connection provider must remain mikro");
  }

  const health = await api("POST", `/api/erp/connections/${MIKRO_CONNECTION}/health-check`);
  if (health.status !== 200 || !health.json?.isHealthy) {
    fail(`Mikro test health expected healthy 200 got ${health.status}`);
  }

  const customersBefore = await api("GET", `/api/erp/customers?connectionProfileId=${MIKRO_CONNECTION}`);
  const customerCountBefore = Array.isArray(customersBefore.json) ? customersBefore.json.length : 0;

  const syncCustomers1 = await api("POST", `/api/erp/connections/${MIKRO_CONNECTION}/sync/customers`);
  if (syncCustomers1.status !== 200) fail(`Mikro customer sync #1 expected 200 got ${syncCustomers1.status}`);
  const created1 = syncCustomers1.json?.recordsCreated ?? 0;
  if (customerCountBefore === 0 && created1 < 1) fail("Mikro customer sync #1 must create records on empty state");

  const syncCustomers2 = await api("POST", `/api/erp/connections/${MIKRO_CONNECTION}/sync/customers`);
  if (syncCustomers2.status !== 200) fail(`Mikro customer sync #2 expected 200 got ${syncCustomers2.status}`);
  if ((syncCustomers2.json?.recordsCreated ?? -1) !== 0) {
    fail("Mikro customer sync #2 must be idempotent (0 creates)");
  }

  const productsBefore = await api("GET", `/api/erp/products?connectionProfileId=${MIKRO_CONNECTION}`);
  const productCountBefore = Array.isArray(productsBefore.json) ? productsBefore.json.length : 0;
  const syncProducts1 = await api("POST", `/api/erp/connections/${MIKRO_CONNECTION}/sync/products`);
  if (syncProducts1.status !== 200) fail(`Mikro product sync #1 expected 200 got ${syncProducts1.status}`);
  const createdProducts1 = syncProducts1.json?.recordsCreated ?? 0;
  if (productCountBefore === 0 && createdProducts1 < 1) fail("Mikro product sync #1 must create records on empty state");

  const syncProducts2 = await api("POST", `/api/erp/connections/${MIKRO_CONNECTION}/sync/products`);
  if (syncProducts2.status !== 200) fail(`Mikro product sync #2 expected 200 got ${syncProducts2.status}`);
  if ((syncProducts2.json?.recordsCreated ?? -1) !== 0) {
    fail("Mikro product sync #2 must be idempotent (0 creates)");
  }

  const customersAfter = await api("GET", `/api/erp/customers?connectionProfileId=${MIKRO_CONNECTION}`);
  const customerCountAfter = Array.isArray(customersAfter.json) ? customersAfter.json.length : 0;
  if (customerCountAfter < customerCountBefore + created1) {
    fail("Mikro customer projection count must increase after first sync");
  }

  const productsAfter = await api("GET", `/api/erp/products?connectionProfileId=${MIKRO_CONNECTION}`);
  const productCountAfter = Array.isArray(productsAfter.json) ? productsAfter.json.length : 0;
  if (productCountAfter < 2) {
    fail("Mikro product projection count must include fixture records");
  }

  const metadata = health.json?.integrationMetadata ?? {};
  if (metadata["mikro.liveValidated"] === "true") {
    fail("test transport must not claim live Mikro validation");
  }

  const connectionDetail = await api("GET", `/api/erp/connections/${MIKRO_CONNECTION}`);
  if (connectionDetail.status !== 200) fail("Mikro connection detail expected 200");

  const updateFailure = await api("PUT", `/api/erp/connections/${MIKRO_CONNECTION}`, {
    facilityId: FACILITY,
    expectedVersion: connectionDetail.json?.version ?? 1,
    name: connectionDetail.json?.name ?? "Development Mikro Test Transport",
    externalCompanyId: connectionDetail.json?.externalCompanyId,
    externalCompanyCode: connectionDetail.json?.externalCompanyCode,
    baseEndpoint: connectionDetail.json?.baseEndpoint,
    connectionMode: connectionDetail.json?.connectionMode ?? "api",
    credentialReference: "mikro-test-fixture-failure",
    status: connectionDetail.json?.status ?? "configured",
  });
  if (updateFailure.status !== 200) {
    fail(`Mikro failure-mode credential update expected 200 got ${updateFailure.status}`);
  }

  const syncFailure = await api("POST", `/api/erp/connections/${MIKRO_CONNECTION}/sync/customers`);
  if (syncFailure.status === 200 && syncFailure.json?.status !== "failed") {
    fail("Mikro failure-mode sync must fail explicitly");
  }

  const customersPreserved = await api("GET", `/api/erp/customers?connectionProfileId=${MIKRO_CONNECTION}`);
  const preservedCount = Array.isArray(customersPreserved.json) ? customersPreserved.json.length : 0;
  if (preservedCount < customerCountAfter) {
    fail("Mikro failure must preserve prior customer projections");
  }

  const productsPreserved = await api("GET", `/api/erp/products?connectionProfileId=${MIKRO_CONNECTION}`);
  const preservedProductCount = Array.isArray(productsPreserved.json) ? productsPreserved.json.length : 0;
  if (preservedProductCount < productCountAfter) {
    fail("Mikro failure must preserve prior product projections");
  }

  const restore = await api("PUT", `/api/erp/connections/${MIKRO_CONNECTION}`, {
    facilityId: FACILITY,
    expectedVersion: updateFailure.json?.version ?? connectionDetail.json?.version ?? 1,
    name: connectionDetail.json?.name ?? "Development Mikro Test Transport",
    externalCompanyId: connectionDetail.json?.externalCompanyId,
    externalCompanyCode: connectionDetail.json?.externalCompanyCode,
    baseEndpoint: connectionDetail.json?.baseEndpoint,
    connectionMode: connectionDetail.json?.connectionMode ?? "api",
    credentialReference: "mikro-test-fixture",
    status: connectionDetail.json?.status ?? "configured",
  });
  if (restore.status !== 200) {
    fail(`Mikro credential restore expected 200 got ${restore.status}`);
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-MIKRO-1 adapter verification passed.");

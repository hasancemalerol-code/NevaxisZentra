import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const NETSIS_CONNECTION = "bb000001-0093-4000-8000-000000000093";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-NETSIS-1 adapter verification failed: ${message}`);
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
    "X-NevaxisFactOS-Tenant": TENANT,
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

console.log("Running ERP-NETSIS-1 adapter verifier.");

const netsisAdapter = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisErpAdapter.cs");
const netsisTransport = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/INetsisReadTransport.cs");
const netsisFixture = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisReadTransportTestFixture.cs");
const netsisMapper = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisSourceRecords.cs");
const adapters = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpAdapters.cs");
const syncRepo = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpSyncRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const entities = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpEntities.cs");
const bridgeProgram = readRequired("tools/NevaxisFactOS.NetsisBridge/Program.cs");

requireText(netsisAdapter, "class NetsisErpAdapter", "NetsisErpAdapter");
forbidText(adapters, "class NetsisErpAdapterPlaceholder", "placeholder removed");
requireText(netsisAdapter, "ProviderKey => ErpProviderKeys.Netsis", "provider key netsis");
requireText(netsisTransport, "interface INetsisReadTransport", "transport abstraction");
requireText(netsisAdapter, "ErpCapabilities.CustomersRead", "customers.read");
requireText(netsisAdapter, "ErpCapabilities.ProductsRead", "products.read");
forbidText(netsisAdapter, "ErpCapabilities.OrdersRead", "orders unsupported");
forbidText(netsisAdapter, "ErpCapabilities.InventoryRead", "inventory unsupported");
forbidText(netsisAdapter, "IErpWritebackAdapter", "no writeback");
requireText(netsisMapper, "NetsisCustomerMapper", "customer mapping");
requireText(netsisMapper, "NetsisProductMapper", "product mapping");
requireText(netsisMapper, "ErpExternalCustomerRecord", "canonical customer mapping");
requireText(netsisMapper, "ErpExternalProductRecord", "canonical product mapping");
requireText(netsisTransport, "NetsisReadPageRequest", "pagination request");
const netsisReadPage = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisReadPage.cs");
requireText(netsisReadPage, "ContinuationToken", "pagination cursor");
requireText(netsisAdapter, "CancelAfter", "timeout support");
requireText(netsisAdapter, "CancellationToken", "cancellation support");
requireText(netsisFixture, "NETSIS-TEST-CUST-001", "test customer fixture");
requireText(netsisFixture, "NETSIS-TEST-PRD-001", "test product fixture");
requireText(netsisFixture, "Netsis Test Fixture", "test fixture naming");
forbidText(netsisFixture, "Netsis Simulator", "not a simulator");
forbidText(netsisAdapter, "fallback to simulator", "no simulator fallback");
forbidText(netsisAdapter, "LogoErpAdapter", "no Logo fallback");
forbidText(netsisAdapter, "MikroErpAdapter", "no Mikro fallback");
requireText(endpoints, "AddSingleton<IErpAdapter, NetsisErpAdapter>", "Netsis adapter DI");
requireText(syncRepo, "ReadCustomersAsync", "existing canonical sync path");
requireText(entities, "Netsis = \"netsis\"", "provider key constant");
forbidText(bridgeProgram, "MapPost", "no POST mutation in bridge");
forbidText(bridgeProgram, "MapPut", "no PUT in bridge");
forbidText(bridgeProgram, "MapDelete", "no DELETE in bridge");
forbidText(bridgeProgram, "sql", "no SQL in bridge");

console.log("Running ERP-NETSIS-1 adapter live checks.");

try {
  const connections = await api("GET", "/api/erp/connections");
  if (connections.status !== 200 || !Array.isArray(connections.json)) {
    fail(`connections list expected 200 got ${connections.status}`);
  }

  const netsisConnection = connections.json.find((row) => row.id === NETSIS_CONNECTION);
  if (!netsisConnection) {
    fail("development Netsis test connection missing");
  }

  if (netsisConnection.provider !== "netsis") {
    fail("Netsis test connection provider must remain netsis");
  }

  const health = await api("POST", `/api/erp/connections/${NETSIS_CONNECTION}/health-check`);
  if (health.status !== 200 || !health.json?.isHealthy) {
    fail(`Netsis test health expected healthy 200 got ${health.status}`);
  }

  const customersBefore = await api("GET", `/api/erp/customers?connectionProfileId=${NETSIS_CONNECTION}`);
  const customerCountBefore = Array.isArray(customersBefore.json) ? customersBefore.json.length : 0;

  const syncCustomers1 = await api("POST", `/api/erp/connections/${NETSIS_CONNECTION}/sync/customers`);
  if (syncCustomers1.status !== 200) fail(`Netsis customer sync #1 expected 200 got ${syncCustomers1.status}`);
  const created1 = syncCustomers1.json?.recordsCreated ?? 0;
  if (customerCountBefore === 0 && created1 < 1) fail("Netsis customer sync #1 must create records on empty state");

  const syncCustomers2 = await api("POST", `/api/erp/connections/${NETSIS_CONNECTION}/sync/customers`);
  if (syncCustomers2.status !== 200) fail(`Netsis customer sync #2 expected 200 got ${syncCustomers2.status}`);
  if ((syncCustomers2.json?.recordsCreated ?? -1) !== 0) {
    fail("Netsis customer sync #2 must be idempotent (0 creates)");
  }

  const syncProducts1 = await api("POST", `/api/erp/connections/${NETSIS_CONNECTION}/sync/products`);
  if (syncProducts1.status !== 200) fail(`Netsis product sync #1 expected 200 got ${syncProducts1.status}`);
  const productsBefore = await api("GET", `/api/erp/products?connectionProfileId=${NETSIS_CONNECTION}`);
  const productCountBefore = Array.isArray(productsBefore.json) ? productsBefore.json.length : 0;
  const createdProducts1 = syncProducts1.json?.recordsCreated ?? 0;
  if (productCountBefore === 0 && createdProducts1 < 1) fail("Netsis product sync #1 must create records on empty state");

  const syncProducts2 = await api("POST", `/api/erp/connections/${NETSIS_CONNECTION}/sync/products`);
  if (syncProducts2.status !== 200) fail(`Netsis product sync #2 expected 200 got ${syncProducts2.status}`);
  if ((syncProducts2.json?.recordsCreated ?? -1) !== 0) {
    fail("Netsis product sync #2 must be idempotent (0 creates)");
  }

  const customersAfter = await api("GET", `/api/erp/customers?connectionProfileId=${NETSIS_CONNECTION}`);
  const customerCountAfter = Array.isArray(customersAfter.json) ? customersAfter.json.length : 0;
  if (customerCountAfter < 2) {
    fail("Netsis customer projection count must include fixture records");
  }

  const productsAfter = await api("GET", `/api/erp/products?connectionProfileId=${NETSIS_CONNECTION}`);
  const productCountAfter = Array.isArray(productsAfter.json) ? productsAfter.json.length : 0;
  if (productCountAfter < 2) {
    fail("Netsis product projection count must include fixture records");
  }

  const metadata = health.json?.integrationMetadata ?? {};
  if (metadata["netsis.liveValidated"] === "true") {
    fail("test transport must not claim live Netsis validation");
  }

  const connectionDetail = await api("GET", `/api/erp/connections/${NETSIS_CONNECTION}`);
  if (connectionDetail.status !== 200) fail("Netsis connection detail expected 200");

  const updateFailure = await api("PUT", `/api/erp/connections/${NETSIS_CONNECTION}`, {
    facilityId: FACILITY,
    expectedVersion: connectionDetail.json?.version ?? 1,
    name: connectionDetail.json?.name ?? "Development Netsis Test Transport",
    externalCompanyId: connectionDetail.json?.externalCompanyId,
    externalCompanyCode: connectionDetail.json?.externalCompanyCode,
    baseEndpoint: connectionDetail.json?.baseEndpoint,
    connectionMode: connectionDetail.json?.connectionMode ?? "api",
    credentialReference: "netsis-test-fixture-failure",
    status: connectionDetail.json?.status ?? "configured",
  });
  if (updateFailure.status !== 200) {
    fail(`Netsis failure-mode credential update expected 200 got ${updateFailure.status}`);
  }

  const syncFailure = await api("POST", `/api/erp/connections/${NETSIS_CONNECTION}/sync/customers`);
  if (syncFailure.status === 200 && syncFailure.json?.status !== "failed") {
    fail("Netsis failure-mode sync must fail explicitly");
  }

  const customersPreserved = await api("GET", `/api/erp/customers?connectionProfileId=${NETSIS_CONNECTION}`);
  const preservedCount = Array.isArray(customersPreserved.json) ? customersPreserved.json.length : 0;
  if (preservedCount < customerCountAfter) {
    fail("Netsis failure must preserve prior customer projections");
  }

  const productsPreserved = await api("GET", `/api/erp/products?connectionProfileId=${NETSIS_CONNECTION}`);
  const preservedProductCount = Array.isArray(productsPreserved.json) ? productsPreserved.json.length : 0;
  if (preservedProductCount < productCountAfter) {
    fail("Netsis failure must preserve prior product projections");
  }

  const restore = await api("PUT", `/api/erp/connections/${NETSIS_CONNECTION}`, {
    facilityId: FACILITY,
    expectedVersion: updateFailure.json?.version ?? connectionDetail.json?.version ?? 1,
    name: connectionDetail.json?.name ?? "Development Netsis Test Transport",
    externalCompanyId: connectionDetail.json?.externalCompanyId,
    externalCompanyCode: connectionDetail.json?.externalCompanyCode,
    baseEndpoint: connectionDetail.json?.baseEndpoint,
    connectionMode: connectionDetail.json?.connectionMode ?? "api",
    credentialReference: "netsis-test-fixture",
    status: connectionDetail.json?.status ?? "configured",
  });
  if (restore.status !== 200) {
    fail(`Netsis credential restore expected 200 got ${restore.status}`);
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-NETSIS-1 adapter verification passed.");

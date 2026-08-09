import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.CREVAFACTOS_API_BASE ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const LOGO_CONNECTION = "bb000001-0091-4000-8000-000000000091";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-LOGO-1 adapter verification failed: ${message}`);
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
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": OPERATOR,
    "X-CrevaFactOS-Tenant": TENANT,
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

console.log("Running ERP-LOGO-1 adapter verifier.");

const logoAdapter = readRequired("packages/CrevaFactOS.Persistence/Erp/Logo/LogoErpAdapter.cs");
const logoTransport = readRequired("packages/CrevaFactOS.Persistence/Erp/Logo/ILogoReadTransport.cs");
const logoFixture = readRequired("packages/CrevaFactOS.Persistence/Erp/Logo/LogoReadTransportTestFixture.cs");
const logoMapper = readRequired("packages/CrevaFactOS.Persistence/Erp/Logo/LogoSourceRecords.cs");
const adapters = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpAdapters.cs");
const syncRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpSyncRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const entities = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpEntities.cs");

requireText(logoAdapter, "class LogoErpAdapter", "LogoErpAdapter");
forbidText(adapters, "class LogoErpAdapterPlaceholder", "placeholder removed");
requireText(logoAdapter, "ProviderKey => ErpProviderKeys.Logo", "provider key logo");
requireText(logoTransport, "interface ILogoReadTransport", "transport abstraction");
requireText(logoAdapter, "ErpCapabilities.CustomersRead", "customers.read");
requireText(logoAdapter, "ErpCapabilities.ProductsRead", "products.read");
forbidText(logoAdapter, "ErpCapabilities.OrdersRead", "orders unsupported");
forbidText(logoAdapter, "ErpCapabilities.InventoryRead", "inventory unsupported");
forbidText(logoAdapter, "IErpWritebackAdapter", "no writeback");
requireText(logoMapper, "LogoCustomerMapper", "customer mapping");
requireText(logoMapper, "LogoProductMapper", "product mapping");
requireText(logoMapper, "ErpExternalCustomerRecord", "canonical customer mapping");
requireText(logoMapper, "ErpExternalProductRecord", "canonical product mapping");
requireText(logoTransport, "LogoReadPageRequest", "pagination request");
const logoReadPage = readRequired("packages/CrevaFactOS.Persistence/Erp/Logo/LogoReadPage.cs");
requireText(logoReadPage, "ContinuationToken", "pagination cursor");
requireText(logoAdapter, "CancelAfter", "timeout support");
requireText(logoAdapter, "CancellationToken", "cancellation support");
requireText(logoFixture, "LOGO-TEST-CUST-001", "test customer fixture");
requireText(logoFixture, "LOGO-TEST-PRD-001", "test product fixture");
requireText(logoFixture, "Logo Test Fixture", "test fixture naming");
forbidText(logoFixture, "Logo Simulator", "not a simulator");
forbidText(logoAdapter, "LG_", "no guessed Logo tables");
forbidText(logoAdapter, "fallback to simulator", "no simulator fallback");
requireText(endpoints, "AddSingleton<IErpAdapter, LogoErpAdapter>", "Logo adapter DI");
requireText(syncRepo, "ReadCustomersAsync", "existing canonical sync path");
requireText(entities, "Logo = \"logo\"", "provider key constant");

console.log("Running ERP-LOGO-1 adapter live checks.");

try {
  const connections = await api("GET", "/api/erp/connections");
  if (connections.status !== 200 || !Array.isArray(connections.json)) {
    fail(`connections list expected 200 got ${connections.status}`);
  }

  const logoConnection = connections.json.find((row) => row.id === LOGO_CONNECTION);
  if (!logoConnection) {
    fail("development Logo test connection missing");
  }

  if (logoConnection.provider !== "logo") {
    fail("Logo test connection provider must remain logo");
  }

  const health = await api("POST", `/api/erp/connections/${LOGO_CONNECTION}/health-check`);
  if (health.status !== 200 || !health.json?.isHealthy) {
    fail(`Logo test health expected healthy 200 got ${health.status}`);
  }

  const customersBefore = await api("GET", `/api/erp/customers?connectionProfileId=${LOGO_CONNECTION}`);
  const customerCountBefore = Array.isArray(customersBefore.json) ? customersBefore.json.length : 0;

  const syncCustomers1 = await api("POST", `/api/erp/connections/${LOGO_CONNECTION}/sync/customers`);
  if (syncCustomers1.status !== 200) fail(`Logo customer sync #1 expected 200 got ${syncCustomers1.status}`);
  const created1 = syncCustomers1.json?.recordsCreated ?? 0;
  if (created1 < 1) fail("Logo customer sync #1 must create records");

  const syncCustomers2 = await api("POST", `/api/erp/connections/${LOGO_CONNECTION}/sync/customers`);
  if (syncCustomers2.status !== 200) fail(`Logo customer sync #2 expected 200 got ${syncCustomers2.status}`);
  if ((syncCustomers2.json?.recordsCreated ?? -1) !== 0) {
    fail("Logo customer sync #2 must be idempotent (0 creates)");
  }

  const syncProducts1 = await api("POST", `/api/erp/connections/${LOGO_CONNECTION}/sync/products`);
  if (syncProducts1.status !== 200) fail(`Logo product sync #1 expected 200 got ${syncProducts1.status}`);
  if ((syncProducts1.json?.recordsCreated ?? 0) < 1) fail("Logo product sync #1 must create records");

  const syncProducts2 = await api("POST", `/api/erp/connections/${LOGO_CONNECTION}/sync/products`);
  if (syncProducts2.status !== 200) fail(`Logo product sync #2 expected 200 got ${syncProducts2.status}`);
  if ((syncProducts2.json?.recordsCreated ?? -1) !== 0) {
    fail("Logo product sync #2 must be idempotent (0 creates)");
  }

  const customersAfter = await api("GET", `/api/erp/customers?connectionProfileId=${LOGO_CONNECTION}`);
  const customerCountAfter = Array.isArray(customersAfter.json) ? customersAfter.json.length : 0;
  if (customerCountAfter < customerCountBefore + created1) {
    fail("Logo customer projection count must increase after first sync");
  }

  const metadata = health.json?.integrationMetadata ?? {};
  if (metadata["logo.liveValidated"] === "true") {
    fail("test transport must not claim live Logo validation");
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-LOGO-1 adapter verification passed.");

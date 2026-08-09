import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`ERP-1L core verification failed: ${message}`);
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

console.log("Running ERP-1L core verifier.");

const adapterContract = readRequired("packages/CrevaFactOS.Persistence/Erp/IErpAdapter.cs");
const adapters = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpAdapters.cs");
const entities = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpEntities.cs");
const syncRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpSyncRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260809200000_AddErpIntegrationFoundation.cs",
);

requireText(entities, "External ERP remains system of record", "ErpEntities");
requireText(adapterContract, "interface IErpAdapter", "IErpAdapter");
requireText(entities, "ErpCapabilities", "ErpEntities");
requireText(entities, "CustomersRead = \"customers.read\"", "ErpCapabilities");
requireText(entities, "ProductsRead = \"products.read\"", "ErpCapabilities");
requireText(entities, "Writeback = \"writeback\"", "future writeback capability reserved");
requireText(adapterContract, "IReadOnlySet<string> Capabilities", "capability contract on adapter");

requireText(adapters, "class SimulatorErpAdapter", "simulator adapter");
requireText(adapters, "ERP-CUST-001", "simulator customer id");
requireText(adapters, "ABC Plastik Sanayi A.Ş.", "simulator customer name");
requireText(adapters, "ERP-PRD-001", "simulator product");
requireText(adapters, "PP CAP 32", "simulator product name");
requireText(adapters, "class LogoErpAdapterPlaceholder", "Logo readiness");
requireText(adapters, "class MikroErpAdapterPlaceholder", "Mikro readiness");
requireText(adapters, "class NetsisErpAdapterPlaceholder", "Netsis readiness");

requireText(entities, "class CanonicalErpCustomer", "canonical customer");
requireText(entities, "class CanonicalErpProduct", "canonical product");
requireText(entities, "ExternalId", "external identity");
requireText(syncRepo, "RecordsCreated", "idempotent sync counters");
requireText(syncRepo, "RecordsUpdated", "idempotent sync counters");
requireText(syncRepo, "RecordsSkipped", "idempotent sync counters");
requireText(syncRepo, "ConnectionProfileId == profile.Id", "external identity uniqueness path");
requireText(syncRepo, "ExternalId == record.ExternalId", "external identity key");

forbidText(adapterContract, "WriteCustomer", "IErpAdapter");
forbidText(adapterContract, "WriteProduct", "IErpAdapter");
forbidText(adapterContract, "PostDocument", "IErpAdapter");
forbidText(endpoints, "writeback", "erp endpoints");
forbidText(syncRepo, "ProductionOrders", "erp sync must not mutate Production");
forbidText(syncRepo, "QuantityOnHand", "ErpSyncRepository must not mutate stock quantities");
forbidText(syncRepo, "Accounting", "erp sync must not mutate accounting");

if (existsSync(resolve(projectRoot, "packages/CrevaFactOS.Persistence/Erp/ErpInventoryRepository.cs"))) {
  const inventoryRepo = readRequired("packages/CrevaFactOS.Persistence/Erp/ErpInventoryRepository.cs");
  forbidText(inventoryRepo, "StockMovement", "inventory projection must not create stock ledger");
  forbidText(inventoryRepo, "WriteInventory", "no inventory writeback");
}

forbidText(adapters, "Logo.Tiger", "no vendor-specific leakage in adapters file beyond provider key");
forbidText(syncRepo, "Logo.", "no Logo leakage outside adapter");
forbidText(syncRepo, "Mikro.", "no Mikro leakage outside adapter");
forbidText(syncRepo, "Netsis.", "no Netsis leakage outside adapter");
forbidText(entities, "LogoCompanyCode", "no vendor-specific fields on canonical entities");

requireText(migration, "erp_connection_profiles", "migration");
requireText(migration, "erp_customers", "migration");
requireText(migration, "erp_products", "migration");
requireText(migration, "erp_sync_runs", "migration");

console.log("ERP-1L core verification passed.");

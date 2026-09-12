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
const DEFAULT_COMPANY_CODE = "SIM-FACTORY-01";
const FAIL_COMPANY_CODE = "SIM-INVENTORY-FAIL";

function fail(message) {
  console.error(`ERP-3L sync reliability verification failed: ${message}`);
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

async function getConnection() {
  const r = await api("GET", `/api/erp/connections/${CONNECTION}`);
  if (r.status !== 200 || !r.json) fail(`connection read expected 200 got ${r.status}`);
  return r.json;
}

async function updateConnectionCompanyCode(connection, externalCompanyCode) {
  const r = await api("PUT", `/api/erp/connections/${CONNECTION}`, {
    facilityId: FACILITY,
    expectedVersion: connection.version ?? 1,
    name: connection.name,
    externalCompanyId: connection.externalCompanyId,
    externalCompanyCode,
    baseEndpoint: connection.baseEndpoint ?? null,
    connectionMode: connection.connectionMode,
    credentialReference: null,
    status: connection.status,
  });
  if (r.status !== 200) {
    fail(`connection update expected 200 got ${r.status}`);
  }
}

console.log("Running ERP-3L sync reliability verifier.");

const inventoryRepo = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpInventoryRepository.cs");
const adapters = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpAdapters.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const entities = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpEntities.cs");

requireText(inventoryRepo, "ListSyncReliabilityAsync", "ErpInventoryRepository");
requireText(inventoryRepo, "BuildSyncReliability", "sync reliability builder");
requireText(inventoryRepo, "ErpSyncEntityTypes.Inventory", "inventory reliability entity");
requireText(endpoints, "/sync-reliability", "sync reliability endpoint");
requireText(adapters, "SIM-INVENTORY-FAIL", "inventory fail sentinel");
requireText(entities, "Inventory = \"inventory\"", "inventory sync entity type");

console.log("Running ERP-3L sync reliability live smoke.");

let connectionSnapshot = null;

try {
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);

  let r = await api("GET", `/api/erp/sync-reliability?connectionProfileId=${CONNECTION}`);
  if (r.status !== 200 || !Array.isArray(r.json)) fail("sync reliability expected 200 array");
  const entityTypes = r.json.map((row) => row.entityType);
  for (const expected of ["customers", "products", "orders", "warehouses", "inventory", "purchasing"]) {
    if (!entityTypes.includes(expected)) fail(`sync reliability missing entity ${expected}`);
  }
  const inventoryBeforeFail = r.json.find((row) => row.entityType === "inventory");
  if (!inventoryBeforeFail?.lastSuccessfulSyncAtUtc) {
    fail("inventory reliability must include lastSuccessfulSyncAtUtc before failure test");
  }

  r = await api("GET", "/api/erp/inventory");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("inventory baseline expected 200 array");
  const baselineCount = r.json.length;
  if (baselineCount < 1) fail("inventory baseline count must be >= 1");

  connectionSnapshot = await getConnection();
  await updateConnectionCompanyCode(connectionSnapshot, FAIL_COMPANY_CODE);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);
  if (r.status !== 503) {
    fail(`failed inventory sync expected 503 got ${r.status}`);
  }

  r = await api("GET", "/api/erp/inventory");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("inventory after failed sync expected 200 array");
  if (r.json.length !== baselineCount) {
    fail(`failed sync must preserve inventory rows expected ${baselineCount} got ${r.json.length}`);
  }
  const preserved = r.json.find(
    (row) => row.externalProductId === "ERP-PRD-002" && row.externalWarehouseId === "ERP-WH-001",
  );
  if (!preserved) fail("ERP-PRD-002 balance must be preserved after failed sync");
  if (Number(preserved.quantityAvailable) !== 2500) {
    fail(`preserved available must remain 2500 got ${preserved.quantityAvailable}`);
  }

  r = await api("GET", `/api/erp/sync-reliability?connectionProfileId=${CONNECTION}`);
  if (r.status !== 200 || !Array.isArray(r.json)) fail("sync reliability after failure expected 200");
  const inventoryAfterFail = r.json.find((row) => row.entityType === "inventory");
  if (!inventoryAfterFail) fail("inventory reliability row missing after failure");
  if (inventoryAfterFail.lastRunStatus !== "failed") {
    fail(`inventory lastRunStatus must be failed got ${inventoryAfterFail.lastRunStatus}`);
  }
  if ((inventoryAfterFail.consecutiveFailures ?? 0) < 1) {
    fail("inventory consecutiveFailures must be >= 1 after failed sync");
  }
  if (!inventoryAfterFail.lastFailedSyncAtUtc) {
    fail("inventory lastFailedSyncAtUtc required after failed sync");
  }

  connectionSnapshot = await getConnection();
  await updateConnectionCompanyCode(connectionSnapshot, DEFAULT_COMPANY_CODE);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/inventory`);
  if (r.status !== 200) fail(`inventory recovery sync expected 200 got ${r.status}`);

  r = await api("GET", `/api/erp/sync-reliability?connectionProfileId=${CONNECTION}`);
  if (r.status !== 200 || !Array.isArray(r.json)) fail("sync reliability after recovery expected 200");
  const inventoryRecovered = r.json.find((row) => row.entityType === "inventory");
  if (inventoryRecovered?.lastRunStatus !== "completed") {
    fail(`inventory recovery lastRunStatus must be completed got ${inventoryRecovered?.lastRunStatus}`);
  }

  console.log(
    `ERP-3L sync reliability live smoke passed: baselineRows=${baselineCount}, consecutiveFailures=${inventoryAfterFail.consecutiveFailures}`,
  );
} catch (error) {
  if (connectionSnapshot) {
    try {
      const latest = await getConnection();
      if (latest.externalCompanyCode === FAIL_COMPANY_CODE) {
        await updateConnectionCompanyCode(latest, DEFAULT_COMPANY_CODE);
      }
    } catch {
      // best-effort restore
    }
  }
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-3L sync reliability verification passed.");

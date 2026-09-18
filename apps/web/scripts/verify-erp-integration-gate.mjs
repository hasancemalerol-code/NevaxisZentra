import { execSync, spawn } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE ?? "http://127.0.0.1:3001";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const REVIEWER_SUBJECT_ID = "bb000001-0020-4000-8000-000000000020";
const TENANT = "dev-pilot-tenant";
const DEFAULT_COMPANY_CODE = "SIM-FACTORY-01";
const CUSTOMER_TARGET = "bb000001-0091-4000-8000-000000000091";
const PRODUCT_TARGET_1 = "bb000001-0092-4000-8000-000000000092";
const PRODUCT_TARGET_2 = "bb000001-0093-4000-8000-000000000093";

function fail(message) {
  console.error(`ERP integration gate verification failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": TENANT,
  };
}

function getListeningPid(port) {
  try {
    const output = execSync(`netstat -ano | findstr ":${port}"`, { encoding: "utf8" });
    for (const line of output.split(/\r?\n/)) {
      if (line.includes("LISTENING")) {
        const parts = line.trim().split(/\s+/);
        const pid = Number(parts.at(-1));
        if (Number.isFinite(pid) && pid > 0) {
          return pid;
        }
      }
    }
  } catch {
    return null;
  }
  return null;
}

function stopPid(pid, label) {
  if (!pid) return;
  try {
    execSync(`taskkill /PID ${pid} /F`, { stdio: "ignore" });
    console.log(`Stopped ${label} PID ${pid}.`);
  } catch {
    console.warn(`Could not stop ${label} PID ${pid}.`);
  }
}

async function waitForHealth(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${BASE}/health/live`);
      if (response.ok) return true;
    } catch {
      // retry
    }
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 2000));
  }
  return false;
}

async function waitForErpReady(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(
        `${BASE}/api/erp/connections/${CONNECTION}?facilityId=${FACILITY}`,
        { headers: headers() },
      );
      if (response.status === 200) return true;
    } catch {
      // retry
    }
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 2000));
  }
  return false;
}

async function api(method, path, body, subject = OPERATOR) {
  const url = path.includes("facilityId=")
    ? `${BASE}${path}`
    : `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY}`;
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
  if (r.status !== 200) fail(`connection update expected 200 got ${r.status}`);
  return r.json;
}

function buildWritebackPayload(reference) {
  return JSON.stringify({
    customerExternalId: "ERP-CUST-001",
    orderReference: reference,
    requestedDeliveryDateUtc: "2026-09-01T00:00:00Z",
    lines: [{ productExternalId: "ERP-PRD-001", quantity: 100, unit: "AD" }],
  });
}

function expectedSimulatorReceipt(writebackId) {
  const compact = writebackId.replaceAll("-", "").toUpperCase();
  return `SIM-WB-${compact.slice(0, 8)}`;
}

function assertExecuteBlocked(result, label) {
  if (result.status === 200 || result.status === 201) {
    fail(`${label} must be blocked (got ${result.status})`);
  }
}

console.log("Running ERP integration gate verifier.");

const adapters = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpAdapters.cs");
const entities = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpEntities.cs");
const adapterContract = readRequired("packages/NevaxisZentra.Persistence/Erp/IErpAdapter.cs");

for (const [label, key] of [
  ["simulator", "Simulator = \"simulator\""],
  ["logo", "Logo = \"logo\""],
  ["mikro", "Mikro = \"mikro\""],
  ["netsis", "Netsis = \"netsis\""],
]) {
  requireText(entities, key, `provider key ${label}`);
}
requireText(entities, "CustomersRead = \"customers.read\"", "customers.read");
requireText(entities, "ProductsRead = \"products.read\"", "products.read");
requireText(entities, "OrdersRead = \"orders.read\"", "orders.read");
requireText(entities, "WarehousesRead = \"warehouses.read\"", "warehouses.read");
requireText(entities, "InventoryRead = \"inventory.read\"", "inventory.read");
requireText(entities, "PurchasingRead = \"purchasing.read\"", "purchasing.read");
requireText(entities, "ProductionRead = \"production.read\"", "production.read");
requireText(entities, "DocumentsRead = \"documents.read\"", "documents.read");
requireText(entities, "SalesOrdersWriteDraft = \"sales-orders.write-draft\"", "simulator writeback capability");
requireText(adapters, "class SimulatorErpAdapter", "simulator adapter");
const logoAdapter = readRequired("packages/NevaxisZentra.Persistence/Erp/Logo/LogoErpAdapter.cs");
requireText(logoAdapter, "class LogoErpAdapter", "logo adapter");
const mikroAdapter = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroErpAdapter.cs");
requireText(mikroAdapter, "class MikroErpAdapter", "mikro adapter");
forbidText(adapters, "class MikroErpAdapterPlaceholder", "mikro placeholder removed");
requireText(adapters, "class NetsisErpAdapterPlaceholder", "netsis placeholder");
requireText(adapterContract, "interface IErpAdapter", "IErpAdapter");
requireText(readRequired("packages/NevaxisZentra.Persistence/Erp/IErpWritebackAdapter.cs"), "interface IErpWritebackAdapter", "writeback adapter contract");

const duplicatePid = getListeningPid(5011);
if (duplicatePid) stopPid(duplicatePid, "duplicate API on 5011");

const canonicalPidBefore = getListeningPid(5000);
if (!canonicalPidBefore) fail(`canonical API must be listening on ${BASE}.`);
console.log(`Canonical API PID: ${canonicalPidBefore}`);

try {
  let r = await api("GET", `/api/erp/connections/${CONNECTION}`);
  if (r.status !== 200) fail(`connection profile expected 200 got ${r.status}`);
  if (r.json?.provider !== "simulator") fail(`provider must be simulator got ${r.json?.provider}`);
  if (r.json?.facilityId !== FACILITY) fail(`facility mapping must be explicit got ${r.json?.facilityId}`);
  if (!r.json?.externalCompanyCode) fail("external company mapping required");
  if (r.json?.id !== CONNECTION) fail(`connection id must match canonical profile`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/health-check`);
  if (r.status !== 200) fail(`health check expected 200 got ${r.status}`);
  if (r.json?.isHealthy !== true) fail(`health must be healthy got ${r.json?.isHealthy}`);
  if (typeof r.json?.latencyMs !== "number") fail("health latency required");
  if (!r.json?.checkedAtUtc) fail("health timestamp required");

  r = await api("GET", `/api/erp/connections/${CONNECTION}`);
  if (!r.json?.lastHealthCheckAtUtc) fail("last health timestamp must persist");
  if (!r.json?.version) fail("connection version must persist after health");

  const serializedConnection = JSON.stringify(r.json);
  if (/password|apiKey|connectionString|bearer/i.test(serializedConnection)) {
    fail("connection must not expose plaintext secrets");
  }

  async function syncTwice(entityPath, label) {
    let first = await api("POST", `/api/erp/connections/${CONNECTION}/sync/${entityPath}`);
    if (first.status !== 200) fail(`${label} sync #1 expected 200 got ${first.status}`);
    let second = await api("POST", `/api/erp/connections/${CONNECTION}/sync/${entityPath}`);
    if (second.status !== 200) fail(`${label} sync #2 expected 200 got ${second.status}`);
    if ((second.json?.recordsCreated ?? 0) !== 0) {
      fail(`${label} sync #2 created must be 0 got ${second.json?.recordsCreated}`);
    }
    return { first, second };
  }

  const customerSync = await syncTwice("customers", "customer");
  const productSync = await syncTwice("products", "product");
  const orderSync = await syncTwice("orders", "order");
  const warehouseSync = await syncTwice("warehouses", "warehouse");
  const inventorySync = await syncTwice("inventory", "inventory");
  const purchasingSync = await syncTwice("purchasing", "purchasing");
  const productionSync = await syncTwice("production", "production");
  const documentsSync = await syncTwice("documents", "documents");

  r = await api("GET", "/api/erp/customers");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("customers list expected 200 array");
  const customerCount = r.json.length;
  const cust001 = r.json.find((row) => row.externalId === "ERP-CUST-001");
  if (!cust001) fail("ERP-CUST-001 missing");
  if (r.json.filter((row) => row.externalId === "ERP-CUST-001").length !== 1) {
    fail("ERP-CUST-001 duplicate detected");
  }

  r = await api("GET", "/api/erp/products");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("products list expected 200 array");
  const productCount = r.json.length;
  if (!r.json.find((row) => row.externalId === "ERP-PRD-001")) fail("ERP-PRD-001 missing");
  if (!r.json.find((row) => row.externalId === "ERP-PRD-002")) fail("ERP-PRD-002 missing");

  r = await api("GET", "/api/erp/orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("orders list expected 200 array");
  const orderCount = r.json.length;
  const order = r.json.find((row) => row.externalId === "ERP-ORD-001");
  if (!order) fail("ERP-ORD-001 missing");

  r = await api("GET", `/api/erp/orders/${order.id}`);
  if (r.status !== 200) fail("order detail expected 200");
  const lineCount = r.json?.lines?.length ?? 0;
  if (lineCount < 2) fail(`ERP-ORD-001 must have 2 lines got ${lineCount}`);

  r = await api("GET", `/api/erp/orders/${order.id}/readiness`);
  if (r.status !== 200) fail("readiness expected 200");
  const readinessBefore = r.json?.readyForOperationalUse;
  const blockingBefore = (r.json?.blockingReasons ?? []).map((b) => b.code);

  if (readinessBefore !== true) {
    if (readinessBefore !== false) fail("readiness before mapping must be false when not ready");
    if (!blockingBefore.some((c) => c === "customer-unmapped" || c === "product-unmapped")) {
      fail(`blocking reasons required got ${blockingBefore.join(",")}`);
    }

    r = await api("GET", "/api/erp/reconciliation/customers");
    const custRow = r.json?.find((row) => row.externalId === "ERP-CUST-001");
    if (!custRow?.mappingId) fail("customer mapping row missing");
    r = await api("POST", `/api/erp/mappings/${custRow.mappingId}/confirm`, {
      facilityId: FACILITY,
      targetEntityId: CUSTOMER_TARGET,
      expectedVersion: custRow.mappingVersion ?? 1,
    });
    if (r.status !== 200) fail(`customer mapping confirm expected 200 got ${r.status}`);

    r = await api("GET", "/api/erp/reconciliation/products");
    const prod1 = r.json?.find((row) => row.externalId === "ERP-PRD-001");
    const prod2 = r.json?.find((row) => row.externalId === "ERP-PRD-002");
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
  }

  r = await api("GET", `/api/erp/orders/${order.id}/readiness`);
  if (r.json?.readyForOperationalUse !== true) {
    fail(`readiness after mapping must be true got ${r.json?.readyForOperationalUse}`);
  }

  r = await api("GET", `/api/erp/orders/${order.id}/material-availability`);
  if (r.status !== 200) fail("material availability expected 200");
  const materialLine = (r.json?.lines ?? []).find((line) => line.externalProductId === "ERP-PRD-002");
  if (!materialLine) fail("ERP-PRD-002 material line missing");
  if (Number(materialLine.requiredQuantity) !== 5000) {
    fail(`required must be 5000 got ${materialLine.requiredQuantity}`);
  }
  if (Number(materialLine.availableQuantity) !== 2500) {
    fail(`available must be 2500 got ${materialLine.availableQuantity}`);
  }
  if (Number(materialLine.shortageQuantity) !== 2500) {
    fail(`shortage must be 2500 got ${materialLine.shortageQuantity}`);
  }
  const coverage = materialLine.purchaseCoverage;
  if (!coverage) fail("purchase coverage required");
  if (Number(coverage.openPurchaseQuantity) !== 5000) {
    fail(`open purchase must be 5000 got ${coverage.openPurchaseQuantity}`);
  }
  if (coverage.coverageStatus !== "covered") {
    fail(`coverage must be covered got ${coverage.coverageStatus}`);
  }

  r = await api("GET", "/api/erp/warehouses");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("warehouses list expected 200 array");
  if (r.json.length !== 2) fail(`warehouse final count must be 2 got ${r.json.length}`);
  if (!r.json.find((row) => row.externalId === "ERP-WH-001")) fail("ERP-WH-001 missing");
  if (!r.json.find((row) => row.externalId === "ERP-WH-002")) fail("ERP-WH-002 missing");

  r = await api("GET", "/api/erp/inventory");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("inventory list expected 200 array");
  if (r.json.length !== 2) fail(`inventory final count must be 2 got ${r.json.length}`);
  if (!r.json.find((row) => row.externalProductId === "ERP-PRD-001" && row.externalWarehouseId === "ERP-WH-001")) {
    fail("ERP-PRD-001 / ERP-WH-001 balance missing");
  }
  if (!r.json.find((row) => row.externalProductId === "ERP-PRD-002" && row.externalWarehouseId === "ERP-WH-001")) {
    fail("ERP-PRD-002 / ERP-WH-001 balance missing");
  }

  r = await api("GET", "/api/erp/purchase-orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("purchase orders expected 200 array");
  const poCount = r.json.length;
  const po = r.json.find((row) => row.externalId === "ERP-PO-001");
  if (!po) fail("ERP-PO-001 missing");
  r = await api("GET", `/api/erp/purchase-orders/${po.id}`);
  if ((r.json?.lines?.length ?? 0) < 1) fail("ERP-PO-001 must have 1 line");

  r = await api("GET", "/api/erp/production-orders");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("production orders expected 200 array");
  const productionCount = r.json.length;
  const mo = r.json.find((row) => row.externalId === "ERP-MO-001");
  if (!mo) fail("ERP-MO-001 missing");
  if (Number(mo.plannedQuantity) !== 10000) fail(`ERP-MO-001 planned must be 10000`);
  if (Number(mo.completedQuantity) !== 4000) fail(`ERP-MO-001 completed must be 4000`);
  if (Number(mo.scrapQuantity) !== 100) fail(`ERP-MO-001 scrap must be 100`);
  if (mo.status !== "in-progress") fail(`ERP-MO-001 status must be in-progress got ${mo.status}`);

  r = await api("GET", "/api/erp/documents");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("documents expected 200 array");
  const documentCount = r.json.length;
  const doc001 = r.json.find((row) => row.externalId === "ERP-DOC-001");
  const doc002 = r.json.find((row) => row.externalId === "ERP-DOC-002");
  const doc003 = r.json.find((row) => row.externalId === "ERP-DOC-003");
  if (!doc001 || doc001.relatedExternalEntityId !== "ERP-ORD-001") fail("ERP-DOC-001 -> ERP-ORD-001");
  if (!doc002 || doc002.relatedExternalEntityId !== "ERP-PO-001") fail("ERP-DOC-002 -> ERP-PO-001");
  if (!doc003 || doc003.relatedExternalEntityId !== "ERP-MO-001") fail("ERP-DOC-003 -> ERP-MO-001");

  r = await api("GET", `/api/erp/sync-reliability?connectionProfileId=${CONNECTION}`);
  if (r.status !== 200 || !Array.isArray(r.json)) fail("sync reliability expected 200 array");
  const entityTypes = r.json.map((row) => row.entityType);
  for (const expected of ["customers", "products", "orders", "warehouses", "inventory", "purchasing", "production", "documents"]) {
    if (!entityTypes.includes(expected)) fail(`sync reliability missing ${expected}`);
    const row = r.json.find((item) => item.entityType === expected);
    if (!row?.lastRunStatus) fail(`${expected} lastRunStatus required`);
    if (typeof row.consecutiveFailures !== "number") fail(`${expected} consecutiveFailures required`);
  }

  for (const row of r.json) {
    if (row.freshnessStatus && !["fresh", "stale", "unknown"].includes(row.freshnessStatus)) {
      fail(`unexpected freshness ${row.freshnessStatus} for ${row.entityType}`);
    }
  }

  async function exerciseFailMode(failCode, entityPath, listPath, preserveCheck) {
    const connection = await getConnection();
    await updateConnectionCompanyCode(connection, failCode);
    const failed = await api("POST", `/api/erp/connections/${CONNECTION}/sync/${entityPath}`);
    if (failed.status !== 503) fail(`${failCode} sync expected 503 got ${failed.status}`);
    const after = await api("GET", listPath);
    if (after.status !== 200 || !Array.isArray(after.json)) fail(`${failCode} list after fail expected 200`);
    if (after.json.length === 0) fail(`${failCode} must preserve prior data`);
    preserveCheck(after.json);
    const reliability = await api("GET", `/api/erp/sync-reliability?connectionProfileId=${CONNECTION}`);
    const rel = reliability.json?.find((item) => item.entityType === entityPath);
    if (!rel?.lastFailedSyncAtUtc) fail(`${failCode} must record last failure`);
    if (rel?.currentErrorMessage && /password|token|secret/i.test(rel.currentErrorMessage)) {
      fail(`${failCode} sanitized error must not leak secrets`);
    }
    const restored = await getConnection();
    await updateConnectionCompanyCode(restored, DEFAULT_COMPANY_CODE);
  }

  await exerciseFailMode("SIM-INVENTORY-FAIL", "inventory", "/api/erp/inventory", (rows) => {
    const preserved = rows.find((row) => row.externalProductId === "ERP-PRD-002" && row.externalWarehouseId === "ERP-WH-001");
    if (!preserved || Number(preserved.quantityAvailable) !== 2500) fail("inventory prior data not preserved");
  });
  await exerciseFailMode("SIM-PRODUCTION-FAIL", "production", "/api/erp/production-orders", (rows) => {
    if (!rows.find((row) => row.externalId === "ERP-MO-001")) fail("production prior data not preserved");
  });
  await exerciseFailMode("SIM-DOCUMENTS-FAIL", "documents", "/api/erp/documents", (rows) => {
    if (!rows.find((row) => row.externalId === "ERP-DOC-001")) fail("documents prior data not preserved");
  });

  const runSuffix = Date.now().toString(36);
  const idempotencyKey = `verify-erp-integration-gate-${runSuffix}`;
  const payloadJson = buildWritebackPayload(`WB-GATE-${runSuffix}`);
  r = await api("POST", "/api/erp/writebacks", {
    facilityId: FACILITY,
    connectionProfileId: CONNECTION,
    commandType: "sales-order.create-draft",
    payloadJson,
    idempotencyKey,
    targetEntityType: null,
    targetExternalId: null,
  });
  if (r.status !== 201) fail(`writeback create expected 201 got ${r.status}`);
  const writebackId = r.json?.id;
  let writebackVersion = r.json?.version;
  r = await api("GET", `/api/erp/writebacks/${writebackId}`);
  if (r.status !== 200) fail("writeback detail after create expected 200");
  writebackVersion = r.json?.version ?? writebackVersion;
  const payloadHash = r.json?.payloadHash;
  if (!payloadHash) fail("writeback payloadHash required");

  r = await api("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: writebackVersion,
  });
  assertExecuteBlocked(r, "execute draft");

  r = await api("POST", `/api/erp/writebacks/${writebackId}/submit`, {
    facilityId: FACILITY,
    expectedVersion: writebackVersion,
  });
  if (r.status !== 200) fail(`submit expected 200 got ${r.status}`);
  writebackVersion = r.json?.version;

  r = await api("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: writebackVersion,
  });
  assertExecuteBlocked(r, "execute pending approval");

  r = await api(
    "POST",
    `/api/erp/writebacks/${writebackId}/approve`,
    { facilityId: FACILITY, expectedVersion: writebackVersion, comment: "Integration gate approval" },
    REVIEWER,
  );
  if (r.status !== 200) fail(`approve expected 200 got ${r.status}`);
  writebackVersion = r.json?.version;

  r = await api("GET", `/api/erp/writebacks/${writebackId}`);
  const approval = (r.json?.approvals ?? []).find((row) => row.decision === "approved");
  if (!approval) fail("approval record required");
  if (approval.decidedBySubjectId !== REVIEWER_SUBJECT_ID) {
    fail(`approver must be server-derived ${REVIEWER_SUBJECT_ID}`);
  }
  if (approval.requestVersion == null) fail("approval RequestVersion required");
  if (!approval.payloadHash) fail("approval PayloadHash required");
  if (approval.payloadHash !== payloadHash) fail("approval must bind exact PayloadHash");

  r = await api("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: writebackVersion,
  });
  if (r.status !== 200) fail(`execute expected 200 got ${r.status}`);
  const receipt = r.json?.externalExecutionReference ?? (await api("GET", `/api/erp/writebacks/${writebackId}`)).json?.externalExecutionReference;
  if (receipt !== expectedSimulatorReceipt(writebackId)) fail(`receipt mismatch got ${receipt}`);

  r = await api("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: r.json?.version ?? writebackVersion,
  });
  if (r.status !== 200) fail(`repeat execute expected 200 got ${r.status}`);

  const restartSnapshot = {
    connectionId: CONNECTION,
    customerCount,
    productCount,
    orderCount,
    lineCount,
    warehouseCount: 2,
    inventoryCount: 2,
    poCount,
    productionCount,
    documentCount,
    writebackId,
    receipt,
    approvalSubject: approval.decidedBySubjectId,
    cust001ExternalId: "ERP-CUST-001",
    orderExternalId: "ERP-ORD-001",
  };

  stopPid(canonicalPidBefore, "canonical API on 5000");
  const restarted = spawn(
    process.platform === "win32" ? "dotnet.exe" : "dotnet",
    ["run", "--project", "apps/api/NevaxisZentra.Api.csproj", "--no-launch-profile"],
    {
      cwd: projectRoot,
      detached: true,
      stdio: "ignore",
      env: {
        ...process.env,
        ASPNETCORE_URLS: "http://localhost:5000",
        ASPNETCORE_ENVIRONMENT: process.env.ASPNETCORE_ENVIRONMENT ?? "Development",
      },
    },
  );
  restarted.unref();

  if (!(await waitForHealth())) fail("API restart failed — live health did not recover on port 5000");
  if (!(await waitForErpReady())) fail("API restart failed — ERP connection read did not recover on port 5000");

  r = await api("GET", `/api/erp/connections/${CONNECTION}`);
  if (r.status !== 200 || r.json?.provider !== "simulator") fail("connection restart readback failed");

  r = await api("GET", "/api/erp/customers");
  if (r.json?.length !== restartSnapshot.customerCount) fail("customer restart readback count mismatch");
  if (!r.json?.find((row) => row.externalId === "ERP-CUST-001")) fail("ERP-CUST-001 restart readback failed");

  r = await api("GET", "/api/erp/products");
  if (r.json?.length !== restartSnapshot.productCount) fail("product restart readback count mismatch");

  r = await api("GET", "/api/erp/orders");
  if (r.json?.length !== restartSnapshot.orderCount) fail("order restart readback count mismatch");
  if (!r.json?.find((row) => row.externalId === "ERP-ORD-001")) fail("ERP-ORD-001 restart readback failed");

  r = await api("GET", "/api/erp/warehouses");
  if (r.json?.length !== 2) fail("warehouse restart readback failed");

  r = await api("GET", "/api/erp/inventory");
  if (r.json?.length !== 2) fail("inventory restart readback failed");

  r = await api("GET", "/api/erp/purchase-orders");
  if (r.json?.length !== restartSnapshot.poCount) fail("purchasing restart readback failed");

  r = await api("GET", "/api/erp/production-orders");
  if (r.json?.length !== restartSnapshot.productionCount) fail("production restart readback failed");
  if (!r.json?.find((row) => row.externalId === "ERP-MO-001")) fail("ERP-MO-001 restart readback failed");

  r = await api("GET", "/api/erp/documents");
  if (r.json?.length !== restartSnapshot.documentCount) fail("documents restart readback failed");

  r = await api("GET", `/api/erp/writebacks/${writebackId}`);
  if (r.status !== 200) fail("writeback restart readback failed");
  if (r.json?.externalExecutionReference !== restartSnapshot.receipt) fail("receipt restart readback failed");
  const approvalAfter = (r.json?.approvals ?? []).find((row) => row.decision === "approved");
  if (approvalAfter?.decidedBySubjectId !== restartSnapshot.approvalSubject) fail("approval restart readback failed");

  r = await api("GET", `/api/erp/sync-reliability?connectionProfileId=${CONNECTION}`);
  if (r.status !== 200) fail("reliability restart readback failed");

  const uiRoutes = [
    "/erp-entegrasyonlari/baglanti",
    "/erp-entegrasyonlari/senkronizasyonlar",
    "/erp-entegrasyonlari/siparisler",
    "/erp-entegrasyonlari/eslestirme",
    "/erp-entegrasyonlari/stok",
    "/erp-entegrasyonlari/satin-alma",
    "/erp-entegrasyonlari/uretim",
    "/erp-entegrasyonlari/belgeler",
    "/erp-entegrasyonlari/onayli-islemler",
    "/erp-entegrasyonlari/saglik",
    "/erp-entegrasyonlari/hata-kuyrugu",
  ];
  for (const route of uiRoutes) {
    const page = await fetch(`${WEB}${route}`);
    if (page.status !== 200) fail(`${route} expected HTTP 200 got ${page.status}`);
  }

  r = await api("GET", "/api/erp/orders");
  const orderForDetail = r.json?.find((row) => row.externalId === "ERP-ORD-001");
  if (orderForDetail?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/siparisler/${orderForDetail.id}`);
    if (detail.status !== 200) fail("order detail page expected 200");
  }
  const poForDetail = (await api("GET", "/api/erp/purchase-orders")).json?.find((row) => row.externalId === "ERP-PO-001");
  if (poForDetail?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/satin-alma/${poForDetail.id}`);
    if (detail.status !== 200) fail("purchase detail page expected 200");
  }
  const moForDetail = (await api("GET", "/api/erp/production-orders")).json?.find((row) => row.externalId === "ERP-MO-001");
  if (moForDetail?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/uretim/${moForDetail.id}`);
    if (detail.status !== 200) fail("production detail page expected 200");
  }
  const docForDetail = (await api("GET", "/api/erp/documents")).json?.find((row) => row.externalId === "ERP-DOC-001");
  if (docForDetail?.id) {
    const detail = await fetch(`${WEB}/erp-entegrasyonlari/belgeler/${docForDetail.id}`);
    if (detail.status !== 200) fail("document detail page expected 200");
  }
  const wbDetail = await fetch(`${WEB}/erp-entegrasyonlari/onayli-islemler/${writebackId}`);
  if (wbDetail.status !== 200) fail("writeback detail page expected 200");

  console.log(
    `ERP integration gate live scenario passed: customers=${customerCount}, orders=${orderCount}, writeback=${writebackId}, receipt=${receipt}`,
  );
  console.log(
    `Sync idempotency: cust2=${customerSync.second.json?.recordsCreated}, prod2=${productSync.second.json?.recordsCreated}, ord2=${orderSync.second.json?.recordsCreated}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

const canonicalPidAfter = getListeningPid(5000);
console.log(`Canonical API PID after gate: ${canonicalPidAfter ?? "unknown"}`);
console.log("ERP integration gate verification passed.");

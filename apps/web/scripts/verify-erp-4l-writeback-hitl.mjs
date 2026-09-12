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
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-4L writeback HITL verification failed: ${message}`);
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
    "X-NevaxisFactOS-Tenant": TENANT,
  };
}

async function readApi(method, path, subject = OPERATOR) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${FACILITY}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject),
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

async function writeApi(method, path, body, subject = OPERATOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(subject),
    body: JSON.stringify(body),
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

function buildWritebackPayload(reference) {
  return JSON.stringify({
    customerExternalId: "ERP-CUST-001",
    orderReference: reference,
    requestedDeliveryDateUtc: "2026-09-01T00:00:00Z",
    lines: [{ productExternalId: "ERP-PRD-001", quantity: 100, unit: "AD" }],
  });
}

function assertExecuteBlocked(result, label) {
  if (result.status === 200 || result.status === 201) {
    fail(`${label} must be blocked (got ${result.status})`);
  }
}

function expectedSimulatorReceipt(writebackId) {
  const compact = writebackId.replaceAll("-", "").toUpperCase();
  return `SIM-WB-${compact.slice(0, 8)}`;
}

console.log("Running ERP-4L writeback HITL verifier.");

const writebackRepo = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpWritebackRepository.cs");
const writebackAdapter = readRequired("packages/NevaxisFactOS.Persistence/Erp/IErpWritebackAdapter.cs");
const adapters = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpAdapters.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const entities = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpEntities.cs");

requireText(writebackRepo, "ResolveExecutionBlock", "writeback execution guard");
requireText(writebackRepo, "WritebackNotApproved", "writeback not-approved guard");
requireText(writebackRepo, "ErpWritebackStatuses.Succeeded", "writeback succeeded idempotency");
requireText(adapters, "SimulatorWritebackAdapter", "simulator writeback adapter");
requireText(adapters, "SIM-WB-", "simulator writeback receipt");
requireText(endpoints, "MapPost(\"/writebacks", "writeback create endpoint");
requireText(endpoints, "/submit", "writeback submit endpoint");
requireText(endpoints, "/approve", "writeback approve endpoint");
requireText(endpoints, "/reject", "writeback reject endpoint");
requireText(endpoints, "/execute", "writeback execute endpoint");
requireText(entities, "SalesOrderCreateDraft", "sales-order.create-draft command");

forbidText(writebackAdapter, "LogoErpAdapterPlaceholder", "Logo must not implement writeback adapter");
forbidText(writebackAdapter, "MikroErpAdapterPlaceholder", "Mikro must not implement writeback adapter");
forbidText(writebackAdapter, "NetsisErpAdapterPlaceholder", "Netsis must not implement writeback adapter");
forbidText(adapters, "class LogoErpAdapterPlaceholder : IErpWritebackAdapter", "Logo writeback unsupported");
forbidText(adapters, "class MikroErpAdapter : IErpWritebackAdapter", "Mikro writeback unsupported");
forbidText(adapters, "class NetsisErpAdapterPlaceholder : IErpWritebackAdapter", "Netsis writeback unsupported");

requireText(endpoints, "SimulatorWritebackAdapter", "only simulator writeback adapter registered");

console.log("Running ERP-4L writeback HITL live scenario.");

try {
  const runSuffix = Date.now().toString(36);
  const idempotencyKey = `verify-erp-4l-hitl-${runSuffix}`;
  const payloadJson = buildWritebackPayload(`WB-HITL-${runSuffix}`);

  let r = await writeApi("POST", "/api/erp/writebacks", {
    facilityId: FACILITY,
    connectionProfileId: CONNECTION,
    commandType: "sales-order.create-draft",
    payloadJson,
    idempotencyKey,
    targetEntityType: null,
    targetExternalId: null,
  });
  if (r.status !== 201) fail(`create draft expected 201 got ${r.status}`);
  const writebackId = r.json?.id;
  let version = r.json?.version;
  if (!writebackId || !version) fail("create draft must return id and version");

  r = await writeApi("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: version,
  });
  assertExecuteBlocked(r, "execute on draft");

  r = await writeApi("POST", `/api/erp/writebacks/${writebackId}/submit`, {
    facilityId: FACILITY,
    expectedVersion: version,
  });
  if (r.status !== 200) fail(`submit expected 200 got ${r.status}`);
  version = r.json?.version;
  if (!version) fail("submit must return version");

  r = await writeApi("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: version,
  });
  assertExecuteBlocked(r, "execute on pending-approval");

  r = await writeApi(
    "POST",
    `/api/erp/writebacks/${writebackId}/approve`,
    { facilityId: FACILITY, expectedVersion: version, comment: "ERP-4L verifier approval" },
    REVIEWER,
  );
  if (r.status !== 200) fail(`approve expected 200 got ${r.status}`);
  version = r.json?.version;
  if (!version) fail("approve must return version");

  r = await writeApi("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: version,
  });
  if (r.status !== 200) fail(`execute after approve expected 200 got ${r.status}`);
  version = r.json?.version;
  if (!version) fail("execute must return version");

  r = await readApi("GET", `/api/erp/writebacks/${writebackId}`);
  if (r.status !== 200) fail("writeback detail expected 200");
  if (r.json?.status !== "succeeded") {
    fail(`writeback status must be succeeded got ${r.json?.status}`);
  }
  const receipt = r.json?.externalExecutionReference;
  const expectedReceipt = expectedSimulatorReceipt(writebackId);
  if (receipt !== expectedReceipt) {
    fail(`simulator receipt must be ${expectedReceipt} got ${receipt}`);
  }
  if (!String(receipt).startsWith("SIM-WB-")) {
    fail("externalExecutionReference must start with SIM-WB-");
  }

  const detailVersion = r.json?.version;
  r = await writeApi("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: detailVersion,
  });
  if (r.status !== 200) fail(`repeat execute expected 200 got ${r.status}`);

  r = await readApi("GET", `/api/erp/writebacks/${writebackId}`);
  if (r.json?.externalExecutionReference !== receipt) {
    fail("repeat execute must not create duplicate receipt");
  }

  const rejectKey = `verify-erp-4l-reject-${runSuffix}`;
  r = await writeApi("POST", "/api/erp/writebacks", {
    facilityId: FACILITY,
    connectionProfileId: CONNECTION,
    commandType: "sales-order.create-draft",
    payloadJson: buildWritebackPayload(`WB-REJECT-${runSuffix}`),
    idempotencyKey: rejectKey,
    targetEntityType: null,
    targetExternalId: null,
  });
  if (r.status !== 201) fail(`reject scenario create expected 201 got ${r.status}`);
  const rejectId = r.json?.id;
  let rejectVersion = r.json?.version;

  r = await writeApi("POST", `/api/erp/writebacks/${rejectId}/submit`, {
    facilityId: FACILITY,
    expectedVersion: rejectVersion,
  });
  if (r.status !== 200) fail(`reject scenario submit expected 200 got ${r.status}`);
  rejectVersion = r.json?.version;

  r = await writeApi(
    "POST",
    `/api/erp/writebacks/${rejectId}/reject`,
    { facilityId: FACILITY, expectedVersion: rejectVersion, comment: "ERP-4L verifier rejection" },
    REVIEWER,
  );
  if (r.status !== 200) fail(`reject expected 200 got ${r.status}`);
  rejectVersion = r.json?.version;

  r = await writeApi("POST", `/api/erp/writebacks/${rejectId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: rejectVersion,
  });
  assertExecuteBlocked(r, "execute on rejected");

  console.log(
    `ERP-4L writeback HITL live scenario passed: writebackId=${writebackId}, receipt=${receipt}`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-4L writeback HITL verification passed.");

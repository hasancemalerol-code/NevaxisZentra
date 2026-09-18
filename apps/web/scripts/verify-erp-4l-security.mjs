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
const REVIEWER = "planning-pilot-reviewer";
const REVIEWER_SUBJECT_ID = "bb000001-0020-4000-8000-000000000020";
const NEGATIVE = "quality-negative-test-user";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-4L security verification failed: ${message}`);
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

async function readApi(method, path, subject = OPERATOR, facilityId = FACILITY) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facilityId}`;
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
    lines: [{ productExternalId: "ERP-PRD-001", quantity: 50, unit: "AD" }],
  });
}

console.log("Running ERP-4L security verifier.");

const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Erp/ErpEndpointHelpers.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809230000_AddErpProductionDocumentsAndWritebackFoundation.cs",
);
const productionRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpProductionRepository.cs");
const writebackRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpWritebackRepository.cs");
const permissionKeys = readRequired("packages/NevaxisZentra.Persistence/Authorization/ErpPermissionKeys.cs");

requireText(permissionKeys, "WritebackRequest", "writeback request permission");
requireText(permissionKeys, "WritebackApprove", "writeback approve permission");
requireText(permissionKeys, "WritebackExecute", "writeback execute permission");
requireText(endpoints, "ErpPermissionKeys.Sync", "sync permission");
requireText(endpoints, "ErpPermissionKeys.WritebackRequest", "writeback request route permission");
requireText(endpoints, "ErpPermissionKeys.WritebackApprove", "writeback approve route permission");
requireText(endpoints, "ErpPermissionKeys.WritebackExecute", "writeback execute route permission");
requireText(endpoints, "ResolveReadAccessAsync", "read permission resolver");
requireText(helpers, "Status401Unauthorized", "401 mapping");
requireText(helpers, "Status403Forbidden", "403 mapping");
requireText(helpers, "Status409Conflict", "409 mapping");
requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration RLS");
requireText(migration, "erp_production_orders", "production migration");
requireText(migration, "erp_documents", "documents migration");
requireText(migration, "erp_writeback_requests", "writeback migration");
requireText(migration, "erp_writeback_approvals", "writeback approvals migration");
requireText(migration, "GRANT SELECT, INSERT ON TABLE erp_writeback_approvals", "append-only approvals grant");
requireText(writebackRepo, "DecidedBySubjectId = context.IdentitySubjectId", "server-derived approver");

forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "erp endpoints");
forbidText(productionRepo, "Logo.", "no Logo leakage outside adapter");
forbidText(productionRepo, "Mikro.", "no Mikro leakage outside adapter");
forbidText(productionRepo, "Netsis.", "no Netsis leakage outside adapter");

const approveRequestSection = endpoints.slice(
  endpoints.indexOf("internal sealed record ApproveErpWritebackRequest"),
  endpoints.indexOf("internal sealed record RejectErpWritebackRequest"),
);
forbidText(approveRequestSection, "DecidedBySubjectId", "approve request must not accept approver");
forbidText(approveRequestSection, "ApproverSubjectId", "approve request must not accept approver");

console.log("Running ERP-4L security live checks.");

try {
  let r = await fetch(`${BASE}/api/erp/production-orders?facilityId=${FACILITY}`);
  if (r.status !== 401) fail(`401 expected without auth got ${r.status}`);

  r = await readApi("GET", "/api/erp/production-orders", NEGATIVE);
  if (r.status !== 403) fail(`missing erp.read on production expected 403 got ${r.status}`);

  r = await readApi("GET", "/api/erp/documents", NEGATIVE);
  if (r.status !== 403) fail(`missing erp.read on documents expected 403 got ${r.status}`);

  r = await readApi("GET", "/api/erp/writebacks", NEGATIVE);
  if (r.status !== 403) fail(`missing erp.read on writebacks expected 403 got ${r.status}`);

  r = await readApi("POST", `/api/erp/connections/${CONNECTION}/sync/production`, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.sync on production expected 403 got ${r.status}`);

  r = await readApi("POST", `/api/erp/connections/${CONNECTION}/sync/documents`, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.sync on documents expected 403 got ${r.status}`);

  r = await writeApi(
    "POST",
    "/api/erp/writebacks",
    {
      facilityId: FACILITY,
      connectionProfileId: CONNECTION,
      commandType: "sales-order.create-draft",
      payloadJson: buildWritebackPayload("WB-SEC-NEG"),
      idempotencyKey: `verify-erp-4l-sec-neg-${Date.now()}`,
      targetEntityType: null,
      targetExternalId: null,
    },
    NEGATIVE,
  );
  if (r.status !== 403) fail(`missing erp.writeback.request expected 403 got ${r.status}`);

  r = await readApi("GET", "/api/erp/production-orders", OPERATOR, UNAUTHORIZED_FACILITY);
  if (r.status !== 403) fail(`wrong facility on production expected 403 got ${r.status}`);

  const crossTenant = await fetch(`${BASE}/api/erp/documents?facilityId=${FACILITY}`, {
    headers: {
      ...headers(OPERATOR),
      "X-NevaxisZentra-Tenant": "other-tenant-not-pilot",
    },
  });
  if (crossTenant.status !== 403) {
    fail(`cross-tenant isolation expected 403 got ${crossTenant.status}`);
  }

  await readApi("POST", `/api/erp/connections/${CONNECTION}/sync/production`);
  await readApi("POST", `/api/erp/connections/${CONNECTION}/sync/documents`);

  r = await readApi("GET", "/api/erp/production-orders");
  if (r.status !== 200) fail("authorized production read expected 200");

  r = await readApi("GET", "/api/erp/documents");
  if (r.status !== 200) fail("authorized documents read expected 200");

  r = await readApi("GET", "/api/erp/writebacks");
  if (r.status !== 200) fail("authorized writebacks read expected 200");

  const deleteProduction = await fetch(
    `${BASE}/api/erp/production-orders/f0000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`,
    { method: "DELETE", headers: headers() },
  );
  if (deleteProduction.status !== 405 && deleteProduction.status !== 404) {
    fail(`DELETE production unavailable expected 405/404 got ${deleteProduction.status}`);
  }

  const deleteDocument = await fetch(
    `${BASE}/api/erp/documents/f0000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`,
    { method: "DELETE", headers: headers() },
  );
  if (deleteDocument.status !== 405 && deleteDocument.status !== 404) {
    fail(`DELETE document unavailable expected 405/404 got ${deleteDocument.status}`);
  }

  const deleteWriteback = await fetch(
    `${BASE}/api/erp/writebacks/f0000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`,
    { method: "DELETE", headers: headers() },
  );
  if (deleteWriteback.status !== 405 && deleteWriteback.status !== 404) {
    fail(`DELETE writeback unavailable expected 405/404 got ${deleteWriteback.status}`);
  }

  const runSuffix = Date.now().toString(36);
  r = await writeApi("POST", "/api/erp/writebacks", {
    facilityId: FACILITY,
    connectionProfileId: CONNECTION,
    commandType: "sales-order.create-draft",
    payloadJson: buildWritebackPayload(`WB-SEC-STALE-${runSuffix}`),
    idempotencyKey: `verify-erp-4l-sec-stale-${runSuffix}`,
    targetEntityType: null,
    targetExternalId: null,
  });
  if (r.status !== 201) fail(`security writeback create expected 201 got ${r.status}`);
  const writebackId = r.json?.id;
  let version = r.json?.version;

  r = await writeApi("POST", `/api/erp/writebacks/${writebackId}/submit`, {
    facilityId: FACILITY,
    expectedVersion: version,
  });
  if (r.status !== 200) fail(`security writeback submit expected 200 got ${r.status}`);
  version = r.json?.version;

  const staleApprove = await writeApi(
    "POST",
    `/api/erp/writebacks/${writebackId}/approve`,
    { facilityId: FACILITY, expectedVersion: 99999, comment: null },
    REVIEWER,
  );
  if (staleApprove.status !== 409) {
    fail(`stale writeback approval expected 409 got ${staleApprove.status}`);
  }

  const staleNegative = await writeApi(
    "POST",
    `/api/erp/writebacks/${writebackId}/approve`,
    { facilityId: FACILITY, expectedVersion: 99999, comment: null },
    NEGATIVE,
  );
  if (staleNegative.status !== 403) {
    fail(`missing erp.writeback.approve expected 403 got ${staleNegative.status}`);
  }

  const [c1, c2] = await Promise.all([
    writeApi(
      "POST",
      `/api/erp/writebacks/${writebackId}/approve`,
      { facilityId: FACILITY, expectedVersion: version, comment: "concurrent-1" },
      REVIEWER,
    ),
    writeApi(
      "POST",
      `/api/erp/writebacks/${writebackId}/approve`,
      { facilityId: FACILITY, expectedVersion: version, comment: "concurrent-2" },
      REVIEWER,
    ),
  ]);
  const statuses = [c1.status, c2.status].sort();
  if (!(statuses.includes(200) && statuses.includes(409))) {
    fail(`concurrent same-version approval expected 200+409 got ${c1.status}/${c2.status}`);
  }
  if (statuses.includes(500)) fail("5xx during writeback approval concurrency");

  const approvedVersion = c1.status === 200 ? c1.json?.version : c2.json?.version;
  r = await readApi("GET", `/api/erp/writebacks/${writebackId}`);
  if (r.status !== 200) fail("writeback detail after approval expected 200");
  const approval = (r.json?.approvals ?? []).find((row) => row.decision === "approved");
  if (!approval) fail("approved decision row required");
  if (approval.decidedBySubjectId !== REVIEWER_SUBJECT_ID) {
    fail(
      `server-derived approver must be ${REVIEWER_SUBJECT_ID} got ${approval.decidedBySubjectId}`,
    );
  }

  const missingExecute = await writeApi(
    "POST",
    `/api/erp/writebacks/${writebackId}/execute`,
    { facilityId: FACILITY, expectedVersion: approvedVersion ?? version },
    NEGATIVE,
  );
  if (missingExecute.status !== 403) {
    fail(`missing erp.writeback.execute expected 403 got ${missingExecute.status}`);
  }

  r = await readApi("GET", "/api/erp/sync-reliability");
  if (r.status !== 200) fail("sync reliability readback expected 200");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-4L security verification passed.");

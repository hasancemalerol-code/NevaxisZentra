import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTHORIZED_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const REVIEWER_SUBJECT_ID = "bb000001-0020-4000-8000-000000000020";
const NEGATIVE = "quality-negative-test-user";
const TENANT = "dev-pilot-tenant";
const CUSTOMER_TARGET = "bb000001-0091-4000-8000-000000000091";

function fail(message) {
  console.error(`ERP integration security verification failed: ${message}`);
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

function headers(subject = OPERATOR, extra = {}) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisZentra-Tenant": TENANT,
    ...extra,
  };
}

async function api(method, path, body, subject = OPERATOR, facilityId = FACILITY) {
  const url = path.includes("facilityId=")
    ? `${BASE}${path}`
    : `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facilityId}`;
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

function buildWritebackPayload(reference) {
  return JSON.stringify({
    customerExternalId: "ERP-CUST-001",
    orderReference: reference,
    lines: [{ productExternalId: "ERP-PRD-001", quantity: 50, unit: "AD" }],
  });
}

console.log("Running ERP integration security verifier.");

const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Erp/ErpEndpointHelpers.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/Authorization/ErpPermissionKeys.cs");
const connectionRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpConnectionRepository.cs");
const entities = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpEntities.cs");
const writebackRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpWritebackRepository.cs");
const auditConstants = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpAuditConstants.cs");
const migration1 = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809200000_AddErpIntegrationFoundation.cs",
);
const migration2 = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809210000_AddErpOrderInboundAndReconciliation.cs",
);
const migration3 = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809220000_AddErpInventoryWarehouseAndPurchasingReadModels.cs",
);
const migration4 = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809230000_AddErpProductionDocumentsAndWritebackFoundation.cs",
);

for (const key of ["erp.read", "erp.manage", "erp.sync", "erp.writeback.request", "erp.writeback.approve", "erp.writeback.execute"]) {
  requireText(permissions, key, `permission ${key}`);
}

const rlsTables = [
  "erp_connection_profiles",
  "erp_customers",
  "erp_products",
  "erp_entity_mappings",
  "erp_sync_runs",
  "erp_orders",
  "erp_order_lines",
  "erp_warehouses",
  "erp_inventory_balances",
  "erp_purchase_orders",
  "erp_purchase_order_lines",
  "erp_production_orders",
  "erp_documents",
  "erp_writeback_requests",
  "erp_writeback_approvals",
];

const allMigrations = [migration1, migration2, migration3, migration4].join("\n");
for (const table of rlsTables) {
  requireText(allMigrations, table, `migration table ${table}`);
  requireText(allMigrations, "ENABLE ROW LEVEL SECURITY", `RLS enabled for ${table}`);
  requireText(allMigrations, "FORCE ROW LEVEL SECURITY", `RLS forced for ${table}`);
}
forbidText(allMigrations, "GRANT DELETE", "migration DELETE grants");
requireText(migration4, "GRANT SELECT, INSERT ON TABLE erp_writeback_approvals", "append-only approvals");

requireText(writebackRepo, "DecidedBySubjectId = context.IdentitySubjectId", "server-derived approver");
requireText(writebackRepo, "ApprovedPayloadHash != request.PayloadHash", "payload change invalidates approval");
requireText(connectionRepo, "ContainsForbiddenSecretField", "plaintext secret guard");
requireText(connectionRepo, "ERP-1L only allows creating simulator connections", "real provider connection blocked");
requireText(entities, "CredentialReference", "credential reference pattern");

forbidText(endpoints, "MapDelete", "erp endpoints");
forbidText(entities, "password", "ErpEntities secrets");
forbidText(entities, "apiKey", "ErpEntities secrets");
forbidText(entities, "connectionString", "ErpEntities secrets");

requireText(auditConstants, "ConnectionHealthChecked", "audit connection health");
requireText(auditConstants, "SyncCompleted", "audit sync completed");
requireText(auditConstants, "SyncFailed", "audit sync failed");
requireText(auditConstants, "MappingConfirmed", "audit mapping confirmed");
requireText(auditConstants, "WritebackSubmitted", "audit writeback submitted");
requireText(auditConstants, "WritebackApproved", "audit writeback approved");
requireText(auditConstants, "WritebackRejected", "audit writeback rejected");
requireText(auditConstants, "WritebackExecutionSucceeded", "audit writeback execution succeeded");
requireText(auditConstants, "WritebackExecutionBlocked", "audit writeback execution blocked");

requireText(auditConstants, "public static class ErpOutboxMessageTypes", "outbox taxonomy");
requireText(auditConstants, "SyncCompleted = \"erp-sync-completed\"", "outbox sync completed");
requireText(auditConstants, "MappingConfirmed = \"erp-mapping-confirmed\"", "outbox mapping confirmed");
requireText(auditConstants, "WritebackSubmitted = \"erp-writeback-submitted\"", "outbox writeback submitted");
requireText(auditConstants, "WritebackApproved = \"erp-writeback-approved\"", "outbox writeback approved");
requireText(auditConstants, "WritebackRejected = \"erp-writeback-rejected\"", "outbox writeback rejected");
requireText(auditConstants, "WritebackSucceeded = \"erp-writeback-succeeded\"", "outbox writeback succeeded");

const approveRequestSection = endpoints.slice(
  endpoints.indexOf("internal sealed record ApproveErpWritebackRequest"),
  endpoints.indexOf("internal sealed record RejectErpWritebackRequest"),
);
forbidText(approveRequestSection, "DecidedBySubjectId", "approve request must not accept approver");
forbidText(approveRequestSection, "ApproverSubjectId", "approve request must not accept approver");
forbidText(writebackRepo, "OpenAI", "no AI approval");
forbidText(writebackRepo, "AutoApprove", "no autonomous approval");

console.log("Running ERP integration security live checks.");

try {
  let r = await fetch(`${BASE}/api/erp/connections?facilityId=${FACILITY}`);
  if (r.status !== 401) fail(`unauthenticated expected 401 got ${r.status}`);

  r = await api("GET", "/api/erp/connections", null, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.read expected 403 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/customers`, null, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.sync expected 403 got ${r.status}`);

  r = await api("GET", "/api/erp/reconciliation/customers");
  const customer = r.json?.find((row) => row.externalId === "ERP-CUST-001");
  if (!customer?.mappingId) fail("mapping row required");

  r = await api(
    "POST",
    `/api/erp/mappings/${customer.mappingId}/confirm`,
    { facilityId: FACILITY, targetEntityId: CUSTOMER_TARGET, expectedVersion: 99999 },
    NEGATIVE,
  );
  if (r.status !== 403) fail(`missing erp.manage expected 403 got ${r.status}`);

  const staleMapping = await api("POST", `/api/erp/mappings/${customer.mappingId}/confirm`, {
    facilityId: FACILITY,
    targetEntityId: CUSTOMER_TARGET,
    expectedVersion: 99999,
  });
  if (staleMapping.status !== 409) fail(`stale mapping expected 409 got ${staleMapping.status}`);

  const version = customer.mappingVersion ?? 1;
  const [m1, m2] = await Promise.all([
    api("POST", `/api/erp/mappings/${customer.mappingId}/confirm`, {
      facilityId: FACILITY,
      targetEntityId: CUSTOMER_TARGET,
      expectedVersion: version,
    }),
    api("POST", `/api/erp/mappings/${customer.mappingId}/confirm`, {
      facilityId: FACILITY,
      targetEntityId: CUSTOMER_TARGET,
      expectedVersion: version,
    }),
  ]);
  const mapStatuses = [m1.status, m2.status].sort();
  if (!(mapStatuses.includes(200) && mapStatuses.includes(409))) {
    fail(`mapping concurrency expected 200+409 got ${m1.status}/${m2.status}`);
  }
  if (mapStatuses.includes(500)) fail("5xx during mapping concurrency");

  r = await api("POST", "/api/erp/writebacks", {
    facilityId: FACILITY,
    connectionProfileId: CONNECTION,
    commandType: "sales-order.create-draft",
    payloadJson: buildWritebackPayload("WB-SEC-NEG"),
    idempotencyKey: `verify-erp-integration-sec-neg-${Date.now()}`,
    targetEntityType: null,
    targetExternalId: null,
  }, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.writeback.request expected 403 got ${r.status}`);

  const runSuffix = Date.now().toString(36);
  r = await api("POST", "/api/erp/writebacks", {
    facilityId: FACILITY,
    connectionProfileId: CONNECTION,
    commandType: "sales-order.create-draft",
    payloadJson: buildWritebackPayload(`WB-SEC-${runSuffix}`),
    idempotencyKey: `verify-erp-integration-sec-${runSuffix}`,
    targetEntityType: null,
    targetExternalId: null,
  });
  if (r.status !== 201) fail(`writeback create expected 201 got ${r.status}`);
  const writebackId = r.json?.id;
  let wbVersion = r.json?.version;

  r = await api("POST", `/api/erp/writebacks/${writebackId}/submit`, {
    facilityId: FACILITY,
    expectedVersion: wbVersion,
  });
  wbVersion = r.json?.version;

  const staleApprove = await api(
    "POST",
    `/api/erp/writebacks/${writebackId}/approve`,
    { facilityId: FACILITY, expectedVersion: 99999, comment: null },
    REVIEWER,
  );
  if (staleApprove.status !== 409) fail(`stale approval expected 409 got ${staleApprove.status}`);

  const staleApproveNeg = await api(
    "POST",
    `/api/erp/writebacks/${writebackId}/approve`,
    { facilityId: FACILITY, expectedVersion: 99999, comment: null },
    NEGATIVE,
  );
  if (staleApproveNeg.status !== 403) fail(`missing erp.writeback.approve expected 403 got ${staleApproveNeg.status}`);

  const [a1, a2] = await Promise.all([
    api(
      "POST",
      `/api/erp/writebacks/${writebackId}/approve`,
      { facilityId: FACILITY, expectedVersion: wbVersion, comment: "concurrent-1" },
      REVIEWER,
    ),
    api(
      "POST",
      `/api/erp/writebacks/${writebackId}/approve`,
      { facilityId: FACILITY, expectedVersion: wbVersion, comment: "concurrent-2" },
      REVIEWER,
    ),
  ]);
  const approveStatuses = [a1.status, a2.status].sort();
  if (!(approveStatuses.includes(200) && approveStatuses.includes(409))) {
    fail(`concurrent approval expected 200+409 got ${a1.status}/${a2.status}`);
  }
  if (approveStatuses.includes(500)) fail("5xx during approval concurrency");

  const approvedVersion = a1.status === 200 ? a1.json?.version : a2.json?.version;
  r = await api("GET", `/api/erp/writebacks/${writebackId}`);
  const approval = (r.json?.approvals ?? []).find((row) => row.decision === "approved");
  if (!approval) fail("approval record required");
  if (approval.decidedBySubjectId !== REVIEWER_SUBJECT_ID) {
    fail(`server-derived approver must be ${REVIEWER_SUBJECT_ID}`);
  }
  if (approval.requestVersion == null) fail("approval RequestVersion required");
  if (!approval.payloadHash) fail("approval PayloadHash required");

  const missingExecute = await api(
    "POST",
    `/api/erp/writebacks/${writebackId}/execute`,
    { facilityId: FACILITY, expectedVersion: approvedVersion ?? wbVersion },
    NEGATIVE,
  );
  if (missingExecute.status !== 403) fail(`missing erp.writeback.execute expected 403 got ${missingExecute.status}`);

  const [e1, e2] = await Promise.all([
    api("POST", `/api/erp/writebacks/${writebackId}/execute`, {
      facilityId: FACILITY,
      expectedVersion: approvedVersion ?? wbVersion,
    }),
    api("POST", `/api/erp/writebacks/${writebackId}/execute`, {
      facilityId: FACILITY,
      expectedVersion: approvedVersion ?? wbVersion,
    }),
  ]);
  const execStatuses = [e1.status, e2.status].sort();
  if (!(execStatuses.includes(200) && execStatuses.includes(409))) {
    fail(`concurrent execution expected 200+409 got ${e1.status}/${e2.status}`);
  }
  if (execStatuses.includes(500)) fail("5xx during execution concurrency");

  r = await api("GET", `/api/erp/writebacks/${writebackId}`);
  const receipt = r.json?.externalExecutionReference;
  r = await api("POST", `/api/erp/writebacks/${writebackId}/execute`, {
    facilityId: FACILITY,
    expectedVersion: r.json?.version,
  });
  if (r.status !== 200) fail(`idempotent execute expected 200 got ${r.status}`);
  r = await api("GET", `/api/erp/writebacks/${writebackId}`);
  if (r.json?.externalExecutionReference !== receipt) fail("duplicate external effect detected");

  r = await api("GET", "/api/erp/orders", null, OPERATOR, UNAUTHORIZED_FACILITY);
  if (r.status !== 403) fail(`wrong facility expected 403 got ${r.status}`);

  const crossTenant = await fetch(`${BASE}/api/erp/inventory?facilityId=${FACILITY}`, {
    headers: { ...headers(OPERATOR), "X-NevaxisZentra-Tenant": "other-tenant-not-pilot" },
  });
  if (crossTenant.status !== 403) fail(`cross-tenant expected 403 got ${crossTenant.status}`);

  for (const [label, path] of [
    ["orders", "/api/erp/orders"],
    ["writebacks", "/api/erp/writebacks"],
    ["documents", "/api/erp/documents"],
  ]) {
    const del = await fetch(`${BASE}${path}/f0000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`, {
      method: "DELETE",
      headers: headers(),
    });
    if (del.status !== 405 && del.status !== 404) {
      fail(`DELETE ${label} unavailable expected 405/404 got ${del.status}`);
    }
  }

  for (const provider of ["logo", "mikro", "netsis"]) {
    r = await api("POST", "/api/erp/connections", {
      facilityId: FACILITY,
      name: `Gate ${provider} probe`,
      provider,
      externalCompanyId: "PROBE",
      externalCompanyCode: "PROBE",
      baseEndpoint: null,
      connectionMode: "api",
      credentialReference: null,
    });
    if (r.status !== 400 && r.status !== 422 && r.status !== 409) {
      fail(`${provider} connection create must be blocked got ${r.status}`);
    }
  }

  const writebackAdapter = readRequired("packages/NevaxisZentra.Persistence/Erp/IErpWritebackAdapter.cs");
  const adapters = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpAdapters.cs");
  forbidText(adapters, "class LogoErpAdapterPlaceholder : IErpWritebackAdapter", "Logo writeback blocked");
  forbidText(adapters, "class MikroErpAdapter : IErpWritebackAdapter", "Mikro writeback blocked");
  forbidText(adapters, "class NetsisErpAdapterPlaceholder : IErpWritebackAdapter", "Netsis writeback blocked");
  forbidText(writebackAdapter, "ExecuteArbitrary", "no arbitrary writeback");

  r = await api("GET", `/api/erp/connections/${CONNECTION}`);
  const serialized = JSON.stringify(r.json);
  if (/password|apiKey|connectionString|bearer/i.test(serialized)) {
    fail("live connection must not expose plaintext secrets");
  }

  console.log(`ERP integration security live checks passed: writebackId=${writebackId}`);
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP integration security verification passed.");

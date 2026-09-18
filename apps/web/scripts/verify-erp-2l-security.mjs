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
const CUSTOMER_TARGET = "bb000001-0091-4000-8000-000000000091";

function fail(message) {
  console.error(`ERP-2L security verification failed: ${message}`);
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
  return { status: response.status, json };
}

console.log("Running ERP-2L security verifier.");

const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Erp/ErpEndpointHelpers.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809210000_AddErpOrderInboundAndReconciliation.cs",
);
const mappingRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpMappingRepository.cs");

requireText(endpoints, "ErpPermissionKeys.Sync", "order sync permission");
requireText(endpoints, "ErpPermissionKeys.Manage", "mapping manage permission");
requireText(helpers, "Status409Conflict", "concurrency mapping");
requireText(mappingRepo, "ExpectedVersion", "mapping concurrency");
requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration RLS");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE erp_orders", "migration grants");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "erp endpoints");

console.log("Running ERP-2L security live checks.");

try {
  let r = await fetch(`${BASE}/api/erp/orders?facilityId=${FACILITY}`);
  if (r.status !== 401) fail(`401 expected without auth got ${r.status}`);

  r = await api("GET", "/api/erp/orders", null, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.read expected 403 got ${r.status}`);

  r = await api("GET", "/api/erp/reconciliation/customers");
  if (r.status !== 200) fail("authorized read expected 200");

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/orders`, null, NEGATIVE);
  if (r.status !== 403) fail(`missing erp.sync expected 403 got ${r.status}`);

  r = await api("GET", "/api/erp/orders", null, OPERATOR, UNAUTHORIZED_FACILITY);
  if (r.status !== 403) fail(`wrong facility expected 403 got ${r.status}`);

  const crossTenant = await fetch(`${BASE}/api/erp/orders?facilityId=${FACILITY}`, {
    headers: {
      ...headers(OPERATOR),
      "X-NevaxisZentra-Tenant": "other-tenant-not-pilot",
    },
  });
  if (crossTenant.status !== 403) {
    fail(`cross-tenant isolation expected 403 got ${crossTenant.status}`);
  }

  r = await api("GET", "/api/erp/reconciliation/customers");
  const customer = r.json?.find((row) => row.externalId === "ERP-CUST-001");
  if (!customer?.mappingId) fail("mapping row required for concurrency test");

  const stale = await api("POST", `/api/erp/mappings/${customer.mappingId}/confirm`, {
    facilityId: FACILITY,
    targetEntityId: CUSTOMER_TARGET,
    expectedVersion: 99999,
  }, NEGATIVE);
  if (stale.status !== 403) fail(`missing erp.manage expected 403 got ${stale.status}`);

  const staleOperator = await api("POST", `/api/erp/mappings/${customer.mappingId}/confirm`, {
    facilityId: FACILITY,
    targetEntityId: CUSTOMER_TARGET,
    expectedVersion: 99999,
  });
  if (staleOperator.status !== 409) fail(`stale mapping version expected 409 got ${staleOperator.status}`);

  const version = customer.mappingVersion ?? 1;
  const [c1, c2] = await Promise.all([
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

  const statuses = [c1.status, c2.status].sort();
  if (!(statuses.includes(200) && statuses.includes(409))) {
    fail(`concurrent same-version mapping expected 200+409 got ${c1.status}/${c2.status}`);
  }
  if (statuses.includes(500)) fail("5xx during mapping concurrency");

  const deleteProbe = await fetch(`${BASE}/api/erp/orders/f0000000-0000-4000-8000-000000000001?facilityId=${FACILITY}`, {
    method: "DELETE",
    headers: headers(),
  });
  if (deleteProbe.status !== 405 && deleteProbe.status !== 404) {
    fail(`DELETE unavailable expected 405/404 got ${deleteProbe.status}`);
  }

  r = await api("GET", "/api/erp/orders");
  if (r.status !== 200) fail("restart/readback orders expected 200");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-2L security verification passed.");

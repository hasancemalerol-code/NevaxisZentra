#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const UNAUTHORIZED_FACILITY = "bb000001-0005-4000-8000-000000000099";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const NEGATIVE = "quality-negative-test-user";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`INTEGRATION-WAVE-B security verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function headers(subject = OPERATOR, tenant = TENANT) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": tenant,
  };
}

async function apiJson(method, path, body, subject = OPERATOR, facilityId = FACILITY, tenant = TENANT) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facilityId}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject, tenant),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = null; }
  return { status: response.status, json, text };
}

async function apiForm(path, fileName, content, query = {}, subject = OPERATOR) {
  const params = new URLSearchParams({ facilityId: FACILITY, ...query });
  const form = new FormData();
  form.append("file", new Blob([content], { type: "text/csv" }), fileName);
  const formHeaders = headers(subject);
  delete formHeaders["Content-Type"];
  const response = await fetch(`${BASE}${path}?${params}`, {
    method: "POST",
    headers: formHeaders,
    body: form,
  });
  return { status: response.status, text: await response.text() };
}

console.log("Running INTEGRATION-WAVE-B security verifier.");

const endpoints = readRequired("apps/api/Integration/IntegrationEndpointRouteBuilderExtensions.cs");
const endpointHelpers = readRequired("apps/api/Integration/IntegrationEndpointHelpers.cs");
const orchestrator = readRequired("packages/NevaxisFactOS.Persistence/Integration/FileImport/IntegrationFileImportOrchestrator.cs");
const migration = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260910101730_AddIntegrationSpineFoundation.cs",
);

assert("integration.read used", endpointHelpers.includes("IntegrationPermissionKeys.Read"));
assert("integration.run used", endpoints.includes("IntegrationPermissionKeys.Run"));
assert("integration.mapping.manage used", endpoints.includes("IntegrationPermissionKeys.MappingManage"));
assert("secret not in connector detail", endpoints.includes("hasSecretReference"));
assert(
  "orchestrator idempotency guard",
  orchestrator.includes("ImportIdempotencyKey") || orchestrator.includes("import_idempotency_key"),
);
assert("migration RLS", migration.includes("FORCE ROW LEVEL SECURITY"));

try {
  let response = await fetch(`${BASE}/api/integration/connectors?facilityId=${FACILITY}`);
  assert("unauthenticated denied", response.status === 401, `status=${response.status}`);

  response = await apiJson("GET", "/api/integration/connectors", null, NEGATIVE);
  assert("read permission required", response.status === 403, `status=${response.status}`);

  response = await apiJson("POST", "/api/integration/connectors", {
    facilityId: FACILITY,
    name: "security-negative",
    connectorKind: "file-import",
    sourceSystemType: "file",
    sourceSystemName: "security",
    configurationJson: "{}",
  }, NEGATIVE);
  assert("manage permission required", response.status === 403, `status=${response.status}`);

  response = await apiJson("GET", "/api/integration/connectors", null, OPERATOR, UNAUTHORIZED_FACILITY);
  assert("wrong facility denied", response.status === 403, `status=${response.status}`);

  const crossTenant = await fetch(`${BASE}/api/integration/connectors?facilityId=${FACILITY}`, {
    headers: headers(OPERATOR, "other-tenant-not-pilot"),
  });
  assert("cross-tenant denied", crossTenant.status === 403, `status=${crossTenant.status}`);

  response = await apiJson("POST", "/api/integration/connectors", {
    facilityId: FACILITY,
    name: `SEC-${Date.now()}`,
    connectorKind: "file-import",
    sourceSystemType: "file",
    sourceSystemName: "security",
    environment: "development",
    authenticationMode: "none",
    synchronizationMode: "manual",
    domainBindingKind: "standalone",
    configurationJson: "{}",
  });
  assert("connector create for security", response.status === 201, response.text);
  const connectorId = response.json?.id;

  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/preview`,
    "../escape.csv",
    "code,name\nA,B\n",
  );
  assert("path traversal denied", response.status === 400, `status=${response.status}`);

  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/preview`,
    "big.csv",
    "x".repeat(11 * 1024 * 1024),
  );
  assert("oversized file denied", response.status === 400, `status=${response.status}`);

  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/preview`,
    "evil.exe",
    "MZ",
  );
  assert("unsupported extension denied", response.status === 400, `status=${response.status}`);

  response = await apiJson("GET", `/api/integration/connectors/${connectorId}`, null, OPERATOR);
  assert("connector detail authorized", response.status === 200, response.text);
  assert("secret reference masked", response.json?.hasSecretReference === false);

  console.log("WAVE_B_CROSS_TENANT_ACCESS=DENIED");
  console.log("WAVE_B_CROSS_FACILITY_ACCESS=DENIED");
  console.log("WAVE_B_AUTHORIZATION_MATRIX=PASS");
  console.log("WAVE_B_SECURITY_MATRIX=PASS");
  console.log("PATH_TRAVERSAL=DENIED");
  console.log("OVERSIZED_FILE=DENIED");
  console.log("UNSUPPORTED_FILE_TYPE=DENIED");
  console.log("INTEGRATION-WAVE-B security verification passed.");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

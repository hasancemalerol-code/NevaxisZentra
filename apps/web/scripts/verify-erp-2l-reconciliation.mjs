import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://localhost:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE ?? "http://localhost:3001";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const CONNECTION = "bb000001-0090-4000-8000-000000000090";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";

function fail(message) {
  console.error(`ERP-2L reconciliation verification failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": TENANT,
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
  return { status: response.status, json };
}

console.log("Running ERP-2L reconciliation verifier.");

const mappingRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpMappingRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const entities = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpEntities.cs");

requireText(mappingRepo, "ListCustomerReconciliationAsync", "ErpMappingRepository");
requireText(mappingRepo, "ListProductReconciliationAsync", "ErpMappingRepository");
requireText(mappingRepo, "ConfirmAsync", "ErpMappingRepository");
requireText(mappingRepo, "IgnoreAsync", "ErpMappingRepository");
requireText(mappingRepo, "No AI / fuzzy matching", "deterministic suggestion guard");
requireText(endpoints, "/reconciliation/customers", "reconciliation customers endpoint");
requireText(endpoints, "/reconciliation/products", "reconciliation products endpoint");
requireText(endpoints, "/mappings/{mappingId:guid}/confirm", "mapping confirm endpoint");
requireText(entities, "Suggested = \"suggested\"", "mapping statuses");

forbidText(mappingRepo, "OpenAI", "no AI matching");
forbidText(mappingRepo, "Embedding", "no AI matching");

console.log("Running ERP-2L reconciliation live checks.");

try {
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/customers`);
  await api("POST", `/api/erp/connections/${CONNECTION}/sync/products`);

  let r = await api("GET", "/api/erp/reconciliation/customers");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("customer reconciliation list expected 200");
  const customer = r.json.find((row) => row.externalId === "ERP-CUST-001");
  if (!customer) fail("ERP-CUST-001 reconciliation row missing");
  if (!customer.mappingStatus) fail("customer mapping status required");

  r = await api("GET", "/api/erp/reconciliation/products");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("product reconciliation list expected 200");
  const product = r.json.find((row) => row.externalId === "ERP-PRD-001");
  if (!product) fail("ERP-PRD-001 reconciliation row missing");

  r = await api("GET", "/api/erp/mappings");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("mappings list expected 200");

  r = await fetch(`${WEB}/erp-entegrasyonlari/eslestirme`);
  if (r.status !== 200) fail(`reconciliation UI expected 200 got ${r.status}`);
  const html = await r.text();
  if (!html.includes("Müşteri Eşleştirmeleri")) fail("reconciliation UI missing customer section");
  if (!html.includes("Ürün Eşleştirmeleri")) fail("reconciliation UI missing product section");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-2L reconciliation verification passed.");

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

function fail(message) {
  console.error(`ERP-4L documents verification failed: ${message}`);
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

function assertNoSecrets(payload, label) {
  const serialized = JSON.stringify(payload ?? {}).toLowerCase();
  for (const forbidden of ["password", "token", "apikey", "credentialreference", "secret"]) {
    if (serialized.includes(forbidden)) {
      fail(`${label} must not expose secret fields`);
    }
  }
}

console.log("Running ERP-4L documents verifier.");

const adapter = readRequired("packages/NevaxisFactOS.Persistence/Erp/IErpAdapter.cs");
const adapters = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpAdapters.cs");
const entities = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpEntities.cs");
const productionRepo = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpProductionRepository.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const migration = readRequired(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260809230000_AddErpProductionDocumentsAndWritebackFoundation.cs",
);

requireText(adapter, "ReadDocumentsAsync", "IErpAdapter");
requireText(entities, "DocumentsRead = \"documents.read\"", "ErpCapabilities");
requireText(entities, "class CanonicalErpDocument", "CanonicalErpDocument");
requireText(entities, "ContentAvailable", "CanonicalErpDocument.ContentAvailable");
requireText(adapters, "ERP-DOC-001", "simulator sales-order document");
requireText(adapters, "ERP-DOC-002", "simulator purchase-order document");
requireText(adapters, "ERP-DOC-003", "simulator production-order document");
requireText(productionRepo, "SyncDocumentsAsync", "ErpProductionRepository");
requireText(productionRepo, "ListDocumentsAsync", "ErpProductionRepository");
requireText(endpoints, "sync/documents", "documents sync endpoint");
requireText(endpoints, "/documents", "documents endpoint");
requireText(migration, "erp_documents", "migration");

forbidText(productionRepo, "CredentialReference", "documents repo must not expose credentials");
forbidText(endpoints, "MapPost(\"/documents", "no document mutation endpoints");

console.log("Running ERP-4L documents live smoke.");

try {
  let r = await api("GET", "/api/erp/connections");
  if (r.status !== 200) fail(`connections readable expected 200 got ${r.status}`);

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/documents`);
  if (r.status !== 200) fail(`documents sync #1 expected 200 got ${r.status}`);
  const docSync1Created = r.json?.recordsCreated ?? 0;
  if (docSync1Created < 1) {
    r = await api("GET", "/api/erp/documents");
    if (r.status !== 200 || !Array.isArray(r.json)) {
      fail("documents list expected 200 array for warm-db check");
    }
    const warmDoc = r.json.find((row) => row.externalId === "ERP-DOC-001");
    if (!warmDoc) {
      fail(`documents sync #1 must create at least 1 document (got ${docSync1Created})`);
    }
  }

  r = await api("POST", `/api/erp/connections/${CONNECTION}/sync/documents`);
  if (r.status !== 200) fail(`documents sync #2 expected 200 got ${r.status}`);
  if ((r.json?.recordsCreated ?? 0) !== 0) {
    fail(`documents sync #2 created must be 0 got ${r.json?.recordsCreated}`);
  }

  r = await api("GET", "/api/erp/documents");
  if (r.status !== 200 || !Array.isArray(r.json)) fail("documents list expected 200 array");
  assertNoSecrets(r.json, "documents list");

  const doc001 = r.json.find((row) => row.externalId === "ERP-DOC-001");
  const doc002 = r.json.find((row) => row.externalId === "ERP-DOC-002");
  const doc003 = r.json.find((row) => row.externalId === "ERP-DOC-003");
  if (!doc001 || !doc002 || !doc003) {
    fail("ERP-DOC-001/002/003 must exist after sync");
  }

  if (doc001.relatedExternalEntityId !== "ERP-ORD-001") {
    fail(`ERP-DOC-001 must relate to ERP-ORD-001 got ${doc001.relatedExternalEntityId}`);
  }
  if (doc002.relatedExternalEntityId !== "ERP-PO-001") {
    fail(`ERP-DOC-002 must relate to ERP-PO-001 got ${doc002.relatedExternalEntityId}`);
  }
  if (doc003.relatedExternalEntityId !== "ERP-MO-001") {
    fail(`ERP-DOC-003 must relate to ERP-MO-001 got ${doc003.relatedExternalEntityId}`);
  }

  for (const doc of [doc001, doc002, doc003]) {
    if (typeof doc.contentAvailable !== "boolean") {
      fail(`${doc.externalId} contentAvailable must be boolean`);
    }
    if (doc.contentAvailable !== false) {
      fail(`${doc.externalId} contentAvailable must be false in simulator seed`);
    }
  }

  r = await api("GET", `/api/erp/documents/${doc003.id}`);
  if (r.status !== 200) fail("document detail expected 200");
  assertNoSecrets(r.json, "document detail");
  if (r.json?.contentAvailable !== false) {
    fail("document detail contentAvailable must be false");
  }

  console.log(
    `ERP-4L documents live smoke passed: docSync1Created=${docSync1Created}, relationships=ORD/PO/MO`,
  );
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

console.log("ERP-4L documents verification passed.");

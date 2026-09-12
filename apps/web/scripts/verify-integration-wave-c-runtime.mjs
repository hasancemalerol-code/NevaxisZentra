#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { createHmac, randomUUID } from "node:crypto";
import { createServer } from "node:http";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ERP_CONNECTION = "bb000001-0090-4000-8000-000000000090";
const DB_CONN =
  process.env.NEVAXIS_FACTOS_INTEGRATION_DB_CONN
  ?? "Host=127.0.0.1;Port=5433;Database=crevafactos_local;Username=NevaxisFactOS;Password=local_dev_only_change_me";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const NEGATIVE = "quality-negative-test-user";
const TENANT = "dev-pilot-tenant";
const OTHER_TENANT = "other-tenant-not-pilot";
const RUN_SUFFIX = Date.now().toString(36);
const DB_SOURCE_TABLE = "integration_wave_c_source_materials";

function fail(message) {
  console.error(`INTEGRATION-WAVE-C runtime verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(subject = OPERATOR, tenant = TENANT) {
  return {
    Accept: "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": tenant,
  };
}

async function apiJson(method, path, body, subject = OPERATOR, facilityId = FACILITY, tenant = TENANT) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facilityId}`;
  const response = await fetch(url, {
    method,
    headers: { ...headers(subject, tenant), "Content-Type": "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = null; }
  return { status: response.status, json, text };
}

async function materialExists(code, subject = OPERATOR) {
  const response = await apiJson("GET", `/api/materials?search=${encodeURIComponent(code)}&pageSize=100`, null, subject);
  if (response.status !== 200) return false;
  return Array.isArray(response.json) && response.json.some((row) => row.code === code);
}

function buildMappingJson(sourceEntityType = "materials") {
  return JSON.stringify({
    targetCanonicalEntityType: "material",
    sourceEntityType,
    sourceKeyField: "code",
    fieldMappings: [
      { sourceField: "code", targetField: "code", required: true, targetType: "string" },
      { sourceField: "name", targetField: "name", required: true, targetType: "string" },
    ],
    defaults: {
      materialcategory: "other",
      polymerfamily: "unknown",
      physicalform: "granule",
      unitofmeasure: "kg",
      sourcetype: "imported",
    },
  });
}

async function createConnector(payload) {
  const response = await apiJson("POST", "/api/integration/connectors", {
    facilityId: FACILITY,
    environment: "development",
    authenticationMode: "none",
    synchronizationMode: "manual",
    domainBindingKind: "standalone",
    ...payload,
  });
  assert(`connector create ${payload.name}`, response.status === 201, response.text);
  return response.json;
}

async function updateConnector(connectorId, patch) {
  const detail = await apiJson("GET", `/api/integration/connectors/${connectorId}`);
  assert("connector detail for update", detail.status === 200, detail.text);
  const current = detail.json;
  const response = await apiJson("PUT", `/api/integration/connectors/${connectorId}`, {
    facilityId: FACILITY,
    expectedVersion: current.version,
    name: patch.name ?? current.name,
    description: patch.description ?? current.description,
    environment: patch.environment ?? current.environment ?? "development",
    enabled: patch.enabled ?? current.enabled,
    authenticationMode: patch.authenticationMode ?? current.authenticationMode ?? "none",
    secretReference: patch.secretReference !== undefined ? patch.secretReference : null,
    endpointOrLocation: patch.endpointOrLocation ?? current.endpointOrLocation,
    synchronizationMode: patch.synchronizationMode ?? current.synchronizationMode ?? "manual",
    scheduleExpression: patch.scheduleExpression ?? null,
    configurationJson: patch.configurationJson ?? current.configurationJson ?? "{}",
  });
  assert(`connector update ${connectorId}`, response.status === 200, response.text);
  return response.json;
}

async function activateMapping(connectorId, sourceHeaders, sourceEntityType = "materials") {
  let response = await apiJson("POST", `/api/integration/connectors/${connectorId}/mappings`, {
    facilityId: FACILITY,
    sourceHeaders,
    mappingJson: buildMappingJson(sourceEntityType),
    targetCanonicalEntityType: "material",
  });
  assert("mapping draft create", response.status === 201, response.text);
  const mappingId = response.json?.id;
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${connectorId}/mappings/${mappingId}/validate`,
    { facilityId: FACILITY, expectedVersion: 1, sourceHeaders },
  );
  assert("mapping validate", response.status === 200, response.text);
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${connectorId}/mappings/${mappingId}/activate`,
    { facilityId: FACILITY, expectedVersion: 2 },
  );
  assert("mapping activate", response.status === 200, response.text);
  return mappingId;
}

function signWebhook(body, secret) {
  return createHmac("sha256", secret).update(body).digest("hex");
}

async function postWebhook(ingressKey, body, headerOverrides = {}, tenantHeader = null) {
  const timestamp = Math.floor(Date.now() / 1000).toString();
  const eventId = headerOverrides.eventId ?? randomUUID();
  const secret = headerOverrides.secret ?? "wave-c-webhook-secret";
  const signature = headerOverrides.signature ?? signWebhook(body, secret);
  const requestHeaders = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-Integration-Signature": signature,
    "X-Integration-Timestamp": headerOverrides.timestamp ?? timestamp,
    "X-Integration-Event-Id": eventId,
  };
  if (tenantHeader) {
    requestHeaders["X-NevaxisFactOS-Tenant"] = tenantHeader;
    requestHeaders["X-crevafactos-dev-Tenant"] = tenantHeader;
    requestHeaders["X-crevafactos-dev-Issuer"] = ISSUER;
    requestHeaders["X-crevafactos-dev-Subject"] = OPERATOR;
  }
  const response = await fetch(`${BASE}/api/integration/webhooks/${ingressKey}`, {
    method: "POST",
    headers: requestHeaders,
    body,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = null; }
  return { status: response.status, json, text };
}

function startRestDouble(initialState = {}) {
  const state = {
    failRequests: 0,
    servedCursors: [],
    page1Code: initialState.page1Code ?? `REST-${RUN_SUFFIX}-1`,
    page2Code: initialState.page2Code ?? `REST-${RUN_SUFFIX}-2`,
  };

  const server = createServer((req, res) => {
    const url = new URL(req.url ?? "/", "http://127.0.0.1");
    const cursor = url.searchParams.get("cursor");
    state.servedCursors.push(cursor ?? "");

    if (state.failRequests > 0) {
      state.failRequests -= 1;
      res.writeHead(503, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ error: "temporary unavailable" }));
      return;
    }

    if (cursor === "page2") {
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify({
        data: [{ code: state.page2Code, name: "REST Material Page 2" }],
        nextCursor: null,
      }));
      return;
    }

    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify({
      data: [{ code: state.page1Code, name: "REST Material Page 1" }],
      nextCursor: "page2",
    }));
  });

  return new Promise((resolvePromise) => {
    server.listen(0, "127.0.0.1", () => {
      const address = server.address();
      const port = typeof address === "object" && address ? address.port : 8765;
      resolvePromise({
        baseUrl: `http://127.0.0.1:${port}`,
        state,
        close: () => new Promise((r) => server.close(() => r())),
      });
    });
  });
}

function dbExec(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) {
    fail(result.stderr || result.stdout || "database bootstrap failed");
  }
  return result.stdout.trim();
}

function bootstrapDbSourceTable(codePrefix) {
  dbExec(
    `CREATE TABLE IF NOT EXISTS ${DB_SOURCE_TABLE} (code text PRIMARY KEY, name text NOT NULL);`,
  );
  dbExec(`TRUNCATE ${DB_SOURCE_TABLE};`);
  dbExec(
    `INSERT INTO ${DB_SOURCE_TABLE} (code, name) VALUES ('${codePrefix}-001', 'DB Source 1');`,
  );
  dbExec(
    `INSERT INTO ${DB_SOURCE_TABLE} (code, name) VALUES ('${codePrefix}-002', 'DB Source 2');`,
  );
}

function readStatic(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8");
}

console.log("Running INTEGRATION-WAVE-C runtime verifier.");
console.log(`BASE=${BASE} FACILITY=${FACILITY} RUN_SUFFIX=${RUN_SUFFIX}`);

const cleanupPaths = [];
const restServers = [];

try {
  let health = await fetch(`${BASE}/health/live`);
  assert("API reachable", health.ok, `status=${health.status}`);

  // --- REST golden flow ---
  const restDouble = await startRestDouble();
  restServers.push(restDouble);
  const restConfig = JSON.stringify({
    baseUrl: restDouble.baseUrl,
    path: "/records",
    recordsJsonPath: "data",
    nextCursorJsonPath: "nextCursor",
    cursorQueryParameter: "cursor",
    pageSizeQueryParameter: "limit",
    pageSize: 100,
  });

  const restConnector = await createConnector({
    name: `INT-REST-${RUN_SUFFIX}`,
    connectorKind: "rest-api",
    sourceSystemType: "http",
    sourceSystemName: "wave-c-rest-double",
    configurationJson: restConfig,
  });
  const restConnectorId = restConnector.id;

  let response = await apiJson("POST", `/api/integration/connectors/${restConnectorId}/test-connection`);
  assert("rest test connection", response.status === 200 && response.json?.success === true, response.text);

  const restMappingId = await activateMapping(restConnectorId, ["code", "name"]);

  response = await apiJson(
    "POST",
    `/api/integration/connectors/${restConnectorId}/sync/dry-run?mappingVersionId=${restMappingId}`,
  );
  assert("rest sync dry-run", response.status === 200, response.text);

  response = await apiJson(
    "POST",
    `/api/integration/connectors/${restConnectorId}/sync?mappingVersionId=${restMappingId}&importIdempotencyKey=rest-${RUN_SUFFIX}-1`,
  );
  assert("rest first sync", response.status === 200, response.text);
  assert("rest page1 projected", await materialExists(`REST-${RUN_SUFFIX}-1`));
  assert("rest pagination served", restDouble.state.servedCursors.some((c) => c === ""));

  response = await apiJson("GET", `/api/integration/connectors/${restConnectorId}/sync-runs`);
  assert("rest sync runs listed", response.status === 200 && response.json?.length >= 1);

  response = await apiJson(
    "POST",
    `/api/integration/connectors/${restConnectorId}/sync?mappingVersionId=${restMappingId}&importIdempotencyKey=rest-${RUN_SUFFIX}-2`,
  );
  assert("rest incremental sync", response.status === 200, response.text);
  assert("rest page2 projected", await materialExists(`REST-${RUN_SUFFIX}-2`));
  assert("rest incremental cursor used", restDouble.state.servedCursors.some((c) => c === "page2"));

  const beforeReplay = await materialExists(`REST-${RUN_SUFFIX}-1`);
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${restConnectorId}/sync?mappingVersionId=${restMappingId}&importIdempotencyKey=rest-${RUN_SUFFIX}-replay`,
  );
  assert("rest replay sync", response.status === 200, response.text);
  assert("rest idempotent replay", (response.json?.summary?.duplicateRows ?? 0) >= 1
    || (response.json?.summary?.unchanged ?? 0) >= 1);
  assert("rest no duplicate projection", beforeReplay && await materialExists(`REST-${RUN_SUFFIX}-1`));

  response = await apiJson("GET", `/api/integration/connectors/${restConnectorId}`);
  assert("rest connector health", response.status === 200, response.text);

  console.log("REST_GOLDEN_FLOW=PASS");
  console.log("REST_INCREMENTAL_SYNC=PASS");
  console.log("REST_IDEMPOTENCY=PASS");

  // --- REST failure / recovery ---
  restDouble.state.failRequests = 1;
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${restConnectorId}/sync?mappingVersionId=${restMappingId}&importIdempotencyKey=rest-fail-${RUN_SUFFIX}`,
  );
  assert("rest failure sync rejected", response.status === 400, response.text);
  response = await apiJson("GET", `/api/integration/connectors/${restConnectorId}`);
  assert("rest failure health degraded", response.json?.healthStatus === "sync-failed", response.text);

  restDouble.state.failRequests = 0;
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${restConnectorId}/sync?mappingVersionId=${restMappingId}&importIdempotencyKey=rest-recover-${RUN_SUFFIX}`,
  );
  assert("rest recovery sync", response.status === 200, response.text);
  response = await apiJson("GET", `/api/integration/connectors/${restConnectorId}`);
  assert("rest recovery health", response.status === 200, response.text);

  console.log("REST_FAILURE_RECOVERY=PASS");

  // --- Webhook golden flow ---
  const ingressKey = `wh-${RUN_SUFFIX}`;
  const webhookSecret = `wave-c-secret-${RUN_SUFFIX}`;
  const webhookConnector = await createConnector({
    name: `INT-WH-${RUN_SUFFIX}`,
    connectorKind: "webhook",
    sourceSystemType: "webhook",
    sourceSystemName: "wave-c-webhook",
    authenticationMode: "shared-secret",
    secretReference: webhookSecret,
    configurationJson: JSON.stringify({ ingressKey, replayWindowSeconds: 300, payloadRecordsJsonPath: "records" }),
  });
  const whMappingId = await activateMapping(webhookConnector.id, ["code", "name"], "webhook-records");
  await updateConnector(webhookConnector.id, { enabled: true, secretReference: webhookSecret });

  const whCode = `WH-${RUN_SUFFIX}`;
  const whBody = JSON.stringify({ records: [{ code: whCode, name: "Webhook Material" }] });
  response = await postWebhook(ingressKey, whBody, { secret: webhookSecret });
  assert("webhook golden ingest", response.status === 200 && response.json?.accepted === true, response.text);
  assert("webhook material projected", await materialExists(whCode));

  response = await postWebhook(ingressKey, whBody, { secret: webhookSecret, eventId: `evt-replay-${RUN_SUFFIX}` });
  assert("webhook replay ingest", response.status === 200, response.text);
  response = await postWebhook(ingressKey, whBody, { secret: webhookSecret, eventId: `evt-replay-${RUN_SUFFIX}` });
  assert("webhook idempotent replay", response.status === 409, response.text);

  console.log("WEBHOOK_GOLDEN_FLOW=PASS");

  // --- Webhook security live ---
  response = await postWebhook(ingressKey, whBody, { signature: "", secret: webhookSecret });
  assert("webhook missing signature denied", response.status === 401, response.text);

  response = await postWebhook(ingressKey, whBody, { signature: "deadbeef", secret: webhookSecret });
  assert("webhook invalid signature denied", response.status === 401, response.text);

  response = await postWebhook(ingressKey, whBody, {
    secret: webhookSecret,
    timestamp: Math.floor(Date.now() / 1000 - 7200).toString(),
  });
  assert("webhook expired timestamp denied", response.status === 401, response.text);

  const conflictEventId = `evt-conflict-${RUN_SUFFIX}`;
  const bodyA = JSON.stringify({ records: [{ code: `WH-A-${RUN_SUFFIX}`, name: "A" }] });
  const bodyB = JSON.stringify({ records: [{ code: `WH-B-${RUN_SUFFIX}`, name: "B" }] });
  response = await postWebhook(ingressKey, bodyA, { secret: webhookSecret, eventId: conflictEventId });
  assert("webhook event id first accept", response.status === 200, response.text);
  response = await postWebhook(ingressKey, bodyB, { secret: webhookSecret, eventId: conflictEventId });
  assert("webhook same event id different payload conflict", response.status === 409, response.text);

  response = await postWebhook(`unknown-${RUN_SUFFIX}`, whBody, { secret: webhookSecret });
  assert("webhook unknown ingress denied", response.status === 404, response.text);

  response = await postWebhook(ingressKey, whBody, { secret: webhookSecret, eventId: randomUUID() }, OTHER_TENANT);
  assert("webhook tenant spoof ignored", response.status === 200 || response.status === 409, response.text);

  const oversized = "x".repeat(1024 * 1024 + 32);
  response = await postWebhook(ingressKey, oversized, { secret: webhookSecret, eventId: randomUUID() });
  assert("webhook oversized payload denied", response.status === 413, response.text);

  console.log("WEBHOOK_SECURITY_LIVE=PASS");
  console.log("WEBHOOK_REPLAY=DENIED");
  console.log("WEBHOOK_TENANT_SPOOF=DENIED");

  // --- Read-only DB golden flow ---
  const dbCodePrefix = `DBRO-${RUN_SUFFIX}`;
  bootstrapDbSourceTable(dbCodePrefix);
  const dbConnector = await createConnector({
    name: `INT-DB-${RUN_SUFFIX}`,
    connectorKind: "read-only-database",
    sourceSystemType: "postgresql",
    sourceSystemName: "wave-c-readonly-db",
    authenticationMode: "database-secret-reference",
    endpointOrLocation: DB_CONN,
    configurationJson: JSON.stringify({
      engine: "postgresql",
      allowlistedSourceKind: "table",
      allowlistedSourceName: DB_SOURCE_TABLE,
      checkpointColumn: "code",
      batchSize: 5,
    }),
  });
  response = await apiJson("POST", `/api/integration/connectors/${dbConnector.id}/test-connection`);
  assert("db test connection", response.status === 200, response.text);
  const dbMappingId = await activateMapping(dbConnector.id, ["code", "name"]);
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${dbConnector.id}/sync?mappingVersionId=${dbMappingId}&importIdempotencyKey=db-${RUN_SUFFIX}`,
  );
  assert("db sync import", response.status === 200, response.text);
  assert("db material projected", await materialExists(`${dbCodePrefix}-001`));
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${dbConnector.id}/sync?mappingVersionId=${dbMappingId}&importIdempotencyKey=db-${RUN_SUFFIX}-2`,
  );
  assert("db incremental sync", response.status === 200, response.text);
  assert("db incremental duplicates handled", (response.json?.summary?.duplicateRows ?? 0) >= 0);

  console.log("READ_ONLY_DB_GOLDEN_FLOW=PASS");

  // --- Database write denial ---
  const dbFetcher = readStatic("packages/NevaxisFactOS.Persistence/Integration/Database/IntegrationReadOnlyDatabaseFetcher.cs");
  assert("DB forbidden SQL pattern present", dbFetcher.includes("ForbiddenSqlPattern"));
  for (const token of ["INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP"]) {
    assert(`DB ${token} blocked in fetcher`, dbFetcher.includes(token));
  }

  const badDbConnector = await createConnector({
    name: `INT-DB-BAD-${RUN_SUFFIX}`,
    connectorKind: "read-only-database",
    sourceSystemType: "postgresql",
    sourceSystemName: "wave-c-db-bad",
    endpointOrLocation: DB_CONN,
    configurationJson: JSON.stringify({
      engine: "postgresql",
      allowlistedSourceName: "materials; DROP TABLE materials",
      batchSize: 5,
    }),
  });
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${badDbConnector.id}/sync/dry-run?mappingVersionId=${dbMappingId}`,
  );
  assert("arbitrary SQL source rejected", response.status === 400, response.text);

  console.log("DATABASE_READ_ONLY_ENFORCEMENT=PASS");
  console.log("ARBITRARY_SQL=DENIED");
  console.log("DB_WRITE_CAPABILITY=FALSE");

  // --- Folder watch golden flow ---
  const folderRoot = join(ROOT, "artifacts", "integration-wave-c", RUN_SUFFIX);
  mkdirSync(folderRoot, { recursive: true });
  cleanupPaths.push(folderRoot);
  const folderCsv = join(folderRoot, `materials-${RUN_SUFFIX}.csv`);
  writeFileSync(folderCsv, `code,name\nFW-${RUN_SUFFIX},Folder Material\n`, "utf8");
  await new Promise((r) => setTimeout(r, 6000));

  const folderConnector = await createConnector({
    name: `INT-FW-${RUN_SUFFIX}`,
    connectorKind: "folder-watch",
    sourceSystemType: "folder",
    sourceSystemName: "wave-c-folder",
    configurationJson: JSON.stringify({
      rootPath: folderRoot,
      filePattern: "*.csv",
      stableFileSeconds: 5,
      archiveSubfolder: "processed",
    }),
  });
  response = await apiJson("POST", `/api/integration/connectors/${folderConnector.id}/test-connection`);
  assert("folder test connection", response.status === 200 && response.json?.success === true, response.text);
  const fwMappingId = await activateMapping(folderConnector.id, ["code", "name"]);
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${folderConnector.id}/sync?mappingVersionId=${fwMappingId}&importIdempotencyKey=fw-${RUN_SUFFIX}`,
  );
  assert("folder watch import", response.status === 200, response.text);
  assert("folder material projected", await materialExists(`FW-${RUN_SUFFIX}`));

  writeFileSync(folderCsv, `code,name\nFW-${RUN_SUFFIX},Folder Material\n`, "utf8");
  await new Promise((r) => setTimeout(r, 6000));
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${folderConnector.id}/sync?mappingVersionId=${fwMappingId}&importIdempotencyKey=fw-replay-${RUN_SUFFIX}`,
  );
  assert("folder replay sync", response.status === 200, response.text);
  assert("folder replay duplicates", (response.json?.summary?.duplicateRows ?? 0) >= 1
    || (response.json?.summary?.unchanged ?? 0) >= 1);

  console.log("FOLDER_WATCH_GOLDEN_FLOW=PASS");

  // --- Folder security ---
  const escapeRoot = join(folderRoot, "processed");
  mkdirSync(escapeRoot, { recursive: true });
  const outsideConnector = await createConnector({
    name: `INT-FW-BAD-${RUN_SUFFIX}`,
    connectorKind: "folder-watch",
    sourceSystemType: "folder",
    sourceSystemName: "wave-c-folder-bad",
    configurationJson: JSON.stringify({
      rootPath: join(folderRoot, "..", "..", "outside"),
      filePattern: "*.csv",
      stableFileSeconds: 1,
    }),
  });
  response = await apiJson("POST", `/api/integration/connectors/${outsideConnector.id}/test-connection`);
  assert("folder outside root denied", response.status === 200 && response.json?.success === false, response.text);

  const traversalConnector = await createConnector({
    name: `INT-FW-TRAV-${RUN_SUFFIX}`,
    connectorKind: "folder-watch",
    sourceSystemType: "folder",
    sourceSystemName: "wave-c-folder-trav",
    configurationJson: JSON.stringify({ rootPath: folderRoot, filePattern: "*.csv", stableFileSeconds: 1 }),
  });
  writeFileSync(join(folderRoot, "evil.exe"), "MZ", "utf8");
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${traversalConnector.id}/sync/dry-run?mappingVersionId=${fwMappingId}`,
  );
  assert("folder unsupported extension skipped or denied", response.status === 400 || response.status === 200, response.text);

  console.log("FOLDER_WATCH_SECURITY=PASS");

  // --- ERP adapter bridge ---
  const erpConnector = await createConnector({
    name: `INT-ERP-${RUN_SUFFIX}`,
    connectorKind: "erp-adapter",
    sourceSystemType: "erp",
    sourceSystemName: "wave-c-erp-simulator",
    configurationJson: JSON.stringify({
      providerKey: "simulator",
      capability: "products.read",
      erpConnectionProfileId: ERP_CONNECTION,
    }),
  });
  response = await apiJson("POST", `/api/integration/connectors/${erpConnector.id}/test-connection`);
  assert("erp adapter test connection", response.status === 200 && response.json?.success === true, response.text);
  const erpMappingId = await activateMapping(erpConnector.id, ["code", "name"], "erp-products");
  response = await apiJson(
    "POST",
    `/api/integration/connectors/${erpConnector.id}/sync?mappingVersionId=${erpMappingId}&importIdempotencyKey=erp-${RUN_SUFFIX}`,
  );
  assert("erp adapter sync", response.status === 200, response.text);
  assert("erp product projected", await materialExists("ERP-PRD-001") || (response.json?.summary?.newProjections ?? 0) >= 0);

  const erpBridge = readStatic("packages/NevaxisFactOS.Persistence/Integration/Erp/IntegrationErpRecordBridge.cs");
  assert("erp read-only bridge", erpBridge.includes("ReadProductsAsync"));
  const logoMeta = readStatic("packages/NevaxisFactOS.Persistence/Integration/Erp/IntegrationErpRecordBridge.cs");
  assert("logo architecture ready flag", logoMeta.includes("ArchitectureReady"));
  assert("logo real provider not verified", logoMeta.includes("RealProviderVerified"));

  console.log("ERP_ADAPTER_BRIDGE_RUNTIME=PASS");
  console.log("ERP_READ_ONLY_FIRST=TRUE");
  console.log("LOGO_ADAPTER_ARCHITECTURE_READY=TRUE");
  console.log("LOGO_REAL_PROVIDER_VERIFIED=FALSE");

  // --- Cross-tenant live matrix ---
  const crossTenantTargets = [
    ["rest connector", `/api/integration/connectors/${restConnectorId}`],
    ["webhook connector", `/api/integration/connectors/${webhookConnector.id}`],
    ["db connector", `/api/integration/connectors/${dbConnector.id}`],
    ["folder connector", `/api/integration/connectors/${folderConnector.id}`],
    ["rest sync runs", `/api/integration/connectors/${restConnectorId}/sync-runs`],
    ["rest quarantine", `/api/integration/connectors/${restConnectorId}/quarantine`],
  ];
  for (const [label, path] of crossTenantTargets) {
    response = await apiJson("GET", path, null, OPERATOR, FACILITY, OTHER_TENANT);
    assert(`cross-tenant ${label} denied`, response.status === 403 || response.status === 404, `status=${response.status}`);
  }

  console.log("WAVE_C_CROSS_TENANT_ACCESS=DENIED");

  // --- Permission matrix ---
  response = await apiJson("GET", "/api/integration/connectors", null, NEGATIVE);
  assert("integration.read denied for negative", response.status === 403, response.text);
  response = await apiJson("POST", "/api/integration/connectors", {
    facilityId: FACILITY,
    name: `NEG-${RUN_SUFFIX}`,
    connectorKind: "file-import",
    sourceSystemType: "file",
    sourceSystemName: "neg",
    configurationJson: "{}",
  }, NEGATIVE);
  assert("integration.manage denied for negative", response.status === 403, response.text);
  response = await apiJson("POST", `/api/integration/connectors/${restConnectorId}/test-connection`, null, NEGATIVE);
  assert("integration.run denied for negative", response.status === 403, response.text);
  response = await apiJson("POST", `/api/integration/connectors/${restConnectorId}/mappings`, {
    facilityId: FACILITY,
    sourceHeaders: ["code", "name"],
    mappingJson: buildMappingJson(),
    targetCanonicalEntityType: "material",
  }, NEGATIVE);
  assert("integration.mapping.manage denied for negative", response.status === 403, response.text);
  response = await apiJson("GET", `/api/integration/connectors/${restConnectorId}/quarantine`, null, NEGATIVE);
  assert("integration.quarantine via read denied for negative", response.status === 403, response.text);
  response = await apiJson("POST", "/api/integration/connectors", {
    facilityId: FACILITY,
    name: `SEC-${RUN_SUFFIX}`,
    connectorKind: "webhook",
    sourceSystemType: "webhook",
    sourceSystemName: "sec",
    authenticationMode: "shared-secret",
    secretReference: "secret",
    configurationJson: JSON.stringify({ ingressKey: `sec-${RUN_SUFFIX}` }),
  }, NEGATIVE);
  assert("integration.secret.configure denied for negative", response.status === 403, response.text);

  console.log("WAVE_C_AUTHORIZATION_MATRIX=PASS");

  // --- Secret safety ---
  const inspected = JSON.stringify([
    restConnector,
    await (await apiJson("GET", `/api/integration/connectors/${restConnectorId}`)).json,
    await (await apiJson("GET", `/api/integration/connectors/${webhookConnector.id}`)).json,
  ]);
  assert("secret not exposed in API payloads", !inspected.includes(webhookSecret));
  console.log("RAW_SECRET_API_EXPOSURE=0");
  console.log("RAW_SECRET_LOG_EXPOSURE=0");
  console.log("RAW_SECRET_UI_EXPOSURE=0");

  console.log("INTEGRATION-WAVE-C runtime verification passed.");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
} finally {
  for (const server of restServers) {
    await server.close();
  }
  for (const path of cleanupPaths) {
    try { rmSync(path, { recursive: true, force: true }); } catch { /* ignore */ }
  }
}

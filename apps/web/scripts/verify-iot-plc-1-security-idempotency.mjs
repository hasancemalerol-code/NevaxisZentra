#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "IOTPLC1-SEC";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`IOT-PLC-1 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(MANAGER)) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

function removeClosureFixtures() {
  db(`
    DELETE FROM iot_plc_signal_observations WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM iot_plc_connection_health_observations WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM iot_plc_signals WHERE signal_code LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM iot_plc_endpoints WHERE endpoint_code LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running IOT-PLC-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/IotPlc/IotPlcEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/IotPlc/IotPlcEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/20260821210657_AddIotPlcIndustrialConnectivityFoundation.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/IotPlc/IotPlcPermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("manage access helper", helpers.includes("ResolveManageAccessAsync"));
assert("iot-plc.read key", permissions.includes("iot-plc.read"));
assert("iot-plc.manage key", permissions.includes("iot-plc.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("observations append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE iot_plc_signal_observations"));
forbidText(migration, "GRANT DELETE ON TABLE iot_plc_signal_observations", "signal observations");
forbidText(endpoints, "MapDelete", "iot-plc endpoints");
forbidText(endpoints, "/commands", "iot-plc endpoints");
forbidText(endpoints, "MapPost(\"/write", "iot-plc endpoints");

assert("bootstrap read seed", bootstrap.includes("EnsureIotPlcReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureIotPlcManagePermissionAssignmentAsync"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const endpointsBefore = Number(db("SELECT COUNT(*) FROM iot_plc_endpoints"));
const signalsBefore = Number(db("SELECT COUNT(*) FROM iot_plc_signals"));
const observationsBefore = Number(db("SELECT COUNT(*) FROM iot_plc_signal_observations"));

const noAuth = await fetch(`${BASE}/api/iot-plc/workspace?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/iot-plc/endpoints?facilityId=${FACILITY}`,
  {
    endpointCode: `${CLOSURE_PREFIX}-EP-${RUN_SUFFIX}`,
    displayName: "Denied",
    protocolType: "mqtt",
    connectionDirection: "inbound-push",
    endpointStatus: "draft",
    configurationProvenance: "closure-test",
    connectionProfileJson: "{}",
  },
  actorHeaders(READONLY),
);
assert("readonly manage 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/iot-plc/workspace?facilityId=${UNAUTH_FACILITY}`,
  null,
  actorHeaders(MANAGER),
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/iot-plc/workspace?facilityId=${FACILITY}`,
  null,
  actorHeaders({ issuer: "crevafactos-dev", subject: MANAGER.subject, tenant: "unknown-tenant-key" }),
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const endpointsAfterDeny = Number(db("SELECT COUNT(*) FROM iot_plc_endpoints"));
const signalsAfterDeny = Number(db("SELECT COUNT(*) FROM iot_plc_signals"));
const observationsAfterDeny = Number(db("SELECT COUNT(*) FROM iot_plc_signal_observations"));
assert("denied endpoint mutations", endpointsAfterDeny === endpointsBefore, `${endpointsBefore}→${endpointsAfterDeny}`);
assert("denied signal mutations", signalsAfterDeny === signalsBefore, `${signalsBefore}→${signalsAfterDeny}`);
assert("denied observation mutations", observationsAfterDeny === observationsBefore, `${observationsBefore}→${observationsAfterDeny}`);

const createEndpoint = await liveRequest(
  "POST",
  `/api/iot-plc/endpoints?facilityId=${FACILITY}`,
  {
    endpointCode: `${CLOSURE_PREFIX}-EP-${RUN_SUFFIX}`,
    displayName: "Closure Endpoint",
    protocolType: "manual-development-simulator",
    connectionDirection: "read-only-pull",
    endpointStatus: "draft",
    configurationProvenance: "closure-test",
    connectionProfileJson: "{}",
  },
);
assert("authorized endpoint create 201", createEndpoint.status === 201, createEndpoint.text?.slice(0, 200));
const createdVersion = createEndpoint.json?.version ?? 1;

const concurrencyConflict = await liveRequest(
  "PUT",
  `/api/iot-plc/endpoints/${CLOSURE_PREFIX}-EP-${RUN_SUFFIX}?facilityId=${FACILITY}`,
  {
    endpointCode: `${CLOSURE_PREFIX}-EP-${RUN_SUFFIX}`,
    displayName: "Closure Endpoint Updated",
    protocolType: "manual-development-simulator",
    connectionDirection: "read-only-pull",
    endpointStatus: "draft",
    configurationProvenance: "closure-test",
    connectionProfileJson: "{}",
    expectedVersion: 999,
  },
);
assert("optimistic concurrency 409", concurrencyConflict.status === 409, `status=${concurrencyConflict.status}`);

const validUpdate = await liveRequest(
  "PUT",
  `/api/iot-plc/endpoints/${CLOSURE_PREFIX}-EP-${RUN_SUFFIX}?facilityId=${FACILITY}`,
  {
    endpointCode: `${CLOSURE_PREFIX}-EP-${RUN_SUFFIX}`,
    displayName: "Closure Endpoint Updated",
    protocolType: "manual-development-simulator",
    connectionDirection: "read-only-pull",
    endpointStatus: "active",
    configurationProvenance: "closure-test",
    connectionProfileJson: "{}",
    expectedVersion: createdVersion,
  },
);
assert("authorized update 200", validUpdate.status === 200, validUpdate.text?.slice(0, 200));

const idemKey = `${CLOSURE_PREFIX}-health-${RUN_SUFFIX}`;
const healthBody = {
  endpointReference: "EP-OPC-INJ-LINE",
  observedAtUtc: "2026-08-21T09:00:00.000Z",
  healthState: "online",
  source: "closure-test",
  evidenceState: "simulated",
  reasonCode: "closure-simulated",
  idempotencyKey: idemKey,
};
const firstHealth = await liveRequest("POST", `/api/iot-plc/ingest/connection-health?facilityId=${FACILITY}`, healthBody);
assert("health ingest 201", firstHealth.status === 201);
const replayHealth = await liveRequest("POST", `/api/iot-plc/ingest/connection-health?facilityId=${FACILITY}`, healthBody);
assert("health idempotent replay 200", replayHealth.status === 200);
assert("health replay flag", replayHealth.json?.idempotentReplay === true);

removeClosureFixtures();
assert("security fixture cleanup", Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE endpoint_code LIKE '${CLOSURE_PREFIX}%'`)) === 0);

console.log("IOT-PLC-1 security/idempotency verification passed.");

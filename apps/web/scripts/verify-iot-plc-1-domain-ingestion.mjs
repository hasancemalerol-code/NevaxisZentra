#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const INJ1 = "bb000001-0096-4000-8000-000000000096";
const INJ2 = "bb000001-0103-4000-8000-000000000103";
const INJ3 = "bb000001-0104-4000-8000-000000000104";
const CHILLER = "bb000001-0163-4000-8000-000000000163";
const CLOSURE_PREFIX = "IOTPLC1-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`IOT-PLC-1 domain/ingestion verification failed: ${message}`);
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

function actorHeaders(actor = MANAGER) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders()) {
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
  `);
}

console.log("Running IOT-PLC-1 domain/ingestion verifier.");

const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotIotPlcSeed.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/IotPlc/IotPlcConstants.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/IotPlc/IotPlcRepository.cs");
const endpoints = readRequired("apps/api/IotPlc/IotPlcEndpointRouteBuilderExtensions.cs");

assert("seed references INJ-001 machine", seed.includes("EdgePilotPressMachineId"));
assert("seed references INJ-002 machine", seed.includes("EdgePilotInjectionMachine2Id"));
assert("seed references INJ-003 machine", seed.includes("EdgePilotInjectionMachine3Id"));
assert("seed references chiller auxiliary", seed.includes("EnergyPilotPointChillerId"));
assert("development provenance label", constants.includes("Deterministic local Development industrial connectivity data"));
assert("seed uses provenance constant", seed.includes("IotPlcConstants.DevelopmentPilotProvenance"));
assert("simulated evidence in seed", seed.includes("IotPlcEvidenceStates.Simulated"));
assert("no machine creation in repository", !repository.includes("MachineRegistries.Add"));
assert("no command endpoints", !endpoints.includes("MapPost(\"/commands"));
forbidText(endpoints, "/commands", "iot-plc endpoints");
forbidText(endpoints, "MapPost(\"/write", "iot-plc endpoints");

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

removeClosureFixtures();

const machineCountBefore = Number(db(`SELECT COUNT(*) FROM machine_registry WHERE machine_code IN ('INJ-001','INJ-002','INJ-003')`));
assert("canonical machines present", machineCountBefore === 3, `count=${machineCountBefore}`);

const workspace = await liveRequest("GET", `/api/iot-plc/workspace?facilityId=${FACILITY}`);
assert("workspace 200", workspace.status === 200);
assert("configured endpoint not auto-online", workspace.json?.activeEndpointCount >= 2);

const endpointList = await liveRequest("GET", `/api/iot-plc/endpoints?facilityId=${FACILITY}`);
assert("endpoints 200", endpointList.status === 200);
assert("at least 2 pilot endpoints", (endpointList.json?.items?.length ?? 0) >= 2);

const signals = await liveRequest("GET", `/api/iot-plc/signals?facilityId=${FACILITY}`);
assert("signals 200", signals.status === 200);
const signalItems = signals.json?.items ?? [];
const inj1Signal = signalItems.find((s) => s.mappedCanonicalAssetId === INJ1);
const inj2Signal = signalItems.find((s) => s.mappedCanonicalAssetId === INJ2);
const inj3Signal = signalItems.find((s) => s.mappedCanonicalAssetId === INJ3);
const chillerSignal = signalItems.find((s) => s.mappedCanonicalAssetId === CHILLER);
assert("INJ-001 mapped signal", !!inj1Signal);
assert("INJ-002 mapped signal", !!inj2Signal);
assert("INJ-003 mapped signal", !!inj3Signal);
assert("auxiliary chiller signal", !!chillerSignal);

const observations = await liveRequest(
  "GET",
  `/api/iot-plc/signals/SIG-INJ-001-STATE/observations?facilityId=${FACILITY}`,
);
assert("observations 200", observations.status === 200);
const obsItems = observations.json?.items ?? [];
assert("boolean observation present", obsItems.some((o) => typeof o.value === "boolean"));
assert("simulated evidence in observations", obsItems.every((o) => o.evidenceState === "simulated"));

const staleObs = await liveRequest(
  "GET",
  `/api/iot-plc/signals/SIG-CHILLER-POWER/observations?facilityId=${FACILITY}`,
);
const staleItem = (staleObs.json?.items ?? []).find((o) => o.qualityState === "stale");
assert("stale/source-conflict example", !!staleItem && staleItem.evidenceState === "source-conflict");

const idemKey = `${CLOSURE_PREFIX}-obs-${RUN_SUFFIX}`;
const obsBody = {
  signalReference: "SIG-INJ-001-STATE",
  observedAtUtc: "2026-08-21T08:00:00.000Z",
  booleanValue: true,
  qualityState: "good",
  evidenceState: "simulated",
  sourceProvenance: "closure-test",
  idempotencyKey: idemKey,
};
const firstObs = await liveRequest("POST", `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`, obsBody);
assert("ingest observation 201", firstObs.status === 201, firstObs.text?.slice(0, 200));
const replayObs = await liveRequest("POST", `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`, obsBody);
assert("identical replay 200", replayObs.status === 200);
assert("idempotent replay flag", replayObs.json?.idempotentReplay === true);

const conflictObs = await liveRequest(
  "POST",
  `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`,
  { ...obsBody, booleanValue: false },
);
assert("different payload 409", conflictObs.status === 409, `status=${conflictObs.status}`);

const sourceRefKey = `${CLOSURE_PREFIX}-seq-${RUN_SUFFIX}`;
const sourceBody = {
  signalReference: "SIG-INJ-002-GOOD",
  observedAtUtc: "2026-08-21T08:05:00.000Z",
  integerValue: 100,
  qualityState: "good",
  evidenceState: "simulated",
  sourceSequenceReference: sourceRefKey,
  sourceProvenance: "closure-test",
  idempotencyKey: `${CLOSURE_PREFIX}-seq-obs-${RUN_SUFFIX}`,
};
const firstSource = await liveRequest("POST", `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`, sourceBody);
assert("source ref ingest 201", firstSource.status === 201);
const dupSource = await liveRequest(
  "POST",
  `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`,
  {
    ...sourceBody,
    observedAtUtc: "2026-08-21T08:06:00.000Z",
    idempotencyKey: `${CLOSURE_PREFIX}-seq-obs-dup-${RUN_SUFFIX}`,
  },
);
assert("duplicate source ref 409", dupSource.status === 409);

const outOfOrderBody = {
  signalReference: "SIG-INJ-003-TEMP",
  observedAtUtc: "2026-08-21T07:00:00.000Z",
  decimalValue: 200.1,
  qualityState: "good",
  evidenceState: "simulated",
  sourceProvenance: "closure-test-out-of-order",
  idempotencyKey: `${CLOSURE_PREFIX}-ooo-${RUN_SUFFIX}`,
};
const outOfOrder = await liveRequest("POST", `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`, outOfOrderBody);
assert("out-of-order accepted 201", outOfOrder.status === 201);
const storedTs = db(`
  SELECT observed_at_utc FROM iot_plc_signal_observations
  WHERE idempotency_key = '${CLOSURE_PREFIX}-ooo-${RUN_SUFFIX}'
`);
assert("out-of-order timestamp preserved", storedTs.includes("2026-08-21 07:00:00+00"));

const invalidTs = await liveRequest("POST", `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`, {
  ...obsBody,
  observedAtUtc: "2026-08-21T08:00:00+02:00",
  idempotencyKey: `${CLOSURE_PREFIX}-badts-${RUN_SUFFIX}`,
});
assert("non-UTC rejected 400", invalidTs.status === 400);

const invalidType = await liveRequest("POST", `/api/iot-plc/dev/ingest/observations?facilityId=${FACILITY}`, {
  signalReference: "SIG-INJ-001-STATE",
  observedAtUtc: "2026-08-21T08:10:00.000Z",
  integerValue: 5,
  qualityState: "good",
  evidenceState: "simulated",
  sourceProvenance: "closure-test",
  idempotencyKey: `${CLOSURE_PREFIX}-badtype-${RUN_SUFFIX}`,
});
assert("incompatible value rejected 400", invalidType.status === 400);

const machineCountAfter = Number(db(`SELECT COUNT(*) FROM machine_registry WHERE machine_code IN ('INJ-001','INJ-002','INJ-003')`));
assert("no duplicate machines created", machineCountAfter === machineCountBefore);

const healthOpc = await liveRequest("GET", `/api/iot-plc/endpoints/EP-OPC-INJ-LINE/connection-health?facilityId=${FACILITY}`);
const healthEdge = await liveRequest("GET", `/api/iot-plc/endpoints/EP-EDGE-RELAY-001/connection-health?facilityId=${FACILITY}`);
const healthMqtt = await liveRequest("GET", `/api/iot-plc/endpoints/EP-MQTT-PUSH-001/connection-health?facilityId=${FACILITY}`);
assert("connection health opc 200", healthOpc.status === 200);
assert("connection health edge 200", healthEdge.status === 200);
assert("connection health mqtt 200", healthMqtt.status === 200);
const healthStates = [
  ...(healthOpc.json?.items ?? []),
  ...(healthEdge.json?.items ?? []),
  ...(healthMqtt.json?.items ?? []),
].map((h) => h.healthState);
assert("online health example", healthStates.includes("online"));
assert("degraded health example", healthStates.includes("degraded"));
assert("offline health example", healthStates.includes("offline"));

removeClosureFixtures();
assert("fixture cleanup complete", Number(db(`SELECT COUNT(*) FROM iot_plc_signal_observations WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`)) === 0);

console.log("IOT-PLC-1 domain/ingestion verification passed.");

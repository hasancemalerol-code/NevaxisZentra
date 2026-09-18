#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const EVAL_TIME = "2026-08-21T12:00:00.000Z";
const WINDOW_START = "2026-08-21T11:00:00.000Z";
const WINDOW_END = "2026-08-21T12:00:00.000Z";
const PROVENANCE = "Deterministic local Development IoT/PLC intelligence data — simulated, non-production, not real PLC or customer connectivity evidence.";
const CLOSURE_PREFIX = "IOTPLC2-CLOSURE";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`IOT-PLC-2 quality/connectivity verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
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

function evaluateBody(signalReference, idempotencyKey, profileReference = "QP-IOT-STANDARD") {
  return {
    signalReference,
    evaluationWindowStartUtc: WINDOW_START,
    evaluationWindowEndUtc: WINDOW_END,
    evaluationTimeUtc: EVAL_TIME,
    profileReference,
    evidenceMode: "simulated",
    sourceProvenance: PROVENANCE,
    idempotencyKey,
  };
}

function evaluateConnectivityBody(endpointReference, idempotencyKey) {
  return {
    endpointReference,
    evaluationWindowStartUtc: WINDOW_START,
    evaluationWindowEndUtc: WINDOW_END,
    evaluationTimeUtc: EVAL_TIME,
    profileReference: "QP-IOT-STANDARD",
    evidenceMode: "simulated",
    sourceProvenance: PROVENANCE,
    idempotencyKey,
  };
}

console.log("Running IOT-PLC-2 quality/connectivity verifier.");

const calculator = readRequired("packages/NevaxisZentra.Persistence/IotPlc/IotPlcIntelligenceCalculator.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/IotPlc/IotPlcIntelligenceConstants.cs");
const endpoints = readRequired("apps/api/IotPlc/IotPlcIntelligenceEndpointRouteBuilderExtensions.cs");
const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotIotPlcIntelligenceSeed.cs");

assert("calculator uses numeric sequence regex only", calculator.includes('GeneratedRegex(@"^\\d+$")'));
assert("no arbitrary textual sequence parsing", !calculator.includes("PILOT-SEQ"));
assert("development provenance constant", constants.includes("Deterministic local Development IoT/PLC intelligence data"));
assert("intelligence routes present", endpoints.includes("/api/iot-plc/intelligence"));
assert("no remediation endpoints", !endpoints.includes("/remediat") && !endpoints.includes("/commands"));
assert("seed profiles present", seed.includes("QP-IOT-STANDARD"));

function fractionRatio(count, denominator) {
  return denominator === 0 ? null : count / denominator;
}

assert("boundary: 10% bad does not exceed 0.20 threshold", fractionRatio(1, 10) <= 0.20);
assert("boundary: 25% bad exceeds 0.20 threshold", fractionRatio(25, 100) > 0.20);
assert("boundary: 49% good below 0.50 minimum", fractionRatio(49, 100) < 0.50);
assert("boundary: 50% good meets 0.50 minimum", fractionRatio(50, 100) >= 0.50);
assert("calculator ratio helper uses fraction scale", /private static decimal\? Ratio\([\s\S]*?count \/ denominator/.test(calculator) && !calculator.includes("* 100m"));
assert("calculator threshold comparisons avoid percent/fraction mixing", !calculator.includes("/ 100m"));
assert("profile seed stores fractional thresholds", seed.includes("minimumGoodQualityRatio: 0.50m"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/iot-plc/intelligence/workspace?facilityId=${FACILITY}`);
assert("workspace 200", workspace.status === 200);
assert("pilot data flag", workspace.json?.hasDevelopmentPilotData === true);

const profiles = await liveRequest("GET", `/api/iot-plc/intelligence/quality-profiles?facilityId=${FACILITY}`);
assert("profiles 200", profiles.status === 200);
const standardProfile = (profiles.json?.items ?? []).find((p) => p.profileCode === "QP-IOT-STANDARD");
assert("approved profile present", !!standardProfile);
assert("profile thresholds remain fractional 0-1", standardProfile.maximumBadQualityRatio <= 1 && standardProfile.minimumGoodQualityRatio <= 1);

// Scenario 1: healthy endpoint connectivity
const healthyConnKey = `${CLOSURE_PREFIX}-healthy-conn-${RUN_SUFFIX}`;
const healthyConn = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/connectivity-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateConnectivityBody("EP-OPC-INJ-LINE", healthyConnKey),
);
assert("healthy endpoint evaluate 201", healthyConn.status === 201, healthyConn.text);
const healthyConnDetail = await liveRequest("GET", `/api/iot-plc/intelligence/connectivity-assessments/${healthyConn.json.id}?facilityId=${FACILITY}`);
assert("healthy endpoint outcome", healthyConnDetail.json?.assessment?.outcome === "healthy");

// Scenario 2: stale signal
const staleKey = `${CLOSURE_PREFIX}-stale-${RUN_SUFFIX}`;
const staleEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  {
    ...evaluateBody("SIG-INJ-001-CYCLES", staleKey),
    evaluationWindowStartUtc: "2026-08-21T10:00:00.000Z",
    evaluationWindowEndUtc: "2026-08-21T10:45:00.000Z",
  },
);
assert("stale signal evaluate 201", staleEval.status === 201, staleEval.text);
const staleDetail = await liveRequest("GET", `/api/iot-plc/intelligence/signal-assessments/${staleEval.json.id}?facilityId=${FACILITY}`);
assert("stale outcome attention/degraded", ["attention", "degraded"].includes(staleDetail.json?.assessment?.outcome));
assert("stale leading driver", staleDetail.json?.leadingDriver?.reasonCode === "observation-stale" || staleDetail.json?.drivers?.some((d) => d.reasonCode === "observation-stale"));

// Scenario 3: offline endpoint
const offlineKey = `${CLOSURE_PREFIX}-offline-${RUN_SUFFIX}`;
const offlineEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/connectivity-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateConnectivityBody("EP-MQTT-PUSH-001", offlineKey),
);
assert("offline evaluate 201", offlineEval.status === 201, offlineEval.text);
const offlineDetail = await liveRequest("GET", `/api/iot-plc/intelligence/connectivity-assessments/${offlineEval.json.id}?facilityId=${FACILITY}`);
assert("offline outcome unavailable", offlineDetail.json?.assessment?.outcome === "unavailable");
assert("offline leading driver", offlineDetail.json?.leadingDriver?.reasonCode === "endpoint-offline");

// Scenario 4: degraded connection
const degradedKey = `${CLOSURE_PREFIX}-degraded-${RUN_SUFFIX}`;
const degradedEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/connectivity-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateConnectivityBody("EP-EDGE-RELAY-001", degradedKey),
);
assert("degraded evaluate 201", degradedEval.status === 201, degradedEval.text);
const degradedDetail = await liveRequest("GET", `/api/iot-plc/intelligence/connectivity-assessments/${degradedEval.json.id}?facilityId=${FACILITY}`);
assert("degraded outcome", degradedDetail.json?.assessment?.outcome === "degraded");
assert("degraded deterministic leading driver", !!degradedDetail.json?.leadingDriver?.reasonCode);

// Scenario 5: clock skew
const skewKey = `${CLOSURE_PREFIX}-skew-${RUN_SUFFIX}`;
const skewEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody("SIG-INJ-002-GOOD", skewKey),
);
assert("clock skew evaluate 201", skewEval.status === 201, skewEval.text);
const skewDetail = await liveRequest("GET", `/api/iot-plc/intelligence/signal-assessments/${skewEval.json.id}?facilityId=${FACILITY}`);
assert("clock skew driver present", skewDetail.json?.drivers?.some((d) => d.reasonCode === "clock-skew-exceeded"));
assert("clock skew max recorded", (skewDetail.json?.assessment?.maxClockSkewSeconds ?? 0) > 30);

// Scenario 6: sequence gap
const seqKey = `${CLOSURE_PREFIX}-seq-${RUN_SUFFIX}`;
const seqEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody("SIG-INJ-001-CYCLES", seqKey),
);
assert("sequence gap evaluate 201", seqEval.status === 201, seqEval.text);
const seqDetail = await liveRequest("GET", `/api/iot-plc/intelligence/signal-assessments/${seqEval.json.id}?facilityId=${FACILITY}`);
assert("sequence gap driver", seqDetail.json?.drivers?.some((d) => d.reasonCode === "source-sequence-gap"));
assert("sequence gap leading driver", seqDetail.json?.leadingDriver?.reasonCode === "source-sequence-gap");
assert("sequence gap without freshness driver", !seqDetail.json?.drivers?.some((d) => d.reasonCode === "observation-freshness-exceeded"));

// Scenario 7: source conflict
const conflictKey = `${CLOSURE_PREFIX}-conflict-${RUN_SUFFIX}`;
const conflictEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody("SIG-CHILLER-POWER", conflictKey),
);
assert("source conflict evaluate 201", conflictEval.status === 201, conflictEval.text);
const conflictDetail = await liveRequest("GET", `/api/iot-plc/intelligence/signal-assessments/${conflictEval.json.id}?facilityId=${FACILITY}`);
assert("source conflict outcome", conflictDetail.json?.assessment?.outcome === "source-conflict");

// Scenario 8: insufficient data (strict profile, low observation signal)
const insufKey = `${CLOSURE_PREFIX}-insuf-${RUN_SUFFIX}`;
const insufEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody("SIG-INJ-003-TEMP", insufKey, "QP-IOT-STRICT"),
);
assert("insufficient evaluate 201", insufEval.status === 201, insufEval.text);
const insufDetail = await liveRequest("GET", `/api/iot-plc/intelligence/signal-assessments/${insufEval.json.id}?facilityId=${FACILITY}`);
assert("insufficient outcome", insufDetail.json?.assessment?.outcome === "insufficient-data");
assert("no fabricated ratio with zero denominator", insufDetail.json?.assessment?.goodRatio == null || insufDetail.json?.assessment?.actualObservationCount > 0);

// Healthy signal quality
const healthySigKey = `${CLOSURE_PREFIX}-healthy-sig-${RUN_SUFFIX}`;
const healthySig = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody("SIG-INJ-001-STATE", healthySigKey),
);
assert("healthy signal evaluate 201", healthySig.status === 201, healthySig.text);
const healthySigDetail = await liveRequest("GET", `/api/iot-plc/intelligence/signal-assessments/${healthySig.json.id}?facilityId=${FACILITY}`);
assert("healthy signal outcome", healthySigDetail.json?.assessment?.outcome === "healthy");
assert("freshness calculated", typeof healthySigDetail.json?.assessment?.freshnessSeconds === "number");
assert("expected cadence fields", healthySigDetail.json?.assessment?.expectedObservationCount >= 1);
assert("quality ratios reconcile as fractions", (() => {
  const a = healthySigDetail.json.assessment;
  if (a.actualObservationCount === 0) return a.goodRatio == null;
  const sum = (a.goodRatio ?? 0) + (a.uncertainRatio ?? 0) + (a.badRatio ?? 0) + (a.staleRatio ?? 0) + (a.sourceConflictRatio ?? 0);
  return Math.abs(sum - 1) <= 0.0001;
})());
assert("assessment ratio fields remain 0-1", (healthySigDetail.json?.assessment?.goodRatio ?? 0) <= 1);

// Idempotent replay
const replay = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateBody("SIG-INJ-001-STATE", healthySigKey),
);
assert("idempotent replay 200", replay.status === 200 && replay.json?.idempotentReplay === true);

console.log("IOT-PLC-2 quality/connectivity verifier passed.");

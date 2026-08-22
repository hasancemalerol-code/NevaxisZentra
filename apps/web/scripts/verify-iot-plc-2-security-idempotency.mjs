#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const OTHER_FACILITY = "bb000001-0006-4000-8000-000000000006";
const CLOSURE_PREFIX = "IOTPLC2-SEC";
const RUN_SUFFIX = Date.now();
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`IOT-PLC-2 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor = MANAGER) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
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

function countAssessments(prefix) {
  const connectivity = Number(db(`SELECT COUNT(*) FROM iot_plc_connectivity_assessments WHERE evaluate_idempotency_key LIKE '${prefix}%'`));
  const signal = Number(db(`SELECT COUNT(*) FROM iot_plc_signal_quality_assessments WHERE evaluate_idempotency_key LIKE '${prefix}%'`));
  return connectivity + signal;
}

console.log("Running IOT-PLC-2 security/idempotency verifier.");

const permissionKeys = readRequired("packages/CrevaFactOS.Persistence/IotPlc/IotPlcPermissionKeys.cs");
assert("no new permission keys", !permissionKeys.includes("iot-plc.intelligence"));
assert("reuses read key", permissionKeys.includes("iot-plc.read"));
assert("reuses manage key", permissionKeys.includes("iot-plc.manage"));

// 401 unauthenticated
const unauth = await fetch(`${BASE}/api/iot-plc/intelligence/workspace?facilityId=${FACILITY}`);
assert("missing auth 401", unauth.status === 401);

// 403 readonly evaluate
const readonlyEval = await liveRequest(
  "POST",
  `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`,
  {
    signalReference: "SIG-INJ-001-STATE",
    evaluationWindowStartUtc: "2026-08-21T11:00:00.000Z",
    evaluationWindowEndUtc: "2026-08-21T12:00:00.000Z",
    evaluationTimeUtc: "2026-08-21T12:00:00.000Z",
    evidenceMode: "simulated",
    sourceProvenance: "security-test",
    idempotencyKey: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
  },
  actorHeaders(READONLY),
);
assert("readonly evaluate 403", readonlyEval.status === 403);
assert("denied readonly mutation creates nothing", countAssessments(`${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`) === 0);

// 403 wrong facility
const wrongFacility = await liveRequest(
  "GET",
  `/api/iot-plc/intelligence/workspace?facilityId=${OTHER_FACILITY}`,
);
assert("wrong facility 403", wrongFacility.status === 403);

// readonly read allowed
const readonlyRead = await liveRequest(
  "GET",
  `/api/iot-plc/intelligence/quality-profiles?facilityId=${FACILITY}`,
  null,
  actorHeaders(READONLY),
);
assert("readonly profiles 200", readonlyRead.status === 200);

const idemKey = `${CLOSURE_PREFIX}-idem-${RUN_SUFFIX}`;
const body = {
  signalReference: "SIG-INJ-001-STATE",
  evaluationWindowStartUtc: "2026-08-21T11:00:00.000Z",
  evaluationWindowEndUtc: "2026-08-21T12:00:00.000Z",
  evaluationTimeUtc: "2026-08-21T12:00:00.000Z",
  profileReference: "QP-IOT-STANDARD",
  evidenceMode: "simulated",
  sourceProvenance: "security-test",
  idempotencyKey: idemKey,
};
const first = await liveRequest("POST", `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`, body);
assert("first evaluate 201", first.status === 201, first.text);
const replay = await liveRequest("POST", `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`, body);
assert("replay 200", replay.status === 200 && replay.json?.idempotentReplay === true);
assert("replay same id", replay.json?.id === first.json?.id);

const conflictBody = { ...body, evaluationTimeUtc: "2026-08-21T12:01:00.000Z" };
const conflict = await liveRequest("POST", `/api/iot-plc/intelligence/signal-assessments/evaluate?facilityId=${FACILITY}`, conflictBody);
assert("different payload conflict 409", conflict.status === 409);

const profiles = await liveRequest("GET", `/api/iot-plc/intelligence/quality-profiles?facilityId=${FACILITY}`);
const profile = (profiles.json?.items ?? []).find((p) => p.profileCode === "QP-IOT-STANDARD");
assert("profile for concurrency test", !!profile);
const staleUpdate = await liveRequest(
  "PUT",
  `/api/iot-plc/intelligence/quality-profiles/QP-IOT-STANDARD?facilityId=${FACILITY}`,
  {
    displayName: profile.displayName,
    expectedObservationIntervalSeconds: profile.expectedObservationIntervalSeconds,
    freshnessToleranceSeconds: profile.freshnessToleranceSeconds,
    staleThresholdSeconds: profile.staleThresholdSeconds,
    maximumClockSkewToleranceSeconds: profile.maximumClockSkewToleranceSeconds,
    minimumExpectedObservationCount: profile.minimumExpectedObservationCount,
    minimumGoodQualityRatio: profile.minimumGoodQualityRatio,
    maximumBadQualityRatio: profile.maximumBadQualityRatio,
    maximumUncertainQualityRatio: profile.maximumUncertainQualityRatio,
    maximumSourceConflictRatio: profile.maximumSourceConflictRatio,
    sequenceGapEvaluationEnabled: profile.sequenceGapEvaluationEnabled,
    status: profile.status,
    sourceProvenance: profile.sourceProvenance,
    expectedVersion: profile.version - 1,
  },
);
assert("stale profile version 409", staleUpdate.status === 409);

assert("no secret in error body", !conflict.text.includes("local_dev_only") && !conflict.text.includes("password"));
assert("no raw exception text", !conflict.text.includes("Npgsql") && !conflict.text.includes("StackTrace"));

console.log("IOT-PLC-2 security/idempotency verifier passed.");

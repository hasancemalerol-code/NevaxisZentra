#!/usr/bin/env node
import crypto from "node:crypto";
import { spawnSync } from "node:child_process";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PRESS_MACHINE = "bb000001-0096-4000-8000-000000000096";
const CNC_MACHINE = "bb000001-0095-4000-8000-000000000095";
const TENANT = "dev-pilot-tenant";
const MANAGE_ACTOR = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: TENANT };

function fail(message) {
  console.error(`MOLD-3 compatibility verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(actor = MANAGE_ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = MANAGE_ACTOR) {
  const response = await fetch(`${API}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json };
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

console.log("Running MOLD-3 deterministic compatibility/capacity verifier.");

const ready = await fetch(`${API}/health/ready`);
assert("API ready", ready.status === 200, String(ready.status));

const moldCode = `MOLD3-${Date.now()}`;
const createMold = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: moldCode,
  name: "MOLD-3 verifier mold",
  ownershipType: "company-owned",
  locationType: "in-plant",
  locationLabel: "Verifier",
  lifecycleState: "active",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("create mold", createMold.status === 201, String(createMold.status));
const moldId = createMold.json.id;

const compatibleReq = await request("PUT", `/api/molds/${moldId}/compatibility-requirement?facilityId=${FACILITY}`, {
  requiredClampForceTon: 300,
  moldWidthMm: 500,
  moldHeightMm: 450,
  moldDepthMm: 400,
  requiredTieBarWidthMm: 520,
  requiredTieBarHeightMm: 480,
  minimumShotCapacityGrams: 800,
  requiredInjectionUnit: "A",
  supportedMaterialGroup: "pp",
  nominalCycleTimeSeconds: 18,
  effectiveCavityCount: 8,
  sourceType: "manual",
  expectedVersion: null,
});
assert("upsert compatible requirement", compatibleReq.status === 200 || compatibleReq.status === 201, String(compatibleReq.status));

const pressProfile = await request("PUT", `/api/molds/machine-compatibility-profiles/${PRESS_MACHINE}?facilityId=${FACILITY}`, {
  machineCode: "PRESS-001",
  machineDisplayName: "Enjeksiyon Pres 350T",
  clampForceTon: 350,
  maximumMoldWidthMm: 620,
  maximumMoldHeightMm: 560,
  minimumMoldDepthMm: 180,
  maximumMoldDepthMm: 520,
  tieBarWidthMm: 580,
  tieBarHeightMm: 520,
  maximumShotCapacityGrams: 1200,
  injectionUnit: "A",
  supportedMaterialGroups: ["pp", "abs"],
  sourceType: "manual",
});
assert("upsert press machine profile", pressProfile.status === 200 || pressProfile.status === 201, String(pressProfile.status));

const compatibleEval = await request(
  "POST",
  `/api/molds/${moldId}/compatibility-evaluations/${PRESS_MACHINE}?facilityId=${FACILITY}`,
  {},
);
assert("compatible evaluation", compatibleEval.status === 201 || compatibleEval.status === 200, String(compatibleEval.status));
assert("compatible status", compatibleEval.json.status === "compatible", compatibleEval.json.status);
assert(
  "theoretical capacity",
  compatibleEval.json.theoreticalPartsPerHour === 1600,
  String(compatibleEval.json.theoreticalPartsPerHour),
);

const incompatibleReq = await request("PUT", `/api/molds/${moldId}/compatibility-requirement?facilityId=${FACILITY}`, {
  requiredClampForceTon: 400,
  moldWidthMm: 500,
  moldHeightMm: 450,
  moldDepthMm: 400,
  requiredInjectionUnit: "A",
  supportedMaterialGroup: "pp",
  nominalCycleTimeSeconds: 18,
  effectiveCavityCount: 8,
  sourceType: "manual",
  expectedVersion: compatibleReq.json.version,
});
assert("upsert incompatible requirement", incompatibleReq.status === 200, String(incompatibleReq.status));

const incompatibleEval = await request(
  "POST",
  `/api/molds/${moldId}/compatibility-evaluations/${PRESS_MACHINE}?facilityId=${FACILITY}`,
  {},
);
assert("incompatible evaluation", incompatibleEval.status === 201 || incompatibleEval.status === 200, String(incompatibleEval.status));
assert("incompatible status", incompatibleEval.json.status === "incompatible", incompatibleEval.json.status);

const cncProfile = await request("PUT", `/api/molds/machine-compatibility-profiles/${CNC_MACHINE}?facilityId=${FACILITY}`, {
  machineCode: "CNC-001",
  machineDisplayName: "CNC Tezgâh 001",
  sourceType: "manual",
});
assert("upsert insufficient machine profile", cncProfile.status === 200 || cncProfile.status === 201, String(cncProfile.status));

const insufficientEval = await request(
  "POST",
  `/api/molds/${moldId}/compatibility-evaluations/${CNC_MACHINE}?facilityId=${FACILITY}`,
  {},
);
assert("insufficient-data evaluation", insufficientEval.status === 201 || insufficientEval.status === 200, String(insufficientEval.status));
assert("insufficient-data status", insufficientEval.json.status === "insufficient-data", insufficientEval.json.status);

const dupEval = await request(
  "POST",
  `/api/molds/${moldId}/compatibility-evaluations/${PRESS_MACHINE}?facilityId=${FACILITY}`,
  {},
);
assert("duplicate evaluation replay", dupEval.status === 200, String(dupEval.status));

const beforeCount = db(
  `SELECT COUNT(*) FROM mold_machine_compatibility_assessments WHERE mold_id = '${moldId}' AND machine_asset_id = '${PRESS_MACHINE}'`,
);
assert("duplicate assessment protection", beforeCount === "2", beforeCount);

const conditionalCode = `MOLD3-COND-${Date.now()}`;
const createConditionalMold = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: conditionalCode,
  name: "MOLD-3 conditional verifier mold",
  ownershipType: "company-owned",
  locationType: "in-plant",
  locationLabel: "Verifier",
  lifecycleState: "active",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("create conditional mold", createConditionalMold.status === 201, String(createConditionalMold.status));
const conditionalMoldId = createConditionalMold.json.id;

const conditionalReq = await request(
  "PUT",
  `/api/molds/${conditionalMoldId}/compatibility-requirement?facilityId=${FACILITY}`,
  {
    requiredClampForceTon: 300,
    moldWidthMm: 500,
    moldHeightMm: 450,
    moldDepthMm: 400,
    requiredTieBarWidthMm: 551,
    requiredTieBarHeightMm: 480,
    minimumShotCapacityGrams: 800,
    requiredInjectionUnit: "A",
    supportedMaterialGroup: "pp",
    nominalCycleTimeSeconds: 18,
    effectiveCavityCount: 8,
    sourceType: "manual",
    expectedVersion: null,
  },
);
assert(
  "upsert conditional requirement",
  conditionalReq.status === 200 || conditionalReq.status === 201,
  String(conditionalReq.status),
);

const conditionalProfile = await request(
  "PUT",
  `/api/molds/machine-compatibility-profiles/${PRESS_MACHINE}?facilityId=${FACILITY}`,
  {
    machineCode: "PRESS-001",
    machineDisplayName: "Enjeksiyon Pres 350T",
    clampForceTon: 350,
    maximumMoldWidthMm: 620,
    maximumMoldHeightMm: 560,
    minimumMoldDepthMm: 180,
    maximumMoldDepthMm: 520,
    tieBarWidthMm: 580,
    tieBarHeightMm: 520,
    maximumShotCapacityGrams: 1200,
    injectionUnit: "A",
    supportedMaterialGroups: ["pp", "abs"],
    sourceType: "manual",
  },
);
assert(
  "upsert conditional machine profile",
  conditionalProfile.status === 200 || conditionalProfile.status === 201,
  String(conditionalProfile.status),
);

const conditionalEval = await request(
  "POST",
  `/api/molds/${conditionalMoldId}/compatibility-evaluations/${PRESS_MACHINE}?facilityId=${FACILITY}`,
  {},
);
assert(
  "conditional evaluation HTTP",
  conditionalEval.status === 201 || conditionalEval.status === 200,
  String(conditionalEval.status),
);
assert("conditional status", conditionalEval.json.status === "conditional", conditionalEval.json.status);

const conditionalReasons = Array.isArray(conditionalEval.json.reasons) ? conditionalEval.json.reasons : [];
const conditionalWarning = conditionalReasons.find(
  (reason) => reason.ruleCode === "tie-bar-width-margin-warning" && reason.status === "warn",
);
assert("conditional warning reason code", Boolean(conditionalWarning));
assert(
  "conditional required/available values",
  Boolean(conditionalWarning?.requiredValue) && Boolean(conditionalWarning?.availableValue),
);
assert("conditional Turkish label", conditionalWarning?.labelTr?.includes("Kundura genişliği"));
assert(
  "conditional no hard failure",
  !conditionalReasons.some((reason) => reason.status === "fail"),
);

const conditionalEvidence = conditionalEval.json.evidence ?? {};
assert(
  "conditional evidence disclaimer",
  conditionalEvidence.theoreticalCapacityDisclaimer === "Gerçek üretim kapasitesi veya OEE değildir.",
);
assert("conditional no percentage claim", !JSON.stringify(conditionalEval.json).includes("%"));

const concurrentCode = `MOLD3-RACE-${Date.now()}`;
const createConcurrentMold = await request("POST", `/api/molds?facilityId=${FACILITY}`, {
  code: concurrentCode,
  name: "MOLD-3 concurrent verifier mold",
  ownershipType: "company-owned",
  locationType: "in-plant",
  locationLabel: "Verifier",
  lifecycleState: "active",
  sourceType: "manual",
  idempotencyKey: crypto.randomUUID(),
});
assert("create concurrent mold", createConcurrentMold.status === 201, String(createConcurrentMold.status));
const concurrentMoldId = createConcurrentMold.json.id;

const concurrentReq = await request("PUT", `/api/molds/${concurrentMoldId}/compatibility-requirement?facilityId=${FACILITY}`, {
  requiredClampForceTon: 300,
  moldWidthMm: 500,
  moldHeightMm: 450,
  moldDepthMm: 400,
  requiredTieBarWidthMm: 520,
  requiredTieBarHeightMm: 480,
  minimumShotCapacityGrams: 800,
  requiredInjectionUnit: "A",
  supportedMaterialGroup: "pp",
  nominalCycleTimeSeconds: 18,
  effectiveCavityCount: 8,
  sourceType: "manual",
  expectedVersion: null,
});
assert("upsert concurrent requirement", concurrentReq.status === 200 || concurrentReq.status === 201, String(concurrentReq.status));

const concurrentProfile = await request(
  "PUT",
  `/api/molds/machine-compatibility-profiles/${PRESS_MACHINE}?facilityId=${FACILITY}`,
  {
    machineCode: "PRESS-001",
    machineDisplayName: "Enjeksiyon Pres 350T",
    clampForceTon: 350,
    maximumMoldWidthMm: 620,
    maximumMoldHeightMm: 560,
    minimumMoldDepthMm: 180,
    maximumMoldDepthMm: 520,
    tieBarWidthMm: 580,
    tieBarHeightMm: 520,
    maximumShotCapacityGrams: 1200,
    injectionUnit: "A",
    supportedMaterialGroups: ["pp", "abs"],
    sourceType: "manual",
  },
);
assert(
  "upsert concurrent machine profile",
  concurrentProfile.status === 200 || concurrentProfile.status === 201,
  String(concurrentProfile.status),
);

const concurrentCountBefore = Number(
  db(
    `SELECT COUNT(*) FROM mold_machine_compatibility_assessments WHERE mold_id = '${concurrentMoldId}' AND machine_asset_id = '${PRESS_MACHINE}'`,
  ),
);
assert("concurrent baseline count", concurrentCountBefore === 0, String(concurrentCountBefore));

const concurrentPath = `/api/molds/${concurrentMoldId}/compatibility-evaluations/${PRESS_MACHINE}?facilityId=${FACILITY}`;
const [concurrentA, concurrentB] = await Promise.all([
  request("POST", concurrentPath, {}),
  request("POST", concurrentPath, {}),
]);
assert("concurrent request A success", concurrentA.status === 201 || concurrentA.status === 200, String(concurrentA.status));
assert("concurrent request B success", concurrentB.status === 201 || concurrentB.status === 200, String(concurrentB.status));
assert("concurrent no HTTP 500", concurrentA.status !== 500 && concurrentB.status !== 500);

const concurrentAssessmentIds = [concurrentA.json?.id, concurrentB.json?.id];
assert(
  "concurrent same assessment identity",
  concurrentAssessmentIds[0] && concurrentAssessmentIds[0] === concurrentAssessmentIds[1],
  concurrentAssessmentIds.join(" vs "),
);

const concurrentCountAfter = Number(
  db(
    `SELECT COUNT(*) FROM mold_machine_compatibility_assessments WHERE mold_id = '${concurrentMoldId}' AND machine_asset_id = '${PRESS_MACHINE}'`,
  ),
);
assert("concurrent count increase", concurrentCountAfter - concurrentCountBefore === 1, String(concurrentCountAfter));

const concurrentStatuses = [concurrentA.status, concurrentB.status];
const concurrentReplayFlags = [concurrentA.json?.idempotentReplay, concurrentB.json?.idempotentReplay];
assert(
  "concurrent replay semantics",
  concurrentStatuses.includes(201)
    && concurrentStatuses.includes(200)
    && concurrentReplayFlags.includes(true)
    && concurrentReplayFlags.includes(false),
  `statuses=${concurrentStatuses.join(",")}, replay=${concurrentReplayFlags.join(",")}`,
);

db(`DELETE FROM mold_machine_compatibility_assessments WHERE mold_id = '${conditionalMoldId}'`);
db(`DELETE FROM mold_compatibility_requirements WHERE mold_id = '${conditionalMoldId}'`);
db(`DELETE FROM mold_lifecycle_events WHERE mold_id = '${conditionalMoldId}'`);
db(`DELETE FROM molds WHERE id = '${conditionalMoldId}'`);
assert(
  "conditional closure fixture removed",
  db(`SELECT COUNT(*) FROM molds WHERE id = '${conditionalMoldId}'`) === "0",
);

db(`DELETE FROM mold_machine_compatibility_assessments WHERE mold_id = '${concurrentMoldId}'`);
db(`DELETE FROM mold_compatibility_requirements WHERE mold_id = '${concurrentMoldId}'`);
db(`DELETE FROM mold_lifecycle_events WHERE mold_id = '${concurrentMoldId}'`);
db(`DELETE FROM molds WHERE id = '${concurrentMoldId}'`);
assert(
  "concurrent closure fixture removed",
  db(`SELECT COUNT(*) FROM molds WHERE id = '${concurrentMoldId}'`) === "0",
);

const zeroCycleReq = await request("PUT", `/api/molds/${moldId}/compatibility-requirement?facilityId=${FACILITY}`, {
  requiredClampForceTon: 300,
  nominalCycleTimeSeconds: 0,
  effectiveCavityCount: 8,
  sourceType: "manual",
  expectedVersion: incompatibleReq.json.version,
});
assert("zero cycle-time rejected", zeroCycleReq.status === 400, String(zeroCycleReq.status));

db(`DELETE FROM mold_machine_compatibility_assessments WHERE mold_id = '${moldId}'`);
db(`DELETE FROM mold_compatibility_requirements WHERE mold_id = '${moldId}'`);
db(`DELETE FROM mold_lifecycle_events WHERE mold_id = '${moldId}'`);
db(`DELETE FROM molds WHERE id = '${moldId}'`);
assert("closure fixtures removed", db(`SELECT COUNT(*) FROM molds WHERE id = '${moldId}'`) === "0");

console.log("MOLD-3 deterministic compatibility/capacity verifier passed.");

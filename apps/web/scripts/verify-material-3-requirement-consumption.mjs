#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL_PP = "bb000001-00a2-4000-8000-0000000000a2";
const MATERIAL_ABS = "bb000001-00a4-4000-8000-0000000000a4";
const LOT_PP = "bb000001-00aa-4000-8000-0000000000aa";
const PILOT_PP_PROFILE = "bb000001-00ae-4000-8000-0000000000ae";
const PILOT_ABS_PARTIAL = "bb000001-00af-4000-8000-0000000000af";
const RUN_SUFFIX = Date.now();
const PREFIX = `MAT3-VERIFY-${RUN_SUFFIX}`;
const CLOSURE_PREFIX = `MAT3-CLOSURE-${RUN_SUFFIX}`;
const CLOSURE_CODE_PREFIX = "MAT3-CLOSURE";
const LOCATION_RAW = "bb000001-00a7-4000-8000-0000000000a7";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "production-pilot-operator",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`MATERIAL-3 requirement/consumption verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(actor = ACTOR) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
  };
}

async function request(method, path, body, actor = ACTOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(actor),
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, raw: text };
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function dbCount(sql) {
  const value = Number(db(sql));
  if (!Number.isFinite(value)) fail(`Expected numeric db count, got: ${value}`);
  return value;
}

function closureMaterialPredicate(alias) {
  const prefix = alias ? `${alias}.` : "";
  return `${prefix}code LIKE '${CLOSURE_CODE_PREFIX}%' OR ${prefix}create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`;
}

function createMaterialBody(code, idempotencyKey, overrides = {}) {
  return {
    code,
    name: `${code} Closure Material`,
    materialCategory: "virgin-polymer",
    polymerFamily: "PP",
    physicalForm: "granule",
    unitOfMeasure: "kg",
    isActive: true,
    sourceType: "manual",
    sourceReference: `${CLOSURE_PREFIX}-material-${code}`,
    idempotencyKey,
    ...overrides,
  };
}

function createProfileBody(materialId, idempotencyKey, overrides = {}) {
  return {
    materialId,
    requirementType: "production-order",
    productionOrderReference: `${CLOSURE_PREFIX}-PO`,
    goodPartTargetQuantity: 100,
    partWeightGrams: 10,
    plannedVirginPercent: 85,
    plannedRegrindPercent: 15,
    sourceType: "manual",
    sourceReference: `${CLOSURE_PREFIX}-profile`,
    dataRecordedAtUtc: new Date().toISOString(),
    idempotencyKey,
    ...overrides,
  };
}

function cleanupClosureFixtures() {
  db(`DELETE FROM material_consumption_observations WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%' OR material_id IN (SELECT id FROM materials WHERE ${closureMaterialPredicate()});`);
  db(`DELETE FROM material_requirement_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%' OR material_id IN (SELECT id FROM materials WHERE ${closureMaterialPredicate()});`);
  db(`DELETE FROM material_requirement_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%' OR material_id IN (SELECT id FROM materials WHERE ${closureMaterialPredicate()});`);
  db(`DELETE FROM material_lot_traceability_events WHERE material_lot_id IN (SELECT id FROM material_lots WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%' OR material_id IN (SELECT id FROM materials WHERE ${closureMaterialPredicate()}));`);
  db(`DELETE FROM material_lots WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%' OR material_id IN (SELECT id FROM materials WHERE ${closureMaterialPredicate()});`);
  db(`DELETE FROM material_technical_specifications WHERE material_id IN (SELECT id FROM materials WHERE ${closureMaterialPredicate()});`);
  db(`DELETE FROM materials WHERE ${closureMaterialPredicate()};`);
}

console.log("Running MATERIAL-3 requirement/consumption verifier.");

const ready = await fetch(`${BASE}/health/ready`);
if (!ready.ok) fail("API not ready — start NevaxisFactOS.Api first");
assert("API ready", ready.ok);

// Complete theoretical requirement via pilot PP profile
const overview = await request("GET", `/api/materials/${MATERIAL_PP}/requirement-overview?facilityId=${FACILITY}`);
assert("overview HTTP 200", overview.status === 200);
assert("overview has PP pilot profile", Array.isArray(overview.json?.profiles) && overview.json.profiles.length > 0);

const evalReq = await request("POST", `/api/materials/${MATERIAL_PP}/requirement-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: PILOT_PP_PROFILE,
  idempotencyKey: `${PREFIX}-req-eval`,
});
assert("requirement evaluation HTTP 201", evalReq.status === 201, `status=${evalReq.status}`);

const assessmentId = evalReq.json?.id;
const assessment = await request("GET", `/api/materials/assessments/${assessmentId}?facilityId=${FACILITY}`);
assert("assessment detail HTTP 200", assessment.status === 200);
assert("theoretical requirement calculated", assessment.json?.theoreticalRequirementGrams > 0);
assert("rule set version", assessment.json?.ruleSetVersion === "MATERIAL-REQUIREMENT-V1");

// Insufficient-data ABS profile
const absEval = await request("POST", `/api/materials/${MATERIAL_ABS}/requirement-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: PILOT_ABS_PARTIAL,
  idempotencyKey: `${PREFIX}-abs-eval`,
});
assert("ABS insufficient-data evaluation", absEval.status === 201);
const absAssessment = await request("GET", `/api/materials/assessments/${absEval.json?.id}?facilityId=${FACILITY}`);
assert("ABS assessment insufficient-data", absAssessment.json?.status === "insufficient-data");

// Regrind compliant (PP 15% vs 20% limit) — use pilot complete profile with 20% planned regrind in seed... check exceeds profile
const regrindEval = await request("POST", `/api/materials/${MATERIAL_PP}/regrind-policy-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: PILOT_PP_PROFILE,
  idempotencyKey: `${PREFIX}-regrind-compliant`,
});
assert("regrind evaluation HTTP 201", regrindEval.status === 201);
const regrindAssessment = await request("GET", `/api/materials/assessments/${regrindEval.json?.id}?facilityId=${FACILITY}`);
assert("PP regrind compliant", regrindAssessment.json?.status === "compliant");

// Exceeds-limit profile
const exceedsProfile = overview.json?.profiles?.find((p) => p.plannedRegrindPercent === 25);
if (exceedsProfile) {
  const exceedsEval = await request("POST", `/api/materials/${MATERIAL_PP}/regrind-policy-evaluations?facilityId=${FACILITY}`, {
    requirementProfileId: exceedsProfile.id,
    idempotencyKey: `${PREFIX}-regrind-exceeds`,
  });
  const exceedsAssessment = await request("GET", `/api/materials/assessments/${exceedsEval.json?.id}?facilityId=${FACILITY}`);
  assert("exceeds-limit regrind", exceedsAssessment.json?.status === "exceeds-limit");
}

// Consumption observation append
const obsKey = `${PREFIX}-obs`;
const obsBody = {
  materialId: MATERIAL_PP,
  materialLotId: LOT_PP,
  requirementProfileId: PILOT_PP_PROFILE,
  observationType: "consumption-observed",
  quantity: 2.5,
  unitOfMeasure: "kg",
  virginQuantity: 2.0,
  regrindQuantity: 0.5,
  observedAtUtc: new Date().toISOString(),
  sourceType: "manual",
  idempotencyKey: obsKey,
};
const obsCreate = await request("POST", `/api/materials/consumption-observations?facilityId=${FACILITY}`, obsBody);
assert("consumption observation HTTP 201", obsCreate.status === 201, `status=${obsCreate.status}`);

// Idempotent replay
const obsReplay = await request("POST", `/api/materials/consumption-observations?facilityId=${FACILITY}`, obsBody);
assert("observation idempotent replay HTTP 201", obsReplay.status === 201);
assert("observation idempotent replay flag", obsReplay.json?.idempotentReplay === true);

// Conflicting replay
const obsConflict = await request("POST", `/api/materials/consumption-observations?facilityId=${FACILITY}`, {
  ...obsBody,
  quantity: 99,
});
assert("observation conflicting replay HTTP 409", obsConflict.status === 409);

// Negative quantity
const negObs = await request("POST", `/api/materials/consumption-observations?facilityId=${FACILITY}`, {
  ...obsBody,
  quantity: -1,
  idempotencyKey: `${PREFIX}-neg`,
});
assert("negative quantity HTTP 400", negObs.status === 400);

// Lot mismatch
const lotMismatch = await request("POST", `/api/materials/consumption-observations?facilityId=${FACILITY}`, {
  ...obsBody,
  materialId: MATERIAL_ABS,
  idempotencyKey: `${PREFIX}-lot-mismatch`,
});
assert("lot mismatch HTTP 400", lotMismatch.status === 400);

// Variance evaluation
const varEval = await request("POST", `/api/materials/${MATERIAL_PP}/variance-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: PILOT_PP_PROFILE,
  idempotencyKey: `${PREFIX}-variance`,
});
assert("variance evaluation HTTP 201", varEval.status === 201);
const varAssessment = await request("GET", `/api/materials/assessments/${varEval.json?.id}?facilityId=${FACILITY}`);
assert("variance status present", ["within-tolerance", "above-plan", "below-plan", "insufficient-data"].includes(varAssessment.json?.status));

// Concurrent identical evaluation replay
const evalReplay = await request("POST", `/api/materials/${MATERIAL_PP}/requirement-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: PILOT_PP_PROFILE,
  idempotencyKey: `${PREFIX}-req-eval`,
});
assert("assessment idempotent replay", evalReplay.json?.idempotentReplay === true);

// --- MATERIAL-3-LOCAL-CLOSURE scenarios (MAT3-CLOSURE-*) ---

// TEST 2 — prohibited regrind (MaximumRegrindPercent = 0, planned > 0)
const prohibCode = `${CLOSURE_CODE_PREFIX}-PROH-${RUN_SUFFIX}`;
const prohibCreate = await request("POST", `/api/materials?facilityId=${FACILITY}`, createMaterialBody(prohibCode, `${CLOSURE_PREFIX}-mat-proh`));
assert("closure prohibited material HTTP 201", prohibCreate.status === 201, `status=${prohibCreate.status}`);
const prohibMaterialId = prohibCreate.json?.id;
const prohibDetailBefore = await request("GET", `/api/materials/${prohibMaterialId}?facilityId=${FACILITY}`);
assert("closure prohibited material detail HTTP 200", prohibDetailBefore.status === 200);
const prohibTech = await request("PUT", `/api/materials/${prohibMaterialId}/technical-specification?facilityId=${FACILITY}`, {
  expectedVersion: prohibDetailBefore.json.version,
  densityGPerCm3: 0.905,
  maximumRegrindPercent: 0,
  specificationSource: "technical-policy",
  specificationSourceReference: `${CLOSURE_PREFIX}-policy-prohibited`,
  specificationRecordedAtUtc: new Date().toISOString(),
});
assert("closure prohibited technical policy HTTP 200", prohibTech.status === 200, `status=${prohibTech.status} ${prohibTech.raw}`);
const prohibProfileCreate = await request("POST", `/api/materials/requirement-profiles?facilityId=${FACILITY}`, createProfileBody(prohibMaterialId, `${CLOSURE_PREFIX}-profile-proh`, {
  plannedVirginPercent: 85,
  plannedRegrindPercent: 15,
}));
assert("closure prohibited profile HTTP 201", prohibProfileCreate.status === 201);
const prohibProfileId = prohibProfileCreate.json?.id;
const prohibRegrindEval = await request("POST", `/api/materials/${prohibMaterialId}/regrind-policy-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: prohibProfileId,
  idempotencyKey: `${CLOSURE_PREFIX}-regrind-proh`,
});
assert("closure prohibited regrind eval HTTP 201", prohibRegrindEval.status === 201);
const prohibAssessment = await request("GET", `/api/materials/assessments/${prohibRegrindEval.json?.id}?facilityId=${FACILITY}`);
assert("closure prohibited status", prohibAssessment.json?.status === "prohibited");
const prohibDriver = prohibAssessment.json?.drivers?.find((d) => d.code === "regrind-prohibited");
assert("closure prohibited driver present", Boolean(prohibDriver));
const prohibPlannedEvidence = prohibAssessment.json?.evidence?.find((e) => e.field === "plannedRegrindPercent");
const prohibLimitEvidence = prohibAssessment.json?.evidence?.find((e) => e.field === "maximumRegrindPercent");
assert("closure prohibited planned evidence", prohibPlannedEvidence?.value === "15.00");
assert("closure prohibited limit evidence", prohibLimitEvidence?.value === "0.00");
assert("closure prohibited structured evidence", Array.isArray(prohibAssessment.json?.evidence) && prohibAssessment.json.evidence.length >= 2);
const prohibDetailAfter = await request("GET", `/api/materials/${prohibMaterialId}?facilityId=${FACILITY}`);
assert("closure prohibited no material mutation", prohibDetailAfter.json?.version === prohibTech.json?.version);
assert("closure prohibited no profile mutation", prohibProfileCreate.json?.version === 1);

// TEST 3 — unknown-limit regrind (MaximumRegrindPercent = NULL, planned > 0)
const unkCode = `${CLOSURE_CODE_PREFIX}-UNK-${RUN_SUFFIX}`;
const unkCreate = await request("POST", `/api/materials?facilityId=${FACILITY}`, createMaterialBody(unkCode, `${CLOSURE_PREFIX}-mat-unk`));
assert("closure unknown-limit material HTTP 201", unkCreate.status === 201);
const unkMaterialId = unkCreate.json?.id;
const unkProfileCreate = await request("POST", `/api/materials/requirement-profiles?facilityId=${FACILITY}`, createProfileBody(unkMaterialId, `${CLOSURE_PREFIX}-profile-unk`, {
  plannedVirginPercent: 90,
  plannedRegrindPercent: 10,
}));
assert("closure unknown-limit profile HTTP 201", unkProfileCreate.status === 201);
const unkProfileId = unkProfileCreate.json?.id;
const unkRegrindEval = await request("POST", `/api/materials/${unkMaterialId}/regrind-policy-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: unkProfileId,
  idempotencyKey: `${CLOSURE_PREFIX}-regrind-unk`,
});
assert("closure unknown-limit regrind eval HTTP 201", unkRegrindEval.status === 201);
const unkAssessment = await request("GET", `/api/materials/assessments/${unkRegrindEval.json?.id}?facilityId=${FACILITY}`);
assert("closure unknown-limit status", unkAssessment.json?.status === "insufficient-data");
const unkMissingLimit = unkAssessment.json?.evidence?.find((e) => e.field === "maximumRegrindPercent");
assert("closure unknown-limit missing limit identified", unkMissingLimit?.isMissing === true);
assert("closure unknown-limit no compliant result", unkAssessment.json?.status !== "compliant");
assert("closure unknown-limit no inferred percentage", unkMissingLimit?.value == null || unkMissingLimit?.value === "");

// TEST 4 — within-tolerance variance (5% tolerance, isolated material)
const varCode = `${CLOSURE_CODE_PREFIX}-VAR-${RUN_SUFFIX}`;
const varCreate = await request("POST", `/api/materials?facilityId=${FACILITY}`, createMaterialBody(varCode, `${CLOSURE_PREFIX}-mat-var`));
assert("closure variance material HTTP 201", varCreate.status === 201);
const varMaterialId = varCreate.json?.id;
const varProfileCreate = await request("POST", `/api/materials/requirement-profiles?facilityId=${FACILITY}`, createProfileBody(varMaterialId, `${CLOSURE_PREFIX}-profile-var`, {
  plannedVirginPercent: 100,
  plannedRegrindPercent: 0,
  setupAllowanceGrams: 0,
  processLossAllowancePercent: 0,
}));
assert("closure variance profile HTTP 201", varProfileCreate.status === 201);
const varProfileId = varProfileCreate.json?.id;
const varLotCreate = await request("POST", `/api/materials/lots?facilityId=${FACILITY}`, {
  materialId: varMaterialId,
  lotNumber: `${CLOSURE_CODE_PREFIX}-LOT-${RUN_SUFFIX}`,
  unitOfMeasure: "kg",
  qualityStatus: "pending",
  sourceType: "manual",
  sourceReference: `${CLOSURE_PREFIX}-lot-var`,
  idempotencyKey: `${CLOSURE_PREFIX}-lot-var`,
  storageLocationId: LOCATION_RAW,
  initialQuantity: 50,
});
assert("closure variance lot HTTP 201", varLotCreate.status === 201);
const varLotId = varLotCreate.json?.id;
const lotQtyBefore = db(`SELECT COALESCE(current_observed_quantity::text, 'null') FROM material_lots WHERE id = '${varLotId}';`);
const varObs = await request("POST", `/api/materials/consumption-observations?facilityId=${FACILITY}`, {
  materialId: varMaterialId,
  materialLotId: varLotId,
  requirementProfileId: varProfileId,
  observationType: "consumption-observed",
  quantity: 1.02,
  unitOfMeasure: "kg",
  virginQuantity: 1.02,
  regrindQuantity: 0,
  observedAtUtc: new Date().toISOString(),
  sourceType: "manual",
  sourceReference: `${CLOSURE_PREFIX}-obs-var`,
  idempotencyKey: `${CLOSURE_PREFIX}-obs-var`,
});
assert("closure variance observation HTTP 201", varObs.status === 201);
const lotQtyAfter = db(`SELECT COALESCE(current_observed_quantity::text, 'null') FROM material_lots WHERE id = '${varLotId}';`);
assert("closure variance lot quantity unchanged", lotQtyBefore === lotQtyAfter, `before=${lotQtyBefore} after=${lotQtyAfter}`);
const closureVarEval = await request("POST", `/api/materials/${varMaterialId}/variance-evaluations?facilityId=${FACILITY}`, {
  requirementProfileId: varProfileId,
  idempotencyKey: `${CLOSURE_PREFIX}-variance`,
});
assert("closure variance eval HTTP 201", closureVarEval.status === 201);
const closureVarAssessment = await request("GET", `/api/materials/assessments/${closureVarEval.json?.id}?facilityId=${FACILITY}`);
assert("closure variance within-tolerance status", closureVarAssessment.json?.status === "within-tolerance");
assert("closure variance theoretical present", closureVarAssessment.json?.theoreticalRequirementGrams === 1000);
assert("closure variance observed present", closureVarAssessment.json?.observedConsumptionGrams === 1020);
assert("closure variance grams correct", closureVarAssessment.json?.varianceGrams === 20);
assert("closure variance percent correct", closureVarAssessment.json?.variancePercent === 2);
assert("closure variance drivers present", Array.isArray(closureVarAssessment.json?.drivers) && closureVarAssessment.json.drivers.length > 0);
assert("closure variance evidence present", Array.isArray(closureVarAssessment.json?.evidence) && closureVarAssessment.json.evidence.length > 0);
const closureVarAssessmentJson = JSON.stringify(closureVarAssessment.json ?? {});
assert("closure variance no financial-loss language", !/financial|finansal|kayıp|loss/i.test(closureVarAssessmentJson));

const closurePreviewProfiles = dbCount(`SELECT COUNT(*) FROM material_requirement_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';`);
const closurePreviewObservations = dbCount(`SELECT COUNT(*) FROM material_consumption_observations WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`);
const closurePreviewAssessments = dbCount(`SELECT COUNT(*) FROM material_requirement_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`);
assert("closure fixtures previewed profiles", closurePreviewProfiles >= 3);
assert("closure fixtures previewed observations", closurePreviewObservations >= 1);
assert("closure fixtures previewed assessments", closurePreviewAssessments >= 3);

// Cleanup verifier fixtures only
db(`DELETE FROM material_consumption_observations WHERE idempotency_key LIKE '${PREFIX}%';`);
db(`DELETE FROM material_requirement_assessments WHERE idempotency_key LIKE '${PREFIX}%';`);

cleanupClosureFixtures();

const closureProfilesRemaining = dbCount(`SELECT COUNT(*) FROM material_requirement_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';`);
const closureObservationsRemaining = dbCount(`SELECT COUNT(*) FROM material_consumption_observations WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`);
const closureAssessmentsRemaining = dbCount(`SELECT COUNT(*) FROM material_requirement_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';`);
const closureMaterialsRemaining = dbCount(`SELECT COUNT(*) FROM materials WHERE ${closureMaterialPredicate()};`);
assert("closure profiles remaining = 0", closureProfilesRemaining === 0);
assert("closure observations remaining = 0", closureObservationsRemaining === 0);
assert("closure assessments remaining = 0", closureAssessmentsRemaining === 0);
assert("closure materials remaining = 0", closureMaterialsRemaining === 0);

const pilotProfiles = dbCount("SELECT COUNT(*) FROM material_requirement_profiles WHERE id IN ('bb000001-00ae-4000-8000-0000000000ae','bb000001-00af-4000-8000-0000000000af','bb000001-00b0-4000-8000-0000000000b0');");
assert("pilot requirement profiles preserved", pilotProfiles === 3);

console.log("MATERIAL-3 requirement/consumption verification passed.");

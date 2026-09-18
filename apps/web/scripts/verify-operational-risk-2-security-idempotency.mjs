#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PILOT_PROFILE = "bb000001-0210-4000-8000-000000000210";
const CLOSURE_PREFIX = "OPRISK2-CLOSURE";
const RUN_SUFFIX = Date.now();
const PROVENANCE =
  "Deterministic local Development operational-risk detection data — simulated, non-production, not real customer evidence, predicted loss, or guaranteed savings.";
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
  console.error(`OPERATIONAL-RISK-2 security/idempotency verification failed: ${message}`);
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
    DELETE FROM operational_risk_detection_drivers
    WHERE result_id IN (
      SELECT id FROM operational_risk_detection_results
      WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')
    );
    DELETE FROM operational_risk_detection_results
    WHERE run_id IN (SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM operational_risk_detection_rules
    WHERE profile_id IN (SELECT id FROM operational_risk_detection_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_detection_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

function countClosureArtifacts() {
  return Number(db(`
    SELECT
      (SELECT COUNT(*) FROM operational_risk_detection_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%')
      + (SELECT COUNT(*) FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')
      + (SELECT COUNT(*) FROM operational_risk_detection_results WHERE run_id IN (
          SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'))
      + (SELECT COUNT(*) FROM operational_risk_detection_drivers WHERE result_id IN (
          SELECT id FROM operational_risk_detection_results WHERE run_id IN (
            SELECT id FROM operational_risk_detection_runs WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%')))
  `));
}

console.log("Running OPERATIONAL-RISK-2 security/idempotency verifier.");

const routes = readRequired("apps/api/OperationalRisk/OperationalRiskEndpointRouteBuilderExtensions.cs");
forbidText(routes, "ai-task", "detection routes");
forbidText(routes, "notification", "detection routes");
forbidText(routes, "remediation", "detection routes");
assert("detection profile routes", routes.includes("/detection/profiles"));
assert("detection run routes", routes.includes("/detection/runs"));

removeClosureFixtures();

const noAuth = await liveRequest("GET", `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`, null, {});
assert("missing auth returns 401", noAuth.status === 401);

const readOnlyCreate = await liveRequest(
  "POST",
  `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`,
  {
    profileCode: `${CLOSURE_PREFIX}-PROFILE`,
    displayName: "Closure profile",
    sourceProvenance: PROVENANCE,
    idempotencyKey: `${CLOSURE_PREFIX}-PROFILE-${RUN_SUFFIX}`,
    rules: [{
      ruleCode: "closure-rule",
      sourceModule: "energy",
      sourceOutcome: "attention",
      riskCategory: "energy",
      defaultLikelihood: 3,
      defaultImpact: 3,
      thresholdVarianceBand: null,
      driverReasonCode: "energy-risk",
      priorityOrder: 1,
      isActive: true,
    }],
  },
  actorHeaders(READONLY),
);
assert("read-only profile create returns 403", readOnlyCreate.status === 403);
assert("denied profile create writes zero records", countClosureArtifacts() === 0);

const wrongFacility = await liveRequest(
  "POST",
  `/api/operational-risks/detection/runs?facilityId=${UNAUTH_FACILITY}`,
  {
    profileId: PILOT_PROFILE,
    windowStartUtc: "2026-08-01T00:00:00Z",
    windowEndUtc: "2030-01-01T00:00:00Z",
    sourceProvenance: PROVENANCE,
    idempotencyKey: `${CLOSURE_PREFIX}-WRONG-${RUN_SUFFIX}`,
  },
);
assert("wrong facility returns 403", wrongFacility.status === 403);

const crossTenant = await liveRequest(
  "GET",
  `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`,
  null,
  {
    ...actorHeaders(MANAGER),
    "X-NevaxisZentra-Tenant": "other-tenant",
  },
);
assert("cross-tenant denied", crossTenant.status === 401 || crossTenant.status === 403);

const profileKey = `${CLOSURE_PREFIX}-PROFILE-OK-${RUN_SUFFIX}`;
const profileBody = {
  profileCode: `${CLOSURE_PREFIX}-CODE-${RUN_SUFFIX}`,
  displayName: "Closure approved profile",
  sourceProvenance: PROVENANCE,
  idempotencyKey: profileKey,
  rules: [{
    ruleCode: "closure-energy",
    sourceModule: "energy",
    sourceOutcome: "attention",
    riskCategory: "energy",
    defaultLikelihood: 3,
    defaultImpact: 3,
    thresholdVarianceBand: null,
    driverReasonCode: "energy-risk",
    priorityOrder: 1,
    isActive: true,
  }],
};
const createProfile = await liveRequest("POST", `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`, profileBody);
assert("profile create succeeds", createProfile.status === 201 || createProfile.status === 200, createProfile.text);
const profileId = createProfile.json?.id;
const profileVersion = createProfile.json?.version;

const replayProfile = await liveRequest("POST", `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`, profileBody);
assert("profile replay succeeds", replayProfile.status === 200 || replayProfile.status === 201);
assert("profile replay idempotent", replayProfile.json?.idempotentReplay === true);

const conflictProfile = await liveRequest("POST", `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`, {
  ...profileBody,
  displayName: "Different display name",
});
assert("profile payload conflict returns 409", conflictProfile.status === 409);

const staleUpdate = await liveRequest("PUT", `/api/operational-risks/detection/profiles/${profileId}?facilityId=${FACILITY}`, {
  expectedVersion: profileVersion + 10,
  displayName: "Stale update",
  status: "approved",
  sourceProvenance: PROVENANCE,
});
assert("stale profile version returns 409", staleUpdate.status === 409);

const runKey = `${CLOSURE_PREFIX}-RUN-${RUN_SUFFIX}`;
const runBody = {
  profileId: PILOT_PROFILE,
  windowStartUtc: "2026-08-01T00:00:00Z",
  windowEndUtc: "2030-01-01T00:00:00Z",
  sourceProvenance: PROVENANCE,
  idempotencyKey: runKey,
};
const deniedRun = await liveRequest(
  "POST",
  `/api/operational-risks/detection/runs?facilityId=${FACILITY}`,
  runBody,
  actorHeaders(READONLY),
);
assert("read-only run denied", deniedRun.status === 403);

const run1 = await liveRequest("POST", `/api/operational-risks/detection/runs?facilityId=${FACILITY}`, runBody);
assert("run create succeeds", run1.status === 201 || run1.status === 200, run1.text);
const run2 = await liveRequest("POST", `/api/operational-risks/detection/runs?facilityId=${FACILITY}`, runBody);
assert("run replay idempotent", run2.json?.idempotentReplay === true);

const runConflict = await liveRequest("POST", `/api/operational-risks/detection/runs?facilityId=${FACILITY}`, {
  ...runBody,
  windowEndUtc: "2029-01-01T00:00:00Z",
});
assert("run payload conflict returns 409", runConflict.status === 409);

forbidText(run1.text, "Exception", "run response");
forbidText(run1.text, "password", "run response");
forbidText(run1.text, "ConnectionString", "run response");

removeClosureFixtures();
assert("closure fixtures removed", countClosureArtifacts() === 0);
console.log("OPERATIONAL-RISK-2 security/idempotency verification passed.");

#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const EVIDENCE_WITHIN = "bb000001-0321-4000-8000-000000000321";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "COMMPROFIT1-CLOSURE";
const SEC_PREFIX = "COMMPROFIT1-SEC";
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
  console.error(`COMMERCIAL-PROFITABILITY-1 security/idempotency verification failed: ${message}`);
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

function discoverMigrationBasename() {
  const migrationsDir = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations");
  const match = readdirSync(migrationsDir)
    .filter((name) => name.endsWith(".cs") && !name.endsWith(".Designer.cs"))
    .find((name) => name.includes("AddCommercialProfitabilityIntelligenceFoundation"));
  if (!match) {
    fail("Migration AddCommercialProfitabilityIntelligenceFoundation not found");
  }
  return match;
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
    DELETE FROM commercial_profitability_assessment_cost_components
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR idempotency_key LIKE '${SEC_PREFIX}%'
    );
    DELETE FROM commercial_profitability_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
         OR idempotency_key LIKE '${SEC_PREFIX}%'
    );
    DELETE FROM commercial_profitability_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR idempotency_key LIKE '${SEC_PREFIX}%';
    DELETE FROM commercial_profitability_evidence_observations
    WHERE ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR ingest_idempotency_key LIKE '${SEC_PREFIX}%';
    DELETE FROM commercial_profitability_profiles
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'
       OR create_idempotency_key LIKE '${SEC_PREFIX}%';
  `);
}

console.log("Running COMMERCIAL-PROFITABILITY-1 security/idempotency verifier.");

const migrationBasename = discoverMigrationBasename();
const migrationPath = `packages/NevaxisZentra.Persistence.Migrations/Migrations/${migrationBasename}`;
if (!existsSync(resolve(ROOT, migrationPath))) {
  fail(`Missing migration file ${migrationPath}`);
}

const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialProfitabilityEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/CommercialIntelligence/CommercialIntelligenceEndpointHelpers.cs");
const migration = readRequired(migrationPath);
const permissions = readRequired("packages/NevaxisZentra.Persistence/CommercialIntelligence/CommercialIntelligencePermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs");

assert("endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("manage access helper", helpers.includes("ResolveManageAccessAsync"));
assert("commercial-intelligence.read key", permissions.includes("commercial-intelligence.read"));
assert("commercial-intelligence.manage key", permissions.includes("commercial-intelligence.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("assessment append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_profitability_assessments"));
forbidText(migration, "GRANT DELETE ON TABLE commercial_profitability_assessments", "assessments");
forbidText(migration, "permission_definitions", "profitability migration permissions");
forbidText(endpoints, "MapDelete", "commercial profitability endpoints");
forbidText(endpoints, "ai-task", "commercial profitability endpoints");
forbidText(endpoints, "notification", "commercial profitability endpoints");
forbidText(endpoints, "remediation", "commercial profitability endpoints");
assert("bootstrap profitability seed", bootstrap.includes("EnsureCommercialProfitabilityPilotRecordsAsync"));
assert("bootstrap read seed", bootstrap.includes("EnsureCommercialIntelligenceReadPermissionAssignmentAsync"));
assert("bootstrap manage seed", bootstrap.includes("EnsureCommercialIntelligenceManagePermissionAssignmentAsync"));

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(READONLY);
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const profileVersion = Number(db(`SELECT version FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`));
assert("pilot profile version present", profileVersion >= 1, String(profileVersion));

try {
  removeClosureFixtures();

  const assessmentsBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_assessments"));
  const driversBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_assessment_drivers"));
  const componentsBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_assessment_cost_components"));

  const noAuth = await fetch(`${BASE}/api/commercial-intelligence/profitability/profiles?facilityId=${FACILITY}`);
  assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

  const readonlyWrite = await liveRequest(
    "POST",
    `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`,
    {
      profileId: PROFILE,
      expectedProfileVersion: profileVersion,
      salesEvidenceObservationId: EVIDENCE_WITHIN,
      sourceProvenance: "closure",
      idempotencyKey: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
    },
    readonlyHeaders,
  );
  assert("readonly write 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

  const wrongFacility = await liveRequest(
    "GET",
    `/api/commercial-intelligence/profitability/profiles?facilityId=${UNAUTH_FACILITY}`,
    null,
    managerHeaders,
  );
  assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

  const crossTenant = await liveRequest(
    "GET",
    `/api/commercial-intelligence/profitability/profiles?facilityId=${FACILITY}`,
    null,
    crossTenantHeaders,
  );
  assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

  const assessmentsAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_assessments"));
  const driversAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_assessment_drivers"));
  const componentsAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_assessment_cost_components"));
  assert("denied requests created no assessments", assessmentsAfterDeny === assessmentsBeforeDeny, `${assessmentsBeforeDeny}→${assessmentsAfterDeny}`);
  assert("denied requests created no drivers", driversAfterDeny === driversBeforeDeny, `${driversBeforeDeny}→${driversAfterDeny}`);
  assert("denied requests created no cost components", componentsAfterDeny === componentsBeforeDeny, `${componentsBeforeDeny}→${componentsAfterDeny}`);

  const provenance =
    "Deterministic local Development contribution evidence — simulated, non-production, not real customer, quotation, invoice, order, production, cost, accounting, or market evidence.";
  const evaluateBody = {
    profileId: PROFILE,
    expectedProfileVersion: profileVersion,
    salesEvidenceObservationId: EVIDENCE_WITHIN,
    sourceProvenance: provenance,
    idempotencyKey: `${SEC_PREFIX}-evaluate-${RUN_SUFFIX}`,
  };
  const firstEvaluate = await liveRequest(
    "POST",
    `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`,
    evaluateBody,
    managerHeaders,
  );
  assert("authorized evaluate 201", firstEvaluate.status === 201, firstEvaluate.text?.slice(0, 200));
  forbidText(firstEvaluate.text ?? "", "Npgsql", "evaluate response");
  forbidText(firstEvaluate.text ?? "", "password", "evaluate response");
  forbidText(firstEvaluate.text ?? "", "ConnectionString", "evaluate response");

  const replayEvaluate = await liveRequest(
    "POST",
    `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`,
    evaluateBody,
    managerHeaders,
  );
  assert("identical idempotent replay 200", replayEvaluate.status === 200);
  assert("identical replay flag", replayEvaluate.json?.idempotentReplay === true);
  assert("identical replay same id", replayEvaluate.json?.id === firstEvaluate.json?.id);

  const conflictEvaluate = await liveRequest(
    "POST",
    `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`,
    { ...evaluateBody, salesEvidenceObservationId: "bb000001-0322-4000-8000-000000000322" },
    managerHeaders,
  );
  assert("different payload conflict 409", conflictEvaluate.status === 409, `status=${conflictEvaluate.status}`);
  forbidText(conflictEvaluate.text ?? "", "Npgsql", "conflict response");
  forbidText(conflictEvaluate.text ?? "", "password", "conflict response");
  forbidText(conflictEvaluate.text ?? "", "local_dev_only", "conflict response");

  const staleEvaluate = await liveRequest(
    "POST",
    `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`,
    { ...evaluateBody, expectedProfileVersion: 999, idempotencyKey: `${SEC_PREFIX}-stale-${RUN_SUFFIX}` },
    managerHeaders,
  );
  assert("stale profile version 409", staleEvaluate.status === 409, `status=${staleEvaluate.status}`);

  const staleProfile = await liveRequest(
    "PUT",
    `/api/commercial-intelligence/profitability/profiles/${PROFILE}?facilityId=${FACILITY}`,
    {
      expectedVersion: 999,
      name: "Stale profile",
      status: "approved",
      minimumContributionMarginRatio: 0.15,
      maximumEvidenceAgeDays: 180,
      requiredCostComponentKinds: "direct-material,direct-labor,machine,energy",
      requiredCurrencyCode: "TRY",
      requiredUnitOfMeasure: "kg",
      requiredGrossNetBasis: "net",
      requiredTaxInclusion: "excluded",
      requiredDiscountInclusion: "excluded",
      requiredFreightInclusion: "excluded",
      allowedEvidenceStatuses: "simulated",
      sourceProvenance: provenance,
    },
    managerHeaders,
  );
  assert("stale expected version 409", staleProfile.status === 409, `status=${staleProfile.status}`);

  const concurrentKey = `${SEC_PREFIX}-concurrent-${RUN_SUFFIX}`;
  const concurrentPayload = {
    ...evaluateBody,
    idempotencyKey: concurrentKey,
    salesEvidenceObservationId: "bb000001-0323-4000-8000-000000000323",
  };
  const [concurrentA, concurrentB] = await Promise.all([
    liveRequest("POST", `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`, concurrentPayload, managerHeaders),
    liveRequest("POST", `/api/commercial-intelligence/profitability/assessments/evaluate?facilityId=${FACILITY}`, concurrentPayload, managerHeaders),
  ]);
  const concurrentStatuses = [concurrentA.status, concurrentB.status].sort();
  assert("concurrent replay statuses", concurrentStatuses[0] === 200 || concurrentStatuses[0] === 201, JSON.stringify(concurrentStatuses));
  const concurrentCount = Number(db(`SELECT COUNT(*) FROM commercial_profitability_assessments WHERE idempotency_key = '${concurrentKey}'`));
  assert("concurrent replay one assessment", concurrentCount === 1, `count=${concurrentCount}`);
  const concurrentResponseIds = [concurrentA.json?.id, concurrentB.json?.id].filter(Boolean);
  const concurrentDbId = db(`SELECT id::text FROM commercial_profitability_assessments WHERE idempotency_key = '${concurrentKey}'`);
  assert("concurrent same assessment id", concurrentResponseIds.length === 2 && concurrentResponseIds[0] === concurrentResponseIds[1], JSON.stringify(concurrentResponseIds));
  assert("concurrent assessment id matches db", concurrentResponseIds[0] === concurrentDbId, `${concurrentResponseIds[0]} vs ${concurrentDbId}`);
} finally {
  removeClosureFixtures();
  const closureAssessments = Number(db(`
    SELECT COUNT(*) FROM commercial_profitability_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%' OR idempotency_key LIKE '${SEC_PREFIX}%'
  `));
  const closureProfiles = Number(db(`
    SELECT COUNT(*) FROM commercial_profitability_profiles
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%' OR create_idempotency_key LIKE '${SEC_PREFIX}%'
  `));
  assert("SEC and CLOSURE assessments removed", closureAssessments === 0, `count=${closureAssessments}`);
  assert("SEC and CLOSURE profiles removed", closureProfiles === 0, `count=${closureProfiles}`);
}

console.log("COMMERCIAL-PROFITABILITY-1 security/idempotency verification passed.");

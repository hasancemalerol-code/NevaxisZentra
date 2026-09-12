#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MATERIAL = "bb000001-03c0-4000-8000-0000000003c0";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CLOSURE_PREFIX = "COMMPROFIT2-CLOSURE";
const SEC_PREFIX = "COMMPROFIT2-SEC";
const RUN_SUFFIX = Date.now();
const REF_START = "2026-01-01T00:00:00.000Z";
const REF_END = "2026-06-30T23:59:59.000Z";
const CUR_START = "2026-07-01T00:00:00.000Z";
const CUR_END = "2026-12-31T23:59:59.000Z";
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
  console.error(`COMMERCIAL-PROFITABILITY-2 security/idempotency verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function readProvenance() {
  const constants = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialIntelligenceConstants.cs");
  const match = constants.match(/ProfitabilityComparisonDevelopmentPilotProvenance\s*=\s*\n\s*"([^"]+)"/);
  if (!match) fail("ProfitabilityComparisonDevelopmentPilotProvenance not found in CommercialIntelligenceConstants.cs");
  return match[1];
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
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

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
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
    DELETE FROM commercial_profitability_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_profitability_comparison_runs
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%' OR idempotency_key LIKE '${SEC_PREFIX}%'
    );
    DELETE FROM commercial_profitability_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_profitability_comparison_runs
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%' OR idempotency_key LIKE '${SEC_PREFIX}%'
    );
    DELETE FROM commercial_profitability_comparison_runs
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%' OR idempotency_key LIKE '${SEC_PREFIX}%';
  `);
}

console.log("Running COMMERCIAL-PROFITABILITY-2 security/idempotency verifier.");

const endpoints = readRequired("apps/api/CommercialIntelligence/CommercialProfitabilityEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/CommercialIntelligence/CommercialIntelligenceEndpointHelpers.cs");
const migration = readRequired("packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260827155607_AddCommercialProfitabilityComparisonIntelligence.cs");
const permissions = readRequired("packages/NevaxisFactOS.Persistence/CommercialIntelligence/CommercialIntelligencePermissionKeys.cs");
const bootstrap = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotBootstrapService.cs");
const comparisonSeed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotCommercialProfitabilityComparisonSeed.cs");

assert("comparison endpoints require auth", endpoints.includes("RequireAuthorization"));
assert("comparison list endpoint", endpoints.includes('MapGet("/comparisons"'));
assert("comparison evaluate endpoint", endpoints.includes('MapPost("/comparisons/evaluate"'));
assert("read access helper", helpers.includes("ResolveReadAccessAsync"));
assert("manage access helper", helpers.includes("ResolveManageAccessAsync"));
assert("commercial-intelligence.read key", permissions.includes("commercial-intelligence.read"));
assert("commercial-intelligence.manage key", permissions.includes("commercial-intelligence.manage"));
assert("RLS enabled", migration.includes("ENABLE ROW LEVEL SECURITY"));
assert("FORCE RLS", migration.includes("FORCE ROW LEVEL SECURITY"));
assert("comparison runs append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_profitability_comparison_runs"));
assert("comparison results append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_profitability_comparison_results"));
assert("comparison drivers append-only grant", migration.includes("GRANT SELECT, INSERT ON TABLE commercial_profitability_comparison_drivers"));
forbidText(migration, "GRANT DELETE ON TABLE commercial_profitability_comparison_runs", "comparison runs");
forbidText(migration, "GRANT DELETE ON TABLE commercial_profitability_comparison_results", "comparison results");
forbidText(migration, "GRANT DELETE ON TABLE commercial_profitability_comparison_drivers", "comparison drivers");
forbidText(endpoints, "MapDelete", "commercial profitability endpoints");
forbidText(endpoints, "ai-task", "commercial profitability endpoints");
assert("bootstrap profitability seed", bootstrap.includes("EnsureCommercialProfitabilityPilotRecordsAsync"));
assert("bootstrap comparison seed", bootstrap.includes("EnsureCommercialProfitabilityComparisonPilotRecordsAsync"));
assert("comparison seed stable pilot", comparisonSeed.includes("commprofit2-pilot-stable"));
assert("comparison seed twelve scenarios", comparisonSeed.includes("commprofit2-pilot-source-conflict"));

const managerHeaders = actorHeaders(MANAGER);
const readonlyHeaders = actorHeaders(READONLY);
const crossTenantHeaders = actorHeaders({
  issuer: "crevafactos-dev",
  subject: MANAGER.subject,
  tenant: "unknown-tenant-key",
});

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const comparisonsList = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}`,
  null,
  managerHeaders,
);
assert("comparison list endpoint live", comparisonsList.status === 200, `status=${comparisonsList.status}`);

const profileVersion = Number(db(`SELECT version FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`));
assert("pilot profile version present", profileVersion >= 1, String(profileVersion));

removeClosureFixtures();

const runsBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_comparison_runs"));
const resultsBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_comparison_results"));
const driversBeforeDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_comparison_drivers"));

const noAuth = await fetch(`${BASE}/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, `status=${noAuth.status}`);

const readonlyWrite = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`,
  {
    profileId: PROFILE,
    expectedProfileVersion: profileVersion,
    comparisonType: "temporal-contribution-trend",
    materialId: MATERIAL,
    currentWindowStartUtc: CUR_START,
    currentWindowEndUtc: CUR_END,
    referenceWindowStartUtc: REF_START,
    referenceWindowEndUtc: REF_END,
    sourceProvenance: "closure",
    idempotencyKey: `${CLOSURE_PREFIX}-readonly-${RUN_SUFFIX}`,
  },
  readonlyHeaders,
);
assert("readonly write 403", readonlyWrite.status === 403, `status=${readonlyWrite.status}`);

const wrongFacility = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${UNAUTH_FACILITY}`,
  null,
  managerHeaders,
);
assert("wrong facility 403", wrongFacility.status === 403, `status=${wrongFacility.status}`);

const crossTenant = await liveRequest(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}`,
  null,
  crossTenantHeaders,
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, `status=${crossTenant.status}`);

const runsAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_comparison_runs"));
const resultsAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_comparison_results"));
const driversAfterDeny = Number(db("SELECT COUNT(*) FROM commercial_profitability_comparison_drivers"));
assert("denied requests created no runs", runsAfterDeny === runsBeforeDeny, `${runsBeforeDeny}→${runsAfterDeny}`);
assert("denied requests created no results", resultsAfterDeny === resultsBeforeDeny, `${resultsBeforeDeny}→${resultsAfterDeny}`);
assert("denied requests created no drivers", driversAfterDeny === driversBeforeDeny, `${driversBeforeDeny}→${driversAfterDeny}`);

const provenance = readProvenance();
const evaluateBody = {
  profileId: PROFILE,
  expectedProfileVersion: profileVersion,
  comparisonType: "temporal-contribution-trend",
  materialId: MATERIAL,
  currentWindowStartUtc: CUR_START,
  currentWindowEndUtc: CUR_END,
  referenceWindowStartUtc: REF_START,
  referenceWindowEndUtc: REF_END,
  sourceProvenance: provenance,
  idempotencyKey: `${SEC_PREFIX}-evaluate-${RUN_SUFFIX}`,
};
const firstEvaluate = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`,
  evaluateBody,
  managerHeaders,
);
assert("authorized evaluate 201", firstEvaluate.status === 201, firstEvaluate.text?.slice(0, 200));
forbidText(firstEvaluate.text ?? "", "Npgsql", "evaluate response");
forbidText(firstEvaluate.text ?? "", "password", "evaluate response");

const replayEvaluate = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`,
  evaluateBody,
  managerHeaders,
);
assert("identical idempotent replay 200", replayEvaluate.status === 200);
assert("identical replay flag", replayEvaluate.json?.idempotentReplay === true);
assert("identical replay same id", replayEvaluate.json?.id === firstEvaluate.json?.id);

const conflictEvaluate = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`,
  { ...evaluateBody, currentWindowEndUtc: "2026-12-30T23:59:59.000Z" },
  managerHeaders,
);
assert("different payload conflict 409", conflictEvaluate.status === 409, `status=${conflictEvaluate.status}`);
forbidText(conflictEvaluate.text ?? "", "Npgsql", "conflict response");
forbidText(conflictEvaluate.text ?? "", "local_dev_only", "conflict response");

const staleEvaluate = await liveRequest(
  "POST",
  `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`,
  { ...evaluateBody, expectedProfileVersion: 999, idempotencyKey: `${SEC_PREFIX}-stale-${RUN_SUFFIX}` },
  managerHeaders,
);
assert("stale profile version 409", staleEvaluate.status === 409, `status=${staleEvaluate.status}`);

const concurrentKey = `${SEC_PREFIX}-concurrent-${RUN_SUFFIX}`;
const concurrentPayload = {
  ...evaluateBody,
  idempotencyKey: concurrentKey,
  currentWindowStartUtc: "2026-07-02T00:00:00.000Z",
  currentWindowEndUtc: "2026-12-30T23:59:59.000Z",
};
const [concurrentA, concurrentB] = await Promise.all([
  liveRequest("POST", `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`, concurrentPayload, managerHeaders),
  liveRequest("POST", `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`, concurrentPayload, managerHeaders),
]);
const concurrentStatuses = [concurrentA.status, concurrentB.status].sort();
assert("concurrent replay statuses", concurrentStatuses[0] === 200 || concurrentStatuses[0] === 201, JSON.stringify(concurrentStatuses));
const concurrentCount = Number(db(`SELECT COUNT(*) FROM commercial_profitability_comparison_runs WHERE idempotency_key = '${concurrentKey}'`));
assert("concurrent replay one run", concurrentCount === 1, `count=${concurrentCount}`);
const concurrentRunId = db(`SELECT id::text FROM commercial_profitability_comparison_runs WHERE idempotency_key = '${concurrentKey}'`);
const concurrentResponseIds = [concurrentA.json?.id, concurrentB.json?.id].filter(Boolean);
assert("concurrent same run id", concurrentResponseIds.length === 2 && concurrentResponseIds[0] === concurrentResponseIds[1], JSON.stringify(concurrentResponseIds));
assert("concurrent run id matches db", concurrentResponseIds[0] === concurrentRunId, `${concurrentResponseIds[0]} vs ${concurrentRunId}`);

removeClosureFixtures();
const closureRuns = Number(db(`
  SELECT COUNT(*) FROM commercial_profitability_comparison_runs
  WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%' OR idempotency_key LIKE '${SEC_PREFIX}%'
`));
assert("SEC and CLOSURE runs removed", closureRuns === 0, `count=${closureRuns}`);

console.log("COMMERCIAL-PROFITABILITY-2 security/idempotency verification passed.");

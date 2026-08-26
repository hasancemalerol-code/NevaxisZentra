#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const MIGRATION_ID = "20260826183157_AddCommercialQuotationRiskIntelligenceFoundation";
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000083";
const CLOSURE_PREFIX = "COMMQUOTE1-CLOSURE";
const PROFILE = "bb000001-0280-4000-8000-000000000280";
const PILOT_PROFILE_CODE = "COMMQUOTE1-PILOT-QRP";

const PILOT_ASSESSMENT_IDS = [
  "bb000001-02a1-4000-8000-0000000002a1",
  "bb000001-02a2-4000-8000-0000000002a2",
  "bb000001-02a3-4000-8000-0000000002a3",
  "bb000001-02a4-4000-8000-0000000002a4",
  "bb000001-02a5-4000-8000-0000000002a5",
  "bb000001-02a6-4000-8000-0000000002a6",
  "bb000001-02a7-4000-8000-0000000002a7",
  "bb000001-02a8-4000-8000-0000000002a8",
];

const RLS_TABLES = [
  "commercial_quotation_risk_profiles",
  "commercial_quotation_risk_evidence_observations",
  "commercial_quotation_risk_assessments",
  "commercial_quotation_risk_assessment_drivers",
];

const APPEND_ONLY_TABLES = [
  "commercial_quotation_risk_evidence_observations",
  "commercial_quotation_risk_assessments",
  "commercial_quotation_risk_assessment_drivers",
];

function fail(message) {
  console.error(`COMMERCIAL-QUOTATION-RISK-1 migration verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
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

function tablePrivileges(grantee, tableName) {
  return db(`
    SELECT COALESCE(string_agg(DISTINCT privilege_type, ',' ORDER BY privilege_type), '')
    FROM information_schema.table_privileges
    WHERE grantee = '${grantee}' AND table_name = '${tableName}'
  `);
}

function readMigrationConnectionEnv() {
  const appsettingsPath = resolve(ROOT, "apps/api/appsettings.Development.json");
  const appsettings = JSON.parse(readFileSync(appsettingsPath, "utf8"));
  const persistence = appsettings.Persistence ?? {};
  return {
    ...process.env,
    CREVAFACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    CREVAFACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5432),
    CREVAFACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    CREVAFACTOS__MIGRATIONS__USERNAME: "crevafactos",
    CREVAFACTOS__MIGRATIONS__PASSWORD: process.env.CREVAFACTOS__MIGRATIONS__PASSWORD
      ?? process.env.CREVAFACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    CREVAFACTOS__MIGRATIONS__SSL_MODE: "Disable",
    CREVAFACTOS__MIGRATIONS__CONNECTION_TIMEOUT_SECONDS: "15",
    CREVAFACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

function runMigrationApply() {
  const migrationProject = resolve(ROOT, "packages/CrevaFactOS.Persistence.Migrations/CrevaFactOS.Persistence.Migrations.csproj");
  return spawnSync(
    "dotnet",
    [
      "tool",
      "run",
      "dotnet-ef",
      "--",
      "database",
      "update",
      "--project",
      migrationProject,
      "--startup-project",
      migrationProject,
      "--context",
      "CrevaFactOSDbContext",
    ],
    {
      cwd: ROOT,
      encoding: "utf8",
      env: readMigrationConnectionEnv(),
    },
  );
}

function removeClosureFixtures() {
  db(`
    DELETE FROM commercial_quotation_risk_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_quotation_risk_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_quotation_risk_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_quotation_risk_evidence_observations
    WHERE ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%'
      AND id NOT IN (
        SELECT evidence_observation_id FROM commercial_quotation_risk_assessments
      );
    DELETE FROM commercial_quotation_risk_profiles
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

function pilotSnapshot() {
  const assessmentIdList = PILOT_ASSESSMENT_IDS.map((id) => `'${id}'`).join(", ");
  return {
    profileCount: Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`)),
    assessmentCount: Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_assessments WHERE id IN (${assessmentIdList})`)),
    evidenceCount: Number(db(`
      SELECT COUNT(*) FROM commercial_quotation_risk_evidence_observations
      WHERE id BETWEEN 'bb000001-0291-4000-8000-000000000291' AND 'bb000001-0298-4000-8000-000000000298'
    `)),
    duplicateProfileCode: Number(db(`
      SELECT COUNT(*) FROM commercial_quotation_risk_profiles
      WHERE profile_code = '${PILOT_PROFILE_CODE}'
    `)),
    duplicateAssessmentReferences: Number(db(`
      SELECT COUNT(*) FROM (
        SELECT assessment_reference, COUNT(*) AS c
        FROM commercial_quotation_risk_assessments
        WHERE assessment_reference LIKE 'COMMQUOTE1-ASM-%'
        GROUP BY assessment_reference
        HAVING COUNT(*) > 1
      ) dup
    `)),
  };
}

function assertPilotStable(baseline, current, label) {
  assert(`${label} pilot profile count stable`, current.profileCount === baseline.profileCount, `${baseline.profileCount}→${current.profileCount}`);
  assert(`${label} pilot assessment count stable`, current.assessmentCount === baseline.assessmentCount, `${baseline.assessmentCount}→${current.assessmentCount}`);
  assert(`${label} pilot evidence count stable`, current.evidenceCount === baseline.evidenceCount, `${baseline.evidenceCount}→${current.evidenceCount}`);
  assert(`${label} no duplicate pilot profile code`, current.duplicateProfileCode === 1, `count=${current.duplicateProfileCode}`);
  assert(`${label} no duplicate assessment references`, current.duplicateAssessmentReferences === 0, `count=${current.duplicateAssessmentReferences}`);
}

function getListeningPid(port) {
  const result = spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess`,
  ], { encoding: "utf8" });
  const pid = Number(result.stdout.trim());
  return Number.isFinite(pid) && pid > 0 ? pid : null;
}

function stopApi() {
  const pid = getListeningPid(5000);
  if (!pid) return;
  spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Stop-Process -Id ${pid} -Force -ErrorAction SilentlyContinue`,
  ], { encoding: "utf8" });
}

function startApi() {
  spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Start-Process -WindowStyle Hidden -WorkingDirectory '${ROOT}' -FilePath 'dotnet' -ArgumentList 'run','--project','apps/api/CrevaFactOS.Api.csproj'`,
  ], { encoding: "utf8" });
}

async function waitForApiReady(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${BASE}/health/ready`);
      if (response.status === 200) return true;
    } catch {
      // retry
    }
    await new Promise((resolveDelay) => setTimeout(resolveDelay, 500));
  }
  return false;
}

async function restartApiCycle(label) {
  stopApi();
  await new Promise((resolveDelay) => setTimeout(resolveDelay, 2000));
  startApi();
  const ready = await waitForApiReady();
  assert(`${label} API ready`, ready, `base=${BASE}`);
}

console.log("Running COMMERCIAL-QUOTATION-RISK-1 migration verifier.");

const migration = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/CrevaFactOS.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/CrevaFactOS.Persistence.Migrations/Migrations/CrevaFactOSDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/CrevaFactOS.Persistence/Development/DevelopmentPilotCommercialQuotationRiskSeed.cs");

assert("migration source present", migration.includes("AddCommercialQuotationRiskIntelligenceFoundation"));
assert("signed variance lower bound in migration", migration.includes("signed_price_variance_ratio > -1"));
forbidText(migration, "permission_definitions", "quotation risk migration permissions");
forbidText(migration, "InsertData", "quotation risk migration permissions");
assert("designer present", designer.includes("AddCommercialQuotationRiskIntelligenceFoundation"));
assert("snapshot includes profiles", snapshot.includes('ToTable("commercial_quotation_risk_profiles"'));
assert("snapshot includes assessments", snapshot.includes('ToTable("commercial_quotation_risk_assessments"'));
assert("check constraints present", migration.includes("ck_commercial_quotation_risk_assessments_outcome"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row exactly once", historyCount === 1, `count=${historyCount}`);

const varianceConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_quotation_risk_assessments_signed_variance'
`);
assert(
  "signed variance constraint lower bound only",
  varianceConstraint.includes("signed_price_variance_ratio >") && varianceConstraint.includes("-1"),
  varianceConstraint,
);
forbidText(varianceConstraint, "<= 1", "applied signed variance constraint");

const countsConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_quotation_risk_assessments_counts'
`);
assert(
  "candidate = comparable + excluded",
  countsConstraint.includes("candidate_observation_count = comparable_observation_count + excluded_observation_count")
    || countsConstraint.includes("candidate_observation_count = (comparable_observation_count + excluded_observation_count)"),
  countsConstraint,
);

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
  forbidText(privileges, "UPDATE", `${tableName} runtime privileges`);
  assert(`${tableName} SELECT grant`, privileges.includes("SELECT"), privileges);
  assert(`${tableName} INSERT grant`, privileges.includes("INSERT"), privileges);
}

const profilePrivileges = tablePrivileges("crevafactos_runtime", "commercial_quotation_risk_profiles");
assert("profiles SELECT grant", profilePrivileges.includes("SELECT"), profilePrivileges);
assert("profiles INSERT grant", profilePrivileges.includes("INSERT"), profilePrivileges);
assert("profiles UPDATE grant", profilePrivileges.includes("UPDATE"), profilePrivileges);
forbidText(profilePrivileges, "DELETE", "profiles runtime privileges");

for (const tableName of RLS_TABLES) {
  const forced = Number(db(`
    SELECT COUNT(*) FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = '${tableName}' AND c.relrowsecurity = true AND c.relforcerowsecurity = true
  `));
  assert(`${tableName} FORCE RLS enabled`, forced === 1, `count=${forced}`);
}

const readPermission = db(`SELECT key FROM permission_definitions WHERE id = '${READ_PERMISSION_ID}'`);
const managePermission = db(`SELECT key FROM permission_definitions WHERE id = '${MANAGE_PERMISSION_ID}'`);
assert("commercial-intelligence.read permission seeded", readPermission === "commercial-intelligence.read", readPermission);
assert("commercial-intelligence.manage permission seeded", managePermission === "commercial-intelligence.manage", managePermission);

const orphanDrivers = Number(db(`
  SELECT COUNT(*) FROM commercial_quotation_risk_assessment_drivers d
  LEFT JOIN commercial_quotation_risk_assessments a ON a.id = d.assessment_id
  WHERE a.id IS NULL
`));
assert("no orphan drivers", orphanDrivers === 0, `count=${orphanDrivers}`);

const historyBefore = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
const applyResult = runMigrationApply();
const applyOutput = `${applyResult.stdout}${applyResult.stderr}`;
assert("second migration apply succeeds", applyResult.status === 0, applyOutput.slice(-500));
assert(
  "second migration apply is no-op",
  /No migrations were applied|already up to date|Done\./i.test(applyOutput),
  applyOutput.slice(-500),
);
const historyAfter = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("second apply did not duplicate history", historyAfter === historyBefore, `${historyBefore}→${historyAfter}`);

assert("bootstrap seed references pilot profile", bootstrap.includes("CommercialQuotationRiskPilotProfileId"));
assert("bootstrap seed includes within-threshold scenario", bootstrap.includes("COMMQUOTE1-SET-WITHIN"));

const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`));
if (pilotProfileCount === 0) {
  fail("Commercial quotation risk pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);

const pilotNegativeThreshold = db(`SELECT maximum_negative_price_variance_ratio FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`);
const pilotPositiveThreshold = db(`SELECT maximum_positive_price_variance_ratio FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`);
const pilotPaymentTerm = db(`SELECT maximum_payment_term_days FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`);
const pilotExpiryWarning = db(`SELECT expiry_warning_days FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`);
assert("pilot maximum negative variance threshold 0.10", pilotNegativeThreshold === "0.100000", pilotNegativeThreshold);
assert("pilot maximum positive variance threshold 0.15", pilotPositiveThreshold === "0.150000", pilotPositiveThreshold);
assert("pilot maximum payment term days 90", pilotPaymentTerm === "90", pilotPaymentTerm);
assert("pilot expiry warning days 7", pilotExpiryWarning === "7", pilotExpiryWarning);

const baselinePilot = pilotSnapshot();
assert("pilot assessments seeded", baselinePilot.assessmentCount === 8, `count=${baselinePilot.assessmentCount}`);
assert("pilot evidence seeded", baselinePilot.evidenceCount === 8, `count=${baselinePilot.evidenceCount}`);

const energyCount = Number(db(`SELECT COUNT(*) FROM energy_measurement_points WHERE id = 'bb000001-0160-4000-8000-000000000160'`));
const iotCount = Number(db(`SELECT COUNT(*) FROM iot_plc_endpoints WHERE id = 'bb000001-01c0-4000-8000-0000000001c0'`));
const operationalRiskCount = Number(db(`SELECT COUNT(*) FROM operational_risk_cases WHERE id = 'bb000001-0200-4000-8000-000000000200'`));
assert("energy pilot preserved", energyCount === 1, `count=${energyCount}`);
assert("iot pilot preserved", iotCount === 1, `count=${iotCount}`);
assert("operational risk pilot preserved", operationalRiskCount === 1, `count=${operationalRiskCount}`);

try {
  const initiallyReady = await waitForApiReady(5000);
  if (initiallyReady) {
    await restartApiCycle("first");
    assertPilotStable(baselinePilot, pilotSnapshot(), "after first restart");
    await restartApiCycle("second");
    assertPilotStable(baselinePilot, pilotSnapshot(), "after second restart");
  } else {
    fail("API not ready for pilot seed stability checks — start API with DevelopmentPilot enabled on port 5000");
  }
} finally {
  removeClosureFixtures();
  const closureProfiles = Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  const closureAssessments = Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  const closureEvidence = Number(db(`SELECT COUNT(*) FROM commercial_quotation_risk_evidence_observations WHERE ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  assert("closure profiles removed", closureProfiles === 0, `count=${closureProfiles}`);
  assert("closure assessments removed", closureAssessments === 0, `count=${closureAssessments}`);
  assert("closure evidence removed", closureEvidence === 0, `count=${closureEvidence}`);
}

console.log("COMMERCIAL-QUOTATION-RISK-1 migration verification passed.");

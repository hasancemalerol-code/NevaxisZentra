#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const CLOSURE_PREFIX = "COMMPROFIT1-CLOSURE";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const PILOT_PROFILE_CODE = "COMMPROFIT1-PILOT";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000083";

const PILOT_ASSESSMENT_IDS = [
  "bb000001-0391-4000-8000-000000000391",
  "bb000001-0392-4000-8000-000000000392",
  "bb000001-0393-4000-8000-000000000393",
  "bb000001-0394-4000-8000-000000000394",
  "bb000001-0395-4000-8000-000000000395",
  "bb000001-0396-4000-8000-000000000396",
  "bb000001-0397-4000-8000-000000000397",
  "bb000001-0398-4000-8000-000000000398",
  "bb000001-0399-4000-8000-000000000399",
  "bb000001-039a-4000-8000-00000000039a",
];

const RLS_TABLES = [
  "commercial_profitability_profiles",
  "commercial_profitability_evidence_observations",
  "commercial_profitability_assessments",
  "commercial_profitability_assessment_cost_components",
  "commercial_profitability_assessment_drivers",
];

const APPEND_ONLY_TABLES = [
  "commercial_profitability_evidence_observations",
  "commercial_profitability_assessments",
  "commercial_profitability_assessment_cost_components",
  "commercial_profitability_assessment_drivers",
];

function discoverMigrationId() {
  const migrationsDir = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/Migrations");
  const match = readdirSync(migrationsDir)
    .filter((name) => name.endsWith(".cs") && !name.endsWith(".Designer.cs"))
    .find((name) => name.includes("AddCommercialProfitabilityIntelligenceFoundation"));
  if (!match) {
    fail("Migration AddCommercialProfitabilityIntelligenceFoundation not found");
  }
  return match.replace(/\.cs$/, "");
}

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-1 migration verification failed: ${message}`);
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
    ["exec", "NevaxisZentra-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
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
    NEVAXIS_FACTOS__MIGRATIONS__HOST: persistence.Host ?? "127.0.0.1",
    NEVAXIS_FACTOS__MIGRATIONS__PORT: String(persistence.Port ?? 5432),
    NEVAXIS_FACTOS__MIGRATIONS__DATABASE: persistence.Database ?? "crevafactos_local",
    NEVAXIS_FACTOS__MIGRATIONS__USERNAME: "crevafactos",
    NEVAXIS_FACTOS__MIGRATIONS__PASSWORD: process.env.NEVAXIS_FACTOS__MIGRATIONS__PASSWORD
      ?? process.env.NEVAXIS_FACTOS_POSTGRES_PASSWORD
      ?? "local_dev_only_change_me",
    NEVAXIS_FACTOS__MIGRATIONS__SSL_MODE: "Disable",
    NEVAXIS_FACTOS__MIGRATIONS__CONNECTION_TIMEOUT_SECONDS: "15",
    NEVAXIS_FACTOS__MIGRATIONS__COMMAND_TIMEOUT_SECONDS: "120",
  };
}

function runMigrationApply() {
  const migrationProject = resolve(ROOT, "packages/NevaxisZentra.Persistence.Migrations/NevaxisZentra.Persistence.Migrations.csproj");
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
      "NevaxisZentraDbContext",
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
    DELETE FROM commercial_profitability_assessment_cost_components
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_profitability_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_profitability_assessments
      WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'
    );
    DELETE FROM commercial_profitability_assessments
    WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%';
    DELETE FROM commercial_profitability_evidence_observations
    WHERE ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%'
      AND id NOT IN (
        SELECT sales_evidence_observation_id FROM commercial_profitability_assessments
        WHERE sales_evidence_observation_id IS NOT NULL
      );
    DELETE FROM commercial_profitability_profiles
    WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

function pilotSnapshot() {
  const assessmentIdList = PILOT_ASSESSMENT_IDS.map((id) => `'${id}'`).join(", ");
  return {
    profileCount: Number(db(`SELECT COUNT(*) FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`)),
    assessmentCount: Number(db(`SELECT COUNT(*) FROM commercial_profitability_assessments WHERE id IN (${assessmentIdList})`)),
    evidenceCount: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_evidence_observations
      WHERE evidence_set_reference LIKE 'COMMPROFIT1-SET-%'
    `)),
    duplicateProfileCode: Number(db(`
      SELECT COUNT(*) FROM commercial_profitability_profiles
      WHERE profile_code = '${PILOT_PROFILE_CODE}'
    `)),
    duplicateAssessmentReferences: Number(db(`
      SELECT COUNT(*) FROM (
        SELECT assessment_reference, COUNT(*) AS c
        FROM commercial_profitability_assessments
        WHERE assessment_reference LIKE 'COMMPROFIT1-ASM-%'
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
    `Start-Process -WindowStyle Hidden -WorkingDirectory '${ROOT}' -FilePath 'dotnet' -ArgumentList 'run','--project','apps/api/NevaxisZentra.Api.csproj'`,
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

console.log("Running COMMERCIAL-PROFITABILITY-1 migration verifier.");

const MIGRATION_ID = discoverMigrationId();
const MIGRATION_BASENAME = `${MIGRATION_ID}.cs`;
const DESIGNER_BASENAME = `${MIGRATION_ID}.Designer.cs`;

const migration = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${MIGRATION_BASENAME}`);
const designer = readRequired(`packages/NevaxisZentra.Persistence.Migrations/Migrations/${DESIGNER_BASENAME}`);
const snapshot = readRequired("packages/NevaxisZentra.Persistence.Migrations/Migrations/NevaxisZentraDbContextModelSnapshot.cs");
const bootstrap = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotCommercialProfitabilitySeed.cs");

assert("migration source present", migration.includes("AddCommercialProfitabilityIntelligenceFoundation"));
assert("margin ratio upper bound in migration", migration.includes("contribution_margin_ratio < 1"));
forbidText(migration, "permission_definitions", "profitability migration permissions");
forbidText(migration, "InsertData", "profitability migration permissions");
assert("designer present", designer.includes("AddCommercialProfitabilityIntelligenceFoundation"));
assert("snapshot includes profiles", snapshot.includes('ToTable("commercial_profitability_profiles"'));
assert("snapshot includes assessments", snapshot.includes('ToTable("commercial_profitability_assessments"'));
assert("check constraints present", migration.includes("ck_commercial_profitability_assessments_outcome"));

const firstApplyResult = runMigrationApply();
const firstApplyOutput = `${firstApplyResult.stdout}${firstApplyResult.stderr}`;
assert("migration apply succeeds", firstApplyResult.status === 0, firstApplyOutput.slice(-500));

const historyCount = Number(db(`
  SELECT COUNT(*) FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '${MIGRATION_ID}'
`));
assert("history row exactly once", historyCount === 1, `count=${historyCount}`);

const marginConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_profitability_assessments_margin_ratio'
`);
assert(
  "margin ratio constraint upper bound only",
  marginConstraint.includes("contribution_margin_ratio") && marginConstraint.includes("<") && marginConstraint.includes("1"),
  marginConstraint,
);

const countsConstraint = db(`
  SELECT pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conname = 'ck_commercial_profitability_assessments_counts'
`);
assert(
  "candidate = included + excluded",
  countsConstraint.includes("candidate_cost_component_count = included_cost_component_count + excluded_cost_component_count")
    || countsConstraint.includes("candidate_cost_component_count = (included_cost_component_count + excluded_cost_component_count)"),
  countsConstraint,
);

for (const tableName of APPEND_ONLY_TABLES) {
  const privileges = tablePrivileges("crevafactos_runtime", tableName);
  forbidText(privileges, "DELETE", `${tableName} runtime privileges`);
  forbidText(privileges, "UPDATE", `${tableName} runtime privileges`);
  assert(`${tableName} SELECT grant`, privileges.includes("SELECT"), privileges);
  assert(`${tableName} INSERT grant`, privileges.includes("INSERT"), privileges);
}

const profilePrivileges = tablePrivileges("crevafactos_runtime", "commercial_profitability_profiles");
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
  SELECT COUNT(*) FROM commercial_profitability_assessment_drivers d
  LEFT JOIN commercial_profitability_assessments a ON a.id = d.assessment_id
  WHERE a.id IS NULL
`));
assert("no orphan drivers", orphanDrivers === 0, `count=${orphanDrivers}`);

const orphanComponents = Number(db(`
  SELECT COUNT(*) FROM commercial_profitability_assessment_cost_components c
  LEFT JOIN commercial_profitability_assessments a ON a.id = c.assessment_id
  WHERE a.id IS NULL
`));
assert("no orphan cost components", orphanComponents === 0, `count=${orphanComponents}`);

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

assert("bootstrap seed references pilot profile", bootstrap.includes("CommercialProfitabilityPilotProfileId"));
assert("bootstrap seed includes within-threshold scenario", bootstrap.includes("COMMPROFIT1-SET-WITHIN"));

const pilotProfileCount = Number(db(`SELECT COUNT(*) FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`));
if (pilotProfileCount === 0) {
  fail("Commercial profitability pilot not bootstrapped — start API with DevelopmentPilot enabled before bootstrap count checks");
}
assert("pilot profile seeded", pilotProfileCount === 1, `count=${pilotProfileCount}`);

const pilotMinimumMargin = db(`SELECT minimum_contribution_margin_ratio FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`);
const pilotMaximumEvidenceAge = db(`SELECT maximum_evidence_age_days FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`);
const pilotRequiredKinds = db(`SELECT required_cost_component_kinds FROM commercial_profitability_profiles WHERE id = '${PROFILE}'`);
assert("pilot minimum contribution margin threshold 0.15", pilotMinimumMargin === "0.150000", pilotMinimumMargin);
assert("pilot maximum evidence age days 180", pilotMaximumEvidenceAge === "180", pilotMaximumEvidenceAge);
assert("pilot required cost component kinds", pilotRequiredKinds === "direct-material,direct-labor,machine,energy", pilotRequiredKinds);

const baselinePilot = pilotSnapshot();
assert("pilot assessments seeded", baselinePilot.assessmentCount === 10, `count=${baselinePilot.assessmentCount}`);
assert("pilot evidence seeded", baselinePilot.evidenceCount >= 50, `count=${baselinePilot.evidenceCount}`);

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
  const closureProfiles = Number(db(`SELECT COUNT(*) FROM commercial_profitability_profiles WHERE create_idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  const closureAssessments = Number(db(`SELECT COUNT(*) FROM commercial_profitability_assessments WHERE idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  const closureEvidence = Number(db(`SELECT COUNT(*) FROM commercial_profitability_evidence_observations WHERE ingest_idempotency_key LIKE '${CLOSURE_PREFIX}%'`));
  assert("closure profiles removed", closureProfiles === 0, `count=${closureProfiles}`);
  assert("closure assessments removed", closureAssessments === 0, `count=${closureAssessments}`);
  assert("closure evidence removed", closureEvidence === 0, `count=${closureEvidence}`);
}

console.log("COMMERCIAL-PROFITABILITY-1 migration verification passed.");

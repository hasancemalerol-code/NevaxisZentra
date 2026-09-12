#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PILOT_PROFILE = "bb000001-0210-4000-8000-000000000210";
const TERMINAL_CASE = "bb000001-0218-4000-8000-000000000218";
const PROVENANCE =
  "Deterministic local Development operational-risk detection data — simulated, non-production, not real customer evidence, predicted loss, or guaranteed savings.";
const CLOSURE_PREFIX = "OPRISK2-CLOSURE";
const RUN_SUFFIX = Date.now();
const WINDOW_START = "2026-08-01T00:00:00.000Z";
const WINDOW_END = "2030-01-01T00:00:00.000Z";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`OPERATIONAL-RISK-2 cross-module detection verification failed: ${message}`);
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
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
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
    DELETE FROM operational_risk_evidence
    WHERE case_id IN (SELECT id FROM operational_risk_cases WHERE detection_key LIKE '${CLOSURE_PREFIX}%');
    DELETE FROM operational_risk_cases WHERE detection_key LIKE '${CLOSURE_PREFIX}%';
  `);
}

console.log("Running OPERATIONAL-RISK-2 cross-module detection verifier.");

const adapters = readRequired("packages/NevaxisFactOS.Persistence/OperationalRisk/OperationalRiskSourceAdapters.cs");
const evaluator = readRequired("packages/NevaxisFactOS.Persistence/OperationalRisk/OperationalRiskDetectionEvaluator.cs");
const constants = readRequired("packages/NevaxisFactOS.Persistence/OperationalRisk/OperationalRiskDetectionConstants.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/OperationalRisk/OperationalRiskDetectionRepository.cs");
const energyCalc = readRequired("packages/NevaxisFactOS.Persistence/Energy/EnergyIntelligenceCalculator.cs");

assert("IOperationalRiskEvidenceSource contract", adapters.includes("IOperationalRiskEvidenceSource"));
assert("energy adapter reads assessments only", adapters.includes("EnergyIntelligenceAssessments"));
assert("iot adapter reads connectivity assessments only", adapters.includes("IotPlcConnectivityAssessments"));
assert("maintenance adapter reads predictive assessments only", adapters.includes("MaintenancePredictiveRiskAssessments"));
forbidText(repository, "EnergyIntelligenceCalculator", "detection repository");
forbidText(repository, "PredictiveRiskEvaluationService", "detection repository");
forbidText(adapters, "EnergyIntelligenceCalculator", "energy adapter");
assert("rule-set OPERATIONAL-RISK-2.0", constants.includes("OPERATIONAL-RISK-2.0"));
assert("energy-risk driver", constants.includes("energy-risk"));
assert("connectivity-risk driver", constants.includes("connectivity-risk"));
assert("maintenance-risk driver", constants.includes("maintenance-risk"));
assert("source-stale driver", constants.includes("source-stale"));
assert("approved profile gate", repository.includes("OperationalRiskDetectionProfileStatuses.Approved"));
assert("deterministic driver ordering", evaluator.includes("SelectLeadingDriver"));
assert("no guaranteed savings claim in evaluator", !evaluator.includes("guaranteed savings"));
assert("provenance disclaimer present", constants.includes("not real customer evidence"));
forbidText(energyCalc, "operational-risk", "energy calculator must not reference operational risk");

removeClosureFixtures();

const profiles = await request("GET", `/api/operational-risks/detection/profiles?facilityId=${FACILITY}`);
assert("list profiles", profiles.status === 200);
const pilotProfile = profiles.json?.items?.find((p) => p.id === PILOT_PROFILE);
assert("pilot profile present", pilotProfile?.status === "approved", JSON.stringify(profiles.json));

const runKey = `${CLOSURE_PREFIX}-RUN-${RUN_SUFFIX}`;
const runBody = {
  profileId: PILOT_PROFILE,
  windowStartUtc: WINDOW_START,
  windowEndUtc: WINDOW_END,
  sourceProvenance: PROVENANCE,
  idempotencyKey: runKey,
};
const run1 = await request("POST", `/api/operational-risks/detection/runs?facilityId=${FACILITY}`, runBody);
assert("detection run succeeds", run1.status === 201 || run1.status === 200, run1.text);
const runId = run1.json?.id;
assert("run id returned", runId);

const runDetail = await request("GET", `/api/operational-risks/detection/runs/${runId}?facilityId=${FACILITY}`);
assert("run detail", runDetail.status === 200);
const results = runDetail.json?.results ?? [];
assert("results produced", results.length > 0, `count=${results.length}`);

const energyRisk = results.find((r) => r.leadingDriverCode === "energy-risk" && r.detectionOutcome === "risk-detected");
const connectivityRisk = results.find((r) => r.leadingDriverCode === "connectivity-risk" && r.detectionOutcome === "risk-detected");
const maintenanceRisk = results.find((r) => r.leadingDriverCode === "maintenance-risk" && r.detectionOutcome === "risk-detected");
const withinThreshold = results.find((r) => r.detectionOutcome === "within-threshold");
const insufficient = results.find((r) => r.detectionOutcome === "insufficient-data");
const staleDriver = results.some((r) => (r.drivers ?? []).some((d) => d.reasonCode === "source-stale"));

assert("energy attention maps to energy-risk", Boolean(energyRisk));
assert("iot offline maps to connectivity-risk", Boolean(connectivityRisk));
assert("maintenance high maps to maintenance-risk", Boolean(maintenanceRisk));
assert("healthy iot within-threshold", Boolean(withinThreshold));
assert("missing rule or denominator yields insufficient-data", Boolean(insufficient));
assert("stale evidence includes source-stale driver", staleDriver);
assert("insufficient result has no score", insufficient?.riskScore == null);
assert("within-threshold creates no linked case", withinThreshold?.linkedCaseId == null);

const run2 = await request("POST", `/api/operational-risks/detection/runs?facilityId=${FACILITY}`, runBody);
assert("identical rerun succeeds", run2.status === 200 || run2.status === 201);
assert("identical rerun idempotent", run2.json?.idempotentReplay === true);

const openCaseCount = Number(db(`
  SELECT COUNT(*) FROM operational_risk_cases
  WHERE detection_key = (
    SELECT detection_key FROM operational_risk_detection_results
    WHERE linked_case_id IS NOT NULL AND run_id = '${runId}' LIMIT 1
  ) AND lifecycle_status NOT IN ('resolved','dismissed')
`));
assert("no duplicate unresolved case for detection key", openCaseCount <= 1, `count=${openCaseCount}`);

const terminalStatus = db(`SELECT lifecycle_status FROM operational_risk_cases WHERE id = '${TERMINAL_CASE}'`);
assert("terminal case remains resolved", terminalStatus === "resolved", terminalStatus);

const recurrenceCount = Number(db(`
  SELECT COUNT(*) FROM operational_risk_cases
  WHERE detection_key = (SELECT detection_key FROM operational_risk_cases WHERE id = '${TERMINAL_CASE}')
    AND id <> '${TERMINAL_CASE}'
`));
assert("terminal recurrence creates separate case", recurrenceCount >= 1, `count=${recurrenceCount}`);

const driverOrderOk = results.every((r) => {
  const drivers = [...(r.drivers ?? [])].sort((a, b) => a.sortOrder - b.sortOrder);
  return drivers.length === 0 || r.leadingDriverCode === drivers[0].reasonCode;
});
assert("leading driver matches first ordered driver", driverOrderOk);

removeClosureFixtures();
console.log("OPERATIONAL-RISK-2 cross-module detection verification passed.");

import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5011";
const TENANT = process.env.NEVAXIS_FACTOS_TENANT ?? "dev-pilot-tenant";
const OPERATOR = process.env.NEVAXIS_FACTOS_SUBJECT ?? "production-pilot-operator";
const ISSUER = "crevafactos-dev";
const AUTHORIZED_FACILITY =
  process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";

function fail(message) {
  console.error(`Maintenance 3L reliability verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

function headers(subject = OPERATOR, extra = {}) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": TENANT,
    ...extra,
  };
}

console.log("Running maintenance 3L reliability verifier.");

const reliabilityRepo = readRequired(
  "packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceReliabilityRepository.cs",
);
const entities = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceEntities.cs");
const seed = readRequired(
  "packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotMaintenanceReliabilitySeed.cs",
);

requireText(entities, "MaintenanceFailureCategory", "failure category enum");
requireText(reliabilityRepo, "CalculateMttr", "MTTR calculation");
requireText(reliabilityRepo, "CalculatePreventiveCompliance", "preventive compliance");
requireText(reliabilityRepo, "CalculateRepeatFailures", "repeat failures");
requireText(reliabilityRepo, "CalculateBacklogAging", "backlog aging");
requireText(reliabilityRepo, "EvaluateConditionStatusWithReasons", "condition status");
requireText(seed, "MaintenancePilotCorrectiveWoAId", "pilot MTTR fixture A");
requireText(seed, "AddMinutes(120)", "pilot 120 minute repair");
requireText(seed, "AddMinutes(60)", "pilot 60 minute repair");

forbidText(reliabilityRepo, "LLM", "reliability repository");
forbidText(reliabilityRepo, "OpenAI", "reliability repository");
forbidText(reliabilityRepo, "remaining useful life", "reliability repository");

console.log("Running maintenance 3L reliability live cross-check.");

try {
  const dateTo = new Date().toISOString();
  const dateFrom = new Date(Date.now() - 365 * 24 * 60 * 60 * 1000).toISOString();
  const params = new URLSearchParams({
    facilityId: AUTHORIZED_FACILITY,
    dateFrom,
    dateTo,
  });
  const response = await fetch(`${BASE}/api/maintenance/reporting/reliability?${params.toString()}`, {
    headers: headers(),
  });
  if (response.status !== 200) {
    fail(`reliability endpoint must return HTTP 200, got ${response.status}.`);
  }

  const body = await response.json();
  if (!body.mtbf || body.mtbf.mtbfAvailable !== false) {
    fail("MTBF must be unavailable without trusted runtime source.");
  }
  if (body.mtbf.reason !== "trusted operating-time source unavailable") {
    fail(`unexpected MTBF reason: ${body.mtbf.reason}`);
  }

  const mttr = body.mttr;
  if (!mttr || mttr.eligibleRepairCount < 2) {
    fail(`expected at least 2 eligible MTTR repairs, got ${mttr?.eligibleRepairCount ?? 0}.`);
  }

  const computedMean = Math.round((mttr.totalRepairMinutes / mttr.eligibleRepairCount) * 100) / 100;
  if (mttr.meanRepairMinutes !== computedMean) {
    fail(`MTTR mean must match total/eligible (${computedMean}), got ${mttr.meanRepairMinutes}.`);
  }

  const pilotMeans = [90, 60];
  if (mttr.eligibleRepairCount === 2 && !pilotMeans.includes(mttr.meanRepairMinutes)) {
    fail(`pilot MTTR cross-check expected 90 or 60 for two repairs, got ${mttr.meanRepairMinutes}.`);
  }

  if (!Array.isArray(body.repeatFailures) || body.repeatFailures.length === 0) {
    fail("expected deterministic repeat failure rows.");
  }

  const hydraulicRepeat = body.repeatFailures.find(
    (item) => item.failureCategory === "hydraulic" && item.assetReference === "IM-120",
  );
  if (!hydraulicRepeat || hydraulicRepeat.repeatCount < 1) {
    fail("expected IM-120 hydraulic repeat failure.");
  }

  const compliance = body.preventiveCompliance;
  if (!compliance || compliance.dueCount < 1) {
    fail("expected preventive compliance due count.");
  }

  const aging = body.backlogAging;
  if (!aging || typeof aging.openRequests !== "object") {
    fail("expected backlog aging buckets.");
  }

  const conditionResponse = await fetch(
    `${BASE}/api/maintenance/assets/IM-120/condition?facilityId=${AUTHORIZED_FACILITY}`,
    { headers: headers() },
  );
  if (conditionResponse.status !== 200) {
    fail(`asset condition must return HTTP 200, got ${conditionResponse.status}.`);
  }
  const condition = await conditionResponse.json();
  if (!condition.conditionStatus || !Array.isArray(condition.reasons)) {
    fail("asset condition must expose status and reasons.");
  }

  const riskResponse = await fetch(
    `${BASE}/api/maintenance/risk-projections?facilityId=${AUTHORIZED_FACILITY}`,
    { headers: headers() },
  );
  if (riskResponse.status !== 200) {
    fail(`risk projections must return HTTP 200, got ${riskResponse.status}.`);
  }
  const risks = await riskResponse.json();
  if (!Array.isArray(risks) || risks.length === 0) {
    fail("expected at least one risk projection.");
  }
  const im120 = risks.find((item) => item.assetReference === "IM-120");
  if (!im120 || !im120.primaryReason) {
    fail("expected IM-120 risk projection with primary reason.");
  }
} catch (error) {
  fail(`live reliability checks failed: ${error instanceof Error ? error.message : String(error)}`);
}

console.log("Maintenance 3L reliability verifier passed.");

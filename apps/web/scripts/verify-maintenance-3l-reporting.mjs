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
  console.error(`Maintenance 3L reporting verification failed: ${message}`);
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
    "X-NevaxisZentra-Tenant": TENANT,
    ...extra,
  };
}

async function getJson(path, query = {}) {
  const params = new URLSearchParams({ facilityId: AUTHORIZED_FACILITY, ...query });
  const response = await fetch(`${BASE}${path}?${params.toString()}`, { headers: headers() });
  const body = response.status === 204 ? null : await response.json().catch(() => null);
  return { status: response.status, body };
}

console.log("Running maintenance 3L reporting verifier.");

const reportingEndpoints = readRequired(
  "apps/api/Maintenance/MaintenanceReportingEndpointRouteBuilderExtensions.cs",
);
const reliabilityRepo = readRequired(
  "packages/NevaxisZentra.Persistence/Maintenance/MaintenanceReliabilityRepository.cs",
);
const reliabilityModels = readRequired(
  "packages/NevaxisZentra.Persistence/Maintenance/MaintenanceReliabilityModels.cs",
);
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809190000_AddMaintenanceReliabilityFoundation.cs",
);
const mapper = readRequired("apps/api/Maintenance/MaintenanceApiMapper.cs");

requireText(reportingEndpoints, 'MapGet("/overview"', "reporting overview endpoint");
requireText(reportingEndpoints, 'MapGet("/reliability"', "reporting reliability endpoint");
requireText(reportingEndpoints, 'MapGet("/failures"', "reporting failures endpoint");
requireText(reportingEndpoints, 'MapGet("/assets/{assetReference}/reliability"', "asset reliability endpoint");
requireText(reportingEndpoints, 'MapGet("/condition-signals"', "condition signals endpoint");
requireText(reportingEndpoints, 'MapGet("/risk-projections"', "risk projections endpoint");
requireText(reportingEndpoints, "ResolveReadAccessAsync", "reporting auth");
requireText(reportingEndpoints, "UnavailableMaintenanceOperatingTimeSource", "MTBF unavailable adapter");
requireText(reliabilityRepo, "MTTR (eligible completed corrective maintenance)", "MTTR label");
requireText(reliabilityRepo, "trusted operating-time source unavailable", "MTBF unavailable reason");
requireText(reliabilityRepo, "BuildFailureSignature", "failure signature");
requireText(reliabilityRepo, "MaintenanceAssetRiskProjectionReadModel", "risk projection");
requireText(reliabilityModels, "MaintenanceConditionSignalReadModel", "condition signal contract");
requireText(migration, "failure_category", "migration failure category");
requireText(mapper, "MapReliabilityOverview", "mapper overview");

forbidText(reportingEndpoints, "MapPost", "reporting endpoints");
forbidText(reportingEndpoints, "MapDelete", "reporting endpoints");
forbidText(reportingEndpoints, "MapPut", "reporting endpoints");
forbidText(reliabilityRepo, "mqtt", "reliability repository");
forbidText(reliabilityRepo, "opcua", "reliability repository");
forbidText(reliabilityRepo, "predictive maintenance", "reliability repository");
forbidText(reliabilityRepo, "technician", "reliability repository");
forbidText(reliabilityRepo, "EmployeeScore", "reliability repository");

console.log("Running maintenance 3L reporting live checks.");

try {
  const dateTo = new Date().toISOString();
  const dateFrom = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString();

  const overview = await getJson("/api/maintenance/reporting/overview", {
    dateFrom,
    dateTo,
  });
  if (overview.status !== 200) {
    fail(`reporting overview must return HTTP 200, got ${overview.status}.`);
  }

  const reliability = await getJson("/api/maintenance/reporting/reliability", {
    dateFrom,
    dateTo,
  });
  if (reliability.status !== 200) {
    fail(`reporting reliability must return HTTP 200, got ${reliability.status}.`);
  }

  const failures = await getJson("/api/maintenance/reporting/failures", {
    dateFrom,
    dateTo,
    groupBy: "failure-category",
  });
  if (failures.status !== 200) {
    fail(`failure breakdown must return HTTP 200, got ${failures.status}.`);
  }

  const assetReliability = await getJson("/api/maintenance/assets/IM-120/reliability");
  if (assetReliability.status !== 200) {
    fail(`asset reliability must return HTTP 200, got ${assetReliability.status}.`);
  }

  const conditionSignals = await getJson("/api/maintenance/condition-signals");
  if (conditionSignals.status !== 200) {
    fail(`condition signals must return HTTP 200, got ${conditionSignals.status}.`);
  }

  const riskProjections = await getJson("/api/maintenance/risk-projections");
  if (riskProjections.status !== 200) {
    fail(`risk projections must return HTTP 200, got ${riskProjections.status}.`);
  }

  const postOverview = await fetch(`${BASE}/api/maintenance/reporting/overview?facilityId=${AUTHORIZED_FACILITY}`, {
    method: "POST",
    headers: headers(),
  });
  if (postOverview.status !== 405) {
    fail(`reporting POST must return HTTP 405, got ${postOverview.status}.`);
  }

  const deleteOverview = await fetch(`${BASE}/api/maintenance/reporting/overview?facilityId=${AUTHORIZED_FACILITY}`, {
    method: "DELETE",
    headers: headers(),
  });
  if (deleteOverview.status !== 405) {
    fail(`reporting DELETE must return HTTP 405, got ${deleteOverview.status}.`);
  }
} catch (error) {
  fail(`live reporting checks failed: ${error instanceof Error ? error.message : String(error)}`);
}

console.log("Maintenance 3L reporting verifier passed.");

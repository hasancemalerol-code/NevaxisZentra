import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 3L reporting verification failed: ${message}`);
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

console.log("Running quality 3L reporting verifier.");

const endpoints = readRequired("apps/api/Quality/QualityReportingEndpointRouteBuilderExtensions.cs");
const reportingRepo = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityReportingRepository.cs");
const domain = readRequired("apps/web/src/features/quality-control/domain/quality-control.ts");
const contracts = readRequired("apps/web/src/features/quality-control/server/quality-control-api-contracts.ts");
const client = readRequired("apps/web/src/features/quality-control/server/quality-control-api-client.ts");
const reader = readRequired("apps/web/src/features/quality-control/server/quality-control-reader.ts");
const reportingPage = readRequired("apps/web/src/app/(app)/kalite/raporlama/page.tsx");
const mainPage = readRequired("apps/web/src/app/(app)/kalite/page.tsx");

requireText(endpoints, "/reporting/overview", "reporting endpoints");
requireText(endpoints, "/reporting/inspections", "reporting endpoints");
requireText(endpoints, "/reporting/ncrs", "reporting endpoints");
requireText(endpoints, "/reporting/holds", "reporting endpoints");
requireText(endpoints, "/reporting/corrective-actions", "reporting endpoints");
requireText(endpoints, "/reporting/trends", "reporting endpoints");
requireText(endpoints, "/reporting/failures", "reporting endpoints");
requireText(endpoints, "ResolveReadAccessAsync", "reporting endpoints");
forbidText(endpoints, "MapPost", "reporting endpoints");
forbidText(endpoints, "MapPut", "reporting endpoints");
forbidText(endpoints, "MapDelete", "reporting endpoints");

requireText(reportingRepo, "ComputeFirstPassYieldAsync", "QualityReportingRepository");
requireText(reportingRepo, "GetTrendsAsync", "QualityReportingRepository");
requireText(reportingRepo, "GetFailureBreakdownAsync", "QualityReportingRepository");

requireText(domain, "QualityReportingOverview", "quality-control domain");
requireText(domain, "QualityTrendPeriod", "quality-control domain");
requireText(domain, "QualityFailureBreakdownItem", "quality-control domain");
requireText(domain, "firstPassYieldPercent", "quality-control domain");
requireText(domain, "passRatePercent", "quality-control domain");
requireText(domain, "releaseRatePercent", "quality-control domain");

requireText(contracts, "parseQualityReportingOverviewApiResponse", "api contracts");
requireText(contracts, "parseQualityTrendListApiResponse", "api contracts");
requireText(contracts, "parseQualityFailureBreakdownApiResponse", "api contracts");
requireText(contracts, "parseQualityNcrAnalyticsApiResponse", "api contracts");
requireText(contracts, "parseQualityHoldAnalyticsApiResponse", "api contracts");
requireText(contracts, "parseQualityCorrectiveActionAnalyticsApiResponse", "api contracts");

requireText(client, "getQualityReportingOverview", "api client");
requireText(client, "listQualityReportingTrends", "api client");
requireText(client, "listQualityFailureBreakdown", "api client");
requireText(client, "/api/quality/reporting/overview", "api client");
requireText(client, "/api/quality/reporting/trends", "api client");
requireText(client, "/api/quality/reporting/failures", "api client");
const reportingClientSection = client.slice(client.indexOf("export type QualityReportingFilters"));
forbidText(reportingClientSection, "postJson(", "api client reporting section");
forbidText(reportingClientSection, "putJson(", "api client reporting section");
forbidText(reportingClientSection, 'method: "DELETE"', "api client reporting section");

requireText(reader, "readQualityReportingOverview", "quality-control reader");
requireText(reader, "readQualityReportingPage", "quality-control reader");

requireText(reportingPage, "production-execution-page", "reporting page");
requireText(reportingPage, "Kalite Raporları", "reporting page");
requireText(reportingPage, "production-execution-kpis", "reporting page");
requireText(reportingPage, "production-execution-table", "reporting page");
requireText(reportingPage, "Başarısızlık dağılımı", "reporting page");
requireText(reportingPage, "readQualityReportingPage", "reporting page");
forbidText(reportingPage, "chart", "reporting page");

requireText(mainPage, "/kalite/raporlama", "kalite main page");
requireText(mainPage, "Kalite Raporları", "kalite main page");
requireText(mainPage, "readQualityReportingOverview", "kalite main page");

forbidText(reportingRepo, "OpenAI", "QualityReportingRepository");
forbidText(reportingPage, "OpenAI", "reporting page");

console.log("Quality 3L reporting verification passed.");

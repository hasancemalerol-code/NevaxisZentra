import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 3L security/UI verification failed: ${message}`);
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

console.log("Running quality 3L security/UI verifier.");

const endpoints = readRequired("apps/api/Quality/QualityReportingEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Quality/QualityEndpointHelpers.cs");
const reportingRepo = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityReportingRepository.cs");
const traceabilityRepo = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityTraceabilityRepository.cs");
const apiClient = readRequired("apps/web/src/features/quality-control/server/quality-control-api-client.ts");
const writer = readRequired("apps/web/src/features/quality-control/server/quality-control-writer.ts");
const reportingPage = readRequired("apps/web/src/app/(app)/kalite/raporlama/page.tsx");
const summaryPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/ozet/page.tsx");
const mainPage = readRequired("apps/web/src/app/(app)/kalite/page.tsx");
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");

requireText(endpoints, "RequireAuthorization", "reporting endpoints");
requireText(endpoints, "ResolveReadAccessAsync", "reporting endpoints");
requireText(helpers, "QualityPermissionKeys.Read", "quality endpoint helpers");
requireText(helpers, "Status401Unauthorized", "quality endpoint helpers");
requireText(helpers, "Status403Forbidden", "quality endpoint helpers");

forbidText(endpoints, "MapPost", "reporting endpoints");
forbidText(endpoints, "MapPut", "reporting endpoints");
forbidText(endpoints, "MapDelete", "reporting endpoints");
forbidText(endpoints, "OpenAI", "reporting endpoints");

requireText(apiClient, 'import "./quality-control-server-boundary"', "quality-control-api-client");
const reportingClientSection = apiClient.slice(apiClient.indexOf("export type QualityReportingFilters"));
forbidText(reportingClientSection, "postJson(", "reporting api client");
forbidText(reportingClientSection, "putJson(", "reporting api client");
forbidText(apiClient, 'method: "DELETE"', "quality-control-api-client");
forbidText(reportingRepo, "employeeScore", "QualityReportingRepository");
forbidText(reportingRepo, "EmployeeScore", "QualityReportingRepository");
forbidText(reportingRepo, "operatorScore", "QualityReportingRepository");
forbidText(traceabilityRepo, "employeeScore", "QualityTraceabilityRepository");
forbidText(traceabilityRepo, "EmployeeScore", "QualityTraceabilityRepository");

forbidText(writer, "readQualityReportingOverview", "quality-control writer");
forbidText(writer, "readInspectionSummary", "quality-control writer");

requireText(reportingPage, "production-execution-page", "reporting page");
requireText(reportingPage, "salt okunur", "reporting page");
requireText(summaryPage, "production-execution-page", "summary page");
requireText(summaryPage, "salt okunur", "summary page");
requireText(mainPage, "Kalite Raporları", "kalite main page");
requireText(mainPage, "/kalite/raporlama", "kalite main page");

requireText(nav, 'href: "/kalite"', "primary navigation");
forbidText(nav, 'href: "/kalite/raporlama"', "primary navigation");
forbidText(mainPage, "globals.css", "kalite main page");
forbidText(reportingPage, "globals.css", "reporting page");

forbidText(reportingPage, "OpenAI", "reporting page");
forbidText(summaryPage, "OpenAI", "summary page");
forbidText(reportingRepo, "OpenAI", "QualityReportingRepository");

console.log("Quality 3L security/UI verification passed.");

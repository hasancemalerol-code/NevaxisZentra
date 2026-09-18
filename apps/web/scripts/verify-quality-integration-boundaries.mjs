import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality integration boundaries verification failed: ${message}`);
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

console.log("Running quality integration boundaries verifier.");

const contextReader = readRequired(
  "packages/NevaxisZentra.Persistence/Quality/ProductionOrderContextReader.cs",
);
const ncrRepo = readRequired("packages/NevaxisZentra.Persistence/Quality/QualityNonconformanceRepository.cs");
const reportingRepo = readRequired("packages/NevaxisZentra.Persistence/Quality/QualityReportingRepository.cs");
const engine = readRequired("packages/NevaxisZentra.Persistence/Quality/QualityEvaluationEngine.cs");
const exportReadiness = readRequired(
  "packages/NevaxisZentra.Persistence/Quality/QualityTraceabilityRepository.cs",
);
const writer = readRequired("apps/web/src/features/quality-control/server/quality-control-writer.ts");
const dashboardView = readRequired(
  "apps/web/src/features/quality-control/components/quality-dashboard-view.tsx",
);
const nav = readRequired("apps/web/src/lib/navigation/primary-navigation.ts");
const globalsCss = readRequired("apps/web/src/app/globals.css");

// Quality ownership preserved
requireText(contextReader, "ProductionOrders.AsNoTracking", "ProductionOrderContextReader");
requireText(ncrRepo, "QualityNcrOutboxMessageTypes", "NCR repository");
requireText(reportingRepo, "GetOverviewAsync", "reporting repository");

// Production boundary — read-only production context
forbidText(contextReader, "ProductionOrders.Update", "ProductionOrderContextReader");
forbidText(contextReader, "GoodQuantity =", "ProductionOrderContextReader");
forbidText(ncrRepo, "ProductionOrders.Update", "NCR repository");
forbidText(ncrRepo, "GoodQuantity =", "NCR repository");

// Export boundary — read-only projection
requireText(exportReadiness, "GetExportReadinessAsync", "export readiness reader");
requireText(exportReadiness, "QualityExportReadinessProjectionReadModel", "export readiness reader");
forbidText(exportReadiness, "ExportCases.Update", "export readiness reader");
forbidText(exportReadiness, "ExportShipments", "export readiness reader");
forbidText(reportingRepo, "ExportShipments", "reporting repository");

// ERP boundary — disposition is decision record only
forbidText(ncrRepo, "Logo", "NCR repository");
forbidText(ncrRepo, "Mikro", "NCR repository");
forbidText(ncrRepo, "InventoryMovement", "NCR repository");
forbidText(ncrRepo, "ScrapTransaction", "NCR repository");

// AI / human authority
forbidText(engine, "OpenAI", "evaluation engine");
forbidText(engine, "Ai", "evaluation engine");
forbidText(writer, "autoRelease", "quality writer");
forbidText(dashboardView, "official certificate", "dashboard view");

// Employee scoring
forbidText(reportingRepo, "employeeScore", "reporting repository");
forbidText(reportingRepo, "operatorScore", "reporting repository");
forbidText(reportingRepo, "inspectorRanking", "reporting repository");
forbidText(dashboardView, "employeeScore", "dashboard view");

// Reporting read-only on web boundary
forbidText(writer, "readQualityReportingOverview", "quality writer mutation boundary");

// Sidebar / globals unchanged
requireText(nav, 'href: "/kalite"', "primary navigation");
forbidText(nav, 'href: "/kalite/ncr"', "primary navigation");
forbidText(globalsCss, "quality-page", "globals.css");

console.log("Quality integration boundaries verification passed.");

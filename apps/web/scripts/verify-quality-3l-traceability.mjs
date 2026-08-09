import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 3L traceability verification failed: ${message}`);
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

console.log("Running quality 3L traceability verifier.");

const endpoints = readRequired("apps/api/Quality/QualityReportingEndpointRouteBuilderExtensions.cs");
const traceabilityRepo = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityTraceabilityRepository.cs");
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260809160000_AddQualityTraceabilitySnapshots.cs",
);
const entities = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityEntities.cs");
const domain = readRequired("apps/web/src/features/quality-control/domain/quality-control.ts");
const contracts = readRequired("apps/web/src/features/quality-control/server/quality-control-api-contracts.ts");
const client = readRequired("apps/web/src/features/quality-control/server/quality-control-api-client.ts");
const reader = readRequired("apps/web/src/features/quality-control/server/quality-control-reader.ts");
const traceabilityPanel = readRequired(
  "apps/web/src/features/quality-control/components/quality-traceability-panel.tsx",
);
const summaryPage = readRequired(
  "apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/ozet/page.tsx",
);
const inspectionPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/page.tsx");

requireText(endpoints, "/traceability/production-orders/{productionOrderId:guid}", "traceability endpoints");
requireText(endpoints, "/traceability/products/{productReference}", "traceability endpoints");
requireText(endpoints, "/inspections/{inspectionId:guid}/summary", "traceability endpoints");
requireText(endpoints, "/inspections/{inspectionId:guid}/release-intelligence", "traceability endpoints");
requireText(endpoints, "/production-orders/{productionOrderId:guid}/export-readiness", "traceability endpoints");

requireText(traceabilityRepo, "DefinitionCodeSnapshot", "QualityTraceabilityRepository");
requireText(traceabilityRepo, "GetProductionOrderTraceabilityAsync", "QualityTraceabilityRepository");
requireText(traceabilityRepo, "GetInspectionSummaryAsync", "QualityTraceabilityRepository");

requireText(migration, "plan_code_snapshot", "traceability migration");
requireText(migration, "definition_code_snapshot", "traceability migration");
requireText(migration, "critical_snapshot", "traceability migration");
requireText(entities, "PlanCodeSnapshot", "QualityEntities");
requireText(entities, "DefinitionCodeSnapshot", "QualityEntities");
requireText(entities, "CriticalSnapshot", "QualityEntities");

requireText(domain, "ProductionOrderTraceability", "quality-control domain");
requireText(domain, "QualityInspectionInternalSummary", "quality-control domain");
requireText(domain, "QualityReleaseIntelligence", "quality-control domain");
requireText(domain, "controlPlanRevision", "quality-control domain");

requireText(contracts, "parseProductionOrderTraceabilityApiResponse", "api contracts");
requireText(contracts, "parseQualityInspectionInternalSummaryApiResponse", "api contracts");
requireText(contracts, "parseQualityReleaseIntelligenceApiResponse", "api contracts");
requireText(contracts, "parseQualityExportReadinessApiResponse", "api contracts");

requireText(client, "getProductionOrderTraceability", "api client");
requireText(client, "getQualityInspectionInternalSummary", "api client");
requireText(client, "getQualityReleaseIntelligence", "api client");
requireText(client, "/api/quality/traceability/production-orders/", "api client");
requireText(client, "/api/quality/inspections/", "api client summary");
requireText(client, "/release-intelligence", "api client");

requireText(reader, "readProductionOrderTraceability", "quality-control reader");
requireText(reader, "readInspectionSummary", "quality-control reader");
requireText(reader, "readReleaseIntelligence", "quality-control reader");

requireText(traceabilityPanel, "QualityTraceabilityPanel", "traceability panel");
requireText(traceabilityPanel, "Kalite izlenebilirliği", "traceability panel");
requireText(traceabilityPanel, "quality-ca-list", "traceability panel");

requireText(summaryPage, "Kalite Kontrol Özeti", "summary page");
requireText(summaryPage, "readInspectionSummary", "summary page");
requireText(summaryPage, "Release Durumu", "summary page");
requireText(summaryPage, "quality-table", "summary page");

requireText(inspectionPage, "QualityTraceabilityPanel", "inspection page");
requireText(inspectionPage, "/ozet", "inspection page");
requireText(inspectionPage, "readProductionOrderTraceability", "inspection page");

forbidText(traceabilityPanel, '"use server"', "traceability panel");
forbidText(summaryPage, "OpenAI", "summary page");

console.log("Quality 3L traceability verification passed.");

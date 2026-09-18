import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Export integration gate verification failed: ${message}`);
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

console.log("Running export integration gate verifier.");

// EXPORT-1L
requireText(
  readRequired(
    "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808140000_AddExportCaseReadinessAndDocumentCore.cs",
  ),
  "export_cases",
  "EXPORT-1L migration",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Export/ExportReadinessEngine.cs"),
  "ExportReadinessEngine",
  "EXPORT-1L readiness",
);
requireText(
  readRequired("apps/web/scripts/verify-export-foundation.mjs"),
  "Export foundation verification passed",
  "EXPORT-1L verifier",
);

// EXPORT-2L
requireText(
  readRequired(
    "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808160000_AddExportPackingShipmentAndBrokerWorkspace.cs",
  ),
  "export_shipments",
  "EXPORT-2L migration",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Export/ExportPackingUnit.cs"),
  "ExportPackingUnit",
  "EXPORT-2L packing",
);

// EXPORT-3L
requireText(
  readRequired(
    "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808180000_AddExportCountryComplianceAndResearch.cs",
  ),
  "export_country_compliance_rules",
  "EXPORT-3L migration",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Export/ExportTraceabilityReadRepository.cs"),
  "GetTraceabilityAsync",
  "EXPORT-3L traceability",
);

// EXPORT-4L
requireText(
  readRequired(
    "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808200000_AddExportAiCopilotAndRecommendations.cs",
  ),
  "export_ai_analysis_runs",
  "EXPORT-4L migration",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Export/ExportAiContextBuilder.cs"),
  "BuildForExportCaseAsync",
  "EXPORT-4L AI context",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/AiUsage/AiUsageMeteringService.cs"),
  "RecordUsageAsync",
  "EXPORT-4L metering",
);

// EXPORT-UI-1
const ihracatPage = readRequired("apps/web/src/app/(app)/ihracat/page.tsx");
requireText(ihracatPage, "Yeni Ülke Araştır", "EXPORT-UI-1");
requireText(ihracatPage, "/ihracat/ulke-arastirma", "EXPORT-UI-1");

// PLANNING-LOGISTICS-1L integration
requireText(
  readRequired(
    "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808210000_AddPlanningLogisticsConstraints.cs",
  ),
  "planning_logistics_constraint_evaluations",
  "Planning Logistics migration",
);
requireText(
  readRequired(
    "packages/NevaxisZentra.Persistence/ProductionPlanning/PlanningLogisticsExportReadinessNormalizer.cs",
  ),
  "ResolveNormalizedBlockingReason",
  "Planning Logistics normalizer",
);
requireText(
  readRequired("apps/api/ProductionPlanning/PlanningLogisticsEndpointRouteBuilderExtensions.cs"),
  "/logistics-constraints",
  "Planning Logistics endpoints",
);

const intelligence = readRequired(
  "packages/NevaxisZentra.Persistence/Export/ExportCrossModuleIntelligence.cs",
);
requireText(intelligence, "ExportPlanningLogisticsConstraintRef", "Cross-module intelligence");
requireText(intelligence, "LoadPlanningAsync", "Cross-module intelligence");

const detailWorkspace = readRequired(
  "apps/web/src/features/export/components/export-detail-workspace.tsx",
);
requireText(detailWorkspace, "ExportAiCopilotPanel", "export detail workspace");
requireText(detailWorkspace, "ExportTraceabilityPanel", "export detail workspace");
requireText(detailWorkspace, "ExportCompliancePanel", "export detail workspace");
requireText(detailWorkspace, "ExportBrokerPanel", "export detail workspace");
requireText(detailWorkspace, "ExportShipmentPanel", "export detail workspace");

forbidText(
  readRequired("apps/api/Export/ExportEndpointRouteBuilderExtensions.cs"),
  "MapDelete",
  "Export endpoints",
);

console.log("Export integration gate verification passed.");

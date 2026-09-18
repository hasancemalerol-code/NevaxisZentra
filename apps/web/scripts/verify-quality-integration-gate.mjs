import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality integration gate verification failed: ${message}`);
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

console.log("Running quality integration gate verifier.");

// QUALITY-1L
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Quality/QualityEvaluationEngine.cs"),
  "EvaluateMeasurement",
  "QUALITY-1L engine",
);
requireText(
  readRequired("apps/api/Quality/QualityEndpointRouteBuilderExtensions.cs"),
  "/inspections/{inspectionId:guid}/release",
  "QUALITY-1L endpoints",
);

// QUALITY-2L
requireText(
  readRequired("apps/api/Quality/QualityNcrEndpointRouteBuilderExtensions.cs"),
  "/ncrs/{ncrId:guid}/hold",
  "QUALITY-2L NCR endpoints",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Quality/QualityNonconformanceRepository.cs"),
  "DuplicateNonconformance",
  "QUALITY-2L duplicate guard",
);

// QUALITY-3L
requireText(
  readRequired("apps/api/Quality/QualityReportingEndpointRouteBuilderExtensions.cs"),
  "/reporting/overview",
  "QUALITY-3L reporting",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Quality/QualityTraceabilityRepository.cs"),
  "GetProductHistoryAsync",
  "QUALITY-3L traceability",
);

// QUALITY UI
requireText(
  readRequired("apps/web/src/features/quality-control/components/quality-dashboard-view.tsx"),
  "quality-dashboard-kpis",
  "QUALITY UI dashboard",
);
requireText(
  readRequired("apps/web/src/features/quality-control/components/quality-page-shell.tsx"),
  "QualityPageShell",
  "QUALITY UI shell",
);
requireText(
  readRequired("apps/web/src/app/(app)/kalite/ncr/page.tsx"),
  "QualityNcrFilters",
  "QUALITY UI NCR",
);

// Integration wiring
requireText(readRequired("apps/api/Program.cs"), "MapQualityEndpoints", "Program.cs");
requireText(readRequired("apps/api/Program.cs"), "AddNevaxisZentraQuality", "Program.cs");

forbidText(
  readRequired("apps/api/Quality/QualityEndpointRouteBuilderExtensions.cs"),
  "MapDelete",
  "quality endpoints",
);
forbidText(
  readRequired("apps/api/Quality/QualityReportingEndpointRouteBuilderExtensions.cs"),
  "MapPost",
  "reporting endpoints",
);

console.log("Quality integration gate verification passed.");

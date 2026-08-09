import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 1L core verification failed: ${message}`);
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

console.log("Running quality 1L core verifier.");

const endpoints = readRequired("apps/api/Quality/QualityEndpointRouteBuilderExtensions.cs");
const engine = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityEvaluationEngine.cs");
const inspectionRepo = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityInspectionRepository.cs");
const contextReader = readRequired("packages/CrevaFactOS.Persistence/Quality/ProductionOrderContextReader.cs");
const program = readRequired("apps/api/Program.cs");
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260809140000_AddQualityControlCore.cs",
);
const kalitePage = readRequired("apps/web/src/app/(app)/kalite/page.tsx");

requireText(endpoints, "MapGroup(\"/api/quality\")", "quality endpoints");
requireText(endpoints, "/control-plans", "quality endpoints");
requireText(endpoints, "/inspections/{inspectionId:guid}/release", "quality endpoints");
requireText(endpoints, "QualityPermissionKeys.Release", "quality endpoints");
requireText(program, "AddCrevaFactOSQuality", "Program.cs");
requireText(program, "MapQualityEndpoints", "Program.cs");

requireText(engine, "EvaluateMeasurement", "QualityEvaluationEngine");
requireText(engine, "ComputeOverallResult", "QualityEvaluationEngine");
forbidText(engine, "OpenAI", "QualityEvaluationEngine");
forbidText(engine, "Ai", "QualityEvaluationEngine");

requireText(contextReader, "ProductionOrders.AsNoTracking", "ProductionOrderContextReader");
forbidText(contextReader, "ProductionOrders.Update", "ProductionOrderContextReader");
forbidText(contextReader, "GoodQuantity =", "ProductionOrderContextReader");

requireText(inspectionRepo, "QualityFailureCodes.ReleaseNotAllowed", "QualityInspectionRepository");
requireText(inspectionRepo, "QualityOutboxMessageTypes.InspectionCompleted", "QualityInspectionRepository");

forbidText(endpoints, "MapDelete", "quality endpoints");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(inspectionRepo, "Ncr", "QualityInspectionRepository");
forbidText(inspectionRepo, "Capa", "QualityInspectionRepository");

requireText(migration, "quality_control_plans", "migration");
requireText(migration, "quality_inspection_definitions", "migration");
requireText(migration, "quality_inspections", "migration");
requireText(migration, "quality_inspection_results", "migration");

requireText(kalitePage, "Kalite Kontrol", "kalite page");
requireText(kalitePage, "Açık Kontroller", "kalite page");
requireText(kalitePage, "Yeni Kontrol Planı", "kalite page");

console.log("Quality 1L core verification passed.");

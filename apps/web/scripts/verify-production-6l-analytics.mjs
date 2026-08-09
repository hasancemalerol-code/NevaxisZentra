import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production 6L analytics verification failed: ${message}`);
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

console.log("Running production 6L analytics verifier.");

const engine = readRequired("packages/CrevaFactOS.Persistence/Production/ProductionReportingEngine.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Production/ProductionReportingRepository.cs");
const models = readRequired("packages/CrevaFactOS.Persistence/Production/ProductionReportingModels.cs");

requireText(engine, "ComputeYieldPercent", "ProductionReportingEngine");
requireText(engine, "ComputeCompletionRatePercent", "ProductionReportingEngine");
requireText(engine, "ComputeRiskSignals", "ProductionReportingEngine");
requireText(engine, "shipment-deadline-risk", "ProductionReportingEngine");
requireText(engine, "expected-cycle-unavailable", "ProductionReportingEngine");
forbidText(engine, "LLM", "ProductionReportingEngine");
forbidText(engine, "employeeScore", "ProductionReportingEngine");

requireText(repository, "GetScrapAnalyticsAsync", "ProductionReportingRepository");
requireText(repository, "GetDowntimeAnalyticsAsync", "ProductionReportingRepository");
requireText(repository, "GetSetupAnalyticsAsync", "ProductionReportingRepository");
requireText(repository, "GetCycleAnalyticsAsync", "ProductionReportingRepository");
requireText(repository, "ListShiftAnalyticsAsync", "ProductionReportingRepository");
requireText(repository, "GetExceptionAnalyticsAsync", "ProductionReportingRepository");
requireText(models, "ProductionPlanActualComparisonReadModel", "reporting models");

console.log("Production 6L analytics verification passed.");

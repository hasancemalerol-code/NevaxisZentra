import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production 6L boundaries verification failed: ${message}`);
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

console.log("Running production 6L boundaries verifier.");

const signalReader = readRequired(
  "packages/NevaxisFactOS.Persistence/Production/ProductionPlanningLogisticsSignalReader.cs",
);
const repository = readRequired(
  "packages/NevaxisFactOS.Persistence/Production/ProductionReportingRepository.cs",
);
const engine = readRequired(
  "packages/NevaxisFactOS.Persistence/Production/ProductionReportingEngine.cs",
);

requireText(signalReader, "PlanningLogisticsConstraintEvaluations", "logistics signal reader");
requireText(signalReader, "Planning Logistics signals only", "logistics signal reader");
forbidText(signalReader, "ExportShipments", "logistics signal reader");
forbidText(signalReader, "ExportBroker", "logistics signal reader");
forbidText(signalReader, "Compliance", "logistics signal reader");

forbidText(repository, "Quality", "ProductionReportingRepository");
forbidText(repository, "Ncr", "ProductionReportingRepository");
forbidText(engine, "ranking", "ProductionReportingEngine");
forbidText(engine, "disciplinary", "ProductionReportingEngine");
forbidText(engine, "payroll", "ProductionReportingEngine");

console.log("Production 6L boundaries verification passed.");

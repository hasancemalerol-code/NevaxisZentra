import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptsDirectory = dirname(fileURLToPath(import.meta.url));
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`MOLD-1 mutation boundary verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  return readFileSync(resolve(projectRoot, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

console.log("Running MOLD-1 mutation-boundary verifier.");

const repo = readRequired("packages/CrevaFactOS.Persistence/Mold/MoldRepository.cs");
const endpoints = readRequired("apps/api/Mold/MoldEndpointRouteBuilderExtensions.cs");

forbidText(repo, "MaintenanceRequests", "maintenance boundary");
forbidText(repo, "ProductionOrders", "production boundary");
forbidText(repo, "MachineRegistry", "edge boundary");
forbidText(repo, "ErpConnection", "erp boundary");
forbidText(endpoints, "maintenance.", "maintenance API coupling");
forbidText(endpoints, "production.", "production API coupling");

console.log("MOLD-1 mutation-boundary verification passed.");

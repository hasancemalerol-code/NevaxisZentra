import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`ERP-NETSIS-1 security verification failed: ${message}`);
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

console.log("Running ERP-NETSIS-1 security verifier.");

const netsisAdapter = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisErpAdapter.cs");
const netsisTransport = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/INetsisReadTransport.cs");
const netsisFixture = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisReadTransportTestFixture.cs");
const netsisDescriptor = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisConnectionDescriptor.cs");
const netsisBridge = readRequired("packages/NevaxisFactOS.Persistence/Erp/Netsis/NetsisBridgeReadTransport.cs");
const bridgeProgram = readRequired("tools/NevaxisFactOS.NetsisBridge/Program.cs");
const bridgeCsproj = readRequired("tools/NevaxisFactOS.NetsisBridge/NevaxisFactOS.NetsisBridge.csproj");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const adapters = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpAdapters.cs");

forbidText(netsisTransport, "Password", "no plaintext password in transport");
forbidText(netsisBridge, "Password", "no plaintext password in bridge transport");
forbidText(netsisBridge, "connectionString", "no connection string in bridge");
forbidText(netsisDescriptor, "password", "no password in descriptor");
forbidText(netsisFixture, "password", "no password in fixture");
requireText(netsisTransport, "ResolveBearerToken", "credential resolved via reference");
forbidText(netsisTransport, "= \"netsis-test-fixture\"", "no hardcoded token in transport class (only in constants)");
forbidText(netsisBridge, "SELECT ", "no SQL in bridge transport");
forbidText(netsisBridge, "INSERT ", "no SQL insert in bridge transport");
forbidText(bridgeProgram, "MapPost", "no POST routes in bridge");
forbidText(bridgeProgram, "MapPut", "no PUT routes in bridge");
forbidText(bridgeProgram, "MapDelete", "no DELETE routes in bridge");
forbidText(bridgeProgram, "/sql", "no generic SQL endpoint");
forbidText(bridgeProgram, "/query", "no generic query endpoint");
forbidText(bridgeProgram, "/execute", "no execute endpoint");
forbidText(adapters, "class NetsisErpAdapterPlaceholder", "placeholder replaced");
forbidText(netsisAdapter, "IErpWritebackAdapter", "no Netsis writeback");
forbidText(netsisAdapter, "SimulatorErpAdapter", "no Simulator fallback");
forbidText(netsisAdapter, "LogoErpAdapter", "no Logo fallback");
forbidText(netsisAdapter, "MikroErpAdapter", "no Mikro fallback");
requireText(bridgeProgram, "Status405MethodNotAllowed", "bridge rejects non-GET");
requireText(bridgeProgram, "AuthToken", "bridge requires auth token config");
requireText(bridgeProgram, "Status401Unauthorized", "bridge returns 401 on bad token");
requireText(netsisAdapter, "netsis.liveValidated", "live-validation flag present");
forbidText(netsisFixture, "Netsis Simulator", "fixture not called simulator");
requireText(netsisFixture, "Netsis Test Fixture", "fixture clearly named test");

console.log("ERP-NETSIS-1 security verification passed.");

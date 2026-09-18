import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`ERP-MIKRO-1 security verification failed: ${message}`);
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

console.log("Running ERP-MIKRO-1 security verifier.");

const mikroAdapter = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroErpAdapter.cs");
const mikroBridgeTransport = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroBridgeReadTransport.cs");
const mikroBridgeProgram = readRequired("tools/NevaxisZentra.MikroBridge/Program.cs");
const mikroBridgeReadme = readRequired("tools/NevaxisZentra.MikroBridge/README.md");
const repoSupport = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpRepositorySupport.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const writebackRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpWritebackRepository.cs");
const mikroFixture = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroReadTransportTestFixture.cs");
const seed = readRequired("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotErpSeed.cs");
const adapters = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpAdapters.cs");

forbidText(mikroAdapter, "password=", "no plaintext password");
forbidText(mikroAdapter, "connectionstring", "no connection string");
forbidText(mikroBridgeProgram, "MapPost(", "bridge read-only");
forbidText(mikroBridgeProgram, "MapPut(", "bridge read-only");
forbidText(mikroBridgeProgram, "MapDelete(", "bridge read-only");
forbidText(mikroBridgeProgram, "ExecuteSql", "no generic SQL endpoint");
requireText(mikroBridgeProgram, "MapGet(\"/health\"", "bridge health endpoint");
requireText(mikroBridgeProgram, "MapGet(\"/context\"", "bridge context endpoint");
requireText(mikroBridgeProgram, "MapGet(\"/customers\"", "bridge customers endpoint");
requireText(mikroBridgeProgram, "MapGet(\"/products\"", "bridge products endpoint");
requireText(mikroBridgeProgram, "Authorization", "bridge authentication");
forbidText(mikroBridgeProgram, "authToken:", "health must not return secret");
requireText(mikroBridgeReadme, "No mutation routes", "bridge scope documented");
requireText(mikroBridgeReadme, "No generic SQL endpoint", "bridge scope documented");
requireText(repoSupport, "CredentialReference", "credential reference pattern");
requireText(repoSupport, "ContainsForbiddenSecretField", "credential validation");
forbidText(endpoints, "fetch(\"http://127.0.0.1", "no browser-direct bridge call");
forbidText(mikroAdapter, "Insert", "no Mikro write");
forbidText(mikroAdapter, "UpdateStock", "no Mikro write");
forbidText(mikroAdapter, "ExecuteSql", "no Mikro SQL");
forbidText(mikroAdapter, "IErpWritebackAdapter", "Mikro writeback unsupported");
forbidText(writebackRepo, "MikroErpAdapter", "Mikro writeback unsupported");
requireText(mikroAdapter, "MikroTransportErrorCodes", "sanitized error model");
requireText(seed, "EnsureErpMikroTestConnectionAsync", "dev-only mikro test seed");
const mikroCredentialRefs = readRequired("packages/NevaxisZentra.Persistence/Erp/Mikro/MikroTransportKind.cs");
requireText(mikroCredentialRefs, "mikro-test-fixture", "test fixture credential reference");
forbidText(seed, "Status = ErpConnectionStatuses.Healthy", "test fixture not production healthy claim at seed");
forbidText(mikroAdapter, "LogoErpAdapter", "no Logo fallback");
forbidText(mikroAdapter, "SimulatorErpAdapter", "no simulator fallback");
forbidText(adapters, "class MikroErpAdapter : IErpWritebackAdapter", "Mikro writeback blocked");
forbidText(mikroBridgeProgram, "WindowsFirewall", "no firewall manipulation");

console.log("ERP-MIKRO-1 security verification passed.");

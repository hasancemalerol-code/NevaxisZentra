import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`ERP-LOGO-1 security verification failed: ${message}`);
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

console.log("Running ERP-LOGO-1 security verifier.");

const logoAdapter = readRequired("packages/NevaxisFactOS.Persistence/Erp/Logo/LogoErpAdapter.cs");
const logoBridgeTransport = readRequired("packages/NevaxisFactOS.Persistence/Erp/Logo/LogoBridgeReadTransport.cs");
const logoBridgeProgram = readRequired("tools/NevaxisFactOS.LogoBridge/Program.cs");
const logoBridgeReadme = readRequired("tools/NevaxisFactOS.LogoBridge/README.md");
const repoSupport = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpRepositorySupport.cs");
const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const writebackRepo = readRequired("packages/NevaxisFactOS.Persistence/Erp/ErpWritebackRepository.cs");
const logoFixture = readRequired("packages/NevaxisFactOS.Persistence/Erp/Logo/LogoReadTransportTestFixture.cs");
const seed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotErpSeed.cs");

forbidText(logoAdapter, "password=", "no plaintext password");
forbidText(logoAdapter, "connectionstring", "no connection string");
forbidText(logoBridgeProgram, "MapPost(", "bridge read-only");
forbidText(logoBridgeProgram, "MapPut(", "bridge read-only");
forbidText(logoBridgeProgram, "MapDelete(", "bridge read-only");
forbidText(logoBridgeProgram, "ExecuteSql", "no generic SQL endpoint");
requireText(logoBridgeProgram, "MapGet(\"/health\"", "bridge health endpoint");
requireText(logoBridgeProgram, "MapGet(\"/context\"", "bridge context endpoint");
requireText(logoBridgeProgram, "MapGet(\"/customers\"", "bridge customers endpoint");
requireText(logoBridgeProgram, "MapGet(\"/products\"", "bridge products endpoint");
requireText(logoBridgeProgram, "Authorization", "bridge authentication");
forbidText(logoBridgeProgram, "authToken:", "health must not return secret");
requireText(logoBridgeReadme, "No mutation routes", "bridge scope documented");
requireText(logoBridgeReadme, "No generic SQL endpoint", "bridge scope documented");
requireText(repoSupport, "CredentialReference", "credential reference pattern");
requireText(repoSupport, "ContainsForbiddenSecretField", "credential validation");
forbidText(endpoints, "fetch(\"http://127.0.0.1", "no browser-direct bridge call");
forbidText(logoAdapter, "Insert", "no Logo write");
forbidText(logoAdapter, "UpdateStock", "no Logo write");
forbidText(logoAdapter, "ExecuteSql", "no Logo SQL");
forbidText(logoAdapter, "IErpWritebackAdapter", "Logo writeback unsupported");
forbidText(writebackRepo, "LogoErpAdapter", "Logo writeback unsupported");
requireText(logoAdapter, "LogoTransportErrorCodes", "sanitized error model");
requireText(seed, "EnsureErpLogoTestConnectionAsync", "dev-only logo test seed");
const logoCredentialRefs = readRequired("packages/NevaxisFactOS.Persistence/Erp/Logo/LogoTransportKind.cs");
requireText(logoCredentialRefs, "logo-test-fixture", "test fixture credential reference");
forbidText(seed, "Status = ErpConnectionStatuses.Healthy", "test fixture not production healthy claim at seed");

console.log("ERP-LOGO-1 security verification passed.");

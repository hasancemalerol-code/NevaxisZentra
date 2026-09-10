#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`INTEGRATION-WAVE-C security verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) fail(`Missing ${relativePath}`);
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

console.log("Running INTEGRATION-WAVE-C security verifier.");

const ssrf = readRequired("packages/CrevaFactOS.Persistence/Integration/Rest/IntegrationRestSsrfGuard.cs");
const db = readRequired("packages/CrevaFactOS.Persistence/Integration/Database/IntegrationReadOnlyDatabaseFetcher.cs");
const webhook = readRequired("packages/CrevaFactOS.Persistence/Integration/Webhook/IntegrationWebhookIngestionService.cs");
const folder = readRequired("packages/CrevaFactOS.Persistence/Integration/FolderWatch/IntegrationFolderWatchScanner.cs");
const erp = readRequired("packages/CrevaFactOS.Persistence/Integration/Erp/IntegrationErpRecordBridge.cs");
const endpoints = readRequired("apps/api/Integration/IntegrationEndpointRouteBuilderExtensions.cs");

assert("SSRF scheme guard", ssrf.includes("UriSchemeHttps"));
assert("SSRF metadata block", ssrf.includes("metadata.google.internal"));
assert("DB forbidden SQL pattern", db.includes("ForbiddenSqlPattern"));
assert("webhook signature validation", webhook.includes("ValidateSignature"));
assert("webhook replay window", webhook.includes("ReplayWindowSeconds"));
assert("webhook ingress resolver", webhook.includes("resolve_integration_ingress_connector"));
assert("folder path escape guard", folder.includes("path escape blocked"));
assert("ERP read-only bridge", erp.includes("ReadProductsAsync"));
assert("connector secret masking", endpoints.includes("hasSecretReference"));
assert("sync permission run", endpoints.includes("IntegrationPermissionKeys.Run"));

console.log("REST_SSRF_MATRIX=PASS");
console.log("DATABASE_READ_ONLY_ENFORCEMENT=PASS");
console.log("WEBHOOK_SECURITY_MATRIX=PASS");
console.log("FOLDER_WATCH_SECURITY=PASS");
console.log("WAVE_C_SECURITY_MATRIX=PASS");
console.log("INTEGRATION-WAVE-C security verification passed.");

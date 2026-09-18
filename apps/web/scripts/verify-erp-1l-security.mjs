import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`ERP-1L security verification failed: ${message}`);
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

console.log("Running ERP-1L security verifier.");

const endpoints = readRequired("apps/api/Erp/ErpEndpointRouteBuilderExtensions.cs");
const helpers = readRequired("apps/api/Erp/ErpEndpointHelpers.cs");
const permissions = readRequired("packages/NevaxisZentra.Persistence/Authorization/ErpPermissionKeys.cs");
const migration = readRequired(
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260809200000_AddErpIntegrationFoundation.cs",
);
const connectionRepo = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpConnectionRepository.cs");
const entities = readRequired("packages/NevaxisZentra.Persistence/Erp/ErpEntities.cs");

requireText(endpoints, "RequireAuthorization", "erp endpoints");
requireText(permissions, "erp.read", "ErpPermissionKeys");
requireText(permissions, "erp.manage", "ErpPermissionKeys");
requireText(permissions, "erp.sync", "ErpPermissionKeys");
requireText(helpers, "ErpPermissionKeys.Read", "ErpEndpointHelpers");
requireText(endpoints, "ErpPermissionKeys.Manage", "erp endpoints");
requireText(endpoints, "ErpPermissionKeys.Sync", "erp endpoints");
requireText(helpers, "Status401Unauthorized", "ErpEndpointHelpers");
requireText(helpers, "Status403Forbidden", "ErpEndpointHelpers");
requireText(helpers, "Status409Conflict", "ErpEndpointHelpers");
requireText(helpers, "ConcurrencyConflict", "ErpEndpointHelpers");
requireText(connectionRepo, "ExpectedVersion", "ErpConnectionRepository");
requireText(connectionRepo, "FacilityId == context.FacilityId", "facility scoping");
requireText(connectionRepo, "TenantId == context.TenantId", "tenant scoping");

requireText(migration, "ENABLE ROW LEVEL SECURITY", "migration");
requireText(migration, "FORCE ROW LEVEL SECURITY", "migration");
requireText(migration, "REVOKE ALL ON TABLE erp_connection_profiles", "migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE erp_connection_profiles", "migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE erp_customers", "migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE erp_products", "migration");
requireText(migration, "GRANT SELECT, INSERT, UPDATE ON TABLE erp_sync_runs", "migration");
forbidText(migration, "GRANT DELETE", "migration");

forbidText(endpoints, "MapDelete", "erp endpoints");
forbidText(endpoints, "AllowAnonymous", "erp endpoints");

forbidText(entities, "password", "ErpEntities");
forbidText(entities, "apiKey", "ErpEntities");
forbidText(entities, "ApiKey", "ErpEntities");
forbidText(entities, "connectionString", "ErpEntities");
forbidText(entities, "ConnectionString", "ErpEntities");
requireText(entities, "CredentialReference", "secret reference only");

requireText(connectionRepo, "ContainsForbiddenSecretField", "plaintext secret guard");

console.log("ERP-1L security verification passed.");

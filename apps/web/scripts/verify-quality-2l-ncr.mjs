import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Quality 2L NCR verification failed: ${message}`);
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

console.log("Running quality 2L NCR verifier.");

const endpoints = readRequired("apps/api/Quality/QualityNcrEndpointRouteBuilderExtensions.cs");
const lifecycle = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityNcrLifecycleEngine.cs");
const ncrRepo = readRequired("packages/CrevaFactOS.Persistence/Quality/QualityNonconformanceRepository.cs");
const signalReader = readRequired("packages/CrevaFactOS.Persistence/Quality/ProductionQualitySignalReader.cs");
const migration = readRequired(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260809150000_AddQualityNcrHoldAndCorrectiveAction.cs",
);
const domain = readRequired("apps/web/src/features/quality-control/domain/quality-control.ts");
const contracts = readRequired("apps/web/src/features/quality-control/server/quality-control-api-contracts.ts");
const client = readRequired("apps/web/src/features/quality-control/server/quality-control-api-client.ts");
const writer = readRequired("apps/web/src/features/quality-control/server/quality-control-writer.ts");
const reader = readRequired("apps/web/src/features/quality-control/server/quality-control-reader.ts");
const ncrActions = readRequired("apps/web/src/features/quality-control/components/quality-ncr-actions.tsx");
const ncrListPage = readRequired("apps/web/src/app/(app)/kalite/ncr/page.tsx");
const ncrDetailPage = readRequired("apps/web/src/app/(app)/kalite/ncr/[ncrId]/page.tsx");
const inspectionPage = readRequired("apps/web/src/app/(app)/kalite/kontroller/[inspectionId]/page.tsx");
const inspectionNcrLink = readRequired(
  "apps/web/src/features/quality-control/components/quality-inspection-ncr-link.tsx",
);

requireText(endpoints, "/ncrs", "NCR endpoints");
requireText(endpoints, "/ncrs/{ncrId:guid}/resolve", "NCR endpoints");
requireText(endpoints, "/ncrs/{ncrId:guid}/close", "NCR endpoints");
requireText(endpoints, "/ncrs/{ncrId:guid}/hold", "NCR endpoints");
requireText(endpoints, "/ncrs/{ncrId:guid}/hold/release", "NCR endpoints");
requireText(endpoints, "/ncrs/{ncrId:guid}/disposition", "NCR endpoints");
requireText(endpoints, "/corrective-actions/{actionId:guid}/complete", "NCR endpoints");
requireText(endpoints, "/production-orders/{productionOrderId:guid}/signal", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.NcrManage", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.Hold", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.Disposition", "NCR endpoints");
requireText(endpoints, "QualityPermissionKeys.Release", "NCR endpoints");

requireText(lifecycle, "CanResolve", "QualityNcrLifecycleEngine");
requireText(lifecycle, "CanClose", "QualityNcrLifecycleEngine");
requireText(lifecycle, "RequiresDisposition", "QualityNcrLifecycleEngine");

requireText(ncrRepo, "DuplicateNonconformance", "QualityNonconformanceRepository");
requireText(ncrRepo, "ProductionOrders.AsNoTracking", "QualityNonconformanceRepository");
forbidText(ncrRepo, "ProductionOrders.Update", "QualityNonconformanceRepository");
forbidText(ncrRepo, "GoodQuantity =", "QualityNonconformanceRepository");

requireText(signalReader, "QualityHoldActive", "ProductionQualitySignalReader");
requireText(signalReader, "Blocking", "ProductionQualitySignalReader");

requireText(migration, "quality_nonconformances", "migration");
requireText(migration, "quality_holds", "migration");
requireText(migration, "quality_corrective_actions", "migration");
forbidText(migration, "GRANT DELETE", "migration");
forbidText(endpoints, "MapDelete", "NCR endpoints");

requireText(domain, "QualityNcrSummary", "quality-control domain");
requireText(domain, "QualityNcrDetail", "quality-control domain");
requireText(domain, "openNcrCount", "quality-control domain");
requireText(domain, "linkedNonconformances", "quality-control domain");

requireText(contracts, "parseQualityNcrListApiResponse", "api contracts");
requireText(contracts, "parseQualityNcrDetailApiResponse", "api contracts");
requireText(contracts, "openNcrCount", "api contracts");
requireText(contracts, "linkedNonconformances", "api contracts");

requireText(client, "listQualityNcrs", "api client");
requireText(client, "createQualityNcr", "api client");
requireText(client, "placeQualityHold", "api client");
requireText(client, "releaseQualityHold", "api client");
requireText(client, "decideQualityDisposition", "api client");
requireText(client, "resolveQualityNcr", "api client");
requireText(client, "closeQualityNcr", "api client");
requireText(client, "getProductionQualitySignal", "api client");

requireText(reader, "readQualityNcrList", "quality-control reader");
requireText(reader, "readQualityNcrById", "quality-control reader");

requireText(writer, "createQualityNcrAction", "quality-control writer");
requireText(writer, "placeQualityHoldAction", "quality-control writer");
requireText(writer, "releaseQualityHoldAction", "quality-control writer");
requireText(writer, "decideQualityDispositionAction", "quality-control writer");
requireText(writer, "resolveQualityNcrAction", "quality-control writer");
requireText(writer, "closeQualityNcrAction", "quality-control writer");

requireText(ncrActions, "Hold Koy", "quality-ncr-actions");
requireText(ncrActions, "Hold Kaldır", "quality-ncr-actions");
requireText(ncrActions, "Disposition Belirle", "quality-ncr-actions");
requireText(ncrActions, "Düzeltici Aksiyon Ekle", "quality-ncr-actions");
requireText(ncrActions, "Çöz", "quality-ncr-actions");
requireText(ncrActions, "Kapat", "quality-ncr-actions");

requireText(ncrListPage, "Uygunsuzluk Raporları", "NCR list page");
requireText(ncrListPage, "production-execution-page", "NCR list page");
requireText(ncrDetailPage, "Kalite Blokesi", "NCR detail page");
requireText(inspectionPage, "QualityInspectionNcrLink", "inspection page");
requireText(inspectionNcrLink, "NCR Oluştur", "quality-inspection-ncr-link");

forbidText(ncrRepo, "OpenAI", "QualityNonconformanceRepository");
forbidText(ncrRepo, "Capa", "QualityNonconformanceRepository");
forbidText(ncrActions, "5 Why", "quality-ncr-actions");
forbidText(ncrActions, "Ishikawa", "quality-ncr-actions");

console.log("Quality 2L NCR verification passed.");

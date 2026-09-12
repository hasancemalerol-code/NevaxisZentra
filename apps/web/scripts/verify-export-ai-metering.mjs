import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export AI metering verification failed: ${message}`);
  process.exit(1);
}

function readRequired(baseUrl, relativePath) {
  const filePath = resolvePath(baseUrl, relativePath);
  if (!existsSync(filePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(filePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

const usageRecord = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence/AiUsage/AiUsageRecord.cs",
);
requireText(usageRecord, "class AiUsageRecord", "AiUsageRecord");
requireText(usageRecord, "CreditsConsumed", "AiUsageRecord");
requireText(usageRecord, "Module", "AiUsageRecord");
requireText(usageRecord, "RunId", "AiUsageRecord");

const meteringInterface = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence/AiUsage/IAiUsageMeteringService.cs",
);
requireText(meteringInterface, "interface IAiUsageMeteringService", "IAiUsageMeteringService");
requireText(meteringInterface, "RecordUsageAsync", "IAiUsageMeteringService");
requireText(meteringInterface, "GetCurrentUsageAsync", "IAiUsageMeteringService");
requireText(meteringInterface, "AiUsageSummaryReadModel", "IAiUsageMeteringService");

const meteringService = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence/AiUsage/AiUsageMeteringService.cs",
);
requireText(meteringService, "class AiUsageMeteringService", "AiUsageMeteringService");
requireText(meteringService, "AiUsageRecords.Add", "AiUsageMeteringService");
requireText(meteringService, "GetCurrentUsageAsync", "AiUsageMeteringService");
requireText(meteringService, "UnavailableAiUsageMeteringService", "AiUsageMeteringService");

const entityConfig = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence/Configurations/AiUsageRecordEntityConfiguration.cs",
);
requireText(entityConfig, "AiUsageRecord", "AiUsageRecordEntityConfiguration");
requireText(entityConfig, "ai_usage_records", "AiUsageRecordEntityConfiguration");

const writeRepo = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence/Export/ExportAiWriteRepository.cs",
);
requireText(writeRepo, "AiUsageRecords.Add", "ExportAiWriteRepository");
requireText(writeRepo, "CreditsConsumed", "ExportAiWriteRepository");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export4LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "/api/ai/usage/current", "Export 4L usage endpoint");
requireText(endpoints, "GetCurrentAiUsageAsync", "Export 4L usage endpoint");
requireText(endpoints, "IAiUsageMeteringService", "Export 4L usage endpoint");
requireText(endpoints, "AiUsageSummaryDto", "Export 4L usage endpoint");

const serviceRegistration = readRequired(
  repoRootUrl,
  "apps/api/Export/ExportEndpointRouteBuilderExtensions.cs",
);
requireText(serviceRegistration, "IAiUsageMeteringService", "Export service registration");
requireText(serviceRegistration, "AiUsageMeteringService", "Export service registration");

const dtos = readRequired(repoRootUrl, "apps/api/Export/Export4LDtos.cs");
requireText(dtos, "AiUsageSummaryDto", "Export4LDtos");
requireText(dtos, "CreditsConsumedThisPeriod", "Export4LDtos");
requireText(dtos, "MonthlyQuota", "Export4LDtos");

const domain = readRequired(webRootUrl, "src/features/export/domain/export-4l.ts");
requireText(domain, "AiUsageSummary", "export-4l domain");
requireText(domain, "creditsConsumedThisPeriod", "export-4l domain");

const contracts = readRequired(
  webRootUrl,
  "src/features/export/server/export-4l-api-contracts.ts",
);
requireText(contracts, "getCurrentAiUsage", "export-4l-api-contracts");
requireText(contracts, "parseAiUsageSummaryApiResponse", "export-4l-api-contracts");
requireText(contracts, "/api/ai/usage/current", "export-4l-api-contracts");

const reader = readRequired(
  webRootUrl,
  "src/features/export/server/export-4l-reader.ts",
);
requireText(reader, "readCurrentAiUsage", "export-4l-reader");

const copilotPanel = readRequired(
  webRootUrl,
  "src/features/export/components/export-ai-copilot-panel.tsx",
);
requireText(copilotPanel, "AiUsageSummary", "export-ai-copilot-panel");
requireText(copilotPanel, "creditsConsumed", "export-ai-copilot-panel");

const migration = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260808200000_AddExportAiCopilotAndRecommendations.cs",
);
requireText(migration, "ai_usage_records", "EXPORT-4L migration");
requireText(migration, "credits_consumed", "EXPORT-4L migration");
requireText(migration, "append-only metering", "EXPORT-4L migration");

console.log("Export AI metering verification passed.");

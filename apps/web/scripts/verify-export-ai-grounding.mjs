import { existsSync, readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(`Export AI grounding verification failed: ${message}`);
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

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

const entities = readRequired(
  repoRootUrl,
  "packages/NevaxisZentra.Persistence/Export/ExportAiEntities.cs",
);
requireText(entities, "PromptVersion", "ExportAiEntities");
requireText(entities, "ReasoningSummary", "ExportAiEntities");
requireText(entities, "SourceReferencesJson", "ExportAiEntities");
forbidText(entities, "ChainOfThought", "ExportAiEntities");
forbidText(entities, "chainOfThought", "ExportAiEntities");
forbidText(entities, "RawReasoning", "ExportAiEntities");

const models = readRequired(
  repoRootUrl,
  "packages/NevaxisZentra.Persistence/Export/Export4LModels.cs",
);
requireText(models, "ExportAiPromptVersions", "Export4LModels");
requireText(models, "ExportAiSourceReference", "Export4LModels");
requireText(models, "ReasoningSummary", "Export4LModels");
requireText(models, "export.case.overview.v1", "Export4LModels");
requireText(models, "export.compliance.explain.v1", "Export4LModels");
forbidText(models, "chain-of-thought", "Export4LModels");
forbidText(models, "ChainOfThought", "Export4LModels");

const writeRepo = readRequired(
  repoRootUrl,
  "packages/NevaxisZentra.Persistence/Export/ExportAiWriteRepository.cs",
);
requireText(writeRepo, "ResolvePromptVersion", "ExportAiWriteRepository");
requireText(writeRepo, "ExportAiPromptVersions", "ExportAiWriteRepository");
requireText(writeRepo, "run.ReasoningSummary = output.ReasoningSummary", "ExportAiWriteRepository");
requireText(writeRepo, "SerializeSources", "ExportAiWriteRepository");

const dtos = readRequired(repoRootUrl, "apps/api/Export/Export4LDtos.cs");
requireText(dtos, "ExportAiSourceReferenceDto", "Export4LDtos");
requireText(dtos, "PromptVersion", "Export4LDtos");
requireText(dtos, "ReasoningSummary", "Export4LDtos");
requireText(dtos, "SourceReferences", "Export4LDtos");
forbidText(dtos, "ChainOfThought", "Export4LDtos");

const endpoints = readRequired(
  repoRootUrl,
  "apps/api/Export/Export4LEndpointRouteBuilderExtensions.cs",
);
requireText(endpoints, "PromptVersion", "Export 4L endpoints");
requireText(endpoints, "ReasoningSummary", "Export 4L endpoints");
requireText(endpoints, "MapSourceReference", "Export 4L endpoints");
requireText(endpoints, "SourceReferences", "Export 4L endpoints");

const domain = readRequired(webRootUrl, "src/features/export/domain/export-4l.ts");
requireText(domain, "promptVersion", "export-4l domain");
requireText(domain, "reasoningSummary", "export-4l domain");
requireText(domain, "sourceReferences", "export-4l domain");
requireText(domain, "ExportAiSourceReference", "export-4l domain");
forbidText(domain, "chainOfThought", "export-4l domain");

const contracts = readRequired(
  webRootUrl,
  "src/features/export/server/export-4l-api-contracts.ts",
);
requireText(contracts, "parseSourceReference", "export-4l-api-contracts");
requireText(contracts, "promptVersion", "export-4l-api-contracts");
requireText(contracts, "reasoningSummary", "export-4l-api-contracts");
requireText(contracts, "sourceReferences", "export-4l-api-contracts");
forbidText(contracts, "chainOfThought", "export-4l-api-contracts");
forbidText(contracts, "chain-of-thought", "export-4l-api-contracts");

const copilotPanel = readRequired(
  webRootUrl,
  "src/features/export/components/export-ai-copilot-panel.tsx",
);
requireText(copilotPanel, "reasoningSummary", "export-ai-copilot-panel");
requireText(copilotPanel, "sourceReferences", "export-ai-copilot-panel");
requireText(copilotPanel, "getExportAiSourceTypeLabel", "export-ai-copilot-panel");
forbidText(copilotPanel, "chainOfThought", "export-ai-copilot-panel");

const migration = readRequired(
  repoRootUrl,
  "packages/NevaxisZentra.Persistence.Migrations/Migrations/20260808200000_AddExportAiCopilotAndRecommendations.cs",
);
requireText(migration, "prompt_version", "EXPORT-4L migration");
requireText(migration, "reasoning_summary", "EXPORT-4L migration");
requireText(migration, "source_references_json", "EXPORT-4L migration");
requireText(migration, "ck_export_ai_findings_source_references", "EXPORT-4L migration");
forbidText(migration, "chain_of_thought", "EXPORT-4L migration");

console.log("Export AI grounding verification passed.");

#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function read(rel) {
  const abs = resolve(ROOT, rel);
  if (!existsSync(abs)) throw new Error(`Missing ${rel}`);
  return readFileSync(abs, "utf8");
}

function assert(label, ok, detail = "") {
  if (!ok) {
    console.error(`✗ ${label}${detail ? `: ${detail}` : ""}`);
    process.exit(1);
  }
  console.log(`✓ ${label}`);
}

const models = read("packages/NevaxisFactOS.Persistence/Maintenance/MaintenanceAiAgentModels.cs");
const provider = read("packages/NevaxisFactOS.Persistence/Maintenance/DeterministicMaintenanceAiProvider.cs");
const endpoints = read("apps/api/Maintenance/MaintenanceAiEndpointRouteBuilderExtensions.cs");

assert("IMaintenanceAiProvider interface", models.includes("interface IMaintenanceAiProvider"));
assert("structured provider request", models.includes("MaintenanceAiProviderRequest"));
assert("structured provider result", models.includes("MaintenanceAiProviderResult"));
assert("prompt version field", models.includes("PromptVersion"));
assert("correlation identity support", models.includes("CorrelationId") || models.includes("correlationId"));
assert("development provider flag", provider.includes("IsDevelopmentProvider => true"));
assert("deterministic provider name", provider.includes("DeterministicDevelopment"));
assert("no OpenAI SDK", !provider.includes("OpenAI") && !provider.includes("Azure.AI"));
assert("no Anthropic SDK", !provider.includes("Anthropic"));
assert("DI registers IMaintenanceAiProvider", endpoints.includes("AddScoped<IMaintenanceAiProvider"));
assert("no generic AI execution endpoint", !endpoints.includes("/ai/execute"));
assert("narrow maintenance AI endpoints only", endpoints.includes("/ai-drafts/generate"));

console.log("MAINT-AI-1 AI provider boundary verifier PASS");

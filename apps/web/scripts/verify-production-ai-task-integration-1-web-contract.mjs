#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`PRODUCTION-AI-TASK-INTEGRATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running PRODUCTION-AI-TASK-INTEGRATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/uretim/istisnalar-ve-gorevler/page.tsx");
const view = read("apps/web/src/features/production-execution/components/production-follow-up-workspace-view.tsx");
const reader = read("apps/web/src/features/production-execution/server/production-follow-up-reader.ts");
const apiClient = read("apps/web/src/features/production-execution/server/production-follow-up-api-client.ts");
const contracts = read("apps/web/src/features/production-execution/server/production-follow-up-api-contracts.ts");
const actions = read("apps/web/src/features/production-execution/server/production-follow-up-actions.ts");
const labels = read("apps/web/src/features/production-execution/presentation/production-follow-up-labels.ts");
const domain = read("apps/web/src/features/production-execution/domain/production-follow-up.ts");
const sourceSection = read("apps/web/src/features/ai-task-center/components/ai-task-source-reference-section.tsx");
const endpoints = read("apps/api/Production/ProductionFollowUpEndpointRouteBuilderExtensions.cs");

assert("dedicated istisnalar-ve-gorevler page exists", page.includes("ProductionFollowUpWorkspaceView"));
assert("page requires production.execution.read", page.includes('forbidUnlessGranted("production.execution.read")'));

assert("workspace testid", view.includes('data-testid="production-follow-up-workspace"'));
assert("kpi grid testid", view.includes('data-testid="production-follow-up-kpi-grid"'));
assert("exception table testid", view.includes('data-testid="production-follow-up-table"'));
assert("detail ready testid", view.includes('data-testid="production-follow-up-detail-ready"'));
assert("create task button testid", view.includes('data-testid="production-follow-up-create-task-button"'));
assert("linked task link testid", view.includes('data-testid="production-follow-up-open-linked-task"'));
assert("automation disclaimer testid", view.includes('data-testid="production-follow-up-automation-disclaimer"'));
assert("insufficient evidence testid", view.includes('data-testid="production-follow-up-insufficient-evidence"'));
assert("exception url param", view.includes('searchParams.get("exception")') || page.includes("params.exception"));

assert("production review task label", labels.includes("Üretim inceleme görev taslağı oluştur"));
assert("data review task label", labels.includes("Veri inceleme görev taslağı oluştur"));
assert("linked task navigation", view.includes("/ai-gorevleri/"));
assert("acknowledge action", view.includes("production-follow-up-acknowledge-button"));
assert("acknowledge label", view.includes("Görüldü olarak işaretle"));
assert("turkish disclaimer", labels.includes("Otomatik üretim emri, makine, stok veya kalite kararı uygulanmaz."));

assert("reader workspace loader", reader.includes("getProductionFollowUpWorkspace"));
assert("reader exception detail", reader.includes("getProductionFollowUpDetail"));

assert("api client workspace path", apiClient.includes("/api/production/follow-up/workspace"));
assert("api client task draft path", apiClient.includes("/task-draft"));
assert("api client follow-up path", apiClient.includes("/follow-up"));
assert("contracts workspace payload parser", contracts.includes("parseProductionFollowUpWorkspacePayload"));
assert("contracts follow-up fields", contracts.includes("followUpStatus") && contracts.includes("linkedAiTaskId"));

assert("actions task draft permission", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("actions follow-up permission", actions.includes('denyUnlessGranted("production.execution.exception")'));
assert("labels follow-up statuses", labels.includes("requires-review"));

assert("domain follow-up status type", domain.includes("ProductionFollowUpStatus"));
assert("domain workspace payload type", domain.includes("ProductionFollowUpWorkspacePayload"));

assert("source section production link", sourceSection.includes('sourceModule === "production"'));
assert("source section production deep link", sourceSection.includes("/uretim/istisnalar-ve-gorevler?exception="));

assert("api task draft endpoint", endpoints.includes('MapPost("/follow-up/{exceptionId:guid}/task-draft"'));
assert("api workspace endpoint", endpoints.includes('MapGet("/follow-up/workspace"'));
assert("api source module production", endpoints.includes('ProductionFollowUpConstants.SourceModule'));

console.log("PRODUCTION-AI-TASK-INTEGRATION-1 web contract verification passed.");

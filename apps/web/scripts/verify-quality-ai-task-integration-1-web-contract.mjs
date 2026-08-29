#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`QUALITY-AI-TASK-INTEGRATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running QUALITY-AI-TASK-INTEGRATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/kalite/takip-ve-gorevler/page.tsx");
const view = read("apps/web/src/features/quality-control/components/quality-ncr-follow-up-workspace-view.tsx");
const reader = read("apps/web/src/features/quality-control/server/quality-ncr-follow-up-reader.ts");
const apiClient = read("apps/web/src/features/quality-control/server/quality-ncr-follow-up-api-client.ts");
const contracts = read("apps/web/src/features/quality-control/server/quality-ncr-follow-up-api-contracts.ts");
const actions = read("apps/web/src/features/quality-control/server/quality-ncr-follow-up-actions.ts");
const labels = read("apps/web/src/features/quality-control/presentation/quality-ncr-follow-up-labels.ts");
const domain = read("apps/web/src/features/quality-control/domain/quality-ncr-follow-up.ts");
const nav = read("apps/web/src/features/quality-control/components/quality-workspace-nav.tsx");
const sourceSection = read("apps/web/src/features/ai-task-center/components/ai-task-source-reference-section.tsx");
const endpoints = read("apps/api/Quality/QualityNcrFollowUpEndpointRouteBuilderExtensions.cs");

assert("dedicated takip-ve-gorevler page exists", page.includes("QualityNcrFollowUpWorkspaceView"));
assert("page requires quality.read", page.includes('forbidUnlessGranted("quality.read")'));
assert("navigation route present", nav.includes('href: "/kalite/takip-ve-gorevler"') && nav.includes("Takip ve Görevler"));

assert("workspace testid", view.includes('data-testid="quality-ncr-follow-up-workspace"'));
assert("kpi grid testid", view.includes('data-testid="quality-ncr-follow-up-kpi-grid"'));
assert("finding table testid", view.includes('data-testid="quality-ncr-follow-up-table"'));
assert("detail ready testid", view.includes('data-testid="quality-ncr-follow-up-detail-ready"'));
assert("create task button testid", view.includes('data-testid="quality-ncr-follow-up-create-task-button"'));
assert("linked task link testid", view.includes('data-testid="quality-ncr-follow-up-linked-task-link"'));
assert("automation disclaimer testid", view.includes('data-testid="quality-ncr-follow-up-automation-disclaimer"'));
assert("insufficient evidence testid", view.includes('data-testid="quality-ncr-follow-up-insufficient-evidence"'));
assert("finding url param", view.includes('searchParams.get("finding")') || page.includes("params.finding"));

assert("quality review task label", view.includes("Kalite inceleme görev taslağı oluştur"));
assert("data review task label", view.includes("Veri inceleme görev taslağı oluştur"));
assert("linked task navigation", view.includes("/ai-gorevleri/"));
assert("acknowledge action", view.includes("quality-ncr-follow-up-acknowledge-button"));

assert("reader workspace loader", reader.includes("getQualityNcrFollowUpWorkspace"));
assert("reader finding detail", reader.includes("getQualityNcrFollowUpDetail"));

assert("api client workspace path", apiClient.includes("/api/quality/ncr-follow-up/workspace"));
assert("api client task draft path", apiClient.includes("/task-draft"));
assert("api client follow-up path", apiClient.includes("/follow-up"));
assert("contracts workspace payload parser", contracts.includes("parseQualityNcrFollowUpWorkspacePayload"));
assert("contracts follow-up fields", contracts.includes("followUpStatus") && contracts.includes("linkedAiTaskId"));

assert("actions task draft permission", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("actions follow-up permission", actions.includes('denyUnlessGranted("quality.ncr.manage")'));
assert("labels follow-up statuses", labels.includes("requires-review"));
assert("labels task disclaimer", labels.includes("Otomatik üretim, stok veya kalite kararı uygulanmaz"));

assert("domain follow-up status type", domain.includes("QualityNcrFollowUpStatus"));
assert("domain workspace payload type", domain.includes("QualityNcrFollowUpWorkspacePayload"));

assert("source section quality link", sourceSection.includes('sourceModule === "quality"'));
assert("source section finding deep link", sourceSection.includes("/kalite/takip-ve-gorevler?finding="));

assert("api task draft endpoint", endpoints.includes('MapPost("/ncr-follow-up/{ncrId:guid}/task-draft"'));
assert("api workspace endpoint", endpoints.includes('MapGet("/ncr-follow-up/workspace"'));
assert("api source module quality", endpoints.includes('QualityFollowUpConstants.SourceModule'));

console.log("QUALITY-AI-TASK-INTEGRATION-1 web contract verification passed.");

#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MOLD-AI-TASK-INTEGRATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running MOLD-AI-TASK-INTEGRATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/kaliplar/takip-ve-gorevler/page.tsx");
const view = read("apps/web/src/features/molds/components/mold-follow-up-workspace-view.tsx");
const reader = read("apps/web/src/features/molds/server/mold-follow-up-reader.ts");
const apiClient = read("apps/web/src/features/molds/server/mold-follow-up-api-client.ts");
const contracts = read("apps/web/src/features/molds/server/mold-follow-up-api-contracts.ts");
const actions = read("apps/web/src/features/molds/server/mold-follow-up-actions.ts");
const labels = read("apps/web/src/features/molds/presentation/mold-follow-up-labels.ts");
const domain = read("apps/web/src/features/molds/domain/mold-follow-up.ts");
const sourceSection = read("apps/web/src/features/ai-task-center/components/ai-task-source-reference-section.tsx");
const endpoints = read("apps/api/Mold/MoldFollowUpEndpointRouteBuilderExtensions.cs");

assert("dedicated takip-ve-gorevler page exists", page.includes("MoldFollowUpWorkspaceView"));
assert("page requires mold.read", page.includes('forbidUnlessGranted("mold.read")'));

assert("workspace testid", view.includes('data-testid="mold-follow-up-workspace"'));
assert("kpi grid testid", view.includes('data-testid="mold-follow-up-kpi-grid"'));
assert("mold table testid", view.includes('data-testid="mold-follow-up-table"'));
assert("detail ready testid", view.includes('data-testid="mold-follow-up-detail-ready"'));
assert("create task button testid", view.includes('data-testid="mold-follow-up-create-task-button"'));
assert("linked task link testid", view.includes('data-testid="mold-follow-up-linked-task-link"'));
assert("automation disclaimer testid", view.includes('data-testid="mold-follow-up-automation-disclaimer"'));
assert("insufficient evidence testid", view.includes('data-testid="mold-follow-up-insufficient-evidence"'));
assert("mold url param", view.includes('searchParams.get("mold")') || page.includes("params.mold"));

assert("technical review task label", labels.includes("Kalıp teknik inceleme görev taslağı oluştur"));
assert("data review task label", labels.includes("Kalıp veri inceleme görev taslağı oluştur"));
assert("linked task navigation", view.includes("/ai-gorevleri/"));
assert("acknowledge action", view.includes("mold-follow-up-acknowledge-button"));
assert("acknowledge label", view.includes("Görüldü olarak işaretle"));

assert("reader workspace loader", reader.includes("getMoldFollowUpWorkspace"));
assert("reader mold detail", reader.includes("getMoldFollowUpDetail"));

assert("api client workspace path", apiClient.includes("/api/molds/follow-up/workspace"));
assert("api client task draft path", apiClient.includes("/task-draft"));
assert("api client follow-up path", apiClient.includes("/follow-up"));
assert("contracts workspace payload parser", contracts.includes("parseMoldFollowUpWorkspacePayload"));
assert("contracts follow-up fields", contracts.includes("followUpStatus") && contracts.includes("linkedAiTaskId"));

assert("actions task draft permission", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("actions follow-up permission", actions.includes('denyUnlessGranted("mold.manage")'));
assert("labels follow-up statuses", labels.includes("requires-review"));
assert("labels task disclaimer", labels.includes("Otomatik bakım emri, kalıp yaşam döngüsü veya üretim kararı uygulanmaz"));

assert("domain follow-up status type", domain.includes("MoldFollowUpStatus"));
assert("domain workspace payload type", domain.includes("MoldFollowUpWorkspacePayload"));

assert("source section mold link", sourceSection.includes('sourceModule === "mold"'));
assert("source section mold deep link", sourceSection.includes("/kaliplar/takip-ve-gorevler?mold="));

assert("api task draft endpoint", endpoints.includes('MapPost("/follow-up/{moldId:guid}/task-draft"'));
assert("api workspace endpoint", endpoints.includes('MapGet("/follow-up/workspace"'));
assert("api source module mold", endpoints.includes('MoldFollowUpConstants.SourceModule'));

console.log("MOLD-AI-TASK-INTEGRATION-1 web contract verification passed.");

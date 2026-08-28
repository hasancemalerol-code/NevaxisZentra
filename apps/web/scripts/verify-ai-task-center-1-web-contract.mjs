#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`AI-TASK-CENTER-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

const forbid = (source, forbidden, label) => {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
};

console.log("Running AI-TASK-CENTER-1 web contract verifier.");
const listPage = read("apps/web/src/app/(app)/ai-gorevleri/page.tsx");
const detailPage = read("apps/web/src/app/(app)/ai-gorevleri/[taskId]/page.tsx");
const createPage = read("apps/web/src/app/(app)/ai-gorevleri/yeni/page.tsx");
const queries = read("apps/web/src/features/ai-task-center/server/ai-task-queries.ts");
const actions = read("apps/web/src/features/ai-task-center/server/ai-task-actions.ts");
const apiClient = read("apps/web/src/features/ai-task-center/server/ai-task-api-client.ts");
const actionPanel = read("apps/web/src/features/ai-task-center/components/ai-task-action-panel-client.tsx");
const previewData = read("apps/web/src/features/ai-task-center/data/ai-task-preview-data.ts");

const home = read("apps/web/src/app/(app)/page.tsx");
assert("home uses list page content", home.includes("AiTaskListPageContent"));
forbid(home, "AI_TASK_PREVIEW_DATA", "home page");
forbid(listPage, "AI_TASK_PREVIEW_DATA", "list page");
forbid(detailPage, "AI_TASK_PREVIEW_DATA", "detail page");
forbid(detailPage, "getAiTaskPreviewById", "detail page");
forbid(detailPage, "generateStaticParams", "detail page");
forbid(createPage, "henüz kullanıma hazır değil", "create page stub");

assert("list route exists", listPage.includes("AiTaskListPageContent"));
assert("detail reads server data", detailPage.includes("readAiTaskDetail"));
assert("create form wired", createPage.includes("AiTaskCreateForm"));
assert("source reference section", detailPage.includes("AiTaskSourceReferenceSection"));
assert("audit timeline test id", detailPage.includes('data-testid="ai-task-audit-timeline"'));

assert("queries use ai-task.read", queries.includes('denyUnlessGranted("ai-task.read")'));
assert("create action uses ai-task.create", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("transition uses ai-task.review", actions.includes('denyUnlessGranted("ai-task.review")'));

assert("api client list endpoint", apiClient.includes("/api/ai-tasks"));
assert("api client transitions endpoint", apiClient.includes("/transitions"));
assert("api client source-linked endpoint", apiClient.includes("/source-linked"));

assert("action panel calls transition action", actionPanel.includes("transitionAiTaskAction"));
forbid(actionPanel, "Gerçek görev güncelleme servisi henüz bağlı değil", "action panel");
forbid(actionPanel, "yalnızca arayüz önizlemesidir", "action panel");
assert("reject note required client-side", actionPanel.includes("Ret gerekçesi zorunludur"));

assert("preview data marked fixture", previewData.includes("Isolated test fixture data"));

console.log("AI-TASK-CENTER-1 web contract verification passed.");

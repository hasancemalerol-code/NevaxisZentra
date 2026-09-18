#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`AI-TASK-CENTER-UNIFICATION-1 contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running AI-TASK-CENTER-UNIFICATION-1 contract verifier.");

const home = read("apps/web/src/app/(app)/page.tsx");
const listPage = read("apps/web/src/app/(app)/ai-gorevleri/page.tsx");
const inboxRedirect = read("apps/web/src/app/(app)/teams/is-kutum/page.tsx");
const listContent = read("apps/web/src/features/ai-task-center/components/ai-task-list-page-content.tsx");
const unifiedWorkspace = read("apps/web/src/features/ai-task-center/components/ai-task-center-unified-workspace.tsx");
const composition = read("apps/web/src/features/ai-task-center/application/ai-task-center-composition.ts");
const centerQueries = read("apps/web/src/features/ai-task-center/server/ai-task-center-queries.ts");
const centerItem = read("apps/web/src/features/ai-task-center/domain/ai-task-center-item.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const dashboard = read("apps/web/src/app/(app)/dashboard/page.tsx");
const workInboxRepo = read("packages/NevaxisZentra.Persistence/Teams/TeamsWorkInboxRepository.cs");
const originResolver = read("packages/NevaxisZentra.Persistence/AiTasks/AiTaskListOriginResolver.cs");
const nextEnv = read("apps/web/next-env.d.ts");

assert("home uses unified list content", home.includes("AiTaskListPageContent"));
assert("alias route uses unified list content", listPage.includes("AiTaskListPageContent"));
assert("center name preserved", listContent.includes("AI Görev Merkezi"));
assert("product explanation", listContent.includes("AI destekli birleşik görev ve aksiyon yönetimi"));
assert("unified query", centerQueries.includes("readAiTaskCenterUnified"));
assert("reuses work inbox query", centerQueries.includes("readTeamsWorkInbox"));
assert("reuses ai task list", centerQueries.includes("listAiTasks"));
assert("no new persistence migration", !centerQueries.includes("migration") && !centerQueries.includes("ExecuteWriteAsync"));

assert("five views", centerItem.includes("Bana Atananlar") && centerItem.includes("Tüm Görevler"));
assert("approvals handovers view", centerItem.includes("Onay ve Teslimler"));
assert("mentions view", centerItem.includes("Etiketlemeler"));
assert("completed view", centerItem.includes("Tamamlananlar"));

assert("provable teams filter only", centerItem.includes("Teams mesajından oluşturulan"));
assert("ai filter removed", !centerItem.includes("AI tarafından oluşturulan"));
assert("manual filter removed", !centerItem.includes("Manuel / operasyonel"));

assert("source labels", centerItem.includes("AI Analizi") && centerItem.includes("Manuel Görev"));
assert("neutral labels", centerItem.includes("Operasyonel Görev") && centerItem.includes("Kaynak belirtilmemiş"));
assert("teams source label", centerItem.includes("Teams Mesajı"));
assert("approval label", centerItem.includes("Onay Talebi"));
assert("handover label", centerItem.includes("Vardiya Teslimi"));
assert("mention label", centerItem.includes("Etiketleme"));

assert("authoritative origin resolver", originResolver.includes("AiTaskListOriginResolver"));
assert("teams linkage proof", originResolver.includes("TeamsConstants.TeamsSourceModule"));
assert("manual source type proof", originResolver.includes('"manual"'));
assert("ai model version proof", originResolver.includes("ModelVersion"));
assert("no sourceModule-only manual inference", !composition.includes("non-empty") && !composition.includes("classifyTaskOrigin"));

assert("dedupe composition", composition.includes("composeAiTaskCenterItems"));
assert("skip duplicate inbox tasks", composition.includes('inboxItem.sourceKind === "ai-task"'));
assert("teams-only origin filter", composition.includes('item.listOriginKind === "teams-message"'));
assert("dynamic facility column", unifiedWorkspace.includes("showFacilityColumn"));
assert("dynamic assignee column", unifiedWorkspace.includes("showAssigneeColumn"));
assert("work inbox dedup reused", workInboxRepo.includes("GroupBy(item => (item.SourceKind, item.SourceId))"));

assert("unified workspace testid", unifiedWorkspace.includes("ai-task-center-unified-workspace"));
assert("row testid pattern", unifiedWorkspace.includes("ai-task-center-item-"));
assert("deep link column", unifiedWorkspace.includes("item.deepLinkPath"));

assert("legacy inbox redirect", inboxRedirect.includes("redirect(") && inboxRedirect.includes("/?gorunum="));

assert("single nav entry", navigation.match(/AI Görev Merkezi/g)?.length === 1);
assert("nav href home", navigation.includes('label: "AI Görev Merkezi"') && navigation.includes('href: "/"'));

assert("dashboard no operasyon oncelikleri", !dashboard.includes("Operasyon Öncelikleri"));
assert("dashboard no operasyon-oncelikleri-panel", !dashboard.includes("operasyon-oncelikleri-panel"));
assert("piyasa ozeti unchanged", dashboard.includes("PiyasaOzetiPanel"));

assert("next-env matches tracked template", nextEnv.includes('reference types="next"') && nextEnv.includes("NOTE: This file should not be edited"));
assert("next-env not build-only artifact", !nextEnv.includes("next/types/global") || nextEnv.includes("next/image-types/global"));

console.log("AI-TASK-CENTER-UNIFICATION-1 contract verification passed.");

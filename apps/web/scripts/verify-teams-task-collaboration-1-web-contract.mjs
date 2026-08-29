#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-TASK-COLLABORATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-TASK-COLLABORATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/teams/page.tsx");
const client = read("apps/web/src/features/teams/components/teams-workspace-client.tsx");
const draftForm = read("apps/web/src/features/teams/components/teams-message-task-draft-form.tsx");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");
const queries = read("apps/web/src/features/teams/server/teams-queries.ts");
const sourceLink = read("apps/web/src/features/teams/components/ai-task-teams-message-source-link.tsx");
const sourceSection = read("apps/web/src/features/ai-task-center/components/ai-task-source-reference-section.tsx");
const taskPage = read("apps/web/src/app/(app)/ai-gorevleri/[taskId]/page.tsx");
const constants = read("apps/web/src/features/teams/domain/teams-constants.ts");
const endpoints = read("apps/api/Teams/TeamsEndpointRouteBuilderExtensions.cs");

  assert("teams page message param", page.includes("message?: string"));
  assert("channel loading testid", client.includes("teams-selected-channel-loading"));
assert("canCreateTask wiring", page.includes("canCreateTask"));
assert("selectedMessageId wiring", page.includes("selectedMessageId"));

assert("message deep-link selection", client.includes("&message="));
assert("convert to task action", client.includes("Göreve dönüştür"));
assert("linked task open label", client.includes("Bağlı görevi aç"));
assert("permission denied state", client.includes("teams-message-task-create-denied"));
assert("message select testid", client.includes("teams-message-select-"));
assert("convert action testid", client.includes("teams-message-convert-to-task"));
assert("linked task testid", client.includes("teams-message-linked-task-"));

assert("draft form testid", draftForm.includes("teams-message-task-draft-form"));
assert("draft error testid", draftForm.includes("teams-message-task-draft-error"));
assert("immutable source preview", draftForm.includes("teams-message-task-source-preview"));
assert("server action not direct api in form", draftForm.includes("createMessageTaskDraftAction"));

assert("server action create draft", actions.includes("createMessageTaskDraftAction"));
assert("ai-task.create gate", actions.includes('denyUnlessGranted("ai-task.create")'));
assert("teams.read gate", actions.includes('denyUnlessGranted("teams.read")'));

assert("api client task draft endpoint", apiClient.includes("/task-draft"));
assert("api client source context endpoint", apiClient.includes("/source-context"));
assert("linked task type", apiClient.includes("linkedTask"));

assert("message source query", queries.includes("readTeamsMessageSourceContext"));

assert("teams message source link", sourceLink.includes("Teams bağlamını aç"));
assert("unavailable state", sourceLink.includes("ai-task-teams-message-source-unavailable"));
assert("teams source label", sourceSection.includes('teams: "Teams mesajı"'));
assert("task page teams message link", taskPage.includes("AiTaskTeamsMessageSourceLink"));

assert("disclaimer constant", constants.includes("Görev taslağı yalnızca insan incelemesi için oluşturulur"));
assert("api messages route family", endpoints.includes('/api/teams/messages'));

console.log("TEAMS-TASK-COLLABORATION-1 web contract verification passed.");

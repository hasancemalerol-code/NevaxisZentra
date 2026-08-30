#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-SHIFT-HANDOVER-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-SHIFT-HANDOVER-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/teams/page.tsx");
const client = read("apps/web/src/features/teams/components/teams-workspace-client.tsx");
const createPage = read("apps/web/src/app/(app)/teams/devretme/yeni/page.tsx");
const listPage = read("apps/web/src/app/(app)/teams/devretme/page.tsx");
const detailPage = read("apps/web/src/app/(app)/teams/devretme/[handoverId]/page.tsx");
const form = read("apps/web/src/features/teams/components/teams-handover-form.tsx");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const actionPanel = read("apps/web/src/features/teams/components/teams-handover-actions.tsx");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");
const queries = read("apps/web/src/features/teams/server/teams-queries.ts");
const constants = read("apps/web/src/features/teams/domain/teams-constants.ts");
const endpoints = read("apps/api/Teams/TeamsHandoverEndpointRouteBuilderExtensions.cs");

assert("create permission wiring", page.includes("teams.handover.create"));
assert("read permission wiring", page.includes("teams.handover.read"));
assert("canCreateHandover prop", client.includes("canCreateHandover"));
assert("create handover action", client.includes("Vardiya devri oluştur"));
assert("message handover testid", client.includes("teams-message-create-handover"));
assert("create page gate", createPage.includes('forbidUnlessGranted("teams.handover.create")'));
assert("create page source access", createPage.includes("readAiTaskDetail"));
assert("list page handover.read", listPage.includes('forbidUnlessGranted("teams.handover.read")'));
assert("detail page source link", detailPage.includes("teams-handover-item-source-"));
assert("form testid", form.includes("teams-handover-create-form"));
assert("form server action", form.includes("createTeamsHandoverAction"));
assert("no client actor id", !form.includes("authorId") && !form.includes("actorId"));
assert("publish action", actionPanel.includes("publishTeamsHandoverAction"));
assert("acknowledge action", actionPanel.includes("acknowledgeTeamsHandoverAction"));
assert("create action permission", actions.includes('denyUnlessGranted("teams.handover.create")'));
assert("acknowledge action permission", actions.includes('denyUnlessGranted("teams.handover.acknowledge")'));
assert("api list", apiClient.includes("/api/teams/handovers"));
assert("api publish", apiClient.includes("/publish"));
assert("api acknowledgements", apiClient.includes("/acknowledgements"));
assert("query list", queries.includes("listTeamsHandovers"));
assert("turkish disclaimer", constants.includes("yalnızca operasyon bağlamı kaydeder"));
assert("endpoint family", endpoints.includes("/api/teams/handovers"));
assert("no delete endpoint", !endpoints.includes("MapDelete"));
assert("turkish routes", listPage.includes("/teams/devretme") && createPage.includes("Vardiya devri oluştur"));

console.log("TEAMS-SHIFT-HANDOVER-1 web contract verification passed.");

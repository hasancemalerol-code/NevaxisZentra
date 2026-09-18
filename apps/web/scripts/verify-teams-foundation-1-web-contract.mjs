#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-FOUNDATION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-FOUNDATION-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/teams/page.tsx");
const client = read("apps/web/src/features/teams/components/teams-workspace-client.tsx");
const css = read("apps/web/src/features/teams/components/teams-workspace.css");
const queries = read("apps/web/src/features/teams/server/teams-queries.ts");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const taskSection = read("apps/web/src/features/teams/components/ai-task-teams-context-section.tsx");
const taskPage = read("apps/web/src/app/(app)/ai-gorevleri/[taskId]/page.tsx");
const sections = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const subscription = read("apps/web/src/features/saas-subscription/domain/saas-subscription-labels.ts");
const backendEntitlements = read("packages/NevaxisZentra.Persistence/SaasSubscription/SaasSubscriptionModuleEntitlements.cs");
const endpoints = read("apps/api/Teams/TeamsEndpointRouteBuilderExtensions.cs");
const constants = read("packages/NevaxisZentra.Persistence/Teams/TeamsConstants.cs");

assert("dedicated /teams page", page.includes("TeamsWorkspaceClient"));
assert("teams.read gate", page.includes('forbidUnlessGranted("teams.read")'));
assert("teams page force dynamic", page.includes('export const dynamic = "force-dynamic"'));
assert("canWrite prop wiring", page.includes("canWrite={canWrite}"));
assert("catch-all excludes /teams", sections.includes('item.href !== "/teams"'));

assert("workspace testid", client.includes('data-testid="teams-workspace"'));
assert("channel rail testid", client.includes('data-testid="teams-channel-rail"'));
assert("timeline testid", client.includes('data-testid="teams-message-timeline"'));
assert("composer testid", client.includes('data-testid="teams-message-composer"'));
assert("composer input testid", client.includes('data-testid="teams-composer-input"'));
assert("selected channel ready testid", client.includes('data-testid="teams-selected-channel-ready"'));
assert("composer error testid", client.includes('data-testid="teams-composer-error"'));
assert("composer pending testid", client.includes('data-testid="teams-composer-pending"'));
assert("read-only notice testid", client.includes('data-testid="teams-read-only-notice"'));
assert("send button testid", client.includes('data-testid="teams-send-message"'));
assert("mention chooser", client.includes("Etiketle:"));
assert("okundu label", client.includes("Okundu:"));
assert("gorevi ac link", client.includes("Görevi aç"));
assert("turkish disclaimer in api", constants.includes("mesaj göndermek otomatik"));

assert("queries list channels", queries.includes("listTeamsChannels"));
assert("queries channel detail", queries.includes("getTeamsChannelDetail"));
assert("queries session context", queries.includes("resolveTeamsRequestContext"));
assert("api client channels path", apiClient.includes("/api/teams/channels"));
assert("api client session fetch", apiClient.includes("fetchTrustedApi"));
assert("actions teams.write", actions.includes('denyUnlessGranted("teams.write")'));

assert("ai task teams link section", taskSection.includes("Teams bağlamını aç"));
assert("task page includes teams section", taskPage.includes("AiTaskTeamsContextSection"));
assert("teams context testid", taskSection.includes('data-testid="ai-task-teams-context-link"'));

assert("subscription /teams mapping web", subscription.includes('prefix: "/teams"') && subscription.includes("module.teams"));
assert("subscription /teams mapping backend", backendEntitlements.includes('("/teams", EntitlementCodes.Teams)'));

assert("api route family", endpoints.includes('/api/teams/channels'));
assert("responsive workspace css", css.includes("@media"));

console.log("TEAMS-FOUNDATION-1 web contract verification passed.");

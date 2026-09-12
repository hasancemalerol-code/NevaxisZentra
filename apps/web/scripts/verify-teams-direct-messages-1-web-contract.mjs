#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function assert(label, ok) {
  if (!ok) {
    console.error(`TEAMS-DIRECT-MESSAGES-1 web contract failed: ${label}`);
    process.exit(1);
  }
  console.log(`✓ ${label}`);
}

function read(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-DIRECT-MESSAGES-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/teams/ozel-mesajlar/page.tsx");
const client = read("apps/web/src/features/teams/components/teams-direct-messages-client.tsx");
const endpoints = read("apps/api/Teams/TeamsDirectMessageEndpointRouteBuilderExtensions.cs");
const repository = read("packages/NevaxisFactOS.Persistence/Teams/TeamsDirectMessageRepository.cs");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");

assert("turkish route page", page.includes("Özel mesajlar"));
assert("direct.read gate", page.includes('forbidUnlessGranted("teams.direct.read")'));
assert("index link", read("apps/web/src/app/(app)/teams/page.tsx").includes("teams-direct-messages-index-link"));
assert("conversation list testid", client.includes("teams-direct-conversation-list"));
assert("message thread testid", client.includes("teams-direct-message-thread"));
assert("disclaimer", client.includes("teams-direct-messages-disclaimer"));
assert("api path", apiClient.includes("/api/teams/direct-messages"));
assert("no delete action", !actions.includes("DELETE") && !repository.includes("Remove("));
assert("GET only for list in endpoints", endpoints.includes('group.MapGet("/", ListConversationsAsync)'));
assert("post message route", endpoints.includes("/messages"));
assert("mark read route", endpoints.includes("/read"));

console.log("TEAMS-DIRECT-MESSAGES-1 web contract verification passed.");

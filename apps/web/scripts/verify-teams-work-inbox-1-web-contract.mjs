#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-WORK-INBOX-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-WORK-INBOX-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/teams/page.tsx");
const inboxPage = read("apps/web/src/app/(app)/teams/is-kutum/page.tsx");
const client = read("apps/web/src/features/teams/components/teams-workspace-client.tsx");
const queries = read("apps/web/src/features/teams/server/teams-queries.ts");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const constants = read("apps/web/src/features/teams/domain/teams-constants.ts");
const endpoints = read("apps/api/Teams/TeamsWorkInboxEndpointRouteBuilderExtensions.cs");
const repository = read("packages/NevaxisFactOS.Persistence/Teams/TeamsWorkInboxRepository.cs");

assert("inbox index link", page.includes("teams-work-inbox-index-link"));
assert("workspace inbox link", client.includes("teams-channel-work-inbox-link"));
assert("turkish route", inboxPage.includes("İş kutum"));
assert("list page teams.read", inboxPage.includes('forbidUnlessGranted("teams.read")'));
assert("inbox list testid", inboxPage.includes("teams-work-inbox-list"));
assert("item deep link", inboxPage.includes("item.deepLinkPath"));
assert("no mutation controls on page", !inboxPage.includes("Action") && !inboxPage.includes("acknowledge") && !inboxPage.includes("decide"));
assert("server query", queries.includes("getTeamsWorkInbox"));
assert("api path", apiClient.includes("/api/teams/work-inbox"));
assert("no inbox write action", !actions.includes("work-inbox") && !actions.includes("WorkInbox"));
assert("disclaimer", constants.includes("yalnızca mevcut kaynaklardaki size ait işleri"));
assert("GET only endpoint", endpoints.includes('MapGet("/", GetInboxAsync)') && !endpoints.includes("MapPost") && !endpoints.includes("MapDelete"));
assert("source permission gates", repository.includes("CanReadAiTasks") && repository.includes("CanReviewApprovals") && repository.includes("CanAcknowledgeHandovers"));
assert("dedup", repository.includes("GroupBy(item => (item.SourceKind, item.SourceId))"));
assert("no task lifecycle", !repository.includes("TransitionTaskAsync") && !repository.includes("CreateSourceLinkedTaskAsync"));

console.log("TEAMS-WORK-INBOX-1 web contract verification passed.");

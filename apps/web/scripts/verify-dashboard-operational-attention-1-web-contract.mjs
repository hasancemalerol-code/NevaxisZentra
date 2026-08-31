#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`DASHBOARD-OPERATIONAL-ATTENTION-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running DASHBOARD-OPERATIONAL-ATTENTION-1 web contract verifier.");

const dashboardPage = read("apps/web/src/app/(app)/dashboard/page.tsx");
const panel = read("apps/web/src/components/dashboard/operasyon-oncelikleri-panel.tsx");
const queries = read("apps/web/src/features/teams/server/teams-queries.ts");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const constants = read("apps/web/src/features/teams/domain/teams-constants.ts");
const endpoints = read("apps/api/Teams/TeamsWorkInboxEndpointRouteBuilderExtensions.cs");
const repository = read("packages/CrevaFactOS.Persistence/Teams/TeamsWorkInboxRepository.cs");

assert("dashboard panel mounted", dashboardPage.includes("OperasyonOncelikleriPanel"));
assert("action context strip placement", dashboardPage.includes("command-center__action-context-strip"));
assert("piyasa ozeti placement preserved", dashboardPage.includes("command-center__titlebar-context-strip") && dashboardPage.includes("PiyasaOzetiPanel"));
assert("turkish panel title", panel.includes("Operasyon Öncelikleri"));
assert("panel test id", panel.includes("dashboard-operasyon-oncelikleri-panel"));
assert("max five items", panel.includes("MAX_PRIORITY_ITEMS = 5") && panel.includes(".slice(0, MAX_PRIORITY_ITEMS)"));
assert("reuse work inbox query", panel.includes("readTeamsWorkInbox"));
assert("no new dashboard inbox endpoint", !apiClient.includes("/api/dashboard/operational-attention"));
assert("source labels", panel.includes("TEAMS_WORK_INBOX_SOURCE_KINDS"));
assert("deep link rows", panel.includes("item.deepLinkPath"));
assert("inbox footer link", panel.includes('href={WORK_INBOX_HREF}') && panel.includes("Tüm iş kutumu aç"));
assert("turkish empty state", panel.includes("bekleyen iş yok"));
assert("no fake priority counts", !panel.includes("placeholder") && !panel.includes("fake"));
assert("no mutation controls", !panel.includes("Action") && !panel.includes("acknowledge") && !panel.includes("decide") && !panel.includes("markRead"));
assert("server query contract", queries.includes("getTeamsWorkInbox"));
assert("api path", apiClient.includes("/api/teams/work-inbox"));
assert("no inbox write action", !actions.includes("work-inbox") && !actions.includes("WorkInbox"));
assert("disclaimer reuse", constants.includes("yalnızca mevcut kaynaklardaki size ait işleri"));
assert("GET only endpoint", endpoints.includes('MapGet("/", GetInboxAsync)') && !endpoints.includes("MapPost"));
assert("source permission gates", repository.includes("CanReadAiTasks") && repository.includes("CanReviewApprovals") && repository.includes("CanAcknowledgeHandovers"));
assert("no duplicate persistence", !repository.includes("operational_attention") && !repository.includes("OperationalAttention"));

console.log("DASHBOARD-OPERATIONAL-ATTENTION-1 web contract verification passed.");

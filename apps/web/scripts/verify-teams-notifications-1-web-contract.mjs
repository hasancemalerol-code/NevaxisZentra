#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-NOTIFICATIONS-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-NOTIFICATIONS-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/teams/bildirimler/page.tsx");
const teamsPage = read("apps/web/src/app/(app)/teams/page.tsx");
const client = read("apps/web/src/features/teams/components/teams-notifications-client.tsx");
const shell = read("apps/web/src/components/app-shell/notifications-control.tsx");
const layout = read("apps/web/src/app/(app)/layout.tsx");
const queries = read("apps/web/src/features/teams/server/teams-queries.ts");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const constants = read("apps/web/src/features/teams/domain/teams-constants.ts");
const endpoints = read("apps/api/Teams/TeamsNotificationEndpointRouteBuilderExtensions.cs");
const repository = read("packages/CrevaFactOS.Persistence/Teams/TeamsNotificationRepository.cs");

assert("turkish route page", page.includes("Bildirim merkezi"));
assert("notifications.read gate", page.includes('forbidUnlessGranted("teams.notifications.read")'));
assert("teams index link", teamsPage.includes("teams-notifications-index-link"));
assert("shell link enabled", shell.includes('/teams/bildirimler') && !shell.includes("disabled"));
assert("shell unread badge", shell.includes("shell-notifications-unread-badge"));
assert("layout unread count", layout.includes("NotificationsControlServer"));
assert("server notifications control", read("apps/web/src/components/app-shell/notifications-control-server.tsx").includes("readTeamsNotifications"));
assert("list testid", page.includes("teams-notifications-list") || client.includes("teams-notifications-list"));
assert("disclaimer", constants.includes("Bildirimler yalnızca erişebildiğiniz Teams kaynaklarına yönlendirir"));
assert("server query", queries.includes("readTeamsNotifications"));
assert("mark read action only mutation", actions.includes("markTeamsNotificationReadAction"));
assert("unread count action", actions.includes("readTeamsNotificationUnreadCountAction"));
assert("no notification create action", !actions.includes("createTeamsNotification"));
assert("api path", apiClient.includes("/api/teams/notifications"));
assert("GET list endpoint", endpoints.includes('MapGet("/", ListNotificationsAsync)'));
assert("POST read endpoint", endpoints.includes("MapPost"));
assert("source access gate on read", repository.includes("CanOpen"));
assert("no external push", !repository.includes("email") && !shell.includes("push"));

console.log("TEAMS-NOTIFICATIONS-1 web contract verification passed.");

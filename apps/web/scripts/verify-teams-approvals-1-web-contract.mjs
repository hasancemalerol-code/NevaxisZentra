#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`TEAMS-APPROVALS-1 web contract verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

console.log("Running TEAMS-APPROVALS-1 web contract verifier.");

const page = read("apps/web/src/app/(app)/teams/page.tsx");
const client = read("apps/web/src/features/teams/components/teams-workspace-client.tsx");
const createPage = read("apps/web/src/app/(app)/teams/onaylar/yeni/page.tsx");
const listPage = read("apps/web/src/app/(app)/teams/onaylar/page.tsx");
const detailPage = read("apps/web/src/app/(app)/teams/onaylar/[requestId]/page.tsx");
const form = read("apps/web/src/features/teams/components/teams-approval-request-form.tsx");
const decision = read("apps/web/src/features/teams/components/teams-approval-decision-form.tsx");
const actions = read("apps/web/src/features/teams/server/teams-actions.ts");
const apiClient = read("apps/web/src/features/teams/server/teams-api-client.ts");
const queries = read("apps/web/src/features/teams/server/teams-queries.ts");
const constants = read("apps/web/src/features/teams/domain/teams-constants.ts");
const endpoints = read("apps/api/Teams/TeamsApprovalEndpointRouteBuilderExtensions.cs");
const taskSection = read("apps/web/src/features/teams/components/ai-task-teams-context-section.tsx");

assert("create permission wiring", page.includes("teams.approvals.create"));
assert("canCreateApproval prop", client.includes("canCreateApproval"));
assert("Onay iste action", client.includes("Onay iste"));
assert("message approval testid", client.includes("teams-message-request-approval"));
assert("create page gate", createPage.includes('forbidUnlessGranted("teams.approvals.create")'));
assert("list page teams.read", listPage.includes('forbidUnlessGranted("teams.read")'));
assert("detail page source link", detailPage.includes("teams-approval-source-link"));
assert("form testid", form.includes("teams-approval-request-form"));
assert("form server action", form.includes("createTeamsApprovalAction"));
assert("no client actor id", !form.includes("requestedBy") && !form.includes("actorId"));
assert("decision approved", decision.includes("teams-approval-decide-approved"));
assert("decision rejected", decision.includes("teams-approval-decide-rejected"));
assert("decision changes", decision.includes("teams-approval-decide-changes"));
assert("cancel action", decision.includes("cancelTeamsApprovalAction"));
assert("create action permission", actions.includes('denyUnlessGranted("teams.approvals.create")'));
assert("review action permission", actions.includes('denyUnlessGranted("teams.approvals.review")'));
assert("manage action permission", actions.includes('denyUnlessGranted("teams.approvals.manage")'));
assert("api list", apiClient.includes("/api/teams/approvals"));
assert("api decide", apiClient.includes("/decisions"));
assert("query list", queries.includes("listTeamsApprovals"));
assert("turkish disclaimer", constants.includes("yalnızca insan kararı kaydeder"));
assert("endpoint family", endpoints.includes('/api/teams/approvals'));
assert("no delete endpoint", !endpoints.includes("MapDelete"));
assert("task approval entry", taskSection.includes("ai-task-request-approval"));

console.log("TEAMS-APPROVALS-1 web contract verification passed.");

#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const URETIM_CHANNEL = "bb000001-03d1-4000-8000-0000000003d1";
const REVIEWER_MEMBERSHIP = "bb000001-0021-4000-8000-000000000021";
const OWNER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const REVIEWER = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };
const NEGATIVE = { issuer: "crevafactos-dev", subject: "quality-negative-test-user", tenant: "dev-pilot-tenant" };
const RUN_SUFFIX = Date.now();

function fail(message) {
  console.error(`DASHBOARD-OPERATIONAL-ATTENTION-1 security verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(OWNER)) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json, text };
}

console.log("Running DASHBOARD-OPERATIONAL-ATTENTION-1 security verifier.");

const panel = readRequired("apps/web/src/components/dashboard/operasyon-oncelikleri-panel.tsx");
const endpoints = readRequired("apps/api/Teams/TeamsWorkInboxEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Teams/TeamsWorkInboxRepository.cs");
const helpers = readRequired("apps/api/Teams/TeamsEndpointHelpers.cs");

assert("panel read-only links only", panel.includes("<Link") && !panel.includes("type=\"submit\"") && !panel.includes("teams-actions"));
assert("panel hides items on failure", panel.includes('result.status === "failure"') && panel.includes("unavailable"));
assert("panel caps items", panel.includes("slice(0, MAX_PRIORITY_ITEMS)"));
assert("inbox route reused", endpoints.includes("/api/teams/work-inbox"));
assert("read resolver", endpoints.includes("ResolveReadAccessAsync"));
assert("capability resolver", helpers.includes("ResolveWorkInboxCapabilitiesAsync"));
assert("denied source omitted", repository.includes("if (capabilities.CanReadAiTasks)") && repository.includes("if (capabilities.CanReviewApprovals)"));
assert("no write methods", !repository.includes("ExecuteWriteAsync"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauth = await liveRequest("GET", `/api/teams/work-inbox?facilityId=${FACILITY}`, null, {});
assert("unauthenticated inbox 401", unauth.status === 401);

const mention = await liveRequest(
  "POST",
  `/api/teams/channels/${URETIM_CHANNEL}/messages?facilityId=${FACILITY}`,
  {
    body: `Operasyon öncelik etiket ${RUN_SUFFIX}`,
    idempotencyKey: `dashboard-attention-mention-${RUN_SUFFIX}`,
    mentionedMembershipIds: [REVIEWER_MEMBERSHIP],
  },
);
assert("mention seeded", mention.status === 200 || mention.status === 201);
const mentionId = mention.json?.id;
assert("mention id", Boolean(mentionId));

const approval = await liveRequest(
  "POST",
  `/api/teams/approvals?facilityId=${FACILITY}`,
  {
    channelId: URETIM_CHANNEL,
    messageId: mentionId,
    title: `Operasyon öncelik onay ${RUN_SUFFIX}`,
    rationale: "Dashboard öncelik kanıtı.",
    idempotencyKey: `dashboard-attention-approval-${RUN_SUFFIX}`,
    reviewers: [{ tenantMembershipId: REVIEWER_MEMBERSHIP, role: "reviewer" }],
  },
);
assert("approval seeded", approval.status === 200 || approval.status === 201, approval.text?.slice(0, 200));
const approvalId = approval.json?.id;

const reviewerInbox = await liveRequest("GET", `/api/teams/work-inbox?facilityId=${FACILITY}`, null, actorHeaders(REVIEWER));
assert("reviewer can read inbox", reviewerInbox.status === 200, reviewerInbox.text?.slice(0, 200));
const reviewerItems = reviewerInbox.json?.items ?? [];
assert("reviewer sees mention", reviewerItems.some((item) => item.sourceKind === "mention" && item.sourceId === mentionId));
assert("reviewer sees approval", reviewerItems.some((item) => item.sourceKind === "approval" && item.sourceId === approvalId));
assert("dashboard cap compatible", reviewerItems.length <= 50);

const negative = await liveRequest("GET", `/api/teams/work-inbox?facilityId=${FACILITY}`, null, actorHeaders(NEGATIVE));
assert("non-member denied or empty", negative.status === 403 || negative.status === 200);
if (negative.status === 200) {
  const leaked = (negative.json?.items ?? []).some((item) =>
    item.sourceId === mentionId || item.sourceId === approvalId);
  assert("non-member does not see channel-scoped items", !leaked);
  const text = JSON.stringify(negative.json ?? {});
  assert("non-member response has no seeded titles", !text.includes(`Operasyon öncelik onay ${RUN_SUFFIX}`));
  assert("non-member response has no seeded deep links", !text.includes(mentionId) && !text.includes(approvalId));
}

const mutate = await liveRequest("POST", `/api/teams/work-inbox?facilityId=${FACILITY}`, { sourceId: mentionId });
assert("inbox rejects mutation", mutate.status === 404 || mutate.status === 405 || mutate.status === 401);

console.log("DASHBOARD-OPERATIONAL-ATTENTION-1 security verification passed.");

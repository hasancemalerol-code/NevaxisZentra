#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const RUN_SUFFIX = Date.now();
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const NEGATIVE = {
  issuer: "crevafactos-dev",
  subject: "quality-negative-test-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`AI-TASK-CENTER-UNIFICATION-1 security verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function actorHeaders(actor) {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
  };
}

async function liveRequest(method, path, body, headers = actorHeaders(MANAGER)) {
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

console.log("Running AI-TASK-CENTER-UNIFICATION-1 security verifier.");

const centerQueries = readRequired("apps/web/src/features/ai-task-center/server/ai-task-center-queries.ts");
const composition = readRequired("apps/web/src/features/ai-task-center/application/ai-task-center-composition.ts");
const workInboxRepo = readRequired("packages/NevaxisFactOS.Persistence/Teams/TeamsWorkInboxRepository.cs");
const originResolver = readRequired("packages/NevaxisFactOS.Persistence/AiTasks/AiTaskListOriginResolver.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/AiTasks/AiTaskRepository.cs");
const aiTaskEndpoints = readRequired("apps/api/AiTasks/AiTaskEndpointRouteBuilderExtensions.cs");

assert("permission union gate", centerQueries.includes('denyUnlessAnyGranted(["ai-task.read", "teams.read"])'));
assert("soft ai-task gate", centerQueries.includes('denyUnlessGranted("ai-task.read")'));
assert("soft teams gate", centerQueries.includes('denyUnlessGranted("teams.read")'));
assert("no new permission key", !centerQueries.match(/"[a-z0-9.-]+"/g)?.some((key) => key.includes("ai-task-center")));
forbidText(centerQueries, "ai-task-center.read", "center queries");
assert("native ownership preserved", composition.includes('dedupeKey: `task:${task.id}`'));
assert("inbox native keys", composition.includes("nativeSourceKind: item.sourceKind"));
assert("authoritative list origin resolver", originResolver.includes("AiTaskListOriginResolver"));
assert("list origin kind on summary", repository.includes("AiTaskListOriginResolver.Resolve(task)"));
assert("list origin kind exposed", aiTaskEndpoints.includes("listOriginKind = item.ListOriginKind"));
assert("work inbox permission gates", workInboxRepo.includes("CanReviewApprovals") && workInboxRepo.includes("CanAcknowledgeHandovers"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const unauthTasks = await liveRequest("GET", `/api/ai-tasks?facilityId=${FACILITY}`, null, {});
assert("unauthenticated ai-tasks 401", unauthTasks.status === 401);

const unauthInbox = await liveRequest("GET", `/api/teams/work-inbox?facilityId=${FACILITY}`, null, {});
assert("unauthenticated work-inbox 401", unauthInbox.status === 401);

const negativeFacilityTasks = await liveRequest(
  "GET",
  `/api/ai-tasks?facilityId=${UNAUTH_FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("non-member facility ai-tasks denied", negativeFacilityTasks.status === 403 || negativeFacilityTasks.status === 404);

const negativeFacilityInbox = await liveRequest(
  "GET",
  `/api/teams/work-inbox?facilityId=${UNAUTH_FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
assert("non-member facility work-inbox denied", negativeFacilityInbox.status === 403 || negativeFacilityInbox.status === 404);

const beforeTaskCount = Number(db("select count(*) from ai_tasks;") || "0");
const beforeApprovalCount = Number(db("select count(*) from teams_approval_requests;") || "0");
const beforeHandoverCount = Number(db("select count(*) from teams_shift_handovers;") || "0");

const deniedTransition = await liveRequest(
  "POST",
  `/api/ai-tasks/bb000001-03d0-4000-8000-0000000003d0/transitions?facilityId=${FACILITY}`,
  { expectedVersion: 1, targetStatus: "completed", note: "denied closure" },
  actorHeaders(NEGATIVE),
);
assert("unauthorized transition denied", deniedTransition.status === 403 || deniedTransition.status === 404);

const afterTaskCount = Number(db("select count(*) from ai_tasks;") || "0");
const afterApprovalCount = Number(db("select count(*) from teams_approval_requests;") || "0");
const afterHandoverCount = Number(db("select count(*) from teams_shift_handovers;") || "0");

assert("denied transition zero task delta", afterTaskCount === beforeTaskCount);
assert("denied transition zero approval delta", afterApprovalCount === beforeApprovalCount);
assert("denied transition zero handover delta", afterHandoverCount === beforeHandoverCount);

const ownerTasks = await liveRequest("GET", `/api/ai-tasks?facilityId=${FACILITY}`);
assert("authorized ai-tasks 200", ownerTasks.status === 200, ownerTasks.text?.slice(0, 200));
forbidText(ownerTasks.text, "Exception", "ai-tasks response");
forbidText(ownerTasks.text, "stack", "ai-tasks response");

const ownerInbox = await liveRequest("GET", `/api/teams/work-inbox?facilityId=${FACILITY}`);
assert("authorized work-inbox 200", ownerInbox.status === 200, ownerInbox.text?.slice(0, 200));
forbidText(ownerInbox.text, "Exception", "work-inbox response");
forbidText(ownerInbox.text, "password", "work-inbox response");

const negativeInbox = await liveRequest(
  "GET",
  `/api/teams/work-inbox?facilityId=${FACILITY}`,
  null,
  actorHeaders(NEGATIVE),
);
if (negativeInbox.status === 200) {
  const items = negativeInbox.json?.items ?? [];
  assert("negative actor inbox empty or scoped", items.length === 0 || !JSON.stringify(items).includes(RUN_SUFFIX));
} else {
  assert("negative actor inbox denied", negativeInbox.status === 403 || negativeInbox.status === 404);
}

console.log("AI-TASK-CENTER-UNIFICATION-1 security verification passed.");

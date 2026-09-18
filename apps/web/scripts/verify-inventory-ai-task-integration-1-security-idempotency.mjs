#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const SHORTAGE_ASSESSMENT = "bb000001-00b3-4000-8000-0000000000b3";
const INSUFFICIENT_ASSESSMENT = "bb000001-00b4-4000-8000-0000000000b4";
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`INVENTORY-AI-TASK-INTEGRATION-1 security/idempotency verification failed: ${message}`);
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
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
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

console.log("Running INVENTORY-AI-TASK-INTEGRATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Material/MaterialAvailabilityRiskFollowUpEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/NevaxisZentra.Persistence/Material/MaterialAvailabilityRiskFollowUpRepository.cs");
const presentation = readRequired("packages/NevaxisZentra.Persistence/Material/MaterialAvailabilityRiskFollowUpPresentationBuilder.cs");
const constants = readRequired("packages/NevaxisZentra.Persistence/Material/MaterialAvailabilityRiskFollowUpConstants.cs");

assert("task draft endpoint registered", endpoints.includes("/{assessmentId:guid}/task-draft"));
assert("follow-up endpoint registered", endpoints.includes("/{assessmentId:guid}/follow-up"));
assert("task draft uses ai create access", endpoints.includes("ResolveCreateAccessAsync"));
assert("follow-up uses material write access", endpoints.includes("ResolveWriteAccessAsync"));
assert("repository link task method", repository.includes("LinkTaskAsync"));
assert("idempotency key builder", presentation.includes("BuildTaskDraftIdempotencyKey"));
assert("data review purpose", constants.includes("TaskPurposeDataReviewDraft"));
assert("source module material", constants.includes('SourceModule = "material"'));
assert("no auto action disclaimer", constants.includes("otomatik stok, satın alma veya üretim işlemi yapmaz"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/materials/availability-risks/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace capabilities authoritative", workspace.json?.capabilities?.canRead === true);
assert("workspace assessments array", Array.isArray(workspace.json?.assessments));
assert("pilot shortage assessment present", workspace.json?.assessments?.some((row) => row.id === SHORTAGE_ASSESSMENT));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/materials/availability-risks/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const readonlyTaskDraft = await liveRequest(
  "POST",
  `/api/materials/availability-risks/${SHORTAGE_ASSESSMENT}/task-draft?facilityId=${FACILITY}`,
  {},
  actorHeaders(READONLY),
);
assert("readonly task draft forbidden", readonlyTaskDraft.status === 403);

const detail = await liveRequest(
  "GET",
  `/api/materials/availability-risks/${SHORTAGE_ASSESSMENT}?facilityId=${FACILITY}`,
);
assert("detail includes follow-up fields", detail.status === 200 && detail.json?.followUpStatus);
assert("detail canCreateTaskDraft authoritative", typeof detail.json?.canCreateTaskDraft === "boolean");

const insufficientDetail = await liveRequest(
  "GET",
  `/api/materials/availability-risks/${INSUFFICIENT_ASSESSMENT}?facilityId=${FACILITY}`,
);
assert("insufficient assessment eligible for data review", insufficientDetail.json?.taskDraftPurpose === "data-review-draft");

const invalidTransition = await liveRequest(
  "POST",
  `/api/materials/availability-risks/${INSUFFICIENT_ASSESSMENT}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json?.version ?? 1, targetFollowUpStatus: "resolved" },
);
assert("invalid transition returns 409", invalidTransition.status === 409);

const dismissWithoutReason = await liveRequest(
  "POST",
  `/api/materials/availability-risks/${INSUFFICIENT_ASSESSMENT}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json?.version ?? 1, targetFollowUpStatus: "dismissed" },
);
assert("dismiss without reason returns 400", dismissWithoutReason.status === 400);

const firstDraft = await liveRequest(
  "POST",
  `/api/materials/availability-risks/${SHORTAGE_ASSESSMENT}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("first task draft succeeds or idempotent replay", firstDraft.status === 200 && firstDraft.json?.taskId);
const taskId = firstDraft.json?.taskId;

const replayDraft = await liveRequest(
  "POST",
  `/api/materials/availability-risks/${SHORTAGE_ASSESSMENT}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("task draft replay succeeds", replayDraft.status === 200);
assert("task draft idempotent replay", replayDraft.json?.idempotentReplay === true);
assert("task draft same task id", replayDraft.json?.taskId === taskId);

console.log("INVENTORY-AI-TASK-INTEGRATION-1 security/idempotency verification passed.");

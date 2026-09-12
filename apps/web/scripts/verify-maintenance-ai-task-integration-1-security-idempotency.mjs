#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`MAINTENANCE-AI-TASK-INTEGRATION-1 security/idempotency verification failed: ${message}`);
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

console.log("Running MAINTENANCE-AI-TASK-INTEGRATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Maintenance/MaintenancePredictiveRiskEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenancePredictiveRiskRepository.cs");
const presentation = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenancePredictiveRiskPresentationBuilder.cs");
const constants = readRequired("packages/NevaxisFactOS.Persistence/Maintenance/MaintenancePredictiveRiskConstants.cs");

assert("task draft endpoint registered", endpoints.includes('MapPost("/predictive-risk/{assessmentId:guid}/task-draft"'));
assert("follow-up requires manage permission", endpoints.includes("MaintenancePermissionKeys.Manage"));
assert("task draft uses ai create access", endpoints.includes("ResolveCreateAccessAsync"));
assert("repository link task method", repository.includes("LinkTaskAsync"));
assert("idempotency key builder", presentation.includes("BuildTaskDraftIdempotencyKey"));
assert("insufficient-data data review purpose", constants.includes("TaskPurposeDataReviewDraft"));
assert("no auto action disclaimer", constants.includes("otomatik bakım emri"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

await liveRequest("POST", `/api/maintenance/condition/risk/evaluate?facilityId=${FACILITY}`);

const workspace = await liveRequest("GET", `/api/maintenance/predictive-risk/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace capabilities authoritative", workspace.json?.capabilities?.canRead === true);
assert("workspace assets array", Array.isArray(workspace.json?.assets));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/maintenance/predictive-risk/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const wrongFacility = await liveRequest("GET", `/api/maintenance/predictive-risk/workspace?facilityId=${UNAUTH_FACILITY}`);
assert("wrong facility workspace rejected", wrongFacility.status === 403);

const taskCandidate = (workspace.json?.assets ?? []).find(
  (row) => row.riskBand === "high" || row.riskBand === "critical" || row.riskBand === "watch" || row.riskBand === "insufficient-data",
);
assert("pilot risk asset present", Boolean(taskCandidate?.assessmentId), JSON.stringify(workspace.json?.assets));

const assessmentId = taskCandidate.assessmentId;
const detail = await liveRequest("GET", `/api/maintenance/predictive-risk/${assessmentId}?facilityId=${FACILITY}`);
assert("assessment detail succeeds", detail.status === 200);
assert("assessment exposes risk band", typeof detail.json?.riskBand === "string");

const readonlyTaskDraft = await liveRequest(
  "POST",
  `/api/maintenance/predictive-risk/${assessmentId}/task-draft?facilityId=${FACILITY}`,
  {},
  actorHeaders(READONLY),
);
assert("readonly cannot create task draft", readonlyTaskDraft.status === 403);

const firstDraft = await liveRequest(
  "POST",
  `/api/maintenance/predictive-risk/${assessmentId}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("manager can create task draft", firstDraft.status === 200, firstDraft.text);
assert("task id returned", Boolean(firstDraft.json?.taskId));

const secondDraft = await liveRequest(
  "POST",
  `/api/maintenance/predictive-risk/${assessmentId}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("idempotent replay succeeds", secondDraft.status === 200);
assert("same task id on replay", secondDraft.json?.taskId === firstDraft.json?.taskId);
assert("idempotent replay flag", secondDraft.json?.idempotentReplay === true);

console.log("MAINTENANCE-AI-TASK-INTEGRATION-1 security/idempotency verification passed.");

#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const PREFERRED_DELIVERY_RISK_ASSESSMENT = "bb000001-00c9-4000-8000-0000000000c9";
const PREFERRED_OVERDUE_ASSESSMENT = "bb000001-00c8-4000-8000-0000000000c8";
const MANAGER = { issuer: "crevafactos-dev", subject: "production-pilot-operator", tenant: "dev-pilot-tenant" };
const READONLY = { issuer: "crevafactos-dev", subject: "planning-pilot-reviewer", tenant: "dev-pilot-tenant" };

function fail(message) {
  console.error(`PROCUREMENT-AI-TASK-INTEGRATION-1 security/idempotency verification failed: ${message}`);
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

function resolveAssessmentId(assessments, preferredId, result) {
  if (assessments?.some((row) => row.id === preferredId)) {
    return preferredId;
  }

  return assessments?.find((row) => row.result === result)?.id ?? null;
}

console.log("Running PROCUREMENT-AI-TASK-INTEGRATION-1 security/idempotency verifier.");

const endpoints = readRequired("apps/api/Procurement/ProcurementDeliveryRiskFollowUpEndpointRouteBuilderExtensions.cs");
const repository = readRequired("packages/CrevaFactOS.Persistence/Procurement/ProcurementDeliveryRiskFollowUpRepository.cs");
const presentation = readRequired("packages/CrevaFactOS.Persistence/Procurement/ProcurementDeliveryRiskFollowUpPresentationBuilder.cs");
const constants = readRequired("packages/CrevaFactOS.Persistence/Procurement/ProcurementDeliveryRiskFollowUpConstants.cs");

assert("task draft endpoint registered", endpoints.includes("/{assessmentId:guid}/task-draft"));
assert("follow-up endpoint registered", endpoints.includes("/{assessmentId:guid}/follow-up"));
assert("task draft uses ai create access", endpoints.includes("ResolveCreateAccessAsync"));
assert("follow-up uses erp manage access", endpoints.includes("ErpPermissionKeys.Manage"));
assert("repository link task method", repository.includes("LinkTaskAsync"));
assert("idempotency key builder", presentation.includes("procurement-delivery-risk-task:"));
assert("data review purpose", constants.includes("TaskPurposeDataReviewDraft"));
assert("source module procurement", constants.includes('SourceModule = "procurement"'));
assert("no auto action disclaimer", constants.includes("Otomatik satın alma, tedarikçi iletişimi, stok veya üretim kararı uygulanmaz"));

const ready = await fetch(`${BASE}/health/ready`);
assert("API ready", ready.status === 200, `status=${ready.status}`);

const workspace = await liveRequest("GET", `/api/procurement/delivery-risks/workspace?facilityId=${FACILITY}`);
assert("authorized workspace succeeds", workspace.status === 200);
assert("workspace canCreateTask capability", typeof workspace.json?.capabilities?.canCreateTask === "boolean");
const assessments = workspace.json?.assessments ?? [];
const deliveryRiskAssessmentId = resolveAssessmentId(assessments, PREFERRED_DELIVERY_RISK_ASSESSMENT, "delivery-risk");
const overdueAssessmentId = resolveAssessmentId(assessments, PREFERRED_OVERDUE_ASSESSMENT, "overdue");
const insufficientAssessmentId = resolveAssessmentId(assessments, null, "insufficient-data");
const onTrackAssessmentId = resolveAssessmentId(assessments, null, "on-track");
assert(
  "pilot overdue or delivery-risk assessment present",
  Boolean(deliveryRiskAssessmentId || overdueAssessmentId),
);
assert("pilot insufficient-data assessment present", Boolean(insufficientAssessmentId));
assert("pilot on-track assessment present", Boolean(onTrackAssessmentId));

const unauthWorkspace = await liveRequest(
  "GET",
  `/api/procurement/delivery-risks/workspace?facilityId=${FACILITY}`,
  null,
  {},
);
assert("unauthenticated workspace rejected", unauthWorkspace.status === 401);

const readonlyTaskDraft = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/${deliveryRiskAssessmentId}/task-draft?facilityId=${FACILITY}`,
  {},
  actorHeaders(READONLY),
);
assert("readonly task draft forbidden", readonlyTaskDraft.status === 403);

const deliveryRiskDetail = await liveRequest(
  "GET",
  `/api/procurement/delivery-risks/${deliveryRiskAssessmentId}?facilityId=${FACILITY}`,
);
assert("delivery-risk detail includes follow-up fields", deliveryRiskDetail.status === 200 && deliveryRiskDetail.json?.followUpStatus);
if (deliveryRiskDetail.json?.linkedAiTaskId) {
  assert("delivery-risk already linked for replay", deliveryRiskDetail.json?.followUpStatus === "task-created");
} else {
  assert("delivery-risk canCreateTaskDraft true", deliveryRiskDetail.json?.canCreateTaskDraft === true);
}
assert("delivery-risk human review purpose", deliveryRiskDetail.json?.taskDraftPurpose === "human-review-draft");

const insufficientDetail = await liveRequest(
  "GET",
  `/api/procurement/delivery-risks/${insufficientAssessmentId}?facilityId=${FACILITY}`,
);
assert("insufficient detail succeeds", insufficientDetail.status === 200);
assert("insufficient data review purpose", insufficientDetail.json?.taskDraftPurpose === "data-review-draft");
assert("insufficient hasInsufficientEvidence", insufficientDetail.json?.hasInsufficientEvidence === true);

const onTrackDetail = await liveRequest(
  "GET",
  `/api/procurement/delivery-risks/${onTrackAssessmentId}?facilityId=${FACILITY}`,
);
assert("on-track cannot create task", onTrackDetail.json?.canCreateTaskDraft === false);

const invalidTransition = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/${insufficientAssessmentId}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json?.version ?? 1, targetFollowUpStatus: "resolved" },
);
assert("invalid transition returns 409", invalidTransition.status === 409);

const dismissWithoutReason = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/${insufficientAssessmentId}/follow-up?facilityId=${FACILITY}`,
  { expectedVersion: insufficientDetail.json?.version ?? 1, targetFollowUpStatus: "dismissed" },
);
assert("dismiss without reason returns 400", dismissWithoutReason.status === 400);

const overdueDetail = overdueAssessmentId
  ? await liveRequest(
      "GET",
      `/api/procurement/delivery-risks/${overdueAssessmentId}?facilityId=${FACILITY}`,
    )
  : { status: 404, json: null };
const taskDraftTarget =
  overdueDetail.status === 200 && !overdueDetail.json?.linkedAiTaskId
    ? overdueAssessmentId
    : deliveryRiskAssessmentId;

const firstDraft = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("first task draft succeeds", firstDraft.status === 200 && firstDraft.json?.taskId);
const taskId = firstDraft.json?.taskId;

const replayDraft = await liveRequest(
  "POST",
  `/api/procurement/delivery-risks/${taskDraftTarget}/task-draft?facilityId=${FACILITY}`,
  {},
);
assert("task draft replay succeeds", replayDraft.status === 200);
assert("task draft idempotent replay", replayDraft.json?.idempotentReplay === true);
assert("task draft same task id", replayDraft.json?.taskId === taskId);

const linkedDetail = await liveRequest(
  "GET",
  `/api/procurement/delivery-risks/${taskDraftTarget}?facilityId=${FACILITY}`,
);
assert("linked task id persisted", linkedDetail.json?.linkedAiTaskId === taskId);
assert("follow-up task-created", linkedDetail.json?.followUpStatus === "task-created");

console.log("PROCUREMENT-AI-TASK-INTEGRATION-1 security/idempotency verification passed.");

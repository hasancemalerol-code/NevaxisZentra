import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./shift-assignment-server-boundary";

import {
  getAssignmentStatusLabel,
  getAuditEventTypeLabel,
  getEligibilityOutcomeLabel,
  getEvidenceSourceLabel,
  getReasonCodeLabel,
  getSeverityLabel,
  getAvailabilityLabel,
  getCompetencyCodeLabel,
  getFreshnessLabel,
  LOCAL_PILOT_DISCLAIMER,
} from "../domain/shift-assignment-labels";
import type {
  ApproveShiftAssignmentCommand,
  CancelShiftAssignmentCommand,
  CreateShiftAssignmentDraftCommand,
  ShiftAssignmentAuditEvent,
  ShiftAssignmentCandidateDetail,
  ShiftAssignmentCandidateSummary,
  ShiftAssignmentCandidatesResult,
  ShiftAssignmentCapabilities,
  ShiftAssignmentDetail,
  ShiftAssignmentEligibilityReason,
  ShiftAssignmentEligibilitySummary,
  ShiftAssignmentListFilters,
  ShiftAssignmentListResult,
  ShiftAssignmentRecord,
  ShiftAssignmentSelectionContext,
  ShiftAssignmentWriteResult,
} from "../domain/shift-assignment-types";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type ShiftAssignmentApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "subscription-required"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict"
    | "not-found"
    | "eligibility-blocked"
    | "overlap-conflict";
  message: string;
  retryable: boolean;
}>;

export type ShiftAssignmentApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ShiftAssignmentApiFailure;

function readString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readNumber(value: unknown): number | null {
  if (value === null || value === undefined) {
    return null;
  }

  return typeof value === "number" ? value : null;
}

function readBoolean(value: unknown): boolean {
  return value === true;
}

function parseCapabilities(value: unknown): ShiftAssignmentCapabilities {
  const item = value as Record<string, unknown> | undefined;
  return {
    canRead: item?.canRead === true,
    canManage: item?.canManage === true,
  };
}

function parseSummary(value: unknown): ShiftAssignmentEligibilitySummary {
  const item = value as Record<string, unknown>;
  return {
    eligibleCount: readNumber(item.eligibleCount),
    conditionalCount: readNumber(item.conditionalCount),
    ineligibleCount: readNumber(item.ineligibleCount),
    insufficientDataCount: readNumber(item.insufficientDataCount),
  };
}

function parseReason(value: unknown): ShiftAssignmentEligibilityReason {
  const item = value as Record<string, unknown>;
  const reasonCode = String(item.reasonCode ?? "");
  const severity = String(item.severity ?? "info");
  const evidenceSource = String(item.evidenceSource ?? "");
  const freshnessState = readString(item.freshnessState);
  return {
    reasonCode,
    labelTr: getReasonCodeLabel(reasonCode, readString(item.labelTr)),
    severity,
    severityLabel: getSeverityLabel(severity, readString(item.severityLabel)),
    requiredValue: readString(item.requiredValue),
    observedValue: readString(item.observedValue),
    evidenceSource,
    evidenceSourceLabel: getEvidenceSourceLabel(
      evidenceSource,
      readString(item.evidenceSourceLabel),
    ),
    evidenceTimestampUtc: readString(item.evidenceTimestampUtc),
    freshnessState,
    freshnessLabel: freshnessState
      ? getFreshnessLabel(freshnessState, readString(item.freshnessLabel))
      : undefined,
  };
}

function parseCandidateSummary(value: unknown): ShiftAssignmentCandidateSummary {
  const item = value as Record<string, unknown>;
  const availabilityState = String(item.availabilityState ?? "unknown");
  const freshnessState = String(item.freshnessState ?? "unknown");
  const outcome = String(item.outcome ?? "insufficient-data");
  const primaryReasonCode = readString(item.primaryReasonCode);
  return {
    personId: String(item.personId),
    eligibilityResultId: String(item.eligibilityResultId ?? item.resultId ?? item.personId),
    employeeCode: String(item.employeeCode ?? ""),
    displayName: String(item.displayName ?? ""),
    department: readString(item.department),
    jobTitle: readString(item.jobTitle),
    availabilityState,
    availabilityLabel: getAvailabilityLabel(
      availabilityState,
      readString(item.availabilityLabel),
    ),
    competencySummary: String(item.competencySummary ?? "—"),
    freshnessState,
    freshnessLabel: getFreshnessLabel(freshnessState, readString(item.freshnessLabel)),
    outcome: outcome as ShiftAssignmentCandidateSummary["outcome"],
    outcomeLabel: getEligibilityOutcomeLabel(outcome, readString(item.outcomeLabel)),
    primaryReasonCode,
    primaryReasonLabel: primaryReasonCode
      ? getReasonCodeLabel(primaryReasonCode, readString(item.primaryReasonLabel))
      : undefined,
    hasOverlapWarning: readBoolean(item.hasOverlapWarning),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseCandidateDetail(value: unknown): ShiftAssignmentCandidateDetail {
  const item = value as Record<string, unknown>;
  const summary = parseCandidateSummary(item);
  const sourceSystem = String(item.sourceSystem ?? "");
  const reasons = Array.isArray(item.reasons) ? item.reasons.map(parseReason) : [];
  return {
    ...summary,
    sourceSystem,
    sourceLabel:
      readString(item.sourceLabel) ??
      (sourceSystem === "local-development-pilot"
        ? LOCAL_PILOT_DISCLAIMER
        : "Harici kaynak"),
    sourceUpdatedAtUtc: readString(item.sourceUpdatedAtUtc),
    shiftLabel: readString(item.shiftLabel),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    assessmentId: String(item.assessmentId ?? ""),
    reasons,
  };
}

function parseCandidatesResult(payload: unknown): ShiftAssignmentCandidatesResult {
  const item = payload as Record<string, unknown>;
  const candidates = Array.isArray(item.candidates)
    ? item.candidates.map(parseCandidateSummary)
    : [];
  return {
    assessmentId: String(item.assessmentId ?? ""),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    evaluatedAtUtc: String(item.evaluatedAtUtc ?? ""),
    summary: parseSummary(item.summary),
    candidates,
    capabilities: parseCapabilities(item.capabilities),
  };
}

function parseAssignmentRecord(value: unknown): ShiftAssignmentRecord {
  const item = value as Record<string, unknown>;
  const status = String(item.status ?? "draft");
  return {
    id: String(item.id),
    productionOrderReference: String(item.productionOrderReference ?? ""),
    productionOrderLabel: readString(item.productionOrderLabel),
    machineAssetLabel: String(item.machineAssetLabel ?? "—"),
    shiftLabel: String(item.shiftLabel ?? "—"),
    shiftDate: String(item.shiftDate ?? ""),
    employeeCode: String(item.employeeCode ?? ""),
    displayName: String(item.displayName ?? ""),
    status: status as ShiftAssignmentRecord["status"],
    statusLabel: getAssignmentStatusLabel(status, readString(item.statusLabel)),
    plannedStartUtc: String(item.plannedStartUtc ?? ""),
    plannedEndUtc: String(item.plannedEndUtc ?? ""),
    approvedByDisplayName: readString(item.approvedByDisplayName),
    approvedAtUtc: readString(item.approvedAtUtc),
    cancelledByDisplayName: readString(item.cancelledByDisplayName),
    cancelledAtUtc: readString(item.cancelledAtUtc),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    version: Number(item.version ?? 0),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseAssignmentDetail(value: unknown): ShiftAssignmentDetail {
  const item = value as Record<string, unknown>;
  const record = parseAssignmentRecord(item);
  return {
    ...record,
    eligibilityAssessmentId: String(item.eligibilityAssessmentId ?? ""),
    eligibilityResultId: String(item.eligibilityResultId ?? ""),
    department: readString(item.department),
    jobTitle: readString(item.jobTitle),
    machineFamilyScope: readString(item.machineFamilyScope),
    processScope: readString(item.processScope),
  };
}

function parseAssignmentList(payload: unknown): ShiftAssignmentListResult {
  const item = payload as Record<string, unknown>;
  const items = Array.isArray(item.items) ? item.items.map(parseAssignmentRecord) : [];
  return {
    items,
    totalCount: Number(item.totalCount ?? items.length),
    page: Number(item.page ?? 1),
    pageSize: Number(item.pageSize ?? items.length),
    capabilities: parseCapabilities(item.capabilities),
  };
}

function parseAuditEvent(value: unknown): ShiftAssignmentAuditEvent {
  const item = value as Record<string, unknown>;
  const eventType = String(item.eventType ?? "");
  return {
    id: String(item.id),
    eventType,
    eventTypeLabel: getAuditEventTypeLabel(eventType, readString(item.eventTypeLabel)),
    actorDisplayName: readString(item.actorDisplayName),
    occurredAtUtc: String(item.occurredAtUtc ?? ""),
    summary: String(item.summary ?? ""),
  };
}

function parseWriteResult(payload: unknown): ShiftAssignmentWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    version: Number(item.version ?? 0),
    idempotentReplay: item.idempotentReplay === true,
  };
}

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): ShiftAssignmentApiFailure["code"] {
  if (failure?.kind === "subscription-required") return "subscription-required";
  if (failure?.kind === "unauthenticated") return "unauthenticated";
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

function mapFailureCodeFromProblem(problem: Record<string, unknown>): ShiftAssignmentApiFailure["code"] {
  const extensions = problem.extensions as Record<string, unknown> | undefined;
  const code = String(extensions?.code ?? extensions?.failureCode ?? "");
  if (code.includes("eligibility")) return "eligibility-blocked";
  if (code.includes("overlap")) return "overlap-conflict";
  if (code.includes("concurrency")) return "concurrency-conflict";
  return mapFailureStatus(Number(problem.status ?? 500));
}

function contextToQuery(
  context: ShiftAssignmentSelectionContext,
): Record<string, string | undefined> {
  return {
    productionOrderReference: context.productionOrderReference,
    machineAssetId: context.machineAssetId,
    productionShiftId: context.productionShiftId,
    shiftDate: context.shiftDate,
    requiredCompetencyCode: context.requiredCompetencyCode,
    machineFamilyScope: context.machineFamilyScope,
    processScope: context.processScope,
  };
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<ShiftAssignmentApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (!response.ok) {
      const failure = await classifyFailedResponse(response);
      return {
        status: "failure",
        code: mapFailureStatus(response.status, failure),
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `Operatör atama isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Operatör atama API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

async function mutateJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  method: "POST",
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<ShiftAssignmentApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method,
      headers: {
        ...context.requestHeaders,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    if (!response.ok) {
      const failure = await classifyFailedResponse(response);
      let message = `Operatör atama işlemi başarısız (${response.status}).`;
      let code = mapFailureStatus(response.status, failure);
      try {
        const problem = (await response.json()) as Record<string, unknown>;
        if (typeof problem.title === "string") {
          message = problem.title;
        }
        code = mapFailureCodeFromProblem(problem);
      } catch {
        // ignore parse errors
      }

      return {
        status: "failure",
        code,
        message,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Operatör atama API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

export function listShiftAssignmentCandidates(
  context: ProductionOrderRequestContext,
  selection: ShiftAssignmentSelectionContext,
): Promise<ShiftAssignmentApiResult<ShiftAssignmentCandidatesResult>> {
  return getJson(
    context,
    "/api/shift-assignments/candidates",
    contextToQuery(selection),
    parseCandidatesResult,
  );
}

export function getShiftAssignmentCandidate(
  context: ProductionOrderRequestContext,
  personId: string,
  selection: ShiftAssignmentSelectionContext,
): Promise<ShiftAssignmentApiResult<ShiftAssignmentCandidateDetail>> {
  return getJson(
    context,
    `/api/shift-assignments/candidates/${encodeURIComponent(personId)}`,
    contextToQuery(selection),
    parseCandidateDetail,
  );
}

export function listShiftAssignments(
  context: ProductionOrderRequestContext,
  filters: ShiftAssignmentListFilters = {},
): Promise<ShiftAssignmentApiResult<ShiftAssignmentListResult>> {
  return getJson(
    context,
    "/api/shift-assignments/assignments",
    {
      productionOrderReference: filters.productionOrderReference,
      status: filters.status,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseAssignmentList,
  );
}

export function getShiftAssignment(
  context: ProductionOrderRequestContext,
  assignmentId: string,
): Promise<ShiftAssignmentApiResult<ShiftAssignmentDetail>> {
  return getJson(
    context,
    `/api/shift-assignments/assignments/${encodeURIComponent(assignmentId)}`,
    undefined,
    parseAssignmentDetail,
  );
}

export function listShiftAssignmentAuditEvents(
  context: ProductionOrderRequestContext,
  assignmentId: string,
): Promise<ShiftAssignmentApiResult<readonly ShiftAssignmentAuditEvent[]>> {
  return getJson(
    context,
    `/api/shift-assignments/assignments/${encodeURIComponent(assignmentId)}/audit-events`,
    undefined,
    (payload) => {
      if (!Array.isArray(payload)) {
        throw new Error("Expected audit event list array.");
      }
      return payload.map(parseAuditEvent);
    },
  );
}

export function createShiftAssignmentDraft(
  context: ProductionOrderRequestContext,
  command: CreateShiftAssignmentDraftCommand,
): Promise<ShiftAssignmentApiResult<ShiftAssignmentWriteResult>> {
  return mutateJson(
    context,
    "/api/shift-assignments/assignments",
    "POST",
    command,
    parseWriteResult,
  );
}

export function approveShiftAssignment(
  context: ProductionOrderRequestContext,
  assignmentId: string,
  command: ApproveShiftAssignmentCommand,
): Promise<ShiftAssignmentApiResult<ShiftAssignmentWriteResult>> {
  return mutateJson(
    context,
    `/api/shift-assignments/assignments/${encodeURIComponent(assignmentId)}/approve`,
    "POST",
    command,
    parseWriteResult,
  );
}

export function cancelShiftAssignment(
  context: ProductionOrderRequestContext,
  assignmentId: string,
  command: CancelShiftAssignmentCommand,
): Promise<ShiftAssignmentApiResult<ShiftAssignmentWriteResult>> {
  return mutateJson(
    context,
    `/api/shift-assignments/assignments/${encodeURIComponent(assignmentId)}/cancel`,
    "POST",
    command,
    parseWriteResult,
  );
}

export async function probeShiftAssignmentPermissions(
  context: ProductionOrderRequestContext,
): Promise<ShiftAssignmentCapabilities> {
  const result = await listShiftAssignments(context, { page: 1, pageSize: 1 });
  if (result.status === "success") {
    return result.value.capabilities;
  }

  if (result.code === "permission-denied" || result.code === "unauthenticated") {
    return { canRead: false, canManage: false };
  }

  return { canRead: true, canManage: false };
}

export { getCompetencyCodeLabel };

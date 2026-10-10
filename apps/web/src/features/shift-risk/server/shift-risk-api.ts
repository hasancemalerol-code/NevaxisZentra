import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./shift-risk-server-boundary";

import {
  getShiftRiskCategoryLabel,
  getShiftRiskOutcomeLabel,
  getShiftRiskSeverityLabel,
  LOCAL_PILOT_DISCLAIMER,
} from "../domain/shift-risk-labels";
import type {
  EvaluateShiftRiskCommand,
  ShiftRiskAssessmentDetail,
  ShiftRiskAssessmentListFilters,
  ShiftRiskAssessmentListResult,
  ShiftRiskAssessmentSummary,
  ShiftRiskAssignmentEvidence,
  ShiftRiskCapabilities,
  ShiftRiskDriver,
  ShiftRiskOverview,
  ShiftRiskWriteResult,
} from "../domain/shift-risk-types";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type ShiftRiskApiFailure = Readonly<{
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
    | "idempotency-conflict";
  message: string;
  retryable: boolean;
}>;

export type ShiftRiskApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ShiftRiskApiFailure;

function readString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readNumber(value: unknown): number | null {
  if (value === null || value === undefined) return null;
  return typeof value === "number" ? value : null;
}

function readBoolean(value: unknown): boolean {
  return value === true;
}

function parseCapabilities(value: unknown): ShiftRiskCapabilities {
  const item = value as Record<string, unknown> | undefined;
  return {
    canRead: item?.canRead === true,
    canManage: item?.canManage === true,
  };
}

function parseDriver(value: unknown): ShiftRiskDriver {
  const item = value as Record<string, unknown>;
  const category = String(item.category ?? "");
  const severity = String(item.severity ?? "info");
  const reasonCode = String(item.reasonCode ?? "");
  return {
    id: String(item.id),
    reasonCode,
    labelTr: String(item.labelTr ?? reasonCode),
    category,
    categoryLabel: getShiftRiskCategoryLabel(category, readString(item.categoryLabel)),
    severity,
    severityLabel: getShiftRiskSeverityLabel(severity, readString(item.severityLabel)),
    requiredValue: readString(item.requiredValue),
    observedValue: readString(item.observedValue),
    gapValue: readString(item.gapValue),
    evidenceSource: String(item.evidenceSource ?? ""),
    evidenceTimestampUtc: readString(item.evidenceTimestampUtc),
    freshnessState: readString(item.freshnessState),
    freshnessLabel: readString(item.freshnessLabel),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    isLeadingRisk: readBoolean(item.isLeadingRisk),
  };
}

function parseAssignmentEvidence(value: unknown): ShiftRiskAssignmentEvidence {
  const item = value as Record<string, unknown>;
  const status = String(item.status ?? "");
  const exclusionReasonCode = readString(item.exclusionReasonCode);
  return {
    assignmentId: String(item.assignmentId ?? item.id ?? ""),
    workforcePersonId: String(item.workforcePersonId ?? ""),
    employeeCode: String(item.employeeCode ?? ""),
    displayName: String(item.displayName ?? ""),
    status,
    statusLabel: String(item.statusLabel ?? status),
    plannedStartUtc: String(item.plannedStartUtc ?? ""),
    plannedEndUtc: String(item.plannedEndUtc ?? ""),
    exclusionReasonCode,
    exclusionReasonLabel: readString(item.exclusionReasonLabel),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseAssessmentSummary(value: unknown): ShiftRiskAssessmentSummary {
  const item = value as Record<string, unknown>;
  const outcome = String(item.outcome ?? "insufficient-data");
  return {
    id: String(item.id),
    coverageRequirementId: readString(item.coverageRequirementId),
    productionShiftId: String(item.productionShiftId ?? ""),
    shiftLabel: String(item.shiftLabel ?? "—"),
    shiftDate: String(item.shiftDate ?? ""),
    productionOrderReference: readString(item.productionOrderReference),
    machineAssetId: readString(item.machineAssetId),
    machineAssetLabel: readString(item.machineAssetLabel),
    requiredCompetencyCode: readString(item.requiredCompetencyCode),
    minimumRequiredCount: readNumber(item.minimumRequiredCount),
    approvedEligibleCount: Number(item.approvedEligibleCount ?? 0),
    coverageGap: Number(item.coverageGap ?? 0),
    outcome,
    outcomeLabel: getShiftRiskOutcomeLabel(outcome, readString(item.outcomeLabel)),
    leadingDriverCategory: readString(item.leadingDriverCategory),
    leadingReasonCode: readString(item.leadingReasonCode),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    evaluatedAtUtc: String(item.evaluatedAtUtc ?? ""),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseAssessmentDetail(value: unknown): ShiftRiskAssessmentDetail {
  const item = value as Record<string, unknown>;
  const summary = parseAssessmentSummary(item);
  const drivers = Array.isArray(item.drivers) ? item.drivers.map(parseDriver) : [];
  const counted = Array.isArray(item.countedAssignments)
    ? item.countedAssignments.map(parseAssignmentEvidence)
    : [];
  const excluded = Array.isArray(item.excludedAssignments)
    ? item.excludedAssignments.map(parseAssignmentEvidence)
    : [];
  return {
    ...summary,
    plannedStartUtc: String(item.plannedStartUtc ?? ""),
    plannedEndUtc: String(item.plannedEndUtc ?? ""),
    machineFamilyScope: readString(item.machineFamilyScope),
    isCriticalRole: item.isCriticalRole === null ? null : readBoolean(item.isCriticalRole),
    leadingDriverId: readString(item.leadingDriverId),
    sourceFreshnessSnapshotJson: readString(item.sourceFreshnessSnapshotJson),
    drivers,
    countedAssignments: counted,
    excludedAssignments: excluded,
  };
}

function parseOverview(value: unknown): ShiftRiskOverview {
  const item = value as Record<string, unknown>;
  return {
    evaluatedContextCount: Number(item.evaluatedContextCount ?? 0),
    clearCount: Number(item.clearCount ?? 0),
    attentionCount: Number(item.attentionCount ?? 0),
    criticalCount: Number(item.criticalCount ?? 0),
    insufficientDataCount: Number(item.insufficientDataCount ?? 0),
    coverageGapCount: Number(item.coverageGapCount ?? 0),
    criticalRoleGapCount: Number(item.criticalRoleGapCount ?? 0),
    conflictCount: Number(item.conflictCount ?? 0),
    capabilities: parseCapabilities(item.capabilities),
  };
}

function parseAssessmentList(payload: unknown): ShiftRiskAssessmentListResult {
  const item = payload as Record<string, unknown>;
  const items = Array.isArray(item.items) ? item.items.map(parseAssessmentSummary) : [];
  return {
    items,
    totalCount: Number(item.totalCount ?? items.length),
    page: Number(item.page ?? 1),
    pageSize: Number(item.pageSize ?? items.length),
    capabilities: parseCapabilities(item.capabilities),
  };
}

function parseWriteResult(payload: unknown): ShiftRiskWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id ?? item.entityId ?? ""),
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

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): ShiftRiskApiFailure["code"] {
  if (failure?.kind === "subscription-required") return "subscription-required";
  if (failure?.kind === "unauthenticated") return "unauthenticated";
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

function mapFailureCodeFromProblem(problem: Record<string, unknown>): ShiftRiskApiFailure["code"] {
  const extensions = problem.extensions as Record<string, unknown> | undefined;
  const code = String(extensions?.code ?? extensions?.failureCode ?? "");
  if (code.includes("idempotency")) return "idempotency-conflict";
  if (code.includes("concurrency")) return "concurrency-conflict";
  return mapFailureStatus(Number(problem.status ?? 500));
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<ShiftRiskApiResult<T>> {
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
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `Vardiya risk isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Vardiya risk API bağlantısı kurulamadı.",
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
): Promise<ShiftRiskApiResult<T>> {
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
      let message = `Vardiya risk işlemi başarısız (${response.status}).`;
      let code = mapFailureStatus(response.status, failure);
      try {
        const problem = (await response.json()) as Record<string, unknown>;
        if (typeof problem.title === "string") message = problem.title;
        code = mapFailureCodeFromProblem(problem);
      } catch {
        // ignore
      }
      return { status: "failure", code, message, retryable: response.status >= 500 };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Vardiya risk API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

export function getShiftRiskOverview(
  context: ProductionOrderRequestContext,
): Promise<ShiftRiskApiResult<ShiftRiskOverview>> {
  return getJson(context, "/api/shift-risks/overview", undefined, parseOverview);
}

export function listShiftRiskAssessments(
  context: ProductionOrderRequestContext,
  filters: ShiftRiskAssessmentListFilters = {},
): Promise<ShiftRiskApiResult<ShiftRiskAssessmentListResult>> {
  return getJson(
    context,
    "/api/shift-risks/assessments",
    {
      productionShiftId: filters.productionShiftId,
      productionOrderReference: filters.productionOrderReference,
      machineAssetId: filters.machineAssetId,
      outcome: filters.outcome,
      driverCategory: filters.driverCategory,
      criticalOnly: filters.criticalOnly ? "true" : undefined,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseAssessmentList,
  );
}

export function getShiftRiskAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<ShiftRiskApiResult<ShiftRiskAssessmentDetail>> {
  return getJson(
    context,
    `/api/shift-risks/assessments/${encodeURIComponent(assessmentId)}`,
    undefined,
    parseAssessmentDetail,
  );
}

export function evaluateShiftRisk(
  context: ProductionOrderRequestContext,
  command: EvaluateShiftRiskCommand,
): Promise<ShiftRiskApiResult<ShiftRiskWriteResult>> {
  return mutateJson(context, "/api/shift-risks/evaluations", "POST", command, parseWriteResult);
}

export async function probeShiftRiskPermissions(
  context: ProductionOrderRequestContext,
): Promise<ShiftRiskCapabilities> {
  const result = await getShiftRiskOverview(context);
  if (result.status === "success") {
    return result.value.capabilities;
  }
  if (result.code === "permission-denied" || result.code === "unauthenticated") {
    return { canRead: false, canManage: false };
  }
  return { canRead: true, canManage: false };
}

export { LOCAL_PILOT_DISCLAIMER };

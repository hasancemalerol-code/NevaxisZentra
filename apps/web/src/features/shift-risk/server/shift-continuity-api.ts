import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./shift-risk-server-boundary";

import {
  getShiftContinuityCategoryLabel,
  getShiftContinuityOutcomeLabel,
  getShiftContinuitySeverityLabel,
  LOCAL_PILOT_DISCLAIMER,
} from "../domain/shift-continuity-labels";
import type {
  EvaluateShiftContinuityCommand,
  ShiftContinuityAssessmentDetail,
  ShiftContinuityAssessmentListFilters,
  ShiftContinuityAssessmentListResult,
  ShiftContinuityAssessmentSummary,
  ShiftContinuityAssignmentEvidence,
  ShiftContinuityCapabilities,
  ShiftContinuityCompetencyEvidence,
  ShiftContinuityCoverageImpact,
  ShiftContinuityDriver,
  ShiftContinuityOverview,
  ShiftContinuityWorkloadFacts,
  ShiftContinuityWriteResult,
} from "../domain/shift-continuity-types";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type ShiftContinuityApiFailure = Readonly<{
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

export type ShiftContinuityApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ShiftContinuityApiFailure;

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

function parseCapabilities(value: unknown): ShiftContinuityCapabilities {
  const item = value as Record<string, unknown> | undefined;
  return {
    canRead: item?.canRead === true,
    canManage: item?.canManage === true,
  };
}

function parseDriver(value: unknown): ShiftContinuityDriver {
  const item = value as Record<string, unknown>;
  const category = String(item.category ?? "");
  const severity = String(item.severity ?? "info");
  const reasonCode = String(item.reasonCode ?? "");
  return {
    id: String(item.id),
    reasonCode,
    labelTr: String(item.labelTr ?? reasonCode),
    category,
    categoryLabel: getShiftContinuityCategoryLabel(category, readString(item.categoryLabel)),
    severity,
    severityLabel: getShiftContinuitySeverityLabel(severity, readString(item.severityLabel)),
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

function parseCompetencyEvidence(value: unknown): ShiftContinuityCompetencyEvidence {
  const item = value as Record<string, unknown>;
  const verificationStatus = String(item.verificationStatus ?? "");
  return {
    competencyCode: String(item.competencyCode ?? ""),
    competencyLabel: String(item.competencyLabel ?? item.competencyCode ?? "—"),
    verificationStatus,
    verificationStatusLabel: String(item.verificationStatusLabel ?? verificationStatus),
    validFromUtc: readString(item.validFromUtc),
    validUntilUtc: readString(item.validUntilUtc),
    requiresAttention: readBoolean(item.requiresAttention),
  };
}

function parseAssignmentEvidence(value: unknown): ShiftContinuityAssignmentEvidence {
  const item = value as Record<string, unknown>;
  const status = String(item.status ?? "");
  return {
    assignmentId: String(item.assignmentId ?? item.id ?? ""),
    status,
    statusLabel: String(item.statusLabel ?? status),
    shiftLabel: String(item.shiftLabel ?? "—"),
    shiftDate: String(item.shiftDate ?? ""),
    productionOrderReference: readString(item.productionOrderReference),
    machineAssetLabel: readString(item.machineAssetLabel),
    plannedStartUtc: String(item.plannedStartUtc ?? ""),
    plannedEndUtc: String(item.plannedEndUtc ?? ""),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseWorkloadFacts(value: unknown): ShiftContinuityWorkloadFacts {
  const item = value as Record<string, unknown> | undefined;
  return {
    approvedAssignmentCount: Number(item?.approvedAssignmentCount ?? 0),
    plannedAssignmentMinutes: Number(item?.plannedAssignmentMinutes ?? 0),
    authoritativeThresholdAvailable: readBoolean(item?.authoritativeThresholdAvailable),
    thresholdValue: readString(item?.thresholdValue),
    conclusionRequiresThreshold: readBoolean(item?.conclusionRequiresThreshold),
  };
}

function parseCoverageImpact(value: unknown): ShiftContinuityCoverageImpact {
  const item = value as Record<string, unknown> | undefined;
  return {
    requiredCount: readNumber(item?.requiredCount),
    trustworthyAvailableCount: readNumber(item?.trustworthyAvailableCount),
    coverageGap: readNumber(item?.coverageGap),
    isCriticalRole: item?.isCriticalRole === null ? null : readBoolean(item?.isCriticalRole),
    requirementConfigured: readBoolean(item?.requirementConfigured),
  };
}

function parseAssessmentSummary(value: unknown): ShiftContinuityAssessmentSummary {
  const item = value as Record<string, unknown>;
  const outcome = String(item.outcome ?? "insufficient-data");
  const availabilityState = String(item.availabilityState ?? "unknown");
  const competencyState = String(item.competencyState ?? "unknown");
  const freshnessState = String(item.freshnessState ?? "unknown");
  return {
    id: String(item.id),
    workforcePersonId: String(item.workforcePersonId ?? ""),
    employeeCode: String(item.employeeCode ?? ""),
    displayName: String(item.displayName ?? "—"),
    productionShiftId: String(item.productionShiftId ?? ""),
    shiftLabel: String(item.shiftLabel ?? "—"),
    shiftDate: String(item.shiftDate ?? ""),
    productionOrderReference: readString(item.productionOrderReference),
    machineAssetId: readString(item.machineAssetId),
    machineAssetLabel: readString(item.machineAssetLabel),
    requiredCompetencyCode: readString(item.requiredCompetencyCode),
    availabilityState,
    availabilityLabel: String(item.availabilityLabel ?? availabilityState),
    competencyState,
    competencyLabel: String(item.competencyLabel ?? competencyState),
    freshnessState,
    freshnessLabel: String(item.freshnessLabel ?? freshnessState),
    approvedAssignmentCount: Number(item.approvedAssignmentCount ?? 0),
    plannedAssignmentMinutes: Number(item.plannedAssignmentMinutes ?? 0),
    outcome,
    outcomeLabel: getShiftContinuityOutcomeLabel(outcome, readString(item.outcomeLabel)),
    leadingDriverCategory: readString(item.leadingDriverCategory),
    leadingReasonCode: readString(item.leadingReasonCode),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    evaluatedAtUtc: String(item.evaluatedAtUtc ?? ""),
    isCriticalContext: readBoolean(item.isCriticalContext),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseAssessmentDetail(value: unknown): ShiftContinuityAssessmentDetail {
  const item = value as Record<string, unknown>;
  const summary = parseAssessmentSummary(item);
  const drivers = Array.isArray(item.drivers) ? item.drivers.map(parseDriver) : [];
  const competencies = Array.isArray(item.competencies)
    ? item.competencies.map(parseCompetencyEvidence)
    : [];
  const approvedAssignments = Array.isArray(item.approvedAssignments)
    ? item.approvedAssignments.map(parseAssignmentEvidence)
    : [];
  return {
    ...summary,
    department: readString(item.department),
    jobTitle: readString(item.jobTitle),
    isActive: item.isActive !== false,
    plannedStartUtc: String(item.plannedStartUtc ?? ""),
    plannedEndUtc: String(item.plannedEndUtc ?? ""),
    sourceSystem: String(item.sourceSystem ?? ""),
    sourceLabel: String(item.sourceLabel ?? item.sourceSystem ?? "—"),
    sourceShiftReference: readString(item.sourceShiftReference),
    sourceCalendarReference: readString(item.sourceCalendarReference),
    sourceUpdatedAtUtc: readString(item.sourceUpdatedAtUtc),
    synchronizedAtUtc: readString(item.synchronizedAtUtc),
    sourceLimitations: Array.isArray(item.sourceLimitations)
      ? item.sourceLimitations.map((entry) => String(entry))
      : [],
    leadingDriverId: readString(item.leadingDriverId),
    sourceFreshnessSnapshotJson: readString(item.sourceFreshnessSnapshotJson),
    workloadFacts: parseWorkloadFacts(item.workloadFacts ?? item),
    coverageImpact: parseCoverageImpact(item.coverageImpact),
    competencies,
    approvedAssignments,
    drivers,
  };
}

function parseOverview(value: unknown): ShiftContinuityOverview {
  const item = value as Record<string, unknown>;
  return {
    evaluatedContextCount: Number(item.evaluatedContextCount ?? 0),
    absenceImpactCount: Number(item.absenceImpactCount ?? 0),
    competencyExpiredCount: Number(item.competencyExpiredCount ?? 0),
    competencyExpiringCount: Number(item.competencyExpiringCount ?? 0),
    staleOrUnknownSourceCount: Number(
      item.staleOrUnknownSourceCount ?? item.staleOrUnknownCount ?? 0,
    ),
    assignmentConflictCount: Number(
      item.assignmentConflictCount ?? item.assignmentOverlapCount ?? 0,
    ),
    coverageImpactCount: Number(item.coverageImpactCount ?? 0),
    insufficientDataCount: Number(item.insufficientDataCount ?? 0),
    capabilities: parseCapabilities(item.capabilities),
  };
}

function parseAssessmentList(payload: unknown): ShiftContinuityAssessmentListResult {
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

function parseWriteResult(payload: unknown): ShiftContinuityWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id ?? item.entityId ?? item.assessmentId ?? ""),
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

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): ShiftContinuityApiFailure["code"] {
  if (failure?.kind === "subscription-required") return "subscription-required";
  if (failure?.kind === "unauthenticated") return "unauthenticated";
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

function mapFailureCodeFromProblem(problem: Record<string, unknown>): ShiftContinuityApiFailure["code"] {
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
): Promise<ShiftContinuityApiResult<T>> {
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
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `Personel sürekliliği isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Personel sürekliliği API bağlantısı kurulamadı.",
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
): Promise<ShiftContinuityApiResult<T>> {
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
      let message = `Personel sürekliliği işlemi başarısız (${response.status}).`;
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
      message: "Personel sürekliliği API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

export function getShiftContinuityOverview(
  context: ProductionOrderRequestContext,
): Promise<ShiftContinuityApiResult<ShiftContinuityOverview>> {
  return getJson(context, "/api/shift-risks/workforce-continuity/overview", undefined, parseOverview);
}

export function listShiftContinuityAssessments(
  context: ProductionOrderRequestContext,
  filters: ShiftContinuityAssessmentListFilters = {},
): Promise<ShiftContinuityApiResult<ShiftContinuityAssessmentListResult>> {
  return getJson(
    context,
    "/api/shift-risks/workforce-continuity/assessments",
    {
      productionShiftId: filters.productionShiftId,
      shiftDate: filters.shiftDate,
      outcome: filters.outcome,
      driverCategory: filters.driverCategory,
      competencyCode: filters.competencyCode,
      availability: filters.availability,
      freshness: filters.freshness,
      criticalOnly: filters.criticalOnly ? "true" : undefined,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseAssessmentList,
  );
}

export function getShiftContinuityAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<ShiftContinuityApiResult<ShiftContinuityAssessmentDetail>> {
  return getJson(
    context,
    `/api/shift-risks/workforce-continuity/assessments/${encodeURIComponent(assessmentId)}`,
    undefined,
    parseAssessmentDetail,
  );
}

export function evaluateShiftContinuity(
  context: ProductionOrderRequestContext,
  command: EvaluateShiftContinuityCommand,
): Promise<ShiftContinuityApiResult<ShiftContinuityWriteResult>> {
  return mutateJson(
    context,
    "/api/shift-risks/workforce-continuity/evaluations",
    "POST",
    command,
    parseWriteResult,
  );
}

export async function probeShiftContinuityPermissions(
  context: ProductionOrderRequestContext,
): Promise<ShiftContinuityCapabilities> {
  const result = await getShiftContinuityOverview(context);
  if (result.status === "success") {
    return result.value.capabilities;
  }
  if (result.code === "permission-denied" || result.code === "unauthenticated") {
    return { canRead: false, canManage: false };
  }
  return { canRead: true, canManage: false };
}

export { LOCAL_PILOT_DISCLAIMER };

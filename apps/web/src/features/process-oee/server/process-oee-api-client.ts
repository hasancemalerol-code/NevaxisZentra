import "./process-oee-server-boundary";

import type {
  ProcessOeeAssessmentDetail,
  ProcessOeeAssessmentSummary,
  ProcessOeeLossDetail,
  ProcessOeeWorkspaceSummary,
  ProcessOeeWriteResult,
} from "../domain/process-oee";
import {
  getLossCategoryLabel,
  getLossExplanationLabel,
  getLossFreshnessLabel,
  getProcessOeeStatusLabel,
  getDriverSeverityLabel,
} from "../presentation/process-oee-labels";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type ProcessOeeApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type ProcessOeeApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ProcessOeeApiFailure;

export type ProcessOeeAssessmentListFilters = Readonly<{
  machineAssetId?: string;
  productionOrderReference?: string;
  status?: string;
  evaluatedFromUtc?: string;
  evaluatedToUtc?: string;
  page?: number;
  pageSize?: number;
}>;

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

function parseDriver(value: unknown) {
  const item = value as Record<string, unknown>;
  return {
    code: String(item.code),
    label: String(item.label),
    value: readString(item.value),
    severity: String(item.severity),
  };
}

function parseEvidence(value: unknown) {
  const item = value as Record<string, unknown>;
  return {
    field: String(item.field),
    value: readString(item.value),
    source: readString(item.source),
    recordedAtUtc: readString(item.recordedAtUtc),
    isMissing: readBoolean(item.isMissing),
    isConflict: readBoolean(item.isConflict),
    conflictReason: readString(item.conflictReason),
  };
}

function parseAssessmentSummary(value: unknown): ProcessOeeAssessmentSummary {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    machineAssetId: String(item.machineAssetId),
    machineCode: readString(item.machineCode),
    machineDisplayName: readString(item.machineDisplayName),
    productionOrderReference: readString(item.productionOrderReference),
    moldReference: readString(item.moldReference),
    windowStartUtc: String(item.windowStartUtc),
    windowEndUtc: String(item.windowEndUtc),
    status: String(item.status),
    statusLabel: String(item.statusLabel ?? item.status),
    availabilityPercent: readNumber(item.availabilityPercent),
    performancePercent: readNumber(item.performancePercent),
    qualityPercent: readNumber(item.qualityPercent),
    oeePercent: readNumber(item.oeePercent),
    evaluatedAtUtc: String(item.evaluatedAtUtc),
  };
}

function parseAssessmentSummaryList(payload: unknown): readonly ProcessOeeAssessmentSummary[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected assessment list array.");
  }

  return payload.map(parseAssessmentSummary);
}

function parseAssessmentDetail(payload: unknown): ProcessOeeAssessmentDetail {
  const item = payload as Record<string, unknown>;
  const summary = parseAssessmentSummary(item);
  return {
    ...summary,
    evaluationWindowId: String(item.evaluationWindowId),
    ruleSetVersion: String(item.ruleSetVersion),
    plannedProductionSeconds: readNumber(item.plannedProductionSeconds),
    operatingSeconds: readNumber(item.operatingSeconds),
    downtimeSeconds: readNumber(item.downtimeSeconds),
    unknownSeconds: readNumber(item.unknownSeconds),
    observedCycleCount: readNumber(item.observedCycleCount),
    derivedShotCount: readNumber(item.derivedShotCount),
    totalPieceCount: readNumber(item.totalPieceCount),
    goodPieceCount: readNumber(item.goodPieceCount),
    scrapPieceCount: readNumber(item.scrapPieceCount),
    performanceRawPercent: readNumber(item.performanceRawPercent),
    drivers: Array.isArray(item.drivers) ? item.drivers.map(parseDriver) : [],
    evidence: Array.isArray(item.evidence) ? item.evidence.map(parseEvidence) : [],
    missingFields: Array.isArray(item.missingFields)
      ? item.missingFields.map(String)
      : [],
    conflicts: Array.isArray(item.conflicts) ? item.conflicts.map(String) : [],
    sourceSystem: readString(item.sourceSystem),
    sourceFreshnessUtc: readString(item.sourceFreshnessUtc),
    isSimulatorDerived: readBoolean(item.isSimulatorDerived),
    calculationHonestyDisclaimer: String(item.calculationHonestyDisclaimer ?? ""),
    simulatorHonestyDisclaimer: readString(item.simulatorHonestyDisclaimer),
  };
}

function parseWorkspaceSummary(payload: unknown): ProcessOeeWorkspaceSummary {
  const item = payload as Record<string, unknown>;
  const capabilities = item.capabilities as Record<string, unknown> | undefined;
  return {
    totalWindowCount: Number(item.totalWindowCount ?? 0),
    assessmentCount: Number(item.assessmentCount ?? 0),
    calculatedCount: Number(item.calculatedCount ?? 0),
    insufficientDataCount: Number(item.insufficientDataCount ?? 0),
    reviewRequiredCount: Number(item.reviewRequiredCount ?? 0),
    staleDataCount: Number(item.staleDataCount ?? 0),
    latestOeePercent: readNumber(item.latestOeePercent),
    capabilities: {
      canRead: capabilities?.canRead === true,
      canManage: capabilities?.canManage === true,
    },
  };
}

function parseWriteResult(payload: unknown): ProcessOeeWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    version: Number(item.version),
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

function mapFailureStatus(status: number): ProcessOeeApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<ProcessOeeApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status),
        message: `Proses OEE isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Proses OEE API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

async function mutateJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<ProcessOeeApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method: "POST",
      headers: {
        ...context.requestHeaders,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    if (!response.ok) {
      let message = `Proses OEE işlemi başarısız (${response.status}).`;
      try {
        const problem = (await response.json()) as { title?: string };
        if (problem.title) {
          message = problem.title;
        }
      } catch {
        // ignore parse errors
      }

      return {
        status: "failure",
        code: mapFailureStatus(response.status),
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
      message: "Proses OEE API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

export function getProcessOeeWorkspaceSummary(
  context: ProductionOrderRequestContext,
): Promise<ProcessOeeApiResult<ProcessOeeWorkspaceSummary>> {
  return getJson(
    context,
    "/api/process-oee/workspace-summary",
    undefined,
    parseWorkspaceSummary,
  );
}

export function listProcessOeeAssessments(
  context: ProductionOrderRequestContext,
  filters: ProcessOeeAssessmentListFilters = {},
): Promise<ProcessOeeApiResult<readonly ProcessOeeAssessmentSummary[]>> {
  return getJson(
    context,
    "/api/process-oee/assessments",
    {
      machineAssetId: filters.machineAssetId,
      productionOrderReference: filters.productionOrderReference,
      status: filters.status,
      evaluatedFromUtc: filters.evaluatedFromUtc,
      evaluatedToUtc: filters.evaluatedToUtc,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseAssessmentSummaryList,
  );
}

export function getProcessOeeAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<ProcessOeeApiResult<ProcessOeeAssessmentDetail>> {
  return getJson(
    context,
    `/api/process-oee/assessments/${encodeURIComponent(assessmentId)}`,
    undefined,
    parseAssessmentDetail,
  );
}

function parseLossDriver(value: unknown) {
  const item = value as Record<string, unknown>;
  const category = String(item.category);
  const severity = String(item.severity);
  const explanationCode = String(item.explanationCode);
  const freshnessState = String(item.freshnessState);
  return {
    id: String(item.id),
    reasonCode: String(item.reasonCode),
    category,
    categoryLabel: getLossCategoryLabel(category),
    severity,
    severityLabel: getDriverSeverityLabel(severity),
    operationalPriority: Number(item.operationalPriority ?? 0),
    observedValue: readNumber(item.observedValue),
    expectedValue: readNumber(item.expectedValue),
    absoluteLossValue: readNumber(item.absoluteLossValue),
    affectedDurationSeconds: readNumber(item.affectedDurationSeconds),
    affectedQuantity: readNumber(item.affectedQuantity),
    sourceReferences: Array.isArray(item.sourceReferences)
      ? item.sourceReferences.map(String)
      : [],
    evidenceTimestampUtc: readString(item.evidenceTimestampUtc),
    freshnessState,
    freshnessLabel: getLossFreshnessLabel(freshnessState),
    explanationLabel: getLossExplanationLabel(explanationCode),
    dataLimitations: Array.isArray(item.dataLimitations)
      ? item.dataLimitations.map(String)
      : [],
    ruleSetVersion: String(item.ruleSetVersion),
    isLeadingLoss: readBoolean(item.isLeadingLoss),
    normalizedImpactPoints: readNumber(item.normalizedImpactPoints),
  };
}

function parseLossDetail(payload: unknown): ProcessOeeLossDetail {
  const item = payload as Record<string, unknown>;
  const assessmentStatus = String(item.assessmentStatus);
  const drivers = Array.isArray(item.drivers) ? item.drivers.map(parseLossDriver) : [];
  const leadingLossRaw = item.leadingLoss;
  return {
    assessmentId: String(item.assessmentId),
    evaluationWindowId: String(item.evaluationWindowId),
    assessmentStatus,
    assessmentStatusLabel: getProcessOeeStatusLabel(
      assessmentStatus,
      readString(item.assessmentStatusLabel),
    ),
    ruleSetVersion: String(item.ruleSetVersion),
    leadingLossMethod: String(item.leadingLossMethod),
    leadingLoss: leadingLossRaw ? parseLossDriver(leadingLossRaw) : undefined,
    drivers,
    evidence: Array.isArray(item.evidence) ? item.evidence.map(parseEvidence) : [],
    missingFields: Array.isArray(item.missingFields)
      ? item.missingFields.map(String)
      : [],
    conflicts: Array.isArray(item.conflicts) ? item.conflicts.map(String) : [],
    sourceSystem: readString(item.sourceSystem),
    sourceFreshnessUtc: readString(item.sourceFreshnessUtc),
    isSimulatorDerived: readBoolean(item.isSimulatorDerived),
    calculationHonestyDisclaimer: String(item.calculationHonestyDisclaimer ?? ""),
    simulatorHonestyDisclaimer: readString(item.simulatorHonestyDisclaimer),
  };
}

export function getProcessOeeAssessmentLosses(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<ProcessOeeApiResult<ProcessOeeLossDetail>> {
  return getJson(
    context,
    `/api/process-oee/assessments/${encodeURIComponent(assessmentId)}/losses`,
    undefined,
    parseLossDetail,
  );
}

export function evaluateProcessOeeWindow(
  context: ProductionOrderRequestContext,
  windowId: string,
  idempotencyKey: string,
): Promise<ProcessOeeApiResult<ProcessOeeWriteResult>> {
  return mutateJson(
    context,
    `/api/process-oee/evaluation-windows/${encodeURIComponent(windowId)}/evaluations`,
    { idempotencyKey },
    parseWriteResult,
  );
}

export async function probeProcessOeeReadPermission(
  context: ProductionOrderRequestContext,
): Promise<boolean> {
  const result = await getProcessOeeWorkspaceSummary(context);
  if (result.status === "success") {
    return result.value.capabilities.canRead;
  }

  return false;
}

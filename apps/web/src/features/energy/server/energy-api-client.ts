import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./energy-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { EnergyWriteResult } from "../domain/energy-types";

export type EnergyApiFailure = Readonly<{
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
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type EnergyApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | EnergyApiFailure;

export type RawEnergyWorkspace = Readonly<{
  activeMeasurementPointCount: number;
  recentWindowCount: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  hasDevelopmentPilotData: boolean;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawEnergyIntelligenceWorkspace = Readonly<{
  baselineCount: number;
  activeTargetCount: number;
  recentAssessmentCount: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  hasDevelopmentPilotData: boolean;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawEnergyMeasurementPoint = Readonly<{
  id: string;
  pointCode: string;
  displayName: string;
  measurementScope: string;
  machineAssetId: string | null;
  machineAssetLabel: string | null;
  edgeAssetReference: string | null;
  equipmentCategory: string;
  measurementUnit: string;
  status: string;
  sourceProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawEnergyConsumptionWindow = Readonly<{
  id: string;
  measurementPointId: string;
  measurementPointCode: string;
  windowStartUtc: string;
  windowEndUtc: string;
  consumedKwh: number;
  averagePowerKw: number | null;
  maximumDemandKw: number | null;
  calculationMethod: string;
  evidenceStatus: string;
  readingCount: number;
  freshnessState: string;
  ruleSetVersion: string;
  costAmount: number | null;
  costCurrencyCode: string | null;
  missingDataExplanation: string | null;
  calculatedAtUtc: string;
  isPilotData: boolean;
}>;

export type RawEnergyAllocation = Readonly<{
  id: string;
  consumptionWindowId: string;
  targetType: string;
  targetReference: string | null;
  allocatedKwh: number;
  allocationMethod: string;
  evidenceSource: string;
  confidenceStatus: string;
  ruleSetVersion: string;
  calculatedAtUtc: string;
}>;

export type RawEnergyBaseline = Readonly<{
  id: string;
  baselineReference: string;
  comparisonDimension: string;
  measurementPointCode: string | null;
  machineAssetId: string | null;
  shiftReference: string | null;
  productionContextReference: string | null;
  reportingWindowDurationSeconds: number;
  sampleCount: number;
  baselineConsumedKwh: number | null;
  baselineMaximumDemandKw: number | null;
  medianCenter: number | null;
  madDispersion: number | null;
  normalizedDispersion: number | null;
  relativeTolerance: number | null;
  upperThresholdConsumedKwh: number | null;
  upperThresholdDemandKw: number | null;
  calculationMethod: string;
  evidenceStatus: string;
  ruleSetVersion: string;
  provenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawEnergyTarget = Readonly<{
  id: string;
  targetReference: string;
  targetScope: string;
  scopeReference: string;
  targetMetric: string;
  targetValue: number;
  unit: string;
  tolerance: number;
  effectiveFromUtc: string;
  effectiveToUtc: string;
  sourceProvenance: string;
  status: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawEnergyAssessmentSummary = Readonly<{
  id: string;
  assessmentReference: string;
  comparisonDimension: string;
  measurementPointCode: string | null;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  actualConsumedKwh: number | null;
  actualMaximumDemandKw: number | null;
  varianceConsumedKwh: number | null;
  variancePercent: number | null;
  outcome: string;
  leadingDriverCode: string;
  potentialReducibleKwh: number | null;
  estimatedCostImpact: number | null;
  costCurrencyCode: string | null;
  opportunityLabel: string | null;
  evidenceStatus: string;
  evaluatedAtUtc: string;
  isPilotData: boolean;
}>;

export type RawEnergyAssessmentDriver = Readonly<{
  id: string;
  reasonCode: string;
  labelTr: string;
  severity: string;
  category: string;
  actualValue: number | null;
  actualUnit: string | null;
  comparisonValue: number | null;
  comparisonUnit: string | null;
  normalizedVariance: number | null;
  explanation: string | null;
  evidenceSource: string | null;
  freshnessState: string;
  isLeadingDriver: boolean;
}>;

export type RawEnergyAssessmentDetail = Readonly<{
  summary: RawEnergyAssessmentSummary;
  baselineReference: string | null;
  targetReference: string | null;
  drivers: readonly RawEnergyAssessmentDriver[];
}>;

function readString(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}

function readNumber(value: unknown): number | null {
  if (value === null || value === undefined) return null;
  return typeof value === "number" ? value : null;
}

function readBoolean(value: unknown): boolean {
  return value === true;
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

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): EnergyApiFailure["code"] {
  if (failure?.kind === "subscription-required") return "subscription-required";
  if (failure?.kind === "unauthenticated") return "unauthenticated";
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
): Promise<EnergyApiResult<T>> {
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
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `Enerji isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Enerji API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

async function mutateJson<T>(
  context: ProductionOrderRequestContext,
  method: "POST" | "PUT",
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<EnergyApiResult<T>> {
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
      let message = `Enerji işlemi başarısız (${response.status}).`;
      try {
        const problem = (await response.json()) as { title?: string; detail?: string };
        if (problem.title) message = problem.title;
        else if (problem.detail) message = problem.detail;
      } catch {
        // ignore
      }

      return {
        status: "failure",
        code: mapFailureStatus(response.status, failure),
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
      message: "Enerji API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

function parseCapabilities(value: unknown) {
  const item = value as Record<string, unknown>;
  return {
    canRead: readBoolean(item.canRead),
    canManage: readBoolean(item.canManage),
  };
}

function parseWorkspace(payload: unknown): RawEnergyWorkspace {
  const item = payload as Record<string, unknown>;
  return {
    activeMeasurementPointCount: Number(item.activeMeasurementPointCount ?? 0),
    recentWindowCount: Number(item.recentWindowCount ?? 0),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    provenanceDisclaimer: String(item.provenanceDisclaimer ?? ""),
    hasDevelopmentPilotData: readBoolean(item.hasDevelopmentPilotData),
    capabilities: parseCapabilities(item.capabilities),
  };
}

function parseIntelligenceWorkspace(payload: unknown): RawEnergyIntelligenceWorkspace {
  const item = payload as Record<string, unknown>;
  return {
    baselineCount: Number(item.baselineCount ?? 0),
    activeTargetCount: Number(item.activeTargetCount ?? 0),
    recentAssessmentCount: Number(item.recentAssessmentCount ?? 0),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    provenanceDisclaimer: String(item.provenanceDisclaimer ?? ""),
    hasDevelopmentPilotData: readBoolean(item.hasDevelopmentPilotData),
    capabilities: parseCapabilities(item.capabilities),
  };
}

function parseMeasurementPoint(value: unknown): RawEnergyMeasurementPoint {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    pointCode: String(item.pointCode),
    displayName: String(item.displayName),
    measurementScope: String(item.measurementScope),
    machineAssetId: readString(item.machineAssetId),
    machineAssetLabel: readString(item.machineAssetLabel),
    edgeAssetReference: readString(item.edgeAssetReference),
    equipmentCategory: String(item.equipmentCategory),
    measurementUnit: String(item.measurementUnit ?? "kWh"),
    status: String(item.status),
    sourceProvenance: String(item.sourceProvenance ?? ""),
    version: Number(item.version ?? 1),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseConsumptionWindow(value: unknown): RawEnergyConsumptionWindow {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    measurementPointId: String(item.measurementPointId),
    measurementPointCode: String(item.measurementPointCode ?? ""),
    windowStartUtc: String(item.windowStartUtc),
    windowEndUtc: String(item.windowEndUtc),
    consumedKwh: Number(item.consumedKwh ?? 0),
    averagePowerKw: readNumber(item.averagePowerKw),
    maximumDemandKw: readNumber(item.maximumDemandKw),
    calculationMethod: String(item.calculationMethod ?? ""),
    evidenceStatus: String(item.evidenceStatus ?? ""),
    readingCount: Number(item.readingCount ?? 0),
    freshnessState: String(item.freshnessState ?? "unknown"),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    costAmount: readNumber(item.costAmount),
    costCurrencyCode: readString(item.costCurrencyCode),
    missingDataExplanation: readString(item.missingDataExplanation),
    calculatedAtUtc: String(item.calculatedAtUtc ?? ""),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseAllocation(value: unknown): RawEnergyAllocation {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    consumptionWindowId: String(item.consumptionWindowId),
    targetType: String(item.targetType),
    targetReference: readString(item.targetReference),
    allocatedKwh: Number(item.allocatedKwh ?? 0),
    allocationMethod: String(item.allocationMethod ?? ""),
    evidenceSource: String(item.evidenceSource ?? ""),
    confidenceStatus: String(item.confidenceStatus ?? ""),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    calculatedAtUtc: String(item.calculatedAtUtc ?? ""),
  };
}

function parseBaseline(value: unknown): RawEnergyBaseline {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    baselineReference: String(item.baselineReference),
    comparisonDimension: String(item.comparisonDimension),
    measurementPointCode: readString(item.measurementPointCode),
    machineAssetId: readString(item.machineAssetId),
    shiftReference: readString(item.shiftReference),
    productionContextReference: readString(item.productionContextReference),
    reportingWindowDurationSeconds: Number(item.reportingWindowDurationSeconds ?? 0),
    sampleCount: Number(item.sampleCount ?? 0),
    baselineConsumedKwh: readNumber(item.baselineConsumedKwh),
    baselineMaximumDemandKw: readNumber(item.baselineMaximumDemandKw),
    medianCenter: readNumber(item.medianCenter),
    madDispersion: readNumber(item.madDispersion),
    normalizedDispersion: readNumber(item.normalizedDispersion),
    relativeTolerance: readNumber(item.relativeTolerance),
    upperThresholdConsumedKwh: readNumber(item.upperThresholdConsumedKwh),
    upperThresholdDemandKw: readNumber(item.upperThresholdDemandKw),
    calculationMethod: String(item.calculationMethod ?? ""),
    evidenceStatus: String(item.evidenceStatus ?? ""),
    ruleSetVersion: String(item.ruleSetVersion ?? ""),
    provenance: String(item.provenance ?? ""),
    version: Number(item.version ?? 1),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseTarget(value: unknown): RawEnergyTarget {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    targetReference: String(item.targetReference),
    targetScope: String(item.targetScope),
    scopeReference: String(item.scopeReference ?? ""),
    targetMetric: String(item.targetMetric),
    targetValue: Number(item.targetValue ?? 0),
    unit: String(item.unit ?? ""),
    tolerance: Number(item.tolerance ?? 0),
    effectiveFromUtc: String(item.effectiveFromUtc),
    effectiveToUtc: String(item.effectiveToUtc),
    sourceProvenance: String(item.sourceProvenance ?? ""),
    status: String(item.status ?? ""),
    version: Number(item.version ?? 1),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseAssessmentSummary(value: unknown): RawEnergyAssessmentSummary {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    assessmentReference: String(item.assessmentReference),
    comparisonDimension: String(item.comparisonDimension),
    measurementPointCode: readString(item.measurementPointCode),
    evaluationWindowStartUtc: String(item.evaluationWindowStartUtc),
    evaluationWindowEndUtc: String(item.evaluationWindowEndUtc),
    actualConsumedKwh: readNumber(item.actualConsumedKwh),
    actualMaximumDemandKw: readNumber(item.actualMaximumDemandKw),
    varianceConsumedKwh: readNumber(item.varianceConsumedKwh),
    variancePercent: readNumber(item.variancePercent),
    outcome: String(item.outcome ?? ""),
    leadingDriverCode: String(item.leadingDriverCode ?? ""),
    potentialReducibleKwh: readNumber(item.potentialReducibleKwh),
    estimatedCostImpact: readNumber(item.estimatedCostImpact),
    costCurrencyCode: readString(item.costCurrencyCode),
    opportunityLabel: readString(item.opportunityLabel),
    evidenceStatus: String(item.evidenceStatus ?? ""),
    evaluatedAtUtc: String(item.evaluatedAtUtc ?? ""),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseAssessmentDriver(value: unknown): RawEnergyAssessmentDriver {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    reasonCode: String(item.reasonCode),
    labelTr: String(item.labelTr ?? item.reasonCode),
    severity: String(item.severity ?? ""),
    category: String(item.category ?? ""),
    actualValue: readNumber(item.actualValue),
    actualUnit: readString(item.actualUnit),
    comparisonValue: readNumber(item.comparisonValue),
    comparisonUnit: readString(item.comparisonUnit),
    normalizedVariance: readNumber(item.normalizedVariance),
    explanation: readString(item.explanation),
    evidenceSource: readString(item.evidenceSource),
    freshnessState: String(item.freshnessState ?? "unknown"),
    isLeadingDriver: readBoolean(item.isLeadingDriver),
  };
}

function parseAssessmentDetail(payload: unknown): RawEnergyAssessmentDetail {
  const item = payload as Record<string, unknown>;
  const assessment = item.assessment as Record<string, unknown>;
  const summary = parseAssessmentSummary(assessment.summary ?? assessment);
  const drivers = Array.isArray(assessment.drivers)
    ? assessment.drivers.map(parseAssessmentDriver)
    : Array.isArray(item.drivers)
      ? item.drivers.map(parseAssessmentDriver)
      : [];

  return {
    summary,
    baselineReference: readString(assessment.baselineReference ?? item.baselineReference),
    targetReference: readString(assessment.targetReference ?? item.targetReference),
    drivers,
  };
}

function parseWriteResult(payload: unknown): EnergyWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id ?? item.targetReference ?? item.assessmentReference ?? ""),
    version: Number(item.version ?? 1),
    idempotentReplay: readBoolean(item.idempotentReplay),
  };
}

function parseListItems<T>(payload: unknown, parser: (value: unknown) => T): readonly T[] {
  const item = payload as Record<string, unknown>;
  if (!Array.isArray(item.items)) return [];
  return item.items.map(parser);
}

export function getEnergyWorkspace(
  context: ProductionOrderRequestContext,
): Promise<EnergyApiResult<RawEnergyWorkspace>> {
  return getJson(context, "/api/energy/workspace", undefined, parseWorkspace);
}

export function getEnergyIntelligenceWorkspace(
  context: ProductionOrderRequestContext,
): Promise<EnergyApiResult<RawEnergyIntelligenceWorkspace>> {
  return getJson(context, "/api/energy/intelligence/workspace", undefined, parseIntelligenceWorkspace);
}

export function listEnergyMeasurementPoints(
  context: ProductionOrderRequestContext,
  query: Record<string, string | undefined> = {},
): Promise<EnergyApiResult<readonly RawEnergyMeasurementPoint[]>> {
  return getJson(
    context,
    "/api/energy/measurement-points",
    { page: "1", pageSize: "100", ...query },
    (payload) => parseListItems(payload, parseMeasurementPoint),
  );
}

export function getEnergyMeasurementPoint(
  context: ProductionOrderRequestContext,
  reference: string,
): Promise<EnergyApiResult<RawEnergyMeasurementPoint>> {
  return getJson(
    context,
    `/api/energy/measurement-points/${encodeURIComponent(reference)}`,
    undefined,
    (payload) => {
      const item = payload as Record<string, unknown>;
      return parseMeasurementPoint(item.measurementPoint);
    },
  );
}

export function listEnergyConsumptionWindows(
  context: ProductionOrderRequestContext,
  query: Record<string, string | undefined> = {},
): Promise<EnergyApiResult<readonly RawEnergyConsumptionWindow[]>> {
  return getJson(
    context,
    "/api/energy/consumption",
    { page: "1", pageSize: "100", ...query },
    (payload) => parseListItems(payload, parseConsumptionWindow),
  );
}

export function listEnergyAllocations(
  context: ProductionOrderRequestContext,
  query: Record<string, string | undefined> = {},
): Promise<EnergyApiResult<readonly RawEnergyAllocation[]>> {
  return getJson(
    context,
    "/api/energy/allocations",
    { page: "1", pageSize: "100", ...query },
    (payload) => parseListItems(payload, parseAllocation),
  );
}

export function listEnergyBaselines(
  context: ProductionOrderRequestContext,
): Promise<EnergyApiResult<readonly RawEnergyBaseline[]>> {
  return getJson(
    context,
    "/api/energy/baselines",
    { page: "1", pageSize: "100" },
    (payload) => parseListItems(payload, parseBaseline),
  );
}

export function listEnergyTargets(
  context: ProductionOrderRequestContext,
): Promise<EnergyApiResult<readonly RawEnergyTarget[]>> {
  return getJson(
    context,
    "/api/energy/targets",
    { page: "1", pageSize: "100" },
    (payload) => parseListItems(payload, parseTarget),
  );
}

export function listEnergyAssessments(
  context: ProductionOrderRequestContext,
  query: Record<string, string | undefined> = {},
): Promise<EnergyApiResult<readonly RawEnergyAssessmentSummary[]>> {
  return getJson(
    context,
    "/api/energy/assessments",
    { page: "1", pageSize: "100", ...query },
    (payload) => parseListItems(payload, parseAssessmentSummary),
  );
}

export function getEnergyAssessment(
  context: ProductionOrderRequestContext,
  reference: string,
): Promise<EnergyApiResult<RawEnergyAssessmentDetail>> {
  return getJson(
    context,
    `/api/energy/assessments/${encodeURIComponent(reference)}`,
    undefined,
    parseAssessmentDetail,
  );
}

export function createEnergyTarget(
  context: ProductionOrderRequestContext,
  body: unknown,
): Promise<EnergyApiResult<EnergyWriteResult>> {
  return mutateJson(context, "POST", "/api/energy/targets", body, parseWriteResult);
}

export function updateEnergyTarget(
  context: ProductionOrderRequestContext,
  reference: string,
  body: unknown,
): Promise<EnergyApiResult<EnergyWriteResult>> {
  return mutateJson(
    context,
    "PUT",
    `/api/energy/targets/${encodeURIComponent(reference)}`,
    body,
    parseWriteResult,
  );
}

export function evaluateEnergyAssessment(
  context: ProductionOrderRequestContext,
  body: unknown,
): Promise<EnergyApiResult<EnergyWriteResult>> {
  return mutateJson(context, "POST", "/api/energy/assessments/evaluate", body, parseWriteResult);
}

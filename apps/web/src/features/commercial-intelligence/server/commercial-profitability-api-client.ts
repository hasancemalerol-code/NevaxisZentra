import { refusalCode } from "@/lib/api/api-failure-classifier";
import "./commercial-intelligence-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  CommercialIntelligenceApiFailure,
  CommercialIntelligenceApiResult,
} from "./commercial-intelligence-api-client";

export type RawCommercialProfitabilityWorkspace = Readonly<{
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  defaultMinimumContributionMarginRatio: number;
  defaultMaximumEvidenceAgeDays: number;
  defaultRequiredCostComponentKinds: string;
  realRevenueMappingComplete: boolean;
  realCostMappingComplete: boolean;
  fullCostMappingComplete: boolean;
  realProfitabilityMappingComplete: boolean;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawCommercialProfitabilityProfile = Readonly<{
  id: string;
  profileCode: string;
  name: string;
  status: string;
  minimumContributionMarginRatio: number;
  maximumEvidenceAgeDays: number;
  requiredCostComponentKinds: string;
  requiredCurrencyCode: string;
  requiredUnitOfMeasure: string;
  requiredGrossNetBasis: string;
  requiredTaxInclusion: string;
  requiredDiscountInclusion: string;
  requiredFreightInclusion: string;
  allowedEvidenceStatuses: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawCommercialProfitabilityComparisonSummary = Readonly<{
  id: string;
  comparisonReference: string;
  profileId: string;
  profileCode: string;
  comparisonType: string;
  materialId: string | null;
  customerReference: string | null;
  comparisonOutcome: string;
  evidenceStatus: string;
  aggregationMethod: string;
  candidateAssessmentCount: number;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  referenceMedianContributionMarginRatio: number | null;
  currentMedianContributionMarginRatio: number | null;
  contributionMarginDelta: number | null;
  referenceWindowStartUtc: string | null;
  referenceWindowEndUtc: string | null;
  currentWindowStartUtc: string;
  currentWindowEndUtc: string;
  leadingDriverCode: string | null;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  createdAtUtc: string;
  isPilotData: boolean;
  realProfitabilityMappingComplete: boolean;
}>;

export type RawCommercialProfitabilityComparisonResult = Readonly<{
  id: string;
  resultDimension: string;
  canonicalResultReference: string;
  customerReference: string | null;
  materialId: string | null;
  componentKind: string | null;
  candidateAssessmentCount: number;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  completeCoverageCount: number;
  partialCoverageCount: number;
  negativeContributionCount: number;
  belowThresholdCount: number;
  referenceComparableAssessmentCount: number;
  currentComparableAssessmentCount: number;
  referenceExcludedAssessmentCount: number;
  currentExcludedAssessmentCount: number;
  referenceMedianContributionMarginRatio: number | null;
  currentMedianContributionMarginRatio: number | null;
  contributionMarginDelta: number | null;
  referenceMedianUnitContribution: number | null;
  currentMedianUnitContribution: number | null;
  referenceMedianUnitCost: number | null;
  currentMedianUnitCost: number | null;
  signedUnitCostChange: number | null;
  signedUnitCostChangeRatio: number | null;
  referenceMedianCostShare: number | null;
  currentMedianCostShare: number | null;
  costShareDelta: number | null;
  positionState: string | null;
  contributionRank: number | null;
  isTied: boolean;
  evidenceStatus: string;
  sortOrder: number;
}>;

export type RawCommercialProfitabilityComparisonDetail = RawCommercialProfitabilityComparisonSummary & Readonly<{
  profileVersion: string;
  completeCoverageCount: number;
  partialCoverageCount: number;
  insufficientDataCount: number;
  sourceConflictCount: number;
  negativeContributionCount: number;
  belowThresholdCount: number;
  referenceCandidateAssessmentCount: number;
  referenceComparableAssessmentCount: number;
  referenceExcludedAssessmentCount: number;
  referenceCompleteCoverageCount: number;
  referencePartialCoverageCount: number;
  referenceInsufficientDataCount: number;
  referenceSourceConflictCount: number;
  referenceNegativeContributionCount: number;
  referenceBelowThresholdCount: number;
  currentCandidateAssessmentCount: number;
  currentComparableAssessmentCount: number;
  currentExcludedAssessmentCount: number;
  currentCompleteCoverageCount: number;
  currentPartialCoverageCount: number;
  currentInsufficientDataCount: number;
  currentSourceConflictCount: number;
  currentNegativeContributionCount: number;
  currentBelowThresholdCount: number;
  referenceMedianUnitContribution: number | null;
  currentMedianUnitContribution: number | null;
  sourceProvenance: string;
  results: readonly RawCommercialProfitabilityComparisonResult[];
  drivers: ReadonlyArray<Readonly<{
    id: string;
    comparisonResultId: string | null;
    reasonCode: string;
    severity: string;
    actualValue: number | null;
    referenceValue: number | null;
    thresholdValue: number | null;
    unit: string | null;
    normalizedVariance: number | null;
    affectedAssessmentCount: number;
    supportingText: string;
    sortOrder: number;
    isLeadingDriver: boolean;
  }>>;
}>;

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

function mapFailureStatus(status: number, payload: unknown): CommercialIntelligenceApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return refusalCode(status, payload);
  if (status === 404) return "not-found";
  if (status === 409) {
    const code = typeof payload === "object" && payload && "failureCode" in payload
      ? String((payload as { failureCode?: string }).failureCode ?? "")
      : "";
    if (code.includes("idempotency")) return "idempotency-conflict";
    return "concurrency-conflict";
  }
  if (status === 400) return "invalid-request";
  return "source-unavailable";
}

function failureMessage(status: number, payload: unknown): string {
  if (typeof payload === "object" && payload) {
    const reason = "failureReason" in payload ? String((payload as { failureReason?: string }).failureReason ?? "") : "";
    const detail = "detail" in payload ? String((payload as { detail?: string }).detail ?? "") : "";
    const title = "title" in payload ? String((payload as { title?: string }).title ?? "") : "";
    return reason || detail || title || `Kârlılık karşılaştırma isteği başarısız (${status}).`;
  }
  return `Kârlılık karşılaştırma isteği başarısız (${status}).`;
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<CommercialIntelligenceApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    let payload: unknown = null;
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }

    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status, payload),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }

    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Kârlılık karşılaştırma API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

export async function getCommercialProfitabilityWorkspace(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<RawCommercialProfitabilityWorkspace>> {
  return getJson(context, "/api/commercial-intelligence/profitability/workspace", undefined, (payload) => {
    const data = payload as RawCommercialProfitabilityWorkspace & {
      capabilities?: { canRead: boolean; canManage: boolean };
    };
    return {
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
      defaultMinimumContributionMarginRatio: data.defaultMinimumContributionMarginRatio ?? 0,
      defaultMaximumEvidenceAgeDays: data.defaultMaximumEvidenceAgeDays ?? 0,
      defaultRequiredCostComponentKinds: data.defaultRequiredCostComponentKinds ?? "",
      realRevenueMappingComplete: data.realRevenueMappingComplete === true,
      realCostMappingComplete: data.realCostMappingComplete === true,
      fullCostMappingComplete: data.fullCostMappingComplete === true,
      realProfitabilityMappingComplete: data.realProfitabilityMappingComplete === true,
      capabilities: data.capabilities ?? { canRead: true, canManage: false },
    };
  });
}

export async function listCommercialProfitabilityProfiles(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialProfitabilityProfile[];
  ruleSetVersion: string;
  provenanceDisclaimer: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/profitability/profiles", undefined, (payload) => {
    const data = payload as {
      items: RawCommercialProfitabilityProfile[];
      ruleSetVersion: string;
      provenanceDisclaimer: string;
    };
    return {
      items: data.items ?? [],
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
    };
  });
}

export async function listCommercialProfitabilityComparisons(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    comparisonType?: string;
    profileId?: string;
    materialId?: string;
    customerReference?: string;
    outcome?: string;
    evidenceStatus?: string;
    windowFromUtc?: string;
    windowToUtc?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialProfitabilityComparisonSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
  realProfitabilityMappingComplete: boolean;
}>>> {
  return getJson(context, "/api/commercial-intelligence/profitability/comparisons", {
    comparisonType: filters.comparisonType,
    profileId: filters.profileId,
    materialId: filters.materialId,
    customerReference: filters.customerReference,
    outcome: filters.outcome,
    evidenceStatus: filters.evidenceStatus,
    windowFromUtc: filters.windowFromUtc,
    windowToUtc: filters.windowToUtc,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialProfitabilityComparisonSummary[];
      page: number;
      pageSize: number;
      ruleSetVersion: string;
      realProfitabilityMappingComplete: boolean;
    };
    return {
      items: data.items ?? [],
      page: data.page ?? 1,
      pageSize: data.pageSize ?? 25,
      ruleSetVersion: data.ruleSetVersion ?? "",
      realProfitabilityMappingComplete: data.realProfitabilityMappingComplete === true,
    };
  });
}

export async function getCommercialProfitabilityComparison(
  context: ProductionOrderRequestContext,
  comparisonId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialProfitabilityComparisonDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/profitability/comparisons/${comparisonId}`,
    undefined,
    (payload) => payload as RawCommercialProfitabilityComparisonDetail,
  );
}

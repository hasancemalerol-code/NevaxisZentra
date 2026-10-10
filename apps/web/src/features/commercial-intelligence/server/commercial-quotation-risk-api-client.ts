import { refusalCode } from "@/lib/api/api-failure-classifier";
import "./commercial-intelligence-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  CommercialIntelligenceApiFailure,
  CommercialIntelligenceApiResult,
} from "./commercial-intelligence-api-client";

export type RawCommercialQuotationRiskWorkspace = Readonly<{
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  defaultMinimumComparableObservations: number;
  defaultMaximumNegativePriceVarianceRatio: number;
  defaultMaximumPositivePriceVarianceRatio: number;
  defaultMaximumPaymentTermDays: number;
  defaultExpiryWarningDays: number;
  realQuotationMappingComplete: boolean;
  comparisonRuleSetVersion: string;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawCommercialQuotationRiskProfile = Readonly<{
  id: string;
  profileCode: string;
  name: string;
  status: string;
  referenceWindowDurationDays: number;
  minimumComparableObservations: number;
  maximumNegativePriceVarianceRatio: number;
  maximumPositivePriceVarianceRatio: number;
  maximumPaymentTermDays: number;
  expiryWarningDays: number;
  maximumEvidenceAgeDays: number;
  maximumRiskIncidenceDelta: number;
  allowedEvidenceStatuses: string;
  allowedSalesEvidenceKinds: string;
  requiredTaxInclusion: string;
  requiredDiscountInclusion: string;
  requiredFreightInclusion: string;
  requiredGrossNetBasis: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawCommercialQuotationRiskAssessmentSummary = Readonly<{
  id: string;
  assessmentReference: string;
  profileId: string;
  profileCode: string;
  evidenceObservationId: string;
  quotationReference: string;
  customerReference: string;
  materialId: string;
  outcome: string;
  quotedComparableUnitPrice: number;
  referenceMedianUnitPrice: number | null;
  signedPriceVarianceRatio: number | null;
  candidateObservationCount: number;
  comparableObservationCount: number;
  excludedObservationCount: number;
  paymentTermDays: number;
  remainingValidityDays: number | null;
  currencyCode: string;
  unitOfMeasure: string;
  referenceWindowStartUtc: string;
  referenceWindowEndUtc: string;
  leadingDriverCode: string | null;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  createdAtUtc: string;
  isPilotData: boolean;
}>;

export type RawCommercialQuotationRiskAssessmentDetail = RawCommercialQuotationRiskAssessmentSummary & Readonly<{
  profileVersion: string;
  grossNetBasis: string;
  taxBasis: string;
  discountBasis: string;
  freightBasis: string;
  evidenceStatus: string;
  sourceProvenance: string;
  drivers: ReadonlyArray<Readonly<{
    id: string;
    reasonCode: string;
    severity: string;
    actualValue: number | null;
    thresholdValue: number | null;
    unit: string | null;
    normalizedVariance: number | null;
    affectedObservationCount: number;
    supportingText: string;
    sortOrder: number;
    isLeadingDriver: boolean;
  }>>;
}>;

export type RawCommercialQuotationRiskComparisonSummary = Readonly<{
  id: string;
  comparisonReference: string;
  profileId: string;
  profileCode: string;
  comparisonType: string;
  materialId: string | null;
  customerReference: string | null;
  comparisonOutcome: string;
  evidenceStatus: string;
  candidateAssessmentCount: number;
  comparableAssessmentCount: number;
  excludedAssessmentCount: number;
  referenceRiskIncidence: number | null;
  currentRiskIncidence: number | null;
  riskIncidenceDelta: number | null;
  referenceWindowStartUtc: string | null;
  referenceWindowEndUtc: string | null;
  currentWindowStartUtc: string;
  currentWindowEndUtc: string;
  leadingDriverCode: string | null;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  createdAtUtc: string;
  isPilotData: boolean;
  realQuotationMappingComplete: boolean;
}>;

export type RawCommercialQuotationRiskComparisonDetail = RawCommercialQuotationRiskComparisonSummary & Readonly<{
  profileVersion: string;
  riskBearingAssessmentCount: number;
  insufficientDataCount: number;
  sourceConflictCount: number;
  priceRiskCount: number;
  expiryRiskCount: number;
  paymentTermRiskCount: number;
  referenceCandidateAssessmentCount: number | null;
  referenceComparableAssessmentCount: number | null;
  referenceExcludedAssessmentCount: number | null;
  referenceRiskBearingAssessmentCount: number | null;
  currentCandidateAssessmentCount: number | null;
  currentComparableAssessmentCount: number | null;
  currentExcludedAssessmentCount: number | null;
  currentRiskBearingAssessmentCount: number | null;
  sourceProvenance: string;
  customerResults: ReadonlyArray<Readonly<{
    id: string;
    customerReference: string;
    candidateAssessmentCount: number;
    comparableAssessmentCount: number;
    excludedAssessmentCount: number;
    riskBearingAssessmentCount: number;
    riskIncidenceFraction: number | null;
    priceRiskCount: number;
    expiryRiskCount: number;
    paymentTermRiskCount: number;
    positionState: string;
    riskRank: number | null;
    isTied: boolean;
    evidenceStatus: string;
  }>>;
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
  return "source-unavailable";
}

function failureMessage(status: number, payload: unknown): string {
  if (typeof payload === "object" && payload) {
    const reason = "failureReason" in payload ? String((payload as { failureReason?: string }).failureReason ?? "") : "";
    const detail = "detail" in payload ? String((payload as { detail?: string }).detail ?? "") : "";
    const title = "title" in payload ? String((payload as { title?: string }).title ?? "") : "";
    return reason || detail || title || `Teklif risk isteği başarısız (${status}).`;
  }
  return `Teklif risk isteği başarısız (${status}).`;
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
      message: "Teklif risk API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

async function writeJson<T>(
  context: ProductionOrderRequestContext,
  method: "POST" | "PUT",
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<CommercialIntelligenceApiResult<T>> {
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
      message: "Teklif risk yazma isteği başarısız oldu.",
      retryable: true,
    };
  }
}

export async function getCommercialQuotationRiskWorkspace(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<RawCommercialQuotationRiskWorkspace>> {
  return getJson(context, "/api/commercial-intelligence/quotation-risks/workspace", undefined, (payload) => {
    const data = payload as RawCommercialQuotationRiskWorkspace & {
      capabilities?: { canRead: boolean; canManage: boolean };
    };
    return {
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
      defaultMinimumComparableObservations: data.defaultMinimumComparableObservations ?? 0,
      defaultMaximumNegativePriceVarianceRatio: data.defaultMaximumNegativePriceVarianceRatio ?? 0,
      defaultMaximumPositivePriceVarianceRatio: data.defaultMaximumPositivePriceVarianceRatio ?? 0,
      defaultMaximumPaymentTermDays: data.defaultMaximumPaymentTermDays ?? 0,
      defaultExpiryWarningDays: data.defaultExpiryWarningDays ?? 0,
      realQuotationMappingComplete: data.realQuotationMappingComplete === true,
      comparisonRuleSetVersion: data.comparisonRuleSetVersion ?? "",
      capabilities: data.capabilities ?? { canRead: true, canManage: false },
    };
  });
}

export async function listCommercialQuotationRiskProfiles(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialQuotationRiskProfile[];
  ruleSetVersion: string;
  provenanceDisclaimer: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/quotation-risks/profiles", undefined, (payload) => {
    const data = payload as {
      items: RawCommercialQuotationRiskProfile[];
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

export async function listCommercialQuotationRiskAssessments(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    materialId?: string;
    outcome?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialQuotationRiskAssessmentSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/quotation-risks/assessments", {
    materialId: filters.materialId,
    outcome: filters.outcome,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialQuotationRiskAssessmentSummary[];
      page: number;
      pageSize: number;
      ruleSetVersion: string;
    };
    return {
      items: data.items ?? [],
      page: data.page ?? 1,
      pageSize: data.pageSize ?? 25,
      ruleSetVersion: data.ruleSetVersion ?? "",
    };
  });
}

export async function getCommercialQuotationRiskAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialQuotationRiskAssessmentDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/quotation-risks/assessments/${assessmentId}`,
    undefined,
    (payload) => payload as RawCommercialQuotationRiskAssessmentDetail,
  );
}

export async function listCommercialQuotationRiskComparisons(
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
  items: readonly RawCommercialQuotationRiskComparisonSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
  realQuotationMappingComplete: boolean;
}>>> {
  return getJson(context, "/api/commercial-intelligence/quotation-risks/comparisons", {
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
      items: RawCommercialQuotationRiskComparisonSummary[];
      page: number;
      pageSize: number;
      ruleSetVersion: string;
      realQuotationMappingComplete: boolean;
    };
    return {
      items: data.items ?? [],
      page: data.page ?? 1,
      pageSize: data.pageSize ?? 25,
      ruleSetVersion: data.ruleSetVersion ?? "",
      realQuotationMappingComplete: data.realQuotationMappingComplete === true,
    };
  });
}

export async function getCommercialQuotationRiskComparison(
  context: ProductionOrderRequestContext,
  comparisonId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialQuotationRiskComparisonDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/quotation-risks/comparisons/${comparisonId}`,
    undefined,
    (payload) => payload as RawCommercialQuotationRiskComparisonDetail,
  );
}

export async function updateCommercialQuotationRiskProfile(
  context: ProductionOrderRequestContext,
  profileId: string,
  input: Readonly<{
    expectedVersion: number;
    name: string;
    status: string;
    referenceWindowDurationDays: number;
    minimumComparableObservations: number;
    maximumNegativePriceVarianceRatio: number;
    maximumPositivePriceVarianceRatio: number;
    maximumPaymentTermDays: number;
    expiryWarningDays: number;
    maximumEvidenceAgeDays: number;
    allowedEvidenceStatuses: string;
    allowedSalesEvidenceKinds: string;
    requiredTaxInclusion: string;
    requiredDiscountInclusion: string;
    requiredFreightInclusion: string;
    requiredGrossNetBasis: string;
    sourceProvenance: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  entityId: string;
  version: number;
  idempotentReplay: boolean;
}>>> {
  return writeJson(
    context,
    "PUT",
    `/api/commercial-intelligence/quotation-risks/profiles/${profileId}`,
    {
      expectedVersion: input.expectedVersion,
      name: input.name,
      status: input.status,
      referenceWindowDurationDays: input.referenceWindowDurationDays,
      minimumComparableObservations: input.minimumComparableObservations,
      maximumNegativePriceVarianceRatio: input.maximumNegativePriceVarianceRatio,
      maximumPositivePriceVarianceRatio: input.maximumPositivePriceVarianceRatio,
      maximumPaymentTermDays: input.maximumPaymentTermDays,
      expiryWarningDays: input.expiryWarningDays,
      maximumEvidenceAgeDays: input.maximumEvidenceAgeDays,
      allowedEvidenceStatuses: input.allowedEvidenceStatuses,
      allowedSalesEvidenceKinds: input.allowedSalesEvidenceKinds,
      requiredTaxInclusion: input.requiredTaxInclusion,
      requiredDiscountInclusion: input.requiredDiscountInclusion,
      requiredFreightInclusion: input.requiredFreightInclusion,
      requiredGrossNetBasis: input.requiredGrossNetBasis,
      sourceProvenance: input.sourceProvenance,
    },
    (payload) => {
      const data = payload as { id?: string; entityId?: string; version?: number; idempotentReplay?: boolean };
      return {
        entityId: String(data.id ?? data.entityId ?? profileId),
        version: Number(data.version ?? input.expectedVersion),
        idempotentReplay: data.idempotentReplay === true,
      };
    },
  );
}

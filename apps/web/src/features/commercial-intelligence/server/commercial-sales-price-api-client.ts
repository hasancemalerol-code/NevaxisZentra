import { stripFixtureRecords } from "@/lib/runtime/fixture-data";
import { refusalCode } from "@/lib/api/api-failure-classifier";
import "./commercial-intelligence-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  CommercialIntelligenceApiFailure,
  CommercialIntelligenceApiResult,
} from "./commercial-intelligence-api-client";

export type RawCommercialSalesPriceWorkspace = Readonly<{
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  referenceCenter: string;
  defaultMinimumComparableObservations: number;
  realTransactionMappingComplete: boolean;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawCommercialSalesPriceProfile = Readonly<{
  id: string;
  profileCode: string;
  displayName: string;
  status: string;
  comparisonDimension: string;
  referenceWindowDurationDays: number;
  minimumComparableObservations: number;
  maximumPositiveVarianceRatio: number;
  customerSpreadThreshold: number | null;
  allowedEvidenceStatuses: string;
  allowedSourceEvidenceKinds: string;
  requiredTaxInclusion: string;
  requiredDiscountInclusion: string;
  requiredFreightInclusion: string;
  requiredGrossNetBasis: string;
  referenceCenter: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawCommercialSalesPriceAssessmentSummary = Readonly<{
  id: string;
  assessmentReference: string;
  profileId: string;
  profileCode: string;
  materialId: string;
  customerReference: string | null;
  sourceEvidenceKind: string;
  comparisonDimension: string;
  assessmentOutcome: string;
  currentUnitPrice: number;
  referenceMedianUnitPrice: number | null;
  varianceRatio: number | null;
  candidateObservationCount: number;
  comparableObservationCount: number;
  excludedObservationCount: number;
  currencyCode: string;
  normalizedUnit: string;
  windowStartUtc: string;
  windowEndUtc: string;
  leadingDriverCode: string | null;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  createdAtUtc: string;
  isPilotData: boolean;
}>;

export type RawCommercialSalesPriceAssessmentDetail = RawCommercialSalesPriceAssessmentSummary & Readonly<{
  absoluteVariance: number | null;
  priceBasisGrossNet: string;
  priceBasisTaxInclusion: string;
  priceBasisDiscountInclusion: string;
  priceBasisFreightInclusion: string;
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

export type RawCommercialSalesPriceComparisonSummary = Readonly<{
  id: string;
  comparisonReference: string;
  profileId: string;
  profileCode: string;
  comparisonType: string;
  materialId: string;
  customerReference: string | null;
  comparisonOutcome: string;
  currentMedianUnitPrice: number | null;
  referenceMedianUnitPrice: number | null;
  signedVarianceRatio: number | null;
  customerSpreadRatio: number | null;
  candidateObservationCount: number;
  comparableObservationCount: number;
  excludedObservationCount: number;
  currencyCode: string;
  normalizedUnit: string;
  currentWindowStartUtc: string;
  currentWindowEndUtc: string;
  referenceWindowStartUtc: string | null;
  referenceWindowEndUtc: string | null;
  leadingDriverCode: string | null;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  createdAtUtc: string;
  isPilotData: boolean;
}>;

export type RawCommercialSalesPriceComparisonDetail = RawCommercialSalesPriceComparisonSummary & Readonly<{
  profileVersion: string;
  sourceEvidenceKind: string;
  priceBasisGrossNet: string;
  priceBasisTaxInclusion: string;
  priceBasisDiscountInclusion: string;
  priceBasisFreightInclusion: string;
  evidenceStatus: string;
  sourceProvenance: string;
  customerResults: ReadonlyArray<Readonly<{
    id: string;
    customerReference: string | null;
    comparableObservationCount: number;
    customerMedianUnitPrice: number | null;
    lowestComparableMedianUnitPrice: number | null;
    relativePricePositionRatio: number | null;
    priceRank: number;
    isTied: boolean;
    positionState: string;
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
    affectedObservationCount: number;
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
    return reason || detail || title || `Satış fiyat isteği başarısız (${status}).`;
  }
  return `Satış fiyat isteği başarısız (${status}).`;
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

    const guarded = stripFixtureRecords(parser(payload));

    if (guarded === null) {

      // A fixture record is not a customer record outside isolated environments.

      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;

    }

    return { status: "success", value: guarded };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Satış fiyat API'sine ulaşılamadı.",
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

    const guarded = stripFixtureRecords(parser(payload));

    if (guarded === null) {

      // A fixture record is not a customer record outside isolated environments.

      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;

    }

    return { status: "success", value: guarded };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Satış fiyat yazma isteği başarısız oldu.",
      retryable: true,
    };
  }
}

export async function getCommercialSalesPriceWorkspace(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<RawCommercialSalesPriceWorkspace>> {
  return getJson(context, "/api/commercial-intelligence/sales-prices/workspace", undefined, (payload) => {
    const data = payload as RawCommercialSalesPriceWorkspace & {
      capabilities?: { canRead: boolean; canManage: boolean };
    };
    return {
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
      referenceCenter: data.referenceCenter ?? "",
      defaultMinimumComparableObservations: data.defaultMinimumComparableObservations ?? 0,
      realTransactionMappingComplete: data.realTransactionMappingComplete === true,
      capabilities: data.capabilities ?? { canRead: true, canManage: false },
    };
  });
}

export async function listCommercialSalesPriceProfiles(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialSalesPriceProfile[];
  ruleSetVersion: string;
  provenanceDisclaimer: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/sales-prices/profiles", undefined, (payload) => {
    const data = payload as {
      items: RawCommercialSalesPriceProfile[];
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

export async function listCommercialSalesPriceAssessments(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    materialId?: string;
    assessmentOutcome?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialSalesPriceAssessmentSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/sales-prices/assessments", {
    materialId: filters.materialId,
    assessmentOutcome: filters.assessmentOutcome,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialSalesPriceAssessmentSummary[];
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

export async function getCommercialSalesPriceAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialSalesPriceAssessmentDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/sales-prices/assessments/${assessmentId}`,
    undefined,
    (payload) => payload as RawCommercialSalesPriceAssessmentDetail,
  );
}

export async function evaluateCommercialSalesPriceAssessment(
  context: ProductionOrderRequestContext,
  input: Readonly<{
    profileId: string;
    materialId: string;
    customerReference?: string | null;
    sourceEvidenceKind: string;
    comparisonDimension: string;
    currentUnitPrice: number;
    currencyCode: string;
    normalizedUnit: string;
    windowStartUtc: string;
    windowEndUtc: string;
    priceBasisGrossNet: string;
    priceBasisTaxInclusion: string;
    priceBasisDiscountInclusion: string;
    priceBasisFreightInclusion: string;
    sourceProvenance: string;
    idempotencyKey: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{ entityId: string; idempotentReplay: boolean }>>> {
  return writeJson(context, "POST", "/api/commercial-intelligence/sales-prices/assessments/evaluate", input, (payload) => {
    const data = payload as { id: string; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

export async function listCommercialSalesPriceComparisons(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    comparisonType?: string;
    materialId?: string;
    customerReference?: string;
    comparisonOutcome?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialSalesPriceComparisonSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/sales-prices/comparisons", {
    comparisonType: filters.comparisonType,
    materialId: filters.materialId,
    customerReference: filters.customerReference,
    comparisonOutcome: filters.comparisonOutcome,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialSalesPriceComparisonSummary[];
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

export async function getCommercialSalesPriceComparison(
  context: ProductionOrderRequestContext,
  comparisonId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialSalesPriceComparisonDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/sales-prices/comparisons/${comparisonId}`,
    undefined,
    (payload) => payload as RawCommercialSalesPriceComparisonDetail,
  );
}

export async function evaluateCommercialSalesPriceComparison(
  context: ProductionOrderRequestContext,
  input: Readonly<{
    profileId: string;
    expectedProfileVersion: number;
    comparisonType: string;
    materialId: string;
    customerReference?: string | null;
    sourceEvidenceKind: string;
    currentWindowStartUtc: string;
    currentWindowEndUtc: string;
    referenceWindowStartUtc?: string | null;
    referenceWindowEndUtc?: string | null;
    currencyCode: string;
    normalizedUnit: string;
    priceBasisGrossNet: string;
    priceBasisTaxInclusion: string;
    priceBasisDiscountInclusion: string;
    priceBasisFreightInclusion: string;
    sourceProvenance: string;
    idempotencyKey: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{ entityId: string; idempotentReplay: boolean }>>> {
  return writeJson(context, "POST", "/api/commercial-intelligence/sales-prices/comparisons/evaluate", input, (payload) => {
    const data = payload as { id: string; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

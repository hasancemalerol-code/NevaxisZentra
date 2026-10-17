import { stripFixtureRecords } from "@/lib/runtime/fixture-data";
import { refusalCode } from "@/lib/api/api-failure-classifier";
import "./commercial-intelligence-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type CommercialIntelligenceApiFailure = Readonly<{
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
    | "idempotency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type CommercialIntelligenceApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | CommercialIntelligenceApiFailure;

export type RawCommercialIntelligenceWorkspace = Readonly<{
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  referenceCenter: string;
  defaultMinimumComparableObservations: number;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawCommercialIntelligenceProfile = Readonly<{
  id: string;
  profileCode: string;
  displayName: string;
  status: string;
  comparisonDimension: string;
  referenceWindowDurationDays: number;
  minimumComparableObservations: number;
  maximumPositiveVarianceRatio: number;
  supplierSpreadThreshold: number | null;
  allowedEvidenceStatuses: string;
  requiredTaxInclusion: string;
  requiredDiscountInclusion: string;
  requiredFreightInclusion: string;
  referenceCenter: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type RawCommercialIntelligenceAssessmentSummary = Readonly<{
  id: string;
  assessmentReference: string;
  profileId: string;
  profileCode: string;
  materialId: string;
  supplierReference: string | null;
  comparisonDimension: string;
  assessmentOutcome: string;
  currentUnitPrice: number;
  referenceMedianUnitPrice: number | null;
  varianceRatio: number | null;
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

export type RawCommercialIntelligenceAssessmentDetail = RawCommercialIntelligenceAssessmentSummary & Readonly<{
  absoluteVariance: number | null;
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

export type RawCommercialIntelligenceComparisonSummary = Readonly<{
  id: string;
  comparisonReference: string;
  profileId: string;
  profileCode: string;
  comparisonType: string;
  materialId: string;
  comparisonOutcome: string;
  currentMedianUnitPrice: number | null;
  referenceMedianUnitPrice: number | null;
  signedVarianceRatio: number | null;
  supplierSpreadRatio: number | null;
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

export type RawCommercialIntelligenceComparisonDetail = RawCommercialIntelligenceComparisonSummary & Readonly<{
  profileVersion: string;
  priceBasisTaxInclusion: string;
  priceBasisDiscountInclusion: string;
  priceBasisFreightInclusion: string;
  evidenceStatus: string;
  sourceProvenance: string;
  supplierResults: ReadonlyArray<Readonly<{
    id: string;
    supplierReference: string | null;
    comparableObservationCount: number;
    supplierMedianUnitPrice: number | null;
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
  unavailableDimensions: readonly string[];
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
    return reason || detail || title || `Ticari zekâ isteği başarısız (${status}).`;
  }
  return `Ticari zekâ isteği başarısız (${status}).`;
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
      message: "Ticari zekâ API'sine ulaşılamadı.",
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
      message: "Ticari zekâ yazma isteği başarısız oldu.",
      retryable: true,
    };
  }
}

export async function getCommercialIntelligenceWorkspace(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<RawCommercialIntelligenceWorkspace>> {
  return getJson(context, "/api/commercial-intelligence/purchase-prices/workspace", undefined, (payload) => {
    const data = payload as RawCommercialIntelligenceWorkspace & {
      capabilities?: { canRead: boolean; canManage: boolean };
    };
    return {
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
      referenceCenter: data.referenceCenter ?? "",
      defaultMinimumComparableObservations: data.defaultMinimumComparableObservations ?? 0,
      capabilities: data.capabilities ?? { canRead: true, canManage: false },
    };
  });
}

export async function listCommercialIntelligenceProfiles(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialIntelligenceProfile[];
  ruleSetVersion: string;
  provenanceDisclaimer: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/purchase-prices/profiles", undefined, (payload) => {
    const data = payload as {
      items: RawCommercialIntelligenceProfile[];
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

export async function updateCommercialIntelligenceProfile(
  context: ProductionOrderRequestContext,
  profileId: string,
  input: Readonly<{
    expectedVersion: number;
    displayName: string;
    status: string;
    comparisonDimension: string;
    referenceWindowDurationDays: number;
    minimumComparableObservations: number;
    maximumPositiveVarianceRatio: number;
    supplierSpreadThreshold: number | null;
    allowedEvidenceStatuses: string;
    requiredTaxInclusion: string;
    requiredDiscountInclusion: string;
    requiredFreightInclusion: string;
    sourceProvenance: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{ entityId: string; version: number; idempotentReplay: boolean }>>> {
  return writeJson(context, "PUT", `/api/commercial-intelligence/purchase-prices/profiles/${profileId}`, input, (payload) => {
    const data = payload as { id: string; version: number; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      version: data.version,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

export async function listCommercialIntelligenceAssessments(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    materialId?: string;
    assessmentOutcome?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialIntelligenceAssessmentSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/purchase-prices/assessments", {
    materialId: filters.materialId,
    assessmentOutcome: filters.assessmentOutcome,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialIntelligenceAssessmentSummary[];
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

export async function getCommercialIntelligenceAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialIntelligenceAssessmentDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/purchase-prices/assessments/${assessmentId}`,
    undefined,
    (payload) => payload as RawCommercialIntelligenceAssessmentDetail,
  );
}

export async function evaluateCommercialIntelligenceAssessment(
  context: ProductionOrderRequestContext,
  input: Readonly<{
    profileId: string;
    materialId: string;
    supplierReference?: string | null;
    comparisonDimension: string;
    currentUnitPrice: number;
    currencyCode: string;
    normalizedUnit: string;
    windowStartUtc: string;
    windowEndUtc: string;
    priceBasisTaxInclusion: string;
    priceBasisDiscountInclusion: string;
    priceBasisFreightInclusion: string;
    sourceProvenance: string;
    idempotencyKey: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{ entityId: string; idempotentReplay: boolean }>>> {
  return writeJson(context, "POST", "/api/commercial-intelligence/purchase-prices/assessments/evaluate", input, (payload) => {
    const data = payload as { id: string; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

export async function listCommercialIntelligenceComparisons(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    comparisonType?: string;
    materialId?: string;
    supplierReference?: string;
    outcome?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialIntelligenceComparisonSummary[];
  page: number;
  pageSize: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  unavailableDimensions: readonly string[];
}>>> {
  return getJson(context, "/api/commercial-intelligence/purchase-prices/comparisons", {
    comparisonType: filters.comparisonType,
    materialId: filters.materialId,
    supplierReference: filters.supplierReference,
    outcome: filters.outcome,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialIntelligenceComparisonSummary[];
      page: number;
      pageSize: number;
      ruleSetVersion: string;
      provenanceDisclaimer: string;
      unavailableDimensions: string[];
    };
    return {
      items: data.items ?? [],
      page: data.page ?? 1,
      pageSize: data.pageSize ?? 25,
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
      unavailableDimensions: data.unavailableDimensions ?? [],
    };
  });
}

export async function getCommercialIntelligenceComparison(
  context: ProductionOrderRequestContext,
  comparisonId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialIntelligenceComparisonDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/purchase-prices/comparisons/${comparisonId}`,
    undefined,
    (payload) => payload as RawCommercialIntelligenceComparisonDetail,
  );
}

export async function evaluateCommercialIntelligenceComparison(
  context: ProductionOrderRequestContext,
  input: Readonly<{
    profileId: string;
    expectedProfileVersion: number;
    comparisonType: string;
    materialId: string;
    currentWindowStartUtc: string;
    currentWindowEndUtc: string;
    referenceWindowStartUtc?: string | null;
    referenceWindowEndUtc?: string | null;
    currencyCode: string;
    normalizedUnit: string;
    priceBasisTaxInclusion: string;
    priceBasisDiscountInclusion: string;
    priceBasisFreightInclusion: string;
    sourceProvenance: string;
    idempotencyKey: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{ entityId: string; idempotentReplay: boolean }>>> {
  return writeJson(context, "POST", "/api/commercial-intelligence/purchase-prices/comparisons/evaluate", input, (payload) => {
    const data = payload as { id: string; idempotentReplay?: boolean };
    return {
      entityId: data.id,
      idempotentReplay: data.idempotentReplay === true,
    };
  });
}

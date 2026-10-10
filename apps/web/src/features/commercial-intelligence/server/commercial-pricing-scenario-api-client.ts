import { refusalCode } from "@/lib/api/api-failure-classifier";
import "./commercial-intelligence-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  CommercialIntelligenceApiFailure,
  CommercialIntelligenceApiResult,
} from "./commercial-intelligence-api-client";

export type RawCommercialPricingScenarioWorkspace = Readonly<{
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  kpis: Readonly<{
    draftCount: number;
    needsReviewCount: number;
    eligibleCount: number;
    blockedCount: number;
    approvedCount: number;
  }>;
  capabilities: Readonly<{
    canRead: boolean;
    canManage: boolean;
    canReview: boolean;
    canCreateTask: boolean;
  }>;
  automationDisclaimer: string;
}>;

export type RawCommercialPricingScenarioSummary = Readonly<{
  id: string;
  scenarioReference: string;
  lifecycleStatus: string;
  guardrailResult: string | null;
  profileId: string;
  materialId: string | null;
  customerReference: string | null;
  baselineUnitPrice: number;
  proposedUnitPrice: number;
  proposedDiscountRatio: number | null;
  validityWindowStartUtc: string | null;
  validityWindowEndUtc: string | null;
  linkedAiTaskId: string | null;
  evaluationCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}>;

export type RawCommercialPricingScenarioDriver = Readonly<{
  reasonCode: string;
  severity: string;
  actualValue: number | null;
  thresholdValue: number | null;
  unit: string | null;
  normalizedVariance: number | null;
  affectedComponentCount: number | null;
  supportingText: string;
}>;

export type RawCommercialPricingScenarioEvaluationSnapshot = Readonly<{
  id: string;
  scenarioVersionAtEvaluation: number;
  evaluationSequence: number;
  guardrailResult: string;
  assessmentOutcome: string;
  evidenceStatus: string;
  costCoverageStatus: string;
  leadingDriverCode: string | null;
  insufficientDataReason: string | null;
  baselineUnitPrice: number;
  proposedUnitPrice: number;
  totalIncludedUnitCost: number | null;
  expectedUnitContribution: number | null;
  expectedContributionMarginRatio: number | null;
  baselineContributionMarginRatio: number | null;
  contributionMarginDelta: number | null;
  profileMinimumContributionMarginRatio: number | null;
  profileId: string;
  profileVersion: string;
  inputSnapshotJson: string;
  outputSnapshotJson: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  evaluatedBy: string;
  drivers: readonly RawCommercialPricingScenarioDriver[];
}>;

export type RawCommercialPricingScenarioAuditEvent = Readonly<{
  id: string;
  eventType: string;
  payloadJson: string;
  actorSubjectId: string;
  occurredAtUtc: string;
}>;

export type RawCommercialPricingScenarioDetail = RawCommercialPricingScenarioSummary & Readonly<{
  profileVersion: string;
  salesEvidenceObservationId: string;
  quantity: number | null;
  commercialRationale: string;
  latestEvaluationSnapshotId: string | null;
  ruleSetVersion: string;
  sourceProvenance: string;
  latestEvaluation: RawCommercialPricingScenarioEvaluationSnapshot | null;
  evaluationHistory: readonly RawCommercialPricingScenarioEvaluationSnapshot[];
  auditEvents: readonly RawCommercialPricingScenarioAuditEvent[];
  deepLink: string;
  automationDisclaimer: string;
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
    return reason || detail || title || `Fiyat senaryosu isteği başarısız (${status}).`;
  }
  return `Fiyat senaryosu isteği başarısız (${status}).`;
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
      message: "Fiyat senaryosu API'sine ulaşılamadı.",
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
      message: "Fiyat senaryosu yazma isteği başarısız oldu.",
      retryable: true,
    };
  }
}

export async function getCommercialPricingScenarioWorkspace(
  context: ProductionOrderRequestContext,
): Promise<CommercialIntelligenceApiResult<RawCommercialPricingScenarioWorkspace>> {
  return getJson(context, "/api/commercial-intelligence/pricing-scenarios/workspace", undefined, (payload) => {
    const data = payload as RawCommercialPricingScenarioWorkspace;
    return {
      ruleSetVersion: data.ruleSetVersion ?? "",
      provenanceDisclaimer: data.provenanceDisclaimer ?? "",
      kpis: {
        draftCount: data.kpis?.draftCount ?? 0,
        needsReviewCount: data.kpis?.needsReviewCount ?? 0,
        eligibleCount: data.kpis?.eligibleCount ?? 0,
        blockedCount: data.kpis?.blockedCount ?? 0,
        approvedCount: data.kpis?.approvedCount ?? 0,
      },
      capabilities: {
        canRead: data.capabilities?.canRead !== false,
        canManage: data.capabilities?.canManage === true,
        canReview: data.capabilities?.canReview === true,
        canCreateTask: data.capabilities?.canCreateTask === true,
      },
      automationDisclaimer: data.automationDisclaimer ?? "Bu onay fiyatı otomatik olarak uygulamaz.",
    };
  });
}

export async function listCommercialPricingScenarios(
  context: ProductionOrderRequestContext,
  filters: Readonly<{
    lifecycleStatus?: string;
    guardrailResult?: string;
    profileId?: string;
    materialId?: string;
    customerReference?: string;
    page?: number;
    pageSize?: number;
  }> = {},
): Promise<CommercialIntelligenceApiResult<Readonly<{
  items: readonly RawCommercialPricingScenarioSummary[];
  ruleSetVersion: string;
  automationDisclaimer: string;
}>>> {
  return getJson(context, "/api/commercial-intelligence/pricing-scenarios", {
    lifecycleStatus: filters.lifecycleStatus,
    guardrailResult: filters.guardrailResult,
    profileId: filters.profileId,
    materialId: filters.materialId,
    customerReference: filters.customerReference,
    page: filters.page ? String(filters.page) : undefined,
    pageSize: filters.pageSize ? String(filters.pageSize) : undefined,
  }, (payload) => {
    const data = payload as {
      items: RawCommercialPricingScenarioSummary[];
      ruleSetVersion: string;
      automationDisclaimer: string;
    };
    return {
      items: data.items ?? [],
      ruleSetVersion: data.ruleSetVersion ?? "",
      automationDisclaimer: data.automationDisclaimer ?? "Bu onay fiyatı otomatik olarak uygulamaz.",
    };
  });
}

export async function getCommercialPricingScenario(
  context: ProductionOrderRequestContext,
  scenarioId: string,
): Promise<CommercialIntelligenceApiResult<RawCommercialPricingScenarioDetail>> {
  return getJson(
    context,
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}`,
    undefined,
    (payload) => payload as RawCommercialPricingScenarioDetail,
  );
}

export async function createCommercialPricingScenario(
  context: ProductionOrderRequestContext,
  input: Readonly<{
    profileId: string;
    expectedProfileVersion: number;
    salesEvidenceObservationId: string;
    proposedUnitPrice: number;
    proposedDiscountRatio?: number | null;
    quantity?: number | null;
    validityWindowStartUtc?: string | null;
    validityWindowEndUtc?: string | null;
    commercialRationale: string;
    sourceProvenance: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  entityId: string | null;
  version: number;
  idempotentReplay: boolean;
  scenario: RawCommercialPricingScenarioDetail | null;
}>>> {
  return writeJson(
    context,
    "POST",
    "/api/commercial-intelligence/pricing-scenarios",
    {
      profileId: input.profileId,
      expectedProfileVersion: input.expectedProfileVersion,
      salesEvidenceObservationId: input.salesEvidenceObservationId,
      proposedUnitPrice: input.proposedUnitPrice,
      proposedDiscountRatio: input.proposedDiscountRatio ?? null,
      quantity: input.quantity ?? null,
      validityWindowStartUtc: input.validityWindowStartUtc ?? null,
      validityWindowEndUtc: input.validityWindowEndUtc ?? null,
      commercialRationale: input.commercialRationale,
      sourceProvenance: input.sourceProvenance,
    },
    (payload) => {
      const data = payload as {
        entityId?: string | null;
        version?: number;
        idempotentReplay?: boolean;
        scenario?: RawCommercialPricingScenarioDetail | null;
      };
      return {
        entityId: data.entityId ?? null,
        version: data.version ?? 0,
        idempotentReplay: data.idempotentReplay === true,
        scenario: data.scenario ?? null,
      };
    },
  );
}

export async function updateCommercialPricingScenario(
  context: ProductionOrderRequestContext,
  scenarioId: string,
  input: Readonly<{
    expectedVersion: number;
    profileId: string;
    expectedProfileVersion: number;
    salesEvidenceObservationId: string;
    proposedUnitPrice: number;
    proposedDiscountRatio?: number | null;
    quantity?: number | null;
    validityWindowStartUtc?: string | null;
    validityWindowEndUtc?: string | null;
    commercialRationale: string;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  entityId: string;
  version: number;
  idempotentReplay: boolean;
  scenario: RawCommercialPricingScenarioDetail | null;
}>>> {
  return writeJson(
    context,
    "PUT",
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}`,
    {
      expectedVersion: input.expectedVersion,
      profileId: input.profileId,
      expectedProfileVersion: input.expectedProfileVersion,
      salesEvidenceObservationId: input.salesEvidenceObservationId,
      proposedUnitPrice: input.proposedUnitPrice,
      proposedDiscountRatio: input.proposedDiscountRatio ?? null,
      quantity: input.quantity ?? null,
      validityWindowStartUtc: input.validityWindowStartUtc ?? null,
      validityWindowEndUtc: input.validityWindowEndUtc ?? null,
      commercialRationale: input.commercialRationale,
    },
    (payload) => {
      const data = payload as {
        entityId: string;
        version: number;
        idempotentReplay?: boolean;
        scenario?: RawCommercialPricingScenarioDetail | null;
      };
      return {
        entityId: data.entityId,
        version: data.version,
        idempotentReplay: data.idempotentReplay === true,
        scenario: data.scenario ?? null,
      };
    },
  );
}

export async function evaluateCommercialPricingScenario(
  context: ProductionOrderRequestContext,
  scenarioId: string,
  input: Readonly<{ expectedVersion: number }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  entityId: string;
  version: number;
  idempotentReplay: boolean;
  scenario: RawCommercialPricingScenarioDetail | null;
}>>> {
  return writeJson(
    context,
    "POST",
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/evaluate`,
    { expectedVersion: input.expectedVersion },
    (payload) => {
      const data = payload as {
        entityId: string;
        version: number;
        idempotentReplay?: boolean;
        scenario?: RawCommercialPricingScenarioDetail | null;
      };
      return {
        entityId: data.entityId,
        version: data.version,
        idempotentReplay: data.idempotentReplay === true,
        scenario: data.scenario ?? null,
      };
    },
  );
}

export async function transitionCommercialPricingScenario(
  context: ProductionOrderRequestContext,
  scenarioId: string,
  input: Readonly<{
    expectedVersion: number;
    targetLifecycleStatus: string;
    note?: string | null;
  }>,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  entityId: string;
  version: number;
  idempotentReplay: boolean;
  scenario: RawCommercialPricingScenarioDetail | null;
}>>> {
  return writeJson(
    context,
    "POST",
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/transitions`,
    {
      expectedVersion: input.expectedVersion,
      targetLifecycleStatus: input.targetLifecycleStatus,
      note: input.note ?? null,
    },
    (payload) => {
      const data = payload as {
        entityId: string;
        version: number;
        idempotentReplay?: boolean;
        scenario?: RawCommercialPricingScenarioDetail | null;
      };
      return {
        entityId: data.entityId,
        version: data.version,
        idempotentReplay: data.idempotentReplay === true,
        scenario: data.scenario ?? null,
      };
    },
  );
}

export async function createCommercialPricingScenarioTaskDraft(
  context: ProductionOrderRequestContext,
  scenarioId: string,
): Promise<CommercialIntelligenceApiResult<Readonly<{
  taskId: string;
  scenarioId: string;
  idempotentReplay: boolean;
  scenario: RawCommercialPricingScenarioDetail;
}>>> {
  return writeJson(
    context,
    "POST",
    `/api/commercial-intelligence/pricing-scenarios/${scenarioId}/task-draft`,
    {},
    (payload) => {
      const data = payload as {
        taskId: string;
        scenarioId: string;
        idempotentReplay?: boolean;
        scenario: RawCommercialPricingScenarioDetail;
      };
      return {
        taskId: data.taskId,
        scenarioId: data.scenarioId,
        idempotentReplay: data.idempotentReplay === true,
        scenario: data.scenario,
      };
    },
  );
}

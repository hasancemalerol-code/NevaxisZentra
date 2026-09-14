import "./golden-demo-p1-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type GoldenDemoP1WorkspaceApi = Readonly<{
  event: Readonly<{
    id: string;
    machineAssetId: string;
    machineCode: string;
    title: string;
    status: string;
    moldReference?: string;
    productReference?: string;
    shiftTargetGoodParts: number;
    shiftStartUtc: string;
    shiftEndUtc: string;
  }>;
  baseline: Readonly<{
    referenceSource: string;
    referenceSourceLabel: string;
    totalCycleSeconds: number;
    referenceKwhPerGoodPart?: number;
  }> | null;
  cycleLoss: Readonly<{
    hasBaseline: boolean;
    baselineFailureReason?: string;
    referenceCycleSeconds: number;
    currentCycleSeconds: number;
    cycleLossSeconds: number;
    microStopEquivalentLossSeconds: number;
    phases: readonly Readonly<{
      phaseCode: string;
      label: string;
      referenceSeconds: number;
      currentSeconds: number;
      lossSeconds: number;
      isLoss: boolean;
      isImprovement: boolean;
    }>[];
    losses: readonly Readonly<{ phaseCode: string; label: string; lossSeconds: number }>[];
    improvements: readonly Readonly<{ phaseCode: string; label: string; lossSeconds: number }>[];
  }>;
  contributors: readonly Readonly<{
    rank: number;
    phaseCode: string;
    label: string;
    contributorKind: string;
    measuredDeviationSeconds: number;
    contributionToCycleLossSeconds: number;
    confidence: string;
    suggestedNextStep: string;
    evidenceWindow: string;
  }>[];
  recovery: Readonly<{
    currentExpectedShiftGoodParts: number;
    recoveredExpectedShiftGoodParts: number;
    productionRecoveryGoodParts: number;
    productionRecoveryPercent: number;
    recoverableCycleSeconds: number;
    remainingShiftMinutes: number;
    confidenceLevel: string;
    assumptions: readonly string[];
    safetyMessage: string;
  }>;
  whatIfScenarios: readonly Readonly<{
    scenarioCode: string;
    title: string;
    projectedEndOfShiftGoodParts: number;
    productionGapGoodParts: number;
    continuedCycleLossSeconds: number;
    interventionDurationMinutes?: number;
    recoveredProductionOpportunity?: number;
    riskExposure: string;
    isRecommended: boolean;
    recommendationReason: string;
  }>[];
  plan: Readonly<{
    id: string;
    status: string;
    recommendationSummary: string;
    recommendedScenarioCode: string;
    requiresApproval: boolean;
    approvedBySubjectId?: string;
    approvedAtUtc?: string;
  }> | null;
  planActions: readonly Readonly<{
    id: string;
    sortOrder: number;
    actionCode: string;
    title: string;
    responsibleRole: string;
    priority: string;
    status: string;
    evidenceRequired: string;
    estimatedDowntimeMinutes: number;
    requiresApproval: boolean;
    validationCriteria: string;
    completedAtUtc?: string;
  }>[];
  agentFindings: readonly Readonly<{
    agentCode: string;
    workerCode: string;
    summary: string;
    evidenceClassification: string;
    generatedAtUtc: string;
  }>[];
  verification: Readonly<{
    result: string;
    summary: string;
    recoveredGoodPartsPerShift: number;
    verifiedAtUtc: string;
  }> | null;
  beforeAfter: Readonly<{
    beforeCycleSeconds: number;
    afterCycleSeconds: number;
    beforeGoodPartsPerHour: number;
    afterGoodPartsPerHour: number;
    beforeMicroStopCount: number;
    afterMicroStopCount: number;
    beforeRejectRatePercent: number;
    afterRejectRatePercent: number;
    beforeKwhPerGoodPart?: number;
    afterKwhPerGoodPart?: number;
    recoveredGoodPartsPerShift: number;
    outcomeLabel: string;
  }> | null;
  metrics: Readonly<Record<string, unknown>>;
  businessImpact: Readonly<{
    operationalGoodPartsPerShift: number;
    capacityImpactPercent: number;
    energyIntensityImpactPercent?: number;
    downtimeMinutesAvoided?: number;
    verifiedBusinessImpactTry?: number;
    estimatedBusinessImpactTry?: number;
  }>;
}>;

type ApiResult<T> =
  | { status: "success"; value: T }
  | { status: "failure"; code: string; message: string };

async function goldenDemoP1Fetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<ApiResult<T>> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...context.requestHeaders,
      ...init?.headers,
    },
    cache: "no-store",
  });

  const text = await response.text();
  let json: unknown;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    return { status: "failure", code: "invalid-json", message: "API yanıtı okunamadı." };
  }

  if (!response.ok) {
    const message =
      typeof json === "object" && json !== null && "message" in json
        ? String((json as { message: string }).message)
        : `HTTP ${response.status}`;
    return { status: "failure", code: `http-${response.status}`, message };
  }

  return { status: "success", value: json as T };
}

export async function getGoldenDemoP1Workspace(context: ProductionOrderRequestContext) {
  return goldenDemoP1Fetch<GoldenDemoP1WorkspaceApi>(context, "/api/golden-demo-p1/workspace");
}

export async function approveGoldenDemoPlan(
  context: ProductionOrderRequestContext,
  eventId: string,
  comment?: string,
) {
  return goldenDemoP1Fetch<{ id: string; status: string }>(
    context,
    `/api/golden-demo-p1/events/${eventId}/approve`,
    { method: "POST", body: JSON.stringify({ comment: comment ?? null }) },
  );
}

export async function executeGoldenDemoPlan(context: ProductionOrderRequestContext, eventId: string) {
  return goldenDemoP1Fetch<{ id: string; status: string }>(
    context,
    `/api/golden-demo-p1/events/${eventId}/execute`,
    { method: "POST", body: JSON.stringify({ idempotencyKey: crypto.randomUUID() }) },
  );
}

export async function completeGoldenDemoIntervention(
  context: ProductionOrderRequestContext,
  eventId: string,
) {
  return goldenDemoP1Fetch<{ id: string; status: string }>(
    context,
    `/api/golden-demo-p1/events/${eventId}/complete-intervention`,
    { method: "POST", body: JSON.stringify({ idempotencyKey: crypto.randomUUID() }) },
  );
}

export async function verifyGoldenDemoOutcome(context: ProductionOrderRequestContext, eventId: string) {
  return goldenDemoP1Fetch<{
    id: string;
    result: string;
    summary: string;
    recoveredGoodPartsPerShift: number;
  }>(context, `/api/golden-demo-p1/events/${eventId}/verify`, {
    method: "POST",
    body: JSON.stringify({ idempotencyKey: crypto.randomUUID() }),
  });
}

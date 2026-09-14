import "./golden-demo-p2b-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type GoldenDemoP2BAgentEvidenceApi = Readonly<{
  findingId: string;
  agentCode: string;
  workerCode: string;
  agentLabel: string;
  summary: string;
  evidenceClassification: string;
  generatedAtUtc: string;
  dataInputsReviewed: readonly string[];
  calculationInputs: Readonly<Record<string, string>>;
  calculationResults: Readonly<Record<string, string>>;
  conclusion: string;
  confidence: string;
  insufficientEvidenceState?: string | null;
  productionEventId: string;
  machineAssetId?: string | null;
  machineCode: string;
  resultingAction: string;
  planReferenceId?: string | null;
  workReferenceId?: string | null;
}>;

export type GoldenDemoP2BDigitalFactoryTeamApi = Readonly<{
  productionEventId: string;
  machineCode: string;
  agents: readonly GoldenDemoP2BAgentEvidenceApi[];
  orchestrator: Readonly<{
    whatIsHappening: string;
    whySummary: string;
    currentExpectedShiftGoodParts: number;
    recoveredExpectedShiftGoodParts: number;
    productionRecoveryGoodParts: number;
    whatShouldHappenNext: string;
    humanApprovalRequired: boolean;
    approvingRole: string;
    requiredControlReason: string;
    expectedOperationalOutcome: string;
    inputsUsed: readonly string[];
    insufficientSpecialistEvidence: readonly string[];
  }>;
  activityTimeline: readonly Readonly<{
    timestampUtc: string;
    actor: string;
    action: string;
    status: string;
    sourceReference: string;
    evidenceType: string;
    resultSummary?: string | null;
  }>[];
  approvalSheet: Readonly<{
    isApprovalRequired: boolean;
    isApproved: boolean;
    recommendedIntervention: string;
    whyRecommended: string;
    expectedInterruptionMinutes: number;
    currentProductionExposureGoodParts: number;
    currentExpectedShiftGoodParts: number;
    recoveredExpectedShiftGoodParts: number;
    recoveryOpportunityGoodParts: number;
    operationalRisk: string;
    approvingRole: string;
    evidenceReferences: readonly string[];
    requestedAtUtc: string;
    requestedByActor: string;
    approvedBySubjectId?: string | null;
    approvedAtUtc?: string | null;
    approvalStatus: string;
    auditReference: string;
  }>;
  planActions: readonly Readonly<{
    actionId: string;
    planId: string;
    sortOrder: number;
    title: string;
    responsibleRole: string;
    status: string;
    evidenceRequired: string;
    validationCriteria: string;
    sourceAgentCode: string;
    sourceAgentLabel: string;
    relatedTechnicalFinding: string;
    completedAtUtc?: string | null;
    workReferenceId: string;
  }>[];
  interventionTimeline: readonly Readonly<{
    stageCode: string;
    stageLabel: string;
    status: string;
    occurredAtUtc?: string | null;
    actor: string;
  }>[];
}>;

type FetchResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; code: string; message: string }>;

async function goldenDemoP2BFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
): Promise<FetchResult<T>> {
  const url = `${context.apiBaseUrl}${path}${path.includes("?") ? "&" : "?"}facilityId=${context.facilityId}`;
  const response = await fetch(url, {
    headers: {
      Accept: "application/json",
      ...context.requestHeaders,
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

export async function getGoldenDemoP2BDigitalFactoryTeam(context: ProductionOrderRequestContext) {
  return goldenDemoP2BFetch<GoldenDemoP2BDigitalFactoryTeamApi>(
    context,
    "/api/golden-demo-p2b/digital-factory-team",
  );
}

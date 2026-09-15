import "./factory-decision-center-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type FactoryDecisionCenterSummaryApi = Readonly<{
  criticalDecisions: number;
  outcomesAtRisk: number;
  pendingHumanApproval: number;
  recoveryOperations: number;
  pendingVerification: number;
}>;

export type FactoryDecisionCaseQueueItemApi = Readonly<{
  caseId: string;
  title: string;
  severity: string;
  status: string;
  orderReference: string;
  customerName: string;
  deliveryCommitmentLabel: string;
  riskSummary: string;
  requiredQuantity: number;
  projectedQuantity: number;
  gapQuantity: number;
  recommendedStrategy: string;
  digitalWorkerNames: readonly string[];
  decisionRequired?: string;
  currentBlockerCode?: string;
  verificationState: string;
  updatedAtUtc: string;
  concurrencyToken: number;
}>;

export type FactoryDecisionCenterLiveQueuesApi = Readonly<{
  criticalDecisions: readonly FactoryDecisionCaseQueueItemApi[];
  activeRecoveryOperations: readonly FactoryDecisionCaseQueueItemApi[];
  pendingHumanApproval: readonly FactoryDecisionCaseQueueItemApi[];
  blockedOrEscalated: readonly FactoryDecisionCaseQueueItemApi[];
  pendingVerification: readonly FactoryDecisionCaseQueueItemApi[];
  verifiedClosedToday: readonly FactoryDecisionCaseQueueItemApi[];
}>;

export type FactoryDecisionCenterWorkspaceApi = Readonly<{
  summary: FactoryDecisionCenterSummaryApi;
  priorityQueue: readonly FactoryDecisionCaseQueueItemApi[];
  liveQueues: FactoryDecisionCenterLiveQueuesApi;
  generatedAtUtc: string;
}>;

export type FactoryDecisionRecoveryStepApi = Readonly<{
  order: number;
  title: string;
  workerCode: string;
  workerName: string;
  status: string;
  linkedCaseId?: string;
  actionKey?: string;
}>;

export type FactoryDecisionWorkerAssignmentApi = Readonly<{
  workerCode: string;
  workerName: string;
  responsibility: string;
  status: string;
  blockerCode?: string;
}>;

export type FactoryDecisionEvidenceItemApi = Readonly<{
  category: string;
  label: string;
  status: string;
  summary: string;
  recordedAtUtc?: string;
}>;

export type FactoryDecisionApprovalCardApi = Readonly<{
  approvalId: string;
  decision: string;
  impact: string;
  status: string;
  concurrencyToken: number;
}>;

export type FactoryDecisionCaseDetailApi = Readonly<{
  caseId: string;
  title: string;
  severity: string;
  status: string;
  operationalProblem: string;
  businessOutcomeAtRisk: string;
  facilityLabel: string;
  orderReference: string;
  customerName: string;
  machineCode: string;
  moldCode: string;
  materialCode: string;
  deliveryCommitmentLabel: string;
  outcomeRisk: Readonly<{
    summary: string;
    requiredQuantity: number;
    projectedQuantity: number;
    gapQuantity: number;
    timeRemainingLabel: string;
    confidence: number;
  }>;
  assessment: Readonly<{
    primaryCause: string;
    contributingFactors: readonly string[];
  }>;
  recoveryPlan: readonly FactoryDecisionRecoveryStepApi[];
  digitalWorkers: readonly FactoryDecisionWorkerAssignmentApi[];
  humanDecision?: FactoryDecisionApprovalCardApi;
  executionProgress: readonly FactoryDecisionWorkerAssignmentApi[];
  evidence: readonly FactoryDecisionEvidenceItemApi[];
  verifiedOutcome: Readonly<{
    isVerified: boolean;
    requiredQuantity: number;
    verifiedProjection: number;
    deliveryProtected: boolean;
    recoveryGain: number;
    verifiedAtUtc?: string;
  }>;
  timeline: readonly Readonly<{ label: string; description: string; occurredAtUtc: string }>[];
  concurrencyToken: number;
}>;

async function factoryDecisionCenterFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
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

  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? body.title ?? "İstek başarısız.",
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readFactoryDecisionCenterWorkspace(
  context: ProductionOrderRequestContext,
): Promise<{ status: "success"; value: FactoryDecisionCenterWorkspaceApi } | { status: "failure"; code: string; message: string }> {
  return factoryDecisionCenterFetch<FactoryDecisionCenterWorkspaceApi>(context, "/api/factory-decision-center/workspace");
}

export async function readFactoryDecisionCaseDetail(
  context: ProductionOrderRequestContext,
  caseId: string,
): Promise<{ status: "success"; value: FactoryDecisionCaseDetailApi } | { status: "failure"; code: string; message: string }> {
  return factoryDecisionCenterFetch<FactoryDecisionCaseDetailApi>(context, `/api/factory-decision-center/cases/${caseId}`);
}

export async function postFactoryDecisionApproval(
  context: ProductionOrderRequestContext,
  caseId: string,
  approvalId: string,
  body: { approved: boolean; note?: string; concurrencyToken: number },
): Promise<{ status: "success"; value: FactoryDecisionCaseDetailApi } | { status: "failure"; code: string; message: string }> {
  return factoryDecisionCenterFetch<FactoryDecisionCaseDetailApi>(
    context,
    `/api/factory-decision-center/cases/${caseId}/approvals/${approvalId}/decision`,
    { method: "POST", body: JSON.stringify(body) },
  );
}

export async function postFactoryDecisionExecute(
  context: ProductionOrderRequestContext,
  caseId: string,
  concurrencyToken: number,
): Promise<{ status: "success"; value: FactoryDecisionCaseDetailApi } | { status: "failure"; code: string; message: string }> {
  return factoryDecisionCenterFetch<FactoryDecisionCaseDetailApi>(
    context,
    `/api/factory-decision-center/cases/${caseId}/execute`,
    { method: "POST", body: JSON.stringify({ concurrencyToken }) },
  );
}

export async function postFactoryDecisionVerify(
  context: ProductionOrderRequestContext,
  caseId: string,
  concurrencyToken: number,
): Promise<{ status: "success"; value: FactoryDecisionCaseDetailApi } | { status: "failure"; code: string; message: string }> {
  return factoryDecisionCenterFetch<FactoryDecisionCaseDetailApi>(
    context,
    `/api/factory-decision-center/cases/${caseId}/verify`,
    { method: "POST", body: JSON.stringify({ concurrencyToken }) },
  );
}

export async function postFactoryDecisionReset(
  context: ProductionOrderRequestContext,
): Promise<{ status: "success"; value: FactoryDecisionCenterWorkspaceApi } | { status: "failure"; code: string; message: string }> {
  return factoryDecisionCenterFetch<FactoryDecisionCenterWorkspaceApi>(
    context,
    "/api/factory-decision-center/reset",
    { method: "POST", body: JSON.stringify({}) },
  );
}

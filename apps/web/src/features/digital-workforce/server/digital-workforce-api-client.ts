import "./digital-workforce-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import type { DigitalAutonomyMode } from "../domain/digital-workforce-types";

export type DigitalWorkerApiSummary = Readonly<{
  id: string;
  code: string;
  name: string;
  description: string;
  mission: string;
  status: string;
  autonomyMode: string;
  enabled: boolean;
  isPaused: boolean;
  isKilled: boolean;
  isHumanControlled: boolean;
  sourceClassification: string;
  activeCaseCount: number;
  queueCount: number;
  completedToday: number;
  completedThisMonth: number;
  verifiedClosures: number;
  pendingApprovals: number;
  lastActivityAt: string | null;
  aiCostToday: number;
  permissionStatus: string;
  concurrencyToken: number;
}>;

export type DigitalWorkforceControlCenterSnapshot = Readonly<{
  workers: readonly DigitalWorkerApiSummary[];
  globalPauseActive: boolean;
  globalKillActive: boolean;
  openCases: number;
  escalatedCases: number;
  pendingApprovals: number;
  verificationRate: number;
  totalAiCostToday: number;
  totalAiCostMonth: number;
}>;

export type DigitalWorkCaseApiSummary = Readonly<{
  id: string;
  title: string;
  description: string;
  status: string;
  currentStage: string;
  severity: string;
  primaryWorkerId: string;
  primaryWorkerCode: string;
  primaryWorkerName: string;
  requiresApproval: boolean;
  isHumanControlled: boolean;
  verificationState: string;
  sourceClassification: string;
  startedAtUtc: string | null;
  closedAtUtc: string | null;
  verifiedAtUtc: string | null;
  outcome: string;
  concurrencyToken: number;
}>;

export type DigitalWorkActivityApiSummary = Readonly<{
  id: string;
  caseId: string;
  workerId: string;
  workerCode: string;
  actionType: string;
  description: string;
  result: string;
  sourceClassification: string;
  createdAtUtc: string;
}>;

export type DigitalWorkApprovalApiSummary = Readonly<{
  id: string;
  caseId: string;
  workerId: string;
  workerCode: string;
  action: string;
  businessReason: string;
  impact: string;
  riskLevel: number;
  amount: number | null;
  status: string;
  createdAtUtc: string;
  expiresAtUtc: string | null;
  concurrencyToken: number;
}>;

export type DigitalWorkerPolicyApi = Readonly<{
  allowedToolsJson: string;
  allowedActionsJson: string;
  prohibitedActionsJson: string;
  approvalRequiredForJson: string;
  maxTransactionValue: number;
  maxRiskLevel: number;
  externalCommunicationPolicy: string;
  aiBudgetDaily: number;
  aiBudgetMonthly: number;
  concurrencyToken: number;
}>;

export type DigitalWorkerDetailSnapshot = Readonly<{
  workerId: string;
  code: string;
  name: string;
  description: string;
  mission: string;
  status: string;
  autonomyMode: string;
  isPaused: boolean;
  isKilled: boolean;
  isHumanControlled: boolean;
  sourceClassification: string;
  concurrencyToken: number;
  policy: DigitalWorkerPolicyApi | null;
  budget: Readonly<{ dailyLimit: number; monthlyLimit: number; dailyConsumed: number; monthlyConsumed: number; state: string }> | null;
  currentCase: DigitalWorkCaseApiSummary | null;
  openCases: readonly DigitalWorkCaseApiSummary[];
  closedCases: readonly DigitalWorkCaseApiSummary[];
  activity: readonly DigitalWorkActivityApiSummary[];
  approvals: readonly DigitalWorkApprovalApiSummary[];
}>;

export type UpdatePolicyPayload = Readonly<{
  allowedToolsJson: string;
  allowedActionsJson: string;
  prohibitedActionsJson: string;
  approvalRequiredForJson: string;
  maxTransactionValue: number;
  maxRiskLevel: number;
  externalCommunicationPolicy: string;
  aiBudgetDaily: number;
  aiBudgetMonthly: number;
  concurrencyToken: number;
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
      if (value) {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

async function request(
  context: ProductionOrderRequestContext,
  method: string,
  path: string,
  body?: unknown,
  query?: Record<string, string | undefined>,
): Promise<Response> {
  const response = await fetch(buildUrl(context, path, query), {
    method,
    headers: {
      ...context.requestHeaders,
      "Content-Type": "application/json",
    },
    body: body ? JSON.stringify(body) : undefined,
    cache: "no-store",
  });
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `İstek başarısız (${response.status})`);
  }
  return response;
}

export async function fetchDigitalWorkforceControlCenter(
  context: ProductionOrderRequestContext,
): Promise<DigitalWorkforceControlCenterSnapshot> {
  const response = await request(context, "GET", "/api/digital-workforce/control-center");
  return (await response.json()) as DigitalWorkforceControlCenterSnapshot;
}

export async function fetchWorkerDetail(
  context: ProductionOrderRequestContext,
  workerApiId: string,
): Promise<DigitalWorkerDetailSnapshot> {
  const response = await request(context, "GET", `/api/digital-workforce/workers/${workerApiId}/detail`);
  return (await response.json()) as DigitalWorkerDetailSnapshot;
}

export async function updateWorkerAutonomy(
  context: ProductionOrderRequestContext,
  workerApiId: string,
  autonomyMode: DigitalAutonomyMode,
  concurrencyToken: number,
): Promise<void> {
  await request(context, "PUT", `/api/digital-workforce/workers/${workerApiId}/autonomy`, {
    autonomyMode,
    concurrencyToken,
  });
}

export async function updateWorkerPolicy(
  context: ProductionOrderRequestContext,
  workerApiId: string,
  payload: UpdatePolicyPayload,
): Promise<void> {
  await request(context, "PUT", `/api/digital-workforce/workers/${workerApiId}/policy`, payload);
}

export async function pauseWorker(
  context: ProductionOrderRequestContext,
  workerApiId: string,
  concurrencyToken: number,
): Promise<void> {
  await request(context, "POST", `/api/digital-workforce/workers/${workerApiId}/pause`, undefined, {
    concurrencyToken: String(concurrencyToken),
  });
}

export async function resumeWorker(
  context: ProductionOrderRequestContext,
  workerApiId: string,
  concurrencyToken: number,
): Promise<void> {
  await request(context, "POST", `/api/digital-workforce/workers/${workerApiId}/resume`, undefined, {
    concurrencyToken: String(concurrencyToken),
  });
}

export async function globalPause(context: ProductionOrderRequestContext): Promise<void> {
  await request(context, "POST", "/api/digital-workforce/control/global-pause");
}

export async function globalResume(context: ProductionOrderRequestContext): Promise<void> {
  await request(context, "POST", "/api/digital-workforce/control/global-resume");
}

export async function globalKill(context: ProductionOrderRequestContext): Promise<void> {
  await request(context, "POST", "/api/digital-workforce/control/global-kill");
}

export async function globalSuggestMode(context: ProductionOrderRequestContext): Promise<void> {
  await request(context, "POST", "/api/digital-workforce/control/global-suggest-mode");
}

export async function applyDefaultPolicies(context: ProductionOrderRequestContext): Promise<void> {
  await request(context, "POST", "/api/digital-workforce/control/apply-policies");
}

export async function takeOverCase(
  context: ProductionOrderRequestContext,
  caseApiId: string,
): Promise<void> {
  await request(context, "POST", `/api/digital-workforce/cases/${caseApiId}/takeover`);
}

export async function resumeHumanControl(
  context: ProductionOrderRequestContext,
  caseApiId: string,
): Promise<void> {
  await request(context, "POST", `/api/digital-workforce/cases/${caseApiId}/resume-human-control`);
}

export async function decideApproval(
  context: ProductionOrderRequestContext,
  approvalId: string,
  decision: "approved" | "rejected" | "modified",
  concurrencyToken: number,
): Promise<void> {
  await request(context, "POST", `/api/digital-workforce/approvals/${approvalId}/decision`, {
    decision,
    concurrencyToken,
  });
}

export async function fetchApprovals(
  context: ProductionOrderRequestContext,
  status?: string,
): Promise<readonly DigitalWorkApprovalApiSummary[]> {
  const response = await request(context, "GET", "/api/digital-workforce/approvals", undefined, {
    status,
  });
  return (await response.json()) as DigitalWorkApprovalApiSummary[];
}

export async function fetchActivity(
  context: ProductionOrderRequestContext,
): Promise<readonly DigitalWorkActivityApiSummary[]> {
  const response = await request(context, "GET", "/api/digital-workforce/activity");
  return (await response.json()) as DigitalWorkActivityApiSummary[];
}

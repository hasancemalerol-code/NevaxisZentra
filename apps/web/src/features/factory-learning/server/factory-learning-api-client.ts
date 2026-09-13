import "./factory-learning-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type FactoryLearningCommandSummaryApi = Readonly<{
  todayLossMinutes: number;
  sevenDayLossMinutes: number;
  thirtyDayLossMinutes: number;
  recurringLossCount: number;
  activeExperimentCount: number;
  verifiedImprovementCount: number;
  regressingImprovementCount: number;
}>;

export type FactoryLearningParetoRowApi = Readonly<{
  category: string;
  label: string;
  totalMinutes: number;
  contributionPercent: number;
  occurrenceCount: number;
  dataClassification: string;
}>;

export type FactoryLearningLossApi = Readonly<{
  id: string;
  category: string;
  title: string;
  summary: string;
  machineId?: string;
  durationMinutes: number;
  sourceModule: string;
  evidenceJson: string;
  dataClassification: string;
  confidenceScore: number;
  recurrenceCount: number;
  lossTreePathJson: string;
  lastSeenAtUtc: string;
}>;

export type FactoryLearningPatternApi = Readonly<{
  id: string;
  patternType: string;
  title: string;
  summary: string;
  patternKey: string;
  occurrenceCount: number;
  confidenceScore: number;
  dataClassification: string;
}>;

export type FactoryLearningOpportunityApi = Readonly<{
  id: string;
  title: string;
  problemSummary: string;
  status: string;
  priorityScore: number;
  recoverablePotentialScore: number;
  impactClassification: string;
  domain: string;
  confidenceScore: number;
  ownerSubjectId: string;
  concurrencyToken: number;
}>;

export type FactoryLearningExperimentApi = Readonly<{
  id: string;
  opportunityId: string;
  experimentType: string;
  title: string;
  hypothesis: string;
  status: string;
  outcome: string;
  approvalState: string;
  startedAtUtc?: string;
  completedAtUtc?: string;
  verifiedAtUtc?: string;
  concurrencyToken: number;
}>;

export type FactoryLearningVerifiedImprovementApi = Readonly<{
  id: string;
  title: string;
  outcomeSummary: string;
  beforeJson: string;
  afterJson: string;
  deltaJson: string;
  effectivenessScore: number;
  impactClassification: string;
  standardizationState: string;
  sustainabilityStatus: string;
  verifiedAtUtc: string;
  concurrencyToken: number;
}>;

export type FactoryLearningMemoryApi = Readonly<{
  id: string;
  problemPattern: string;
  resultSummary: string;
  effectivenessScore: number;
  memoryQuality: string;
  evidenceJson: string;
  recordedAtUtc: string;
}>;

export type FactoryLearningInsightApi = Readonly<{
  insightType: string;
  title: string;
  summary: string;
  confidenceScore: number;
  evidenceSummary: string;
  linkedMemoryId?: string;
}>;

export type FactoryLearningWorkspaceApi = Readonly<{
  commandSummary: FactoryLearningCommandSummaryApi;
  paretoRows: readonly FactoryLearningParetoRowApi[];
  losses: readonly FactoryLearningLossApi[];
  patterns: readonly FactoryLearningPatternApi[];
  opportunities: readonly FactoryLearningOpportunityApi[];
  activeExperiments: readonly FactoryLearningExperimentApi[];
  verifiedImprovements: readonly FactoryLearningVerifiedImprovementApi[];
  learningMemories: readonly FactoryLearningMemoryApi[];
  insights: readonly FactoryLearningInsightApi[];
  dataFreshnessUtc: string;
}>;

async function factoryLearningFetch<T>(
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
    const code =
      typeof json === "object" && json !== null && "code" in json
        ? String((json as { code: string }).code)
        : `http-${response.status}`;
    return { status: "failure", code, message };
  }

  return { status: "success", value: json as T };
}

export async function getFactoryLearningWorkspace(context: ProductionOrderRequestContext) {
  return factoryLearningFetch<FactoryLearningWorkspaceApi>(context, "/api/factory-learning/workspace");
}

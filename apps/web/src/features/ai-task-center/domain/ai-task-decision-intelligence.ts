export type AiTaskDecisionConfidenceBand = "high" | "medium" | "low" | "insufficient-data";

export type AiTaskDecisionImpactBand = "low" | "medium" | "high" | "critical" | "insufficient-data";

export type AiTaskDecisionSlaRiskBand = "low" | "watch" | "high" | "critical" | "insufficient-data";

export type AiTaskDecisionTriage = Readonly<{
  suggestedCategory: string | null;
  suggestedTaskType: string | null;
  suggestedPriority: string | null;
  suggestedSeverity: string | null;
  suggestedDepartment: string | null;
  suggestedTeamChannelId: string | null;
  businessImpact: string | null;
  urgencyReason: string | null;
  operationalImpactBand: string;
  urgencyBand: string;
  slaBreachRiskBand: string;
  estimatedRiskReason: string | null;
  contributingFactors: readonly string[];
  recommendedIntervention: string | null;
  explanation: string;
  confidenceBand: AiTaskDecisionConfidenceBand;
  insufficientData: boolean;
  nextActions: readonly AiTaskDecisionNextAction[];
  evidenceRecommendations: readonly AiTaskDecisionEvidenceRecommendation[];
  relatedCandidates: readonly AiTaskDecisionRelatedCandidate[];
  policyComparison: AiTaskDecisionPolicyComparison | null;
}>;

export type AiTaskDecisionNextAction = Readonly<{
  actionType: string;
  title: string;
  rationale: string;
  confidenceBand: AiTaskDecisionConfidenceBand;
  urgencyBand: string;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  payloadJson: string;
}>;

export type AiTaskDecisionEvidenceRecommendation = Readonly<{
  evidenceType: string;
  title: string;
  rationale: string;
  policyMandatory: boolean;
  confidenceBand: AiTaskDecisionConfidenceBand;
}>;

export type AiTaskDecisionRelatedCandidate = Readonly<{
  candidateTaskId: string;
  relationshipType: string;
  confidenceBand: AiTaskDecisionConfidenceBand;
  explanation: string;
  candidateTaskTitle?: string;
  candidateTaskStatus?: string;
  candidateTaskPriority?: string;
}>;

export type AiTaskDecisionPolicyComparison = Readonly<{
  policySummary: string;
  policyActions: readonly string[];
  aiSummary: string;
  aiDifferences: readonly string[];
}>;

export type AiTaskDecisionClosureReview = Readonly<{
  closureReadiness: string;
  concerns: readonly string[];
  recommendations: readonly string[];
  explanation: string;
  confidenceBand: AiTaskDecisionConfidenceBand;
  insufficientData: boolean;
}>;

export type AiTaskDecisionRecommendation = Readonly<{
  id: string;
  analysisId: string;
  actionType: string;
  title: string;
  rationale: string;
  confidenceBand: AiTaskDecisionConfidenceBand;
  urgencyBand: string;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  payloadJson: string;
  status: string;
  feedbackDecision: string | null;
  feedbackReason: string | null;
  feedbackAtUtc: string | null;
  appliedResultJson: string | null;
  createdAtUtc: string;
}>;

export type AiTaskDecisionAnalysis = Readonly<{
  id: string;
  taskId: string;
  analysisType: string;
  providerName: string;
  modelName: string;
  promptTemplateVersion: string;
  confidenceBand: AiTaskDecisionConfidenceBand;
  triage: AiTaskDecisionTriage | null;
  closureReview: AiTaskDecisionClosureReview | null;
  superseded: boolean;
  supersededByAnalysisId: string | null;
  createdAtUtc: string;
  createdBy: string;
  recommendations: readonly AiTaskDecisionRecommendation[];
}>;

export type AiTaskDecisionRelationship = Readonly<{
  id: string;
  sourceTaskId: string;
  targetTaskId: string;
  targetTaskTitle: string;
  targetTaskStatus: string;
  targetTaskPriority: string;
  targetSourceModule: string | null;
  relationshipType: string;
  status: string;
  confidenceBand: AiTaskDecisionConfidenceBand;
  explanation: string | null;
  confirmedAtUtc: string | null;
  createdAtUtc: string;
}>;

export type AiTaskDecisionWorkspace = Readonly<{
  latestAnalysis: AiTaskDecisionAnalysis | null;
  history: readonly AiTaskDecisionAnalysis[];
  relationships: readonly AiTaskDecisionRelationship[];
  suggestedRelated: readonly AiTaskDecisionRelatedCandidate[];
  providerAvailable: boolean;
  providerStatusMessage: string | null;
}>;

export type AiTaskDecisionCapabilities = Readonly<{
  canRead: boolean;
  canAnalyze: boolean;
  canFeedback: boolean;
}>;

/** The task assistant is rule based; the provider id is shown as what it is, never as an AI model. */
export function getAiTaskProviderLabel(providerName: string): string {
  return providerName === "deterministic-development"
    ? "Kural tabanlı (yapay zekâ modeli değil)"
    : providerName;
}

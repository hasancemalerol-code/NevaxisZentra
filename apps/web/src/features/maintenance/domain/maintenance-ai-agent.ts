export type MaintenanceAiDraftStatus =
  | "draft"
  | "under-review"
  | "approved"
  | "rejected"
  | "stale"
  | "executed"
  | "execution-failed";

export type MaintenanceAiQualitativeConfidence = "low" | "medium" | "high";

export type MaintenanceAiEvidenceReference = Readonly<{
  evidenceType: string;
  evidenceReference: string;
  summary: string;
}>;

export type MaintenanceAiStructuredOutput = Readonly<{
  summary: string;
  observedCondition: string;
  suggestedTitle: string;
  suggestedDescription: string;
  suggestedPriority: string;
  suggestedRequestType: string;
  recommendedReviewType: string;
  evidenceReferences: readonly MaintenanceAiEvidenceReference[];
  missingInformation: readonly string[];
  uncertaintyExplanation: string;
  qualitativeConfidence: MaintenanceAiQualitativeConfidence;
  safetyNotes: readonly string[];
  prohibitedActionConfirmation: boolean;
}>;

export type MaintenanceAiDraftDetail = Readonly<{
  id: string;
  assetReference: string;
  assetName?: string;
  status: MaintenanceAiDraftStatus;
  qualitativeConfidence: MaintenanceAiQualitativeConfidence;
  conditionEvaluationReference: string;
  riskAssessmentId: string;
  inputFingerprint: string;
  ruleSetVersion: string;
  providerIdentifier: string;
  modelIdentifier: string;
  promptVersion: string;
  isDevelopmentProvider: boolean;
  dataFreshness: string;
  dataSufficiency: string;
  riskBand: string;
  deterministicRecommendationCategory: string;
  validatedOutput: MaintenanceAiStructuredOutput;
  aiOriginalOutput: MaintenanceAiStructuredOutput;
  suggestedTitle: string;
  suggestedDescription: string;
  suggestedPriority: string;
  suggestedRequestType: string;
  humanEditedTitle?: string;
  humanEditedDescription?: string;
  humanEditedPriority?: string;
  humanEditedRequestType?: string;
  humanEditorSubjectId?: string;
  humanEditedAtUtc?: string;
  generatedBySubjectId: string;
  generatedAtUtc: string;
  approvedBySubjectId?: string;
  approvedAtUtc?: string;
  rejectedBySubjectId?: string;
  rejectedAtUtc?: string;
  rejectionReason?: string;
  createdMaintenanceRequestId?: string;
  createdMaintenanceRequestNumber?: string;
  failureCode?: string;
  failureMessage?: string;
  version: number;
  auditTrail: readonly Readonly<{
    action: string;
    actorSubjectId: string;
    draftVersion: number;
    resultCode: string;
    occurredAtUtc: string;
  }>[];
}>;

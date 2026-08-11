export type MaintenancePredictiveRiskBand =
  | "insufficient-data"
  | "low"
  | "watch"
  | "high"
  | "critical";

export type MaintenancePredictiveReviewPriority =
  | "routine"
  | "elevated"
  | "planned"
  | "urgent";

export type MaintenancePredictiveRecommendationCategory =
  | "review-data-quality"
  | "continue-routine-monitoring"
  | "close-monitoring"
  | "plan-technical-review"
  | "urgent-authorized-technical-review";

export type MaintenancePredictiveRiskOverview = Readonly<{
  monitoredAssetCount: number;
  insufficientDataCount: number;
  lowRiskCount: number;
  watchCount: number;
  highRiskCount: number;
  criticalRiskCount: number;
  lastAssessedAtUtc?: string;
}>;

export type MaintenancePredictiveRiskAssetRow = Readonly<{
  assetReference: string;
  assetName?: string;
  machineCode?: string;
  machineName?: string;
  riskBand: MaintenancePredictiveRiskBand;
  reviewPriority: MaintenancePredictiveReviewPriority;
  dataFreshness: string;
  dataSufficiency: string;
  assessedAtUtc?: string;
}>;

export type MaintenancePredictiveRiskDriver = Readonly<{
  driverCode: string;
  summary: string;
}>;

export type MaintenancePredictiveRiskEvidence = Readonly<{
  evidenceType: string;
  evidenceReference: string;
  summary: string;
}>;

export type MaintenancePredictiveRecommendation = Readonly<{
  category: MaintenancePredictiveRecommendationCategory;
  reviewPriority: MaintenancePredictiveReviewPriority;
  explanation: string;
  ruleSetVersion: string;
  generatedAtUtc: string;
  status: string;
}>;

export type MaintenancePredictiveRiskAssessment = Readonly<{
  assetReference: string;
  assetName?: string;
  machineCode?: string;
  machineName?: string;
  riskBand: MaintenancePredictiveRiskBand;
  reviewPriority: MaintenancePredictiveReviewPriority;
  ruleSetVersion: string;
  conditionEvaluationIdentity?: string;
  dataFreshness: string;
  dataSufficiency: string;
  activeAnomalyCount: number;
  maintenanceHistorySummary: string;
  explanation: string;
  assessedAtUtc: string;
  riskDrivers: readonly MaintenancePredictiveRiskDriver[];
  evidence: readonly MaintenancePredictiveRiskEvidence[];
  recommendation?: MaintenancePredictiveRecommendation;
}>;

export type MaintenancePredictiveRiskWorkspace = Readonly<{
  overview: MaintenancePredictiveRiskOverview;
  assets: readonly MaintenancePredictiveRiskAssetRow[];
}>;

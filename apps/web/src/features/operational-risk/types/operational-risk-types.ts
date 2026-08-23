export type OperationalRiskTabId =
  | "overview"
  | "cases"
  | "profiles"
  | "runs";

export type OperationalRiskKpiTone =
  | "critical"
  | "attention"
  | "informational"
  | "controlled"
  | "insufficient"
  | "conflict"
  | "neutral";

export type OperationalRiskKpiCard = Readonly<{
  id: string;
  label: string;
  value: string;
  detail: string;
  tone: OperationalRiskKpiTone;
  scopeLabel: string;
  evidenceLabel: string;
  stateBadge: string;
  unavailableReason?: string;
}>;

export type OperationalRiskDistributionSlice = Readonly<{
  key: string;
  label: string;
  count: number;
  ratio: number | null;
  color: string;
}>;

export type OperationalRiskSourceAdapterState =
  | "active"
  | "unavailable"
  | "insufficient-data"
  | "source-conflict";

export type OperationalRiskSourceAdapterCoverage = Readonly<{
  module: string;
  label: string;
  state: OperationalRiskSourceAdapterState;
  detail: string;
  evidenceLabel: string;
}>;

export type OperationalRiskCaseSummary = Readonly<{
  id: string;
  caseReference: string;
  category: string;
  categoryLabel: string;
  scopeType: string;
  scopeLabel: string;
  scopeReference: string | null;
  title: string;
  evidenceState: string;
  evidenceLabel: string;
  likelihoodLevel: number | null;
  impactLevel: number | null;
  riskScore: number | null;
  severityBand: string | null;
  severityLabel: string;
  assessmentOutcome: string;
  assessmentLabel: string;
  lifecycleStatus: string;
  lifecycleLabel: string;
  sourceModule: string | null;
  sourceModuleLabel: string;
  ruleSetVersion: string;
  firstDetectedAtUtc: string;
  lastObservedAtUtc: string;
  lastObservedLabel: string;
  version: number;
  isPilotData: boolean;
  isTerminal: boolean;
}>;

export type OperationalRiskEvidenceItem = Readonly<{
  id: string;
  evidenceTypeCode: string;
  evidenceTypeLabel: string;
  evidenceState: string;
  evidenceLabel: string;
  observedAtUtc: string;
  observedLabel: string;
  sourceModule: string | null;
  sourceModuleLabel: string;
  sourceReference: string | null;
  summary: string;
  numericValue: number | null;
  unit: string | null;
  comparisonReference: string | null;
  sourceProvenance: string;
  createdAtUtc: string;
  isPilotData: boolean;
  technicalAuditTokens: readonly string[];
}>;

export type OperationalRiskStatusHistoryItem = Readonly<{
  id: string;
  previousStatus: string;
  previousLabel: string;
  newStatus: string;
  newLabel: string;
  reasonNote: string | null;
  actorSubjectId: string;
  occurredAtUtc: string;
  occurredLabel: string;
  expectedCaseVersion: number;
  sourceProvenance: string;
}>;

export type OperationalRiskCaseDetail = Readonly<{
  summary: OperationalRiskCaseSummary;
  description: string;
  sourceProvenance: string;
  evidence: readonly OperationalRiskEvidenceItem[];
  statusHistory: readonly OperationalRiskStatusHistoryItem[];
  allowedTransitions: readonly string[];
  leadingDriverLabel: string | null;
  supportingDriverLabels: readonly string[];
}>;

export type OperationalRiskDetectionProfileRule = Readonly<{
  id: string;
  ruleCode: string;
  sourceModule: string;
  sourceModuleLabel: string;
  riskCategory: string;
  categoryLabel: string;
  thresholdVarianceBand: number | null;
  driverReasonCode: string;
  driverLabel: string;
  isActive: boolean;
  priorityOrder: number;
}>;

export type OperationalRiskDetectionProfileSummary = Readonly<{
  id: string;
  profileCode: string;
  displayName: string;
  status: string;
  statusLabel: string;
  ruleSetVersion: string;
  sourceProvenance: string;
  version: number;
  ruleCount: number;
  activeRuleCount: number;
  sourceModules: readonly string[];
  sourceModuleLabels: readonly string[];
  categories: readonly string[];
  categoryLabels: readonly string[];
  rules: readonly OperationalRiskDetectionProfileRule[];
  isExecutable: boolean;
  isPilotData: boolean;
  updatedLabel: string;
}>;

export type OperationalRiskDetectionRunSummary = Readonly<{
  id: string;
  runReference: string;
  profileId: string;
  profileCode: string;
  windowStartUtc: string;
  windowEndUtc: string;
  windowLabel: string;
  status: string;
  statusLabel: string;
  sourceCount: number;
  resultCount: number;
  outcomeCounts: Readonly<Record<string, number>>;
  ruleSetVersion: string;
  createdAtUtc: string;
  createdLabel: string;
  completedAtUtc: string | null;
  completedLabel: string | null;
  isPilotData: boolean;
}>;

export type OperationalRiskDetectionDriver = Readonly<{
  id: string;
  reasonCode: string;
  reasonLabel: string;
  severity: string;
  severityLabel: string;
  actualValue: number | null;
  thresholdValue: number | null;
  unit: string | null;
  normalizedVariance: number | null;
  supportingText: string;
  sortOrder: number;
}>;

export type OperationalRiskDetectionResult = Readonly<{
  id: string;
  sourceModule: string;
  sourceModuleLabel: string;
  sourceReference: string;
  riskCategory: string;
  categoryLabel: string;
  scopeType: string;
  scopeLabel: string;
  scopeReference: string | null;
  evidenceState: string;
  evidenceLabel: string;
  sourceOutcome: string;
  sourceOutcomeLabel: string;
  detectionOutcome: string;
  detectionOutcomeLabel: string;
  actualValue: number | null;
  thresholdValue: number | null;
  unit: string | null;
  normalizedVariance: number | null;
  likelihoodLevel: number | null;
  impactLevel: number | null;
  riskScore: number | null;
  severityBand: string | null;
  severityLabel: string;
  leadingDriverCode: string | null;
  leadingDriverLabel: string;
  linkedCaseId: string | null;
  linkedCaseReference: string | null;
  detectionKey: string;
  sourceAssessmentReference: string;
  evaluatedAtUtc: string;
  evaluatedLabel: string;
  drivers: readonly OperationalRiskDetectionDriver[];
}>;

export type OperationalRiskDetectionRunDetail = Readonly<{
  summary: OperationalRiskDetectionRunSummary;
  sourceProvenance: string;
  results: readonly OperationalRiskDetectionResult[];
  groupedOutcomes: Readonly<Record<string, readonly OperationalRiskDetectionResult[]>>;
}>;

export type OperationalRiskDetectionSummary = Readonly<{
  recentRuns: readonly OperationalRiskDetectionRunSummary[];
  outcomeTotals: Readonly<Record<string, number>>;
  reconciledTotal: number;
  hasRuns: boolean;
}>;

export type OperationalRiskWorkspacePayload = Readonly<{
  facilityLabel: string;
  reportingPeriodLabel: string;
  freshnessLabel: string;
  evidenceLabel: string;
  provenanceDisclaimer: string;
  ruleSetVersion: string;
  hasDevelopmentPilotData: boolean;
  kpis: readonly OperationalRiskKpiCard[];
  distribution: Readonly<{
    slices: readonly OperationalRiskDistributionSlice[];
    total: number;
    centerLabel: string;
    unavailableReason: string | null;
  }>;
  sourceCoverage: readonly OperationalRiskSourceAdapterCoverage[];
  priorityCases: readonly OperationalRiskCaseSummary[];
  detectionSummary: OperationalRiskDetectionSummary;
  cases: readonly OperationalRiskCaseSummary[];
  profiles: readonly OperationalRiskDetectionProfileSummary[];
  runs: readonly OperationalRiskDetectionRunSummary[];
}>;

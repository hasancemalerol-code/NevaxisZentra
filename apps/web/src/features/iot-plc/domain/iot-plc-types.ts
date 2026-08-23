export type IotPlcTabId = "overview" | "connections" | "signals" | "quality";

export type IotPlcHealthBucket = "online" | "degraded" | "offline" | "unknown";

export type IotPlcKpiTone =
  | "info"
  | "success"
  | "warning"
  | "danger"
  | "unavailable";

export type IotPlcKpiCard = Readonly<{
  id: string;
  label: string;
  value: string;
  comparisonLabel: string;
  tone: IotPlcKpiTone;
  badgeLabel: string | null;
}>;

export type IotPlcHealthSegment = Readonly<{
  bucket: IotPlcHealthBucket;
  label: string;
  count: number;
  percent: number;
  color: string;
}>;

export type IotPlcQualityDistributionSlice = Readonly<{
  key: string;
  label: string;
  count: number;
  ratio: number | null;
  color: string;
}>;

export type IotPlcFinding = Readonly<{
  id: string;
  title: string;
  detail: string;
  severity: "danger" | "warning" | "info" | "neutral";
  technicalCode: string | null;
  targetLabel?: string;
  leadingDriverLabel?: string;
  periodLabel?: string;
  evidenceLabel?: string;
  outcomeLabel?: string;
}>;

export type IotPlcHealthTimelinePoint = Readonly<{
  endpointCode: string;
  endpointLabel: string;
  observedAtUtc: string;
  healthState: string;
  healthLabel: string;
  evidenceState: string;
}>;

export type IotPlcEndpointSummary = Readonly<{
  reference: string;
  endpointCode: string;
  displayName: string;
  protocolType: string;
  protocolLabel: string;
  connectionDirection: string;
  connectionDirectionLabel: string;
  endpointStatus: string;
  endpointStatusLabel: string;
  healthState: string | null;
  healthLabel: string;
  healthBucket: IotPlcHealthBucket;
  lastEvidenceAtUtc: string | null;
  lastEvidenceLabel: string;
  signalCount: number;
  evidenceModeLabel: string;
  configurationProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type IotPlcEndpointDetail = Readonly<{
  summary: IotPlcEndpointSummary;
  connectionProfileJson: string;
  mappedSignals: readonly IotPlcSignalSummary[];
  healthHistory: readonly IotPlcHealthTimelinePoint[];
}>;

export type IotPlcSignalSummary = Readonly<{
  reference: string;
  signalCode: string;
  displayName: string;
  endpointCode: string;
  endpointLabel: string;
  semanticType: string;
  semanticTypeLabel: string;
  dataType: string;
  engineeringUnit: string | null;
  mappedAssetLabel: string;
  isEnabled: boolean;
  enabledLabel: string;
  latestObservationAtUtc: string | null;
  latestObservationLabel: string;
  latestQualityState: string | null;
  latestQualityLabel: string;
  latestEvidenceLabel: string;
  samplingExpectation: string | null;
  sourceAddress: string;
  sourceProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type IotPlcSignalDetail = Readonly<{
  summary: IotPlcSignalSummary;
  recentObservations: readonly IotPlcObservationSummary[];
  qualityDistribution: readonly IotPlcQualityDistributionSlice[];
  qualityDistributionAvailable: boolean;
}>;

export type IotPlcObservationSummary = Readonly<{
  observedAtUtc: string;
  receivedAtUtc: string;
  qualityState: string;
  qualityLabel: string;
  evidenceState: string;
  evidenceLabel: string;
  valueLabel: string;
  sequenceReference: string | null;
}>;

export type IotPlcQualityProfileSummary = Readonly<{
  reference: string;
  profileCode: string;
  displayName: string;
  status: string;
  statusLabel: string;
  minimumGoodQualityRatio: number;
  maximumBadQualityRatio: number;
  maximumUncertainQualityRatio: number;
  maximumSourceConflictRatio: number;
  freshnessToleranceSeconds: number;
  staleThresholdSeconds: number;
  sequenceGapEvaluationEnabled: boolean;
  sourceProvenance: string;
  version: number;
  isPilotData: boolean;
}>;

export type IotPlcAssessmentSummary = Readonly<{
  id: string;
  assessmentKind: "connectivity" | "signal";
  targetLabel: string;
  targetReference: string;
  evaluationPeriodLabel: string;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  evidenceMode: string;
  evidenceLabel: string;
  outcome: string;
  outcomeLabel: string;
  leadingDriverCode: string | null;
  leadingDriverLabel: string;
  profileCode: string | null;
  evaluatedAtUtc: string;
  isPilotData: boolean;
}>;

export type IotPlcDriverSummary = Readonly<{
  reasonCode: string;
  reasonLabel: string;
  explanation: string;
  isLeadingDriver: boolean;
  affectedObservationCount: number;
}>;

export type IotPlcAssessmentDetail = Readonly<{
  summary: IotPlcAssessmentSummary;
  actualObservationCount: number | null;
  expectedObservationCount: number | null;
  completenessRatio: number | null;
  goodRatio: number | null;
  uncertainRatio: number | null;
  badRatio: number | null;
  staleRatio: number | null;
  sourceConflictRatio: number | null;
  freshnessSeconds: number | null;
  maxClockSkewSeconds: number | null;
  latestHealthState: string | null;
  healthObservationAvailabilityRatio: number | null;
  profileLabel: string;
  provenance: string;
  drivers: readonly IotPlcDriverSummary[];
}>;

export type IotPlcWorkspaceMeta = Readonly<{
  facilityLabel: string;
  organizationLabel: string;
  refreshedAtUtc: string;
  refreshedLabel: string;
  provenanceDisclaimer: string;
  hasDevelopmentPilotData: boolean;
  ruleSetVersion: string;
  standardProfileStaleSeconds: number | null;
  standardProfileFreshnessSeconds: number | null;
}>;

export type IotPlcWorkspacePayload = Readonly<{
  meta: IotPlcWorkspaceMeta;
  kpis: readonly IotPlcKpiCard[];
  healthComposition: readonly IotPlcHealthSegment[];
  healthTotal: number;
  qualityDistribution: readonly IotPlcQualityDistributionSlice[];
  qualityDenominator: number | null;
  qualityAssessmentLabel: string | null;
  qualityContractViolationCount: number;
  qualitySelectedAssessmentId: string | null;
  findings: readonly IotPlcFinding[];
  healthTimeline: readonly IotPlcHealthTimelinePoint[];
  endpoints: readonly IotPlcEndpointSummary[];
  signals: readonly IotPlcSignalSummary[];
  profiles: readonly IotPlcQualityProfileSummary[];
  connectivityAssessments: readonly IotPlcAssessmentSummary[];
  signalAssessments: readonly IotPlcAssessmentSummary[];
  partialErrors: readonly string[];
  sourceAvailable: boolean;
}>;

export type IotPlcWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

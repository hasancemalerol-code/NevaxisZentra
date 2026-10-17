export type ExecutiveCommandCenterTimeScope = "today" | "shift" | "recent";

export type ExecutiveCommandCenterFilters = Readonly<{
  severity?: string;
  outcomeType?: string;
  timeScope: ExecutiveCommandCenterTimeScope;
}>;

export type ExecutiveScopeState = "full" | "partial";

export type ExecutiveOutcomeCardTone = "negative" | "warning" | "neutral" | "positive";

export type ExecutiveOutcomeCard = Readonly<{
  id: string;
  label: string;
  value: string;
  detail: string;
  tone: ExecutiveOutcomeCardTone;
  drillDownHref: string;
  drillDownLabel: string;
}>;

export type ExecutivePrioritySection =
  | "critical-now"
  | "decision-today"
  | "active-recovery"
  | "pending-verification"
  | "verified-today";

export type ExecutiveDrillDownModule =
  | "decision"
  | "intelligence"
  | "task"
  | "workforce"
  | "production";

export type ExecutivePriorityItem = Readonly<{
  id: string;
  section: ExecutivePrioritySection;
  outcome: string;
  impact: string;
  status: string;
  owner: string;
  nextAction: string;
  timeToImpact?: string;
  verificationState: string;
  drillDownHref: string;
  drillDownModule: ExecutiveDrillDownModule;
}>;

export type ExecutivePredictiveRisk = Readonly<{
  id: string;
  machineCode: string;
  orderReference?: string;
  observedSummary: string;
  predictedSummary: string;
  horizon: string;
  confidenceBand: string;
  recommendedAction: string;
  drillDownHref: string;
}>;

export type ExecutiveApprovalItem = Readonly<{
  id: string;
  caseId: string;
  decision: string;
  businessOutcome: string;
  expectedBenefit: string;
  tradeoff: string;
  requester: string;
  urgency: string;
  drillDownHref: string;
  recoveryContext?: string;
}>;

export type ExecutiveDigitalWorkforceStatus = Readonly<{
  activeWorkers: number;
  activeCases: number;
  waitingApproval: number;
  blocked: number;
  failed: number;
  verifiedOutcomes: number;
  drillDownHref: string;
  /** False when the deferred Digital Workforce module is off: the panel is not rendered at all. */
  available: boolean;
}>;

export type ExecutiveVerifiedValueItem = Readonly<{
  id: string;
  outcomeLabel: string;
  orderReference: string;
  summary: string;
  verifiedAtLabel: string;
  drillDownHref: string;
}>;

export type ExecutiveOperationalExposure = Readonly<{
  id: string;
  label: string;
  value: string;
  detail: string;
  drillDownHref: string;
}>;

export type ExecutiveTimelineEntry = Readonly<{
  id: string;
  timeLabel: string;
  occurredAtUtc: string;
  label: string;
  description: string;
  drillDownHref?: string;
}>;

export type ExecutiveThroughputCaseItem = Readonly<{
  id: string;
  lineCode: string;
  lineId: string;
  currentPhase: string;
  caseStatus: string;
  constraintLabel: string;
  selectedIntervention?: string;
  approvalState: string;
  observationState: string;
  verifiedGainPerHour?: number;
  verificationOutcome?: string;
  drillDownHref: string;
}>;

export type ExecutiveThroughputSummary = Readonly<{
  activeCaseCount: number;
  pendingApprovalCount: number;
  verifiedImprovementTodayCount: number;
  activeCases: readonly ExecutiveThroughputCaseItem[];
  verifiedToday: readonly ExecutiveThroughputCaseItem[];
  drillDownHref: string;
}>;

export type ExecutiveCommandCenterWorkspacePayload = Readonly<{
  generatedAtUtc: string;
  narrative: string;
  outcomeCards: readonly ExecutiveOutcomeCard[];
  priorityStack: readonly ExecutivePriorityItem[];
  predictiveRisks: readonly ExecutivePredictiveRisk[];
  approvals: readonly ExecutiveApprovalItem[];
  digitalWorkforce: ExecutiveDigitalWorkforceStatus;
  verifiedValue: readonly ExecutiveVerifiedValueItem[];
  throughputOptimizer: ExecutiveThroughputSummary;
  operationalExposure: readonly ExecutiveOperationalExposure[];
  timeline: readonly ExecutiveTimelineEntry[];
  scopeState: ExecutiveScopeState;
  unavailableSources: readonly string[];
  filters: ExecutiveCommandCenterFilters;
}>;

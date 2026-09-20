export type WorksListItem = Readonly<{
  id: string;
  title: string;
  problemSummary: string;
  displayState: string;
  priorityLabel: string;
  workTypeLabel: string;
  factoryContextLabel: string;
  ownerLabel: string;
  actingSystemLabel: string;
  slaLabel: string | null;
  hasPendingApproval: boolean;
  isBlocked: boolean;
  blockerLabel: string | null;
  expectedImpact: string | null;
  lastUpdateLabel: string;
  updatedAtUtc: string;
  status: string;
  verificationState: string;
  outcomeLabel: string;
  severity: string;
  primaryDomain: string;
  concurrencyToken: number;
}>;

export type WorksQueueCounts = Readonly<{
  assigned: number;
  team: number;
  pendingApproval: number;
  critical: number;
  blocked: number;
  inProgress: number;
  observing: number;
  verificationPending: number;
  completedToday: number;
}>;

export type WorksWorkspaceSnapshot = Readonly<{
  queueCounts: WorksQueueCounts;
  items: readonly WorksListItem[];
  activeQueue: string;
  totalCount: number;
}>;

export type WorksApprovalPanelItem = Readonly<{
  id: string;
  action: string;
  businessReason: string;
  impact: string;
  riskLevel: number;
  status: string;
  concurrencyToken: number;
}>;

export type WorksBlockerPanel = Readonly<{
  code: string;
  label: string;
  sinceUtc: string | null;
  detail: string;
}>;

export type WorksDependencyItem = Readonly<{
  label: string;
  relation: string;
  detail: string;
}>;

export type WorksRelatedWorkItem = Readonly<{
  id: string;
  title: string;
  relation: string;
  displayState: string;
}>;

export type WorksEvidenceItem = Readonly<{
  kind: string;
  label: string;
  detail: string;
  occurredAtUtc: string | null;
}>;

export type WorksScenarioContextPanel = Readonly<{
  runId: string;
  lineCode: string;
  currentPhase: string;
  selectedOptionLabel: string | null;
  verificationOutcome: string | null;
  baselineThroughput: number | null;
  predictedThroughput: number | null;
  actualThroughput: number | null;
  scenarioHref: string;
}>;

export type WorksTimelineEntry = Readonly<{
  code: string;
  label: string;
  occurredAtUtc: string;
  detail: string;
}>;

export type WorksVerificationSummary = Readonly<{
  outcomeLabel: string;
  beforeValue: number | null;
  afterValue: number | null;
  verificationState: string;
  verifiedAtUtc: string | null;
  verifiedByLabel: string | null;
}>;

export type WorksCaseDetailSnapshot = Readonly<{
  summary: WorksListItem;
  description: string;
  impactSummary: string;
  nextStepLabel: string;
  activeActionLabel: string | null;
  pendingApproval: WorksApprovalPanelItem | null;
  blocker: WorksBlockerPanel | null;
  dependencies: readonly WorksDependencyItem[];
  relatedWork: readonly WorksRelatedWorkItem[];
  timeline: readonly WorksTimelineEntry[];
  verification: WorksVerificationSummary | null;
  scenarioContext: WorksScenarioContextPanel | null;
  evidence: readonly WorksEvidenceItem[];
  permittedActions: readonly string[];
  factoryHref: string | null;
  planHref: string | null;
  scenarioHref: string | null;
  digitalWorkforceHref: string | null;
}>;

export type WorksWorkspacePagePayload = Readonly<{
  workspace: WorksWorkspaceSnapshot;
  caseDetail: WorksCaseDetailSnapshot | null;
  canDecideApproval: boolean;
  initialQueue: string;
  selectedCaseId: string | null;
  search: string;
}>;

export const DIGITAL_WORKFORCE_ROUTE = "/dijital-is-gucu";

export type DigitalAutonomyMode = "observe" | "draft" | "limited" | "full";

export type DigitalWorkerRuntimeStatus =
  | "working"
  | "observing"
  | "awaiting-approval"
  | "paused"
  | "needs-intervention"
  | "idle"
  | "human-controlled";

export type DigitalWorkCaseStatus =
  | "detected"
  | "analyzing"
  | "planning"
  | "awaiting-approval"
  | "acting"
  | "verifying"
  | "closed"
  | "escalation";

export type DigitalWorkStage =
  | "detect"
  | "analyze"
  | "decide"
  | "plan"
  | "approve"
  | "act"
  | "verify"
  | "close";

export type DigitalApprovalPolicy = "always-ask" | "ask-above-threshold" | "auto-allowed" | "never-allowed";

export type DigitalEvidenceKind =
  | "erp"
  | "iot"
  | "maintenance"
  | "quality"
  | "approval"
  | "file"
  | "operation";

export type DigitalDataProvenance = "demo" | "simulated" | "real";

export type DigitalWorkerId =
  | "dw-fact-001"
  | "dw-fact-002"
  | "dw-fact-003"
  | "dw-fact-004"
  | "dw-fact-005"
  | "dw-fact-006"
  | "dw-fact-007";

export type DigitalSystemAccess = Readonly<{
  label: string;
  allowed: boolean;
}>;

export type DigitalApprovalRule = Readonly<{
  action: string;
  policy: DigitalApprovalPolicy;
  thresholdLabel?: string;
}>;

export type DigitalPermissionEnvelope = Readonly<{
  systems: readonly DigitalSystemAccess[];
  facilities: readonly string[];
  sites: readonly string[];
  lines: readonly string[];
  machines: readonly string[];
  modules: readonly string[];
  integrations: readonly string[];
  readableData: readonly string[];
  creatableTasks: readonly string[];
  draftableItems: readonly string[];
  allowedActions: readonly string[];
  forbiddenActions: readonly string[];
  approvalRequiredActions: readonly string[];
  maxTransactionLabel: string;
  maxRiskLabel: string;
  externalCommunication: string;
  workingHours: string;
  escalationRule: string;
  machineSafety: Readonly<{
    plcWrite: "closed";
    machineCommand: "closed";
    safetyOverride: "prohibited";
  }>;
  approvalRules: readonly DigitalApprovalRule[];
}>;

export type DigitalWorkerKpis = Readonly<{
  handledToday: number;
  completedToday: number;
  completedThisWeek: number;
  completedThisMonth: number;
  verifiedClosed: number;
  escalations: number;
  pendingApprovals: number;
  averageClosureLabel: string;
  successRateLabel: string;
  estimatedTimeSavedLabel: string;
  aiCostTodayLabel: string;
  aiCostMonthLabel: string;
  aiCostPerCaseLabel: string;
  monthlyBudgetLabel: string;
  budgetState: "normal" | "near-limit" | "limit-reached";
  runningSinceLabel: string;
}>;

export type DigitalWorkerDefinition = Readonly<{
  id: DigitalWorkerId;
  code: string;
  name: string;
  mission: string;
  isCoordinator: boolean;
  defaultAutonomy: DigitalAutonomyMode;
  defaultStatus: DigitalWorkerRuntimeStatus;
  facilityLabel: string;
  currentAssignment: string | null;
  currentStage: DigitalWorkStage | null;
  nextAction: string;
  startedAtLabel: string | null;
  approvalRequiredNow: boolean;
  queueCount: number;
  pendingApprovalCount: number;
  lastOutcome: string;
  lastOutcomeVerified: boolean;
  todayCompleted: number;
  aiCostTodayLabel: string;
  kpis: DigitalWorkerKpis;
  envelope: DigitalPermissionEnvelope;
}>;

export type DigitalWorkCase = Readonly<{
  id: string;
  title: string;
  description: string;
  source: string;
  severity: "critical" | "high" | "medium";
  status: DigitalWorkCaseStatus;
  currentStage: DigitalWorkStage;
  primaryWorkerId: DigitalWorkerId;
  participatingWorkerIds: readonly DigitalWorkerId[];
  requiresApproval: boolean;
  startedAtLabel: string;
  targetAtLabel: string;
  closedAtLabel: string | null;
  verifiedAtLabel: string | null;
  outcome: string | null;
  businessImpact: string;
  costLabel: string;
  evidenceCount: number;
  verified: boolean;
  provenance: DigitalDataProvenance;
  handoffs: readonly string[];
  evidence: readonly Readonly<{ kind: DigitalEvidenceKind; label: string }>[];
  lifecycleCompleted: readonly DigitalWorkStage[];
  problem: string;
  actions: readonly string[];
  verification: readonly string[];
  result: string | null;
  escalation?: Readonly<{
    reason: string;
    risk: string;
    tried: string;
    decisionNeeded: string;
    recommended: string;
  }>;
}>;

export type DigitalActivityEvent = Readonly<{
  id: string;
  timeLabel: string;
  workerId: DigitalWorkerId;
  message: string;
}>;

export type DigitalApprovalRequest = Readonly<{
  id: string;
  workerId: DigitalWorkerId;
  action: string;
  reason: string;
  impact: string;
  risk: string;
  evidence: string;
  deadlineLabel: string;
  confidenceLabel: string;
  amountLabel?: string;
  provenance: DigitalDataProvenance;
  caseId: string;
}>;

export type DigitalWorkforceKpis = Readonly<{
  activeWorkers: number;
  activeCases: number;
  pendingApprovals: number;
  completedToday: number;
  verifiedClosures: number;
  recoveredProductionRisk: string;
  estimatedTimeSaved: string;
  aiCostToday: string;
}>;

export type DigitalCostControl = Readonly<{
  todayLabel: string;
  monthEstimateLabel: string;
  monthlyBudgetLabel: string;
  budgetUsageLabel: string;
  provenance: DigitalDataProvenance;
}>;

export const AI_TASK_CATEGORIES = [
  "production-planning",
  "live-production",
  "maintenance",
  "quality",
  "material-risk",
  "supply-risk",
  "commercial-intelligence",
  "energy",
  "capacity",
  "delivery-risk",
  "cost-deviation",
  "erp-integration",
] as const;

export type AiTaskCategory =
  (typeof AI_TASK_CATEGORIES)[number];

export const AI_TASK_PRIORITIES = [
  "low",
  "medium",
  "high",
  "critical",
] as const;

export type AiTaskPriority =
  (typeof AI_TASK_PRIORITIES)[number];

export const AI_TASK_STATUSES = [
  "draft",
  "awaiting-review",
  "awaiting-approval",
  "approved",
  "in-progress",
  "completed",
  "rejected",
  "deferred",
  "failed",
  "cancelled",
] as const;

export type AiTaskStatus =
  (typeof AI_TASK_STATUSES)[number];

export const AI_TASK_RISK_LEVELS = [
  "information",
  "low",
  "medium",
  "high",
  "critical",
] as const;

export type AiTaskRiskLevel =
  (typeof AI_TASK_RISK_LEVELS)[number];

export const AI_TASK_APPROVAL_STATES = [
  "not-required",
  "required",
  "pending",
  "approved",
  "rejected",
] as const;

export type AiTaskApprovalState =
  (typeof AI_TASK_APPROVAL_STATES)[number];

export const AI_TASK_ACTION_TYPES = [
  "review",
  "create-draft",
  "update-plan",
  "assign-user",
  "create-maintenance-request",
  "create-quality-record",
  "create-purchase-request",
  "create-transfer-request",
  "send-notification",
  "sync-to-erp",
] as const;

export type AiTaskActionType =
  (typeof AI_TASK_ACTION_TYPES)[number];

export type AiTaskId = string;
export type UserId = string;

export type AiTaskConfidence = Readonly<{
  score: number;
  explanation: string;
  modelVersion?: string;
}>;

export type AiTaskImpact = Readonly<{
  financialAmount?: number;
  currency?: string;
  productionQuantity?: number;
  productionUnit?: string;
  estimatedDelayMinutes?: number;
  estimatedDowntimeMinutes?: number;
  estimatedSavingsAmount?: number;
  summary: string;
}>;

export type AiTaskSourceReference = Readonly<{
  sourceType:
    | "erp"
    | "machine"
    | "iot"
    | "quality"
    | "maintenance"
    | "energy"
    | "manual"
    | "calculated";
  sourceId?: string;
  sourceLabel: string;
  observedAt?: string;
  valueSummary?: string;
}>;

export type AiTaskRecommendation = Readonly<{
  title: string;
  description: string;
  actionType: AiTaskActionType;
  requiresApproval: boolean;
  reversible: boolean;
  targetSystem?:
    | "crevafactos"
    | "erp"
    | "mes"
    | "maintenance"
    | "quality"
    | "notification";
}>;

export type AiTaskApproval = Readonly<{
  state: AiTaskApprovalState;
  requestedAt?: string;
  requestedBy?: UserId;
  decidedAt?: string;
  decidedBy?: UserId;
  decisionNote?: string;
}>;

export type AiTaskAssignee = Readonly<{
  userId: UserId;
  displayName: string;
  role?: string;
}>;

export const AI_TASK_SLA_STATES = [
  "on-track",
  "due-soon",
  "overdue",
  "breached",
  "completed-within-sla",
  "completed-after-sla",
  "not-applicable",
] as const;

export type AiTaskSlaState = (typeof AI_TASK_SLA_STATES)[number];

export const AI_TASK_ESCALATION_STATES = [
  "none",
  "active",
  "resolved",
] as const;

export type AiTaskEscalationState =
  (typeof AI_TASK_ESCALATION_STATES)[number];

export const AI_TASK_ESCALATION_BREACH_TYPES = [
  "acknowledgment",
  "response",
  "resolution",
  "critical-approaching-due",
  "manual",
] as const;

export type AiTaskEscalationBreachType =
  (typeof AI_TASK_ESCALATION_BREACH_TYPES)[number];

export type AiTaskSla = Readonly<{
  state: AiTaskSlaState;
  acknowledgmentDueAt?: string;
  responseDueAt?: string;
  resolutionDueAt?: string;
  isOverdue: boolean;
  breachType?: AiTaskEscalationBreachType;
}>;

export type AiTaskAuditEventType =
  | "created"
  | "analysis-updated"
  | "routed"
  | "assigned"
  | "reassigned"
  | "acknowledged"
  | "priority-changed"
  | "due-date-changed"
  | "sla-warning"
  | "escalated"
  | "approval-requested"
  | "approved"
  | "rejected"
  | "deferred"
  | "execution-started"
  | "execution-completed"
  | "execution-failed"
  | "cancelled"
  | "status-changed"
  | "teams-notification-emitted";

export type AiTaskAuditEvent = Readonly<{
  id: string;
  taskId: AiTaskId;
  eventType: AiTaskAuditEventType;
  occurredAt: string;
  actorType: "ai" | "user" | "system";
  actorId?: string;
  actorLabel: string;
  note?: string;
  previousStatus?: AiTaskStatus;
  nextStatus?: AiTaskStatus;
  metadataJson?: string;
}>;

export type AiTaskExecution = Readonly<{
  startedAt?: string;
  completedAt?: string;
  targetSystem?: string;
  externalReferenceId?: string;
  previousValue?: string;
  appliedValue?: string;
  errorCode?: string;
  errorMessage?: string;
}>;

export type AiTask = Readonly<{
  id: AiTaskId;
  title: string;
  summary: string;
  category: AiTaskCategory;
  priority: AiTaskPriority;
  riskLevel: AiTaskRiskLevel;
  status: AiTaskStatus;

  siteId?: string;
  productionLineId?: string;
  machineId?: string;
  moldId?: string;
  productionOrderId?: string;
  salesOrderId?: string;
  materialId?: string;
  supplierId?: string;
  customerId?: string;

  detectedIssue: string;
  reasoning: string;
  recommendation: AiTaskRecommendation;
  confidence: AiTaskConfidence;
  impact: AiTaskImpact;
  sources: readonly AiTaskSourceReference[];

  assignee?: AiTaskAssignee;
  assignedMembershipId?: string;
  assignedTeamChannelId?: string;
  acknowledgedAtUtc?: string;
  escalationLevel: number;
  escalationState: AiTaskEscalationState;
  slaState: AiTaskSlaState;
  sla?: AiTaskSla;
  assignmentReason?: string;
  assignmentSource?: string;
  approval: AiTaskApproval;
  execution?: AiTaskExecution;

  dueAt?: string;
  createdAt: string;
  updatedAt: string;
  completedAt?: string;

  auditTrail: readonly AiTaskAuditEvent[];
}>;

export type CreateAiTaskInput = Readonly<{
  title: string;
  summary: string;
  category: AiTaskCategory;
  priority: AiTaskPriority;
  riskLevel: AiTaskRiskLevel;

  detectedIssue: string;
  reasoning: string;
  recommendation: AiTaskRecommendation;
  confidence: AiTaskConfidence;
  impact: AiTaskImpact;
  sources: readonly AiTaskSourceReference[];

  siteId?: string;
  productionLineId?: string;
  machineId?: string;
  moldId?: string;
  productionOrderId?: string;
  salesOrderId?: string;
  materialId?: string;
  supplierId?: string;
  customerId?: string;

  dueAt?: string;
}>;

export type UpdateAiTaskStatusInput = Readonly<{
  taskId: AiTaskId;
  nextStatus: AiTaskStatus;
  actorId: UserId;
  note?: string;
}>;

export type ApproveAiTaskInput = Readonly<{
  taskId: AiTaskId;
  approvedBy: UserId;
  decisionNote?: string;
}>;

export type RejectAiTaskInput = Readonly<{
  taskId: AiTaskId;
  rejectedBy: UserId;
  decisionNote: string;
}>;

export function isAiTaskFinalStatus(
  status: AiTaskStatus,
): boolean {
  return [
    "completed",
    "rejected",
    "failed",
    "cancelled",
  ].includes(status);
}

export function doesAiTaskRequireHumanApproval(
  task: Pick<AiTask, "recommendation" | "approval">,
): boolean {
  return (
    task.recommendation.requiresApproval ||
    task.approval.state === "required" ||
    task.approval.state === "pending"
  );
}
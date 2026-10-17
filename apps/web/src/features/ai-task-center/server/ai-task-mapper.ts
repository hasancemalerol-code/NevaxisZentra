import type {
  AiTask,
  AiTaskActionType,
  AiTaskApprovalState,
  AiTaskAuditEvent,
  AiTaskAuditEventType,
  AiTaskCategory,
  AiTaskClosure,
  AiTaskClosurePolicy,
  AiTaskClosureState,
  AiTaskClosureValidation,
  AiTaskEvidence,
  AiTaskEvidenceType,
  AiTaskEscalationBreachType,
  AiTaskEscalationState,
  AiTaskPriority,
  AiTaskResolutionCode,
  AiTaskRiskLevel,
  AiTaskSla,
  AiTaskSlaState,
  AiTaskSourceReference,
  AiTaskStatus,
} from "../domain/ai-task";
import type {
  RawAiTaskClosure,
  RawAiTaskClosurePolicy,
  RawAiTaskClosureValidation,
  RawAiTaskDetail,
  RawAiTaskEvidence,
  RawAiTaskSummary,
  RawAiTaskSla,
} from "./ai-task-api-client";

export type AiTaskListRecord = AiTask &
  Readonly<{
    sourceModule: string | null;
    sourceEntityId: string | null;
    listOriginKind: string;
    version: number;
  }>;

function asCategory(value: string): AiTaskCategory {
  return value as AiTaskCategory;
}

function asPriority(value: string): AiTaskPriority {
  return value as AiTaskPriority;
}

function asRiskLevel(value: string): AiTaskRiskLevel {
  return value as AiTaskRiskLevel;
}

function asStatus(value: string): AiTaskStatus {
  return value as AiTaskStatus;
}

function asApprovalState(value: string): AiTaskApprovalState {
  return value as AiTaskApprovalState;
}

function asActionType(value: string): AiTaskActionType {
  return value as AiTaskActionType;
}

function asAuditEventType(value: string): AiTaskAuditEventType {
  return value as AiTaskAuditEventType;
}

function asSlaState(value: string): AiTaskSlaState {
  return value as AiTaskSlaState;
}

function asEscalationState(value: string): AiTaskEscalationState {
  return value as AiTaskEscalationState;
}

function asBreachType(value: string): AiTaskEscalationBreachType {
  return value as AiTaskEscalationBreachType;
}

function asClosureState(value: string): AiTaskClosureState {
  return value as AiTaskClosureState;
}

function asEvidenceType(value: string): AiTaskEvidenceType {
  return value as AiTaskEvidenceType;
}

function asResolutionCode(value: string): AiTaskResolutionCode {
  return value as AiTaskResolutionCode;
}

function mapClosure(raw: RawAiTaskClosure | undefined): AiTaskClosure | undefined {
  if (!raw) {
    return undefined;
  }

  return {
    state: asClosureState(raw.state),
    resolutionSummary: raw.resolutionSummary ?? undefined,
    resolutionCode: raw.resolutionCode ? asResolutionCode(raw.resolutionCode) : undefined,
    closureReason: raw.closureReason ?? undefined,
    approvalRequired: raw.approvalRequired,
    requestedAt: raw.requestedAtUtc ?? undefined,
    requestedByMembershipId: raw.requestedByMembershipId ?? undefined,
    approvedAt: raw.approvedAtUtc ?? undefined,
    approvedByMembershipId: raw.approvedByMembershipId ?? undefined,
    rejectedAt: raw.rejectedAtUtc ?? undefined,
    rejectedByMembershipId: raw.rejectedByMembershipId ?? undefined,
    rejectionReason: raw.rejectionReason ?? undefined,
    reopenedAt: raw.reopenedAtUtc ?? undefined,
    reopenedByMembershipId: raw.reopenedByMembershipId ?? undefined,
    reopenReason: raw.reopenReason ?? undefined,
    finalClosedAt: raw.finalClosedAtUtc ?? undefined,
  };
}

function mapClosurePolicy(
  raw: RawAiTaskClosurePolicy | undefined,
): AiTaskClosurePolicy | undefined {
  if (!raw) {
    return undefined;
  }

  return {
    policyKey: raw.policyKey,
    name: raw.name,
    minimumEvidenceCount: raw.minimumEvidenceCount,
    requiredEvidenceTypes: raw.requiredEvidenceTypes ?? [],
    approvalRequired: raw.approvalRequired,
    fourEyesRequired: raw.fourEyesRequired,
    resolutionSummaryRequired: raw.resolutionSummaryRequired,
  };
}

function mapClosureValidation(
  raw: RawAiTaskClosureValidation | undefined,
): AiTaskClosureValidation | undefined {
  if (!raw) {
    return undefined;
  }

  return {
    isValid: raw.isValid,
    issues: (raw.issues ?? []).map((issue) => ({
      code: issue.code,
      message: issue.message,
    })),
  };
}

function mapEvidenceItems(
  items: readonly RawAiTaskEvidence[] | undefined,
): readonly AiTaskEvidence[] {
  return (items ?? []).map((item) => ({
    id: item.id,
    taskId: item.taskId,
    evidenceType: asEvidenceType(item.evidenceType),
    title: item.title,
    description: item.description ?? undefined,
    referenceMetadataJson: item.referenceMetadataJson ?? undefined,
    sourceModule: item.sourceModule ?? undefined,
    sourceEntityId: item.sourceEntityId ?? undefined,
    submittedByMembershipId: item.submittedByMembershipId,
    submittedByLabel: item.submittedByLabel,
    submittedAt: item.submittedAtUtc,
  }));
}

/** Single place that decides a task's SLA state from a raw API payload (list summary or detail). */
export function resolveRawSlaState(
  raw: Readonly<{ slaState?: string | null; sla?: { state: string } | null }>,
): string {
  return raw.slaState ?? raw.sla?.state ?? "not-applicable";
}

function mapSla(raw: RawAiTaskSla | undefined, fallbackState: string): AiTaskSla {
  const state = asSlaState(raw?.state ?? fallbackState);
  return {
    state,
    acknowledgmentDueAt: raw?.acknowledgmentDueAtUtc ?? undefined,
    responseDueAt: raw?.responseDueAtUtc ?? undefined,
    resolutionDueAt: raw?.resolutionDueAtUtc ?? undefined,
    isOverdue: raw?.isOverdue ?? false,
    breachType: raw?.breachType ? asBreachType(raw.breachType) : undefined,
  };
}

function mapOwnershipFields(raw: Readonly<{
  assignee: RawAiTaskDetail["assignee"];
  assignedMembershipId: string | null;
  assignedTeamChannelId: string | null;
  acknowledgedAtUtc: string | null;
  escalationLevel: number;
  escalationState: string;
  slaState: string;
}>) {
  return {
    assignee: raw.assignee
      ? {
          userId: raw.assignee.userId,
          displayName: raw.assignee.displayName,
          role: raw.assignee.role ?? undefined,
        }
      : undefined,
    assignedMembershipId: raw.assignedMembershipId ?? undefined,
    assignedTeamChannelId: raw.assignedTeamChannelId ?? undefined,
    acknowledgedAtUtc: raw.acknowledgedAtUtc ?? undefined,
    escalationLevel: raw.escalationLevel ?? 0,
    escalationState: asEscalationState(raw.escalationState ?? "none"),
    slaState: asSlaState(raw.slaState ?? "not-applicable"),
  };
}

function asSourceType(value: string): AiTaskSourceReference["sourceType"] {
  return value as AiTaskSourceReference["sourceType"];
}

function mapSources(
  sources: RawAiTaskDetail["sources"],
): readonly AiTaskSourceReference[] {
  return sources.map((source) => ({
    sourceType: asSourceType(source.sourceType),
    sourceId: source.sourceId ?? undefined,
    sourceLabel: source.sourceLabel,
    observedAt: source.observedAt ?? undefined,
    valueSummary: source.valueSummary ?? undefined,
  }));
}

function mapAuditTrail(
  auditTrail: RawAiTaskDetail["auditTrail"],
): readonly AiTaskAuditEvent[] {
  return auditTrail.map((event) => ({
    id: event.id,
    taskId: event.taskId,
    eventType: asAuditEventType(event.eventType),
    occurredAt: event.occurredAt,
    actorType: event.actorType as AiTaskAuditEvent["actorType"],
    actorId: event.actorId ?? undefined,
    actorLabel: event.actorLabel,
    note: event.note ?? undefined,
    previousStatus: event.previousStatus ? asStatus(event.previousStatus) : undefined,
    nextStatus: event.nextStatus ? asStatus(event.nextStatus) : undefined,
    metadataJson: event.metadataJson ?? undefined,
  }));
}

export function mapRawAiTaskDetail(raw: RawAiTaskDetail): AiTask {
  return {
    id: raw.id,
    title: raw.title,
    summary: raw.summary,
    category: asCategory(raw.category),
    priority: asPriority(raw.priority),
    riskLevel: asRiskLevel(raw.riskLevel),
    status: asStatus(raw.status),
    siteId: raw.siteId ?? undefined,
    productionLineId: raw.productionLineId ?? undefined,
    machineId: raw.machineId ?? undefined,
    moldId: raw.moldId ?? undefined,
    productionOrderId: raw.productionOrderId ?? undefined,
    salesOrderId: raw.salesOrderId ?? undefined,
    materialId: raw.materialId ?? undefined,
    supplierId: raw.supplierId ?? undefined,
    customerId: raw.customerId ?? undefined,
    detectedIssue: raw.detectedIssue,
    reasoning: raw.reasoning,
    recommendation: {
      title: raw.recommendation.title,
      description: raw.recommendation.description,
      actionType: asActionType(raw.recommendation.actionType),
      requiresApproval: raw.recommendation.requiresApproval,
      reversible: raw.recommendation.reversible,
      targetSystem: raw.recommendation.targetSystem as AiTask["recommendation"]["targetSystem"],
    },
    confidence: {
      score: raw.confidence.score,
      explanation: raw.confidence.explanation,
      modelVersion: raw.confidence.modelVersion ?? undefined,
    },
    impact: {
      financialAmount: raw.impact.financialAmount ?? undefined,
      currency: raw.impact.currency ?? undefined,
      productionQuantity: raw.impact.productionQuantity ?? undefined,
      productionUnit: raw.impact.productionUnit ?? undefined,
      estimatedDelayMinutes: raw.impact.estimatedDelayMinutes ?? undefined,
      estimatedDowntimeMinutes: raw.impact.estimatedDowntimeMinutes ?? undefined,
      estimatedSavingsAmount: raw.impact.estimatedSavingsAmount ?? undefined,
      summary: raw.impact.summary,
    },
    sources: mapSources(raw.sources),
    // The detail payload carries the derived SLA only as `sla` (no top-level `slaState`); reading the missing
    // top-level field made the detail page show "Uygulanamaz" while the list showed the real derived state.
    ...mapOwnershipFields({ ...raw, slaState: resolveRawSlaState(raw) }),
    sla: mapSla(raw.sla, resolveRawSlaState(raw)),
    assignmentReason: raw.assignmentReason ?? undefined,
    assignmentSource: raw.assignmentSource ?? undefined,
    approval: {
      state: asApprovalState(raw.approval.state),
      requestedAt: raw.approval.requestedAt ?? undefined,
      requestedBy: raw.approval.requestedBy ?? undefined,
      decidedAt: raw.approval.decidedAt ?? undefined,
      decidedBy: raw.approval.decidedBy ?? undefined,
      decisionNote: raw.approval.decisionNote ?? undefined,
    },
    execution: raw.execution
      ? {
          startedAt: raw.execution.startedAt ?? undefined,
          completedAt: raw.execution.completedAt ?? undefined,
          targetSystem: raw.execution.targetSystem ?? undefined,
          externalReferenceId: raw.execution.externalReferenceId ?? undefined,
          previousValue: raw.execution.previousValue ?? undefined,
          appliedValue: raw.execution.appliedValue ?? undefined,
          errorCode: raw.execution.errorCode ?? undefined,
          errorMessage: raw.execution.errorMessage ?? undefined,
        }
      : undefined,
    dueAt: raw.dueAt ?? undefined,
    createdAt: raw.createdAt,
    updatedAt: raw.updatedAt,
    completedAt: raw.completedAt ?? undefined,
    auditTrail: mapAuditTrail(raw.auditTrail),
    closure: mapClosure(raw.closure),
    closurePolicy: mapClosurePolicy(raw.closurePolicy),
    closureValidation: mapClosureValidation(raw.closureValidation),
    evidence: mapEvidenceItems(raw.evidence),
  };
}

export function mapRawAiTaskSummary(raw: RawAiTaskSummary): AiTaskListRecord {
  const ownership = mapOwnershipFields({
    assignee: raw.assignee ?? null,
    assignedMembershipId: raw.assignedMembershipId ?? null,
    assignedTeamChannelId: raw.assignedTeamChannelId ?? null,
    acknowledgedAtUtc: raw.acknowledgedAtUtc ?? null,
    escalationLevel: raw.escalationLevel ?? 0,
    escalationState: raw.escalationState ?? "none",
    slaState: raw.slaState ?? "not-applicable",
  });

  return {
    id: raw.id,
    title: raw.title,
    summary: raw.summary,
    category: asCategory(raw.category),
    priority: asPriority(raw.priority),
    riskLevel: asRiskLevel(raw.riskLevel),
    status: asStatus(raw.status),
    detectedIssue: raw.summary,
    reasoning: raw.summary,
    recommendation: {
      title: raw.title,
      description: raw.summary,
      actionType: "review",
      requiresApproval: raw.approval.state === "required" || raw.approval.state === "pending",
      reversible: true,
    },
    confidence: {
      score: 0,
      explanation: "",
    },
    impact: {
      summary: raw.summary,
    },
    sources: [],
    ...ownership,
    sla: mapSla(
      {
        state: ownership.slaState,
        acknowledgmentDueAtUtc: null,
        responseDueAtUtc: null,
        resolutionDueAtUtc: null,
        isOverdue: ownership.slaState === "overdue" || ownership.slaState === "breached",
        breachType: raw.slaBreachType ?? null,
      },
      ownership.slaState,
    ),
    approval: {
      state: asApprovalState(raw.approval.state),
    },
    createdAt: raw.createdAt,
    updatedAt: raw.updatedAt,
    dueAt: raw.dueAt ?? undefined,
    auditTrail: [],
    sourceModule: raw.sourceModule,
    sourceEntityId: raw.sourceEntityId,
    listOriginKind: raw.listOriginKind,
    version: raw.version,
    closureState: raw.closureState ? asClosureState(raw.closureState) : undefined,
    evidenceCount: raw.evidenceCount ?? 0,
    closureApprovalRequired: raw.closureApprovalRequired ?? false,
  };
}

export type AiTaskDetailViewModel = Readonly<{
  task: AiTask;
  version: number;
  taskReference: string;
  sourceModule: string | null;
  sourceEntityId: string | null;
  ruleSetVersion: string;
}>;

export function mapRawAiTaskDetailViewModel(raw: RawAiTaskDetail): AiTaskDetailViewModel {
  return {
    task: mapRawAiTaskDetail(raw),
    version: raw.version,
    taskReference: raw.taskReference,
    sourceModule: raw.sourceModule,
    sourceEntityId: raw.sourceEntityId,
    ruleSetVersion: raw.ruleSetVersion,
  };
}

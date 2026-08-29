export type ProductionFollowUpStatus =
  | "requires-review"
  | "acknowledged"
  | "task-created"
  | "resolved"
  | "dismissed";

export type ProductionFollowUpTaskPurpose =
  | "human-review-draft"
  | "data-review-draft";

export type ProductionFollowUpRow = Readonly<{
  id: string;
  productionOrderId: string;
  productionOrderReference: string;
  exceptionType: string;
  severity: string;
  exceptionStatus: string;
  message: string;
  raisedAtUtc: string;
  followUpStatus: ProductionFollowUpStatus;
  linkedAiTaskId?: string;
  version: number;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: ProductionFollowUpTaskPurpose;
  hasInsufficientEvidence: boolean;
  isBlockedOrDelayed: boolean;
}>;

export type ProductionFollowUpAuditEvent = Readonly<{
  eventType: string;
  payloadJson: string;
  actor: string;
  occurredAtUtc: string;
}>;

export type ProductionFollowUpDetail = Readonly<{
  id: string;
  productionOrderId: string;
  productionOrderReference: string;
  productionOrderTitle: string;
  productionOrderStatus: string;
  machineReference?: string;
  moldReference?: string;
  productReference?: string;
  exceptionType: string;
  severity: string;
  exceptionStatus: string;
  message: string;
  raisedAtUtc: string;
  raisedBySubjectId: string;
  followUpStatus: ProductionFollowUpStatus;
  linkedAiTaskId?: string;
  version: number;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: ProductionFollowUpTaskPurpose;
  hasInsufficientEvidence: boolean;
  evidenceSummary: string;
  auditEvents: readonly ProductionFollowUpAuditEvent[];
}>;

export type ProductionFollowUpWorkspacePayload = Readonly<{
  automationDisclaimer: string;
  kpis: Readonly<{
    requiresReviewCount: number;
    acknowledgedCount: number;
    taskLinkedCount: number;
    blockedOrDelayedCount: number;
    dataReviewCount: number;
  }>;
  capabilities: Readonly<{
    canRead: boolean;
    canManage: boolean;
    canCreateTask: boolean;
  }>;
  exceptions: readonly ProductionFollowUpRow[];
}>;

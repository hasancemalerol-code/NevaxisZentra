export type QualityNcrFollowUpStatus =
  | "requires-review"
  | "acknowledged"
  | "task-created"
  | "resolved"
  | "dismissed";

export type QualityNcrFollowUpTaskPurpose =
  | "human-review-draft"
  | "data-review-draft";

export type QualityNcrFollowUpRow = Readonly<{
  id: string;
  ncrNumber: string;
  title: string;
  severity: string;
  status: string;
  followUpStatus: QualityNcrFollowUpStatus;
  holdStatus?: string;
  dispositionStatus?: string;
  productReference?: string;
  productionOrderId?: string;
  productionOrderReference?: string;
  detectedAtUtc: string;
  linkedAiTaskId?: string;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: QualityNcrFollowUpTaskPurpose;
  hasInsufficientEvidence: boolean;
}>;

export type QualityNcrFollowUpAuditEvent = Readonly<{
  eventType: string;
  payloadJson: string;
  actor: string;
  occurredAtUtc: string;
}>;

export type QualityNcrFollowUpFinding = Readonly<{
  id: string;
  ncrNumber: string;
  title: string;
  description: string;
  category?: string;
  severity: string;
  status: string;
  holdStatus?: string;
  dispositionStatus?: string;
  dispositionType?: string;
  dispositionNote?: string;
  productionOrderId?: string;
  productionContext?: Readonly<{
    productionOrderId: string;
    referenceCode?: string;
    productReference?: string;
    status?: string;
  }>;
  qualityInspectionId?: string;
  qualityInspectionNumber?: string;
  qualityInspectionResultId?: string;
  productReference?: string;
  operationReference?: string;
  detectedAtUtc: string;
  version: number;
  followUpStatus: QualityNcrFollowUpStatus;
  linkedAiTaskId?: string;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: QualityNcrFollowUpTaskPurpose;
  hasInsufficientEvidence: boolean;
  evidenceSummary: string;
  auditEvents: readonly QualityNcrFollowUpAuditEvent[];
}>;

export type QualityNcrFollowUpWorkspacePayload = Readonly<{
  automationDisclaimer: string;
  kpis: Readonly<{
    requiresReviewCount: number;
    acknowledgedCount: number;
    taskLinkedCount: number;
    highPriorityCount: number;
    dataReviewCount: number;
  }>;
  capabilities: Readonly<{
    canRead: boolean;
    canManage: boolean;
    canCreateTask: boolean;
  }>;
  findings: readonly QualityNcrFollowUpRow[];
}>;

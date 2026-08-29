export type MaterialAvailabilityRiskStatus =
  | "sufficient"
  | "shortage"
  | "insufficient-data"
  | "source-conflict";

export type MaterialAvailabilityRiskFollowUpStatus =
  | "requires-review"
  | "acknowledged"
  | "task-created"
  | "resolved"
  | "dismissed";

export type MaterialAvailabilityRiskTaskPurpose =
  | "human-review-draft"
  | "data-review-draft";

export type MaterialAvailabilityRiskRow = Readonly<{
  id: string;
  materialId: string;
  materialCode: string;
  materialName: string;
  requirementProfileId?: string;
  productionOrderReference?: string;
  status: MaterialAvailabilityRiskStatus;
  requiredQuantityGrams?: number;
  erpAvailableQuantity?: number;
  erpReservedQuantity?: number;
  erpUnit?: string;
  availableAfterReservationQuantity?: number;
  shortageQuantity?: number;
  sourceFreshness?: string;
  evaluatedAtUtc: string;
  version: number;
  followUpStatus: MaterialAvailabilityRiskFollowUpStatus;
  linkedAiTaskId?: string;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: MaterialAvailabilityRiskTaskPurpose;
}>;

export type MaterialAvailabilityRiskDriver = Readonly<{
  code: string;
  label: string;
  value?: string;
  state: string;
}>;

export type MaterialAvailabilityRiskEvidence = Readonly<{
  field: string;
  value?: string;
  source?: string;
  recordedAtUtc?: string;
  isMissing: boolean;
}>;

export type MaterialAvailabilityRiskAuditEvent = Readonly<{
  eventType: string;
  payloadJson: string;
  actor: string;
  occurredAtUtc: string;
}>;

export type MaterialAvailabilityRiskDetail = Readonly<{
  id: string;
  facilityId: string;
  materialId: string;
  materialCode: string;
  materialName: string;
  requirementProfileId?: string;
  productionOrderReference?: string;
  productReference?: string;
  moldReference?: string;
  status: MaterialAvailabilityRiskStatus;
  ruleSetVersion: string;
  inputFingerprint: string;
  requiredQuantityGrams?: number;
  erpAvailableQuantity?: number;
  erpReservedQuantity?: number;
  erpUnit?: string;
  availableAfterReservationQuantity?: number;
  shortageQuantity?: number;
  erpSourceUpdatedAtUtc?: string;
  sourceFreshness?: string;
  drivers: readonly MaterialAvailabilityRiskDriver[];
  evidence: readonly MaterialAvailabilityRiskEvidence[];
  evaluatedAtUtc: string;
  createdAtUtc: string;
  version: number;
  followUpStatus: MaterialAvailabilityRiskFollowUpStatus;
  linkedAiTaskId?: string;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: MaterialAvailabilityRiskTaskPurpose;
  hasInsufficientEvidence: boolean;
  evidenceSummary: string;
  auditEvents: readonly MaterialAvailabilityRiskAuditEvent[];
}>;

export type MaterialAvailabilityRiskHistoryEntry = Readonly<{
  id: string;
  status: MaterialAvailabilityRiskStatus;
  evaluatedAtUtc: string;
  ruleSetVersion: string;
}>;

export type MaterialAvailabilityRiskWorkspacePayload = Readonly<{
  ruleSetVersion: string;
  automationDisclaimer: string;
  kpis: Readonly<{
    shortageCount: number;
    sufficientCount: number;
    insufficientDataCount: number;
    sourceConflictCount: number;
    requiresReviewCount: number;
    taskLinkedCount: number;
    dataReviewCount: number;
    latestEvaluatedAtUtc?: string;
  }>;
  capabilities: Readonly<{
    canRead: boolean;
    canManage: boolean;
    canCreateTask: boolean;
  }>;
  assessments: readonly MaterialAvailabilityRiskRow[];
}>;

export type MaterialAvailabilityRiskEvaluateResult = Readonly<{
  assessmentId: string;
  version: number;
  idempotentReplay: boolean;
}>;

export type MaterialAvailabilityRiskTaskDraftResult = Readonly<{
  taskId: string;
  assessmentId: string;
  idempotentReplay: boolean;
  assessment: MaterialAvailabilityRiskDetail;
}>;

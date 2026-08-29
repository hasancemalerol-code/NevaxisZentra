export type MoldFollowUpStatus =
  | "requires-review"
  | "acknowledged"
  | "task-created"
  | "resolved"
  | "dismissed";

export type MoldFollowUpTaskPurpose =
  | "human-review-draft"
  | "data-review-draft";

export type MoldFollowUpRow = Readonly<{
  id: string;
  code: string;
  name: string;
  lifecycleState: string;
  followUpStatus: MoldFollowUpStatus;
  currentShotCount?: number;
  nominalShotLife?: number;
  counterRecordedAtUtc?: string;
  primaryProductReference?: string;
  updatedAtUtc: string;
  linkedAiTaskId?: string;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: MoldFollowUpTaskPurpose;
  hasInsufficientEvidence: boolean;
}>;

export type MoldFollowUpLifecycleEvent = Readonly<{
  id: string;
  previousState: string;
  newState: string;
  reason: string;
  actorDisplayName: string;
  occurredAtUtc: string;
  sourceReference?: string;
}>;

export type MoldFollowUpAuditEvent = Readonly<{
  eventType: string;
  payloadJson: string;
  actor: string;
  occurredAtUtc: string;
}>;

export type MoldFollowUpDetail = Readonly<{
  id: string;
  code: string;
  name: string;
  description?: string;
  lifecycleState: string;
  ownershipType: string;
  locationType: string;
  locationLabel?: string;
  cavityCount?: number;
  nominalShotLife?: number;
  currentShotCount?: number;
  counterRecordedAtUtc?: string;
  counterSource?: string;
  primaryProductReference?: string;
  materialFamilyReference?: string;
  manufacturer?: string;
  updatedAtUtc: string;
  version: number;
  followUpStatus: MoldFollowUpStatus;
  linkedAiTaskId?: string;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: MoldFollowUpTaskPurpose;
  hasInsufficientEvidence: boolean;
  evidenceSummary: string;
  lifecycleEvents: readonly MoldFollowUpLifecycleEvent[];
  auditEvents: readonly MoldFollowUpAuditEvent[];
}>;

export type MoldFollowUpWorkspacePayload = Readonly<{
  automationDisclaimer: string;
  kpis: Readonly<{
    requiresReviewCount: number;
    acknowledgedCount: number;
    taskLinkedCount: number;
    maintenanceStateCount: number;
    dataReviewCount: number;
  }>;
  capabilities: Readonly<{
    canRead: boolean;
    canManage: boolean;
    canCreateTask: boolean;
  }>;
  molds: readonly MoldFollowUpRow[];
}>;

export type ProcurementDeliveryRiskResult =
  | "on-track"
  | "delivery-risk"
  | "overdue"
  | "insufficient-data"
  | "source-conflict";

export type ProcurementDeliveryRiskRow = Readonly<{
  id: string;
  purchaseOrderId: string;
  purchaseOrderLineId: string;
  purchaseOrderNumber: string;
  externalSupplierCode?: string;
  supplierName?: string;
  commitmentDateUtc?: string;
  receivedQuantity?: number;
  openQuantity?: number;
  unit?: string;
  result: ProcurementDeliveryRiskResult;
  sourceFreshness?: string;
  evaluatedAtUtc: string;
  followUpStatus?: string;
  linkedAiTaskId?: string;
  version: number;
}>;

export type ProcurementDeliveryRiskFollowUpAuditEvent = Readonly<{
  eventType: string;
  payloadJson: string;
  actor: string;
  occurredAtUtc: string;
}>;

export type ProcurementDeliveryRiskDriver = Readonly<{
  code: string;
  summary: string;
  reference?: string;
  state: string;
}>;

export type ProcurementDeliveryRiskEvidence = Readonly<{
  field: string;
  value?: string;
  source?: string;
  recordedAtUtc?: string;
  isUnknown: boolean;
}>;

export type ProcurementDeliveryRiskDetail = Readonly<{
  id: string;
  facilityId: string;
  purchaseOrderId: string;
  purchaseOrderLineId: string;
  purchaseOrderNumber: string;
  externalSupplierId?: string;
  externalSupplierCode?: string;
  supplierName?: string;
  orderStatus: string;
  lineNumber: number;
  externalProductId?: string;
  externalProductCode?: string;
  lineDescription?: string;
  commitmentDateUtc?: string;
  orderedQuantity?: number;
  receivedQuantity?: number;
  openQuantity?: number;
  unit?: string;
  result: ProcurementDeliveryRiskResult;
  ruleSetVersion: string;
  inputFingerprint: string;
  sourceUpdatedAtUtc?: string;
  sourceFreshness?: string;
  drivers: readonly ProcurementDeliveryRiskDriver[];
  evidence: readonly ProcurementDeliveryRiskEvidence[];
  evaluatedAtUtc: string;
  followUpStatus: string;
  linkedAiTaskId?: string;
  canCreateTaskDraft: boolean;
  taskDraftPurpose?: string;
  hasInsufficientEvidence: boolean;
  evidenceSummary?: string;
  auditEvents: readonly ProcurementDeliveryRiskFollowUpAuditEvent[];
  version: number;
}>;

export type ProcurementDeliveryRiskHistoryEntry = Readonly<{
  id: string;
  result: ProcurementDeliveryRiskResult;
  evaluatedAtUtc: string;
  ruleSetVersion: string;
}>;

export type ProcurementDeliveryRiskWorkspacePayload = Readonly<{
  ruleSetVersion: string;
  automationDisclaimer: string;
  kpis: Readonly<{
    overdueCount: number;
    deliveryRiskCount: number;
    onTrackCount: number;
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
  assessments: readonly ProcurementDeliveryRiskRow[];
}>;

export type ProcurementDeliveryRiskTaskDraftResult = Readonly<{
  taskId: string;
  assessmentId: string;
  idempotentReplay: boolean;
}>;

export type ProcurementDeliveryRiskEvaluateResult = Readonly<{
  assessmentId: string;
  version: number;
  idempotentReplay: boolean;
}>;

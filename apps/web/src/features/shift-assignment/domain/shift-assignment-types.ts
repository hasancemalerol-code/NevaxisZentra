export type ShiftAssignmentCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type ShiftAssignmentEligibilityOutcome =
  | "eligible"
  | "conditional"
  | "ineligible"
  | "insufficient-data";

export type ShiftAssignmentStatus = "draft" | "approved" | "cancelled";

export type ShiftAssignmentContextOption = Readonly<{
  value: string;
  label: string;
}>;

export type ShiftAssignmentContextOptions = Readonly<{
  productionOrders: readonly ShiftAssignmentContextOption[];
  machines: readonly ShiftAssignmentContextOption[];
  shifts: readonly ShiftAssignmentContextOption[];
  competencyCodes: readonly ShiftAssignmentContextOption[];
}>;

export type ShiftAssignmentSelectionContext = Readonly<{
  productionOrderReference: string;
  machineAssetId: string;
  productionShiftId: string;
  shiftDate: string;
  requiredCompetencyCode: string;
  machineFamilyScope?: string;
  processScope?: string;
}>;

export type ShiftAssignmentEligibilitySummary = Readonly<{
  eligibleCount: number | null;
  conditionalCount: number | null;
  ineligibleCount: number | null;
  insufficientDataCount: number | null;
}>;

export type ShiftAssignmentCandidateSummary = Readonly<{
  personId: string;
  eligibilityResultId: string;
  employeeCode: string;
  displayName: string;
  department?: string;
  jobTitle?: string;
  availabilityState: string;
  availabilityLabel: string;
  competencySummary: string;
  freshnessState: string;
  freshnessLabel: string;
  outcome: ShiftAssignmentEligibilityOutcome;
  outcomeLabel: string;
  primaryReasonCode?: string;
  primaryReasonLabel?: string;
  hasOverlapWarning: boolean;
  isPilotData: boolean;
}>;

export type ShiftAssignmentEligibilityReason = Readonly<{
  reasonCode: string;
  labelTr: string;
  severity: string;
  severityLabel: string;
  requiredValue?: string;
  observedValue?: string;
  evidenceSource: string;
  evidenceSourceLabel: string;
  evidenceTimestampUtc?: string;
  freshnessState?: string;
  freshnessLabel?: string;
}>;

export type ShiftAssignmentCandidateDetail = Readonly<{
  personId: string;
  employeeCode: string;
  displayName: string;
  department?: string;
  jobTitle?: string;
  availabilityState: string;
  availabilityLabel: string;
  competencySummary: string;
  freshnessState: string;
  freshnessLabel: string;
  outcome: ShiftAssignmentEligibilityOutcome;
  outcomeLabel: string;
  primaryReasonCode?: string;
  primaryReasonLabel?: string;
  hasOverlapWarning: boolean;
  isPilotData: boolean;
  sourceSystem: string;
  sourceLabel: string;
  sourceUpdatedAtUtc?: string;
  shiftLabel?: string;
  ruleSetVersion: string;
  assessmentId: string;
  reasons: readonly ShiftAssignmentEligibilityReason[];
}>;

export type ShiftAssignmentCandidatesResult = Readonly<{
  assessmentId: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  summary: ShiftAssignmentEligibilitySummary;
  candidates: readonly ShiftAssignmentCandidateSummary[];
  capabilities: ShiftAssignmentCapabilities;
}>;

export type ShiftAssignmentRecord = Readonly<{
  id: string;
  productionOrderReference: string;
  productionOrderLabel?: string;
  machineAssetLabel: string;
  shiftLabel: string;
  shiftDate: string;
  employeeCode: string;
  displayName: string;
  status: ShiftAssignmentStatus;
  statusLabel: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  approvedByDisplayName?: string;
  approvedAtUtc?: string;
  cancelledByDisplayName?: string;
  cancelledAtUtc?: string;
  ruleSetVersion: string;
  version: number;
  isPilotData: boolean;
}>;

export type ShiftAssignmentDetail = ShiftAssignmentRecord &
  Readonly<{
    eligibilityAssessmentId: string;
    eligibilityResultId: string;
    department?: string;
    jobTitle?: string;
    machineFamilyScope?: string;
    processScope?: string;
  }>;

export type ShiftAssignmentAuditEvent = Readonly<{
  id: string;
  eventType: string;
  eventTypeLabel: string;
  actorDisplayName?: string;
  occurredAtUtc: string;
  summary: string;
}>;

export type ShiftAssignmentListResult = Readonly<{
  items: readonly ShiftAssignmentRecord[];
  totalCount: number;
  page: number;
  pageSize: number;
  capabilities: ShiftAssignmentCapabilities;
}>;

export type ShiftAssignmentListFilters = Readonly<{
  productionOrderReference?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}>;

export type CreateShiftAssignmentDraftCommand = Readonly<{
  productionOrderReference: string;
  machineAssetId: string;
  productionShiftId: string;
  shiftDate: string;
  workforcePersonId: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  eligibilityAssessmentId: string;
  eligibilityResultId: string;
  requiredCompetencyCode: string;
  machineFamilyScope?: string;
  processScope?: string;
  idempotencyKey: string;
}>;

export type ApproveShiftAssignmentCommand = Readonly<{
  expectedVersion: number;
  idempotencyKey: string;
}>;

export type CancelShiftAssignmentCommand = Readonly<{
  expectedVersion: number;
  cancellationReason?: string;
}>;

export type ShiftAssignmentWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

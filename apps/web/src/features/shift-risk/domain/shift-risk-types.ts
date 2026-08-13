export type ShiftRiskCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type ShiftRiskOverview = Readonly<{
  evaluatedContextCount: number;
  clearCount: number;
  attentionCount: number;
  criticalCount: number;
  insufficientDataCount: number;
  coverageGapCount: number;
  criticalRoleGapCount: number;
  conflictCount: number;
  capabilities: ShiftRiskCapabilities;
}>;

export type ShiftRiskAssessmentSummary = Readonly<{
  id: string;
  coverageRequirementId?: string;
  productionShiftId: string;
  shiftLabel: string;
  shiftDate: string;
  productionOrderReference?: string;
  machineAssetId?: string;
  machineAssetLabel?: string;
  requiredCompetencyCode?: string;
  minimumRequiredCount: number | null;
  approvedEligibleCount: number;
  coverageGap: number;
  outcome: string;
  outcomeLabel: string;
  leadingDriverCategory?: string;
  leadingReasonCode?: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  isPilotData: boolean;
}>;

export type ShiftRiskDriver = Readonly<{
  id: string;
  reasonCode: string;
  labelTr: string;
  category: string;
  categoryLabel: string;
  severity: string;
  severityLabel: string;
  requiredValue?: string;
  observedValue?: string;
  gapValue?: string;
  evidenceSource: string;
  evidenceTimestampUtc?: string;
  freshnessState?: string;
  freshnessLabel?: string;
  ruleSetVersion: string;
  isLeadingRisk: boolean;
}>;

export type ShiftRiskAssignmentEvidence = Readonly<{
  assignmentId: string;
  workforcePersonId: string;
  employeeCode: string;
  displayName: string;
  status: string;
  statusLabel: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  exclusionReasonCode?: string;
  exclusionReasonLabel?: string;
  isPilotData: boolean;
}>;

export type ShiftRiskAssessmentDetail = Readonly<{
  id: string;
  coverageRequirementId?: string;
  productionShiftId: string;
  shiftLabel: string;
  shiftDate: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  productionOrderReference?: string;
  machineAssetId?: string;
  machineAssetLabel?: string;
  machineFamilyScope?: string;
  requiredCompetencyCode?: string;
  minimumRequiredCount: number | null;
  isCriticalRole: boolean | null;
  approvedEligibleCount: number;
  coverageGap: number;
  outcome: string;
  outcomeLabel: string;
  leadingDriverCategory?: string;
  leadingReasonCode?: string;
  leadingDriverId?: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  sourceFreshnessSnapshotJson?: string;
  drivers: readonly ShiftRiskDriver[];
  countedAssignments: readonly ShiftRiskAssignmentEvidence[];
  excludedAssignments: readonly ShiftRiskAssignmentEvidence[];
  isPilotData: boolean;
}>;

export type ShiftRiskAssessmentListFilters = Readonly<{
  productionShiftId?: string;
  productionOrderReference?: string;
  machineAssetId?: string;
  outcome?: string;
  driverCategory?: string;
  criticalOnly?: boolean;
  page?: number;
  pageSize?: number;
}>;

export type ShiftRiskAssessmentListResult = Readonly<{
  items: readonly ShiftRiskAssessmentSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  capabilities: ShiftRiskCapabilities;
}>;

export type EvaluateShiftRiskCommand = Readonly<{
  productionShiftId: string;
  shiftDate: string;
  productionOrderReference?: string;
  machineAssetId?: string;
  machineFamilyScope?: string;
  requiredCompetencyCode?: string;
  idempotencyKey: string;
}>;

export type ShiftRiskWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

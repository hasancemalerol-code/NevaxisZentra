export type ShiftContinuityCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type ShiftContinuityOverview = Readonly<{
  evaluatedContextCount: number;
  absenceImpactCount: number;
  competencyExpiredCount: number;
  competencyExpiringCount: number;
  staleOrUnknownSourceCount: number;
  assignmentConflictCount: number;
  coverageImpactCount: number;
  insufficientDataCount: number;
  capabilities: ShiftContinuityCapabilities;
}>;

export type ShiftContinuityAssessmentSummary = Readonly<{
  id: string;
  workforcePersonId: string;
  employeeCode: string;
  displayName: string;
  productionShiftId: string;
  shiftLabel: string;
  shiftDate: string;
  productionOrderReference?: string;
  machineAssetId?: string;
  machineAssetLabel?: string;
  requiredCompetencyCode?: string;
  availabilityState: string;
  availabilityLabel: string;
  competencyState: string;
  competencyLabel: string;
  freshnessState: string;
  freshnessLabel: string;
  approvedAssignmentCount: number;
  plannedAssignmentMinutes: number;
  outcome: string;
  outcomeLabel: string;
  leadingDriverCategory?: string;
  leadingReasonCode?: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  isCriticalContext: boolean;
  isPilotData: boolean;
}>;

export type ShiftContinuityDriver = Readonly<{
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

export type ShiftContinuityCompetencyEvidence = Readonly<{
  competencyCode: string;
  competencyLabel: string;
  verificationStatus: string;
  verificationStatusLabel: string;
  validFromUtc?: string;
  validUntilUtc?: string;
  requiresAttention: boolean;
}>;

export type ShiftContinuityAssignmentEvidence = Readonly<{
  assignmentId: string;
  status: string;
  statusLabel: string;
  shiftLabel: string;
  shiftDate: string;
  productionOrderReference?: string;
  machineAssetLabel?: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  isPilotData: boolean;
}>;

export type ShiftContinuityWorkloadFacts = Readonly<{
  approvedAssignmentCount: number;
  plannedAssignmentMinutes: number;
  authoritativeThresholdAvailable: boolean;
  thresholdValue?: string;
  conclusionRequiresThreshold: boolean;
}>;

export type ShiftContinuityCoverageImpact = Readonly<{
  requiredCount: number | null;
  trustworthyAvailableCount: number | null;
  coverageGap: number | null;
  isCriticalRole: boolean | null;
  requirementConfigured: boolean;
}>;

export type ShiftContinuityAssessmentDetail = Readonly<{
  id: string;
  workforcePersonId: string;
  employeeCode: string;
  displayName: string;
  department?: string;
  jobTitle?: string;
  isActive: boolean;
  productionShiftId: string;
  shiftLabel: string;
  shiftDate: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  productionOrderReference?: string;
  machineAssetId?: string;
  machineAssetLabel?: string;
  requiredCompetencyCode?: string;
  availabilityState: string;
  availabilityLabel: string;
  competencyState: string;
  competencyLabel: string;
  freshnessState: string;
  freshnessLabel: string;
  sourceSystem: string;
  sourceLabel: string;
  sourceShiftReference?: string;
  sourceCalendarReference?: string;
  sourceUpdatedAtUtc?: string;
  synchronizedAtUtc?: string;
  sourceLimitations: readonly string[];
  approvedAssignmentCount: number;
  plannedAssignmentMinutes: number;
  outcome: string;
  outcomeLabel: string;
  leadingDriverCategory?: string;
  leadingReasonCode?: string;
  leadingDriverId?: string;
  ruleSetVersion: string;
  evaluatedAtUtc: string;
  sourceFreshnessSnapshotJson?: string;
  workloadFacts: ShiftContinuityWorkloadFacts;
  coverageImpact: ShiftContinuityCoverageImpact;
  competencies: readonly ShiftContinuityCompetencyEvidence[];
  approvedAssignments: readonly ShiftContinuityAssignmentEvidence[];
  drivers: readonly ShiftContinuityDriver[];
  isPilotData: boolean;
}>;

export type ShiftContinuityAssessmentListFilters = Readonly<{
  productionShiftId?: string;
  shiftDate?: string;
  outcome?: string;
  driverCategory?: string;
  competencyCode?: string;
  availability?: string;
  freshness?: string;
  criticalOnly?: boolean;
  page?: number;
  pageSize?: number;
}>;

export type ShiftContinuityAssessmentListResult = Readonly<{
  items: readonly ShiftContinuityAssessmentSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  capabilities: ShiftContinuityCapabilities;
}>;

export type EvaluateShiftContinuityCommand = Readonly<{
  workforcePersonId: string;
  productionShiftId: string;
  shiftDate: string;
  productionOrderReference?: string;
  machineAssetId?: string;
  requiredCompetencyCode?: string;
  idempotencyKey: string;
}>;

export type ShiftContinuityWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

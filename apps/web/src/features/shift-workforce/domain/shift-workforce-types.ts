export type ShiftWorkforceCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type ShiftWorkforceOverview = Readonly<{
  totalWorkforce: number | null;
  activeWorkforce: number | null;
  currentlyAvailable: number | null;
  unavailableOrLeave: number | null;
  staleSourceRecords: number | null;
  competencyAttentionCount: number | null;
  capabilities: ShiftWorkforceCapabilities;
}>;

export type ShiftWorkforcePersonSummary = Readonly<{
  id: string;
  employeeCode: string;
  displayName: string;
  department?: string;
  jobTitle?: string;
  isActive: boolean;
  availabilityState: string;
  availabilityLabel: string;
  shiftLabel?: string;
  sourceSystem: string;
  sourceLabel: string;
  freshnessState: string;
  freshnessLabel: string;
  competencySummary: string;
  competencyAttention: boolean;
}>;

export type ShiftWorkforceCompetencyAssignment = Readonly<{
  id: string;
  competencyCode: string;
  competencyLabel: string;
  machineFamilyScope?: string;
  processScope?: string;
  materialScope?: string;
  equipmentScope?: string;
  competencyLevel: string;
  competencyLevelLabel: string;
  verificationStatus: string;
  verificationStatusLabel: string;
  validFromUtc?: string;
  validUntilUtc?: string;
  verifiedAtUtc?: string;
  operationalNote?: string;
  version: number;
  requiresAttention: boolean;
}>;

export type ShiftWorkforcePersonDetail = Readonly<{
  id: string;
  employeeCode: string;
  displayName: string;
  department?: string;
  jobTitle?: string;
  isActive: boolean;
  availabilityState: string;
  availabilityLabel: string;
  shiftLabel?: string;
  sourceSystem: string;
  sourceLabel: string;
  freshnessState: string;
  freshnessLabel: string;
  sourceShiftReference?: string;
  sourceCalendarReference?: string;
  sourceUpdatedAtUtc?: string;
  synchronizedAtUtc?: string;
  sourceFingerprint?: string;
  sourceLimitations: readonly string[];
  isPilotData: boolean;
  competencies: readonly ShiftWorkforceCompetencyAssignment[];
  version: number;
}>;

export type ShiftWorkforceCompetencyDefinition = Readonly<{
  code: string;
  displayLabelTr: string;
  descriptionTr?: string;
  isActive: boolean;
  sortOrder: number;
}>;

export type ShiftWorkforcePersonListResult = Readonly<{
  items: readonly ShiftWorkforcePersonSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}>;

export type ShiftWorkforceListFilters = Readonly<{
  search?: string;
  isActive?: string;
  availability?: string;
  competencyCode?: string;
  freshness?: string;
  page?: number;
  pageSize?: number;
}>;

export type CreateShiftWorkforceCompetencyCommand = Readonly<{
  competencyDefinitionCode: string;
  machineFamilyScope?: string;
  processScope?: string;
  materialScope?: string;
  equipmentScope?: string;
  competencyLevel: string;
  verificationStatus: string;
  validFromUtc?: string;
  validUntilUtc?: string;
  operationalNote?: string;
  idempotencyKey: string;
}>;

export type UpdateShiftWorkforceCompetencyCommand = Readonly<{
  competencyLevel?: string;
  verificationStatus?: string;
  validFromUtc?: string;
  validUntilUtc?: string;
  operationalNote?: string;
  expectedVersion: number;
}>;

export type ShiftWorkforceWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

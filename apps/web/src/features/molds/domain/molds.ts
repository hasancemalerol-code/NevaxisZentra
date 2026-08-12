export type MoldSummary = Readonly<{
  id: string;
  code: string;
  name: string;
  lifecycleState: string;
  ownershipType: string;
  locationType: string;
  locationLabel?: string;
  facilityId: string;
  cavityCount?: number;
  currentShotCount?: number;
  counterRecordedAtUtc?: string;
  sourceType: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MoldDetail = Readonly<{
  id: string;
  facilityId: string;
  code: string;
  name: string;
  description?: string;
  serialNumber?: string;
  manufacturer?: string;
  manufactureYear?: number;
  manufactureDate?: string;
  commissioningDate?: string;
  sourceType: string;
  externalSourceReference?: string;
  ownershipType: string;
  ownerReference?: string;
  locationType: string;
  locationLabel?: string;
  locationChangedAtUtc?: string;
  lifecycleState: string;
  moldType?: string;
  cavityCount?: number;
  nominalShotLife?: number;
  currentShotCount?: number;
  counterRecordedAtUtc?: string;
  counterSource?: string;
  counterSourceReference?: string;
  counterNote?: string;
  primaryProductReference?: string;
  materialFamilyReference?: string;
  runnerType?: string;
  hotRunnerPresent?: boolean;
  moldWeightKg?: number;
  dimensionLengthMm?: number;
  dimensionWidthMm?: number;
  dimensionHeightMm?: number;
  notes?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MoldLifecycleEvent = Readonly<{
  id: string;
  previousState: string;
  newState: string;
  reason: string;
  actorDisplayName: string;
  occurredAtUtc: string;
  sourceReference?: string;
  version: number;
}>;

export type MoldWorkspaceCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type MoldWorkspaceSummary = Readonly<{
  totalCount: number;
  draftCount: number;
  activeCount: number;
  maintenanceCount: number;
  trialCount: number;
  quarantinedCount: number;
  retiredCount: number;
  scrappedCount: number;
  capabilities: MoldWorkspaceCapabilities;
}>;

export type MoldWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

export type MoldMaintenanceRecord = Readonly<{
  id: string;
  moldId: string;
  maintenanceType: string;
  status: string;
  openedAtUtc: string;
  completedAtUtc?: string;
  summary: string;
  findings?: string;
  actionsTaken?: string;
  performedByDisplayName?: string;
  externalMaintenanceRequestReference?: string;
  externalWorkOrderReference?: string;
  sourceType: string;
  sourceReference?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MoldRevisionRecord = Readonly<{
  id: string;
  moldId: string;
  revisionNumber: string;
  revisionType: string;
  revisionDate: string;
  summary: string;
  reason?: string;
  technicalChange?: string;
  previousCavityCount?: number;
  newCavityCount?: number;
  performedByDisplayName?: string;
  externalReference?: string;
  sourceType: string;
  sourceReference?: string;
  createdAtUtc: string;
}>;

export type MoldTrialRecord = Readonly<{
  id: string;
  moldId: string;
  trialNumber: string;
  trialDate: string;
  result: string;
  machineReference?: string;
  productionOrderReference?: string;
  productReference?: string;
  materialReference?: string;
  cycleTimeSeconds?: number;
  shotCount?: number;
  goodPartCount?: number;
  scrapPartCount?: number;
  findings?: string;
  decision?: string;
  performedByDisplayName?: string;
  sourceType: string;
  sourceReference?: string;
  createdAtUtc: string;
}>;

export type MoldTimelineEntry = Readonly<{
  id: string;
  eventKind: string;
  eventDateUtc: string;
  createdAtUtc: string;
  title: string;
  summary: string;
  statusOrResult?: string;
  reference?: string;
  actorDisplayName?: string;
}>;

export type MoldHistorySummary = Readonly<{
  maintenanceCount: number;
  openMaintenanceCount: number;
  revisionCount: number;
  trialCount: number;
  successfulTrialCount: number;
}>;

export type MoldCompatibilityStatus =
  | "compatible"
  | "conditional"
  | "incompatible"
  | "insufficient-data";

export type MoldCompatibilityRuleStatus = "pass" | "warn" | "fail" | "unknown";

export type MoldCompatibilityRequirement = Readonly<{
  id: string;
  moldId: string;
  requiredClampForceTon?: number;
  moldWidthMm?: number;
  moldHeightMm?: number;
  moldDepthMm?: number;
  requiredTieBarWidthMm?: number;
  requiredTieBarHeightMm?: number;
  minimumShotCapacityGrams?: number;
  requiredInjectionUnit?: string;
  supportedMaterialGroup?: string;
  nominalCycleTimeSeconds?: number;
  effectiveCavityCount?: number;
  sourceType: string;
  sourceReference?: string;
  dataRecordedAtUtc?: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MachineCompatibilityProfile = Readonly<{
  id: string;
  machineAssetId: string;
  machineCode: string;
  machineDisplayName: string;
  clampForceTon?: number;
  maximumMoldWidthMm?: number;
  maximumMoldHeightMm?: number;
  minimumMoldDepthMm?: number;
  maximumMoldDepthMm?: number;
  tieBarWidthMm?: number;
  tieBarHeightMm?: number;
  maximumShotCapacityGrams?: number;
  injectionUnit?: string;
  supportedMaterialGroups: readonly string[];
  sourceType: string;
  sourceReference?: string;
  dataRecordedAtUtc?: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MoldCompatibilityRuleResult = Readonly<{
  ruleCode: string;
  labelTr: string;
  status: string;
  requiredValue?: string;
  availableValue?: string;
  source: string;
  dataTimestampUtc?: string;
  isCritical: boolean;
}>;

export type MoldCompatibilityAssessment = Readonly<{
  id: string;
  moldId: string;
  machineAssetId: string;
  machineCode: string;
  machineDisplayName: string;
  status: MoldCompatibilityStatus | string;
  ruleSetVersion: string;
  requirementVersion: number;
  machineProfileVersion: number;
  evaluatedAtUtc: string;
  reasons: readonly MoldCompatibilityRuleResult[];
  evidence: Readonly<Record<string, string>>;
  theoreticalPartsPerHour?: number;
  requirementStale: boolean;
  machineProfileStale: boolean;
  missingFields: readonly string[];
  idempotentReplay: boolean;
}>;

export type MoldCompatibilityAssessmentSummary = Readonly<{
  assessmentId: string;
  machineAssetId: string;
  machineCode: string;
  machineDisplayName: string;
  status: MoldCompatibilityStatus | string;
  criticalConstraintLabel?: string;
  requirementStale: boolean;
  machineProfileStale: boolean;
  theoreticalPartsPerHour?: number;
  evaluatedAtUtc: string;
}>;

export type MoldCompatibilityOverview = Readonly<{
  moldId: string;
  requirement?: MoldCompatibilityRequirement;
  compatibleCount: number;
  conditionalCount: number;
  incompatibleCount: number;
  insufficientDataCount: number;
  machines: readonly MoldCompatibilityAssessmentSummary[];
}>;

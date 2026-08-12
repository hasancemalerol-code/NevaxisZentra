export type ProcessOeeWorkspaceCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type ProcessOeeWorkspaceSummary = Readonly<{
  totalWindowCount: number;
  assessmentCount: number;
  calculatedCount: number;
  insufficientDataCount: number;
  reviewRequiredCount: number;
  staleDataCount: number;
  latestOeePercent: number | null;
  capabilities: ProcessOeeWorkspaceCapabilities;
}>;

export type ProcessOeeAssessmentSummary = Readonly<{
  id: string;
  machineAssetId: string;
  machineCode?: string;
  machineDisplayName?: string;
  productionOrderReference?: string;
  moldReference?: string;
  windowStartUtc: string;
  windowEndUtc: string;
  status: string;
  statusLabel: string;
  availabilityPercent: number | null;
  performancePercent: number | null;
  qualityPercent: number | null;
  oeePercent: number | null;
  evaluatedAtUtc: string;
}>;

export type ProcessOeeDriver = Readonly<{
  code: string;
  label: string;
  value?: string;
  severity: string;
}>;

export type ProcessOeeEvidence = Readonly<{
  field: string;
  value?: string;
  source?: string;
  recordedAtUtc?: string;
  isMissing: boolean;
  isConflict: boolean;
  conflictReason?: string;
}>;

export type ProcessOeeAssessmentDetail = Readonly<{
  id: string;
  evaluationWindowId: string;
  machineAssetId: string;
  machineCode?: string;
  machineDisplayName?: string;
  productionOrderReference?: string;
  moldReference?: string;
  windowStartUtc: string;
  windowEndUtc: string;
  status: string;
  statusLabel: string;
  ruleSetVersion: string;
  plannedProductionSeconds: number | null;
  operatingSeconds: number | null;
  downtimeSeconds: number | null;
  unknownSeconds: number | null;
  observedCycleCount: number | null;
  derivedShotCount: number | null;
  totalPieceCount: number | null;
  goodPieceCount: number | null;
  scrapPieceCount: number | null;
  availabilityPercent: number | null;
  performanceRawPercent: number | null;
  performancePercent: number | null;
  qualityPercent: number | null;
  oeePercent: number | null;
  drivers: readonly ProcessOeeDriver[];
  evidence: readonly ProcessOeeEvidence[];
  missingFields: readonly string[];
  conflicts: readonly string[];
  sourceSystem?: string;
  sourceFreshnessUtc?: string;
  isSimulatorDerived: boolean;
  calculationHonestyDisclaimer: string;
  simulatorHonestyDisclaimer?: string;
  evaluatedAtUtc: string;
}>;

export type ProcessOeeWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

export type ProcessOeeLossDriver = Readonly<{
  id: string;
  reasonCode: string;
  category: string;
  categoryLabel: string;
  severity: string;
  severityLabel: string;
  operationalPriority: number;
  observedValue: number | null;
  expectedValue: number | null;
  absoluteLossValue: number | null;
  affectedDurationSeconds: number | null;
  affectedQuantity: number | null;
  sourceReferences: readonly string[];
  evidenceTimestampUtc?: string;
  freshnessState: string;
  freshnessLabel: string;
  explanationLabel: string;
  dataLimitations: readonly string[];
  ruleSetVersion: string;
  isLeadingLoss: boolean;
  normalizedImpactPoints: number | null;
}>;

export type ProcessOeeLossDetail = Readonly<{
  assessmentId: string;
  evaluationWindowId: string;
  assessmentStatus: string;
  assessmentStatusLabel: string;
  ruleSetVersion: string;
  leadingLossMethod: string;
  leadingLoss?: ProcessOeeLossDriver;
  drivers: readonly ProcessOeeLossDriver[];
  evidence: readonly ProcessOeeEvidence[];
  missingFields: readonly string[];
  conflicts: readonly string[];
  sourceSystem?: string;
  sourceFreshnessUtc?: string;
  isSimulatorDerived: boolean;
  calculationHonestyDisclaimer: string;
  simulatorHonestyDisclaimer?: string;
}>;

export type ProcessOeeStabilityDriver = Readonly<{
  id: string;
  reasonCode: string;
  category: string;
  categoryLabel: string;
  resultState: string;
  resultStateLabel: string;
  severity: string;
  severityLabel: string;
  operationalPriority: number;
  observedValue: number | null;
  referenceValue: number | null;
  unit: string;
  sampleCount: number;
  timeRangeStartUtc: string;
  timeRangeEndUtc: string;
  sourceReferences: readonly string[];
  freshnessState: string;
  freshnessLabel: string;
  explanationLabel: string;
  dataLimitations: readonly string[];
  ruleSetVersion: string;
}>;

export type ProcessOeeDowntimeBreakdownRow = Readonly<{
  reasonCode: string;
  reasonLabel: string;
  durationSeconds: number;
  occurrenceCount: number;
  sortRank: number;
  isClassified: boolean;
  durationPercent: number | null;
}>;

export type ProcessOeeStabilityDetail = Readonly<{
  assessmentId: string;
  evaluationWindowId: string;
  assessmentStatus: string;
  assessmentStatusLabel: string;
  resultState: string;
  resultStateLabel: string;
  ruleSetVersion: string;
  medianCycleMs: number | null;
  madCycleMs: number | null;
  sampleCount: number;
  driftDirection: string;
  driftDirectionLabel: string;
  microStopCount: number;
  microStopTotalSeconds: number;
  microStopMedianSeconds: number;
  classifiedDowntimeSeconds: number;
  unclassifiedDowntimeSeconds: number;
  drivers: readonly ProcessOeeStabilityDriver[];
  downtimeBreakdown: readonly ProcessOeeDowntimeBreakdownRow[];
  evidence: readonly ProcessOeeEvidence[];
  missingFields: readonly string[];
  conflicts: readonly string[];
  sourceSystem?: string;
  sourceFreshnessUtc?: string;
  isSimulatorDerived: boolean;
  calculationHonestyDisclaimer: string;
  simulatorHonestyDisclaimer?: string;
}>;

export type ProcessOeeShiftComparisonRow = Readonly<{
  assessmentId: string;
  shiftCode: string;
  shiftLabel: string;
  windowStartUtc: string;
  windowEndUtc: string;
  plannedProductionSeconds: number | null;
  operatingSeconds: number | null;
  availabilityPercent: number | null;
  performancePercent: number | null;
  qualityPercent: number | null;
  oeePercent: number | null;
  stabilityResultState: string | null;
  stabilityResultStateLabel: string | null;
  downtimeSeconds: number | null;
  microStopCount: number;
  dataSufficiency: string;
  dataSufficiencyLabel: string;
  freshnessState: string;
  freshnessLabel: string;
  isComparable: boolean;
}>;

export type ProcessOeeShiftComparison = Readonly<{
  periodStartUtc: string;
  periodEndUtc: string;
  isComparable: boolean;
  comparabilityWarningCode: string | null;
  comparabilityWarningLabel: string | null;
  ruleSetVersion: string;
  summary: Readonly<{
    highestOeeShiftLabel: string | null;
    lowestAvailabilityShiftLabel: string | null;
    highestDowntimeShiftLabel: string | null;
    highestVariabilityShiftLabel: string | null;
  }> | null;
  rows: readonly ProcessOeeShiftComparisonRow[];
}>;

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

export type MaterialSummary = Readonly<{
  id: string;
  code: string;
  name: string;
  materialCategory: string;
  polymerFamily: string;
  manufacturer?: string;
  tradeNameOrGrade?: string;
  colorName?: string;
  lifecycleStatus: string;
  isActive: boolean;
  hasTechnicalData: boolean;
  facilityId: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MaterialTechnicalSpecification = Readonly<{
  densityGPerCm3?: number;
  meltFlowRate?: number;
  meltFlowUnit?: string;
  meltFlowTestTemperatureC?: number;
  meltFlowTestLoadKg?: number;
  moistureLimitPercent?: number;
  dryingRequired?: boolean;
  dryingTemperatureMinC?: number;
  dryingTemperatureMaxC?: number;
  dryingTimeMinHours?: number;
  dryingTimeMaxHours?: number;
  meltTemperatureMinC?: number;
  meltTemperatureMaxC?: number;
  moldTemperatureMinC?: number;
  moldTemperatureMaxC?: number;
  storageTemperatureMinC?: number;
  storageTemperatureMaxC?: number;
  maximumRegrindPercent?: number;
  technicalNotes?: string;
  safetyDataSheetReference?: string;
  technicalDataSheetReference?: string;
  specificationSource: string;
  specificationSourceReference?: string;
  specificationRecordedAtUtc?: string;
  hasTechnicalData: boolean;
  version: number;
}>;

export type MaterialDetail = Readonly<{
  id: string;
  facilityId: string;
  code: string;
  name: string;
  description?: string;
  materialCategory: string;
  polymerFamily: string;
  manufacturer?: string;
  tradeNameOrGrade?: string;
  colorName?: string;
  colorCode?: string;
  physicalForm: string;
  lifecycleStatus: string;
  unitOfMeasure: string;
  isActive: boolean;
  sourceType: string;
  sourceSystem?: string;
  sourceReference?: string;
  externalMaterialCode?: string;
  dataRecordedAtUtc?: string;
  technicalSpecification?: MaterialTechnicalSpecification;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MaterialLifecycleEvent = Readonly<{
  id: string;
  previousStatus: string;
  newStatus: string;
  reason: string;
  actorDisplayName: string;
  occurredAtUtc: string;
  sourceReference?: string;
  version: number;
}>;

export type MaterialWorkspaceCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type MaterialWorkspaceSummary = Readonly<{
  totalCount: number;
  activeCount: number;
  blockedCount: number;
  obsoleteCount: number;
  missingTechnicalDataCount: number;
  capabilities: MaterialWorkspaceCapabilities;
}>;

export type MaterialWriteResult = Readonly<{
  id: string;
  version: number;
  idempotentReplay: boolean;
}>;

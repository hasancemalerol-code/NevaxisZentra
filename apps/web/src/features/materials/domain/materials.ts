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

export type MaterialLotSummary = Readonly<{
  id: string;
  materialId: string;
  materialCode: string;
  materialName: string;
  lotNumber: string;
  supplierLotNumber?: string;
  manufacturerLotNumber?: string;
  status: string;
  qualityStatus: string;
  currentWarehouseReference?: string;
  currentLocationReference?: string;
  currentObservedQuantity?: number;
  unitOfMeasure: string;
  expiryDate?: string;
  retestDate?: string;
  isExpiryApproaching: boolean;
  sourceType: string;
  sourceReference?: string;
  dataRecordedAtUtc?: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MaterialLotDetail = Readonly<{
  id: string;
  facilityId: string;
  materialId: string;
  materialCode: string;
  materialName: string;
  lotNumber: string;
  supplierLotNumber?: string;
  manufacturerLotNumber?: string;
  supplierReference?: string;
  purchaseOrderReference?: string;
  goodsReceiptReference?: string;
  externalSystem?: string;
  externalLotReference?: string;
  manufactureDate?: string;
  receivedAtUtc?: string;
  expiryDate?: string;
  retestDate?: string;
  initialQuantity?: number;
  currentObservedQuantity?: number;
  unitOfMeasure: string;
  status: string;
  qualityStatus: string;
  currentStorageLocationId?: string;
  currentWarehouseReference?: string;
  currentLocationReference?: string;
  sourceType: string;
  sourceReference?: string;
  dataRecordedAtUtc?: string;
  notes?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}>;

export type MaterialLotWorkspaceSummary = Readonly<{
  totalCount: number;
  availableCount: number;
  qualityHoldCount: number;
  blockedCount: number;
  expiryApproachingCount: number;
}>;

export type MaterialStorageLocation = Readonly<{
  id: string;
  warehouseCode: string;
  warehouseName: string;
  locationCode: string;
  locationName: string;
  isActive: boolean;
  sourceType: string;
  sourceReference?: string;
  dataRecordedAtUtc?: string;
  version: number;
}>;

export type MaterialLotTimelineEntry = Readonly<{
  id: string;
  eventType: string;
  eventAtUtc: string;
  quantity?: number;
  unitOfMeasure?: string;
  fromWarehouseReference?: string;
  fromLocationReference?: string;
  toWarehouseReference?: string;
  toLocationReference?: string;
  statusBefore?: string;
  statusAfter?: string;
  qualityStatusBefore?: string;
  qualityStatusAfter?: string;
  reason?: string;
  sourceType: string;
  sourceReference?: string;
  actorDisplayName: string;
  createdAtUtc: string;
  version: number;
}>;

export type MaterialLotCertificate = Readonly<{
  id: string;
  certificateType: string;
  documentReference: string;
  documentNumber?: string;
  issuer?: string;
  issuedDate?: string;
  expiryDate?: string;
  sourceType: string;
  sourceReference?: string;
  createdAtUtc: string;
  version: number;
}>;

export type MaterialLotBackwardTraceability = Readonly<{
  lotId: string;
  lotNumber: string;
  materialId: string;
  materialCode: string;
  materialName: string;
  supplierLotNumber?: string;
  manufacturerLotNumber?: string;
  supplierReference?: string;
  purchaseOrderReference?: string;
  goodsReceiptReference?: string;
  externalSystem?: string;
  externalLotReference?: string;
  initialWarehouseReference?: string;
  initialLocationReference?: string;
  certificates: readonly MaterialLotCertificate[];
  sourceEvents: readonly MaterialLotTimelineEntry[];
}>;

export type MaterialLotForwardTraceability = Readonly<{
  lotId: string;
  lotNumber: string;
  currentWarehouseReference?: string;
  currentLocationReference?: string;
  status: string;
  qualityStatus: string;
  locationTransfers: readonly MaterialLotTimelineEntry[];
  statusChanges: readonly MaterialLotTimelineEntry[];
  qualityChanges: readonly MaterialLotTimelineEntry[];
  certificates: readonly MaterialLotCertificate[];
}>;

export type MaterialLotTraceability = Readonly<{
  backward: MaterialLotBackwardTraceability | null;
  forward: MaterialLotForwardTraceability | null;
}>;

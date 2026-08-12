import type {
  MaterialAssessmentDriver,
  MaterialAssessmentEvidence,
  MaterialConsumptionObservation,
  MaterialDetail,
  MaterialLifecycleEvent,
  MaterialLotBackwardTraceability,
  MaterialLotCertificate,
  MaterialLotDetail,
  MaterialLotForwardTraceability,
  MaterialLotSummary,
  MaterialLotTimelineEntry,
  MaterialLotTraceability,
  MaterialLotWorkspaceSummary,
  MaterialRequirementAssessment,
  MaterialRequirementOverview,
  MaterialRequirementProfileDetail,
  MaterialRequirementProfileSummary,
  MaterialStorageLocation,
  MaterialSummary,
  MaterialTechnicalSpecification,
  MaterialWorkspaceSummary,
  MaterialWriteResult,
} from "../domain/materials";

function readString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readNumber(value: unknown): number | undefined {
  return typeof value === "number" ? value : undefined;
}

function readBoolean(value: unknown): boolean | undefined {
  return typeof value === "boolean" ? value : undefined;
}

export function parseMaterialSummary(value: unknown): MaterialSummary {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    code: String(item.code),
    name: String(item.name),
    materialCategory: String(item.materialCategory),
    polymerFamily: String(item.polymerFamily),
    manufacturer: readString(item.manufacturer),
    tradeNameOrGrade: readString(item.tradeNameOrGrade),
    colorName: readString(item.colorName),
    lifecycleStatus: String(item.lifecycleStatus),
    isActive: item.isActive === true,
    hasTechnicalData: item.hasTechnicalData === true,
    facilityId: String(item.facilityId),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialSummaryList(payload: unknown): readonly MaterialSummary[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected material list array.");
  }

  return payload.map(parseMaterialSummary);
}

export function parseMaterialTechnicalSpecification(value: unknown): MaterialTechnicalSpecification {
  const item = value as Record<string, unknown>;
  return {
    densityGPerCm3: readNumber(item.densityGPerCm3),
    meltFlowRate: readNumber(item.meltFlowRate),
    meltFlowUnit: readString(item.meltFlowUnit),
    meltFlowTestTemperatureC: readNumber(item.meltFlowTestTemperatureC),
    meltFlowTestLoadKg: readNumber(item.meltFlowTestLoadKg),
    moistureLimitPercent: readNumber(item.moistureLimitPercent),
    dryingRequired: readBoolean(item.dryingRequired),
    dryingTemperatureMinC: readNumber(item.dryingTemperatureMinC),
    dryingTemperatureMaxC: readNumber(item.dryingTemperatureMaxC),
    dryingTimeMinHours: readNumber(item.dryingTimeMinHours),
    dryingTimeMaxHours: readNumber(item.dryingTimeMaxHours),
    meltTemperatureMinC: readNumber(item.meltTemperatureMinC),
    meltTemperatureMaxC: readNumber(item.meltTemperatureMaxC),
    moldTemperatureMinC: readNumber(item.moldTemperatureMinC),
    moldTemperatureMaxC: readNumber(item.moldTemperatureMaxC),
    storageTemperatureMinC: readNumber(item.storageTemperatureMinC),
    storageTemperatureMaxC: readNumber(item.storageTemperatureMaxC),
    maximumRegrindPercent: readNumber(item.maximumRegrindPercent),
    technicalNotes: readString(item.technicalNotes),
    safetyDataSheetReference: readString(item.safetyDataSheetReference),
    technicalDataSheetReference: readString(item.technicalDataSheetReference),
    specificationSource: String(item.specificationSource),
    specificationSourceReference: readString(item.specificationSourceReference),
    specificationRecordedAtUtc: readString(item.specificationRecordedAtUtc),
    hasTechnicalData: item.hasTechnicalData === true,
    version: Number(item.version),
  };
}

export function parseMaterialDetail(payload: unknown): MaterialDetail {
  const item = payload as Record<string, unknown>;
  const technicalSpec = item.technicalSpecification;
  return {
    id: String(item.id),
    facilityId: String(item.facilityId),
    code: String(item.code),
    name: String(item.name),
    description: readString(item.description),
    materialCategory: String(item.materialCategory),
    polymerFamily: String(item.polymerFamily),
    manufacturer: readString(item.manufacturer),
    tradeNameOrGrade: readString(item.tradeNameOrGrade),
    colorName: readString(item.colorName),
    colorCode: readString(item.colorCode),
    physicalForm: String(item.physicalForm),
    lifecycleStatus: String(item.lifecycleStatus),
    unitOfMeasure: String(item.unitOfMeasure),
    isActive: item.isActive === true,
    sourceType: String(item.sourceType),
    sourceSystem: readString(item.sourceSystem),
    sourceReference: readString(item.sourceReference),
    externalMaterialCode: readString(item.externalMaterialCode),
    dataRecordedAtUtc: readString(item.dataRecordedAtUtc),
    technicalSpecification:
      technicalSpec && typeof technicalSpec === "object"
        ? parseMaterialTechnicalSpecification(technicalSpec)
        : undefined,
    createdAtUtc: String(item.createdAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialLifecycleEvent(value: unknown): MaterialLifecycleEvent {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    previousStatus: String(item.previousStatus),
    newStatus: String(item.newStatus),
    reason: String(item.reason),
    actorDisplayName: String(item.actorDisplayName),
    occurredAtUtc: String(item.occurredAtUtc),
    sourceReference: readString(item.sourceReference),
    version: Number(item.version),
  };
}

export function parseMaterialLifecycleEventList(payload: unknown): readonly MaterialLifecycleEvent[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected lifecycle history array.");
  }

  return payload.map(parseMaterialLifecycleEvent);
}

export function parseMaterialWorkspaceSummary(payload: unknown): MaterialWorkspaceSummary {
  const item = payload as Record<string, unknown>;
  const capabilities = item.capabilities as Record<string, unknown> | undefined;
  return {
    totalCount: Number(item.totalCount),
    activeCount: Number(item.activeCount),
    blockedCount: Number(item.blockedCount),
    obsoleteCount: Number(item.obsoleteCount),
    missingTechnicalDataCount: Number(item.missingTechnicalDataCount),
    capabilities: {
      canRead: capabilities?.canRead === true,
      canManage: capabilities?.canManage === true,
    },
  };
}

export function parseMaterialWriteApiResponse(payload: unknown): MaterialWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    version: Number(item.version),
    idempotentReplay: Boolean(item.idempotentReplay),
  };
}

function readOptionalString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

export function parseMaterialLotSummary(value: unknown): MaterialLotSummary {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    materialId: String(item.materialId),
    materialCode: String(item.materialCode),
    materialName: String(item.materialName),
    lotNumber: String(item.lotNumber),
    supplierLotNumber: readOptionalString(item.supplierLotNumber),
    manufacturerLotNumber: readOptionalString(item.manufacturerLotNumber),
    status: String(item.status),
    qualityStatus: String(item.qualityStatus),
    currentWarehouseReference: readOptionalString(item.currentWarehouseReference),
    currentLocationReference: readOptionalString(item.currentLocationReference),
    currentObservedQuantity: readNumber(item.currentObservedQuantity),
    unitOfMeasure: String(item.unitOfMeasure),
    expiryDate: readOptionalString(item.expiryDate),
    retestDate: readOptionalString(item.retestDate),
    isExpiryApproaching: item.isExpiryApproaching === true,
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    dataRecordedAtUtc: readOptionalString(item.dataRecordedAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialLotSummaryList(payload: unknown): readonly MaterialLotSummary[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected material lot list array.");
  }

  return payload.map(parseMaterialLotSummary);
}

export function parseMaterialLotDetail(payload: unknown): MaterialLotDetail {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    facilityId: String(item.facilityId),
    materialId: String(item.materialId),
    materialCode: String(item.materialCode),
    materialName: String(item.materialName),
    lotNumber: String(item.lotNumber),
    supplierLotNumber: readOptionalString(item.supplierLotNumber),
    manufacturerLotNumber: readOptionalString(item.manufacturerLotNumber),
    supplierReference: readOptionalString(item.supplierReference),
    purchaseOrderReference: readOptionalString(item.purchaseOrderReference),
    goodsReceiptReference: readOptionalString(item.goodsReceiptReference),
    externalSystem: readOptionalString(item.externalSystem),
    externalLotReference: readOptionalString(item.externalLotReference),
    manufactureDate: readOptionalString(item.manufactureDate),
    receivedAtUtc: readOptionalString(item.receivedAtUtc),
    expiryDate: readOptionalString(item.expiryDate),
    retestDate: readOptionalString(item.retestDate),
    initialQuantity: readNumber(item.initialQuantity),
    currentObservedQuantity: readNumber(item.currentObservedQuantity),
    unitOfMeasure: String(item.unitOfMeasure),
    status: String(item.status),
    qualityStatus: String(item.qualityStatus),
    currentStorageLocationId: readOptionalString(item.currentStorageLocationId),
    currentWarehouseReference: readOptionalString(item.currentWarehouseReference),
    currentLocationReference: readOptionalString(item.currentLocationReference),
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    dataRecordedAtUtc: readOptionalString(item.dataRecordedAtUtc),
    notes: readOptionalString(item.notes),
    createdAtUtc: String(item.createdAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialLotWorkspaceSummary(payload: unknown): MaterialLotWorkspaceSummary {
  const item = payload as Record<string, unknown>;
  return {
    totalCount: Number(item.totalCount),
    availableCount: Number(item.availableCount),
    qualityHoldCount: Number(item.qualityHoldCount),
    blockedCount: Number(item.blockedCount),
    expiryApproachingCount: Number(item.expiryApproachingCount),
  };
}

export function parseMaterialStorageLocation(value: unknown): MaterialStorageLocation {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    warehouseCode: String(item.warehouseCode),
    warehouseName: String(item.warehouseName),
    locationCode: String(item.locationCode),
    locationName: String(item.locationName),
    isActive: item.isActive === true,
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    dataRecordedAtUtc: readOptionalString(item.dataRecordedAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialStorageLocationList(payload: unknown): readonly MaterialStorageLocation[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected storage location list array.");
  }

  return payload.map(parseMaterialStorageLocation);
}

export function parseMaterialLotTimelineEntry(value: unknown): MaterialLotTimelineEntry {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    eventType: String(item.eventType),
    eventAtUtc: String(item.eventAtUtc),
    quantity: readNumber(item.quantity),
    unitOfMeasure: readOptionalString(item.unitOfMeasure),
    fromWarehouseReference: readOptionalString(item.fromWarehouseReference),
    fromLocationReference: readOptionalString(item.fromLocationReference),
    toWarehouseReference: readOptionalString(item.toWarehouseReference),
    toLocationReference: readOptionalString(item.toLocationReference),
    statusBefore: readOptionalString(item.statusBefore),
    statusAfter: readOptionalString(item.statusAfter),
    qualityStatusBefore: readOptionalString(item.qualityStatusBefore),
    qualityStatusAfter: readOptionalString(item.qualityStatusAfter),
    reason: readOptionalString(item.reason),
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    actorDisplayName: String(item.actorDisplayName),
    createdAtUtc: String(item.createdAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialLotTimelineEntryList(payload: unknown): readonly MaterialLotTimelineEntry[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected lot timeline array.");
  }

  return payload.map(parseMaterialLotTimelineEntry);
}

export function parseMaterialLotCertificate(value: unknown): MaterialLotCertificate {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    certificateType: String(item.certificateType),
    documentReference: String(item.documentReference),
    documentNumber: readOptionalString(item.documentNumber),
    issuer: readOptionalString(item.issuer),
    issuedDate: readOptionalString(item.issuedDate),
    expiryDate: readOptionalString(item.expiryDate),
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    createdAtUtc: String(item.createdAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialLotBackwardTraceability(payload: unknown): MaterialLotBackwardTraceability {
  const item = payload as Record<string, unknown>;
  const certificates = item.certificates;
  const sourceEvents = item.sourceEvents;
  return {
    lotId: String(item.lotId),
    lotNumber: String(item.lotNumber),
    materialId: String(item.materialId),
    materialCode: String(item.materialCode),
    materialName: String(item.materialName),
    supplierLotNumber: readOptionalString(item.supplierLotNumber),
    manufacturerLotNumber: readOptionalString(item.manufacturerLotNumber),
    supplierReference: readOptionalString(item.supplierReference),
    purchaseOrderReference: readOptionalString(item.purchaseOrderReference),
    goodsReceiptReference: readOptionalString(item.goodsReceiptReference),
    externalSystem: readOptionalString(item.externalSystem),
    externalLotReference: readOptionalString(item.externalLotReference),
    initialWarehouseReference: readOptionalString(item.initialWarehouseReference),
    initialLocationReference: readOptionalString(item.initialLocationReference),
    certificates: Array.isArray(certificates) ? certificates.map(parseMaterialLotCertificate) : [],
    sourceEvents: Array.isArray(sourceEvents) ? sourceEvents.map(parseMaterialLotTimelineEntry) : [],
  };
}

export function parseMaterialLotForwardTraceability(payload: unknown): MaterialLotForwardTraceability {
  const item = payload as Record<string, unknown>;
  const locationTransfers = item.locationTransfers;
  const statusChanges = item.statusChanges;
  const qualityChanges = item.qualityChanges;
  const certificates = item.certificates;
  return {
    lotId: String(item.lotId),
    lotNumber: String(item.lotNumber),
    currentWarehouseReference: readOptionalString(item.currentWarehouseReference),
    currentLocationReference: readOptionalString(item.currentLocationReference),
    status: String(item.status),
    qualityStatus: String(item.qualityStatus),
    locationTransfers: Array.isArray(locationTransfers) ? locationTransfers.map(parseMaterialLotTimelineEntry) : [],
    statusChanges: Array.isArray(statusChanges) ? statusChanges.map(parseMaterialLotTimelineEntry) : [],
    qualityChanges: Array.isArray(qualityChanges) ? qualityChanges.map(parseMaterialLotTimelineEntry) : [],
    certificates: Array.isArray(certificates) ? certificates.map(parseMaterialLotCertificate) : [],
  };
}

export function parseMaterialLotTraceability(payload: unknown): MaterialLotTraceability {
  const item = payload as Record<string, unknown>;
  const backward = item.backward;
  const forward = item.forward;
  return {
    backward:
      backward && typeof backward === "object"
        ? parseMaterialLotBackwardTraceability(backward)
        : null,
    forward:
      forward && typeof forward === "object"
        ? parseMaterialLotForwardTraceability(forward)
        : null,
  };
}

function parseMaterialAssessmentDriver(value: unknown): MaterialAssessmentDriver {
  const item = value as Record<string, unknown>;
  return {
    code: String(item.code),
    label: String(item.label),
    value: readOptionalString(item.value),
    state: String(item.state),
  };
}

function parseMaterialAssessmentEvidence(value: unknown): MaterialAssessmentEvidence {
  const item = value as Record<string, unknown>;
  return {
    field: String(item.field),
    value: readOptionalString(item.value),
    source: readOptionalString(item.source),
    recordedAtUtc: readOptionalString(item.recordedAtUtc),
    isMissing: item.isMissing === true,
  };
}

export function parseMaterialRequirementAssessment(value: unknown): MaterialRequirementAssessment {
  const item = value as Record<string, unknown>;
  const drivers = item.drivers;
  const evidence = item.evidence;
  return {
    id: String(item.id),
    materialId: String(item.materialId),
    requirementProfileId: readOptionalString(item.requirementProfileId),
    assessmentType: String(item.assessmentType),
    status: String(item.status),
    ruleSetVersion: String(item.ruleSetVersion),
    inputFingerprint: String(item.inputFingerprint),
    drivers: Array.isArray(drivers) ? drivers.map(parseMaterialAssessmentDriver) : [],
    evidence: Array.isArray(evidence) ? evidence.map(parseMaterialAssessmentEvidence) : [],
    theoreticalRequirementGrams: readNumber(item.theoreticalRequirementGrams),
    observedConsumptionGrams: readNumber(item.observedConsumptionGrams),
    varianceGrams: readNumber(item.varianceGrams),
    variancePercent: readNumber(item.variancePercent),
    virginRequirementGrams: readNumber(item.virginRequirementGrams),
    regrindRequirementGrams: readNumber(item.regrindRequirementGrams),
    evaluatedAtUtc: String(item.evaluatedAtUtc),
    createdAtUtc: String(item.createdAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialRequirementProfileSummary(value: unknown): MaterialRequirementProfileSummary {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    materialId: String(item.materialId),
    materialCode: String(item.materialCode),
    materialName: String(item.materialName),
    requirementType: String(item.requirementType),
    productionOrderReference: readOptionalString(item.productionOrderReference),
    productReference: readOptionalString(item.productReference),
    moldReference: readOptionalString(item.moldReference),
    goodPartTargetQuantity: readNumber(item.goodPartTargetQuantity),
    plannedRegrindPercent: readNumber(item.plannedRegrindPercent),
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    dataRecordedAtUtc: readOptionalString(item.dataRecordedAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialRequirementProfileSummaryList(
  payload: unknown,
): readonly MaterialRequirementProfileSummary[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected requirement profile list array.");
  }

  return payload.map(parseMaterialRequirementProfileSummary);
}

export function parseMaterialRequirementProfileDetail(payload: unknown): MaterialRequirementProfileDetail {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    facilityId: String(item.facilityId),
    materialId: String(item.materialId),
    materialCode: String(item.materialCode),
    materialName: String(item.materialName),
    requirementType: String(item.requirementType),
    productionOrderReference: readOptionalString(item.productionOrderReference),
    productReference: readOptionalString(item.productReference),
    moldReference: readOptionalString(item.moldReference),
    plannedProductionQuantity: readNumber(item.plannedProductionQuantity),
    goodPartTargetQuantity: readNumber(item.goodPartTargetQuantity),
    partWeightGrams: readNumber(item.partWeightGrams),
    runnerWeightGramsPerShot: readNumber(item.runnerWeightGramsPerShot),
    effectiveCavityCount:
      item.effectiveCavityCount === undefined || item.effectiveCavityCount === null
        ? undefined
        : Number(item.effectiveCavityCount),
    setupAllowanceGrams: readNumber(item.setupAllowanceGrams),
    processLossAllowancePercent: readNumber(item.processLossAllowancePercent),
    plannedVirginPercent: readNumber(item.plannedVirginPercent),
    plannedRegrindPercent: readNumber(item.plannedRegrindPercent),
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    dataRecordedAtUtc: readOptionalString(item.dataRecordedAtUtc),
    createdAtUtc: String(item.createdAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMaterialConsumptionObservation(value: unknown): MaterialConsumptionObservation {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    materialId: String(item.materialId),
    materialCode: String(item.materialCode),
    materialLotId: readOptionalString(item.materialLotId),
    lotNumber: readOptionalString(item.lotNumber),
    requirementProfileId: readOptionalString(item.requirementProfileId),
    observationType: String(item.observationType),
    quantity: Number(item.quantity),
    unitOfMeasure: String(item.unitOfMeasure),
    virginQuantity: readNumber(item.virginQuantity),
    regrindQuantity: readNumber(item.regrindQuantity),
    goodPartQuantity: readNumber(item.goodPartQuantity),
    scrapQuantity: readNumber(item.scrapQuantity),
    productionOrderReference: readOptionalString(item.productionOrderReference),
    productReference: readOptionalString(item.productReference),
    moldReference: readOptionalString(item.moldReference),
    observedAtUtc: String(item.observedAtUtc),
    sourceType: String(item.sourceType),
    sourceReference: readOptionalString(item.sourceReference),
    dataRecordedAtUtc: readOptionalString(item.dataRecordedAtUtc),
    notes: readOptionalString(item.notes),
    createdAtUtc: String(item.createdAtUtc),
    version: Number(item.version),
    disclaimer: readOptionalString(item.disclaimer),
  };
}

export function parseMaterialConsumptionObservationList(
  payload: unknown,
): readonly MaterialConsumptionObservation[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected consumption observation list array.");
  }

  return payload.map(parseMaterialConsumptionObservation);
}

export function parseMaterialRequirementOverview(payload: unknown): MaterialRequirementOverview {
  const item = payload as Record<string, unknown>;
  const currentRequirementAssessment = item.currentRequirementAssessment;
  const currentVarianceAssessment = item.currentVarianceAssessment;
  const currentRegrindAssessment = item.currentRegrindAssessment;
  const currentAvailabilityAssessment = item.currentAvailabilityAssessment;
  const profiles = item.profiles;
  const recentObservations = item.recentObservations;

  return {
    materialId: String(item.materialId),
    materialCode: String(item.materialCode),
    currentRequirementAssessment:
      currentRequirementAssessment && typeof currentRequirementAssessment === "object"
        ? parseMaterialRequirementAssessment(currentRequirementAssessment)
        : undefined,
    currentVarianceAssessment:
      currentVarianceAssessment && typeof currentVarianceAssessment === "object"
        ? parseMaterialRequirementAssessment(currentVarianceAssessment)
        : undefined,
    currentRegrindAssessment:
      currentRegrindAssessment && typeof currentRegrindAssessment === "object"
        ? parseMaterialRequirementAssessment(currentRegrindAssessment)
        : undefined,
    currentAvailabilityAssessment:
      currentAvailabilityAssessment && typeof currentAvailabilityAssessment === "object"
        ? parseMaterialRequirementAssessment(currentAvailabilityAssessment)
        : undefined,
    profiles: Array.isArray(profiles) ? profiles.map(parseMaterialRequirementProfileSummary) : [],
    recentObservations: Array.isArray(recentObservations)
      ? recentObservations.map(parseMaterialConsumptionObservation)
      : [],
    theoreticalRequirementDisclaimer: String(item.theoreticalRequirementDisclaimer),
    consumptionObservationDisclaimer: String(item.consumptionObservationDisclaimer),
  };
}

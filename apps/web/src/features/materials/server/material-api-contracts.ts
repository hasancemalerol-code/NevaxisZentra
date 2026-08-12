import type {
  MaterialDetail,
  MaterialLifecycleEvent,
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

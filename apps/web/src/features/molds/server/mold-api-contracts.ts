import type {
  MachineCompatibilityProfile,
  MoldCompatibilityAssessment,
  MoldCompatibilityOverview,
  MoldCompatibilityRequirement,
  MoldCompatibilityRuleResult,
  MoldDetail,
  MoldLifecycleEvent,
  MoldMaintenanceRecord,
  MoldRevisionRecord,
  MoldSummary,
  MoldTimelineEntry,
  MoldTrialRecord,
  MoldWorkspaceSummary,
  MoldWriteResult,
} from "../domain/molds";

function readString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readNumber(value: unknown): number | undefined {
  return typeof value === "number" ? value : undefined;
}

function readBoolean(value: unknown): boolean | undefined {
  return typeof value === "boolean" ? value : undefined;
}

export function parseMoldSummary(value: unknown): MoldSummary {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    code: String(item.code),
    name: String(item.name),
    lifecycleState: String(item.lifecycleState),
    ownershipType: String(item.ownershipType),
    locationType: String(item.locationType),
    locationLabel: readString(item.locationLabel),
    facilityId: String(item.facilityId),
    cavityCount: readNumber(item.cavityCount),
    currentShotCount: readNumber(item.currentShotCount),
    counterRecordedAtUtc: readString(item.counterRecordedAtUtc),
    sourceType: String(item.sourceType),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMoldSummaryList(payload: unknown): readonly MoldSummary[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected mold list array.");
  }

  return payload.map(parseMoldSummary);
}

export function parseMoldDetail(payload: unknown): MoldDetail {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    facilityId: String(item.facilityId),
    code: String(item.code),
    name: String(item.name),
    description: readString(item.description),
    serialNumber: readString(item.serialNumber),
    manufacturer: readString(item.manufacturer),
    manufactureYear: readNumber(item.manufactureYear),
    manufactureDate: readString(item.manufactureDate),
    commissioningDate: readString(item.commissioningDate),
    sourceType: String(item.sourceType),
    externalSourceReference: readString(item.externalSourceReference),
    ownershipType: String(item.ownershipType),
    ownerReference: readString(item.ownerReference),
    locationType: String(item.locationType),
    locationLabel: readString(item.locationLabel),
    locationChangedAtUtc: readString(item.locationChangedAtUtc),
    lifecycleState: String(item.lifecycleState),
    moldType: readString(item.moldType),
    cavityCount: readNumber(item.cavityCount),
    nominalShotLife: readNumber(item.nominalShotLife),
    currentShotCount: readNumber(item.currentShotCount),
    counterRecordedAtUtc: readString(item.counterRecordedAtUtc),
    counterSource: readString(item.counterSource),
    counterSourceReference: readString(item.counterSourceReference),
    counterNote: readString(item.counterNote),
    primaryProductReference: readString(item.primaryProductReference),
    materialFamilyReference: readString(item.materialFamilyReference),
    runnerType: readString(item.runnerType),
    hotRunnerPresent: readBoolean(item.hotRunnerPresent),
    moldWeightKg: readNumber(item.moldWeightKg),
    dimensionLengthMm: readNumber(item.dimensionLengthMm),
    dimensionWidthMm: readNumber(item.dimensionWidthMm),
    dimensionHeightMm: readNumber(item.dimensionHeightMm),
    notes: readString(item.notes),
    createdAtUtc: String(item.createdAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMoldLifecycleEvent(value: unknown): MoldLifecycleEvent {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    previousState: String(item.previousState),
    newState: String(item.newState),
    reason: String(item.reason),
    actorDisplayName: String(item.actorDisplayName),
    occurredAtUtc: String(item.occurredAtUtc),
    sourceReference: readString(item.sourceReference),
    version: Number(item.version),
  };
}

export function parseMoldLifecycleEventList(payload: unknown): readonly MoldLifecycleEvent[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected lifecycle history array.");
  }

  return payload.map(parseMoldLifecycleEvent);
}

export function parseMoldWorkspaceSummary(payload: unknown): MoldWorkspaceSummary {
  const item = payload as Record<string, unknown>;
  const capabilities = item.capabilities as Record<string, unknown> | undefined;
  return {
    totalCount: Number(item.totalCount),
    draftCount: Number(item.draftCount),
    activeCount: Number(item.activeCount),
    maintenanceCount: Number(item.maintenanceCount),
    trialCount: Number(item.trialCount),
    quarantinedCount: Number(item.quarantinedCount),
    retiredCount: Number(item.retiredCount),
    scrappedCount: Number(item.scrappedCount),
    capabilities: {
      canRead: capabilities?.canRead === true,
      canManage: capabilities?.canManage === true,
    },
  };
}

export function parseMoldWriteApiResponse(payload: unknown): MoldWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    version: Number(item.version),
    idempotentReplay: Boolean(item.idempotentReplay),
  };
}

export function parseMoldMaintenanceRecord(value: unknown): MoldMaintenanceRecord {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    moldId: String(item.moldId),
    maintenanceType: String(item.maintenanceType),
    status: String(item.status),
    openedAtUtc: String(item.openedAtUtc),
    completedAtUtc: readString(item.completedAtUtc),
    summary: String(item.summary),
    findings: readString(item.findings),
    actionsTaken: readString(item.actionsTaken),
    performedByDisplayName: readString(item.performedByDisplayName),
    externalMaintenanceRequestReference: readString(item.externalMaintenanceRequestReference),
    externalWorkOrderReference: readString(item.externalWorkOrderReference),
    sourceType: String(item.sourceType),
    sourceReference: readString(item.sourceReference),
    createdAtUtc: String(item.createdAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMoldMaintenanceRecordList(payload: unknown): readonly MoldMaintenanceRecord[] {
  if (!Array.isArray(payload)) throw new Error("Expected maintenance history array.");
  return payload.map(parseMoldMaintenanceRecord);
}

export function parseMoldRevisionRecord(value: unknown): MoldRevisionRecord {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    moldId: String(item.moldId),
    revisionNumber: String(item.revisionNumber),
    revisionType: String(item.revisionType),
    revisionDate: String(item.revisionDate),
    summary: String(item.summary),
    reason: readString(item.reason),
    technicalChange: readString(item.technicalChange),
    previousCavityCount: readNumber(item.previousCavityCount),
    newCavityCount: readNumber(item.newCavityCount),
    performedByDisplayName: readString(item.performedByDisplayName),
    externalReference: readString(item.externalReference),
    sourceType: String(item.sourceType),
    sourceReference: readString(item.sourceReference),
    createdAtUtc: String(item.createdAtUtc),
  };
}

export function parseMoldRevisionRecordList(payload: unknown): readonly MoldRevisionRecord[] {
  if (!Array.isArray(payload)) throw new Error("Expected revision history array.");
  return payload.map(parseMoldRevisionRecord);
}

export function parseMoldTrialRecord(value: unknown): MoldTrialRecord {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    moldId: String(item.moldId),
    trialNumber: String(item.trialNumber),
    trialDate: String(item.trialDate),
    result: String(item.result),
    machineReference: readString(item.machineReference),
    productionOrderReference: readString(item.productionOrderReference),
    productReference: readString(item.productReference),
    materialReference: readString(item.materialReference),
    cycleTimeSeconds: readNumber(item.cycleTimeSeconds),
    shotCount: readNumber(item.shotCount),
    goodPartCount: readNumber(item.goodPartCount),
    scrapPartCount: readNumber(item.scrapPartCount),
    findings: readString(item.findings),
    decision: readString(item.decision),
    performedByDisplayName: readString(item.performedByDisplayName),
    sourceType: String(item.sourceType),
    sourceReference: readString(item.sourceReference),
    createdAtUtc: String(item.createdAtUtc),
  };
}

export function parseMoldTrialRecordList(payload: unknown): readonly MoldTrialRecord[] {
  if (!Array.isArray(payload)) throw new Error("Expected trial history array.");
  return payload.map(parseMoldTrialRecord);
}

export function parseMoldTimelineEntry(value: unknown): MoldTimelineEntry {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    eventKind: String(item.eventKind),
    eventDateUtc: String(item.eventDateUtc),
    createdAtUtc: String(item.createdAtUtc),
    title: String(item.title),
    summary: String(item.summary),
    statusOrResult: readString(item.statusOrResult),
    reference: readString(item.reference),
    actorDisplayName: readString(item.actorDisplayName),
  };
}

export function parseMoldTimelineEntryList(payload: unknown): readonly MoldTimelineEntry[] {
  if (!Array.isArray(payload)) throw new Error("Expected timeline array.");
  return payload.map(parseMoldTimelineEntry);
}

function readStringList(value: unknown): readonly string[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value.filter((item): item is string => typeof item === "string");
}

function readStringRecord(value: unknown): Readonly<Record<string, string>> {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return {};
  }

  const record: Record<string, string> = {};
  for (const [key, entry] of Object.entries(value as Record<string, unknown>)) {
    if (typeof entry === "string") {
      record[key] = entry;
    }
  }

  return record;
}

export function parseMoldCompatibilityRequirement(payload: unknown): MoldCompatibilityRequirement | null {
  if (payload === null || payload === undefined) {
    return null;
  }

  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id),
    moldId: String(item.moldId),
    requiredClampForceTon: readNumber(item.requiredClampForceTon),
    moldWidthMm: readNumber(item.moldWidthMm),
    moldHeightMm: readNumber(item.moldHeightMm),
    moldDepthMm: readNumber(item.moldDepthMm),
    requiredTieBarWidthMm: readNumber(item.requiredTieBarWidthMm),
    requiredTieBarHeightMm: readNumber(item.requiredTieBarHeightMm),
    minimumShotCapacityGrams: readNumber(item.minimumShotCapacityGrams),
    requiredInjectionUnit: readString(item.requiredInjectionUnit),
    supportedMaterialGroup: readString(item.supportedMaterialGroup),
    nominalCycleTimeSeconds: readNumber(item.nominalCycleTimeSeconds),
    effectiveCavityCount: readNumber(item.effectiveCavityCount),
    sourceType: String(item.sourceType),
    sourceReference: readString(item.sourceReference),
    dataRecordedAtUtc: readString(item.dataRecordedAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMachineCompatibilityProfile(value: unknown): MachineCompatibilityProfile {
  const item = value as Record<string, unknown>;
  return {
    id: String(item.id),
    machineAssetId: String(item.machineAssetId),
    machineCode: String(item.machineCode),
    machineDisplayName: String(item.machineDisplayName),
    clampForceTon: readNumber(item.clampForceTon),
    maximumMoldWidthMm: readNumber(item.maximumMoldWidthMm),
    maximumMoldHeightMm: readNumber(item.maximumMoldHeightMm),
    minimumMoldDepthMm: readNumber(item.minimumMoldDepthMm),
    maximumMoldDepthMm: readNumber(item.maximumMoldDepthMm),
    tieBarWidthMm: readNumber(item.tieBarWidthMm),
    tieBarHeightMm: readNumber(item.tieBarHeightMm),
    maximumShotCapacityGrams: readNumber(item.maximumShotCapacityGrams),
    injectionUnit: readString(item.injectionUnit),
    supportedMaterialGroups: readStringList(item.supportedMaterialGroups),
    sourceType: String(item.sourceType),
    sourceReference: readString(item.sourceReference),
    dataRecordedAtUtc: readString(item.dataRecordedAtUtc),
    updatedAtUtc: String(item.updatedAtUtc),
    version: Number(item.version),
  };
}

export function parseMachineCompatibilityProfileList(payload: unknown): readonly MachineCompatibilityProfile[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected machine compatibility profile array.");
  }

  return payload.map(parseMachineCompatibilityProfile);
}

export function parseMoldCompatibilityRuleResult(value: unknown): MoldCompatibilityRuleResult {
  const item = value as Record<string, unknown>;
  return {
    ruleCode: String(item.ruleCode),
    labelTr: String(item.labelTr),
    status: String(item.status),
    requiredValue: readString(item.requiredValue),
    availableValue: readString(item.availableValue),
    source: String(item.source),
    dataTimestampUtc: readString(item.dataTimestampUtc),
    isCritical: readBoolean(item.isCritical) === true,
  };
}

export function parseMoldCompatibilityAssessment(payload: unknown): MoldCompatibilityAssessment {
  const item = payload as Record<string, unknown>;
  const reasons = Array.isArray(item.reasons)
    ? item.reasons.map(parseMoldCompatibilityRuleResult)
    : [];

  return {
    id: String(item.id),
    moldId: String(item.moldId),
    machineAssetId: String(item.machineAssetId),
    machineCode: String(item.machineCode),
    machineDisplayName: String(item.machineDisplayName),
    status: String(item.status),
    ruleSetVersion: String(item.ruleSetVersion),
    requirementVersion: Number(item.requirementVersion),
    machineProfileVersion: Number(item.machineProfileVersion),
    evaluatedAtUtc: String(item.evaluatedAtUtc),
    reasons,
    evidence: readStringRecord(item.evidence),
    theoreticalPartsPerHour: readNumber(item.theoreticalPartsPerHour),
    requirementStale: readBoolean(item.requirementStale) === true,
    machineProfileStale: readBoolean(item.machineProfileStale) === true,
    missingFields: readStringList(item.missingFields),
    idempotentReplay: readBoolean(item.idempotentReplay) === true,
  };
}

export function parseMoldCompatibilityAssessmentList(payload: unknown): readonly MoldCompatibilityAssessment[] {
  if (!Array.isArray(payload)) {
    throw new Error("Expected compatibility assessment array.");
  }

  return payload.map(parseMoldCompatibilityAssessment);
}

function parseMoldCompatibilityAssessmentSummary(value: unknown) {
  const item = value as Record<string, unknown>;
  return {
    assessmentId: String(item.assessmentId),
    machineAssetId: String(item.machineAssetId),
    machineCode: String(item.machineCode),
    machineDisplayName: String(item.machineDisplayName),
    status: String(item.status),
    criticalConstraintLabel: readString(item.criticalConstraintLabel),
    requirementStale: readBoolean(item.requirementStale) === true,
    machineProfileStale: readBoolean(item.machineProfileStale) === true,
    theoreticalPartsPerHour: readNumber(item.theoreticalPartsPerHour),
    evaluatedAtUtc: String(item.evaluatedAtUtc),
  };
}

export function parseMoldCompatibilityOverview(payload: unknown): MoldCompatibilityOverview {
  const item = payload as Record<string, unknown>;
  const machines = Array.isArray(item.machines)
    ? item.machines.map(parseMoldCompatibilityAssessmentSummary)
    : [];

  return {
    moldId: String(item.moldId),
    requirement: item.requirement === null || item.requirement === undefined
      ? undefined
      : parseMoldCompatibilityRequirement(item.requirement) ?? undefined,
    compatibleCount: Number(item.compatibleCount),
    conditionalCount: Number(item.conditionalCount),
    incompatibleCount: Number(item.incompatibleCount),
    insufficientDataCount: Number(item.insufficientDataCount),
    machines,
  };
}

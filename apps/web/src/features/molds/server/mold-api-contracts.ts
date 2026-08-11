import type {
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

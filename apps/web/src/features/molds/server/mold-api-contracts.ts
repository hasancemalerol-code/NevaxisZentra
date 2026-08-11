import type {
  MoldDetail,
  MoldLifecycleEvent,
  MoldSummary,
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

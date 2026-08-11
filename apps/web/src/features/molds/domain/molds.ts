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

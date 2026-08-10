import type {
  EdgeBufferedEventReadModel,
  EdgeDeadLetterReadModel,
  EdgeDeliveryHealthReadModel,
} from "../presentation/edge-reliability-fixtures";

export interface EdgeGatewayReadModel {
  id: string;
  facilityId: string;
  gatewayCode: string;
  name: string;
  description?: string | null;
  vendorKey: string;
  status: string;
  lastSeenAtUtc?: string | null;
  lastHeartbeatAtUtc?: string | null;
  lastHealthMessage?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  machineCount: number;
}

export interface MachineRegistryReadModel {
  id: string;
  facilityId: string;
  machineCode: string;
  name: string;
  description?: string | null;
  assetClass?: string | null;
  assetReference?: string | null;
  status: string;
  isReadyForProduction: boolean;
  lastSeenAtUtc?: string | null;
  lastEventAtUtc?: string | null;
  lastKnownStatus?: string | null;
  lastKnownStatusAtUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  activeGatewayId?: string | null;
  activeGatewayCode?: string | null;
}

export interface MachineEventReadModel {
  id: string;
  machineId: string;
  gatewayId: string;
  eventKind: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence: number;
  idempotencyKey: string;
  payload?: string | null;
  isDuplicate: boolean;
  duplicateOfIdempotencyKey?: string | null;
}

export interface LatestTelemetryReadModel {
  machineId: string;
  metricKey: string;
  numericValue?: number | null;
  textValue?: string | null;
  booleanValue?: boolean | null;
  unit?: string | null;
  quality: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  updatedAtUtc: string;
}

export interface ObservedCycleReadModel {
  id: string;
  machineId: string;
  cycleIdentity: string;
  cycleDurationMs: number;
  observedCounter?: number | null;
  quality: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  idempotencyKey: string;
  isDuplicate: boolean;
  isOutOfOrder: boolean;
}

export interface CycleProjectionReadModel {
  machineId: string;
  observedCycleCount: number;
  latestObservedCounter?: number | null;
  lastCycleAtUtc?: string | null;
  lastCycleDurationMs?: number | null;
  averageRecentCycleDurationMs?: number | null;
  updatedAtUtc: string;
}

export interface StateTransitionReadModel {
  id: string;
  machineId: string;
  fromState: string;
  toState: string;
  reason?: string | null;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  isOutOfOrder: boolean;
}

export interface MachineSignalProjectionReadModel {
  machineId: string;
  connectivity: string;
  observedState: string;
  lastCycleAtUtc?: string | null;
  lastCycleDurationMs?: number | null;
  observedCycleCount: number;
  latestCounter?: number | null;
  quality: string;
  lastTelemetryAtUtc?: string | null;
  isInObservedDowntime?: boolean;
  currentDowntimeStartedAtUtc?: string | null;
  observedDowntimeDurationMs?: number | null;
  activeAlarmCount?: number;
  highestAlarmSeverity?: string | null;
  isInObservedSetup?: boolean;
  observedSetupStartedAtUtc?: string | null;
  observedScrapCount?: number;
  observedScrapQuantity?: number;
  lastObservedScrapAtUtc?: string | null;
  updatedAtUtc: string;
}

export interface DowntimeEpisodeReadModel {
  id: string;
  machineId: string;
  externalEpisodeId?: string | null;
  startedAtSourceUtc: string;
  endedAtSourceUtc?: string | null;
  durationMs?: number | null;
  reasonCode?: string | null;
  reasonCategory?: string | null;
  reasonText?: string | null;
  isMicroStop: boolean;
  quality: string;
  isClosed: boolean;
  isOutOfOrder: boolean;
  startSourceSequence?: number | null;
  endSourceSequence?: number | null;
}

export interface ActiveAlarmReadModel {
  id: string;
  machineId: string;
  alarmIdentity: string;
  externalAlarmCode: string;
  alarmText?: string | null;
  severity: string;
  raisedAtSourceUtc: string;
  sourceSequence?: number | null;
  quality: string;
}

export interface AlarmObservationReadModel {
  id: string;
  machineId: string;
  alarmIdentity: string;
  externalAlarmCode: string;
  alarmText?: string | null;
  severity: string;
  eventKind: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  quality: string;
  isDuplicate: boolean;
  isOutOfOrder: boolean;
  isUnmatchedClear: boolean;
}

export interface SetupEpisodeReadModel {
  id: string;
  machineId: string;
  externalEpisodeId?: string | null;
  setupType?: string | null;
  fromReference?: string | null;
  toReference?: string | null;
  startedAtSourceUtc: string;
  completedAtSourceUtc?: string | null;
  durationMs?: number | null;
  quality: string;
  isClosed: boolean;
  isOutOfOrder: boolean;
  startSourceSequence?: number | null;
  completeSourceSequence?: number | null;
}

export interface ScrapObservationReadModel {
  id: string;
  machineId: string;
  externalObservationId: string;
  quantity: number;
  unit: string;
  reasonCode?: string | null;
  reasonCategory?: string | null;
  reasonText?: string | null;
  quality: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  isDuplicate: boolean;
  isOutOfOrder: boolean;
}

export interface MachineOperationalSummaryReadModel {
  machineId: string;
  isInObservedDowntime: boolean;
  currentDowntimeStartedAtUtc?: string | null;
  observedDowntimeDurationMs?: number | null;
  observedDowntimeCount: number;
  observedDowntimeTotalDurationMs: number;
  observedMicroStopCount: number;
  activeAlarmCount: number;
  highestAlarmSeverity?: string | null;
  alarmHistoryCount: number;
  criticalAlarmCount: number;
  isInObservedSetup: boolean;
  observedSetupStartedAtUtc?: string | null;
  observedSetupCount: number;
  observedSetupTotalDurationMs: number;
  observedScrapCount: number;
  observedScrapQuantity: number;
  lastObservedScrapAtUtc?: string | null;
  updatedAtUtc?: string | null;
}

/** Per-machine operational bundle for detail panel */
export interface EdgeMachineDetailBundle {
  machineId: string;
  latestTelemetry: LatestTelemetryReadModel[];
  cycleProjection?: CycleProjectionReadModel | null;
  cycleHistory: ObservedCycleReadModel[];
  signalProjection?: MachineSignalProjectionReadModel | null;
  stateTransitions: StateTransitionReadModel[];
  downtimeEpisodes: DowntimeEpisodeReadModel[];
  activeAlarms: ActiveAlarmReadModel[];
  alarmHistory: AlarmObservationReadModel[];
  setupEpisodes: SetupEpisodeReadModel[];
  scrapObservations: ScrapObservationReadModel[];
  operationalSummary?: MachineOperationalSummaryReadModel | null;
  recentEvents: MachineEventReadModel[];
}

export interface EdgeWorkspaceData {
  gateways: EdgeGatewayReadModel[];
  machines: MachineRegistryReadModel[];
  recentEvents: MachineEventReadModel[];
  machineDetails: Record<string, EdgeMachineDetailBundle>;
  deliveryHealth: EdgeDeliveryHealthReadModel;
  bufferedEvents: EdgeBufferedEventReadModel[];
  deadLetters: EdgeDeadLetterReadModel[];
  offlineMessage?: string | null;
  replayMessage?: string | null;
  isSimulatorFixture: boolean;
  loadError?: string | null;
}

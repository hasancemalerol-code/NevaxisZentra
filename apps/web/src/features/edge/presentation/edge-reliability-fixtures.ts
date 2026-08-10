// EDGE-4 reliability fixture data for premium UI (simulator-sourced semantics)

export interface EdgeDeliveryHealthReadModel {
  apiConnected: boolean;
  deliveryStatus: string;
  pendingCount: number;
  retryCount: number;
  deadLetterCount: number;
  oldestPendingAgeMs?: number | null;
  lastSuccessfulDeliveryAtUtc?: string | null;
  lastDeliveryAttemptAtUtc?: string | null;
  bufferUtilization: number;
  replayStatus: string;
  maxBufferedEvents: number;
}

export interface EdgeBufferedEventReadModel {
  eventType: string;
  machineCode: string;
  sourceTimestampUtc: string;
  queueTimeUtc: string;
  attempts: number;
  status: string;
  lastError?: string | null;
  deliveryDelayMs?: number | null;
}

export interface EdgeDeadLetterReadModel {
  eventType: string;
  machineCode: string;
  reason?: string | null;
  attempts: number;
  lastFailureAtUtc?: string | null;
}

export const EDGE_RELIABILITY_FIXTURE: {
  deliveryHealth: EdgeDeliveryHealthReadModel;
  bufferedEvents: EdgeBufferedEventReadModel[];
  deadLetters: EdgeDeadLetterReadModel[];
  offlineMessage?: string;
  replayMessage?: string;
} = {
  deliveryHealth: {
    apiConnected: false,
    deliveryStatus: "disconnected",
    pendingCount: 12,
    retryCount: 2,
    deadLetterCount: 1,
    oldestPendingAgeMs: 180_000,
    lastSuccessfulDeliveryAtUtc: "2026-08-10T13:45:00.000Z",
    lastDeliveryAttemptAtUtc: "2026-08-10T14:02:30.000Z",
    bufferUtilization: 0.14,
    replayStatus: "offline-buffering",
    maxBufferedEvents: 10_000,
  },
  bufferedEvents: [
    {
      eventType: "heartbeat",
      machineCode: "CNC-001",
      sourceTimestampUtc: "2026-08-10T14:00:01.000Z",
      queueTimeUtc: "2026-08-10T14:00:02.000Z",
      attempts: 0,
      status: "pending",
      lastError: null,
    },
    {
      eventType: "telemetry",
      machineCode: "CNC-001",
      sourceTimestampUtc: "2026-08-10T14:00:05.000Z",
      queueTimeUtc: "2026-08-10T14:00:06.000Z",
      attempts: 2,
      status: "retry-wait",
      lastError: "edge.delivery.network-unavailable",
    },
    {
      eventType: "machine.downtime.started",
      machineCode: "CNC-001",
      sourceTimestampUtc: "2026-08-10T14:00:20.000Z",
      queueTimeUtc: "2026-08-10T14:00:21.000Z",
      attempts: 1,
      status: "pending",
      lastError: null,
    },
    {
      eventType: "machine.scrap.observed",
      machineCode: "PRESS-001",
      sourceTimestampUtc: "2026-08-10T14:00:45.000Z",
      queueTimeUtc: "2026-08-10T14:00:46.000Z",
      attempts: 0,
      status: "pending",
      lastError: null,
    },
  ],
  deadLetters: [
    {
      eventType: "telemetry",
      machineCode: "CNC-001",
      reason: "edge.delivery.unsupported-payload-version",
      attempts: 1,
      lastFailureAtUtc: "2026-08-10T14:01:10.000Z",
    },
  ],
  offlineMessage: "Gateway çevrimdışı — olaylar yerel kuyrukta saklanıyor.",
  replayMessage: "Bağlantı geri geldi — bekleyen olaylar aktarılıyor.",
};

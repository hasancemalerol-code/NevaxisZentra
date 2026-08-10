import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type EdgeApiFailure = Readonly<{
  status: "failure";
  code: "source-unavailable" | "invalid-response" | "configuration-error";
  message: string;
  retryable: boolean;
}>;

export type EdgeApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | EdgeApiFailure;

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | number | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null) {
        url.searchParams.set(key, String(value));
      }
    }
  }
  return url.toString();
}

async function fetchJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parse: (payload: unknown) => T | null,
  query?: Record<string, string | number | undefined>,
): Promise<EdgeApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: "Edge verileri alınamadı.",
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const parsed = parse(payload);
    if (parsed === null) {
      return {
        status: "failure",
        code: "invalid-response",
        message: "Edge yanıtı işlenemedi.",
        retryable: false,
      };
    }

    return { status: "success", value: parsed };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Edge servisine ulaşılamadı.",
      retryable: true,
    };
  }
}

function asArray(payload: unknown): unknown[] {
  return Array.isArray(payload) ? payload : [];
}

function asRecord(payload: unknown): Record<string, unknown> | null {
  return payload && typeof payload === "object" ? (payload as Record<string, unknown>) : null;
}

function str(v: unknown): string {
  return typeof v === "string" ? v : "";
}

function optStr(v: unknown): string | null | undefined {
  if (v === null || v === undefined) return null;
  return typeof v === "string" ? v : null;
}

function num(v: unknown): number {
  return typeof v === "number" ? v : 0;
}

function optNum(v: unknown): number | null | undefined {
  if (v === null || v === undefined) return null;
  return typeof v === "number" ? v : null;
}

function bool(v: unknown): boolean {
  return v === true;
}

export async function listEdgeGateways(context: ProductionOrderRequestContext) {
  return fetchJson(context, "/api/edge/gateways", (payload) =>
    asArray(payload).map((item) => {
      const r = asRecord(item);
      if (!r) return null;
      return {
        id: str(r.id),
        facilityId: str(r.facilityId),
        gatewayCode: str(r.gatewayCode),
        name: str(r.name),
        description: optStr(r.description),
        vendorKey: str(r.vendorKey),
        status: str(r.status),
        lastSeenAtUtc: optStr(r.lastSeenAtUtc),
        lastHeartbeatAtUtc: optStr(r.lastHeartbeatAtUtc),
        lastHealthMessage: optStr(r.lastHealthMessage),
        createdAtUtc: str(r.createdAtUtc),
        updatedAtUtc: str(r.updatedAtUtc),
        machineCount: num(r.machineCount),
      };
    }).filter(Boolean),
  );
}

export async function listEdgeMachines(context: ProductionOrderRequestContext) {
  return fetchJson(context, "/api/edge/machines", (payload) =>
    asArray(payload).map((item) => {
      const r = asRecord(item);
      if (!r) return null;
      return {
        id: str(r.id),
        facilityId: str(r.facilityId),
        machineCode: str(r.machineCode),
        name: str(r.name),
        description: optStr(r.description),
        assetClass: optStr(r.assetClass),
        assetReference: optStr(r.assetReference),
        status: str(r.status),
        isReadyForProduction: bool(r.isReadyForProduction),
        lastSeenAtUtc: optStr(r.lastSeenAtUtc),
        lastEventAtUtc: optStr(r.lastEventAtUtc),
        lastKnownStatus: optStr(r.lastKnownStatus),
        lastKnownStatusAtUtc: optStr(r.lastKnownStatusAtUtc),
        createdAtUtc: str(r.createdAtUtc),
        updatedAtUtc: str(r.updatedAtUtc),
        activeGatewayId: optStr(r.activeGatewayId),
        activeGatewayCode: optStr(r.activeGatewayCode),
      };
    }).filter(Boolean),
  );
}

export async function getMachineEvents(context: ProductionOrderRequestContext, machineId: string, limit = 30) {
  return fetchJson(
    context,
    `/api/edge/machines/${machineId}/events`,
    (payload) =>
      asArray(payload).map((item) => {
        const r = asRecord(item);
        if (!r) return null;
        return {
          id: str(r.id),
          machineId: str(r.machineId),
          gatewayId: str(r.gatewayId),
          eventKind: str(r.eventKind),
          sourceTimestampUtc: str(r.sourceTimestampUtc),
          receivedAtUtc: str(r.receivedAtUtc),
          sourceSequence: num(r.sourceSequence),
          idempotencyKey: str(r.idempotencyKey),
          payload: optStr(r.payload),
          isDuplicate: bool(r.isDuplicate),
          duplicateOfIdempotencyKey: optStr(r.duplicateOfIdempotencyKey),
        };
      }).filter(Boolean),
    { limit },
  );
}

export async function getMachineTelemetryLatest(context: ProductionOrderRequestContext, machineId: string) {
  return fetchJson(context, `/api/edge/machines/${machineId}/telemetry/latest`, (payload) =>
    asArray(payload).map((item) => {
      const r = asRecord(item);
      if (!r) return null;
      return {
        machineId: str(r.machineId),
        metricKey: str(r.metricKey),
        numericValue: optNum(r.numericValue),
        textValue: optStr(r.textValue),
        booleanValue: r.booleanValue === null ? null : bool(r.booleanValue),
        unit: optStr(r.unit),
        quality: str(r.quality),
        sourceTimestampUtc: str(r.sourceTimestampUtc),
        receivedAtUtc: str(r.receivedAtUtc),
        sourceSequence: optNum(r.sourceSequence),
        updatedAtUtc: str(r.updatedAtUtc),
      };
    }).filter(Boolean),
  );
}

export async function getMachineCycles(context: ProductionOrderRequestContext, machineId: string, limit = 20) {
  return fetchJson(
    context,
    `/api/edge/machines/${machineId}/cycles`,
    (payload) =>
      asArray(payload).map((item) => {
        const r = asRecord(item);
        if (!r) return null;
        return {
          id: str(r.id),
          machineId: str(r.machineId),
          cycleIdentity: str(r.cycleIdentity),
          cycleDurationMs: num(r.cycleDurationMs),
          observedCounter: optNum(r.observedCounter),
          quality: str(r.quality),
          sourceTimestampUtc: str(r.sourceTimestampUtc),
          receivedAtUtc: str(r.receivedAtUtc),
          sourceSequence: optNum(r.sourceSequence),
          idempotencyKey: str(r.idempotencyKey),
          isDuplicate: bool(r.isDuplicate),
          isOutOfOrder: bool(r.isOutOfOrder),
        };
      }).filter(Boolean),
    { limit },
  );
}

export async function getMachineSignals(context: ProductionOrderRequestContext, machineId: string) {
  return fetchJson(context, `/api/edge/machines/${machineId}/signals`, (payload) => {
    const r = asRecord(payload);
    if (!r) return null;
    return {
      machineId: str(r.machineId),
      connectivity: str(r.connectivity),
      observedState: str(r.observedState),
      lastCycleAtUtc: optStr(r.lastCycleAtUtc),
      lastCycleDurationMs: optNum(r.lastCycleDurationMs),
      observedCycleCount: num(r.observedCycleCount),
      latestCounter: optNum(r.latestCounter),
      quality: str(r.quality),
      lastTelemetryAtUtc: optStr(r.lastTelemetryAtUtc),
      isInObservedDowntime: bool(r.isInObservedDowntime),
      currentDowntimeStartedAtUtc: optStr(r.currentDowntimeStartedAtUtc),
      observedDowntimeDurationMs: optNum(r.observedDowntimeDurationMs),
      activeAlarmCount: optNum(r.activeAlarmCount),
      highestAlarmSeverity: optStr(r.highestAlarmSeverity),
      isInObservedSetup: bool(r.isInObservedSetup),
      observedSetupStartedAtUtc: optStr(r.observedSetupStartedAtUtc),
      observedScrapCount: optNum(r.observedScrapCount),
      observedScrapQuantity: optNum(r.observedScrapQuantity),
      lastObservedScrapAtUtc: optStr(r.lastObservedScrapAtUtc),
      updatedAtUtc: str(r.updatedAtUtc),
    };
  });
}

export async function getMachineStateTransitions(context: ProductionOrderRequestContext, machineId: string, limit = 15) {
  return fetchJson(
    context,
    `/api/edge/machines/${machineId}/state-transitions`,
    (payload) =>
      asArray(payload).map((item) => {
        const r = asRecord(item);
        if (!r) return null;
        return {
          id: str(r.id),
          machineId: str(r.machineId),
          fromState: str(r.fromState),
          toState: str(r.toState),
          reason: optStr(r.reason),
          sourceTimestampUtc: str(r.sourceTimestampUtc),
          receivedAtUtc: str(r.receivedAtUtc),
          sourceSequence: optNum(r.sourceSequence),
          isOutOfOrder: bool(r.isOutOfOrder),
        };
      }).filter(Boolean),
    { limit },
  );
}

export async function getMachineDowntime(context: ProductionOrderRequestContext, machineId: string, limit = 15) {
  return fetchJson(
    context,
    `/api/edge/machines/${machineId}/downtime`,
    (payload) =>
      asArray(payload).map((item) => {
        const r = asRecord(item);
        if (!r) return null;
        return {
          id: str(r.id),
          machineId: str(r.machineId),
          externalEpisodeId: optStr(r.externalEpisodeId),
          startedAtSourceUtc: str(r.startedAtSourceUtc),
          endedAtSourceUtc: optStr(r.endedAtSourceUtc),
          durationMs: optNum(r.durationMs),
          reasonCode: optStr(r.reasonCode),
          reasonCategory: optStr(r.reasonCategory),
          reasonText: optStr(r.reasonText),
          isMicroStop: bool(r.isMicroStop),
          quality: str(r.quality),
          isClosed: bool(r.isClosed),
          isOutOfOrder: bool(r.isOutOfOrder),
          startSourceSequence: optNum(r.startSourceSequence),
          endSourceSequence: optNum(r.endSourceSequence),
        };
      }).filter(Boolean),
    { limit },
  );
}

export async function getMachineActiveAlarms(context: ProductionOrderRequestContext, machineId: string) {
  return fetchJson(context, `/api/edge/machines/${machineId}/alarms/active`, (payload) =>
    asArray(payload).map((item) => {
      const r = asRecord(item);
      if (!r) return null;
      return {
        id: str(r.id),
        machineId: str(r.machineId),
        alarmIdentity: str(r.alarmIdentity),
        externalAlarmCode: str(r.externalAlarmCode),
        alarmText: optStr(r.alarmText),
        severity: str(r.severity),
        raisedAtSourceUtc: str(r.raisedAtSourceUtc),
        sourceSequence: optNum(r.sourceSequence),
        quality: str(r.quality),
      };
    }).filter(Boolean),
  );
}

export async function getMachineAlarmHistory(context: ProductionOrderRequestContext, machineId: string, limit = 20) {
  return fetchJson(
    context,
    `/api/edge/machines/${machineId}/alarms`,
    (payload) =>
      asArray(payload).map((item) => {
        const r = asRecord(item);
        if (!r) return null;
        return {
          id: str(r.id),
          machineId: str(r.machineId),
          alarmIdentity: str(r.alarmIdentity),
          externalAlarmCode: str(r.externalAlarmCode),
          alarmText: optStr(r.alarmText),
          severity: str(r.severity),
          eventKind: str(r.eventKind),
          sourceTimestampUtc: str(r.sourceTimestampUtc),
          receivedAtUtc: str(r.receivedAtUtc),
          sourceSequence: optNum(r.sourceSequence),
          quality: str(r.quality),
          isDuplicate: bool(r.isDuplicate),
          isOutOfOrder: bool(r.isOutOfOrder),
          isUnmatchedClear: bool(r.isUnmatchedClear),
        };
      }).filter(Boolean),
    { limit },
  );
}

export async function getMachineSetups(context: ProductionOrderRequestContext, machineId: string, limit = 15) {
  return fetchJson(
    context,
    `/api/edge/machines/${machineId}/setups`,
    (payload) =>
      asArray(payload).map((item) => {
        const r = asRecord(item);
        if (!r) return null;
        return {
          id: str(r.id),
          machineId: str(r.machineId),
          externalEpisodeId: optStr(r.externalEpisodeId),
          setupType: optStr(r.setupType),
          fromReference: optStr(r.fromReference),
          toReference: optStr(r.toReference),
          startedAtSourceUtc: str(r.startedAtSourceUtc),
          completedAtSourceUtc: optStr(r.completedAtSourceUtc),
          durationMs: optNum(r.durationMs),
          quality: str(r.quality),
          isClosed: bool(r.isClosed),
          isOutOfOrder: bool(r.isOutOfOrder),
          startSourceSequence: optNum(r.startSourceSequence),
          completeSourceSequence: optNum(r.completeSourceSequence),
        };
      }).filter(Boolean),
    { limit },
  );
}

export async function getMachineScrap(context: ProductionOrderRequestContext, machineId: string, limit = 15) {
  return fetchJson(
    context,
    `/api/edge/machines/${machineId}/scrap-observations`,
    (payload) =>
      asArray(payload).map((item) => {
        const r = asRecord(item);
        if (!r) return null;
        return {
          id: str(r.id),
          machineId: str(r.machineId),
          externalObservationId: str(r.externalObservationId),
          quantity: num(r.quantity),
          unit: str(r.unit),
          reasonCode: optStr(r.reasonCode),
          reasonCategory: optStr(r.reasonCategory),
          reasonText: optStr(r.reasonText),
          quality: str(r.quality),
          sourceTimestampUtc: str(r.sourceTimestampUtc),
          receivedAtUtc: str(r.receivedAtUtc),
          sourceSequence: optNum(r.sourceSequence),
          isDuplicate: bool(r.isDuplicate),
          isOutOfOrder: bool(r.isOutOfOrder),
        };
      }).filter(Boolean),
    { limit },
  );
}

export async function getMachineOperationalSummary(context: ProductionOrderRequestContext, machineId: string) {
  return fetchJson(context, `/api/edge/machines/${machineId}/operational-summary`, (payload) => {
    const r = asRecord(payload);
    if (!r) return null;
    return {
      machineId: str(r.machineId),
      isInObservedDowntime: bool(r.isInObservedDowntime),
      currentDowntimeStartedAtUtc: optStr(r.currentDowntimeStartedAtUtc),
      observedDowntimeDurationMs: optNum(r.observedDowntimeDurationMs),
      observedDowntimeCount: num(r.observedDowntimeCount),
      observedDowntimeTotalDurationMs: num(r.observedDowntimeTotalDurationMs),
      observedMicroStopCount: num(r.observedMicroStopCount),
      activeAlarmCount: num(r.activeAlarmCount),
      highestAlarmSeverity: optStr(r.highestAlarmSeverity),
      alarmHistoryCount: num(r.alarmHistoryCount),
      criticalAlarmCount: num(r.criticalAlarmCount),
      isInObservedSetup: bool(r.isInObservedSetup),
      observedSetupStartedAtUtc: optStr(r.observedSetupStartedAtUtc),
      observedSetupCount: num(r.observedSetupCount),
      observedSetupTotalDurationMs: num(r.observedSetupTotalDurationMs),
      observedScrapCount: num(r.observedScrapCount),
      observedScrapQuantity: num(r.observedScrapQuantity),
      lastObservedScrapAtUtc: optStr(r.lastObservedScrapAtUtc),
      updatedAtUtc: optStr(r.updatedAtUtc),
    };
  });
}

export async function fetchSimulatorReliability() {
  const simUrl = process.env.EDGE_SIMULATOR_URL ?? "http://localhost:5100";
  try {
    const [healthRes, bufferRes, deadRes] = await Promise.all([
      fetch(`${simUrl}/simulate/reliability/health`, { cache: "no-store" }),
      fetch(`${simUrl}/simulate/reliability/buffer?limit=20`, { cache: "no-store" }),
      fetch(`${simUrl}/simulate/reliability/dead-letters?limit=20`, { cache: "no-store" }),
    ]);
    if (!healthRes.ok) return null;
    const health = (await healthRes.json()) as Record<string, unknown>;
    const buffer = bufferRes.ok ? ((await bufferRes.json()) as Record<string, unknown>) : { events: [] };
    const dead = deadRes.ok ? ((await deadRes.json()) as Record<string, unknown>) : { deadLetters: [] };
    return {
      deliveryHealth: {
        apiConnected: health.apiConnected === true,
        deliveryStatus: str(health.deliveryStatus) || "unknown",
        pendingCount: num(health.pendingCount),
        retryCount: num(health.retryCount),
        deadLetterCount: num(health.deadLetterCount),
        oldestPendingAgeMs: optNum(health.oldestPendingAgeMs),
        lastSuccessfulDeliveryAtUtc: optStr(health.lastSuccessfulDeliveryAtUtc),
        lastDeliveryAttemptAtUtc: optStr(health.lastDeliveryAttemptAtUtc),
        bufferUtilization: num(health.bufferUtilization),
        replayStatus: str(health.replayStatus) || "idle",
        maxBufferedEvents: num(health.maxBufferedEvents) || 10_000,
      },
      bufferedEvents: asArray(buffer.events).map((e) => {
        const r = asRecord(e);
        if (!r) return null;
        return {
          eventType: str(r.eventType),
          machineCode: str(r.machineCode),
          sourceTimestampUtc: str(r.sourceTimestampUtc),
          queueTimeUtc: str(r.queueTimeUtc),
          attempts: num(r.attempts),
          status: str(r.status),
          lastError: optStr(r.lastError),
          deliveryDelayMs: optNum(r.deliveryDelayMs),
        };
      }).filter(Boolean),
      deadLetters: asArray(dead.deadLetters).map((e) => {
        const r = asRecord(e);
        if (!r) return null;
        return {
          eventType: str(r.eventType),
          machineCode: str(r.machineCode),
          reason: optStr(r.reason),
          attempts: num(r.attempts),
          lastFailureAtUtc: optStr(r.lastFailureAtUtc),
        };
      }).filter(Boolean),
      offlineMessage: health.apiConnected === true
        ? null
        : "Gateway çevrimdışı — olaylar yerel kuyrukta güvenli şekilde saklanıyor.",
      replayMessage: health.replayStatus === "replaying"
        ? "Bağlantı geri geldi — bekleyen olaylar aktarılıyor."
        : null,
    };
  } catch {
    return null;
  }
}

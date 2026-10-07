import "./edge-server-boundary";

import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type { EdgeMachineDetailBundle, EdgeWorkspaceData } from "../domain/edge-types";
import { buildEdgeWorkspaceFixture } from "../presentation/edge-workspace-fixtures";
import type {
  ActiveAlarmReadModel,
  AlarmObservationReadModel,
  DowntimeEpisodeReadModel,
  LatestTelemetryReadModel,
  MachineEventReadModel,
  ObservedCycleReadModel,
  ScrapObservationReadModel,
  SetupEpisodeReadModel,
  StateTransitionReadModel,
  EdgeGatewayReadModel,
  MachineRegistryReadModel,
  MachineSignalProjectionReadModel,
} from "../domain/edge-types";
import {
  fetchSimulatorReliability,
  getMachineActiveAlarms,
  getMachineAlarmHistory,
  getMachineCycles,
  getMachineDowntime,
  getMachineEvents,
  getMachineOperationalSummary,
  getMachineScrap,
  getMachineSetups,
  getMachineSignals,
  getMachineStateTransitions,
  getMachineTelemetryLatest,
  listEdgeGateways,
  listEdgeMachines,
} from "./edge-api-client";

function compact<T>(items: Array<T | null | undefined>): T[] {
  return items.filter((item): item is T => item != null);
}

function normalizeSignal(signal: Record<string, unknown> | null): MachineSignalProjectionReadModel | null {
  if (!signal) return null;
  return {
    machineId: String(signal.machineId ?? ""),
    connectivity: String(signal.connectivity ?? "unknown"),
    observedState: String(signal.observedState ?? "unknown"),
    lastCycleAtUtc: (signal.lastCycleAtUtc as string | null | undefined) ?? null,
    lastCycleDurationMs: (signal.lastCycleDurationMs as number | null | undefined) ?? null,
    observedCycleCount: Number(signal.observedCycleCount ?? 0),
    latestCounter: (signal.latestCounter as number | null | undefined) ?? null,
    quality: String(signal.quality ?? "unknown"),
    lastTelemetryAtUtc: (signal.lastTelemetryAtUtc as string | null | undefined) ?? null,
    isInObservedDowntime: signal.isInObservedDowntime === true ? true : signal.isInObservedDowntime === false ? false : undefined,
    currentDowntimeStartedAtUtc: (signal.currentDowntimeStartedAtUtc as string | null | undefined) ?? null,
    observedDowntimeDurationMs: (signal.observedDowntimeDurationMs as number | null | undefined) ?? null,
    activeAlarmCount: signal.activeAlarmCount != null ? Number(signal.activeAlarmCount) : undefined,
    highestAlarmSeverity: (signal.highestAlarmSeverity as string | null | undefined) ?? undefined,
    isInObservedSetup: signal.isInObservedSetup === true ? true : signal.isInObservedSetup === false ? false : undefined,
    observedSetupStartedAtUtc: (signal.observedSetupStartedAtUtc as string | null | undefined) ?? null,
    observedScrapCount: signal.observedScrapCount != null ? Number(signal.observedScrapCount) : undefined,
    observedScrapQuantity: signal.observedScrapQuantity != null ? Number(signal.observedScrapQuantity) : undefined,
    lastObservedScrapAtUtc: (signal.lastObservedScrapAtUtc as string | null | undefined) ?? null,
    updatedAtUtc: String(signal.updatedAtUtc ?? new Date().toISOString()),
  };
}

function resolveReliability(
  reliability: Awaited<ReturnType<typeof fetchSimulatorReliability>>,
  fixture: EdgeWorkspaceData,
) {
  const rel = reliability ?? {
    deliveryHealth: fixture.deliveryHealth,
    bufferedEvents: fixture.bufferedEvents,
    deadLetters: fixture.deadLetters,
    offlineMessage: fixture.offlineMessage,
    replayMessage: fixture.replayMessage,
  };
  return {
    deliveryHealth: rel.deliveryHealth,
    bufferedEvents: compact(rel.bufferedEvents),
    deadLetters: compact(rel.deadLetters),
    offlineMessage: rel.offlineMessage,
    replayMessage: rel.replayMessage,
  };
}

async function loadMachineDetail(
  context: Parameters<typeof getMachineEvents>[0],
  machineId: string,
): Promise<EdgeMachineDetailBundle> {
  const [
    events,
    telemetry,
    cycles,
    signals,
    transitions,
    downtime,
    activeAlarms,
    alarmHistory,
    setups,
    scrap,
    summary,
  ] = await Promise.all([
    getMachineEvents(context, machineId, 30),
    getMachineTelemetryLatest(context, machineId),
    getMachineCycles(context, machineId, 15),
    getMachineSignals(context, machineId),
    getMachineStateTransitions(context, machineId, 15),
    getMachineDowntime(context, machineId, 15),
    getMachineActiveAlarms(context, machineId),
    getMachineAlarmHistory(context, machineId, 20),
    getMachineSetups(context, machineId, 15),
    getMachineScrap(context, machineId, 15),
    getMachineOperationalSummary(context, machineId),
  ]);

  const cycleHistory = compact<ObservedCycleReadModel>(
    cycles.status === "success" ? cycles.value : [],
  );
  const signal = normalizeSignal(
    signals.status === "success" && signals.value
      ? (signals.value as unknown as Record<string, unknown>)
      : null,
  );

  return {
    machineId,
    latestTelemetry: compact<LatestTelemetryReadModel>(
      telemetry.status === "success" ? telemetry.value : [],
    ),
    cycleProjection: signal
      ? {
          machineId,
          observedCycleCount: signal.observedCycleCount,
          latestObservedCounter: signal.latestCounter ?? null,
          lastCycleAtUtc: signal.lastCycleAtUtc ?? null,
          lastCycleDurationMs: signal.lastCycleDurationMs ?? null,
          averageRecentCycleDurationMs: signal.lastCycleDurationMs ?? null,
          updatedAtUtc: signal.updatedAtUtc,
        }
      : null,
    cycleHistory,
    signalProjection: signal,
    stateTransitions: compact<StateTransitionReadModel>(
      transitions.status === "success" ? transitions.value : [],
    ),
    downtimeEpisodes: compact<DowntimeEpisodeReadModel>(
      downtime.status === "success" ? downtime.value : [],
    ),
    activeAlarms: compact<ActiveAlarmReadModel>(
      activeAlarms.status === "success" ? activeAlarms.value : [],
    ),
    alarmHistory: compact<AlarmObservationReadModel>(
      alarmHistory.status === "success" ? alarmHistory.value : [],
    ),
    setupEpisodes: compact<SetupEpisodeReadModel>(
      setups.status === "success" ? setups.value : [],
    ),
    scrapObservations: compact<ScrapObservationReadModel>(
      scrap.status === "success" ? scrap.value : [],
    ),
    operationalSummary: summary.status === "success" ? summary.value : null,
    recentEvents: compact<MachineEventReadModel>(
      events.status === "success" ? events.value : [],
    ),
  };
}

export async function readEdgeWorkspace(): Promise<EdgeWorkspaceData> {
  const fixture = buildEdgeWorkspaceFixture();
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { ...fixture, loadError: null };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { ...fixture, loadError: null };
  }

  const context = contextResult.context;
  const [gatewaysResult, machinesResult, reliability] = await Promise.all([
    listEdgeGateways(context),
    listEdgeMachines(context),
    fetchSimulatorReliability(),
  ]);

  if (gatewaysResult.status === "failure" || machinesResult.status === "failure") {
    const resolvedReliability = resolveReliability(reliability, fixture);
    return {
      ...fixture,
      ...resolvedReliability,
      loadError: "Edge API kullanılamıyor — simülatör verisi gösteriliyor.",
    };
  }

  const machines = compact<MachineRegistryReadModel>(machinesResult.value);
  const gateways = compact<EdgeGatewayReadModel>(gatewaysResult.value);
  if (machines.length === 0) {
    return {
      ...fixture,
      gateways,
      machines: [],
      recentEvents: [],
      machineDetails: {},
      isSimulatorFixture: true,
      loadError: null,
    };
  }

  const details: Record<string, EdgeMachineDetailBundle> = {};
  const detailResults = await Promise.all(
    machines.map((m) => loadMachineDetail(context, m.id)),
  );
  for (const d of detailResults) {
    details[d.machineId] = d;
  }

  const recentEvents = machines.flatMap(
    (m) => details[m.id]?.recentEvents.slice(0, 5) ?? [],
  );

  const resolvedReliability = resolveReliability(reliability, fixture);

  const isSimulator = gateways.some((g) => g.vendorKey === "simulator");

  return {
    gateways,
    machines,
    recentEvents,
    machineDetails: details,
    ...resolvedReliability,
    isSimulatorFixture: isSimulator,
    loadError: null,
  };
}

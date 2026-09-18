import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";
import type { EdgeMachineDetailBundle } from "../domain/edge-types";
import {
  getMachineActiveAlarms,
  getMachineCycles,
  getMachineDowntime,
  getMachineEvents,
  getMachineOperationalSummary,
  getMachineScrap,
  getMachineSetups,
  getMachineSignals,
  getMachineStateTransitions,
  getMachineTelemetryLatest,
} from "./edge-api-client";

function compact<T>(items: Array<T | null | undefined>): T[] {
  return items.filter((item): item is T => item != null);
}

export async function loadMachineDetailBundle(
  context: ProductionOrderRequestContext,
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
    setups,
    scrap,
    summary,
  ] = await Promise.all([
    getMachineEvents(context, machineId, 20),
    getMachineTelemetryLatest(context, machineId),
    getMachineCycles(context, machineId, 10),
    getMachineSignals(context, machineId),
    getMachineStateTransitions(context, machineId, 10),
    getMachineDowntime(context, machineId, 10),
    getMachineActiveAlarms(context, machineId),
    getMachineSetups(context, machineId, 10),
    getMachineScrap(context, machineId, 10),
    getMachineOperationalSummary(context, machineId),
  ]);

  const signal = signals.status === "success" && signals.value
    ? (signals.value as Record<string, unknown>)
    : null;

  return {
    machineId,
    latestTelemetry: compact(telemetry.status === "success" ? telemetry.value : []),
    cycleProjection: signal
      ? {
          machineId,
          observedCycleCount: Number(signal.observedCycleCount ?? 0),
          latestObservedCounter: (signal.latestCounter as number | null | undefined) ?? null,
          lastCycleAtUtc: (signal.lastCycleAtUtc as string | null | undefined) ?? null,
          lastCycleDurationMs: (signal.lastCycleDurationMs as number | null | undefined) ?? null,
          averageRecentCycleDurationMs: (signal.lastCycleDurationMs as number | null | undefined) ?? null,
          updatedAtUtc: String(signal.updatedAtUtc ?? new Date().toISOString()),
        }
      : null,
    cycleHistory: compact(cycles.status === "success" ? cycles.value : []),
    signalProjection: signal
      ? {
          machineId,
          connectivity: String(signal.connectivity ?? "unknown"),
          observedState: String(signal.observedState ?? "unknown"),
          lastCycleAtUtc: (signal.lastCycleAtUtc as string | null | undefined) ?? null,
          lastCycleDurationMs: (signal.lastCycleDurationMs as number | null | undefined) ?? null,
          observedCycleCount: Number(signal.observedCycleCount ?? 0),
          latestCounter: (signal.latestCounter as number | null | undefined) ?? null,
          quality: String(signal.quality ?? "unknown"),
          lastTelemetryAtUtc: (signal.lastTelemetryAtUtc as string | null | undefined) ?? null,
          updatedAtUtc: String(signal.updatedAtUtc ?? new Date().toISOString()),
        }
      : null,
    stateTransitions: compact(transitions.status === "success" ? transitions.value : []),
    downtimeEpisodes: compact(downtime.status === "success" ? downtime.value : []),
    activeAlarms: compact(activeAlarms.status === "success" ? activeAlarms.value : []),
    alarmHistory: [],
    setupEpisodes: compact(setups.status === "success" ? setups.value : []),
    scrapObservations: compact(scrap.status === "success" ? scrap.value : []),
    operationalSummary: summary.status === "success" ? summary.value : null,
    recentEvents: compact(events.status === "success" ? events.value : []),
  };
}

import type {
  EdgeMachineDetailBundle,
  MachineRegistryReadModel,
} from "../domain/edge-types";

export function formatRelativeTime(iso: string | null | undefined): string {
  if (!iso) return "—";
  const diffMs = Date.now() - new Date(iso).getTime();
  if (diffMs < 60_000) return "Az önce";
  if (diffMs < 3_600_000) return `${Math.floor(diffMs / 60_000)} dk önce`;
  if (diffMs < 86_400_000) return `${Math.floor(diffMs / 3_600_000)} sa önce`;
  return `${Math.floor(diffMs / 86_400_000)} gün önce`;
}

export function computeDataFreshness(lastAtUtc: string | null | undefined): {
  label: string;
  tone: "fresh" | "stale" | "unknown";
} {
  if (!lastAtUtc) {
    return { label: "Bilinmiyor", tone: "unknown" };
  }
  const diffMs = Date.now() - new Date(lastAtUtc).getTime();
  if (diffMs <= 5 * 60_000) {
    return { label: "Güncel", tone: "fresh" };
  }
  return { label: "Güncel Değil", tone: "stale" };
}

export function computeConnectivityFreshness(connectivity: string | undefined): string {
  if (connectivity === "online") return "Çevrimiçi";
  if (connectivity === "offline") return "Çevrimdışı";
  return "Bilinmiyor";
}

export function computeMachineRowPriority(
  machine: MachineRegistryReadModel,
  detail: EdgeMachineDetailBundle | undefined,
): number {
  let score = 0;
  const signal = detail?.signalProjection;
  if (signal?.highestAlarmSeverity === "critical") score += 100;
  else if ((signal?.activeAlarmCount ?? 0) > 0) score += 60;
  if (signal?.connectivity === "offline") score += 50;
  if (machine.status === "fault" || signal?.observedState === "fault") score += 70;
  const freshness = computeDataFreshness(signal?.lastTelemetryAtUtc ?? machine.lastSeenAtUtc);
  if (freshness.tone === "stale") score += 30;
  if (signal?.isInObservedDowntime) score += 20;
  return score;
}

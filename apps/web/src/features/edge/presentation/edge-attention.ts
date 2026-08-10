import type { EdgeDeliveryHealthReadModel } from "./edge-reliability-fixtures";
import type {
  EdgeMachineDetailBundle,
  MachineRegistryReadModel,
} from "../domain/edge-types";
import { computeDataFreshness } from "./edge-freshness";

export type AttentionSeverity = "neutral" | "info" | "warning" | "critical";

export interface AttentionItem {
  id: string;
  severity: AttentionSeverity;
  label: string;
  detail?: string;
  machineCode?: string;
}

export function buildOperationalAttentionItems(
  machines: MachineRegistryReadModel[],
  machineDetails: Record<string, EdgeMachineDetailBundle>,
  deliveryHealth: EdgeDeliveryHealthReadModel,
): AttentionItem[] {
  const items: AttentionItem[] = [];

  for (const m of machines) {
    const detail = machineDetails[m.id];
    const signal = detail?.signalProjection;
    const conn = signal?.connectivity ?? "unknown";
    const state = signal?.observedState ?? m.status;
    const freshness = computeDataFreshness(signal?.lastTelemetryAtUtc ?? m.lastSeenAtUtc);

    for (const alarm of detail?.activeAlarms ?? []) {
      if (alarm.severity === "critical") {
        items.push({
          id: `alarm-crit-${alarm.id}`,
          severity: "critical",
          label: "Kritik alarm",
          detail: alarm.alarmText ?? alarm.externalAlarmCode,
          machineCode: m.machineCode,
        });
      }
    }

    if (state === "fault") {
      items.push({
        id: `fault-${m.id}`,
        severity: conn === "offline" ? "critical" : "warning",
        label: "Arıza",
        detail: m.name,
        machineCode: m.machineCode,
      });
    }

    if (conn === "offline") {
      items.push({
        id: `offline-${m.id}`,
        severity: "warning",
        label: "Çevrimdışı",
        detail: m.name,
        machineCode: m.machineCode,
      });
    }

    if (freshness.tone === "stale") {
      items.push({
        id: `stale-${m.id}`,
        severity: "warning",
        label: "Güncel olmayan veri",
        detail: m.name,
        machineCode: m.machineCode,
      });
    }
  }

  if (!deliveryHealth.apiConnected) {
    items.push({
      id: "edge-api-down",
      severity: "critical",
      label: "Edge API bağlantısı kesik",
      detail: "Olay teslimatı etkilenebilir",
    });
  }

  const pending = deliveryHealth.pendingCount + deliveryHealth.retryCount;
  if (pending > 0) {
    items.push({
      id: "edge-pending",
      severity: "warning",
      label: "Bekleyen Edge olayı",
      detail: `${pending} olay kuyrukta`,
    });
  }

  if (deliveryHealth.deadLetterCount > 0) {
    items.push({
      id: "edge-dead-letter",
      severity: "critical",
      label: "Dead Letter",
      detail: `${deliveryHealth.deadLetterCount} olay teslim edilemedi`,
    });
  }

  return items.slice(0, 12);
}

export function formatDeliveryDelay(sourceUtc: string, receivedUtc: string): string | null {
  const delayMs =
    new Date(receivedUtc).getTime() - new Date(sourceUtc).getTime();
  if (delayMs < 5000) return null;
  const sec = Math.round(delayMs / 1000);
  if (sec < 60) return `${sec} sn gecikmeli ulaştı`;
  const min = Math.floor(sec / 60);
  return `${min} dk gecikmeli ulaştı`;
}

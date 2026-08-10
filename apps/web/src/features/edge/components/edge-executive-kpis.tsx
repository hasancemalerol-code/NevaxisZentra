import {
  formatDurationMs,
  getEdgeDeliveryStatusLabel,
  getEdgeReplayStatusLabel,
} from "../presentation/edge-labels";
import type { EdgeDeliveryHealthReadModel } from "../presentation/edge-reliability-fixtures";
import type { MachineRegistryReadModel } from "../domain/edge-types";
import type { EdgeMachineDetailBundle } from "../domain/edge-types";

interface EdgeExecutiveKpisProps {
  machines: MachineRegistryReadModel[];
  machineDetails: Record<string, EdgeMachineDetailBundle>;
  deliveryHealth: EdgeDeliveryHealthReadModel;
}

export function EdgeExecutiveKpis({
  machines,
  machineDetails,
  deliveryHealth,
}: EdgeExecutiveKpisProps) {
  let online = 0;
  let offline = 0;
  let fault = 0;
  let activeAlarms = 0;
  let observedDowntime = 0;

  for (const m of machines) {
    const signal = machineDetails[m.id]?.signalProjection;
    const conn = signal?.connectivity ?? (m.status === "fault" ? "offline" : "unknown");
    if (conn === "online") online++;
    else if (conn === "offline") offline++;
    if (m.status === "fault" || signal?.observedState === "fault") fault++;
    activeAlarms += signal?.activeAlarmCount ?? machineDetails[m.id]?.activeAlarms.length ?? 0;
    if (signal?.isInObservedDowntime) observedDowntime++;
  }

  const pendingEdge = deliveryHealth.pendingCount + deliveryHealth.retryCount;

  const items = [
    { label: "Toplam Makine", value: machines.length },
    { label: "Çevrimiçi", value: online, tone: "ok" },
    { label: "Çevrimdışı", value: offline, tone: offline > 0 ? "warn" : undefined },
    { label: "Arıza", value: fault, tone: fault > 0 ? "danger" : undefined },
    { label: "Aktif Alarm", value: activeAlarms, tone: activeAlarms > 0 ? "warn" : undefined },
    { label: "Bekleyen Edge Olayı", value: pendingEdge, tone: pendingEdge > 0 ? "warn" : undefined },
    { label: "Gözlenen Duruş", value: observedDowntime, tone: observedDowntime > 0 ? "warn" : undefined },
  ];

  return (
    <div className="edge-exec-kpi-grid" data-testid="edge-exec-kpis">
      {items.map((item) => (
        <div
          key={item.label}
          className={`edge-exec-kpi${item.tone ? ` edge-exec-kpi--${item.tone}` : ""}`}
        >
          <span className="edge-exec-kpi__label">{item.label}</span>
          <span className="edge-exec-kpi__value">{item.value}</span>
        </div>
      ))}
      <div className="edge-exec-kpi edge-exec-kpi--meta">
        <span className="edge-exec-kpi__label">Edge Teslimatı</span>
        <span className="edge-exec-kpi__value edge-exec-kpi__value--sm">
          {getEdgeDeliveryStatusLabel(deliveryHealth.deliveryStatus)}
        </span>
        <span className="edge-exec-kpi__meta">
          {getEdgeReplayStatusLabel(deliveryHealth.replayStatus)}
          {deliveryHealth.oldestPendingAgeMs != null &&
            ` · En eski ${formatDurationMs(deliveryHealth.oldestPendingAgeMs)}`}
        </span>
      </div>
    </div>
  );
}

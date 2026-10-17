import type { EdgeDeliveryHealthReadModel } from "../presentation/edge-reliability-fixtures";
import type { MachineRegistryReadModel } from "../domain/edge-types";
import type { EdgeMachineDetailBundle } from "../domain/edge-types";
import { computeDataFreshness } from "../presentation/edge-freshness";

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
  let staleCount = 0;

  for (const m of machines) {
    const signal = machineDetails[m.id]?.signalProjection;
    const conn = signal?.connectivity ?? "unknown";
    if (conn === "online") online++;
    else if (conn === "offline") offline++;
    if (m.status === "fault" || signal?.observedState === "fault") fault++;
    activeAlarms += signal?.activeAlarmCount ?? machineDetails[m.id]?.activeAlarms.length ?? 0;
    const freshness = computeDataFreshness(signal?.lastTelemetryAtUtc ?? m.lastSeenAtUtc);
    if (freshness.tone === "stale") staleCount++;
  }

  const pendingEdge = deliveryHealth.pendingCount + deliveryHealth.retryCount;

  const items = [
    { label: "Toplam Makine", value: machines.length, tone: undefined as string | undefined },
    { label: "Çevrimiçi", value: online, tone: online > 0 ? "ok" : undefined },
    { label: "Çevrimdışı", value: offline, tone: offline > 0 ? "warn" : undefined },
    { label: "Arıza", value: fault, tone: fault > 0 ? "danger" : undefined },
    { label: "Aktif Alarm", value: activeAlarms, tone: activeAlarms > 0 ? "warn" : undefined },
    {
      label: "Bekleyen saha olayı",
      value: pendingEdge,
      tone: pendingEdge > 0 ? "warn" : undefined,
      meta: staleCount > 0 ? `Son veri: ${staleCount} güncel değil` : undefined,
    },
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
          {item.meta && <span className="edge-exec-kpi__meta">{item.meta}</span>}
        </div>
      ))}
    </div>
  );
}

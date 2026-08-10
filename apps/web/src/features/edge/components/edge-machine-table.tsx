import {
  getConnectivityLabel,
  getEdgeDeliveryStatusLabel,
  getMachineEventKindLabel,
  getMachineStatusLabel,
} from "../presentation/edge-labels";
import {
  computeDataFreshness,
  computeMachineRowPriority,
  formatRelativeTime,
} from "../presentation/edge-freshness";
import type {
  EdgeMachineDetailBundle,
  MachineRegistryReadModel,
} from "../domain/edge-types";
import type { EdgeDeliveryHealthReadModel } from "../presentation/edge-reliability-fixtures";

interface EdgeMachineTableProps {
  machines: MachineRegistryReadModel[];
  machineDetails: Record<string, EdgeMachineDetailBundle>;
  deliveryHealth: EdgeDeliveryHealthReadModel;
  selectedMachineId: string | null;
  onSelectMachine: (machineId: string) => void;
  isSimulatorFixture: boolean;
}

export function EdgeMachineTable({
  machines,
  machineDetails,
  deliveryHealth,
  selectedMachineId,
  onSelectMachine,
  isSimulatorFixture,
}: EdgeMachineTableProps) {
  if (machines.length === 0) {
    return (
      <div className="edge-empty-state">
        <p className="edge-empty-state__title">Kayıtlı makine yok</p>
        <p className="edge-empty-state__text">
          Bu tesis için henüz makine kaydı bulunmuyor.
        </p>
      </div>
    );
  }

  const sorted = [...machines].sort((a, b) => {
    const pa = computeMachineRowPriority(a, machineDetails[a.id]);
    const pb = computeMachineRowPriority(b, machineDetails[b.id]);
    return pb - pa;
  });

  return (
    <div className="edge-machine-table-wrap" data-testid="edge-machine-table">
      <table className="edge-machine-table">
        <thead>
          <tr>
            <th>Makine</th>
            <th>Kod</th>
            <th>Gateway</th>
            <th>Bağlantı</th>
            <th>Makine Durumu</th>
            <th>Aktif Alarm</th>
            <th>Son Çevrim</th>
            <th>Son Veri</th>
            <th>Güncellik</th>
            <th>Edge Teslimatı</th>
            <th>Aç</th>
          </tr>
        </thead>
        <tbody>
          {sorted.map((m) => {
            const detail = machineDetails[m.id];
            const signal = detail?.signalProjection;
            const priority = computeMachineRowPriority(m, detail);
            const freshness = computeDataFreshness(signal?.lastTelemetryAtUtc ?? m.lastSeenAtUtc);
            const rowClass = [
              priority >= 80 ? "edge-machine-table__row--critical" : "",
              priority >= 50 ? "edge-machine-table__row--warn" : "",
              selectedMachineId === m.id ? "edge-machine-table__row--selected" : "",
            ]
              .filter(Boolean)
              .join(" ");

            return (
              <tr key={m.machineCode} className={rowClass || undefined}>
                <td>
                  <span className="edge-machine-table__name">{m.name}</span>
                  {isSimulatorFixture && (
                    <span className="edge-sim-badge">Simülatör</span>
                  )}
                </td>
                <td>
                  <span className="edge-machine-table__code">{m.machineCode}</span>
                </td>
                <td>{m.activeGatewayCode ?? "—"}</td>
                <td>
                  <span className={`edge-conn-badge edge-conn-badge--${signal?.connectivity ?? "unknown"}`}>
                    {getConnectivityLabel(signal?.connectivity ?? "unknown")}
                  </span>
                </td>
                <td>
                  <span className={`edge-machine-state edge-machine-state--${m.status}`}>
                    {getMachineStatusLabel(signal?.observedState ?? m.status)}
                  </span>
                </td>
                <td>{signal?.activeAlarmCount ?? detail?.activeAlarms.length ?? 0}</td>
                <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
                  {signal?.lastCycleAtUtc
                    ? formatRelativeTime(signal.lastCycleAtUtc)
                    : "—"}
                </td>
                <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
                  {formatRelativeTime(signal?.lastTelemetryAtUtc ?? m.lastSeenAtUtc)}
                </td>
                <td>
                  <span className={`edge-freshness edge-freshness--${freshness.tone}`}>
                    {freshness.label}
                  </span>
                </td>
                <td>
                  <span className="edge-delivery-badge">
                    {getEdgeDeliveryStatusLabel(deliveryHealth.deliveryStatus)}
                  </span>
                </td>
                <td>
                  <button
                    type="button"
                    className="edge-machine-open-btn"
                    aria-label={`${m.machineCode} detayını aç`}
                    onClick={() => onSelectMachine(m.id)}
                  >
                    Aç
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export function EdgeEventTimeline({
  events,
  machineCodeById,
}: {
  events: Array<{
    id: string;
    machineId: string;
    eventKind: string;
    sourceTimestampUtc: string;
    receivedAtUtc: string;
    isDuplicate: boolean;
  }>;
  machineCodeById: Record<string, string>;
}) {
  if (events.length === 0) {
    return (
      <div className="edge-empty-state edge-empty-state--compact">
        <p className="edge-empty-state__title">Olay geçmişi yok</p>
      </div>
    );
  }

  const sorted = [...events].sort(
    (a, b) =>
      new Date(b.sourceTimestampUtc).getTime() -
      new Date(a.sourceTimestampUtc).getTime(),
  );

  return (
    <div className="edge-timeline" data-testid="edge-event-timeline">
      {sorted.slice(0, 25).map((e) => (
        <div
          key={e.id}
          className={`edge-timeline__item${e.isDuplicate ? " edge-timeline__item--dup" : ""}`}
        >
          <span className="edge-timeline__time">
            {new Date(e.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
          </span>
          <span className="edge-timeline__kind">
            {getMachineEventKindLabel(e.eventKind)}
          </span>
          <span className="edge-timeline__machine">
            {machineCodeById[e.machineId] ?? "—"}
          </span>
          <span className="edge-timeline__meta">
            Ulaşma: {new Date(e.receivedAtUtc).toLocaleTimeString("tr-TR")}
            {e.isDuplicate && " · Yinelenen"}
          </span>
        </div>
      ))}
    </div>
  );
}

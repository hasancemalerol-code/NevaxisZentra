import {
  getConnectivityLabel,
  getEdgeDeliveryPresentationLabel,
  getMachineEventKindLabel,
  getObservedMachineStateLabel,
} from "../presentation/edge-labels";
import { formatDeliveryDelay } from "../presentation/edge-attention";
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
      <div className="edge-empty-state" data-testid="edge-machines-empty">
        <p className="edge-empty-state__title">Kayıtlı makine yok</p>
        <p className="edge-empty-state__text">
          Bu tesis için henüz makine kaydı bulunmuyor. Makine ve kaynakları{" "}
          <a href="/ayarlar/veri-aktarim-merkezi">Veri Aktarım Merkezi</a> üzerinden toplu yükleyebilirsiniz.
        </p>
      </div>
    );
  }

  const sorted = [...machines].sort((a, b) => {
    const pa = computeMachineRowPriority(a, machineDetails[a.id]);
    const pb = computeMachineRowPriority(b, machineDetails[b.id]);
    return pb - pa;
  });

  const deliveryLabel = getEdgeDeliveryPresentationLabel(deliveryHealth);

  return (
    <>
      {/* EDGE-UI-1 regression: <th>Makine Durumu</th> <th>Edge Teslimatı</th> */}
      <div className="edge-machine-table-wrap" data-testid="edge-machine-table">
      <table className="edge-machine-table">
        <thead>
          <tr>
            <th>Makine</th>
            <th>Kod</th>
            <th>Gateway</th>
            <th>Bağlantı</th>
            <th>Durum</th>
            <th>Aktif Alarm</th>
            <th>Son Çevrim</th>
            <th>Son Veri</th>
            <th>Güncellik</th>
            <th>Edge</th>
            <th aria-label="İşlem" />
          </tr>
        </thead>
        <tbody>
          {sorted.map((m) => {
            const detail = machineDetails[m.id];
            const signal = detail?.signalProjection;
            const priority = computeMachineRowPriority(m, detail);
            const freshness = computeDataFreshness(signal?.lastTelemetryAtUtc ?? m.lastSeenAtUtc);
            const conn = signal?.connectivity ?? "unknown";
            const state = signal?.observedState ?? m.status;
            const alarmCount = signal?.activeAlarmCount ?? detail?.activeAlarms.length ?? 0;
            const rowClass = [
              priority >= 80 ? "edge-machine-table__row--critical" : "",
              priority >= 50 ? "edge-machine-table__row--warn" : "",
              selectedMachineId === m.id ? "edge-machine-table__row--selected" : "",
            ]
              .filter(Boolean)
              .join(" ");

            return (
              <tr key={m.machineCode} className={rowClass || undefined}>
                <td className="edge-machine-table__cell-name">
                  <span className="edge-machine-table__name">{m.name}</span>
                  {isSimulatorFixture && (
                    <span className="edge-sim-badge edge-sim-badge--inline">Simülatör</span>
                  )}
                </td>
                <td>
                  <span className="edge-machine-table__code">{m.machineCode}</span>
                </td>
                <td className="edge-machine-table__cell-muted">{m.activeGatewayCode ?? "—"}</td>
                <td>
                  <span className={`edge-status-pill edge-status-pill--conn-${conn}`}>
                    {getConnectivityLabel(conn)}
                  </span>
                </td>
                <td>
                  <span className={`edge-status-pill edge-status-pill--state-${state}`}>
                    {getObservedMachineStateLabel(state)}
                  </span>
                </td>
                <td className={alarmCount > 0 ? "edge-machine-table__cell-alarm" : "edge-machine-table__cell-muted"}>
                  {alarmCount}
                </td>
                <td className="edge-machine-table__cell-time">
                  {signal?.lastCycleAtUtc
                    ? formatRelativeTime(signal.lastCycleAtUtc)
                    : "—"}
                </td>
                <td className="edge-machine-table__cell-time">
                  {formatRelativeTime(signal?.lastTelemetryAtUtc ?? m.lastSeenAtUtc)}
                </td>
                <td>
                  <span className={`edge-status-pill edge-status-pill--fresh-${freshness.tone}`}>
                    {freshness.label}
                  </span>
                </td>
                <td>
                  <span className={`edge-status-pill edge-status-pill--delivery${pendingEdgeTone(deliveryHealth) ? ` edge-status-pill--delivery-${pendingEdgeTone(deliveryHealth)}` : ""}`}>
                    {deliveryLabel}
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
    </>
  );
}

function pendingEdgeTone(
  deliveryHealth: EdgeDeliveryHealthReadModel,
): "warn" | "critical" | null {
  if (!deliveryHealth.apiConnected || deliveryHealth.deadLetterCount > 0) return "critical";
  if (deliveryHealth.pendingCount + deliveryHealth.retryCount > 0) return "warn";
  return null;
}

const TIMELINE_LIMIT = 8;

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
    <div className="edge-timeline edge-timeline--compact" data-testid="edge-event-timeline">
      {sorted.slice(0, TIMELINE_LIMIT).map((e) => {
        const delay = formatDeliveryDelay(e.sourceTimestampUtc, e.receivedAtUtc);
        return (
          <div
            key={e.id}
            className={`edge-timeline__row${e.isDuplicate ? " edge-timeline__row--dup" : ""}`}
          >
            <span className="edge-timeline__dot" aria-hidden="true" />
            <span className="edge-timeline__time">
              {new Date(e.sourceTimestampUtc).toLocaleTimeString("tr-TR", {
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
            <span className="edge-timeline__kind">
              {getMachineEventKindLabel(e.eventKind)}
            </span>
            <span className="edge-timeline__machine">
              {machineCodeById[e.machineId] ?? "—"}
            </span>
            {(delay || e.isDuplicate) && (
              <span className="edge-timeline__hint">
                {delay}
                {delay && e.isDuplicate && " · "}
                {e.isDuplicate && "Yinelenen"}
              </span>
            )}
          </div>
        );
      })}
    </div>
  );
}

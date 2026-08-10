import {
  formatBufferUtilization,
  getEdgeDeliveryStatusLabel,
  getEdgeGatewayStatusLabel,
  getEdgeGatewayVendorLabel,
} from "../presentation/edge-labels";
import type { EdgeDeliveryHealthReadModel } from "../presentation/edge-reliability-fixtures";
import type { EdgeGatewayReadModel } from "../domain/edge-types";

interface EdgeGatewayWorkspaceProps {
  gateways: EdgeGatewayReadModel[];
  deliveryHealth: EdgeDeliveryHealthReadModel;
}

export function EdgeGatewayWorkspace({ gateways, deliveryHealth }: EdgeGatewayWorkspaceProps) {
  if (gateways.length === 0) {
    return (
      <div className="edge-empty-state edge-empty-state--compact">
        <p className="edge-empty-state__title">Kayıtlı gateway yok</p>
      </div>
    );
  }

  const pending = deliveryHealth.pendingCount + deliveryHealth.retryCount;
  const utilization =
    deliveryHealth.bufferUtilization != null && !Number.isNaN(deliveryHealth.bufferUtilization)
      ? formatBufferUtilization(deliveryHealth.bufferUtilization)
      : "Bilinmiyor";

  return (
    <div className="edge-gateway-panel" data-testid="edge-gateway-workspace">
      <div className="edge-gateway-summary">
        <div className="edge-gateway-summary__item">
          <span className="edge-gateway-summary__label">Gateway</span>
          <span className="edge-gateway-summary__value">{gateways.length}</span>
        </div>
        <div className="edge-gateway-summary__item">
          <span className="edge-gateway-summary__label">API Bağlantısı</span>
          <span className={`edge-gateway-summary__value${deliveryHealth.apiConnected ? " edge-gateway-summary__value--ok" : " edge-gateway-summary__value--warn"}`}>
            {deliveryHealth.apiConnected ? "Bağlı" : "Kesik"}
          </span>
        </div>
        <div className="edge-gateway-summary__item">
          <span className="edge-gateway-summary__label">Bekleyen</span>
          <span className={`edge-gateway-summary__value${pending > 0 ? " edge-gateway-summary__value--warn" : ""}`}>
            {pending}
          </span>
        </div>
        <div className="edge-gateway-summary__item">
          <span className="edge-gateway-summary__label">Dead Letter</span>
          <span className={`edge-gateway-summary__value${deliveryHealth.deadLetterCount > 0 ? " edge-gateway-summary__value--warn" : ""}`}>
            {deliveryHealth.deadLetterCount}
          </span>
        </div>
      </div>

      <div className="edge-gateway-reliability-meta">
        <div className="edge-gateway-reliability-meta__block">
          <span className="edge-gateway-reliability-meta__label">Teslimat</span>
          <span className="edge-gateway-reliability-meta__value">
            {getEdgeDeliveryStatusLabel(deliveryHealth.deliveryStatus)}
          </span>
        </div>
        <div className="edge-gateway-reliability-meta__block">
          <span className="edge-gateway-reliability-meta__label">Kuyruk</span>
          <span className="edge-gateway-reliability-meta__value">
            {pending} bekleyen · {utilization} kullanım
          </span>
        </div>
        {deliveryHealth.bufferUtilization != null && (
          <div className="edge-gateway-util-bar" aria-label={`Kuyruk kullanımı ${utilization}`}>
            <div
              className="edge-gateway-util-bar__fill"
              style={{ width: `${Math.min(100, Math.round(deliveryHealth.bufferUtilization * 100))}%` }}
            />
          </div>
        )}
      </div>

      <div className="edge-gateway-table-wrap">
        <table className="edge-gateway-table">
          <thead>
            <tr>
              <th>Gateway</th>
              <th>Bağlantı Sağlığı</th>
              <th>Transport</th>
              <th>Atanan Makine</th>
              <th>API Bağlantısı</th>
              <th>Bekleyen Olay</th>
              <th>Dead Letter</th>
              <th>Son Teslim</th>
            </tr>
          </thead>
          <tbody>
            {gateways.map((g) => (
              <tr key={g.id}>
                <td>
                  <span className="edge-gateway-table__code">{g.gatewayCode}</span>
                  <span className="edge-gateway-table__name">{g.name}</span>
                </td>
                <td>
                  <span className={`edge-status-pill edge-status-pill--conn-${g.status === "online" ? "online" : g.status === "offline" ? "offline" : "unknown"}`}>
                    {getEdgeGatewayStatusLabel(g.status)}
                  </span>
                </td>
                <td className="edge-gateway-table__muted">{getEdgeGatewayVendorLabel(g.vendorKey)}</td>
                <td>{g.machineCount}</td>
                <td>
                  <span className={deliveryHealth.apiConnected ? "edge-text-ok" : "edge-text-warn"}>
                    {deliveryHealth.apiConnected ? "Bağlı" : "Kesik"}
                  </span>
                </td>
                <td>{pending}</td>
                <td>{deliveryHealth.deadLetterCount}</td>
                <td className="edge-gateway-table__muted">
                  {deliveryHealth.lastSuccessfulDeliveryAtUtc
                    ? new Date(deliveryHealth.lastSuccessfulDeliveryAtUtc).toLocaleString("tr-TR", {
                        day: "2-digit",
                        month: "2-digit",
                        hour: "2-digit",
                        minute: "2-digit",
                      })
                    : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

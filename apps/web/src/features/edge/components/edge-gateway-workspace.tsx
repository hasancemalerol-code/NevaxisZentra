import {
  formatBufferUtilization,
  getEdgeDeliveryStatusLabel,
  getEdgeGatewayStatusLabel,
  getEdgeGatewayVendorLabel,
  getMachineConnectivityLabel,
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
      <div className="edge-empty-state">
        <p className="edge-empty-state__title">Kayıtlı gateway yok</p>
      </div>
    );
  }

  return (
    <div className="edge-gateway-table-wrap" data-testid="edge-gateway-workspace">
      <table className="edge-gateway-table">
        <thead>
          <tr>
            <th>Gateway</th>
            <th>Durum</th>
            <th>Transport</th>
            <th>Son Görülme</th>
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
                <span className={`edge-status edge-status--${g.status}`}>
                  {getEdgeGatewayStatusLabel(g.status)}
                </span>
              </td>
              <td>{getEdgeGatewayVendorLabel(g.vendorKey)}</td>
              <td>{g.lastSeenAtUtc ? getMachineConnectivityLabel(g.lastSeenAtUtc) : "—"}</td>
              <td>{g.machineCount}</td>
              <td>
                <span className={deliveryHealth.apiConnected ? "edge-reliability-status-item__value--ok" : "edge-reliability-status-item__value--warn"}>
                  {deliveryHealth.apiConnected ? "Bağlı" : "Kesik"}
                </span>
              </td>
              <td>{deliveryHealth.pendingCount + deliveryHealth.retryCount}</td>
              <td>{deliveryHealth.deadLetterCount}</td>
              <td>
                {deliveryHealth.lastSuccessfulDeliveryAtUtc
                  ? new Date(deliveryHealth.lastSuccessfulDeliveryAtUtc).toLocaleString("tr-TR")
                  : "—"}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className="edge-gateway-table__meta">
        Teslimat: {getEdgeDeliveryStatusLabel(deliveryHealth.deliveryStatus)}
        {" · "}Kuyruk: {formatBufferUtilization(deliveryHealth.bufferUtilization)}
      </p>
    </div>
  );
}

import Link from "next/link";

import { Card } from "@/components/ui/card";

import {
  FACTORY_NETWORK_TITLE,
  formatNetworkPercent,
  getAllocationStatusLabel,
  getNetworkActionTypeLabel,
} from "@/features/factory-network/domain/factory-network-labels";
import { readFactoryNetworkDashboardPanel } from "@/features/factory-network/server/factory-network-reader";

export async function FactoryNetworkPanel() {
  const panel = await readFactoryNetworkDashboardPanel();
  if (panel.status === "failure") {
    return null;
  }

  const bottleneckLabel =
    panel.resilience.bottlenecks[0] ??
    (panel.highestLoadFacility
      ? `${panel.highestLoadFacility.facilityName} · ${formatNetworkPercent(panel.highestLoadFacility.loadPercent)}`
      : "Dengeli");

  return (
    <section className="dashboard-panel" data-testid="dashboard-factory-network-panel">
      <p className="shift-ops-hero__eyebrow">{FACTORY_NETWORK_TITLE}</p>

      <div className="factory-network-dashboard-grid">
        <Card>
          <span>Ağ kullanımı</span>
          <strong>{formatNetworkPercent(panel.metrics.networkUtilizationPercent)}</strong>
        </Card>
        <Card>
          <span>Riskli sipariş</span>
          <strong>{panel.metrics.ordersAtRisk}</strong>
        </Card>
        <Card>
          <span>Tesisler arası karar</span>
          <strong>{panel.metrics.activeCrossPlantDecisions}</strong>
        </Card>
      </div>

      <ul>
        <li>
          Tesis sayısı: <strong>{panel.facilityCount}</strong>
        </li>
        <li>
          Darboğaz: <strong>{bottleneckLabel}</strong>
        </li>
        <li>
          Son tahsis:{" "}
          {panel.latestAllocation
            ? `${getNetworkActionTypeLabel(panel.latestAllocation.actionType)} · ${getAllocationStatusLabel(panel.latestAllocation.status)}`
            : "Aktif tahsis yok"}
        </li>
        <li>
          Öncelikli fırsat:{" "}
          {panel.topOpportunity ? panel.topOpportunity.title : "Fırsat yok"}
        </li>
      </ul>

      <Link className="shift-ops-cta" href="/fabrika-agi">
        Fabrika Ağı Merkezi
      </Link>
    </section>
  );
}

import Link from "next/link";

import {
  FACTORY_NETWORK_SUBTITLE,
  FACTORY_NETWORK_TITLE,
  formatNetworkPercent,
} from "../domain/factory-network-labels";
import { readFactoryNetworkCrossLinkSummary, readFactoryNetworkWorkspaceDetail } from "../server/factory-network-reader";
import { FactoryNetworkWorkspaceClient } from "./factory-network-workspace-client";

export async function FactoryNetworkWorkspacePanel() {
  const result = await readFactoryNetworkWorkspaceDetail();
  if (result.status === "failure") {
    return null;
  }

  return (
    <section className="commint-panel factory-network-panel" data-testid="factory-network-workspace-panel">
      <header className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">FABRİKA AĞI</p>
          <h2 className="commint-panel__title">{FACTORY_NETWORK_TITLE}</h2>
          <p className="commint-panel__subtitle">{FACTORY_NETWORK_SUBTITLE}</p>
        </div>
      </header>

      <FactoryNetworkWorkspaceClient
        workspace={result.workspace}
        allocationEvaluation={result.allocationEvaluation}
      />
    </section>
  );
}

export async function FactoryNetworkCrossLinkSection() {
  const summary = await readFactoryNetworkCrossLinkSummary();
  if (summary.status === "failure") {
    return null;
  }

  return (
    <section className="factory-network-cross-link" data-testid="factory-network-cross-link">
      <p className="commint-executive__eyebrow">FABRİKA AĞI ETKİSİ</p>
      <h2>{summary.networkName}</h2>
      <p>
        {summary.facilityCount} tesis arasında kapasite dengesi ve tesisler arası tahsis kararları
        planlamayı etkileyebilir.
      </p>
      <div className="factory-network-cross-link__stats">
        <span>
          Riskli sipariş: <strong>{summary.ordersAtRisk}</strong>
        </span>
        <span>
          Bekleyen tahsis: <strong>{summary.pendingAllocationCount}</strong>
        </span>
        <span>
          Ağ fırsatı: <strong>{summary.opportunityCount}</strong>
        </span>
        <span>
          Dengesizlik: <strong>{formatNetworkPercent(summary.capacityImbalancePercent)}</strong>
        </span>
      </div>
      <Link className="shift-ops-cta" href="/fabrika-agi">
        Fabrika Ağı Merkezini Aç
      </Link>
    </section>
  );
}

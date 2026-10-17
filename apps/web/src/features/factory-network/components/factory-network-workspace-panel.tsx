import Link from "next/link";

import { FailureStatePage } from "@/components/ui/failure-state";
import { StatePage } from "@/components/ui/state-page";
import { isNotFoundFailureCode } from "@/lib/api/api-failure-classifier";

import {
  FACTORY_NETWORK_SUBTITLE,
  FACTORY_NETWORK_TITLE,
  formatNetworkPercent,
} from "../domain/factory-network-labels";
import { readFactoryNetworkCrossLinkSummary, readFactoryNetworkWorkspaceDetail } from "../server/factory-network-reader";
import { FactoryNetworkWorkspaceClient } from "./factory-network-workspace-client";

export async function FactoryNetworkWorkspacePanel() {
  const result = await readFactoryNetworkWorkspaceDetail();
  if (result.status === "failure" && isNotFoundFailureCode(result.code)) {
    return (
      <StatePage
        kind="empty"
        title="Fabrika ağı henüz tanımlı değil"
        description="Birden fazla tesisi tek ağda yönetmek için önce fabrika ağı kurulmalı. Ağ tanımlandığında kapasite dengesi ve tahsis kararları burada görünür."
        inline
      />
    );
  }

  if (result.status === "failure") {
    return (
      <FailureStatePage
        code={result.code}
        message={result.message}
        failedTitle="Fabrika ağı bilgisi yüklenemedi"
        backHref="/dashboard"
        backLabel="Ana sayfaya dön"
        inline
      />
    );
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

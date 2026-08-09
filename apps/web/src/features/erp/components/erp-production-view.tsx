import type { ReactNode } from "react";
import Link from "next/link";
import { AlertTriangle, CircleGauge, Factory, PackageCheck } from "lucide-react";

import type {
  CanonicalErpProductionOrderSummary,
  ErpProductionWorkspaceSnapshot,
} from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpProductionOrderStatusLabel,
  getErpProductionOrderStatusTone,
  getErpProviderLabel,
  getErpSourceFreshnessLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpProductionViewProps = Readonly<{
  snapshot: ErpProductionWorkspaceSnapshot;
  productionOrders: readonly CanonicalErpProductionOrderSummary[];
}>;

export function ErpProductionView({ snapshot, productionOrders }: ErpProductionViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Üretim</h1>
          <span>ERP kaynaklı salt okunur üretim projeksiyonu.</span>
          <p className="erp-simulator-note">
            ERP-4L yalnızca ERP Simulator üretim projeksiyonlarını içe aktarır.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/uretim" />

      <section className="erp-kpi-grid erp-kpi-grid--production" aria-label="Üretim özet göstergeleri">
        <ErpKpi
          label="Üretim Emirleri"
          value={snapshot.productionOrderCount}
          description="Tanımlı ERP üretim emri"
          icon={<Factory aria-hidden="true" />}
        />
        <ErpKpi
          label="Devam Eden"
          value={snapshot.inProgressCount}
          description="Serbest veya devam eden"
          icon={<PackageCheck aria-hidden="true" />}
        />
        <ErpKpi
          label="Tamamlanan"
          value={snapshot.completedCount}
          description="Tamamlanan veya kapalı"
          icon={<CircleGauge aria-hidden="true" />}
        />
        <ErpKpi
          label="Güncel Veri"
          value={snapshot.freshRecordCount}
          description="Kaynak tazeliği: güncel"
          icon={<CircleGauge aria-hidden="true" />}
        />
        <ErpKpi
          label="Eski Veri"
          value={snapshot.staleRecordCount}
          description="Kaynak tazeliği: eski"
          icon={<AlertTriangle aria-hidden="true" />}
        />
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Üretim Emirleri</h2>
          <p>ERP üretim no, ürün, miktar ve durum</p>
        </div>
        <div className="erp-table-wrap">
          {productionOrders.length === 0 ? (
            <div className="erp-empty">
              Henüz üretim emri yok. Önce üretim senkronizasyonu çalıştırın.
            </div>
          ) : (
            <table className="erp-table erp-table--production">
              <thead>
                <tr>
                  <th>Üretim No</th>
                  <th>Ürün</th>
                  <th>Planlanan</th>
                  <th>Tamamlanan</th>
                  <th>Durum</th>
                  <th>Plan Başlangıç</th>
                  <th>Tazelik</th>
                  <th>Kaynak</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {productionOrders.map((productionOrder) => {
                  const statusTone = getErpProductionOrderStatusTone(productionOrder.status);
                  const freshnessTone =
                    productionOrder.freshness === "fresh"
                      ? "ready"
                      : productionOrder.freshness === "stale"
                        ? "paused"
                        : "in-progress";

                  return (
                    <tr key={productionOrder.id}>
                      <td>{productionOrder.productionOrderNumber}</td>
                      <td>
                        {productionOrder.productDescription ??
                          productionOrder.externalProductCode ??
                          productionOrder.externalProductId}
                      </td>
                      <td>
                        {formatErpQuantity(productionOrder.plannedQuantity, productionOrder.unit)}
                      </td>
                      <td>
                        {formatErpQuantity(productionOrder.completedQuantity, productionOrder.unit)}
                      </td>
                      <td>
                        <span className={`erp-status erp-status--${statusTone}`}>
                          {getErpProductionOrderStatusLabel(productionOrder.status)}
                        </span>
                      </td>
                      <td>{formatErpDateTime(productionOrder.plannedStartUtc)}</td>
                      <td>
                        <span className={`erp-status erp-status--${freshnessTone}`}>
                          {getErpSourceFreshnessLabel(productionOrder.freshness)}
                        </span>
                      </td>
                      <td>{getErpProviderLabel(productionOrder.provider)}</td>
                      <td>
                        <div className="erp-table-actions">
                          <Link href={`/erp-entegrasyonlari/uretim/${productionOrder.id}`}>Aç</Link>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </main>
  );
}

function ErpKpi({
  label,
  value,
  description,
  icon,
}: Readonly<{
  label: string;
  value: number | string;
  description: string;
  icon: ReactNode;
}>) {
  return (
    <div className="erp-kpi">
      <span className="erp-kpi__icon" aria-hidden="true">
        {icon}
      </span>
      <div>
        <span className="erp-kpi__label">{label}</span>
        <strong className="erp-kpi__value">{value}</strong>
        <small className="erp-kpi__desc">{description}</small>
      </div>
    </div>
  );
}

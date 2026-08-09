import Link from "next/link";

import type { CanonicalErpProductionOrderDetail } from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpProductionOrderStatusLabel,
  getErpProviderLabel,
  getErpSourceFreshnessLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpProductionOrderDetailViewProps = Readonly<{
  productionOrder: CanonicalErpProductionOrderDetail;
}>;

export function ErpProductionOrderDetailView({
  productionOrder,
}: ErpProductionOrderDetailViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>{productionOrder.productionOrderNumber}</h1>
          <span>ERP kaynaklı salt okunur üretim projeksiyonu</span>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/uretim" />

      <p className="erp-readonly-notice" role="note">
        ERP kaynaklı salt okunur üretim projeksiyonu
      </p>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Üretim Emri Özeti</h2>
        </div>
        <dl className="erp-detail-grid">
          <div>
            <dt>Üretim No</dt>
            <dd>{productionOrder.productionOrderNumber}</dd>
          </div>
          <div>
            <dt>Ürün</dt>
            <dd>
              {productionOrder.productDescription ??
                productionOrder.externalProductCode ??
                productionOrder.externalProductId}
            </dd>
          </div>
          <div>
            <dt>Durum</dt>
            <dd>{getErpProductionOrderStatusLabel(productionOrder.status)}</dd>
          </div>
          <div>
            <dt>Planlanan Miktar</dt>
            <dd>{formatErpQuantity(productionOrder.plannedQuantity, productionOrder.unit)}</dd>
          </div>
          <div>
            <dt>Tamamlanan</dt>
            <dd>{formatErpQuantity(productionOrder.completedQuantity, productionOrder.unit)}</dd>
          </div>
          <div>
            <dt>Fire</dt>
            <dd>{formatErpQuantity(productionOrder.scrapQuantity, productionOrder.unit)}</dd>
          </div>
          <div>
            <dt>Plan Başlangıç</dt>
            <dd>{formatErpDateTime(productionOrder.plannedStartUtc)}</dd>
          </div>
          <div>
            <dt>Plan Bitiş</dt>
            <dd>{formatErpDateTime(productionOrder.plannedEndUtc)}</dd>
          </div>
          <div>
            <dt>Gerçek Başlangıç</dt>
            <dd>{formatErpDateTime(productionOrder.actualStartUtc)}</dd>
          </div>
          <div>
            <dt>Gerçek Bitiş</dt>
            <dd>{formatErpDateTime(productionOrder.actualEndUtc)}</dd>
          </div>
          <div>
            <dt>Satış Siparişi</dt>
            <dd>
              {productionOrder.externalSalesOrderCode ??
                productionOrder.externalSalesOrderId ??
                "—"}
            </dd>
          </div>
          <div>
            <dt>Kaynak</dt>
            <dd>
              {getErpProviderLabel(productionOrder.provider)} ·{" "}
              {getErpSourceFreshnessLabel(productionOrder.freshness)}
            </dd>
          </div>
          <div>
            <dt>Harici Referans</dt>
            <dd>{productionOrder.externalCode ?? productionOrder.externalId}</dd>
          </div>
          <div>
            <dt>Son Güncelleme</dt>
            <dd>{formatErpDateTime(productionOrder.updatedAtUtc)}</dd>
          </div>
        </dl>
      </section>

      <p>
        <Link href="/erp-entegrasyonlari/uretim">← Üretim listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/belgeler">Belgeler</Link>
      </p>
    </main>
  );
}

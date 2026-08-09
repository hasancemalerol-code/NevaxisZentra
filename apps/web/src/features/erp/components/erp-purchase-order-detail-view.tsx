import Link from "next/link";

import type { CanonicalErpPurchaseOrderDetail } from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpProviderLabel,
  getErpPurchaseOrderStatusLabel,
  getErpSourceFreshnessLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpPurchaseOrderDetailViewProps = Readonly<{
  purchaseOrder: CanonicalErpPurchaseOrderDetail;
}>;

export function ErpPurchaseOrderDetailView({ purchaseOrder }: ErpPurchaseOrderDetailViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>{purchaseOrder.purchaseOrderNumber}</h1>
          <span>ERP satın alma siparişi özeti — salt okunur.</span>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/satin-alma" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Satın Alma Özeti</h2>
        </div>
        <dl className="erp-detail-grid">
          <div>
            <dt>PO No</dt>
            <dd>{purchaseOrder.purchaseOrderNumber}</dd>
          </div>
          <div>
            <dt>Tedarikçi</dt>
            <dd>
              {purchaseOrder.supplierName ??
                purchaseOrder.externalSupplierCode ??
                purchaseOrder.externalSupplierId ??
                "—"}
            </dd>
          </div>
          <div>
            <dt>Durum</dt>
            <dd>{getErpPurchaseOrderStatusLabel(purchaseOrder.status)}</dd>
          </div>
          <div>
            <dt>Sipariş Tarihi</dt>
            <dd>{formatErpDateTime(purchaseOrder.orderDateUtc)}</dd>
          </div>
          <div>
            <dt>Beklenen Teslim</dt>
            <dd>{formatErpDateTime(purchaseOrder.expectedDeliveryDateUtc)}</dd>
          </div>
          <div>
            <dt>Para Birimi</dt>
            <dd>{purchaseOrder.currencyCode ?? "—"}</dd>
          </div>
          <div>
            <dt>Kaynak</dt>
            <dd>
              {getErpProviderLabel(purchaseOrder.provider)} ·{" "}
              {getErpSourceFreshnessLabel(purchaseOrder.freshness)}
            </dd>
          </div>
          <div>
            <dt>Harici Referans</dt>
            <dd>{purchaseOrder.externalCode ?? purchaseOrder.externalId}</dd>
          </div>
        </dl>
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Satın Alma Satırları</h2>
          <p>Sipariş edilen, teslim alınan ve açık miktarlar</p>
        </div>
        <div className="erp-table-wrap">
          <table className="erp-table">
            <thead>
              <tr>
                <th>Satır</th>
                <th>Ürün Kodu</th>
                <th>Açıklama</th>
                <th>Sipariş</th>
                <th>Teslim</th>
                <th>Açık</th>
                <th>Beklenen Teslim</th>
              </tr>
            </thead>
            <tbody>
              {purchaseOrder.lines.map((line) => (
                <tr key={line.id}>
                  <td>{line.lineNumber}</td>
                  <td>{line.externalProductCode ?? line.externalProductId}</td>
                  <td>{line.description ?? "—"}</td>
                  <td>{formatErpQuantity(line.orderedQuantity, line.unit)}</td>
                  <td>{formatErpQuantity(line.receivedQuantity, line.unit)}</td>
                  <td>{formatErpQuantity(line.openQuantity, line.unit)}</td>
                  <td>{formatErpDateTime(line.expectedDeliveryDateUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <p>
        <Link href="/erp-entegrasyonlari/satin-alma">← Satın alma listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/stok">Stok & Depolar</Link>
      </p>
    </main>
  );
}

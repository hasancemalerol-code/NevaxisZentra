import Link from "next/link";
import { ShoppingCart } from "lucide-react";

import type { CanonicalErpPurchaseOrderSummary } from "../domain/erp";
import {
  formatErpDateTime,
  getErpProviderLabel,
  getErpPurchaseOrderStatusLabel,
  getErpSourceFreshnessLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpPurchasingViewProps = Readonly<{
  purchaseOrders: readonly CanonicalErpPurchaseOrderSummary[];
}>;

export function ErpPurchasingView({ purchaseOrders }: ErpPurchasingViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Satın Alma</h1>
          <span>
            Harici ERP satın alma siparişi projeksiyonları — salt okunur. Onay veya mal kabul
            işlemi yoktur.
          </span>
          <p className="erp-simulator-note">
            ERP-3L yalnızca ERP Simulator satın alma kayıtlarını içe aktarır.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/satin-alma" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Satın Alma Siparişleri</h2>
          <p>ERP PO no, tedarikçi, teslim ve durum</p>
        </div>
        <div className="erp-table-wrap">
          {purchaseOrders.length === 0 ? (
            <div className="erp-empty">
              Henüz satın alma siparişi yok. Önce satın alma senkronizasyonu çalıştırın.
            </div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>PO No</th>
                  <th>Tedarikçi</th>
                  <th>Sipariş Tarihi</th>
                  <th>Beklenen Teslim</th>
                  <th>Satır</th>
                  <th>Açık Satır</th>
                  <th>Durum</th>
                  <th>Kaynak</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {purchaseOrders.map((purchaseOrder) => (
                  <tr key={purchaseOrder.id}>
                    <td>{purchaseOrder.purchaseOrderNumber}</td>
                    <td>
                      {purchaseOrder.supplierName ??
                        purchaseOrder.externalSupplierCode ??
                        purchaseOrder.externalSupplierId ??
                        "—"}
                    </td>
                    <td>{formatErpDateTime(purchaseOrder.orderDateUtc)}</td>
                    <td>{formatErpDateTime(purchaseOrder.expectedDeliveryDateUtc)}</td>
                    <td>{purchaseOrder.lineCount}</td>
                    <td>{purchaseOrder.openLineCount}</td>
                    <td>{getErpPurchaseOrderStatusLabel(purchaseOrder.status)}</td>
                    <td>
                      {getErpProviderLabel(purchaseOrder.provider)} ·{" "}
                      {getErpSourceFreshnessLabel(purchaseOrder.freshness)}
                    </td>
                    <td>
                      <div className="erp-table-actions">
                        <Link href={`/erp-entegrasyonlari/satin-alma/${purchaseOrder.id}`}>Aç</Link>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      {purchaseOrders.length === 0 ? (
        <div className="erp-empty" aria-hidden="true">
          <ShoppingCart />
        </div>
      ) : null}
    </main>
  );
}

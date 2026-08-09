import Link from "next/link";
import { ClipboardList } from "lucide-react";

import type { CanonicalErpOrderSummary } from "../domain/erp";
import {
  formatErpDateTime,
  getErpMappingStatusLabel,
  getErpOrderStatusLabel,
  getErpProviderLabel,
  getErpReadinessBadgeLabel,
  getErpSourceFreshnessLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpOrdersViewProps = Readonly<{
  orders: readonly CanonicalErpOrderSummary[];
}>;

export function ErpOrdersView({ orders }: ErpOrdersViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>ERP Siparişleri</h1>
          <span>
            Harici ERP sisteminden içe aktarılan kanonik sipariş kayıtları, eşleştirme
            durumu ve operasyonel hazırlık.
          </span>
          <p className="erp-simulator-note">
            ERP-2L yalnızca ERP Simulator siparişlerini içe aktarır. Üretim kaydı otomatik
            oluşturulmaz.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/siparisler" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Sipariş Listesi</h2>
          <p>ERP sipariş no, müşteri, teslim ve hazırlık durumu</p>
        </div>
        <div className="erp-table-wrap">
          {orders.length === 0 ? (
            <div className="erp-empty">Henüz ERP siparişi yok. Önce sipariş senkronizasyonu çalıştırın.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>ERP Sipariş No</th>
                  <th>Müşteri</th>
                  <th>Sipariş Tarihi</th>
                  <th>Teslim Tarihi</th>
                  <th>Satır Sayısı</th>
                  <th>Durum</th>
                  <th>Eşleştirme</th>
                  <th>Hazırlık Durumu</th>
                  <th>Kaynak</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => {
                  const blockingCodes = order.readyForOperationalUse
                    ? []
                    : ["customer-unmapped", "product-unmapped"];

                  return (
                    <tr key={order.id}>
                      <td>{order.orderNumber}</td>
                      <td>{order.externalCustomerCode ?? order.externalCustomerId}</td>
                      <td>{formatErpDateTime(order.orderDateUtc)}</td>
                      <td>{formatErpDateTime(order.requestedDeliveryDateUtc)}</td>
                      <td>{order.lineCount}</td>
                      <td>{getErpOrderStatusLabel(order.status)}</td>
                      <td>
                        <span className={`erp-badge erp-badge--${order.customerMappingStatus}`}>
                          {getErpMappingStatusLabel(order.customerMappingStatus)}
                        </span>
                      </td>
                      <td>
                        <span
                          className={`erp-badge erp-badge--${order.readyForOperationalUse ? "mapped" : "unmapped"}`}
                        >
                          {getErpReadinessBadgeLabel(order.readyForOperationalUse, blockingCodes)}
                        </span>
                      </td>
                      <td>
                        {getErpProviderLabel(order.provider)} ·{" "}
                        {getErpSourceFreshnessLabel(order.sourceFreshness)}
                      </td>
                      <td>
                        <div className="erp-table-actions">
                          <Link href={`/erp-entegrasyonlari/siparisler/${order.id}`}>Siparişi Aç</Link>
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

      {orders.length === 0 ? (
        <div className="erp-empty" aria-hidden="true">
          <ClipboardList />
        </div>
      ) : null}
    </main>
  );
}

import Link from "next/link";

import type { CanonicalErpOrderDetail } from "../domain/erp";
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

type ErpOrderDetailViewProps = Readonly<{
  order: CanonicalErpOrderDetail;
}>;

export function ErpOrderDetailView({ order }: ErpOrderDetailViewProps) {
  const blockingCodes = order.readiness.blockingReasons.map((reason) => reason.code);

  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>{order.orderNumber}</h1>
          <span>ERP sipariş özeti, müşteri/ürün eşleştirmesi ve operasyonel hazırlık.</span>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/siparisler" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>ERP Order Summary</h2>
        </div>
        <dl className="erp-detail-grid">
          <div>
            <dt>ERP Sipariş No</dt>
            <dd>{order.orderNumber}</dd>
          </div>
          <div>
            <dt>Müşteri (Harici)</dt>
            <dd>{order.externalCustomerCode ?? order.externalCustomerId}</dd>
          </div>
          <div>
            <dt>Durum</dt>
            <dd>{getErpOrderStatusLabel(order.status)}</dd>
          </div>
          <div>
            <dt>Sipariş Tarihi</dt>
            <dd>{formatErpDateTime(order.orderDateUtc)}</dd>
          </div>
          <div>
            <dt>Teslim Tarihi</dt>
            <dd>{formatErpDateTime(order.requestedDeliveryDateUtc)}</dd>
          </div>
          <div>
            <dt>Kaynak</dt>
            <dd>
              {getErpProviderLabel(order.provider)} ·{" "}
              {getErpSourceFreshnessLabel(order.readiness.sourceFreshness)}
            </dd>
          </div>
          <div>
            <dt>Referans</dt>
            <dd>{order.reference ?? "—"}</dd>
          </div>
          <div>
            <dt>Notlar</dt>
            <dd>{order.notes ?? "—"}</dd>
          </div>
        </dl>
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Hazırlık Durumu</h2>
        </div>
        <p>
          <span
            className={`erp-badge erp-badge--${order.readiness.readyForOperationalUse ? "mapped" : "unmapped"}`}
          >
            {getErpReadinessBadgeLabel(order.readiness.readyForOperationalUse, blockingCodes)}
          </span>
        </p>
        {order.readiness.blockingReasons.length > 0 ? (
          <ul className="erp-blocking-list">
            {order.readiness.blockingReasons.map((reason) => (
              <li key={reason.code}>
                <strong>{reason.code}</strong> — {reason.message}
              </li>
            ))}
          </ul>
        ) : (
          <p className="erp-empty-inline">Engelleyici neden yok.</p>
        )}
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Sipariş Satırları</h2>
        </div>
        <div className="erp-table-wrap">
          <table className="erp-table">
            <thead>
              <tr>
                <th>Satır</th>
                <th>Ürün Kodu</th>
                <th>Açıklama</th>
                <th>Miktar</th>
                <th>Ürün Eşleştirme</th>
              </tr>
            </thead>
            <tbody>
              {order.lines.map((line) => (
                <tr key={line.id}>
                  <td>{line.lineNumber}</td>
                  <td>{line.externalProductCode ?? line.externalProductId}</td>
                  <td>{line.description ?? "—"}</td>
                  <td>
                    {line.quantity} {line.unit}
                  </td>
                  <td>
                    <span className={`erp-badge erp-badge--${line.productMappingStatus}`}>
                      {getErpMappingStatusLabel(line.productMappingStatus)}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <p>
        <Link href="/erp-entegrasyonlari/siparisler">← Sipariş listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/eslestirme">Eşleştirme workspace</Link>
      </p>
    </main>
  );
}

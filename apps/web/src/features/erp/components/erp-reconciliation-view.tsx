import type { ErpCustomerReconciliation, ErpProductReconciliation } from "../domain/erp";
import {
  formatErpDateTime,
  getErpMappingStatusLabel,
  getErpProviderLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpReconciliationViewProps = Readonly<{
  customers: readonly ErpCustomerReconciliation[];
  products: readonly ErpProductReconciliation[];
}>;

export function ErpReconciliationView({ customers, products }: ErpReconciliationViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Eşleştirme</h1>
          <span>
            Harici ERP müşteri ve ürün kayıtlarının CrevaFactOS operasyonel referanslarıyla
            eşleştirilmesi. Otomatik onay yoktur.
          </span>
          <p className="erp-simulator-note">
            Öneri durumunda: “Öneri — kullanıcı onayı gerekli”. AI eşleştirme kullanılmaz.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/eslestirme" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Müşteri Eşleştirmeleri</h2>
        </div>
        <div className="erp-table-wrap">
          {customers.length === 0 ? (
            <div className="erp-empty">Henüz ERP müşteri kaydı yok.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>ERP Kaydı</th>
                  <th>Harici Kod</th>
                  <th>Durum</th>
                  <th>İç Referans</th>
                  <th>Öneri</th>
                  <th>Son Güncelleme</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {customers.map((row) => (
                  <tr key={row.canonicalCustomerId}>
                    <td>{row.name}</td>
                    <td>{row.externalCode ?? row.externalId}</td>
                    <td>
                      <span className={`erp-badge erp-badge--${row.mappingStatus}`}>
                        {getErpMappingStatusLabel(row.mappingStatus)}
                      </span>
                    </td>
                    <td>{row.mappedInternalEntityId ?? "—"}</td>
                    <td>
                      {row.suggestedInternalEntityId
                        ? `Öneri — kullanıcı onayı gerekli (${row.suggestedReason ?? "deterministic"})`
                        : "—"}
                    </td>
                    <td>{formatErpDateTime(row.updatedAtUtc)}</td>
                    <td>
                      <div className="erp-table-actions">
                        <span className="erp-action-hint">Eşleştir / Yoksay (API)</span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Ürün Eşleştirmeleri</h2>
        </div>
        <div className="erp-table-wrap">
          {products.length === 0 ? (
            <div className="erp-empty">Henüz ERP ürün kaydı yok.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>ERP Kaydı</th>
                  <th>Harici Kod</th>
                  <th>Birim</th>
                  <th>Durum</th>
                  <th>İç Referans</th>
                  <th>Öneri</th>
                  <th>Kaynak</th>
                  <th>Son Güncelleme</th>
                </tr>
              </thead>
              <tbody>
                {products.map((row) => (
                  <tr key={row.canonicalProductId}>
                    <td>{row.name}</td>
                    <td>{row.externalCode ?? row.externalId}</td>
                    <td>{row.unit ?? "—"}</td>
                    <td>
                      <span className={`erp-badge erp-badge--${row.mappingStatus}`}>
                        {getErpMappingStatusLabel(row.mappingStatus)}
                      </span>
                    </td>
                    <td>{row.mappedInternalEntityId ?? "—"}</td>
                    <td>
                      {row.suggestedInternalEntityId
                        ? `Öneri — kullanıcı onayı gerekli (${row.suggestedReason ?? "deterministic"})`
                        : "—"}
                    </td>
                    <td>{getErpProviderLabel(row.provider)}</td>
                    <td>{formatErpDateTime(row.updatedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </main>
  );
}

import type { ErpCustomerReconciliation, ErpProductReconciliation } from "../domain/erp";
import {
  formatErpDateTime,
  getErpMappingStatusLabel,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpEmptyState,
  ErpInfoBanner,
  ErpMappingBadge,
  ErpPageHeader,
  ErpProviderBadge,
  ErpSectionHeader,
  getErpInternalReferenceLabel,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpReconciliationViewProps = Readonly<{
  customers: readonly ErpCustomerReconciliation[];
  products: readonly ErpProductReconciliation[];
}>;

const CUSTOMER_FILTER_CHIPS = [
  { label: "Tümü", active: true },
  { label: "Eşleşmedi", active: false },
  { label: "Öneri", active: false },
  { label: "Eşleşti", active: false },
  { label: "Yoksayıldı", active: false },
] as const;

const PRODUCT_FILTER_CHIPS = [
  { label: "Tümü", active: true },
  { label: "Eşleşmedi", active: false },
  { label: "Öneri", active: false },
  { label: "Eşleşti", active: false },
  { label: "Çakışma", active: false },
] as const;

export function ErpReconciliationView({ customers, products }: ErpReconciliationViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="Eşleştirme"
        description="Harici ERP müşteri ve ürün kayıtlarının Zentra operasyonel referanslarıyla eşleştirilmesi. Otomatik onay yoktur."
        notice={
          <ErpInfoBanner>
            Öneri durumunda: Öneri — kullanıcı onayı gerekli. AI eşleştirme kullanılmaz.
          </ErpInfoBanner>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/eslestirme" />

      <section className="erp-section">
        <ErpSectionHeader title="Müşteri Eşleştirmeleri" />
        <div className="erp-filter-chips" aria-label="Müşteri eşleştirme filtreleri">
          {CUSTOMER_FILTER_CHIPS.map((chip) => (
            <span
              key={chip.label}
              className={`erp-filter-chip${chip.active ? " erp-filter-chip--active" : ""}`}
              role="presentation"
            >
              {chip.label}
            </span>
          ))}
        </div>
        {customers.length === 0 ? (
          <ErpEmptyState title="Henüz ERP müşteri kaydı yok." />
        ) : (
          <ErpDataTable>
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
                    <ErpMappingBadge
                      status={row.mappingStatus}
                      label={getErpMappingStatusLabel(row.mappingStatus)}
                    />
                  </td>
                  <td>{getErpInternalReferenceLabel(row.mappedInternalEntityId)}</td>
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
          </ErpDataTable>
        )}
      </section>

      <section className="erp-section">
        <ErpSectionHeader title="Ürün Eşleştirmeleri" />
        <div className="erp-filter-chips" aria-label="Ürün eşleştirme filtreleri">
          {PRODUCT_FILTER_CHIPS.map((chip) => (
            <span
              key={chip.label}
              className={`erp-filter-chip${chip.active ? " erp-filter-chip--active" : ""}`}
              role="presentation"
            >
              {chip.label}
            </span>
          ))}
        </div>
        {products.length === 0 ? (
          <ErpEmptyState title="Henüz ERP ürün kaydı yok." />
        ) : (
          <ErpDataTable>
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
                    <ErpMappingBadge
                      status={row.mappingStatus}
                      label={getErpMappingStatusLabel(row.mappingStatus)}
                    />
                  </td>
                  <td>{getErpInternalReferenceLabel(row.mappedInternalEntityId)}</td>
                  <td>
                    {row.suggestedInternalEntityId
                      ? `Öneri — kullanıcı onayı gerekli (${row.suggestedReason ?? "deterministic"})`
                      : "—"}
                  </td>
                  <td>
                    <ErpProviderBadge provider={row.provider} />
                  </td>
                  <td>{formatErpDateTime(row.updatedAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </section>
    </main>
  );
}

import { CircleCheck, Link2, TriangleAlert } from "lucide-react";

import type { ErpCustomerReconciliation, ErpProductReconciliation } from "../domain/erp";
import {
  formatErpDateTime,
  getErpMappingStatusLabel,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpEmptyState,
  ErpKpiCard,
  ErpKpiGrid,
  ErpReadonlyNotice,
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
  const nothingToReconcile = customers.length === 0 && products.length === 0;
  const allRows: readonly { mappingStatus: string }[] = [...customers, ...products];

  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="Eşleştirme"
        description="Harici ERP müşteri ve ürün kayıtlarının Zentra operasyonel referanslarıyla eşleştirilmesi. Otomatik onay yoktur."
        notice={
          <ErpReadonlyNotice>
            Öneri durumundaki eşleştirmeler kullanıcı onayı gerektirir; otomatik eşleştirme yapılmaz.
          </ErpReadonlyNotice>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/eslestirme" />

      <ErpKpiGrid columns={3}>
        <ErpKpiCard
          label="Öneri"
          value={allRows.filter((row) => row.mappingStatus === "suggested").length}
          description="Kullanıcı onayı bekleyen"
          icon={<Link2 aria-hidden="true" />}
          tone="info"
        />
        <ErpKpiCard
          label="Eşleşmedi"
          value={allRows.filter((row) => row.mappingStatus === "unmapped").length}
          description="Eşleştirme bekleyen kayıt"
          icon={<TriangleAlert aria-hidden="true" />}
          tone="warning"
        />
        <ErpKpiCard
          label="Eşleşti"
          value={allRows.filter((row) => row.mappingStatus === "mapped").length}
          description="Onaylanmış eşleştirme"
          icon={<CircleCheck aria-hidden="true" />}
          tone="success"
        />
      </ErpKpiGrid>

      {nothingToReconcile ? (
        <section className="erp-section">
          <ErpSectionHeader
            title="Müşteri ve Ürün Eşleştirmeleri"
            description="Öneri, eşleşme ve onay durumları"
          />
          <ErpEmptyState
            title="Henüz eşleştirilecek ERP kaydı yok."
            description="Müşteri ve ürün kayıtları, ERP eşitlemesi tamamlandıktan sonra burada listelenir."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        </section>
      ) : null}

      {nothingToReconcile ? null : (
      <section className="erp-section">
        <ErpSectionHeader title="Müşteri Eşleştirmeleri" />
        {customers.length > 0 ? (
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
        ) : null}
        {customers.length === 0 ? (
          <ErpEmptyState title="Henüz ERP müşteri kaydı yok." />
        ) : (
          <ErpDataTable label="Müşteri eşleştirmeleri">
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
                      ? "Öneri — kullanıcı onayı gerekli"
                      : "—"}
                  </td>
                  <td>{formatErpDateTime(row.updatedAtUtc)}</td>
                  <td>
                    <div className="erp-table-actions">
                      <span className="erp-action-hint">Eşleştir / Yoksay</span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </section>

      )}

      {nothingToReconcile ? null : (
      <section className="erp-section">
        <ErpSectionHeader title="Ürün Eşleştirmeleri" />
        {products.length > 0 ? (
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
        ) : null}
        {products.length === 0 ? (
          <ErpEmptyState title="Henüz ERP ürün kaydı yok." />
        ) : (
          <ErpDataTable label="Ürün eşleştirmeleri">
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
                      ? "Öneri — kullanıcı onayı gerekli"
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
      )}
    </div>
  );
}

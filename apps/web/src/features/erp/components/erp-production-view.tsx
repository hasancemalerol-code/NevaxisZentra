import { AlertTriangle, CircleGauge, Factory, PackageCheck, RefreshCw } from "lucide-react";

import type {
  CanonicalErpProductionOrderSummary,
  ErpProductionWorkspaceSnapshot,
} from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpProductionOrderStatusLabel,
  getErpProductionOrderStatusTone,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpEmptyState,
  ErpFreshnessBadge,
  ErpKpiCard,
  ErpKpiGrid,
  ErpPageHeader,
  ErpProviderBadge,
  ErpReadonlyNotice,
  ErpSectionHeader,
  ErpSimulatorNotice,
  ErpStatusBadge,
  ErpTableAction,
  ErpTableActionCell,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpProductionViewProps = Readonly<{
  snapshot: ErpProductionWorkspaceSnapshot;
  productionOrders: readonly CanonicalErpProductionOrderSummary[];
}>;

export function ErpProductionView({ snapshot, productionOrders }: ErpProductionViewProps) {
  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="Üretim"
        description="ERP kaynaklı salt okunur üretim özeti."
        notice={
          <>
            <ErpReadonlyNotice>
              Bu bağlantı ERP üretim kayıtlarını salt okunur olarak içe aktarır.
            </ErpReadonlyNotice>
            <ErpSimulatorNotice />
          </>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/uretim" />

      <ErpKpiGrid>
        <ErpKpiCard
          label="Üretim Emirleri"
          value={snapshot.productionOrderCount}
          description="Üretim emri"
          icon={<Factory aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Devam Eden"
          value={snapshot.inProgressCount}
          description="Devam eden kayıt"
          icon={<PackageCheck aria-hidden="true" />}
          tone="info"
        />
        <ErpKpiCard
          label="Tamamlanan"
          value={snapshot.completedCount}
          description="Tamamlanan, kapalı"
          icon={<CircleGauge aria-hidden="true" />}
          tone="success"
        />
        <ErpKpiCard
          label="Güncel Veri"
          value={snapshot.freshRecordCount}
          description="Güncel kayıt"
          icon={<RefreshCw aria-hidden="true" />}
          tone="success"
        />
        <ErpKpiCard
          label="Eski Veri"
          value={snapshot.staleRecordCount}
          description="Eski kayıt"
          icon={<AlertTriangle aria-hidden="true" />}
          tone="warning"
        />
      </ErpKpiGrid>

      <section className="erp-section">
        <ErpSectionHeader
          title="Üretim Emirleri"
          description="ERP üretim no, ürün, miktar ve durum"
        />
        {productionOrders.length === 0 ? (
          <ErpEmptyState
            title="Henüz üretim emri yok."
            description="Önce üretim eşitlemesini çalıştırın."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        ) : (
          <ErpDataTable label="ERP üretim emirleri" className="erp-table--production">
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
                      <ErpStatusBadge
                        label={getErpProductionOrderStatusLabel(productionOrder.status)}
                        tone={statusTone}
                      />
                    </td>
                    <td>{formatErpDateTime(productionOrder.plannedStartUtc)}</td>
                    <td>
                      <ErpFreshnessBadge freshness={productionOrder.freshness} />
                    </td>
                    <td>
                      <ErpProviderBadge provider={productionOrder.provider} />
                    </td>
                    <ErpTableActionCell>
                      <ErpTableAction href={`/erp-entegrasyonlari/uretim/${productionOrder.id}`}>
                        Aç
                      </ErpTableAction>
                    </ErpTableActionCell>
                  </tr>
                );
              })}
            </tbody>
          </ErpDataTable>
        )}
      </section>
    </div>
  );
}

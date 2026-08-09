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
    <main className="production-execution-page erp-workspace-page">
      <ErpPageHeader
        title="Üretim"
        description="ERP kaynaklı salt okunur üretim projeksiyonu."
        notice={
          <>
            <ErpReadonlyNotice>
              ERP-4L yalnızca ERP Simulator üretim projeksiyonlarını içe aktarır.
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
          description="Tanımlı ERP üretim emri"
          icon={<Factory aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Devam Eden"
          value={snapshot.inProgressCount}
          description="Serbest veya devam eden"
          icon={<PackageCheck aria-hidden="true" />}
          tone="info"
        />
        <ErpKpiCard
          label="Tamamlanan"
          value={snapshot.completedCount}
          description="Tamamlanan veya kapalı"
          icon={<CircleGauge aria-hidden="true" />}
          tone="success"
        />
        <ErpKpiCard
          label="Güncel Veri"
          value={snapshot.freshRecordCount}
          description="Kaynak tazeliği: güncel"
          icon={<CircleGauge aria-hidden="true" />}
          tone="success"
        />
        <ErpKpiCard
          label="Eski Veri"
          value={snapshot.staleRecordCount}
          description="Kaynak tazeliği: eski"
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
            description="Önce üretim senkronizasyonu çalıştırın."
          />
        ) : (
          <ErpDataTable className="erp-table--production">
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
    </main>
  );
}

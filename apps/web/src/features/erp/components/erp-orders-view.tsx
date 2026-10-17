import { CircleCheck, ClipboardList, TriangleAlert } from "lucide-react";

import type { CanonicalErpOrderSummary } from "../domain/erp";
import {
  formatErpDateTime,
  getErpMappingStatusLabel,
  getErpOrderStatusLabel,
  getErpReadinessBadgeLabel,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpEmptyState,
  ErpFreshnessBadge,
  ErpKpiCard,
  ErpKpiGrid,
  ErpMappingBadge,
  ErpPageHeader,
  ErpProviderBadge,
  ErpReadonlyNotice,
  ErpSectionHeader,
  ErpSimulatorNotice,
  ErpTableAction,
  ErpTableActionCell,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpOrdersViewProps = Readonly<{
  orders: readonly CanonicalErpOrderSummary[];
}>;

function getReadinessBadgeClass(ready: boolean, blockingCodes: readonly string[]): string {
  if (ready) {
    return "erp-readiness-badge--ready";
  }
  if (blockingCodes.includes("mapping-conflict")) {
    return "erp-readiness-badge--critical";
  }
  return "erp-readiness-badge--blocked";
}

export function ErpOrdersView({ orders }: ErpOrdersViewProps) {
  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="ERP Siparişleri"
        description="Harici ERP sisteminden içe aktarılan sipariş kayıtları, eşleştirme durumu ve operasyonel hazırlık."
        notice={
          <>
            <ErpReadonlyNotice>
              Bu bağlantı ERP siparişlerini salt okunur olarak içe aktarır. Üretim kaydı otomatik
              oluşturulmaz.
            </ErpReadonlyNotice>
            <ErpSimulatorNotice />
          </>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/siparisler" />

      <ErpKpiGrid columns={3}>
        <ErpKpiCard
          label="ERP Siparişleri"
          value={orders.length}
          description="İçe aktarılan sipariş"
          icon={<ClipboardList aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Operasyona Hazır"
          value={orders.filter((order) => order.readyForOperationalUse).length}
          description="Eşleştirmesi tamamlanmış"
          icon={<CircleCheck aria-hidden="true" />}
          tone="success"
        />
        <ErpKpiCard
          label="İnceleme Gerekiyor"
          value={orders.filter((order) => !order.readyForOperationalUse).length}
          description="Eşleştirme veya veri eksiği"
          icon={<TriangleAlert aria-hidden="true" />}
          tone="warning"
        />
      </ErpKpiGrid>

      <section className="erp-section">
        <ErpSectionHeader
          title="Sipariş Listesi"
          description="ERP sipariş no, müşteri, teslim ve hazırlık durumu"
        />
        {orders.length === 0 ? (
          <ErpEmptyState
            title="Henüz ERP siparişi yok."
            description="Önce sipariş eşitlemesini çalıştırın."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        ) : (
          <ErpDataTable label="ERP siparişleri" className="erp-table--orders">
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
                <th>Tazelik</th>
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
                      <ErpMappingBadge
                        status={order.customerMappingStatus}
                        label={getErpMappingStatusLabel(order.customerMappingStatus)}
                      />
                    </td>
                    <td>
                      <span
                        className={`erp-badge ${getReadinessBadgeClass(order.readyForOperationalUse, blockingCodes)}`}
                      >
                        {getErpReadinessBadgeLabel(order.readyForOperationalUse, blockingCodes)}
                      </span>
                    </td>
                    <td>
                      <ErpProviderBadge provider={order.provider} />
                    </td>
                    <td>
                      <ErpFreshnessBadge freshness={order.sourceFreshness} />
                    </td>
                    <ErpTableActionCell>
                      <ErpTableAction href={`/erp-entegrasyonlari/siparisler/${order.id}`}>
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

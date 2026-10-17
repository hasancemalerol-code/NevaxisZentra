import { keepCode } from "@/lib/presentation/format";
import { PackageOpen, Rows3, ShoppingCart } from "lucide-react";

import type { CanonicalErpPurchaseOrderSummary } from "../domain/erp";
import {
  formatErpDateTime,
  getErpPurchaseOrderStatusLabel,
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
  ErpTableAction,
  ErpTableActionCell,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpPurchasingViewProps = Readonly<{
  purchaseOrders: readonly CanonicalErpPurchaseOrderSummary[];
}>;

export function ErpPurchasingView({ purchaseOrders }: ErpPurchasingViewProps) {
  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="Satın Alma"
        description="Harici ERP satın alma siparişi özetleri — salt okunur. Onay veya mal kabul işlemi yoktur."
        notice={
          <>
            <ErpReadonlyNotice>
              Bu bağlantı ERP satın alma kayıtlarını salt okunur olarak içe aktarır.
            </ErpReadonlyNotice>
            <ErpSimulatorNotice />
          </>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/satin-alma" />

      <ErpKpiGrid columns={3}>
        <ErpKpiCard
          label="Siparişler"
          value={purchaseOrders.length}
          description="İçe aktarılan sipariş"
          icon={<ShoppingCart aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Açık Siparişler"
          value={purchaseOrders.filter((purchaseOrder) => purchaseOrder.openLineCount > 0).length}
          description="Açık satırı bulunan"
          icon={<PackageOpen aria-hidden="true" />}
          tone="info"
        />
        <ErpKpiCard
          label="Açık Satır"
          value={purchaseOrders.reduce((total, purchaseOrder) => total + purchaseOrder.openLineCount, 0)}
          description="Teslim bekleyen satır"
          icon={<Rows3 aria-hidden="true" />}
        />
      </ErpKpiGrid>

      <section className="erp-section">
        <ErpSectionHeader
          title="Satın Alma Siparişleri"
          description="ERP PO no, tedarikçi, teslim ve durum"
        />
        {purchaseOrders.length === 0 ? (
          <ErpEmptyState
            title="Henüz satın alma siparişi yok."
            description="Önce satın alma eşitlemesini çalıştırın."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        ) : (
          <ErpDataTable label="ERP satın alma siparişleri">
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
                <th>Tazelik</th>
                <th>İşlem</th>
              </tr>
            </thead>
            <tbody>
              {purchaseOrders.map((purchaseOrder) => (
                <tr key={purchaseOrder.id}>
                  <td>{keepCode(purchaseOrder.purchaseOrderNumber)}</td>
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
                    <ErpProviderBadge provider={purchaseOrder.provider} />
                  </td>
                  <td>
                    <ErpFreshnessBadge freshness={purchaseOrder.freshness} />
                  </td>
                  <ErpTableActionCell>
                    <ErpTableAction href={`/erp-entegrasyonlari/satin-alma/${purchaseOrder.id}`}>
                      Aç
                    </ErpTableAction>
                  </ErpTableActionCell>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </section>
    </div>
  );
}

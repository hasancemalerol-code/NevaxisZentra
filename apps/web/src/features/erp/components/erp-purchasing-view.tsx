import { ShoppingCart } from "lucide-react";

import type { CanonicalErpPurchaseOrderSummary } from "../domain/erp";
import {
  formatErpDateTime,
  getErpPurchaseOrderStatusLabel,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpEmptyState,
  ErpFreshnessBadge,
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
    <main className="production-execution-page erp-workspace-page">
      <ErpPageHeader
        title="Satın Alma"
        description="Harici ERP satın alma siparişi projeksiyonları — salt okunur. Onay veya mal kabul işlemi yoktur."
        notice={
          <>
            <ErpReadonlyNotice>
              ERP-3L yalnızca ERP Simulator satın alma kayıtlarını içe aktarır.
            </ErpReadonlyNotice>
            <ErpSimulatorNotice />
          </>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/satin-alma" />

      <section className="erp-section">
        <ErpSectionHeader
          title="Satın Alma Siparişleri"
          description="ERP PO no, tedarikçi, teslim ve durum"
        />
        {purchaseOrders.length === 0 ? (
          <ErpEmptyState
            title="Henüz satın alma siparişi yok."
            description="Önce satın alma senkronizasyonu çalıştırın."
          />
        ) : (
          <ErpDataTable>
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

      {purchaseOrders.length === 0 ? (
        <div className="erp-empty" aria-hidden="true">
          <ShoppingCart />
        </div>
      ) : null}
    </main>
  );
}

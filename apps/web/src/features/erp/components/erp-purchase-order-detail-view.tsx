import Link from "next/link";

import type { CanonicalErpPurchaseOrderDetail } from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpPurchaseOrderStatusLabel,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpDetailSection,
  ErpFreshnessBadge,
  ErpMetadataGrid,
  ErpMetadataItem,
  ErpPageHeader,
  ErpProviderBadge,
  ErpReadonlyNotice,
  ErpStatusBadge,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpPurchaseOrderDetailViewProps = Readonly<{
  purchaseOrder: CanonicalErpPurchaseOrderDetail;
}>;

function getPurchaseOrderStatusTone(
  status: string,
): "ready" | "paused" | "critical" | "in-progress" | "neutral" {
  switch (status) {
    case "received":
    case "closed":
      return "ready";
    case "cancelled":
      return "critical";
    case "partially-received":
      return "in-progress";
    default:
      return "neutral";
  }
}

export function ErpPurchaseOrderDetailView({ purchaseOrder }: ErpPurchaseOrderDetailViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title={purchaseOrder.purchaseOrderNumber}
        description="ERP satın alma siparişi özeti — salt okunur."
        notice={
          <ErpReadonlyNotice>
            Bu kayıt ERP kaynağından salt okunur olarak görüntülenir; düzenleme yapılamaz.
          </ErpReadonlyNotice>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/satin-alma" />

      <ErpDetailSection title="Satın Alma Özeti">
        <ErpMetadataGrid>
          <ErpMetadataItem label="PO No">{purchaseOrder.purchaseOrderNumber}</ErpMetadataItem>
          <ErpMetadataItem label="Tedarikçi">
            {purchaseOrder.supplierName ??
              purchaseOrder.externalSupplierCode ??
              purchaseOrder.externalSupplierId ??
              "—"}
          </ErpMetadataItem>
          <ErpMetadataItem label="Durum">
            <ErpStatusBadge
              label={getErpPurchaseOrderStatusLabel(purchaseOrder.status)}
              tone={getPurchaseOrderStatusTone(purchaseOrder.status)}
            />
          </ErpMetadataItem>
          <ErpMetadataItem label="Sipariş Tarihi">
            {formatErpDateTime(purchaseOrder.orderDateUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Beklenen Teslim">
            {formatErpDateTime(purchaseOrder.expectedDeliveryDateUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Para Birimi">{purchaseOrder.currencyCode ?? "—"}</ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="ERP Kaynağı">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Sağlayıcı">
            <ErpProviderBadge provider={purchaseOrder.provider} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Güncellik">
            <ErpFreshnessBadge freshness={purchaseOrder.freshness} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Harici Referans">
            {purchaseOrder.externalCode ?? purchaseOrder.externalId}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Satın Alma Satırları">
        <p>Sipariş edilen, teslim alınan ve açık miktarlar</p>
        <ErpDataTable>
          <thead>
            <tr>
              <th>Satır</th>
              <th>Ürün Kodu</th>
              <th>Açıklama</th>
              <th>Sipariş</th>
              <th>Teslim</th>
              <th>Açık</th>
              <th>Beklenen Teslim</th>
            </tr>
          </thead>
          <tbody>
            {purchaseOrder.lines.map((line) => (
              <tr key={line.id}>
                <td>{line.lineNumber}</td>
                <td>{line.externalProductCode ?? line.externalProductId}</td>
                <td>{line.description ?? "—"}</td>
                <td>{formatErpQuantity(line.orderedQuantity, line.unit)}</td>
                <td>{formatErpQuantity(line.receivedQuantity, line.unit)}</td>
                <td>{formatErpQuantity(line.openQuantity, line.unit)}</td>
                <td>{formatErpDateTime(line.expectedDeliveryDateUtc)}</td>
              </tr>
            ))}
          </tbody>
        </ErpDataTable>
      </ErpDetailSection>

      <p>
        <Link href="/erp-entegrasyonlari/satin-alma">← Satın alma listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/stok">Stok & Depolar</Link>
      </p>
    </main>
  );
}

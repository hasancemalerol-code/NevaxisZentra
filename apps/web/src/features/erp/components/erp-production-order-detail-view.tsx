import Link from "next/link";

import type { CanonicalErpProductionOrderDetail } from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpProductionOrderStatusLabel,
  getErpProductionOrderStatusTone,
} from "../presentation/erp-labels";
import {
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

type ErpProductionOrderDetailViewProps = Readonly<{
  productionOrder: CanonicalErpProductionOrderDetail;
}>;

export function ErpProductionOrderDetailView({
  productionOrder,
}: ErpProductionOrderDetailViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page erp-workspace-page--dense erp-workspace-page--readonly">
      <ErpPageHeader
        title={productionOrder.productionOrderNumber}
        description="ERP kaynaklı salt okunur üretim projeksiyonu"
        notice={
          <ErpReadonlyNotice>
            Bu ekran yalnızca ERP&apos;den senkronize edilen üretim verisini gösterir. NevaxisFactOS
            üretim kaydı oluşturulmaz veya güncellenmez.
          </ErpReadonlyNotice>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/uretim" />

      <ErpDetailSection title="Üretim Emri Özeti">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Üretim No">
            {productionOrder.productionOrderNumber}
          </ErpMetadataItem>
          <ErpMetadataItem label="Durum">
            <ErpStatusBadge
              label={getErpProductionOrderStatusLabel(productionOrder.status)}
              tone={getErpProductionOrderStatusTone(productionOrder.status)}
            />
          </ErpMetadataItem>
          <ErpMetadataItem label="Planlanan Miktar">
            {formatErpQuantity(productionOrder.plannedQuantity, productionOrder.unit)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Tamamlanan">
            {formatErpQuantity(productionOrder.completedQuantity, productionOrder.unit)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Fire">
            {formatErpQuantity(productionOrder.scrapQuantity, productionOrder.unit)}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Ürün Bilgisi">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Ürün">
            {productionOrder.productDescription ??
              productionOrder.externalProductCode ??
              productionOrder.externalProductId}
          </ErpMetadataItem>
          <ErpMetadataItem label="Ürün Kodu">
            {productionOrder.externalProductCode ?? productionOrder.externalProductId}
          </ErpMetadataItem>
          <ErpMetadataItem label="Satış Siparişi">
            {productionOrder.externalSalesOrderCode ??
              productionOrder.externalSalesOrderId ??
              "—"}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Planlama & Gerçekleşme">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Plan Başlangıç">
            {formatErpDateTime(productionOrder.plannedStartUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Plan Bitiş">
            {formatErpDateTime(productionOrder.plannedEndUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Gerçek Başlangıç">
            {formatErpDateTime(productionOrder.actualStartUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Gerçek Bitiş">
            {formatErpDateTime(productionOrder.actualEndUtc)}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="ERP Kaynağı">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Sağlayıcı">
            <ErpProviderBadge provider={productionOrder.provider} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Güncellik">
            <ErpFreshnessBadge freshness={productionOrder.freshness} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Harici Referans">
            {productionOrder.externalCode ?? productionOrder.externalId}
          </ErpMetadataItem>
          <ErpMetadataItem label="Son Güncelleme">
            {formatErpDateTime(productionOrder.updatedAtUtc)}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <p>
        <Link href="/erp-entegrasyonlari/uretim">← Üretim listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/belgeler">Belgeler</Link>
      </p>
    </main>
  );
}

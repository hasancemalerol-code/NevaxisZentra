import Link from "next/link";

import type { CanonicalErpOrderDetail, ErpOrderMaterialAvailability } from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpCoverageStatusLabel,
  getErpMappingStatusLabel,
  getErpOrderStatusLabel,
  getErpReadinessBadgeLabel,
  getErpShortageStatusLabel,
  getErpShortageStatusTone,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpDetailSection,
  ErpEmptyState,
  ErpFreshnessBadge,
  ErpHeaderStatusContext,
  ErpInfoBanner,
  ErpMappingBadge,
  ErpMetadataGrid,
  ErpMetadataItem,
  ErpPageHeader,
  ErpProviderBadge,
  ErpStatusBadge,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpOrderDetailViewProps = Readonly<{
  order: CanonicalErpOrderDetail;
  materialAvailability: ErpOrderMaterialAvailability | null;
}>;

function getOrderStatusTone(
  status: string,
): "ready" | "paused" | "critical" | "in-progress" | "neutral" {
  switch (status) {
    case "fulfilled":
      return "ready";
    case "cancelled":
      return "critical";
    case "partially-fulfilled":
    case "confirmed":
      return "in-progress";
    default:
      return "neutral";
  }
}

function getReadinessTone(
  ready: boolean,
  blockingCodes: readonly string[],
): "ready" | "paused" | "critical" | "in-progress" | "neutral" {
  if (ready) {
    return "ready";
  }
  if (blockingCodes.includes("mapping-conflict")) {
    return "critical";
  }
  if (blockingCodes.includes("source-stale")) {
    return "paused";
  }
  return "in-progress";
}

export function ErpOrderDetailView({ order, materialAvailability }: ErpOrderDetailViewProps) {
  const blockingCodes = order.readiness.blockingReasons.map((reason) => reason.code);
  const customerMappingStatus = order.readiness.customerMapped ? "mapped" : "unmapped";
  const readinessTone = getReadinessTone(order.readiness.readyForOperationalUse, blockingCodes);
  const headerReadinessTone =
    readinessTone === "neutral" ? "in-progress" : readinessTone;

  return (
    <main className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title={order.orderNumber}
        description={`${order.externalCustomerCode ?? order.externalCustomerId} · ERP sipariş özeti ve operasyonel hazırlık`}
        aside={
          <ErpHeaderStatusContext
            healthLabel={getErpReadinessBadgeLabel(order.readiness.readyForOperationalUse, blockingCodes)}
            healthTone={headerReadinessTone}
            syncLabel={`Güncellik: ${formatErpDateTime(order.updatedAtUtc)}`}
          />
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/siparisler" />

      <ErpDetailSection title="Sipariş Özeti">
        <ErpMetadataGrid>
          <ErpMetadataItem label="ERP Sipariş No">{order.orderNumber}</ErpMetadataItem>
          <ErpMetadataItem label="Durum">
            <ErpStatusBadge
              label={getErpOrderStatusLabel(order.status)}
              tone={getOrderStatusTone(order.status)}
            />
          </ErpMetadataItem>
          <ErpMetadataItem label="Sipariş Tarihi">
            {formatErpDateTime(order.orderDateUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Teslim Tarihi">
            {formatErpDateTime(order.requestedDeliveryDateUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Para Birimi">{order.currencyCode ?? "—"}</ErpMetadataItem>
          <ErpMetadataItem label="Referans">{order.reference ?? "—"}</ErpMetadataItem>
          <ErpMetadataItem label="Notlar">{order.notes ?? "—"}</ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="ERP Kaynağı">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Sağlayıcı">
            <ErpProviderBadge provider={order.provider} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Güncellik">
            <ErpFreshnessBadge freshness={order.readiness.sourceFreshness} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Harici Referans">
            {order.externalCode ?? order.externalId}
          </ErpMetadataItem>
          <ErpMetadataItem label="Kaynak Sürümü">{order.sourceVersion ?? "—"}</ErpMetadataItem>
          <ErpMetadataItem label="Kaynak Güncelleme">
            {formatErpDateTime(order.sourceUpdatedAtUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="İçe Aktarım">
            {formatErpDateTime(order.importedAtUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Son Güncelleme">
            {formatErpDateTime(order.updatedAtUtc)}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Müşteri Eşleştirmesi">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Müşteri (Harici)">
            {order.externalCustomerCode ?? order.externalCustomerId}
          </ErpMetadataItem>
          <ErpMetadataItem label="Eşleştirme Durumu">
            <ErpMappingBadge
              status={customerMappingStatus}
              label={getErpMappingStatusLabel(customerMappingStatus)}
            />
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Operasyonel Hazırlık">
        <p>
          <ErpStatusBadge
            label={getErpReadinessBadgeLabel(order.readiness.readyForOperationalUse, blockingCodes)}
            tone={getReadinessTone(order.readiness.readyForOperationalUse, blockingCodes)}
          />
        </p>
        {order.readiness.blockingReasons.length > 0 ? (
          <ul className="erp-blocking-list">
            {order.readiness.blockingReasons.map((reason) => (
              <li key={reason.code}>
                <strong>{reason.code}</strong> — {reason.message}
              </li>
            ))}
          </ul>
        ) : (
          <ErpEmptyState title="Engelleyici neden yok." />
        )}
        {!order.readiness.allProductsMapped ? (
          <ErpInfoBanner title="Ürün eşleştirmesi">
            {order.readiness.missingProductMappings} satırda ürün eşleştirmesi eksik.
          </ErpInfoBanner>
        ) : null}
      </ErpDetailSection>

      <ErpDetailSection title="Sipariş Satırları">
        <ErpDataTable>
          <thead>
            <tr>
              <th>Satır</th>
              <th>Ürün Kodu</th>
              <th>Açıklama</th>
              <th>Miktar</th>
              <th>Ürün Eşleştirme</th>
            </tr>
          </thead>
          <tbody>
            {order.lines.map((line) => (
              <tr key={line.id}>
                <td>{line.lineNumber}</td>
                <td>{line.externalProductCode ?? line.externalProductId}</td>
                <td>{line.description ?? "—"}</td>
                <td>
                  {line.quantity} {line.unit}
                </td>
                <td>
                  <ErpMappingBadge
                    status={line.productMappingStatus}
                    label={getErpMappingStatusLabel(line.productMappingStatus)}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </ErpDataTable>
      </ErpDetailSection>

      <ErpDetailSection title="Malzeme Uygunluğu">
        {materialAvailability?.freshnessNote ? (
          <ErpInfoBanner>{materialAvailability.freshnessNote}</ErpInfoBanner>
        ) : (
          <ErpInfoBanner>ERP stok projeksiyonuna dayalı bilgi</ErpInfoBanner>
        )}
        {!materialAvailability || materialAvailability.lines.length === 0 ? (
          <ErpEmptyState
            title="Malzeme uygunluk verisi yok."
            description="Stok senkronizasyonu tamamlandığında satır bazlı uygunluk burada görünür."
          />
        ) : (
          <ErpDataTable className="erp-table--material-availability">
            <thead>
              <tr>
                <th>Satır</th>
                <th>Ürün Kodu</th>
                <th>Gerekli</th>
                <th>Kullanılabilir</th>
                <th>Eksik</th>
                <th>Durum</th>
                <th>Satın Alma Karşılama</th>
              </tr>
            </thead>
            <tbody>
              {materialAvailability.lines.map((line) => {
                const tone = getErpShortageStatusTone(line.status);

                return (
                  <tr key={line.externalLineId}>
                    <td>{line.lineNumber}</td>
                    <td>{line.externalProductCode ?? line.externalProductId}</td>
                    <td>{formatErpQuantity(line.requiredQuantity)}</td>
                    <td>{formatErpQuantity(line.availableQuantity)}</td>
                    <td>{formatErpQuantity(line.shortageQuantity)}</td>
                    <td>
                      <ErpStatusBadge
                        label={getErpShortageStatusLabel(line.status)}
                        tone={tone}
                      />
                    </td>
                    <td>
                      {line.purchaseCoverage ? (
                        <span className="erp-coverage-detail">
                          {getErpCoverageStatusLabel(line.purchaseCoverage.coverageStatus)}
                          {" · "}
                          {formatErpQuantity(line.purchaseCoverage.openPurchaseQuantity)}
                          {line.purchaseCoverage.nearestExpectedDeliveryDateUtc
                            ? ` · ${formatErpDateTime(line.purchaseCoverage.nearestExpectedDeliveryDateUtc)}`
                            : ""}
                        </span>
                      ) : (
                        "—"
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </ErpDataTable>
        )}
      </ErpDetailSection>

      <p>
        <Link href="/erp-entegrasyonlari/siparisler">← Sipariş listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/eslestirme">Eşleştirme workspace</Link>
      </p>
    </main>
  );
}

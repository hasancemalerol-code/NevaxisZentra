import {
  AlertTriangle,
  Boxes,
  CircleGauge,
  Package,
  ShoppingCart,
  Warehouse,
} from "lucide-react";

import type {
  CanonicalErpInventoryBalance,
  CanonicalErpWarehouse,
  ErpInventoryWorkspaceSnapshot,
  ErpSyncReliability,
} from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpConnectionStatusTone,
  getErpEntityTypeLabel,
  getErpSyncStatusLabel,
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
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpInventoryViewProps = Readonly<{
  snapshot: ErpInventoryWorkspaceSnapshot;
  warehouses: readonly CanonicalErpWarehouse[];
  inventoryBalances: readonly CanonicalErpInventoryBalance[];
  syncReliability: readonly ErpSyncReliability[];
}>;

export function ErpInventoryView({
  snapshot,
  warehouses,
  inventoryBalances,
  syncReliability,
}: ErpInventoryViewProps) {
  const inventoryReliability = syncReliability.filter((row) =>
    ["warehouses", "inventory", "purchasing"].includes(row.entityType),
  );

  return (
    <main className="production-execution-page erp-workspace-page">
      <ErpPageHeader
        title="Stok & Depolar"
        description="Harici ERP depo ve stok projeksiyonları — salt okunur görünüm. Stok hareketi veya ERP yazma işlemi yoktur."
        notice={
          <>
            <ErpReadonlyNotice>
              ERP-3L yalnızca ERP Simulator stok ve satın alma projeksiyonlarını içe aktarır.
            </ErpReadonlyNotice>
            <ErpSimulatorNotice />
          </>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/stok" />

      <ErpKpiGrid>
        <ErpKpiCard
          label="Depolar"
          value={snapshot.warehouseCount}
          description="Tanımlı ERP deposu"
          icon={<Warehouse aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Ürün Stok Kayıtları"
          value={snapshot.inventoryRecordCount}
          description="Depo × ürün stok satırı"
          icon={<Package aria-hidden="true" />}
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
        <ErpKpiCard
          label="Yetersiz Stok"
          value={snapshot.insufficientStockCount}
          description="Kullanılabilir miktar ≤ 0"
          icon={<Boxes aria-hidden="true" />}
          tone="danger"
        />
        <ErpKpiCard
          label="Açık Satın Alma"
          value={snapshot.openPurchaseOrderCount}
          description="Açık satır içeren PO"
          icon={<ShoppingCart aria-hidden="true" />}
        />
      </ErpKpiGrid>

      <section className="erp-section">
        <ErpSectionHeader
          title="Depolar"
          description="Harici ERP depo kayıtları — salt okunur"
        />
        {warehouses.length === 0 ? (
          <ErpEmptyState
            title="Henüz ERP depo kaydı yok."
            description="Önce depo senkronizasyonu çalıştırın."
          />
        ) : (
          <ErpDataTable>
            <thead>
              <tr>
                <th>ERP Kodu</th>
                <th>Depo</th>
                <th>Durum</th>
                <th>Kaynak</th>
                <th>Tazelik</th>
                <th>Son Güncelleme</th>
              </tr>
            </thead>
            <tbody>
              {warehouses.map((warehouse) => (
                <tr key={warehouse.id}>
                  <td>{warehouse.externalCode ?? warehouse.externalId}</td>
                  <td>{warehouse.name}</td>
                  <td>{warehouse.isActive ? "Aktif" : "Pasif"}</td>
                  <td>
                    <ErpProviderBadge provider={warehouse.provider} />
                  </td>
                  <td>
                    <ErpFreshnessBadge freshness={warehouse.freshness} />
                  </td>
                  <td>{formatErpDateTime(warehouse.updatedAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </section>

      <section className="erp-section">
        <ErpSectionHeader
          title="Stok Kayıtları"
          description="Depo bazlı ürün stok projeksiyonları"
        />
        {inventoryBalances.length === 0 ? (
          <ErpEmptyState
            title="Henüz stok kaydı yok."
            description="Önce stok senkronizasyonu çalıştırın."
          />
        ) : (
          <ErpDataTable className="erp-table--inventory">
            <thead>
              <tr>
                <th>Ürün Kodu</th>
                <th>Ürün</th>
                <th>Depo</th>
                <th>Eldeki</th>
                <th>Kullanılabilir</th>
                <th>Rezerve</th>
                <th>Siparişte</th>
                <th>Tazelik</th>
                <th>Kaynak</th>
              </tr>
            </thead>
            <tbody>
              {inventoryBalances.map((balance) => (
                <tr key={balance.id}>
                  <td>{balance.externalProductCode ?? balance.externalProductId}</td>
                  <td>{balance.productName ?? "—"}</td>
                  <td>{balance.externalWarehouseCode ?? balance.warehouseName ?? balance.externalWarehouseId}</td>
                  <td>{formatErpQuantity(balance.quantityOnHand, balance.unit)}</td>
                  <td>{formatErpQuantity(balance.quantityAvailable, balance.unit)}</td>
                  <td>{formatErpQuantity(balance.quantityReserved, balance.unit)}</td>
                  <td>{formatErpQuantity(balance.quantityOnOrder, balance.unit)}</td>
                  <td>
                    <ErpFreshnessBadge freshness={balance.freshness} />
                  </td>
                  <td>
                    <ErpProviderBadge provider={balance.provider} />
                  </td>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </section>

      <section className="erp-section">
        <ErpSectionHeader
          title="Senkronizasyon Sağlığı"
          description="Depo, stok ve satın alma senkronizasyon güvenilirliği"
        />
        {inventoryReliability.length === 0 ? (
          <ErpEmptyState title="Senkronizasyon güvenilirlik verisi yok." />
        ) : (
          <ErpDataTable className="erp-table--sync-reliability">
            <thead>
              <tr>
                <th>Varlık</th>
                <th>Son Başarılı Sync</th>
                <th>Son Hata</th>
                <th>Son Durum</th>
                <th>Ardışık Hata</th>
                <th>Tazelik</th>
                <th>Hata</th>
              </tr>
            </thead>
            <tbody>
              {inventoryReliability.map((row) => {
                const tone = getErpConnectionStatusTone(
                  row.freshnessStatus === "fresh"
                    ? "healthy"
                    : row.freshnessStatus === "stale"
                      ? "degraded"
                      : "configured",
                );

                return (
                  <tr key={row.entityType}>
                    <td>{getErpEntityTypeLabel(row.entityType)}</td>
                    <td>{formatErpDateTime(row.lastSuccessfulSyncAtUtc)}</td>
                    <td>{formatErpDateTime(row.lastFailedSyncAtUtc)}</td>
                    <td>{row.lastRunStatus ? getErpSyncStatusLabel(row.lastRunStatus) : "—"}</td>
                    <td>{row.consecutiveFailures}</td>
                    <td>
                      <ErpStatusBadge
                        label={
                          row.freshnessStatus === "fresh"
                            ? "Güncel"
                            : row.freshnessStatus === "stale"
                              ? "Güncel Değil"
                              : "Bilinmiyor"
                        }
                        tone={tone}
                      />
                    </td>
                    <td>{row.currentErrorMessage ?? "—"}</td>
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

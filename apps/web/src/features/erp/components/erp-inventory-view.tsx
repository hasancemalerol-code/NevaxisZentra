import type { ReactNode } from "react";
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
  getErpProviderLabel,
  getErpSourceFreshnessLabel,
  getErpSyncStatusLabel,
} from "../presentation/erp-labels";
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
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Stok & Depolar</h1>
          <span>
            Harici ERP depo ve stok projeksiyonları — salt okunur görünüm. Stok hareketi veya
            ERP yazma işlemi yoktur.
          </span>
          <p className="erp-simulator-note">
            ERP-3L yalnızca ERP Simulator stok ve satın alma projeksiyonlarını içe aktarır.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/stok" />

      <section className="erp-kpi-grid" aria-label="Stok özet göstergeleri">
        <ErpKpi
          label="Depolar"
          value={snapshot.warehouseCount}
          description="Tanımlı ERP deposu"
          icon={<Warehouse aria-hidden="true" />}
        />
        <ErpKpi
          label="Ürün Stok Kayıtları"
          value={snapshot.inventoryRecordCount}
          description="Depo × ürün stok satırı"
          icon={<Package aria-hidden="true" />}
        />
        <ErpKpi
          label="Güncel Veri"
          value={snapshot.freshRecordCount}
          description="Kaynak tazeliği: güncel"
          icon={<CircleGauge aria-hidden="true" />}
        />
        <ErpKpi
          label="Eski Veri"
          value={snapshot.staleRecordCount}
          description="Kaynak tazeliği: eski"
          icon={<AlertTriangle aria-hidden="true" />}
        />
        <ErpKpi
          label="Yetersiz Stok"
          value={snapshot.insufficientStockCount}
          description="Kullanılabilir miktar ≤ 0"
          icon={<Boxes aria-hidden="true" />}
        />
        <ErpKpi
          label="Açık Satın Alma"
          value={snapshot.openPurchaseOrderCount}
          description="Açık satır içeren PO"
          icon={<ShoppingCart aria-hidden="true" />}
        />
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Depolar</h2>
          <p>Harici ERP depo kayıtları — salt okunur</p>
        </div>
        <div className="erp-table-wrap">
          {warehouses.length === 0 ? (
            <div className="erp-empty">Henüz ERP depo kaydı yok. Önce depo senkronizasyonu çalıştırın.</div>
          ) : (
            <table className="erp-table">
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
                    <td>{getErpProviderLabel(warehouse.provider)}</td>
                    <td>
                      <span
                        className={`erp-status erp-status--${warehouse.freshness === "fresh" ? "ready" : warehouse.freshness === "stale" ? "paused" : "in-progress"}`}
                      >
                        {getErpSourceFreshnessLabel(warehouse.freshness)}
                      </span>
                    </td>
                    <td>{formatErpDateTime(warehouse.updatedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Stok Kayıtları</h2>
          <p>Depo bazlı ürün stok projeksiyonları</p>
        </div>
        <div className="erp-table-wrap">
          {inventoryBalances.length === 0 ? (
            <div className="erp-empty">Henüz stok kaydı yok. Önce stok senkronizasyonu çalıştırın.</div>
          ) : (
            <table className="erp-table erp-table--inventory">
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
                      <span
                        className={`erp-status erp-status--${balance.freshness === "fresh" ? "ready" : balance.freshness === "stale" ? "paused" : "in-progress"}`}
                      >
                        {getErpSourceFreshnessLabel(balance.freshness)}
                      </span>
                    </td>
                    <td>{getErpProviderLabel(balance.provider)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Senkronizasyon Sağlığı</h2>
          <p>Depo, stok ve satın alma senkronizasyon güvenilirliği</p>
        </div>
        <div className="erp-table-wrap">
          {inventoryReliability.length === 0 ? (
            <div className="erp-empty">Senkronizasyon güvenilirlik verisi yok.</div>
          ) : (
            <table className="erp-table erp-table--sync-reliability">
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
                        <span className={`erp-status erp-status--${tone}`}>
                          {getErpSourceFreshnessLabel(row.freshnessStatus)}
                        </span>
                      </td>
                      <td>{row.currentErrorMessage ?? "—"}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </main>
  );
}

function ErpKpi({
  label,
  value,
  description,
  icon,
}: Readonly<{
  label: string;
  value: number | string;
  description: string;
  icon: ReactNode;
}>) {
  return (
    <div className="erp-kpi">
      <span className="erp-kpi__icon" aria-hidden="true">
        {icon}
      </span>
      <div>
        <span className="erp-kpi__label">{label}</span>
        <strong className="erp-kpi__value">{value}</strong>
        <small className="erp-kpi__desc">{description}</small>
      </div>
    </div>
  );
}

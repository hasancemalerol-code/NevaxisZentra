import {
  AlertTriangle,
  Boxes,
  CircleGauge,
  Package,
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

  const nothingImported = warehouses.length === 0 && inventoryBalances.length === 0;
  // A reliability table of dashes (never synced) carries no information: show it once there is a run to report.
  const hasReliabilityData = inventoryReliability.some(
    (row) => row.lastSuccessfulSyncAtUtc || row.lastFailedSyncAtUtc || row.lastRunStatus,
  );

  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="Stok ve Depolar"
        description="Harici ERP depo ve stok özetleri — salt okunur görünüm. Stok hareketi veya ERP yazma işlemi yoktur."
        notice={
          <>
            <ErpReadonlyNotice>
              Bu bağlantı ERP stok ve satın alma kayıtlarını salt okunur olarak içe aktarır.
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
          description="Tanımlı depo"
          icon={<Warehouse aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Stok Kaydı"
          value={snapshot.inventoryRecordCount}
          description="Depo ve ürün satırı"
          icon={<Package aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Güncel Veri"
          value={snapshot.freshRecordCount}
          description="Güncel kayıt"
          icon={<CircleGauge aria-hidden="true" />}
          tone="success"
        />
        <ErpKpiCard
          label="Eski Veri"
          value={snapshot.staleRecordCount}
          description="Eski kayıt"
          icon={<AlertTriangle aria-hidden="true" />}
          tone="warning"
        />
        <ErpKpiCard
          label="Yetersiz Stok"
          value={snapshot.insufficientStockCount}
          description="Miktarı sıfır veya altı"
          icon={<Boxes aria-hidden="true" />}
          tone="danger"
        />
      </ErpKpiGrid>

      {nothingImported ? (
        <section className="erp-section">
          <ErpSectionHeader
            title="Depolar ve Stok Kayıtları"
            description="Harici ERP depo ve stok özetleri — salt okunur"
          />
          <ErpEmptyState
            title="Henüz ERP depo veya stok kaydı yok."
            description="Önce depo ve stok eşitlemesini çalıştırın."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        </section>
      ) : null}

      {nothingImported ? null : (
      <section className="erp-section">
        <ErpSectionHeader
          title="Depolar"
          description="Harici ERP depo kayıtları — salt okunur"
        />
        {warehouses.length === 0 ? (
          <ErpEmptyState
            title="Henüz ERP depo kaydı yok."
            description="Önce depo eşitlemesini çalıştırın."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        ) : (
          <ErpDataTable label="ERP depoları">
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
      )}

      {nothingImported ? null : (
      <section className="erp-section">
        <ErpSectionHeader
          title="Stok Kayıtları"
          description="Depo bazlı ürün stok özetleri"
        />
        {inventoryBalances.length === 0 ? (
          <ErpEmptyState
            title="Henüz stok kaydı yok."
            description="Önce stok eşitlemesini çalıştırın."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        ) : (
          <ErpDataTable label="ERP stok kayıtları" className="erp-table--inventory">
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
      )}

      {hasReliabilityData ? (
      <section className="erp-section">
        <ErpSectionHeader
          title="Eşitleme Sağlığı"
          description="Depo, stok ve satın alma eşitleme güvenilirliği"
        />
        {(

          <ErpDataTable label="Stok eşitleme sağlığı" className="erp-table--sync-reliability">
            <thead>
              <tr>
                <th>Varlık</th>
                <th>Son başarılı eşitleme</th>
                <th>Son Hata</th>
                <th>Son Durum</th>
                <th>Ardışık Hata</th>
                <th>Tazelik</th>
                <th>Hata</th>
              </tr>
            </thead>
            <tbody>
              {inventoryReliability.map((row) => {
                const tone =
                  row.freshnessStatus === "fresh"
                    ? getErpConnectionStatusTone("healthy")
                    : row.freshnessStatus === "stale"
                      ? getErpConnectionStatusTone("degraded")
                      : "neutral";

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
      ) : null}
    </div>
  );
}

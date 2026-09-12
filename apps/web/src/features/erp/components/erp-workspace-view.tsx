import Link from "next/link";
import {
  Cable,
  CheckCircle2,
  CircleGauge,
  Package,
  TriangleAlert,
  Users,
} from "lucide-react";

import type {
  CanonicalErpCustomer,
  CanonicalErpProduct,
  ErpConnection,
  ErpSyncRun,
  ErpWorkspaceSummary,
} from "../domain/erp";
import {
  formatErpDateTime,
  formatErpKpiTimestamp,
  getErpConnectionStatusLabel,
  getErpConnectionStatusTone,
  getErpEntityTypeLabel,
  getErpLogoCapabilities,
  getErpLogoSetupGuidance,
  getErpMikroCapabilities,
  getErpMikroSetupGuidance,
  getErpNetsisCapabilities,
  getErpNetsisSetupGuidance,
  getErpProviderLabel,
  getErpSyncStatusLabel,
} from "../presentation/erp-labels";
import {
  ErpConnectionCard,
  ErpDataTable,
  ErpEmptyState,
  ErpFreshnessBadge,
  ErpHeaderStatusContext,
  ErpKpiCard,
  ErpKpiGrid,
  ErpPageHeader,
  ErpSectionHeader,
  ErpStatusBadge,
  ErpTableIdentifierCell,
  ErpInfoBanner,
  getErpFacilityDisplayLabel,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

const SIMULATOR_CAPABILITIES = [
  "customers.read",
  "products.read",
  "orders.read",
  "warehouses.read",
  "inventory.read",
  "purchasing.read",
  "production.read",
  "documents.read",
] as const;

type ErpWorkspaceViewProps = Readonly<{
  summary: ErpWorkspaceSummary;
  connections: readonly ErpConnection[];
  syncRuns: readonly ErpSyncRun[];
  customers: readonly CanonicalErpCustomer[];
  products: readonly CanonicalErpProduct[];
  activeHref?: string;
}>;

export function ErpWorkspaceView({
  summary,
  connections,
  syncRuns,
  customers,
  products,
  activeHref = "/erp-entegrasyonlari/baglanti",
}: ErpWorkspaceViewProps) {
  const recentSyncRuns = [...syncRuns]
    .sort((a, b) => b.startedAtUtc.localeCompare(a.startedAtUtc))
    .slice(0, 12);

  const latestByEntity = buildLatestSyncByEntity(syncRuns);
  const isConnectionPage = activeHref === "/erp-entegrasyonlari/baglanti";
  const isSyncPage = activeHref === "/erp-entegrasyonlari/senkronizasyonlar";
  const isHealthPage = activeHref === "/erp-entegrasyonlari/saglik";
  const isErrorPage = activeHref === "/erp-entegrasyonlari/hata-kuyrugu";

  const pageTitle = isConnectionPage
    ? "ERP Entegrasyonları"
    : isSyncPage
      ? "Senkronizasyon Merkezi"
      : isHealthPage
        ? "Bağlantı Sağlığı"
        : isErrorPage
          ? "Entegrasyon Hata Kuyruğu"
          : "ERP Entegrasyonları";

  const pageDescription = isConnectionPage
    ? "ERP bağlantılarınızı, veri akışını ve kontrollü ERP işlemlerini tek merkezden yönetin."
    : isSyncPage
      ? "Veri senkronizasyon durumunu, son çalışmaları ve güncellik sinyallerini izleyin."
      : isHealthPage
        ? "ERP bağlantılarının sağlık durumu, gecikme ve son kontrol sonuçları."
        : isErrorPage
          ? "Senkronizasyon ve entegrasyon sırasında oluşan hataların özeti."
          : "ERP bağlantılarınızı, veri akışını ve kontrollü ERP işlemlerini tek merkezden yönetin.";

  const primaryConnection = connections[0];
  const headerHealthStatus = primaryConnection?.status ?? (summary.healthyCount > 0 ? "healthy" : "configured");

  return (
    <main className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title={pageTitle}
        description={pageDescription}
        aside={
          isConnectionPage ? (
            <ErpHeaderStatusContext
              healthLabel={getErpConnectionStatusLabel(headerHealthStatus)}
              healthTone={getErpConnectionStatusTone(headerHealthStatus)}
              syncLabel={`Son senkronizasyon: ${formatErpDateTime(summary.lastSyncAtUtc)}`}
            />
          ) : undefined
        }
      />

      <ErpWorkspaceNav activeHref={activeHref} />

      {(isConnectionPage || isHealthPage) && (
        <ErpKpiGrid>
          <ErpKpiCard
            label="ERP Bağlantıları"
            value={summary.connectionCount}
            description="Tanımlı bağlantı"
            icon={<Cable aria-hidden="true" />}
          />
          <ErpKpiCard
            label="Sağlıklı"
            value={summary.healthyCount}
            description="Sağlık kontrolü başarılı"
            icon={<CheckCircle2 aria-hidden="true" />}
            tone="success"
          />
          <ErpKpiCard
            label="Dikkat Gerektiren"
            value={summary.problematicCount}
            description={summary.problematicCount > 0 ? "İnceleme gereken bağlantı" : "Açık uyarı yok"}
            icon={<TriangleAlert aria-hidden="true" />}
            tone={summary.problematicCount > 0 ? "warning" : "neutral"}
          />
          <ErpKpiCard
            label="Son Senkronizasyon"
            value=""
            timestamp={formatErpKpiTimestamp(summary.lastSyncAtUtc)}
            description="En son tamamlanan senkronizasyon"
            icon={<CircleGauge aria-hidden="true" />}
            tone="info"
          />
          <ErpKpiCard
            label="ERP Müşterileri"
            value={summary.customerCount}
            description="Kanonik müşteri kaydı"
            icon={<Users aria-hidden="true" />}
          />
          <ErpKpiCard
            label="ERP Ürünleri"
            value={summary.productCount}
            description="Kanonik ürün kaydı"
            icon={<Package aria-hidden="true" />}
          />
        </ErpKpiGrid>
      )}

      {(isConnectionPage || isHealthPage) && (
        <section className="erp-section erp-section--compact" id="baglanti-sagligi">
          <ErpSectionHeader
            title="Bağlantı Sağlığı"
            description="Bağlantıların erişilebilirliği ve son kontrol durumu"
          />
          {connections.length === 0 ? (
            <ErpEmptyState
              title="Henüz ERP bağlantısı bulunmuyor."
              description="Pilot ortamda ERP Simulator bağlantısı bootstrap ile oluşturulur."
            />
          ) : (
            <div className="erp-connection-grid">
              {connections.map((connection) => (
                <ErpConnectionCard
                  key={connection.id}
                  provider={connection.provider}
                  providerDisplayName={connection.providerDisplayName}
                  name={connection.name}
                  companyCode={connection.externalCompanyCode}
                  facilityLabel={getErpFacilityDisplayLabel(connection.facilityId)}
                  status={connection.status}
                  latencyMs={connection.lastHealthLatencyMs}
                  lastHealthCheck={formatErpDateTime(connection.lastHealthCheckAtUtc)}
                  lastSuccessfulSync={formatErpDateTime(connection.lastSuccessfulSyncAtUtc)}
                  capabilities={
                    connection.provider === "simulator"
                      ? SIMULATOR_CAPABILITIES
                      : connection.provider === "logo"
                        ? getErpLogoCapabilities()
                        : connection.provider === "mikro"
                          ? getErpMikroCapabilities()
                          : connection.provider === "netsis"
                            ? getErpNetsisCapabilities()
                          : []
                  }
                  connectionMode={connection.connectionMode}
                  hasCredentialReference={connection.hasCredentialReference}
                  lastErrorCode={connection.lastErrorCode}
                  manageHref={`/erp-entegrasyonlari/baglanti#connection-${connection.id}`}
                  syncHref="/erp-entegrasyonlari/senkronizasyonlar"
                />
              ))}
            </div>
          )}
        </section>
      )}

      {(isSyncPage || isConnectionPage) && (
        <section className="erp-section" id="senkronizasyon-durumu">
          <ErpSectionHeader
            title="Senkronizasyon Durumu"
            description="Varlık bazında son senkronizasyon ve güncellik özeti"
            action={
              <Link href="/erp-entegrasyonlari/senkronizasyonlar" className="erp-action erp-action--tertiary">
                Tüm senkronizasyonlar
              </Link>
            }
          />
          <div className="erp-sync-entity-grid">
            {["customers", "products", "orders", "warehouses", "inventory", "purchasing", "production", "documents"].map(
              (entityType) => {
                const latest = latestByEntity.get(entityType);
                const statusTone =
                  latest?.status === "completed"
                    ? "ready"
                    : latest?.status === "failed"
                      ? "critical"
                      : latest?.status === "completed-with-errors"
                        ? "paused"
                        : "in-progress";
                return (
                  <div key={entityType} className="erp-sync-entity-row">
                    <div className="erp-sync-entity-row__title">
                      <strong>{getErpEntityTypeLabel(entityType)}</strong>
                      <ErpStatusBadge
                        label={latest ? getErpSyncStatusLabel(latest.status) : "Henüz çalıştırılmadı"}
                        tone={statusTone}
                      />
                    </div>
                    <div className="erp-sync-entity-row__metrics">
                      <span>
                        <small>Son başarı</small>
                        {formatErpDateTime(latest?.completedAtUtc)}
                      </span>
                      <span>
                        <small>Güncellik</small>
                        <ErpFreshnessBadge freshness={latest ? "fresh" : "unknown"} />
                      </span>
                      <span>
                        <small>Okunan</small>
                        {latest?.recordsRead ?? 0}
                      </span>
                      <span>
                        <small>Değişen</small>
                        {(latest?.recordsCreated ?? 0) + (latest?.recordsUpdated ?? 0)}
                      </span>
                      {(latest?.recordsFailed ?? 0) > 0 ? (
                        <span className="erp-sync-entity-row__failed">
                          <small>Hatalı</small>
                          {latest?.recordsFailed}
                        </span>
                      ) : null}
                    </div>
                  </div>
                );
              },
            )}
          </div>
        </section>
      )}

      {(isSyncPage || isConnectionPage) && (
        <section className="erp-section" id="son-senkronizasyonlar">
          <ErpSectionHeader
            title="Son Senkronizasyonlar"
            description="Operasyonel senkronizasyon aktivitesi"
          />
          {recentSyncRuns.length === 0 ? (
            <ErpEmptyState title="Henüz senkronizasyon çalıştırılmamış." />
          ) : (
            <ErpDataTable className="erp-table--sync-runs">
              <thead>
                <tr>
                  <th>Veri</th>
                  <th>Başlangıç</th>
                  <th>Tamamlanma</th>
                  <th>Durum</th>
                  <th>Okunan</th>
                  <th>Değişen</th>
                  <th>Atlanan</th>
                  <th>Hatalı</th>
                </tr>
              </thead>
              <tbody>
                {recentSyncRuns.map((run) => (
                  <tr key={run.id}>
                    <td>{getErpEntityTypeLabel(run.entityType)}</td>
                    <td>{formatErpDateTime(run.startedAtUtc)}</td>
                    <td>{formatErpDateTime(run.completedAtUtc)}</td>
                    <td>
                      <ErpStatusBadge
                        label={getErpSyncStatusLabel(run.status)}
                        tone={
                          run.status === "completed"
                            ? "ready"
                            : run.status === "failed"
                              ? "critical"
                              : "paused"
                        }
                      />
                    </td>
                    <td>{run.recordsRead}</td>
                    <td>{run.recordsCreated + run.recordsUpdated}</td>
                    <td>{run.recordsSkipped}</td>
                    <td>{run.recordsFailed}</td>
                  </tr>
                ))}
              </tbody>
            </ErpDataTable>
          )}
        </section>
      )}

      {isErrorPage && (
        <section className="erp-section">
          <ErpSectionHeader
            title="Entegrasyon Hataları"
            description="Son senkronizasyonlardan kaynaklanan hata kayıtları"
          />
          {recentSyncRuns.filter((run) => run.errorMessage || run.recordsFailed > 0).length === 0 ? (
            <ErpEmptyState title="Açık entegrasyon hatası bulunmuyor." />
          ) : (
            <ErpDataTable>
              <thead>
                <tr>
                  <th>Veri</th>
                  <th>Başlangıç</th>
                  <th>Durum</th>
                  <th>Hatalı Kayıt</th>
                  <th>Hata</th>
                </tr>
              </thead>
              <tbody>
                {recentSyncRuns
                  .filter((run) => run.errorMessage || run.recordsFailed > 0)
                  .map((run) => (
                    <tr key={run.id}>
                      <td>{getErpEntityTypeLabel(run.entityType)}</td>
                      <td>{formatErpDateTime(run.startedAtUtc)}</td>
                      <td>{getErpSyncStatusLabel(run.status)}</td>
                      <td>{run.recordsFailed}</td>
                      <td>{run.errorMessage ?? "—"}</td>
                    </tr>
                  ))}
              </tbody>
            </ErpDataTable>
          )}
        </section>
      )}

      {isConnectionPage && (
        <>
          <section className="erp-section" id="erp-musteriler">
            <ErpSectionHeader
              title="ERP Müşterileri"
              description="Bağlı ERP sisteminden salt okunur olarak alınan kanonik müşteri kayıtları."
            />
            {customers.length === 0 ? (
              <ErpEmptyState title="Henüz ERP müşterisi bulunmuyor." />
            ) : (
              <ErpDataTable>
                <thead>
                  <tr>
                    <th>ERP Müşteri Kodu</th>
                    <th>Müşteri</th>
                    <th>Vergi No</th>
                    <th>Ülke</th>
                    <th>Durum</th>
                    <th>Kaynak</th>
                    <th>Güncellik</th>
                  </tr>
                </thead>
                <tbody>
                  {customers.map((customer) => (
                    <tr key={customer.id}>
                      <td>
                        <ErpTableIdentifierCell
                          primary={customer.externalCode ?? customer.externalId}
                          secondary={customer.taxOffice ?? undefined}
                        />
                      </td>
                      <td>{customer.name}</td>
                      <td>{customer.taxNumber ?? "—"}</td>
                      <td>{customer.countryCode ?? "—"}</td>
                      <td>{customer.isActive ? "Aktif" : "Pasif"}</td>
                      <td>{getErpProviderLabel(customer.provider)}</td>
                      <td>{formatErpDateTime(customer.updatedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </ErpDataTable>
            )}
          </section>

          <section className="erp-section" id="erp-urunler">
            <ErpSectionHeader
              title="ERP Ürünleri"
              description="Bağlı ERP sisteminden salt okunur olarak alınan kanonik ürün kayıtları."
            />
            {products.length === 0 ? (
              <ErpEmptyState title="Henüz ERP ürünü bulunmuyor." />
            ) : (
              <ErpDataTable>
                <thead>
                  <tr>
                    <th>ERP Ürün Kodu</th>
                    <th>Ürün</th>
                    <th>Birim</th>
                    <th>Barkod</th>
                    <th>Durum</th>
                    <th>Kaynak</th>
                    <th>Güncellik</th>
                  </tr>
                </thead>
                <tbody>
                  {products.map((product) => (
                    <tr key={product.id}>
                      <td>{product.externalCode ?? product.externalId}</td>
                      <td>{product.name}</td>
                      <td>{product.unit ?? "—"}</td>
                      <td>{product.barcode ?? "—"}</td>
                      <td>{product.isActive ? "Aktif" : "Pasif"}</td>
                      <td>{getErpProviderLabel(product.provider)}</td>
                      <td>{formatErpDateTime(product.updatedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </ErpDataTable>
            )}
          </section>
        </>
      )}

      {isConnectionPage && connections.some((connection) => connection.provider === "logo") ? (
        <section className="erp-section erp-section--compact">
          <ErpInfoBanner title="Logo kurulum yönlendirmesi">
            {getErpLogoSetupGuidance()}
          </ErpInfoBanner>
        </section>
      ) : null}

      {isConnectionPage && connections.some((connection) => connection.provider === "netsis") ? (
        <section className="erp-section" aria-label="Netsis kurulum yönlendirmesi">
          <ErpInfoBanner title="Netsis kurulum yönlendirmesi">
            {getErpNetsisSetupGuidance()}
          </ErpInfoBanner>
        </section>
      ) : null}
      {isConnectionPage && connections.some((connection) => connection.provider === "mikro") ? (
        <section className="erp-section erp-section--compact">
          <ErpInfoBanner title="Mikro kurulum yönlendirmesi">
            {getErpMikroSetupGuidance()}
          </ErpInfoBanner>
        </section>
      ) : null}

      <section className="erp-section erp-section--compact" aria-label="Kaynak notu">
        <ErpInfoBanner title="Salt okunur projeksiyon">
          Harici ERP sistem kayıt sahibidir. NevaxisFactOS yalnızca salt okunur kanonik projeksiyonları saklar.
        </ErpInfoBanner>
      </section>
    </main>
  );
}

function buildLatestSyncByEntity(
  syncRuns: readonly ErpSyncRun[],
): Map<string, ErpSyncRun> {
  const map = new Map<string, ErpSyncRun>();
  for (const run of [...syncRuns].sort((a, b) => b.startedAtUtc.localeCompare(a.startedAtUtc))) {
    if (!map.has(run.entityType) && run.status === "completed") {
      map.set(run.entityType, run);
    }
  }
  return map;
}

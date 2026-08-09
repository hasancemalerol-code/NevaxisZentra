import Link from "next/link";
import type { ReactNode } from "react";
import {
  Activity,
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
  getErpConnectionStatusLabel,
  getErpConnectionStatusTone,
  getErpEntityTypeLabel,
  getErpProviderLabel,
  getErpSyncStatusLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

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
    .slice(0, 8);

  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>ERP Entegrasyonları</h1>
          <span>
            Bağlı ERP sistemlerinin bağlantı durumunu, veri senkronizasyonunu ve
            kanonik kayıtlarını yönetin.
          </span>
          <p className="erp-simulator-note">
            ERP-1L yalnızca ERP Simulator bağlantısını uygular. Logo / Mikro / Netsis
            gerçek bağlantı değildir.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref={activeHref} />

      <section className="erp-kpi-grid" aria-label="ERP özet göstergeleri">
        <ErpKpi
          label="Bağlantılar"
          value={summary.connectionCount}
          description="Tanımlı ERP bağlantısı"
          icon={<Cable aria-hidden="true" />}
        />
        <ErpKpi
          label="Sağlıklı"
          value={summary.healthyCount}
          description="Sağlık kontrolü başarılı"
          icon={<CheckCircle2 aria-hidden="true" />}
        />
        <ErpKpi
          label="Sorunlu"
          value={summary.problematicCount}
          description="Dikkat gerektiren bağlantı"
          icon={<TriangleAlert aria-hidden="true" />}
        />
        <ErpKpi
          label="Son Senkronizasyon"
          value={formatErpDateTime(summary.lastSyncAtUtc)}
          description="En son tamamlanan sync"
          icon={<CircleGauge aria-hidden="true" />}
        />
        <ErpKpi
          label="Müşteri Kayıtları"
          value={summary.customerCount}
          description="Kanonik ERP müşteri"
          icon={<Users aria-hidden="true" />}
        />
        <ErpKpi
          label="Ürün Kayıtları"
          value={summary.productCount}
          description="Kanonik ERP ürün"
          icon={<Package aria-hidden="true" />}
        />
      </section>

      <section className="erp-section" id="connection-health">
        <div className="erp-section__header">
          <h2>Connection Health</h2>
          <p>Bağlantı durumu ve son sağlık kontrolü</p>
        </div>
        <div className="erp-table-wrap">
          {connections.length === 0 ? (
            <div className="erp-empty">Henüz ERP bağlantısı yok.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>Provider</th>
                  <th>Durum</th>
                  <th>Son Sağlık Kontrolü</th>
                  <th>Gecikme</th>
                  <th>Hata</th>
                </tr>
              </thead>
              <tbody>
                {connections.map((connection) => {
                  const tone = getErpConnectionStatusTone(connection.status);
                  return (
                    <tr key={`health-${connection.id}`}>
                      <td>
                        {getErpProviderLabel(
                          connection.provider,
                          connection.providerDisplayName,
                        )}
                      </td>
                      <td>
                        <span className={`erp-status erp-status--${tone}`}>
                          {getErpConnectionStatusLabel(connection.status)}
                        </span>
                      </td>
                      <td>{formatErpDateTime(connection.lastHealthCheckAtUtc)}</td>
                      <td>
                        {connection.lastHealthLatencyMs == null
                          ? "—"
                          : `${connection.lastHealthLatencyMs} ms`}
                      </td>
                      <td>{connection.lastErrorMessage ?? "—"}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="erp-section" id="connections">
        <div className="erp-section__header">
          <h2>Connections</h2>
          <p>Tenant / facility scoped ERP connection profiles</p>
        </div>
        <div className="erp-table-wrap">
          {connections.length === 0 ? (
            <div className="erp-empty">Bağlantı kaydı bulunamadı.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>Provider</th>
                  <th>Bağlantı Adı</th>
                  <th>ERP Company</th>
                  <th>Facility</th>
                  <th>Status</th>
                  <th>Son Başarılı Sync</th>
                  <th>Son Health Check</th>
                  <th>Aksiyonlar</th>
                </tr>
              </thead>
              <tbody>
                {connections.map((connection) => {
                  const tone = getErpConnectionStatusTone(connection.status);
                  return (
                    <tr key={connection.id}>
                      <td>
                        {getErpProviderLabel(
                          connection.provider,
                          connection.providerDisplayName,
                        )}
                      </td>
                      <td>{connection.name}</td>
                      <td>{connection.externalCompanyCode ?? "—"}</td>
                      <td>
                        <code>{connection.facilityId.slice(0, 8)}…</code>
                      </td>
                      <td>
                        <span className={`erp-status erp-status--${tone}`}>
                          {getErpConnectionStatusLabel(connection.status)}
                        </span>
                      </td>
                      <td>{formatErpDateTime(connection.lastSuccessfulSyncAtUtc)}</td>
                      <td>{formatErpDateTime(connection.lastHealthCheckAtUtc)}</td>
                      <td>
                        <div className="erp-actions">
                          <Link
                            className="erp-action erp-action--primary"
                            href={`/erp-entegrasyonlari/baglanti#connection-${connection.id}`}
                          >
                            Bağlantıyı Aç
                          </Link>
                          <span className="erp-action" title="API: health-check / sync">
                            Sağlığı Kontrol Et
                          </span>
                          <span className="erp-action">Müşterileri Senkronize Et</span>
                          <span className="erp-action">Ürünleri Senkronize Et</span>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="erp-section" id="sync-runs">
        <div className="erp-section__header">
          <h2>Recent Sync Runs</h2>
          <p>Okunan / oluşturulan / güncellenen / atlanan / hatalı sayaçlar</p>
        </div>
        <div className="erp-table-wrap">
          {recentSyncRuns.length === 0 ? (
            <div className="erp-empty">Henüz senkronizasyon çalışması yok.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>Entity</th>
                  <th>Started</th>
                  <th>Completed</th>
                  <th>Status</th>
                  <th>Read</th>
                  <th>Created</th>
                  <th>Updated</th>
                  <th>Skipped</th>
                  <th>Failed</th>
                  <th>Error</th>
                </tr>
              </thead>
              <tbody>
                {recentSyncRuns.map((run) => (
                  <tr key={run.id}>
                    <td>{getErpEntityTypeLabel(run.entityType)}</td>
                    <td>{formatErpDateTime(run.startedAtUtc)}</td>
                    <td>{formatErpDateTime(run.completedAtUtc)}</td>
                    <td>{getErpSyncStatusLabel(run.status)}</td>
                    <td>{run.recordsRead}</td>
                    <td>{run.recordsCreated}</td>
                    <td>{run.recordsUpdated}</td>
                    <td>{run.recordsSkipped}</td>
                    <td>{run.recordsFailed}</td>
                    <td>{run.errorMessage ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="erp-section" id="canonical-customers">
        <div className="erp-section__header">
          <h2>Canonical Customers</h2>
          <p>Harici ERP müşteri projeksiyonları — salt okunur</p>
        </div>
        <div className="erp-table-wrap">
          {customers.length === 0 ? (
            <div className="erp-empty">Kanonik müşteri kaydı yok.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>ERP Kodu</th>
                  <th>Müşteri</th>
                  <th>Vergi No</th>
                  <th>Ülke</th>
                  <th>Durum</th>
                  <th>Kaynak</th>
                  <th>Son Güncelleme</th>
                </tr>
              </thead>
              <tbody>
                {customers.map((customer) => (
                  <tr key={customer.id}>
                    <td>{customer.externalCode ?? customer.externalId}</td>
                    <td>{customer.name}</td>
                    <td>{customer.taxNumber ?? "—"}</td>
                    <td>{customer.countryCode ?? "—"}</td>
                    <td>{customer.isActive ? "Aktif" : "Pasif"}</td>
                    <td>{getErpProviderLabel(customer.provider)}</td>
                    <td>{formatErpDateTime(customer.updatedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="erp-section" id="canonical-products">
        <div className="erp-section__header">
          <h2>Canonical Products</h2>
          <p>Harici ERP ürün projeksiyonları — salt okunur</p>
        </div>
        <div className="erp-table-wrap">
          {products.length === 0 ? (
            <div className="erp-empty">Kanonik ürün kaydı yok.</div>
          ) : (
            <table className="erp-table">
              <thead>
                <tr>
                  <th>ERP Kodu</th>
                  <th>Ürün</th>
                  <th>Birim</th>
                  <th>Barkod</th>
                  <th>Durum</th>
                  <th>Kaynak</th>
                  <th>Son Güncelleme</th>
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
            </table>
          )}
        </div>
      </section>

      <section className="erp-section" aria-label="Kaynak notu">
        <p className="erp-simulator-note">
          <Activity aria-hidden="true" />
          External ERP remains system of record. CrevaFactOS stores read-only
          canonical projections only. No writeback in ERP-1L.
        </p>
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

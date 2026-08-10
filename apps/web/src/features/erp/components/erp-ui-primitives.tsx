import Link from "next/link";
import type { ReactNode } from "react";

import {
  getErpConnectionStatusLabel,
  getErpConnectionStatusTone,
  getErpLogoAdapterReadyLabel,
  getErpLogoConnectionStateLabel,
  getErpLogoLiveValidatedLabel,
  getErpLogoTransportLabel,
  getErpMikroAdapterReadyLabel,
  getErpMikroConnectionStateLabel,
  getErpMikroLiveValidatedLabel,
  getErpMikroTransportLabel,
  getErpProviderLabel,
  getErpSourceFreshnessLabel,
  getErpSourceFreshnessTone,
} from "../presentation/erp-labels";

/* ── Page shell ── */

type ErpPageHeaderProps = Readonly<{
  eyebrow?: string;
  title: string;
  description: string;
  aside?: ReactNode;
  notice?: ReactNode;
}>;

export function ErpPageHeader({
  eyebrow = "ERP ENTEGRASYONLARI",
  title,
  description,
  aside,
  notice,
}: ErpPageHeaderProps) {
  return (
    <header className="erp-workspace-header erp-workspace-header--executive production-execution-header">
      <div className="erp-workspace-header__intro">
        <p className="erp-page-eyebrow">{eyebrow}</p>
        <h1>{title}</h1>
        <span className="erp-page-description">{description}</span>
        {notice}
      </div>
      {aside ? <div className="erp-workspace-header__aside">{aside}</div> : null}
    </header>
  );
}

export function ErpHeaderStatusContext({
  healthLabel,
  healthTone,
  syncLabel,
}: Readonly<{
  healthLabel: string;
  healthTone: "ready" | "paused" | "critical" | "in-progress";
  syncLabel: string;
}>) {
  return (
    <div className="erp-header-status">
      <ErpStatusBadge label={healthLabel} tone={healthTone} />
      <span className="erp-header-status__sync">{syncLabel}</span>
    </div>
  );
}

type ErpSectionHeaderProps = Readonly<{
  title: string;
  description?: string;
  action?: ReactNode;
}>;

export function ErpSectionHeader({ title, description, action }: ErpSectionHeaderProps) {
  return (
    <div className="erp-section__header">
      <div className="erp-section__header-text">
        <h2>{title}</h2>
        {description ? <p>{description}</p> : null}
      </div>
      {action ? <div className="erp-section__header-action">{action}</div> : null}
    </div>
  );
}

/* ── KPI ── */

type ErpKpiCardProps = Readonly<{
  label: string;
  value: number | string;
  description?: string;
  icon?: ReactNode;
  tone?: "neutral" | "success" | "warning" | "danger" | "info";
  timestamp?: { date: string; time: string };
}>;

export function ErpKpiCard({
  label,
  value,
  description,
  icon,
  tone = "neutral",
  timestamp,
}: ErpKpiCardProps) {
  return (
    <div className={`erp-kpi erp-kpi--${tone}`}>
      {icon ? (
        <span className="erp-kpi__icon" aria-hidden="true">
          {icon}
        </span>
      ) : null}
      <div className="erp-kpi__body">
        <span className="erp-kpi__label">{label}</span>
        {timestamp ? (
          <span className="erp-kpi__value-stack">
            <strong className="erp-kpi__value">{timestamp.date}</strong>
            {timestamp.time ? <span className="erp-kpi__value-time">{timestamp.time}</span> : null}
          </span>
        ) : (
          <strong className="erp-kpi__value">{value}</strong>
        )}
        {description ? <small className="erp-kpi__desc">{description}</small> : null}
      </div>
    </div>
  );
}

export function ErpKpiGrid({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <section className="erp-kpi-grid" aria-label="Özet göstergeler">
      {children}
    </section>
  );
}

/* ── Badges ── */

type ErpStatusBadgeProps = Readonly<{
  label: string;
  tone: "ready" | "paused" | "critical" | "in-progress" | "neutral";
}>;

export function ErpStatusBadge({ label, tone }: ErpStatusBadgeProps) {
  return <span className={`erp-status erp-status--${tone}`}>{label}</span>;
}

type ErpFreshnessBadgeProps = Readonly<{
  freshness: string;
}>;

export function ErpFreshnessBadge({ freshness }: ErpFreshnessBadgeProps) {
  return (
    <span className={`erp-freshness erp-freshness--${getErpSourceFreshnessTone(freshness)}`}>
      {getErpSourceFreshnessLabel(freshness)}
    </span>
  );
}

type ErpProviderBadgeProps = Readonly<{
  provider: string;
  displayName?: string;
}>;

export function ErpProviderBadge({ provider, displayName }: ErpProviderBadgeProps) {
  const isSimulator = provider === "simulator";
  const isLogo = provider === "logo";
  const isMikro = provider === "mikro";
  const primaryLabel = isSimulator ? "ERP Simulator" : getErpProviderLabel(provider, displayName);
  return (
    <span className={`erp-provider-badge${isSimulator ? " erp-provider-badge--simulator" : ""}${isLogo ? " erp-provider-badge--logo" : ""}${isMikro ? " erp-provider-badge--mikro" : ""}`}>
      <strong>{primaryLabel}</strong>
      {isSimulator ? <small>Geliştirme / Test</small> : null}
      {isLogo ? <small>{getErpLogoAdapterReadyLabel()}</small> : null}
      {isMikro ? <small>{getErpMikroAdapterReadyLabel()}</small> : null}
    </span>
  );
}

type ErpMappingBadgeProps = Readonly<{
  status: string;
  label: string;
}>;

export function ErpMappingBadge({ status, label }: ErpMappingBadgeProps) {
  const tone =
    status === "mapped"
      ? "mapped"
      : status === "suggested"
        ? "suggested"
        : status === "conflict"
          ? "conflict"
          : status === "ignored"
            ? "ignored"
            : "unmapped";
  return <span className={`erp-badge erp-badge--${tone}`}>{label}</span>;
}

type ErpHealthIndicatorProps = Readonly<{
  status: string;
  labelOverride?: string;
}>;

export function ErpHealthIndicator({ status, labelOverride }: ErpHealthIndicatorProps) {
  const tone = getErpConnectionStatusTone(status);
  return (
    <span className={`erp-health erp-health--${tone}`}>
      {labelOverride ?? getErpConnectionStatusLabel(status)}
    </span>
  );
}

/* ── Banners ── */

export function ErpSimulatorNotice({ variant = "info" }: Readonly<{ variant?: "info" | "writeback" }>) {
  const message =
    variant === "writeback"
      ? "ERP Simulator — gerçek ERP işlemi değildir."
      : "ERP Simulator — gerçek ERP bağlantısı değildir.";
  return (
    <p className={`erp-simulator-notice erp-simulator-notice--${variant}`} role="note">
      {message}
    </p>
  );
}

export function ErpReadonlyNotice({ children }: Readonly<{ children: ReactNode }>) {
  return <p className="erp-readonly-notice">{children}</p>;
}

type ErpInfoBannerProps = Readonly<{
  title?: string;
  children: ReactNode;
}>;

export function ErpInfoBanner({ title, children }: ErpInfoBannerProps) {
  return (
    <div className="erp-info-banner" role="note">
      {title ? <strong>{title}</strong> : null}
      <p>{children}</p>
    </div>
  );
}

type ErpWarningBannerProps = Readonly<{
  title?: string;
  children: ReactNode;
}>;

export function ErpWarningBanner({ title, children }: ErpWarningBannerProps) {
  return (
    <div className="erp-warning-banner" role="note">
      {title ? <strong>{title}</strong> : null}
      <p>{children}</p>
    </div>
  );
}

/* ── Tables ── */

export function ErpDataTable({
  children,
  className,
  minWidth,
}: Readonly<{
  children: ReactNode;
  className?: string;
  minWidth?: number;
}>) {
  return (
    <div className="erp-table-wrap">
      <table
        className={`erp-table${className ? ` ${className}` : ""}`}
        style={minWidth ? { minWidth: `${minWidth}px` } : undefined}
      >
        {children}
      </table>
    </div>
  );
}

export function ErpTableIdentifierCell({
  primary,
  secondary,
  href,
}: Readonly<{
  primary: ReactNode;
  secondary?: ReactNode;
  href?: string;
}>) {
  return (
    <div className="erp-table__identifier">
      {href ? <Link href={href}>{primary}</Link> : <span className="erp-table__primary">{primary}</span>}
      {secondary ? <small className="erp-table__secondary">{secondary}</small> : null}
    </div>
  );
}

export function ErpTableAction({ href, children }: Readonly<{ href: string; children: ReactNode }>) {
  return (
    <Link href={href} className="erp-table-action">
      {children}
    </Link>
  );
}

export function ErpTableActionCell({ children }: Readonly<{ children: ReactNode }>) {
  return <td className="erp-table__actions">{children}</td>;
}

/* ── Empty / detail ── */

type ErpEmptyStateProps = Readonly<{
  title: string;
  description?: string;
}>;

export function ErpEmptyState({ title, description }: ErpEmptyStateProps) {
  return (
    <div className="erp-empty-state">
      <p className="erp-empty-state__title">{title}</p>
      {description ? <p className="erp-empty-state__desc">{description}</p> : null}
    </div>
  );
}

export function ErpDetailSection({
  title,
  children,
}: Readonly<{ title: string; children: ReactNode }>) {
  return (
    <section className="erp-detail-section">
      <ErpSectionHeader title={title} />
      {children}
    </section>
  );
}

export function ErpMetadataGrid({ children }: Readonly<{ children: ReactNode }>) {
  return <dl className="erp-detail-grid">{children}</dl>;
}

export function ErpMetadataItem({
  label,
  children,
}: Readonly<{ label: string; children: ReactNode }>) {
  return (
    <div className="erp-metadata-item">
      <dt>{label}</dt>
      <dd>{children}</dd>
    </div>
  );
}

/* ── Connection card ── */

type ErpConnectionCardProps = Readonly<{
  provider: string;
  providerDisplayName?: string;
  name: string;
  companyCode: string | null;
  facilityLabel: string;
  status: string;
  connectionMode?: string;
  hasCredentialReference?: boolean;
  lastErrorCode?: string | null;
  latencyMs: number | null;
  lastHealthCheck: string;
  lastSuccessfulSync: string;
  capabilities: readonly string[];
  manageHref: string;
  healthHref?: string;
  syncHref: string;
}>;

const CAPABILITY_LABELS: Record<string, string> = {
  "customers.read": "Müşteri",
  "products.read": "Ürün",
  "orders.read": "Sipariş",
  "warehouses.read": "Depo",
  "inventory.read": "Stok",
  "purchasing.read": "Satın Alma",
  "production.read": "Üretim",
  "documents.read": "Belgeler",
};

export function ErpConnectionCard({
  provider,
  providerDisplayName,
  name,
  companyCode,
  facilityLabel,
  status,
  connectionMode = "api",
  hasCredentialReference = false,
  lastErrorCode = null,
  latencyMs,
  lastHealthCheck,
  lastSuccessfulSync,
  capabilities,
  manageHref,
  healthHref,
  syncHref,
}: ErpConnectionCardProps) {
  const isSimulator = provider === "simulator";
  const isLogo = provider === "logo";
  const isMikro = provider === "mikro";
  const isTestTransport = (isLogo || isMikro) && name.toLowerCase().includes("test transport");
  const logoStateLabel = isLogo
    ? getErpLogoConnectionStateLabel({
        status,
        hasCredentialReference,
        connectionMode,
        lastErrorCode,
      })
    : undefined;
  const mikroStateLabel = isMikro
    ? getErpMikroConnectionStateLabel({
        status,
        hasCredentialReference,
        connectionMode,
        lastErrorCode,
      })
    : undefined;
  const liveValidatedLabel = isLogo && status === "healthy" && !isTestTransport
    ? getErpLogoLiveValidatedLabel(true)
    : isMikro && status === "healthy" && !isTestTransport
      ? getErpMikroLiveValidatedLabel(true)
      : null;

  return (
    <article className="erp-connection-card erp-connection-card--compact">
      <div className="erp-connection-card__top">
        <div className="erp-connection-card__identity">
          <ErpProviderBadge provider={provider} displayName={providerDisplayName} />
          <h3 className="erp-connection-card__name">{name}</h3>
        </div>
        <div className="erp-connection-card__top-actions">
          <ErpHealthIndicator status={status} labelOverride={logoStateLabel ?? mikroStateLabel} />
          <Link href={manageHref} className="erp-action erp-action--primary">
            Bağlantıyı Yönet
          </Link>
        </div>
      </div>
      <dl className="erp-connection-card__meta erp-connection-card__meta--compact">
        <div>
          <dt>ERP Şirketi</dt>
          <dd>{companyCode ?? "—"}</dd>
        </div>
        {isLogo ? (
          <>
            <div>
              <dt>Bağlantı Yöntemi</dt>
              <dd>{getErpLogoTransportLabel(connectionMode, isTestTransport)}</dd>
            </div>
            <div>
              <dt>Logo Ürünü</dt>
              <dd>{isTestTransport ? "Logo Test Fixture" : "—"}</dd>
            </div>
          </>
        ) : null}
        {isMikro ? (
          <>
            <div>
              <dt>Bağlantı Yöntemi</dt>
              <dd>{getErpMikroTransportLabel(connectionMode, isTestTransport)}</dd>
            </div>
            <div>
              <dt>Mikro Ürünü</dt>
              <dd>{isTestTransport ? "Mikro Test Fixture" : "—"}</dd>
            </div>
            <div>
              <dt>Sürüm</dt>
              <dd>{isTestTransport ? "test-1.0" : "—"}</dd>
            </div>
            <div>
              <dt>Dönem</dt>
              <dd>{isTestTransport ? "1" : "—"}</dd>
            </div>
          </>
        ) : null}
        <div>
          <dt>Tesis</dt>
          <dd>{facilityLabel}</dd>
        </div>
        <div>
          <dt>Gecikme</dt>
          <dd>{latencyMs == null ? "—" : `${latencyMs} ms`}</dd>
        </div>
        <div>
          <dt>Son Sağlık Kontrolü</dt>
          <dd>{lastHealthCheck}</dd>
        </div>
        <div className="erp-connection-card__meta-span">
          <dt>Son Başarılı Senkronizasyon</dt>
          <dd>{lastSuccessfulSync}</dd>
        </div>
      </dl>
      <div className="erp-connection-card__footer">
        {capabilities.length > 0 ? (
          <div className="erp-capability-chips erp-capability-chips--compact" aria-label="Desteklenen yetenekler">
            {capabilities.map((cap) => (
              <span key={cap} className="erp-capability-chip">
                {CAPABILITY_LABELS[cap] ?? cap}
              </span>
            ))}
          </div>
        ) : null}
        {liveValidatedLabel ? (
          <p className="erp-connection-card__badge-note" role="status">
            {liveValidatedLabel}
          </p>
        ) : null}
        {isSimulator ? (
          <p className="erp-connection-card__disclaimer" role="note">
            ERP Simulator — gerçek ERP bağlantısı değildir.
          </p>
        ) : null}
        {isTestTransport && isLogo ? (
          <p className="erp-connection-card__disclaimer" role="note">
            Logo test transport — gerçek Logo doğrulaması değildir.
          </p>
        ) : null}
        {isTestTransport && isMikro ? (
          <p className="erp-connection-card__disclaimer" role="note">
            Test bağlantısı — gerçek Mikro ERP bağlantısı değildir.
          </p>
        ) : null}
        <div className="erp-connection-card__actions">
          {healthHref ? (
            <Link href={healthHref} className="erp-action erp-action--secondary">
              Sağlığı Kontrol Et
            </Link>
          ) : (
            <span className="erp-action erp-action--secondary" title="Sağlık kontrolü API üzerinden">
              Sağlığı Kontrol Et
            </span>
          )}
          <Link href={syncHref} className="erp-action erp-action--tertiary">
            Senkronizasyon Merkezi
          </Link>
        </div>
      </div>
    </article>
  );
}

/* ── Sync entity row ── */

export function ErpSyncEntityRow({
  entityLabel,
  statusLabel,
  statusTone,
  lastSuccess,
  freshness,
  recordsRead,
  recordsChanged,
  recordsFailed,
  attention,
}: Readonly<{
  entityLabel: string;
  statusLabel: string;
  statusTone: "ready" | "paused" | "critical" | "in-progress";
  lastSuccess: string;
  freshness: string;
  recordsRead: number;
  recordsChanged: number;
  recordsFailed: number;
  attention?: string | null;
}>) {
  return (
    <div className="erp-sync-entity-row">
      <div className="erp-sync-entity-row__title">
        <strong>{entityLabel}</strong>
        <ErpStatusBadge label={statusLabel} tone={statusTone} />
      </div>
      <div className="erp-sync-entity-row__metrics">
        <span>
          <small>Son başarı</small>
          {lastSuccess}
        </span>
        <span>
          <small>Güncellik</small>
          <ErpFreshnessBadge freshness={freshness} />
        </span>
        <span>
          <small>Okunan</small>
          {recordsRead}
        </span>
        <span>
          <small>Değişen</small>
          {recordsChanged}
        </span>
        {recordsFailed > 0 ? (
          <span className="erp-sync-entity-row__failed">
            <small>Hatalı</small>
            {recordsFailed}
          </span>
        ) : null}
      </div>
      {attention ? <p className="erp-sync-entity-row__attention">{attention}</p> : null}
    </div>
  );
}

/* ── Facility helper ── */

export function getErpFacilityDisplayLabel(facilityId: string): string {
  void facilityId;
  return "Tesis bilgisi";
}

export function getErpActorDisplayLabel(subjectId: string | null | undefined): string {
  if (!subjectId) {
    return "—";
  }
  if (subjectId.includes("-")) {
    return "Yetkili kullanıcı";
  }
  return subjectId;
}

export function getErpInternalReferenceLabel(
  internalId: string | null | undefined,
): string {
  if (!internalId) {
    return "—";
  }
  return "Eşleştirilmiş kayıt";
}

"use client";

import Link from "next/link";
import {
  useCallback,
  useTransition,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  ArrowUpRight,
  Gauge,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
} from "lucide-react";

import {
  buildOperationsCommandCenterFilterParams,
  clearOperationsCommandCenterFilterParams,
  readOperationsCommandCenterFilters,
} from "../application/operations-command-center-filter";
import {
  getOperationsCommandCenterAvailabilityLabel,
  getOperationsCommandCenterStatusParts,
  OPERATIONS_COMMAND_CENTER_COMMERCIAL_LINK_LABEL,
  OPERATIONS_COMMAND_CENTER_EMPTY_QUEUE_NOTE,
  OPERATIONS_COMMAND_CENTER_NO_ACCESS_NOTE,
  OPERATIONS_COMMAND_CENTER_READONLY_NOTE,
  OPERATIONS_COMMAND_CENTER_SUBTITLE,
} from "../domain/operations-command-center-labels";
import type {
  OperationsCommandCenterKpiCard,
  OperationsCommandCenterModuleHealth,
  OperationsCommandCenterQueueItem,
  OperationsCommandCenterScopeState,
  OperationsCommandCenterWorkspacePayload,
} from "../types/operations-command-center-types";
import { refreshOperationsCommandCenterAction } from "../server/operations-command-center-actions";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import "./operations-command-center.css";

type OperationsCommandCenterViewProps = Readonly<{
  workspace: OperationsCommandCenterWorkspacePayload;
}>;

const KPI_ICONS: Record<string, typeof Gauge> = {
  "occ-priority-review": ShieldAlert,
  "occ-open-linked-tasks": Gauge,
};

function scopeBadgeLabel(scopeState: OperationsCommandCenterScopeState | undefined): string | null {
  if (scopeState === "partial") return "Kısmi";
  if (scopeState === "insufficient") return "Erişim yok";
  return null;
}

function KpiCard({ card }: Readonly<{ card: OperationsCommandCenterKpiCard }>) {
  const Icon = KPI_ICONS[card.id] ?? Gauge;
  const scopeBadge = scopeBadgeLabel(card.scopeState);

  return (
    <article
      className={`commint-kpi-card commint-kpi-card__tone--${card.tone === "negative" ? "negative" : card.tone === "warning" ? "warning" : "neutral"}`}
      data-testid={`operations-command-center-kpi-${card.id}`}
    >
      <div className="commint-kpi-card__header">
        <p className="commint-kpi-card__label">{card.label}</p>
        <Icon aria-hidden="true" size={16} />
      </div>
      <p className={`commint-kpi-card__value${/^[\d%.,\-\s]+$/.test(card.value) ? "" : " occ-kpi-value--text"}`}>{card.value}</p>
      <div className="commint-kpi-card__footer">
        <p className="commint-kpi-card__detail">{card.hint}</p>
        {scopeBadge ? (
          <span className="commint-kpi-card__badge">{scopeBadge}</span>
        ) : null}
      </div>
    </article>
  );
}

function StatusPills({ statusLabel }: Readonly<{ statusLabel: string }>) {
  return (
    <span className="occ-status-pills">
      {getOperationsCommandCenterStatusParts(statusLabel).map((part) => (
        <span key={part.label} className={`occ-status-pill occ-status-pill--${part.tone}`}>
          {part.label}
        </span>
      ))}
    </span>
  );
}

function QueueRow({ item }: Readonly<{ item: OperationsCommandCenterQueueItem }>) {
  return (
    <tr data-testid={`operations-command-center-queue-row-${item.id}`}>
      <td>
        <span className="commint-table__secondary occ-queue-reference" data-testid="operations-command-center-module-badge">
          {item.moduleLabel}
          {item.sourceReference && !/^[0-9a-f]{8}$/i.test(item.sourceReference.trim()) ? ` · ${item.sourceReference}` : ""}
        </span>
      </td>
      <td>
        <Link href={item.deepLinkHref} className="commint-table__link">
          {item.title?.trim() || `${item.sourceReference || "Kaydı"} aç`}
          <ArrowUpRight aria-hidden="true" size={14} />
        </Link>
      </td>
      <td><StatusPills statusLabel={item.statusLabel} /></td>
      <td>{item.evidenceLabel}</td>
      <td>{item.freshnessLabel}</td>
      <td>
        {item.linkedTaskHref ? (
          <Link
            href={item.linkedTaskHref}
            className="commint-table__link"
            data-testid="operations-command-center-linked-task-link"
          >
            Görev
            <ArrowUpRight aria-hidden="true" size={14} />
          </Link>
        ) : (
          "—"
        )}
      </td>
    </tr>
  );
}

function ModuleHealthRow({ entry }: Readonly<{ entry: OperationsCommandCenterModuleHealth }>) {
  return (
    <tr data-testid={`operations-command-center-module-health-${entry.moduleCode}`}>
      <td>{entry.moduleLabel}</td>
      <td>{getOperationsCommandCenterAvailabilityLabel(entry.availability)}</td>
      <td>{entry.countLabel ?? "—"}</td>
      <td className="occ-hide-narrow">{entry.hint}</td>
    </tr>
  );
}

export function OperationsCommandCenterView({
  workspace,
}: OperationsCommandCenterViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isRefreshing, startRefresh] = useTransition();
  const [isPending, startTransition] = useTransition();

  const uiFilters = readOperationsCommandCenterFilters(searchParams);

  const applyFilterUpdates = useCallback((updates: Partial<Record<string, string>>) => {
    startTransition(() => {
      const params = buildOperationsCommandCenterFilterParams(searchParams, updates);
      router.replace(`${pathname}?${params.toString()}`);
    });
  }, [pathname, router, searchParams]);

  const clearFilters = useCallback(() => {
    startTransition(() => {
      const params = clearOperationsCommandCenterFilterParams(searchParams);
      router.replace(params.toString() ? `${pathname}?${params.toString()}` : pathname);
    });
  }, [pathname, router, searchParams]);

  const handleRefresh = useCallback(() => {
    startRefresh(async () => {
      await refreshOperationsCommandCenterAction();
      router.refresh();
    });
  }, [router]);

  const hasActiveFilters = Boolean(uiFilters.module || uiFilters.state);
  const factoryCaseEmptyNote = hasActiveFilters
    ? OPERATIONS_COMMAND_CENTER_EMPTY_QUEUE_NOTE
    : "Şu anda aktif fabrika vakası bulunmuyor.";
  const queueEmptyNote = hasActiveFilters
    ? OPERATIONS_COMMAND_CENTER_EMPTY_QUEUE_NOTE
    : "Şu anda öncelikli kayıt bulunmuyor.";

  return (
    <div className="commint-page occ-page" data-testid="operations-command-center-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>Operasyon Yönetim Merkezi</h1>
            <p className="commint-executive__subtitle">{OPERATIONS_COMMAND_CENTER_SUBTITLE}</p>
          </div>
          <div className="commint-executive__actions">
            <button
              type="button"
              className="commint-button commint-button--ghost"
              onClick={handleRefresh}
              disabled={isRefreshing}
              data-testid="operations-command-center-refresh-button"
            >
              {isRefreshing ? (
                <LoaderCircle aria-hidden="true" size={16} />
              ) : (
                <RefreshCw aria-hidden="true" size={16} />
              )}
              Veriyi yenile
            </button>
          </div>
        </div>
        <ul className="commint-executive__meta" data-testid="operations-command-center-scope-strip">
          {workspace.facilityLabel !== "Yetkili tesis" ? <li>Tesis: {workspace.facilityLabel}</li> : null}
          <li>Veri güveni: {workspace.dataConfidenceLabel}</li>
          {scopeBadgeLabel(workspace.scopeState) ? (
            <li>Kapsam: {scopeBadgeLabel(workspace.scopeState)}</li>
          ) : null}
        </ul>
      </header>

      <p className="commint-readonly-note" data-testid="operations-command-center-readonly-note">
        {OPERATIONS_COMMAND_CENTER_READONLY_NOTE}
        <span data-testid="operations-command-center-automation-disclaimer" hidden>
          {workspace.automationDisclaimer}
        </span>
      </p>

      {workspace.partialScopeNote ? (
        <p className="commint-partial-error" data-testid="operations-command-center-partial-note">
          {workspace.partialScopeNote}
        </p>
      ) : null}

      {workspace.noAccessibleSources ? (
        <div
          role="alert"
          className="commint-partial-error"
          data-testid="operations-command-center-no-access"
        >
          {OPERATIONS_COMMAND_CENTER_NO_ACCESS_NOTE}
        </div>
      ) : null}

      <div className="commint-filter-rail occ-filter-rail" data-testid="operations-command-center-filter-rail">
        <label className="commint-filter-field">
          <span>Dönem başlangıcı</span>
          <input
            type="date"
            value={uiFilters.windowFrom}
            onChange={(event) => applyFilterUpdates({ windowFrom: event.target.value })}
            data-testid="operations-command-center-window-from"
          />
        </label>
        <label className="commint-filter-field">
          <span>Dönem bitişi</span>
          <input
            type="date"
            value={uiFilters.windowTo}
            onChange={(event) => applyFilterUpdates({ windowTo: event.target.value })}
            data-testid="operations-command-center-window-to"
          />
        </label>
        <label className="commint-filter-field">
          <span>Modül</span>
          <select
            value={uiFilters.module}
            onChange={(event) => applyFilterUpdates({ module: event.target.value })}
            data-testid="operations-command-center-module-filter"
          >
            <option value="">Tümü</option>
            <option value="factory">Fabrika vakası</option>
            <option value="production-manager">Yapay zekâ üretim yöneticisi</option>
            <option value="production">Üretim</option>
            <option value="quality">Kalite</option>
            <option value="maintenance">Bakım</option>
            <option value="mold">Kalıp</option>
            <option value="material">Malzeme</option>
            <option value="procurement">Satın alma</option>
            <option value="commercial">Ticari zekâ</option>
            <option value="ai-task">Görev</option>
          </select>
        </label>
        <label className="commint-filter-field">
          <span>Durum</span>
          <select
            value={uiFilters.state}
            onChange={(event) => applyFilterUpdates({ state: event.target.value })}
            data-testid="operations-command-center-state-filter"
          >
            <option value="">Tümü</option>
            <option value="review">İnceleme gerektiren</option>
            <option value="linked-task">Bağlı görev</option>
            <option value="insufficient-data">Yetersiz veri</option>
          </select>
        </label>
        <button
          type="button"
          className="commint-button commint-button--ghost occ-filter-rail__clear"
          onClick={clearFilters}
          disabled={isPending}
          data-testid="operations-command-center-clear-filters"
        >
          Filtreleri temizle
        </button>
      </div>

      <section className="commint-panel" aria-labelledby="operations-command-center-kpi-heading">
        <header className="commint-panel__header">
          <h2 id="operations-command-center-kpi-heading" className="commint-panel__title">
            Operasyon göstergeleri
          </h2>
        </header>
        <div className="commint-kpi-grid" data-testid="operations-command-center-kpi-grid">
          {workspace.kpis.map((card) => (
            <KpiCard key={card.id} card={card} />
          ))}
        </div>
      </section>

      <section className="commint-panel" aria-labelledby="operations-command-center-factory-cases-heading">
        <header className="commint-panel__header">
          <h2 id="operations-command-center-factory-cases-heading" className="commint-panel__title">
            Aktif fabrika vakaları
          </h2>
        </header>
        {workspace.queueItems.filter((item) => item.moduleCode === "factory").length === 0 ? (
          <p className="commint-readonly-note occ-empty-cell" data-testid="operations-command-center-factory-case-empty">
            {factoryCaseEmptyNote}
          </p>
        ) : (
        <div className="commint-table-wrap occ-table-wrap" tabIndex={0} role="region" aria-label="Aktif fabrika vakaları (kaydırılabilir)">
        <table aria-label="Aktif fabrika vakaları" className="commint-table occ-table" data-testid="operations-command-center-factory-case-table">
          <thead>
            <tr>
              <th>Vaka</th>
              <th>Önem</th>
              <th>Engel</th>
              <th>Son Aktivite</th>
            </tr>
          </thead>
          <tbody>
            {workspace.queueItems
                .filter((item) => item.moduleCode === "factory")
                .map((item) => (
                  <tr key={item.id} data-testid={`operations-command-center-factory-row-${item.id}`}>
                    <td>
                      <Link href={item.deepLinkHref} className="commint-table__link">
                        {item.title?.trim() || `${item.sourceReference || "Kaydı"} aç`}
                      </Link>
                    </td>
                    <td><StatusPills statusLabel={item.statusLabel} /></td>
                    <td>{item.evidenceLabel}</td>
                    <td>{item.freshnessLabel}</td>
                  </tr>
                ))}
          </tbody>
        </table>
        </div>
        )}
      </section>

      <section className="commint-panel" aria-labelledby="operations-command-center-queue-heading">
        <header className="commint-panel__header">
          <h2 id="operations-command-center-queue-heading" className="commint-panel__title">
            Öncelik kuyruğu
          </h2>
        </header>
        {workspace.queueItems.length === 0 ? (
          <p className="commint-readonly-note" data-testid="operations-command-center-queue-empty">
            {queueEmptyNote}
          </p>
        ) : (
          <div className="commint-table-wrap occ-table-wrap" tabIndex={0} role="region" aria-label="Öncelik kuyruğu (kaydırılabilir)" data-testid="operations-command-center-queue-table">
            <table aria-label="Öncelik kuyruğu" className="commint-table occ-table">
              <thead>
                <tr>
                  <th>Modül / referans</th>
                  <th>Kayıt</th>
                  <th>Durum</th>
                  <th>Kanıt</th>
                  <th>Güncelleme</th>
                  <th>Bağlı görev</th>
                </tr>
              </thead>
              <tbody>
                {workspace.queueItems.map((item) => (
                  <QueueRow key={item.id} item={item} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <details className="occ-disclosure" data-testid="operations-command-center-module-health-disclosure">
        <summary>Modül kapsamı ve erişim</summary>
        <div className="commint-table-wrap occ-table-wrap" tabIndex={0} role="region" aria-label="Modül kapsamı ve erişim (kaydırılabilir)" data-testid="operations-command-center-module-health">
          <table aria-label="Modül kapsamı ve erişim" className="commint-table occ-table">
            <thead>
              <tr>
                <th>Modül</th>
                <th>Erişim</th>
                <th>Kayıt</th>
                <th className="occ-hide-narrow">Açıklama</th>
              </tr>
            </thead>
            <tbody>
              {workspace.moduleHealth.map((entry) => (
                <ModuleHealthRow key={entry.moduleCode} entry={entry} />
              ))}
            </tbody>
          </table>
        </div>
      </details>

      <section className="commint-panel occ-commercial-link" aria-label="Ticari kârlılık">
        <h2 className="commint-panel__title">Ticari kârlılık</h2>
        <Link
          href={workspace.commercialDeepLinkHref}
          className="commint-table__link"
          data-testid="operations-command-center-commercial-deep-link"
        >
          {OPERATIONS_COMMAND_CENTER_COMMERCIAL_LINK_LABEL}
          <ArrowUpRight aria-hidden="true" size={14} />
        </Link>
      </section>
    </div>
  );
}

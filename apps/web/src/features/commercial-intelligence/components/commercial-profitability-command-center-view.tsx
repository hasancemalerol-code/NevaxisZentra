"use client";

import Link from "next/link";
import {
  useCallback,
  useTransition,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  ArrowUpRight,
  ClipboardList,
  Gauge,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  Sparkles,
} from "lucide-react";

import {
  buildCommercialProfitabilityCommandCenterFilterParams,
  clearCommercialProfitabilityCommandCenterFilterParams,
  readCommercialProfitabilityCommandCenterFilters,
} from "../application/commercial-profitability-command-center-filter";
import {
  COMMERCIAL_PROFITABILITY_COMMAND_CENTER_EMPTY_QUEUE_NOTE,
  COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SCOPE_NOTE,
  COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SUBTITLE,
} from "../domain/commercial-profitability-command-center-labels";
import type {
  CommercialProfitabilityCommandCenterKpiCard,
  CommercialProfitabilityCommandCenterQueueItem,
  CommercialProfitabilityCommandCenterScopeState,
  CommercialProfitabilityCommandCenterWorkspacePayload,
} from "../types/commercial-profitability-command-center-types";
import { CommercialKpiCompact, isKpiSetEmpty } from "./commercial-kpi-compact";
import { resolveProfitabilityKpiToneClass } from "./commercial-profitability-presentation";
import { refreshCommercialProfitabilityCommandCenterAction } from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";
import { kpiValueClass } from "./commercial-no-data";

type CommercialProfitabilityCommandCenterViewProps = Readonly<{
  workspace: CommercialProfitabilityCommandCenterWorkspacePayload;
}>;

const KPI_ICONS: Record<string, typeof Gauge> = {
  "cc-alert-active": AlertTriangle,
  "cc-alert-high-critical": ShieldAlert,
  "cc-open-linked-tasks": ClipboardList,
  "cc-scenario-review": Sparkles,
  "cc-scenario-blocked": ShieldAlert,
  "cc-evidence-review": AlertTriangle,
  "cc-scope": Gauge,
};

function scopeBadgeLabel(scopeState: CommercialProfitabilityCommandCenterScopeState | undefined): string | null {
  if (scopeState === "partial") return "Kısmi";
  if (scopeState === "insufficient") return "Yetersiz veri";
  return null;
}

function CommandCenterKpiCard({ card }: Readonly<{ card: CommercialProfitabilityCommandCenterKpiCard }>) {
  const Icon = KPI_ICONS[card.id] ?? Gauge;
  const scopeBadge = scopeBadgeLabel(card.scopeState);

  return (
    <article
      className={`commint-kpi-card ${resolveProfitabilityKpiToneClass(card.tone)}`}
      data-testid={`commercial-profitability-command-center-kpi-${card.id}`}
    >
      <div className="commint-kpi-card__header">
        <p className="commint-kpi-card__label">{card.label}</p>
        <Icon aria-hidden="true" size={16} />
      </div>
      <p className={kpiValueClass(card.value)}>{card.value}</p>
      <div className="commint-kpi-card__footer">
        <p className="commint-kpi-card__detail">{card.hint}</p>
        {scopeBadge ? (
          <span className="commint-kpi-card__badge">{scopeBadge}</span>
        ) : null}
      </div>
    </article>
  );
}

function QueueRow({ item }: Readonly<{ item: CommercialProfitabilityCommandCenterQueueItem }>) {
  return (
    <tr
      data-testid={`commercial-profitability-command-center-queue-row-${item.id}`}
      className={item.humanDecisionRequired ? "commint-variance--negative" : undefined}
    >
      <td>
        <span className="commint-table__secondary">{item.sourceTypeLabel}</span>
        {item.humanDecisionRequired ? (
          <span className="commint-kpi-card__badge" data-testid="commercial-profitability-command-center-human-decision">
            İnsan kararı
          </span>
        ) : null}
      </td>
      <td>
        <Link href={item.deepLinkHref} className="commint-table__link">
          {item.title}
          <ArrowUpRight aria-hidden="true" size={14} />
        </Link>
      </td>
      <td>{item.severityOrStatusLabel}</td>
      <td>{item.evidenceStateLabel}</td>
      <td>{item.ownerActionLabel}</td>
      <td>{item.sortTimestampLabel}</td>
      <td>
        {item.linkedTaskHref ? (
          <Link
            href={item.linkedTaskHref}
            className="commint-table__link"
            data-testid="commercial-profitability-command-center-linked-task-link"
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

export function CommercialProfitabilityCommandCenterView({
  workspace,
}: CommercialProfitabilityCommandCenterViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isRefreshing, startRefresh] = useTransition();
  const [isPending, startTransition] = useTransition();

  const managementKpis = workspace.kpiCards.filter((card) => card.id !== "cc-scope");
  const uiFilters = readCommercialProfitabilityCommandCenterFilters(searchParams);

  const applyFilterUpdates = useCallback((updates: Partial<Record<string, string>>) => {
    startTransition(() => {
      const params = buildCommercialProfitabilityCommandCenterFilterParams(searchParams, updates);
      router.replace(`${pathname}?${params.toString()}`);
    });
  }, [pathname, router, searchParams]);

  const clearFilters = useCallback(() => {
    startTransition(() => {
      const params = clearCommercialProfitabilityCommandCenterFilterParams(searchParams);
      router.replace(params.toString() ? `${pathname}?${params.toString()}` : pathname);
    });
  }, [pathname, router, searchParams]);

  const handleRefresh = useCallback(() => {
    startRefresh(async () => {
      await refreshCommercialProfitabilityCommandCenterAction();
      router.refresh();
    });
  }, [router]);

  return (
    <div className="commint-page" data-testid="commercial-profitability-command-center-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Kârlılık Yönetim Merkezi</h1>
            <p className="commint-executive__subtitle">
              {COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SUBTITLE}
            </p>
          </div>
          <div className="commint-executive__actions">
            <button
              type="button"
              className="commint-button commint-button--ghost"
              onClick={handleRefresh}
              disabled={isRefreshing}
              data-testid="commercial-profitability-command-center-refresh-button"
            >
              {isRefreshing ? (
                <LoaderCircle aria-hidden="true" size={16} />
              ) : (
                <RefreshCw aria-hidden="true" size={16} />
              )}
              Yenile
            </button>
          </div>
        </div>
        <ul className="commint-executive__meta">
          <li>Tesis: {workspace.facilityLabel}</li>
          <li>Dönem: {workspace.timeWindowLabel}</li>
          <li>Son güncelleme: {workspace.dataConfidence.lastRefreshLabel}</li>
        </ul>
      </header>

      <details className="commint-notes">
        <summary>Kapsam ve sınırlar</summary>
        <p className="commint-readonly-note" data-testid="commercial-profitability-command-center-readonly-note">
          {COMMERCIAL_PROFITABILITY_COMMAND_CENTER_SCOPE_NOTE}
        </p>
      </details>

      <div
        className="commint-filter-rail"
        data-testid="commercial-profitability-command-center-filter-rail"
      >
        <label className="commint-filter-field">
          <span>Dönem başlangıcı</span>
          <input
            type="date"
            value={uiFilters.windowFrom}
            onChange={(event) => applyFilterUpdates({ windowFrom: event.target.value })}
            data-testid="commercial-profitability-command-center-window-from"
          />
        </label>
        <label className="commint-filter-field">
          <span>Dönem bitişi</span>
          <input
            type="date"
            value={uiFilters.windowTo}
            onChange={(event) => applyFilterUpdates({ windowTo: event.target.value })}
            data-testid="commercial-profitability-command-center-window-to"
          />
        </label>
        <button
          type="button"
          className="commint-button commint-button--ghost"
          onClick={clearFilters}
          disabled={isPending}
          data-testid="commercial-profitability-command-center-clear-filters"
        >
          Filtreleri temizle
        </button>
      </div>

      {isKpiSetEmpty(managementKpis) ? (
        <CommercialKpiCompact
          ariaLabel="Yönetim göstergeleri"
          testId="commercial-profitability-command-center-kpi-grid"
          items={managementKpis.map((card) => ({ id: card.id, label: card.label, value: card.value, caption: card.hint }))}
        />
      ) : (
        <section className="commint-panel" aria-labelledby="command-center-kpi-heading">
          <header className="commint-panel__header">
            <h2 id="command-center-kpi-heading" className="commint-panel__title">Yönetim göstergeleri</h2>
          </header>
          <div
            className="commint-kpi-grid"
            data-testid="commercial-profitability-command-center-kpi-grid"
          >
            {managementKpis.map((card) => (
              <CommandCenterKpiCard key={card.id} card={card} />
            ))}
          </div>
        </section>
      )}

      <section className="commint-panel" aria-labelledby="command-center-queue-heading">
        <header className="commint-panel__header">
          <h2 id="command-center-queue-heading" className="commint-panel__title">Öncelikli karar kuyruğu</h2>
        </header>
        {workspace.priorityQueue.length === 0 ? (
          <p
            className="commint-empty"
            data-testid="commercial-profitability-command-center-queue-empty"
          >
            {COMMERCIAL_PROFITABILITY_COMMAND_CENTER_EMPTY_QUEUE_NOTE}
          </p>
        ) : (
          <div className="commint-table-wrap" tabIndex={0} data-testid="commercial-profitability-command-center-queue-table">
            <table aria-label="Öncelikli karar kuyruğu" className="commint-table">
              <thead>
                <tr>
                  <th>Kaynak</th>
                  <th>Başlık</th>
                  <th>Önem / durum</th>
                  <th>Kanıt</th>
                  <th>Aksiyon</th>
                  <th>Güncelleme</th>
                  <th>Bağlı görev</th>
                </tr>
              </thead>
              <tbody>
                {workspace.priorityQueue.map((item) => (
                  <QueueRow key={item.id} item={item} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="commint-panel" aria-labelledby="command-center-lenses-heading">
        <header className="commint-panel__header">
          <h2 id="command-center-lenses-heading" className="commint-panel__title">Kârlılık görünümleri</h2>
        </header>
        <ul className="commint-lens-list" data-testid="commercial-profitability-command-center-lenses">
          {workspace.profitabilityLenses.map((lens) => (
            <li key={lens.id} className="commint-lens-list__row">
              <span className="commint-lens-list__text">
                <span className="commint-lens-list__label">{lens.label}</span>
                <span className="commint-lens-list__hint">{lens.hint}</span>
              </span>
              <span className="commint-lens-list__count">
                {lens.previewCount === null ? "—" : lens.previewCount}
                {scopeBadgeLabel(lens.scopeState) ? (
                  <span className="commint-kpi-card__badge">{scopeBadgeLabel(lens.scopeState)}</span>
                ) : null}
              </span>
              <Link
                href={lens.deepLinkHref}
                className="commint-table__link"
                data-testid={`commercial-profitability-command-center-lens-link-${lens.id}`}
              >
                Ayrıntıya git
                <ArrowUpRight aria-hidden="true" size={14} />
              </Link>
            </li>
          ))}
        </ul>
      </section>

      <div className="commint-panel-grid commint-panel-grid--overview">
        <section className="commint-panel" aria-labelledby="command-center-pricing-heading">
          <header className="commint-panel__header">
            <h2 id="command-center-pricing-heading" className="commint-panel__title">Fiyat ve onay özeti</h2>
          </header>
          <div data-testid="commercial-profitability-command-center-pricing-snapshot">
            <ul className="commint-summary-list">
              {workspace.pricingLifecycleSnapshots.map((snapshot) => (
                <li key={snapshot.label}>
                  <Link href={snapshot.deepLinkHref}>{snapshot.label}</Link>
                  <span className="commint-summary-list__count">
                    {snapshot.count === null ? "—" : snapshot.count}
                    {scopeBadgeLabel(snapshot.scopeState) ? ` (${scopeBadgeLabel(snapshot.scopeState)})` : ""}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </section>

        <section className="commint-panel" aria-labelledby="command-center-alert-heading">
          <header className="commint-panel__header">
            <h2 id="command-center-alert-heading" className="commint-panel__title">Uyarı ve görev özeti</h2>
          </header>
          <div data-testid="commercial-profitability-command-center-alert-snapshot">
            <ul className="commint-summary-list">
              {workspace.alertLifecycleSnapshots.map((snapshot) => (
                <li key={snapshot.label}>
                  <Link href={snapshot.deepLinkHref}>{snapshot.label}</Link>
                  <span className="commint-summary-list__count">
                    {snapshot.count === null ? "—" : snapshot.count}
                    {scopeBadgeLabel(snapshot.scopeState) ? ` (${scopeBadgeLabel(snapshot.scopeState)})` : ""}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </section>
      </div>

      <section className="commint-panel" aria-labelledby="command-center-confidence-heading">
        <header className="commint-panel__header">
          <h2 id="command-center-confidence-heading" className="commint-panel__title">Veri güveni</h2>
        </header>
        <ul className="commint-keyvalue" data-testid="commercial-profitability-command-center-data-confidence">
          <li>
            <span>Kapsam</span>
            <span>
              {workspace.dataConfidence.scopeLabel} · {workspace.dataConfidence.facilityLabel} ·{" "}
              {workspace.dataConfidence.timeWindowLabel}
            </span>
          </li>
          <li>
            <span>Son yenileme</span>
            <span>{workspace.dataConfidence.lastRefreshLabel}</span>
          </li>
          {workspace.dataConfidence.partialAggregationNotes.map((note) => (
            <li key={note}>
              <span>Kısmi toplama</span>
              <span>{note}</span>
            </li>
          ))}
          {workspace.dataConfidence.evidenceInsufficiencyNotes.length > 0 ? (
            <li>
              <span>Kanıt yetersizliği</span>
              <span>{workspace.dataConfidence.evidenceInsufficiencyNotes.join("; ")}</span>
            </li>
          ) : null}
          {workspace.dataConfidence.sourceConflictNotes.length > 0 ? (
            <li>
              <span>Kaynak çakışması</span>
              <span>{workspace.dataConfidence.sourceConflictNotes.join("; ")}</span>
            </li>
          ) : null}
          {!workspace.canReadAiTasks ? (
            <li data-testid="commercial-profitability-command-center-ai-task-partial">
              <span>Görev ayrıntıları</span>
              <span>Kısmi kapsam; tam sayım için görev okuma yetkisi gerekir.</span>
            </li>
          ) : null}
          <li data-testid="commercial-profitability-command-center-provenance">
            <span>Kaynak</span>
            <span>{workspace.provenanceDisclaimer}</span>
          </li>
        </ul>
      </section>
    </div>
  );
}

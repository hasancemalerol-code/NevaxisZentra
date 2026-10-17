"use client";

import { Notice } from "@/components/ui/notice";
import Link from "next/link";
import { useCallback, useEffect, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  ArrowUpRight,
  CheckCircle2,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  Users,
} from "lucide-react";

import { getExecutiveEmptyStateMessage } from "../application/executive-command-center-builder";
import {
  buildExecutiveCommandCenterFilterParams,
  clearExecutiveCommandCenterFilterParams,
  hasActiveExecutiveCommandCenterFilters,
  readExecutiveCommandCenterFilters,
} from "../application/executive-command-center-filter";
import {
  EXECUTIVE_COMMAND_CENTER_PARTIAL_NOTE,
  EXECUTIVE_COMMAND_CENTER_SUBTITLE,
  EXECUTIVE_COMMAND_CENTER_TITLE,
  EXECUTIVE_COMMAND_CENTER_EMPTY_THROUGHPUT,
  EXECUTIVE_COMMAND_CENTER_FILTER_SEVERITY_OPTIONS,
  EXECUTIVE_COMMAND_CENTER_FILTER_TIME_SCOPE_OPTIONS,
  EXECUTIVE_PRIORITY_EMPTY,
  EXECUTIVE_PRIORITY_SECTION_LABELS,
} from "../domain/executive-command-center-labels";
import { refreshExecutiveCommandCenterAction } from "../server/executive-command-center-actions";
import { TASK_CENTER_DRILL_DOWN_LABEL } from "@/features/ai-task-center/domain/ai-task-product-labels";
import type {
  ExecutiveCommandCenterWorkspacePayload,
  ExecutiveOutcomeCard,
  ExecutivePriorityItem,
} from "../types/executive-command-center-types";
import { getFactoryDecisionSeverityLabel, getFactoryDecisionStatusLabel } from "@/features/factory-decision-center/domain/factory-decision-center-labels";
import { enumLabel } from "@/lib/presentation/enum-labels";
import { PresentationModeReturnLink } from "@/features/presentation-mode/components/presentation-mode-return-link";

import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import "./executive-command-center.css";
import { customerMessage } from "@/lib/presentation/customer-message";

/** Known decision-center states first, then the shared enum labels; never shows a raw machine value. */
function stateText(value: string | null | undefined): string {
  const text = (value ?? "").trim();
  if (!text) return "—";
  const decisionLabel = getFactoryDecisionStatusLabel(text);
  return decisionLabel !== text ? decisionLabel : enumLabel(text);
}

const VERIFICATION_STATE_LABELS: Readonly<Record<string, string>> = {
  passed: "Doğrulandı",
  pending: "Bekliyor",
  "not-started": "Başlamadı",
  not_started: "Başlamadı",
  failed: "Doğrulanamadı",
};

function verificationText(value: string | null | undefined): string {
  const key = (value ?? "").trim().toLowerCase();
  return VERIFICATION_STATE_LABELS[key] ?? stateText(value);
}

type ExecutiveCommandCenterViewProps = Readonly<{
  workspace: ExecutiveCommandCenterWorkspacePayload;
}>;

function OutcomeCard({ card }: Readonly<{ card: ExecutiveOutcomeCard }>) {
  return (
    <article
      className={`commint-kpi-card commint-kpi-card__tone--${card.tone}`}
      data-testid={`executive-outcome-${card.id}`}
    >
      <p className="commint-kpi-card__label">{card.label}</p>
      <p className="commint-kpi-card__value">{card.value}</p>
      <p className="commint-kpi-card__detail">{card.detail}</p>
      <Link href={card.drillDownHref} className="commint-table__link executive-drill-link">
        {card.drillDownLabel}
        <ArrowUpRight aria-hidden="true" size={14} />
      </Link>
    </article>
  );
}

function PriorityItem({ item }: Readonly<{ item: ExecutivePriorityItem }>) {
  return (
    <article className="executive-stack-item" data-testid={`executive-priority-${item.id}`}>
      <div className="commint-panel__header executive-stack-item__header">
        <div>
          <p className="commint-executive__eyebrow">{EXECUTIVE_PRIORITY_SECTION_LABELS[item.section]}</p>
          <h3 className="executive-stack-item__title">{item.outcome}</h3>
        </div>
        <span className="commint-kpi-card__badge">{stateText(item.status)}</span>
      </div>
      <p className="commint-kpi-card__detail">{item.impact}</p>
      <div className="executive-stack-item__meta">
        <span>Sorumlu: {item.owner}</span>
        <span>Sonraki adım: {item.nextAction}</span>
        {item.timeToImpact ? <span>Etki: {item.timeToImpact}</span> : null}
        <span>Doğrulama: {verificationText(item.verificationState)}</span>
      </div>
      <Link href={item.drillDownHref} className="commint-table__link executive-drill-link">
        Detay
        <ArrowUpRight aria-hidden="true" size={14} />
      </Link>
    </article>
  );
}

export function ExecutiveCommandCenterView({ workspace: initialWorkspace }: ExecutiveCommandCenterViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [message, setMessage] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const uiFilters = readExecutiveCommandCenterFilters(searchParams);
  const filtersActive = hasActiveExecutiveCommandCenterFilters(uiFilters);

  useEffect(() => {
    setWorkspace(initialWorkspace);
  }, [initialWorkspace]);

  const applyFilterUpdates = useCallback((updates: Partial<Record<"severity" | "timeScope", string>>) => {
    startTransition(() => {
      const params = buildExecutiveCommandCenterFilterParams(searchParams, updates);
      router.replace(params.toString() ? `${pathname}?${params.toString()}` : pathname);
    });
  }, [pathname, router, searchParams]);

  const clearFilters = useCallback(() => {
    startTransition(() => {
      const params = clearExecutiveCommandCenterFilterParams(searchParams);
      router.replace(params.toString() ? `${pathname}?${params.toString()}` : pathname);
    });
  }, [pathname, router, searchParams]);

  function handleRefresh() {
    setMessage(null);
    startTransition(async () => {
      const currentFilters = Object.fromEntries(searchParams.entries());
      const result = await refreshExecutiveCommandCenterAction(currentFilters);
      if (result.status === "failure") {
        setMessage(result.message);
        return;
      }
      setWorkspace(result.workspace);
      router.refresh();
    });
  }

  const priorityEmpty = workspace.priorityStack.length === 0;
  const risksEmptyFlag = workspace.predictiveRisks.length === 0;
  const approvalsEmptyFlag = workspace.approvals.length === 0;
  const verifiedEmptyFlag = workspace.verifiedValue.length === 0;
  const timelineEmpty = workspace.timeline.length === 0;

  /** Empty areas collapse into one calm summary instead of one dashed card each. */
  const emptyNotes: ReadonlyArray<Readonly<{ label: string; text: string }>> = [
    priorityEmpty ? { label: "Öncelik yığını", text: EXECUTIVE_PRIORITY_EMPTY } : null,
    risksEmptyFlag ? { label: "Yaklaşan riskler", text: "Yaklaşan risk öngörülmüyor." } : null,
    approvalsEmptyFlag
      ? { label: "İnsan Onayı", text: getExecutiveEmptyStateMessage(workspace, "approvals") }
      : null,
    verifiedEmptyFlag
      ? { label: "Doğrulanmış sonuçlar", text: getExecutiveEmptyStateMessage(workspace, "verified") }
      : null,
    timelineEmpty ? { label: "Zaman çizelgesi", text: "Henüz zaman çizelgesi kaydı yok." } : null,
  ].filter((note): note is { label: string; text: string } => note !== null);

  const sections = [
    "critical-now",
    "decision-today",
    "active-recovery",
    "pending-verification",
    "verified-today",
  ] as const;

  return (
    <div className="commint-page" data-testid="executive-command-center-view">
      <PresentationModeReturnLink />
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">Yönetici komuta katmanı</p>
          <h1>{EXECUTIVE_COMMAND_CENTER_TITLE}</h1>
          <p className="commint-executive__subtitle">{EXECUTIVE_COMMAND_CENTER_SUBTITLE}</p>
        </div>
        <div className="commint-executive__actions">
          <button
            type="button"
            className="commint-button commint-button--ghost"
            disabled={isPending}
            onClick={handleRefresh}
            data-testid="executive-command-center-refresh"
          >
            {isPending ? <LoaderCircle aria-hidden="true" size={16} className="commint-spin" /> : <RefreshCw aria-hidden="true" size={16} />}
            Yenile
          </button>
        </div>
      </header>

      <div
        className={`commint-filter-rail executive-filter-rail${filtersActive ? " executive-filter-rail--active" : ""}`}
        data-testid="executive-command-center-filter-rail"
      >
        {filtersActive ? (
          <p className="executive-filter-rail__active-note" data-testid="executive-command-center-filter-active">
            Filtre aktif
          </p>
        ) : null}
        <label className="commint-filter-rail__field">
          <span className="commint-filter-rail__label">Önem</span>
          <select
            value={uiFilters.severity ?? ""}
            onChange={(event) => applyFilterUpdates({ severity: event.target.value })}
            data-testid="executive-command-center-severity-filter"
          >
            {EXECUTIVE_COMMAND_CENTER_FILTER_SEVERITY_OPTIONS.map((option) => (
              <option key={option.value || "all"} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label className="commint-filter-rail__field">
          <span className="commint-filter-rail__label">Zaman</span>
          <select
            value={uiFilters.timeScope}
            onChange={(event) => applyFilterUpdates({ timeScope: event.target.value })}
            data-testid="executive-command-center-time-scope-filter"
          >
            {EXECUTIVE_COMMAND_CENTER_FILTER_TIME_SCOPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <button
          type="button"
          className="commint-button commint-button--ghost commint-filter-rail__clear"
          onClick={clearFilters}
          disabled={isPending || !filtersActive}
          data-testid="executive-command-center-clear-filters"
        >
          Filtreleri temizle
        </button>
      </div>

      {message ? <Notice tone="danger" alert>{customerMessage(message)}</Notice> : null}
      {workspace.scopeState === "partial" ? (
        <Notice tone="info" data-testid="executive-partial-note">{EXECUTIVE_COMMAND_CENTER_PARTIAL_NOTE}</Notice>
      ) : null}

      {workspace.unavailableSources.includes("factory-decision-center") ? (
        <Notice tone="warning" data-testid="executive-decision-unavailable">Karar merkezi özeti şu anda alınamıyor; diğer bölümler kullanılabilir. <Link href="/fabrika-karar-merkezi" className="commint-table__link">Karar Merkezi’ni aç</Link></Notice>
      ) : (
        <section className="executive-narrative" data-testid="executive-narrative">
          <CheckCircle2 aria-hidden="true" size={18} />
          <p>{workspace.narrative}</p>
        </section>
      )}

      <section className="executive-section">
        <h2 className="executive-section-title">Sonuç Özeti</h2>
        <div className="commint-kpi-grid executive-outcome-grid" data-testid="executive-outcome-cards">
          {workspace.outcomeCards.map((card) => <OutcomeCard key={card.id} card={card} />)}
        </div>
      </section>

      <section className="commint-panel executive-section" data-testid="executive-throughput-optimizer">
        <div className="commint-panel__header">
          <div>
            <p className="commint-executive__eyebrow">Hat verimliliği</p>
            <h3>Verim optimizasyon durumu</h3>
          </div>
        </div>
        <div className="executive-throughput-strip">
          <div><p className="commint-kpi-card__label">Aktif vaka</p><p className="commint-kpi-card__value">{workspace.throughputOptimizer.activeCaseCount}</p></div>
          <div><p className="commint-kpi-card__label">Onay bekliyor</p><p className="commint-kpi-card__value">{workspace.throughputOptimizer.pendingApprovalCount}</p></div>
          <div><p className="commint-kpi-card__label">Bugün doğrulanan</p><p className="commint-kpi-card__value">{workspace.throughputOptimizer.verifiedImprovementTodayCount}</p></div>
        </div>
        <div className="executive-stack executive-stack--cards">
          {workspace.throughputOptimizer.activeCases.length === 0 &&
          workspace.throughputOptimizer.verifiedToday.length === 0 ? (
            <p className="executive-empty">{EXECUTIVE_COMMAND_CENTER_EMPTY_THROUGHPUT}</p>
          ) : (
            [...workspace.throughputOptimizer.activeCases, ...workspace.throughputOptimizer.verifiedToday].slice(0, 4).map((item) => (
              <article key={item.id} className="executive-stack-item" data-testid={`executive-throughput-${item.id}`}>
                <p className="commint-executive__eyebrow">{item.lineCode} · {stateText(item.currentPhase)}</p>
                <p className="commint-kpi-card__detail">Kısıt: {item.constraintLabel}</p>
                {item.selectedIntervention ? (
                  <p className="commint-kpi-card__detail">Müdahale: {item.selectedIntervention}</p>
                ) : null}
                <div className="executive-stack-item__meta">
                  <span>Gözlem: {stateText(item.observationState)}</span>
                  <span>Onay: {item.approvalState}</span>
                  {item.verifiedGainPerHour ? (
                    <span>Doğrulanan: +{item.verifiedGainPerHour.toFixed(0)} iyi/sa</span>
                  ) : null}
                </div>
                <Link href={item.drillDownHref} className="commint-table__link">
                  Hat detayı
                  <ArrowUpRight aria-hidden="true" size={14} />
                </Link>
              </article>
            ))
          )}
        </div>
        <Link href={workspace.throughputOptimizer.drillDownHref} className="commint-table__link executive-drill-link executive-panel-link">
          Üretim hatları
          <ArrowUpRight aria-hidden="true" size={14} />
        </Link>
      </section>

      <section className="executive-board executive-section" data-testid="executive-board">
        {priorityEmpty ? null : (
          <div className="commint-panel executive-panel">
            <div className="commint-panel__header">
              <div>
                <p className="commint-executive__eyebrow">Öncelik yığını</p>
                <h3>Şimdi ilgilenilecekler</h3>
              </div>
            </div>
            <div className="executive-stack" data-testid="executive-priority-stack">
              {sections.flatMap((section) =>
                workspace.priorityStack
                  .filter((item) => item.section === section)
                  .slice(0, 3)
                  .map((item) => <PriorityItem key={item.id} item={item} />),
              )}
            </div>
          </div>
        )}

        {risksEmptyFlag ? null : (
          <div className="commint-panel executive-panel">
            <div className="commint-panel__header">
              <div>
                <p className="commint-executive__eyebrow">Öngörülen kritik riskler</p>
                <h3>Yaklaşan riskler</h3>
              </div>
            </div>
            <div className="executive-stack" data-testid="executive-predictive-risks">
              {workspace.predictiveRisks.map((risk) => (
                <article key={risk.id} className="executive-stack-item" data-testid={`executive-risk-${risk.id}`}>
                  <p className="commint-executive__eyebrow">{risk.machineCode}{risk.orderReference ? ` · ${risk.orderReference}` : ""}</p>
                  <p className="commint-kpi-card__detail">{risk.observedSummary}</p>
                  <p><strong>Öngörü:</strong> {risk.predictedSummary}</p>
                  <div className="executive-stack-item__meta">
                    <span>Ufuk: {risk.horizon}</span>
                    <span>Güven: {risk.confidenceBand}</span>
                  </div>
                  <p className="commint-kpi-card__detail">{risk.recommendedAction}</p>
                  <Link href={risk.drillDownHref} className="commint-table__link">
                    Risk detayı
                    <ArrowUpRight aria-hidden="true" size={14} />
                  </Link>
                </article>
              ))}
            </div>
          </div>
        )}

        {approvalsEmptyFlag ? null : (
          <div className="commint-panel executive-panel">
            <div className="commint-panel__header">
              <div>
                <p className="commint-executive__eyebrow">İnsan Onayı</p>
                <h3>Bekleyen insan kararları</h3>
              </div>
            </div>
            <div className="executive-stack" data-testid="executive-approvals">
              {workspace.approvals.slice(0, 5).map((approval) => (
                <article key={approval.id} className="executive-stack-item">
                  <p className="commint-executive__eyebrow">{getFactoryDecisionSeverityLabel(approval.urgency)}</p>
                  <h3 className="executive-stack-item__title">{approval.decision}</h3>
                  <p className="commint-kpi-card__detail">{approval.businessOutcome}</p>
                  <div className="executive-stack-item__meta">
                    <span>Beklenen: {approval.expectedBenefit}</span>
                    <span>Bedel: {approval.tradeoff}</span>
                    <span>Talep eden: {approval.requester}</span>
                  </div>
                  {approval.recoveryContext ? (
                    <p className="commint-kpi-card__detail">Bağlam: {approval.recoveryContext}</p>
                  ) : null}
                  <Link href={approval.drillDownHref} className="commint-table__link">
                    Kararı aç
                    <ArrowUpRight aria-hidden="true" size={14} />
                  </Link>
                </article>
              ))}
            </div>
          </div>
        )}

        {workspace.digitalWorkforce.available ? (
          <div className="commint-panel executive-panel">
            <div className="commint-panel__header">
              <div>
                <p className="commint-executive__eyebrow">Dijital iş gücü</p>
                <h3>Operasyonel durum</h3>
              </div>
              <Users aria-hidden="true" size={18} />
            </div>
            <div className="executive-throughput-strip executive-throughput-strip--six" data-testid="executive-digital-workforce">
              <div><p className="commint-kpi-card__label">Aktif çalışan</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.activeWorkers}</p></div>
              <div><p className="commint-kpi-card__label">Aktif vaka</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.activeCases}</p></div>
              <div><p className="commint-kpi-card__label">Onay bekliyor</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.waitingApproval}</p></div>
              <div><p className="commint-kpi-card__label">Bloklu</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.blocked}</p></div>
              <div><p className="commint-kpi-card__label">Başarısız</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.failed}</p></div>
              <div><p className="commint-kpi-card__label">Doğrulanan</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.verifiedOutcomes}</p></div>
            </div>
            <Link href={workspace.digitalWorkforce.drillDownHref} className="commint-table__link executive-drill-link executive-panel-link">
              Dijital İş Gücü
              <ArrowUpRight aria-hidden="true" size={14} />
            </Link>
          </div>
        ) : null}

        {verifiedEmptyFlag ? null : (
          <div className="commint-panel executive-panel">
            <div className="commint-panel__header">
              <div>
                <p className="commint-executive__eyebrow">Doğrulanmış değer</p>
                <h3>Doğrulanmış sonuçlar</h3>
              </div>
              <CheckCircle2 aria-hidden="true" size={18} />
            </div>
            <div className="executive-stack executive-stack--cards" data-testid="executive-verified-value">
              {workspace.verifiedValue.map((item) => (
                <article key={item.id} className="executive-stack-item">
                  <p className="commint-executive__eyebrow">{item.outcomeLabel}</p>
                  <h3 className="executive-stack-item__title">{item.orderReference}</h3>
                  <p className="commint-kpi-card__detail">{item.summary}</p>
                  <p className="commint-kpi-card__detail">{item.verifiedAtLabel}</p>
                  <Link href={item.drillDownHref} className="commint-table__link">
                    Vaka detayı
                    <ArrowUpRight aria-hidden="true" size={14} />
                  </Link>
                </article>
              ))}
            </div>
          </div>
        )}

        {timelineEmpty ? null : (
          <div className="commint-panel executive-panel">
            <div className="commint-panel__header">
              <div>
                <p className="commint-executive__eyebrow">Operasyonel zaman çizelgesi</p>
                <h3>Bugünkü akış</h3>
              </div>
            </div>
            <div className="executive-timeline" data-testid="executive-timeline">
              {workspace.timeline.map((entry) => (
                <div key={entry.id} className="executive-timeline__row">
                  <span className="executive-timeline__time">{entry.timeLabel}</span>
                  <div>
                    <strong>{entry.label}</strong>
                    <p className="commint-kpi-card__detail">{entry.description}</p>
                    {entry.drillDownHref ? (
                      <Link href={entry.drillDownHref} className="commint-table__link">
                        Aç
                        <ArrowUpRight aria-hidden="true" size={14} />
                      </Link>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </section>

      <section className="commint-panel executive-section" data-testid="executive-exposure-panel">
        <div className="commint-panel__header">
          <div>
            <p className="commint-executive__eyebrow">Operasyonel etki</p>
            <h3>Kayıp / fırsat görünümü</h3>
          </div>
          <ShieldAlert aria-hidden="true" size={18} />
        </div>
        <div className="executive-stack executive-exposure-grid" data-testid="executive-operational-exposure">
          {workspace.operationalExposure.map((item) => (
            <article key={item.id} className="executive-stack-item">
              <p className="commint-kpi-card__label">{item.label}</p>
              <p className="commint-kpi-card__value executive-exposure-value">{item.value}</p>
              <p className="commint-kpi-card__detail">{item.detail}</p>
              <Link href={item.drillDownHref} className="commint-table__link executive-drill-link">
                İncele
                <ArrowUpRight aria-hidden="true" size={14} />
              </Link>
            </article>
          ))}
        </div>
      </section>

      {emptyNotes.length > 0 ? (
        <section className="commint-panel executive-section executive-quiet" data-testid="executive-quiet-summary">
          <div className="commint-panel__header">
            <div>
              <p className="commint-executive__eyebrow">Sakin bölümler</p>
              <h3>Şu anda kayıt bulunmayan alanlar</h3>
            </div>
            <CheckCircle2 aria-hidden="true" size={18} />
          </div>
          <ul className="executive-quiet__list">
            {emptyNotes.map((note) => (
              <li key={note.label}>
                <strong>{note.label}</strong>
                <span>{note.text}</span>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <footer className="commint-table__secondary executive-section executive-footer-links">
        <p>
          <span className="executive-footer-links__item">
            Karar yönetimi:{" "}
            <Link href="/fabrika-karar-merkezi">
              Fabrika Karar Merkezi
              <ArrowUpRight aria-hidden="true" size={14} />
            </Link>
          </span>
          <span className="executive-footer-links__item">
            Görevler:{" "}
            <Link href="/">
              {TASK_CENTER_DRILL_DOWN_LABEL}
              <ArrowUpRight aria-hidden="true" size={14} />
            </Link>
          </span>
          <span className="executive-footer-links__item">
            Operasyon:{" "}
            <Link href="/operasyon/yonetim-merkezi">
              Operasyon Yönetim Merkezi
              <ArrowUpRight aria-hidden="true" size={14} />
            </Link>
          </span>
        </p>
      </footer>
    </div>
  );
}

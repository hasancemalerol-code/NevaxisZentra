"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  useTransition,
  type KeyboardEvent as ReactKeyboardEvent,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  BarChart3,
  LoaderCircle,
  RefreshCw,
  Scale,
  ShieldAlert,
  TrendingDown,
  TrendingUp,
} from "lucide-react";

import {
  buildProfitabilityComparisonSecondaryLabel,
  filterProfitabilityCostMixComparisons,
  filterProfitabilityPositionComparisons,
  filterProfitabilityTemporalComparisons,
  resolveProfitabilityKpiToneClass,
} from "./commercial-profitability-presentation";
import {
  CommercialProfitabilityComparisonDetailPanel,
  CommercialProfitabilityComparisonFilterRail,
  buildCommercialProfitabilityFilterParams,
  clearCommercialProfitabilityFilterParams,
  inferProfitabilityUnavailableReason,
  readCommercialProfitabilityComparisonFilters,
  toCommercialProfitabilityApiFilters,
  formatProfitabilityMarginDeltaDisplay,
  formatProfitabilityVarianceTone,
} from "./commercial-profitability-workspace-sections";
import {
  COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER,
  COMMERCIAL_PROFITABILITY_EVIDENCE_SCOPE,
  COMMERCIAL_PROFITABILITY_READONLY_NOTE,
  COMMERCIAL_PROFITABILITY_SUBTITLE,
  COMMERCIAL_PROFITABILITY_TAB_OPTIONS,
  REAL_PROFITABILITY_MAPPING_NOTE,
} from "../domain/commercial-profitability-labels";
import type {
  CommercialProfitabilityComparisonDetail,
  CommercialProfitabilityComparisonSummary,
  CommercialProfitabilityTabId,
  CommercialProfitabilityWorkspacePayload,
} from "../types/commercial-profitability-types";
import {
  loadCommercialProfitabilityComparisonDetailAction,
  refreshCommercialProfitabilityWorkspaceAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";
import { InlineUnavailable } from "@/components/ui/state-page";
import { kpiValueClass } from "./commercial-no-data";

type CommercialProfitabilityWorkspaceViewProps = Readonly<{
  workspace: CommercialProfitabilityWorkspacePayload;
  canManage: boolean;
  initialComparisonDetail?: CommercialProfitabilityComparisonDetail | null;
  initialDetailStatus?: "idle" | "ready" | "error";
}>;

const VALID_TAB_IDS = new Set(
  COMMERCIAL_PROFITABILITY_TAB_OPTIONS.map((tab) => tab.id),
);

const KPI_ICONS: Record<string, typeof BarChart3> = {
  "comparable-assessments": BarChart3,
  "deterioration-runs": TrendingDown,
  "improvement-runs": TrendingUp,
  "insufficient-or-conflict": ShieldAlert,
};

function ComparisonSummaryCard({
  comparison,
  selected,
  onSelect,
}: Readonly<{
  comparison: CommercialProfitabilityComparisonSummary;
  selected: boolean;
  onSelect: () => void;
}>) {
  const tone = formatProfitabilityVarianceTone(comparison.comparisonOutcome);
  return (
    <button
      type="button"
      className={`commint-comparison-card ${selected ? "commint-comparison-card--selected" : ""} commint-comparison-card--${tone}`}
      onClick={onSelect}
      aria-pressed={selected}
    >
      <p className="commint-comparison-card__title">{comparison.comparisonReference}</p>
      <p className="commint-comparison-card__meta">{buildProfitabilityComparisonSecondaryLabel(comparison)}</p>
      <p className="commint-comparison-card__value">{comparison.outcomeLabel}</p>
      <p className="commint-comparison-card__detail">
        Marj farkı: {formatProfitabilityMarginDeltaDisplay(comparison.marginDeltaLabel)}
      </p>
    </button>
  );
}

export function CommercialProfitabilityWorkspaceView({
  workspace: initialWorkspace,
  canManage: _canManage,
  initialComparisonDetail = null,
  initialDetailStatus = "idle",
}: CommercialProfitabilityWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");
  const [comparisonDetail, setComparisonDetail] = useState<CommercialProfitabilityComparisonDetail | null>(initialComparisonDetail);
  const [detailStatus, setDetailStatus] = useState<"idle" | "loading" | "ready" | "error">(
    initialDetailStatus === "ready" && initialComparisonDetail ? "ready" : initialDetailStatus,
  );

  const filters = useMemo(
    () => readCommercialProfitabilityComparisonFilters(searchParams),
    [searchParams],
  );

  const activeTab = useMemo<CommercialProfitabilityTabId>(() => {
    const tab = searchParams.get("tab");
    return tab && VALID_TAB_IDS.has(tab as CommercialProfitabilityTabId)
      ? (tab as CommercialProfitabilityTabId)
      : "overview";
  }, [searchParams]);

  const selectedComparisonId = searchParams.get("comparison")
    ?? workspace.selectedTemporalComparisonId
    ?? workspace.selectedCostMixComparisonId
    ?? workspace.selectedPositionComparisonId;

  const temporalComparisons = useMemo(
    () => filterProfitabilityTemporalComparisons(workspace.comparisons),
    [workspace.comparisons],
  );
  const costMixComparisons = useMemo(
    () => filterProfitabilityCostMixComparisons(workspace.comparisons),
    [workspace.comparisons],
  );
  const positionComparisons = useMemo(
    () => filterProfitabilityPositionComparisons(workspace.comparisons),
    [workspace.comparisons],
  );

  const activeComparisons = activeTab === "temporal"
    ? temporalComparisons
    : activeTab === "cost-mix"
      ? costMixComparisons
      : activeTab === "positions"
        ? positionComparisons
        : workspace.comparisons;

  const selectedComparison = activeComparisons.find((item) => item.id === selectedComparisonId)
    ?? activeComparisons[0]
    ?? null;

  const refreshWorkspace = useCallback((
    comparisonId?: string | null,
    nextFilters = filters,
  ) => {
    startTransition(async () => {
      const result = await refreshCommercialProfitabilityWorkspaceAction(
        comparisonId,
        toCommercialProfitabilityApiFilters(nextFilters),
      );
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setWorkspace(result.workspace);
      setFeedback("Çalışma alanı yenilendi.");
      setFeedbackTone("success");
    });
  }, [filters]);

  const updateFilters = useCallback((
    updates: Parameters<typeof buildCommercialProfitabilityFilterParams>[1],
  ) => {
    const params = buildCommercialProfitabilityFilterParams(searchParams, updates);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    const nextFilters = readCommercialProfitabilityComparisonFilters(params);
    refreshWorkspace(selectedComparisonId, nextFilters);
  }, [pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const clearFilters = useCallback(() => {
    const params = clearCommercialProfitabilityFilterParams(searchParams);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    refreshWorkspace(selectedComparisonId, readCommercialProfitabilityComparisonFilters(params));
  }, [pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const setActiveTab = useCallback((tabId: CommercialProfitabilityTabId) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", tabId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const loadComparisonDetail = useCallback((comparisonId: string) => {
    setComparisonDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialProfitabilityComparisonDetailAction(comparisonId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setComparisonDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const setSelectedComparison = useCallback((comparisonId: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("comparison", comparisonId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    loadComparisonDetail(comparisonId);
    startTransition(async () => {
      const result = await refreshCommercialProfitabilityWorkspaceAction(
        comparisonId,
        toCommercialProfitabilityApiFilters(filters),
      );
      if (result.status === "success") setWorkspace(result.workspace);
    });
  }, [filters, loadComparisonDetail, pathname, router, searchParams]);

  useEffect(() => {
    if (!selectedComparison) {
      setComparisonDetail(null);
      setDetailStatus("idle");
      return;
    }
    if (comparisonDetail?.summary.id === selectedComparison.id && detailStatus === "ready") return;
    loadComparisonDetail(selectedComparison.id);
  }, [comparisonDetail?.summary.id, detailStatus, loadComparisonDetail, selectedComparison]);

  const onTabKeyDown = useCallback((event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const nextIndex = event.key === "ArrowRight"
      ? (index + 1) % COMMERCIAL_PROFITABILITY_TAB_OPTIONS.length
      : (index - 1 + COMMERCIAL_PROFITABILITY_TAB_OPTIONS.length) % COMMERCIAL_PROFITABILITY_TAB_OPTIONS.length;
    setActiveTab(COMMERCIAL_PROFITABILITY_TAB_OPTIONS[nextIndex].id);
  }, [setActiveTab]);

  const unavailableReason = inferProfitabilityUnavailableReason(selectedComparison);
  const isEmpty = workspace.comparisons.length === 0;

  return (
    <div className="commint-page" data-testid="commercial-profitability-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Ürün Kârlılığı</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_PROFITABILITY_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>Kapsam: {workspace.facilityLabel}</li>
              <li>Karşılaştırma penceresi: {workspace.reportingPeriodLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Kanıt: {workspace.evidenceLabel}</li>
            </ul>
          </div>
          <div className="commint-executive__actions">
            <button
              type="button"
              className="commint-button"
              onClick={() => refreshWorkspace(selectedComparisonId)}
              disabled={isPending}
              aria-label="Çalışma alanını yenile"
            >
              {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <RefreshCw size={14} />}
              Yenile
            </button>
          </div>
        </div>
        <details className="commint-notes">
          <summary>Kapsam ve sınırlar</summary>
          <p className="commint-executive__scope-note">Kaynak: {COMMERCIAL_PROFITABILITY_EVIDENCE_SCOPE}</p>
          {workspace.hasDevelopmentPilotData ? (
            <p className="commint-provenance-strip" data-testid="commercial-profitability-development-banner">
              <span className="commint-provenance-strip__badge">Örnek veri</span>
              {COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER}
            </p>
          ) : null}
          {!workspace.realProfitabilityMappingComplete ? (
            <p className="commint-readonly-note" data-testid="commercial-profitability-mapping-note">
              {REAL_PROFITABILITY_MAPPING_NOTE}
            </p>
          ) : null}
          <p className="commint-readonly-note" data-testid="commercial-profitability-readonly-note">
            {COMMERCIAL_PROFITABILITY_READONLY_NOTE}
          </p>
        </details>
        {feedback ? <p className={`commint-feedback commint-feedback--${feedbackTone}`}>{feedback}</p> : null}
      </header>

      <CommercialProfitabilityComparisonFilterRail
        workspace={workspace}
        filters={filters}
        onFilterChange={updateFilters}
        onClear={clearFilters}
      />

      <section
        className="commint-kpi-grid"
        data-testid="commercial-profitability-kpi-grid"
        aria-label="Kârlılık karşılaştırma göstergeleri"
      >
        {workspace.kpiCards.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id] ?? BarChart3;
          return (
            <article
              key={kpi.id}
              className={`commint-kpi-card ${resolveProfitabilityKpiToneClass(kpi.tone)}`}
              data-kpi-id={kpi.id}
              aria-label={`${kpi.label}: ${kpi.value}`}
            >
              <div className="commint-kpi-card__header">
                <p className="commint-kpi-card__label">{kpi.label}</p>
                <span className="commint-kpi-card__icon" aria-hidden="true"><Icon size={16} /></span>
              </div>
              <p className={kpiValueClass(kpi.value)}>{kpi.value}</p>
              <p className="commint-kpi-card__detail">{kpi.hint}</p>
            </article>
          );
        })}
      </section>

      <div
        className="commint-tab-rail"
        role="tablist"
        aria-label="Kârlılık karşılaştırma sekmeleri"
        data-testid="commercial-profitability-tab-rail"
      >
        {COMMERCIAL_PROFITABILITY_TAB_OPTIONS.map((tab, index) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            className={activeTab === tab.id ? "commint-tab commint-tab--active" : "commint-tab"}
            onClick={() => setActiveTab(tab.id)}
            onKeyDown={(event) => onTabKeyDown(event, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {isEmpty ? (
        <div className="commint-empty-state" data-testid="commercial-profitability-empty" role="status">
          <Scale aria-hidden="true" />
          <h2>Karşılaştırma sonucu yok</h2>
          <p>Seçili filtreler için kârlılık karşılaştırması bulunamadı.</p>
        </div>
      ) : null}

      {activeTab === "overview" && !isEmpty ? (
        <section className="commint-panel" data-testid="commercial-profitability-overview">
          <header className="commint-panel__header">
            <h2>Genel karşılaştırma özeti</h2>
            <p>Referans ve güncel dönem katkı marjı farkları arka uçtan gelir; arayüzde yeniden hesaplanmaz.</p>
          </header>
          {selectedComparison ? (
            <div className="commint-detail-grid">
              <div className="commint-metric-card">
                <p className="commint-metric-card__label">Referans katkı marjı</p>
                <p className="commint-metric-card__value">{selectedComparison.referenceMarginLabel}</p>
              </div>
              <div className="commint-metric-card">
                <p className="commint-metric-card__label">Güncel katkı marjı</p>
                <p className="commint-metric-card__value">{selectedComparison.currentMarginLabel}</p>
              </div>
              <div className={`commint-metric-card commint-metric-card--${formatProfitabilityVarianceTone(selectedComparison.comparisonOutcome)}`}>
                <p className="commint-metric-card__label">Katkı marjı değişimi</p>
                <p className="commint-metric-card__value">
                  {formatProfitabilityMarginDeltaDisplay(selectedComparison.marginDeltaLabel)}
                </p>
              </div>
              <div className="commint-metric-card">
                <p className="commint-metric-card__label">Sınıflandırma</p>
                <p className="commint-metric-card__value">{selectedComparison.outcomeLabel}</p>
              </div>
            </div>
          ) : null}
          {unavailableReason && selectedComparison ? (
            <div className="commint-insufficient-state" data-testid="commercial-profitability-insufficient" role="status">
              <AlertTriangle aria-hidden="true" />
              <p>{unavailableReason}</p>
            </div>
          ) : null}
          {workspace.positionPreviewRows.length > 0 ? (
            <div className="commint-table-wrap" tabIndex={0}>
              <table aria-label="Genel karşılaştırma özeti" className="commint-table">
                <thead>
                  <tr>
                    <th>Kapsam</th>
                    <th>Güncel marj</th>
                    <th>Karşılaştırılabilir</th>
                    <th>Konum</th>
                  </tr>
                </thead>
                <tbody>
                  {workspace.positionPreviewRows.map((row) => (
                    <tr key={row.id}>
                      <td>{row.scopeLabel}</td>
                      <td>{row.marginLabel}</td>
                      <td>{row.comparableCountLabel}</td>
                      <td>{row.positionStateLabel}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </section>
      ) : null}

      {activeTab === "temporal" ? (
        <section className="commint-panel" data-testid="commercial-profitability-temporal">
          <header className="commint-panel__header">
            <h2>Zamansal katkı eğilimleri</h2>
          </header>
          {temporalComparisons.length === 0 ? (
            <p className="commint-supplier-row__meta">Zamansal karşılaştırma yok</p>
          ) : (
            <div className="commint-comparison-grid">
              {temporalComparisons.map((comparison) => (
                <ComparisonSummaryCard
                  key={comparison.id}
                  comparison={comparison}
                  selected={selectedComparison?.id === comparison.id}
                  onSelect={() => setSelectedComparison(comparison.id)}
                />
              ))}
            </div>
          )}
        </section>
      ) : null}

      {activeTab === "cost-mix" ? (
        <section className="commint-panel" data-testid="commercial-profitability-cost-mix">
          <header className="commint-panel__header">
            <h2>Maliyet bileşeni karışımı</h2>
          </header>
          {costMixComparisons.length === 0 ? (
            <p className="commint-supplier-row__meta">Maliyet bileşeni karşılaştırması yok</p>
          ) : (
            <div className="commint-comparison-grid">
              {costMixComparisons.map((comparison) => (
                <ComparisonSummaryCard
                  key={comparison.id}
                  comparison={comparison}
                  selected={selectedComparison?.id === comparison.id}
                  onSelect={() => setSelectedComparison(comparison.id)}
                />
              ))}
            </div>
          )}
        </section>
      ) : null}

      {activeTab === "positions" ? (
        <section className="commint-panel" data-testid="commercial-profitability-positions">
          <header className="commint-panel__header">
            <h2>Konum karşılaştırması</h2>
          </header>
          {positionComparisons.length === 0 ? (
            <p className="commint-supplier-row__meta">Konum karşılaştırması yok</p>
          ) : (
            <div className="commint-comparison-grid">
              {positionComparisons.map((comparison) => (
                <ComparisonSummaryCard
                  key={comparison.id}
                  comparison={comparison}
                  selected={selectedComparison?.id === comparison.id}
                  onSelect={() => setSelectedComparison(comparison.id)}
                />
              ))}
            </div>
          )}
        </section>
      ) : null}

      {selectedComparison ? (
        <section className="commint-panel" data-testid="commercial-profitability-detail">
          <header className="commint-panel__header">
            <h2>Karşılaştırma detayı</h2>
            <p>{selectedComparison.comparisonReference}</p>
          </header>
          {detailStatus === "loading" || isPending ? (
            <div className="commint-loading-state" data-testid="commercial-profitability-loading" role="status">
              <LoaderCircle className="animate-spin" aria-hidden="true" />
              <p>Karşılaştırma detayı yükleniyor…</p>
            </div>
          ) : null}
          {detailStatus === "error" ? (
            <div className="commint-error-banner" data-testid="commercial-profitability-error" role="alert">
              <ShieldAlert aria-hidden="true" />
              <InlineUnavailable subject="Karşılaştırma detayı" />
            </div>
          ) : null}
          {detailStatus === "ready" && comparisonDetail ? (
            <CommercialProfitabilityComparisonDetailPanel comparisonDetail={comparisonDetail} />
          ) : null}
          {unavailableReason && selectedComparison ? (
            <div className="commint-insufficient-state" data-testid="commercial-profitability-detail-insufficient" role="status">
              <AlertTriangle aria-hidden="true" />
              <p>{unavailableReason}</p>
            </div>
          ) : null}
        </section>
      ) : null}
    </div>
  );
}

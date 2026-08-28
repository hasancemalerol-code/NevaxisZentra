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
  Boxes,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  TrendingDown,
  TrendingUp,
  Wallet,
} from "lucide-react";

import {
  buildCommercialMaterialCostProfitabilityFilterParams,
  clearCommercialMaterialCostProfitabilityFilterParams,
  readCommercialMaterialCostProfitabilityFilters,
  toCommercialMaterialCostProfitabilityApiFilters,
} from "../application/commercial-material-cost-profitability-filter";
import {
  COMMERCIAL_MATERIAL_COST_PROFITABILITY_EMPTY_COST_NOTE,
  COMMERCIAL_MATERIAL_COST_PROFITABILITY_EMPTY_MATERIAL_NOTE,
  COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS,
  COMMERCIAL_MATERIAL_COST_PROFITABILITY_NO_SELECTION_NOTE,
  COMMERCIAL_MATERIAL_COST_PROFITABILITY_SCOPE_NOTE,
  COMMERCIAL_MATERIAL_COST_PROFITABILITY_SUBTITLE,
} from "../domain/commercial-material-cost-profitability-labels";
import {
  COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER,
  COMMERCIAL_PROFITABILITY_READONLY_NOTE,
  REAL_PROFITABILITY_MAPPING_NOTE,
} from "../domain/commercial-profitability-labels";
import type { CommercialProfitabilityComparisonDetail } from "../types/commercial-profitability-types";
import type {
  CommercialMaterialCostProfitabilityLensId,
  CommercialMaterialCostProfitabilityWorkspacePayload,
} from "../types/commercial-material-cost-profitability-types";
import { resolveProfitabilityKpiToneClass } from "./commercial-profitability-presentation";
import {
  CommercialCostDriverProfitabilityComparisonTable,
  CommercialMaterialCostProfitabilityFilterRail,
  CommercialMaterialProfitabilityComparisonTable,
  CommercialProfitabilityComparisonDetailPanel,
  inferProfitabilityUnavailableReason,
} from "./commercial-material-cost-profitability-workspace-sections";
import {
  loadCommercialMaterialCostProfitabilityComparisonDetailAction,
  refreshCommercialMaterialCostProfitabilityWorkspaceAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";

type CommercialMaterialCostProfitabilityWorkspaceViewProps = Readonly<{
  workspace: CommercialMaterialCostProfitabilityWorkspacePayload;
  initialComparisonDetail?: CommercialProfitabilityComparisonDetail | null;
  initialDetailStatus?: "idle" | "ready" | "error";
}>;

const VALID_LENS_IDS = new Set(
  COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS.map((lens) => lens.id),
);

const KPI_ICONS: Record<string, typeof BarChart3> = {
  "material-comparisons": Boxes,
  "cost-driver-comparisons": Wallet,
  "combined-improvement": TrendingUp,
  "combined-deterioration": TrendingDown,
  "combined-stable": BarChart3,
  "combined-insufficient-disputed": ShieldAlert,
  "largest-cost-driver": Wallet,
};

export function CommercialMaterialCostProfitabilityWorkspaceView({
  workspace: initialWorkspace,
  initialComparisonDetail = null,
  initialDetailStatus = "idle",
}: CommercialMaterialCostProfitabilityWorkspaceViewProps) {
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
    () => readCommercialMaterialCostProfitabilityFilters(searchParams),
    [searchParams],
  );

  const activeLens = useMemo<CommercialMaterialCostProfitabilityLensId>(() => {
    const lens = searchParams.get("lens");
    return lens && VALID_LENS_IDS.has(lens as CommercialMaterialCostProfitabilityLensId)
      ? (lens as CommercialMaterialCostProfitabilityLensId)
      : "material";
  }, [searchParams]);

  const selectedComparisonId = searchParams.get("comparison")
    ?? (activeLens === "material"
      ? workspace.selectedMaterialComparisonId
      : workspace.selectedCostMixComparisonId);

  const activeComparisons = activeLens === "material"
    ? workspace.materialComparisons
    : workspace.costMixComparisons;
  const activeTableRows = activeLens === "material"
    ? workspace.materialTableRows
    : workspace.costMixTableRows;
  const activePage = activeLens === "material" ? workspace.materialPage : workspace.costMixPage;
  const activePageSize = activeLens === "material" ? workspace.materialPageSize : workspace.costMixPageSize;
  const activeReturnedCount = activeLens === "material"
    ? workspace.materialReturnedCount
    : workspace.costMixReturnedCount;

  const selectedComparison = activeComparisons.find((item) => item.id === selectedComparisonId) ?? null;
  const unavailableReason = inferProfitabilityUnavailableReason(selectedComparison);
  const isEmpty = activeTableRows.length === 0;

  const refreshWorkspace = useCallback((
    comparisonId?: string | null,
    nextFilters = filters,
    lens = activeLens,
  ) => {
    startTransition(async () => {
      const result = await refreshCommercialMaterialCostProfitabilityWorkspaceAction(
        comparisonId,
        toCommercialMaterialCostProfitabilityApiFilters(nextFilters),
        lens,
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
  }, [activeLens, filters]);

  const updateFilters = useCallback((
    updates: Parameters<typeof buildCommercialMaterialCostProfitabilityFilterParams>[1],
  ) => {
    const params = buildCommercialMaterialCostProfitabilityFilterParams(searchParams, updates);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    const nextFilters = readCommercialMaterialCostProfitabilityFilters(params);
    refreshWorkspace(selectedComparisonId, nextFilters, activeLens);
  }, [activeLens, pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const clearFilters = useCallback(() => {
    const params = clearCommercialMaterialCostProfitabilityFilterParams(searchParams);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    refreshWorkspace(selectedComparisonId, readCommercialMaterialCostProfitabilityFilters(params), activeLens);
  }, [activeLens, pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const setActiveLens = useCallback((lensId: CommercialMaterialCostProfitabilityLensId) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("lens", lensId);
    params.delete("comparison");
    params.set("page", "1");
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    setComparisonDetail(null);
    setDetailStatus("idle");
    refreshWorkspace(null, readCommercialMaterialCostProfitabilityFilters(params), lensId);
  }, [pathname, refreshWorkspace, router, searchParams]);

  const loadComparisonDetail = useCallback((comparisonId: string) => {
    setComparisonDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialMaterialCostProfitabilityComparisonDetailAction(comparisonId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setComparisonDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const selectComparison = useCallback((comparisonId: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("comparison", comparisonId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    loadComparisonDetail(comparisonId);
    startTransition(async () => {
      const result = await refreshCommercialMaterialCostProfitabilityWorkspaceAction(
        comparisonId,
        toCommercialMaterialCostProfitabilityApiFilters(filters),
        activeLens,
      );
      if (result.status === "success") setWorkspace(result.workspace);
    });
  }, [activeLens, filters, loadComparisonDetail, pathname, router, searchParams]);

  useEffect(() => {
    if (!selectedComparisonId) {
      setComparisonDetail(null);
      setDetailStatus("idle");
      return;
    }
    if (!selectedComparison) {
      setComparisonDetail(null);
      setDetailStatus("idle");
      return;
    }
    if (comparisonDetail?.summary.id === selectedComparisonId && detailStatus === "ready") return;
    loadComparisonDetail(selectedComparisonId);
  }, [comparisonDetail?.summary.id, detailStatus, loadComparisonDetail, selectedComparison, selectedComparisonId]);

  const onLensKeyDown = useCallback((event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const nextIndex = event.key === "ArrowRight"
      ? (index + 1) % COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS.length
      : (index - 1 + COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS.length) % COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS.length;
    setActiveLens(COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS[nextIndex].id);
  }, [setActiveLens]);

  return (
    <main className="commint-page" data-testid="commercial-material-cost-profitability-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Malzeme ve Maliyet Kârlılığı</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_MATERIAL_COST_PROFITABILITY_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>{workspace.facilityLabel}</li>
              <li>Karşılaştırma penceresi: {workspace.reportingPeriodLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Filtre kapsamı: {workspace.filterScopeLabel}</li>
              <li>{COMMERCIAL_MATERIAL_COST_PROFITABILITY_SCOPE_NOTE}</li>
            </ul>
          </div>
          <div className="commint-executive__actions">
            <button
              type="button"
              className="commint-button"
              onClick={() => refreshWorkspace(selectedComparisonId, filters, activeLens)}
              disabled={isPending}
              aria-label="Çalışma alanını yenile"
            >
              {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <RefreshCw size={14} />}
              Yenile
            </button>
          </div>
        </div>
        {!workspace.realProfitabilityMappingComplete ? (
          <p className="commint-provenance-strip" data-testid="commercial-material-cost-profitability-development-banner">
            <span className="commint-provenance-strip__badge">Geliştirme</span>
            {COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER}
          </p>
        ) : null}
        {!workspace.realProfitabilityMappingComplete ? (
          <p className="commint-readonly-note" data-testid="commercial-material-cost-profitability-mapping-note">
            {REAL_PROFITABILITY_MAPPING_NOTE}
          </p>
        ) : null}
        <p className="commint-readonly-note" data-testid="commercial-material-cost-profitability-readonly-note">
          {COMMERCIAL_PROFITABILITY_READONLY_NOTE}
        </p>
        {feedback ? <p className={`commint-feedback commint-feedback--${feedbackTone}`}>{feedback}</p> : null}
      </header>

      <CommercialMaterialCostProfitabilityFilterRail
        workspace={workspace}
        filters={filters}
        onFilterChange={updateFilters}
        onClear={clearFilters}
      />

      <section
        className="commint-kpi-grid"
        data-testid="commercial-material-cost-profitability-kpi-grid"
        aria-label="Malzeme ve maliyet kârlılık göstergeleri"
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
              <p className="commint-kpi-card__value">{kpi.value}</p>
              <p className="commint-kpi-card__detail">{kpi.hint}</p>
            </article>
          );
        })}
      </section>

      <div
        className="commint-tab-rail"
        role="tablist"
        aria-label="Malzeme ve maliyet kârlılık lensleri"
        data-testid="commercial-material-cost-profitability-lens-rail"
      >
        {COMMERCIAL_MATERIAL_COST_PROFITABILITY_LENS_OPTIONS.map((lens, index) => (
          <button
            key={lens.id}
            type="button"
            role="tab"
            aria-selected={activeLens === lens.id}
            className={activeLens === lens.id ? "commint-tab commint-tab--active" : "commint-tab"}
            onClick={() => setActiveLens(lens.id)}
            onKeyDown={(event) => onLensKeyDown(event, index)}
            data-testid={`commercial-material-cost-profitability-lens-${lens.id}`}
          >
            {lens.label}
          </button>
        ))}
      </div>

      {isPending && activeTableRows.length === 0 ? (
        <div className="commint-loading-state" data-testid="commercial-material-cost-profitability-loading" role="status">
          <LoaderCircle className="animate-spin" aria-hidden="true" />
          <p>Karşılaştırmalar yükleniyor…</p>
        </div>
      ) : null}

      {isEmpty && !isPending ? (
        <div className="commint-empty-state" data-testid="commercial-material-cost-profitability-empty" role="status">
          <Boxes aria-hidden="true" />
          <h2>{activeLens === "material" ? "Malzeme karşılaştırması yok" : "Maliyet bileşeni karşılaştırması yok"}</h2>
          <p>
            {activeLens === "material"
              ? COMMERCIAL_MATERIAL_COST_PROFITABILITY_EMPTY_MATERIAL_NOTE
              : COMMERCIAL_MATERIAL_COST_PROFITABILITY_EMPTY_COST_NOTE}
          </p>
        </div>
      ) : null}

      {!isEmpty ? (
        <section
          className="commint-panel"
          data-testid={activeLens === "material"
            ? "commercial-material-cost-profitability-material-list"
            : "commercial-material-cost-profitability-cost-list"}
        >
          <header className="commint-panel__header">
            <h2>{activeLens === "material" ? "Malzeme katkı konumu" : "Maliyet bileşeni etkisi"}</h2>
            <p>Sayfa {activePage} · {activeReturnedCount} kayıt (bu sayfa kümesi)</p>
          </header>
          {activeLens === "material" ? (
            <CommercialMaterialProfitabilityComparisonTable
              rows={workspace.materialTableRows}
              selectedComparisonId={selectedComparisonId}
              onSelect={selectComparison}
            />
          ) : (
            <CommercialCostDriverProfitabilityComparisonTable
              rows={workspace.costMixTableRows}
              selectedComparisonId={selectedComparisonId}
              onSelect={selectComparison}
            />
          )}
          <div className="commint-pagination" data-testid="commercial-material-cost-profitability-pagination">
            <button
              type="button"
              className="commint-button commint-button--ghost"
              disabled={activePage <= 1 || isPending}
              onClick={() => updateFilters({ page: String(Math.max(1, activePage - 1)) })}
            >
              Önceki
            </button>
            <span>Sayfa {activePage}</span>
            <button
              type="button"
              className="commint-button commint-button--ghost"
              disabled={activeReturnedCount < activePageSize || isPending}
              onClick={() => updateFilters({ page: String(activePage + 1) })}
            >
              Sonraki
            </button>
          </div>
        </section>
      ) : null}

      <section className="commint-panel" data-testid="commercial-material-cost-profitability-detail">
        <header className="commint-panel__header">
          <h2>{activeLens === "material" ? "Malzeme karşılaştırma detayı" : "Maliyet bileşeni detayı"}</h2>
        </header>
        {!selectedComparisonId ? (
          <p className="commint-supplier-row__meta" data-testid="commercial-material-cost-profitability-no-selection" role="status">
            {COMMERCIAL_MATERIAL_COST_PROFITABILITY_NO_SELECTION_NOTE}
          </p>
        ) : null}
        {selectedComparisonId && !selectedComparison ? (
          <div className="commint-insufficient-state" data-testid="commercial-material-cost-profitability-insufficient" role="status">
            <AlertTriangle aria-hidden="true" />
            <p>Karşılaştırma kaydı yok veya erişilemiyor</p>
          </div>
        ) : null}
        {selectedComparison && unavailableReason ? (
          <div className="commint-insufficient-state" data-testid="commercial-material-cost-profitability-insufficient" role="status">
            <AlertTriangle aria-hidden="true" />
            <p>{unavailableReason}</p>
          </div>
        ) : null}
        {selectedComparisonId && selectedComparison && (detailStatus === "loading" || isPending) ? (
          <div className="commint-loading-state" data-testid="commercial-material-cost-profitability-detail-loading" role="status">
            <LoaderCircle className="animate-spin" aria-hidden="true" />
            <p>Karşılaştırma detayı yükleniyor…</p>
          </div>
        ) : null}
        {detailStatus === "error" ? (
          <div className="commint-error-banner" data-testid="commercial-material-cost-profitability-error" role="alert">
            <ShieldAlert aria-hidden="true" />
            <p>Karşılaştırma detayı yüklenemedi.</p>
          </div>
        ) : null}
        {detailStatus === "ready" && comparisonDetail ? (
          <CommercialProfitabilityComparisonDetailPanel comparisonDetail={comparisonDetail} />
        ) : null}
      </section>
    </main>
  );
}

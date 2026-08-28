"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  useTransition,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  BarChart3,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  TrendingDown,
  TrendingUp,
  Users,
} from "lucide-react";

import {
  buildCommercialCustomerProfitabilityFilterParams,
  clearCommercialCustomerProfitabilityFilterParams,
  readCommercialCustomerProfitabilityFilters,
  toCommercialCustomerProfitabilityApiFilters,
} from "../application/commercial-customer-profitability-filter";
import {
  COMMERCIAL_CUSTOMER_PROFITABILITY_EMPTY_NOTE,
  COMMERCIAL_CUSTOMER_PROFITABILITY_NO_SELECTION_NOTE,
  COMMERCIAL_CUSTOMER_PROFITABILITY_SCOPE_NOTE,
  COMMERCIAL_CUSTOMER_PROFITABILITY_SUBTITLE,
} from "../domain/commercial-customer-profitability-labels";
import {
  COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER,
  COMMERCIAL_PROFITABILITY_READONLY_NOTE,
  REAL_PROFITABILITY_MAPPING_NOTE,
} from "../domain/commercial-profitability-labels";
import type { CommercialProfitabilityComparisonDetail } from "../types/commercial-profitability-types";
import type { CommercialCustomerProfitabilityWorkspacePayload } from "../types/commercial-customer-profitability-types";
import { resolveProfitabilityKpiToneClass } from "./commercial-profitability-presentation";
import {
  CommercialCustomerProfitabilityComparisonTable,
  CommercialCustomerProfitabilityFilterRail,
  CommercialProfitabilityComparisonDetailPanel,
  inferProfitabilityUnavailableReason,
} from "./commercial-customer-profitability-workspace-sections";
import {
  loadCommercialCustomerProfitabilityComparisonDetailAction,
  refreshCommercialCustomerProfitabilityWorkspaceAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";

type CommercialCustomerProfitabilityWorkspaceViewProps = Readonly<{
  workspace: CommercialCustomerProfitabilityWorkspacePayload;
  initialComparisonDetail?: CommercialProfitabilityComparisonDetail | null;
  initialDetailStatus?: "idle" | "ready" | "error";
}>;

const KPI_ICONS: Record<string, typeof BarChart3> = {
  "customer-comparisons": Users,
  "customer-improvement": TrendingUp,
  "customer-deterioration": TrendingDown,
  "customer-stable": BarChart3,
  "customer-insufficient-disputed": ShieldAlert,
};

export function CommercialCustomerProfitabilityWorkspaceView({
  workspace: initialWorkspace,
  initialComparisonDetail = null,
  initialDetailStatus = "idle",
}: CommercialCustomerProfitabilityWorkspaceViewProps) {
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
    () => readCommercialCustomerProfitabilityFilters(searchParams),
    [searchParams],
  );

  const selectedComparisonId = searchParams.get("comparison") ?? workspace.selectedComparisonId;
  const selectedComparison = workspace.comparisons.find((item) => item.id === selectedComparisonId) ?? null;
  const unavailableReason = inferProfitabilityUnavailableReason(selectedComparison);
  const isEmpty = workspace.tableRows.length === 0;

  const refreshWorkspace = useCallback((
    comparisonId?: string | null,
    nextFilters = filters,
  ) => {
    startTransition(async () => {
      const result = await refreshCommercialCustomerProfitabilityWorkspaceAction(
        comparisonId,
        toCommercialCustomerProfitabilityApiFilters(nextFilters),
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
    updates: Parameters<typeof buildCommercialCustomerProfitabilityFilterParams>[1],
  ) => {
    const params = buildCommercialCustomerProfitabilityFilterParams(searchParams, updates);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    const nextFilters = readCommercialCustomerProfitabilityFilters(params);
    refreshWorkspace(selectedComparisonId, nextFilters);
  }, [pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const clearFilters = useCallback(() => {
    const params = clearCommercialCustomerProfitabilityFilterParams(searchParams);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    refreshWorkspace(selectedComparisonId, readCommercialCustomerProfitabilityFilters(params));
  }, [pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const loadComparisonDetail = useCallback((comparisonId: string) => {
    setComparisonDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialCustomerProfitabilityComparisonDetailAction(comparisonId);
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
      const result = await refreshCommercialCustomerProfitabilityWorkspaceAction(
        comparisonId,
        toCommercialCustomerProfitabilityApiFilters(filters),
      );
      if (result.status === "success") setWorkspace(result.workspace);
    });
  }, [filters, loadComparisonDetail, pathname, router, searchParams]);

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

  return (
    <main className="commint-page" data-testid="commercial-customer-profitability-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Müşteri Kârlılığı</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_CUSTOMER_PROFITABILITY_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>{workspace.facilityLabel}</li>
              <li>Karşılaştırma penceresi: {workspace.reportingPeriodLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Sayfa sonucu: {workspace.returnedCount}</li>
              <li>{COMMERCIAL_CUSTOMER_PROFITABILITY_SCOPE_NOTE}</li>
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
        {!workspace.realProfitabilityMappingComplete ? (
          <p className="commint-provenance-strip" data-testid="commercial-customer-profitability-development-banner">
            <span className="commint-provenance-strip__badge">Geliştirme</span>
            {COMMERCIAL_PROFITABILITY_DEVELOPMENT_BANNER}
          </p>
        ) : null}
        {!workspace.realProfitabilityMappingComplete ? (
          <p className="commint-readonly-note" data-testid="commercial-customer-profitability-mapping-note">
            {REAL_PROFITABILITY_MAPPING_NOTE}
          </p>
        ) : null}
        <p className="commint-readonly-note" data-testid="commercial-customer-profitability-readonly-note">
          {COMMERCIAL_PROFITABILITY_READONLY_NOTE}
        </p>
        {feedback ? <p className={`commint-feedback commint-feedback--${feedbackTone}`}>{feedback}</p> : null}
      </header>

      <CommercialCustomerProfitabilityFilterRail
        workspace={workspace}
        filters={filters}
        onFilterChange={updateFilters}
        onClear={clearFilters}
      />

      <section
        className="commint-kpi-grid"
        data-testid="commercial-customer-profitability-kpi-grid"
        aria-label="Müşteri kârlılık göstergeleri"
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

      {isPending && workspace.tableRows.length === 0 ? (
        <div className="commint-loading-state" data-testid="commercial-customer-profitability-loading" role="status">
          <LoaderCircle className="animate-spin" aria-hidden="true" />
          <p>Müşteri kârlılık karşılaştırmaları yükleniyor…</p>
        </div>
      ) : null}

      {isEmpty && !isPending ? (
        <div className="commint-empty-state" data-testid="commercial-customer-profitability-empty" role="status">
          <Users aria-hidden="true" />
          <h2>Müşteri karşılaştırması yok</h2>
          <p>{COMMERCIAL_CUSTOMER_PROFITABILITY_EMPTY_NOTE}</p>
        </div>
      ) : null}

      {!isEmpty ? (
        <section className="commint-panel" data-testid="commercial-customer-profitability-list">
          <header className="commint-panel__header">
            <h2>Müşteri karşılaştırma tablosu</h2>
            <p>Sayfa {workspace.page} · {workspace.returnedCount} kayıt (bu sayfa kümesi)</p>
          </header>
          <CommercialCustomerProfitabilityComparisonTable
            rows={workspace.tableRows}
            selectedComparisonId={selectedComparisonId}
            onSelect={selectComparison}
          />
          <div className="commint-pagination" data-testid="commercial-customer-profitability-pagination">
            <button
              type="button"
              className="commint-button commint-button--ghost"
              disabled={workspace.page <= 1 || isPending}
              onClick={() => updateFilters({ page: String(Math.max(1, workspace.page - 1)) })}
            >
              Önceki
            </button>
            <span>Sayfa {workspace.page}</span>
            <button
              type="button"
              className="commint-button commint-button--ghost"
              disabled={workspace.returnedCount < workspace.pageSize || isPending}
              onClick={() => updateFilters({ page: String(workspace.page + 1) })}
            >
              Sonraki
            </button>
          </div>
        </section>
      ) : null}

      <section className="commint-panel" data-testid="commercial-customer-profitability-detail">
        <header className="commint-panel__header">
          <h2>Müşteri karşılaştırma detayı</h2>
        </header>
        {!selectedComparisonId ? (
          <p className="commint-supplier-row__meta" data-testid="commercial-customer-profitability-no-selection" role="status">
            {COMMERCIAL_CUSTOMER_PROFITABILITY_NO_SELECTION_NOTE}
          </p>
        ) : null}
        {selectedComparisonId && !selectedComparison ? (
          <div className="commint-insufficient-state" data-testid="commercial-customer-profitability-insufficient" role="status">
            <AlertTriangle aria-hidden="true" />
            <p>Karşılaştırma kaydı yok</p>
          </div>
        ) : null}
        {selectedComparison && unavailableReason ? (
          <div className="commint-insufficient-state" data-testid="commercial-customer-profitability-insufficient" role="status">
            <AlertTriangle aria-hidden="true" />
            <p>{unavailableReason}</p>
          </div>
        ) : null}
        {selectedComparisonId && (detailStatus === "loading" || isPending) ? (
          <div className="commint-loading-state" data-testid="commercial-customer-profitability-detail-loading" role="status">
            <LoaderCircle className="animate-spin" aria-hidden="true" />
            <p>Karşılaştırma detayı yükleniyor…</p>
          </div>
        ) : null}
        {detailStatus === "error" ? (
          <div className="commint-error-banner" data-testid="commercial-customer-profitability-error" role="alert">
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

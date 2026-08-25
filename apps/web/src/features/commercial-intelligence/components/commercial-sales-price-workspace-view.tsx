"use client";

import {
  useCallback,
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
} from "lucide-react";

import {
  buildCommercialSalesPriceComparisonSecondaryLabel,
  COMMERCIAL_SALES_PRICE_CUSTOMER_POSITION_TYPE,
  COMMERCIAL_SALES_PRICE_PILOT_MATERIAL_ID,
  COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE,
  resolveCommercialIntelligenceKpiToneClass,
} from "./commercial-intelligence-presentation";
import { CommercialIntelligenceCustomerRow } from "./commercial-intelligence-customer-row";
import {
  buildSalesPriceHistoryRows,
  clearSalesPriceHistoryFilterParams,
  buildSalesPriceHistoryFilterParams,
  filterSalesPriceHistoryRows,
  readSalesPriceHistoryFilters,
  SalesPriceDetailDrawerBody,
  SalesPriceExecutiveDriverSummary,
  SalesPriceHistoryFilterRail,
} from "./commercial-sales-price-workspace-sections";
import {
  formatCount,
  formatCurrency,
} from "../domain/commercial-intelligence-formatters";
import {
  COMMERCIAL_SALES_PRICE_DEVELOPMENT_BANNER,
  COMMERCIAL_INTELLIGENCE_READONLY_NOTE,
  COMMERCIAL_SALES_PRICE_EVIDENCE_SCOPE,
  COMMERCIAL_SALES_PRICE_SUBTITLE,
  COMMERCIAL_SALES_PRICE_TAB_OPTIONS,
  REAL_TRANSACTION_MAPPING_NOTE,
} from "../domain/commercial-intelligence-labels";
import type {
  CommercialSalesPriceAssessmentDetail,
  CommercialSalesPriceComparisonDetail,
  CommercialSalesPriceTabId,
  CommercialSalesPriceWorkspacePayload,
} from "../types/commercial-intelligence-types";
import {
  evaluateCommercialSalesPriceComparisonAction,
  loadCommercialSalesPriceAssessmentDetailAction,
  loadCommercialSalesPriceComparisonDetailAction,
  refreshCommercialSalesPriceWorkspaceAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";

type CommercialSalesPriceWorkspaceViewProps = Readonly<{
  workspace: CommercialSalesPriceWorkspacePayload;
  canManage: boolean;
}>;

const VALID_TAB_IDS = new Set(
  COMMERCIAL_SALES_PRICE_TAB_OPTIONS.map((tab) => tab.id),
);

const KPI_ICONS: Record<string, typeof BarChart3> = {
  "comparable-sales-evidence": BarChart3,
  "excluded-evidence": AlertTriangle,
  "current-median-sales-price": Scale,
  "reference-median-sales-price": Scale,
  "attention-deviations": AlertTriangle,
  "compared-customers": BarChart3,
};

function resolveProfileSourceEvidenceKind(
  profile: CommercialSalesPriceWorkspacePayload["profiles"][number] | undefined,
): string {
  if (!profile) return "manual-development-simulator";
  const first = profile.allowedSourceEvidenceKinds.split(",").map((token) => token.trim()).find(Boolean);
  return first ?? "manual-development-simulator";
}

export function CommercialSalesPriceWorkspaceView({
  workspace: initialWorkspace,
  canManage,
}: CommercialSalesPriceWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");

  const [comparisonDetail, setComparisonDetail] = useState<CommercialSalesPriceComparisonDetail | null>(null);
  const [assessmentDetail, setAssessmentDetail] = useState<CommercialSalesPriceAssessmentDetail | null>(null);
  const [detailStatus, setDetailStatus] = useState<"idle" | "loading" | "ready" | "error">("idle");
  const [detailKind, setDetailKind] = useState<"comparison" | "assessment" | null>(null);
  const [selectedDetailId, setSelectedDetailId] = useState<string | null>(null);

  const [evalProfileId, setEvalProfileId] = useState("");
  const [evalWindowStart, setEvalWindowStart] = useState("");
  const [evalWindowEnd, setEvalWindowEnd] = useState("");
  const [evalReferenceStart, setEvalReferenceStart] = useState("");
  const [evalReferenceEnd, setEvalReferenceEnd] = useState("");

  const historyFilters = useMemo(
    () => readSalesPriceHistoryFilters(searchParams),
    [searchParams],
  );

  const approvedProfiles = useMemo(
    () => workspace.profiles.filter((profile) => profile.isExecutable),
    [workspace.profiles],
  );
  const effectiveEvalProfileId = evalProfileId || approvedProfiles[0]?.id || "";
  const selectedProfile = approvedProfiles.find((profile) => profile.id === effectiveEvalProfileId) ?? null;

  const activeTab = useMemo<CommercialSalesPriceTabId>(() => {
    const tab = searchParams.get("tab");
    return tab && VALID_TAB_IDS.has(tab as CommercialSalesPriceTabId)
      ? (tab as CommercialSalesPriceTabId)
      : "overview";
  }, [searchParams]);

  const selectedComparisonId = searchParams.get("comparison") ?? workspace.selectedCustomerComparisonId;

  const temporalComparisons = useMemo(
    () => workspace.comparisons.filter((item) => item.comparisonType === COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE),
    [workspace.comparisons],
  );
  const customerComparisons = useMemo(
    () => workspace.comparisons.filter((item) => item.comparisonType === COMMERCIAL_SALES_PRICE_CUSTOMER_POSITION_TYPE),
    [workspace.comparisons],
  );

  const historyRows = useMemo(
    () => buildSalesPriceHistoryRows(workspace),
    [workspace],
  );

  const filteredHistoryRows = useMemo(
    () => filterSalesPriceHistoryRows(historyRows, historyFilters),
    [historyFilters, historyRows],
  );

  const updateHistoryFilters = useCallback((updates: Parameters<typeof buildSalesPriceHistoryFilterParams>[1]) => {
    const params = buildSalesPriceHistoryFilterParams(searchParams, updates);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const clearHistoryFilters = useCallback(() => {
    const params = clearSalesPriceHistoryFilterParams(searchParams);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const setActiveTab = useCallback((tabId: CommercialSalesPriceTabId) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", tabId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const refreshWorkspace = useCallback((comparisonId?: string | null, assessmentId?: string | null) => {
    startTransition(async () => {
      const result = await refreshCommercialSalesPriceWorkspaceAction(
        assessmentId,
        comparisonId,
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
  }, []);

  const setSelectedCustomerComparison = useCallback((comparisonId: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("comparison", comparisonId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    startTransition(async () => {
      const result = await refreshCommercialSalesPriceWorkspaceAction(null, comparisonId);
      if (result.status === "success") setWorkspace(result.workspace);
    });
  }, [pathname, router, searchParams]);

  const openComparisonDetail = useCallback((comparisonId: string) => {
    setSelectedDetailId(comparisonId);
    setDetailKind("comparison");
    setComparisonDetail(null);
    setAssessmentDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialSalesPriceComparisonDetailAction(comparisonId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setComparisonDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const openAssessmentDetail = useCallback((assessmentId: string) => {
    setSelectedDetailId(assessmentId);
    setDetailKind("assessment");
    setComparisonDetail(null);
    setAssessmentDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialSalesPriceAssessmentDetailAction(assessmentId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setAssessmentDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const openExecutiveDetail = useCallback((kind: "assessment" | "comparison", id: string) => {
    if (kind === "assessment") openAssessmentDetail(id);
    else openComparisonDetail(id);
  }, [openAssessmentDetail, openComparisonDetail]);

  const closeDetail = useCallback(() => {
    setSelectedDetailId(null);
    setDetailKind(null);
    setComparisonDetail(null);
    setAssessmentDetail(null);
    setDetailStatus("idle");
  }, []);

  const handleEvaluateTemporalComparison = useCallback(() => {
    const profile = approvedProfiles.find((item) => item.id === effectiveEvalProfileId);
    if (!profile || !evalWindowStart || !evalWindowEnd) return;
    const currentStart = new Date(evalWindowStart);
    const currentEnd = new Date(evalWindowEnd);
    const referenceStart = evalReferenceStart ? new Date(evalReferenceStart) : new Date(currentStart);
    const referenceEnd = evalReferenceEnd ? new Date(evalReferenceEnd) : new Date(currentStart);
    if (!evalReferenceStart) referenceStart.setUTCMonth(referenceStart.getUTCMonth() - 3);

    startTransition(async () => {
      const result = await evaluateCommercialSalesPriceComparisonAction({
        profileId: profile.id,
        expectedProfileVersion: profile.version,
        comparisonType: COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE,
        materialId: COMMERCIAL_SALES_PRICE_PILOT_MATERIAL_ID,
        sourceEvidenceKind: resolveProfileSourceEvidenceKind(profile),
        currentWindowStartUtc: currentStart.toISOString(),
        currentWindowEndUtc: currentEnd.toISOString(),
        referenceWindowStartUtc: referenceStart.toISOString(),
        referenceWindowEndUtc: referenceEnd.toISOString(),
        currencyCode: "TRY",
        normalizedUnit: "kg",
        priceBasisGrossNet: profile.requiredGrossNetBasis,
        priceBasisTaxInclusion: profile.requiredTaxInclusion,
        priceBasisDiscountInclusion: profile.requiredDiscountInclusion,
        priceBasisFreightInclusion: profile.requiredFreightInclusion,
        sourceProvenance: profile.sourceProvenance,
        idempotencyKey: `COMMINT-SALES-TEMPORAL-UI-${Date.now()}`,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      const refresh = await refreshCommercialSalesPriceWorkspaceAction(null, result.entityId);
      if (refresh.status === "success") setWorkspace(refresh.workspace);
      setFeedback(result.idempotentReplay ? "Mevcut dönemsel karşılaştırma yeniden kullanıldı." : "Dönemsel karşılaştırma oluşturuldu.");
      setFeedbackTone("success");
    });
  }, [approvedProfiles, effectiveEvalProfileId, evalReferenceEnd, evalReferenceStart, evalWindowEnd, evalWindowStart]);

  const handleEvaluateCustomerComparison = useCallback(() => {
    const profile = approvedProfiles.find((item) => item.id === effectiveEvalProfileId);
    if (!profile || !evalWindowStart || !evalWindowEnd) return;

    startTransition(async () => {
      const result = await evaluateCommercialSalesPriceComparisonAction({
        profileId: profile.id,
        expectedProfileVersion: profile.version,
        comparisonType: COMMERCIAL_SALES_PRICE_CUSTOMER_POSITION_TYPE,
        materialId: COMMERCIAL_SALES_PRICE_PILOT_MATERIAL_ID,
        sourceEvidenceKind: resolveProfileSourceEvidenceKind(profile),
        currentWindowStartUtc: new Date(evalWindowStart).toISOString(),
        currentWindowEndUtc: new Date(evalWindowEnd).toISOString(),
        referenceWindowStartUtc: null,
        referenceWindowEndUtc: null,
        currencyCode: "TRY",
        normalizedUnit: "kg",
        priceBasisGrossNet: profile.requiredGrossNetBasis,
        priceBasisTaxInclusion: profile.requiredTaxInclusion,
        priceBasisDiscountInclusion: profile.requiredDiscountInclusion,
        priceBasisFreightInclusion: profile.requiredFreightInclusion,
        sourceProvenance: profile.sourceProvenance,
        idempotencyKey: `COMMINT-SALES-CUSTOMER-UI-${Date.now()}`,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      const refresh = await refreshCommercialSalesPriceWorkspaceAction(null, result.entityId);
      if (refresh.status === "success") {
        setWorkspace(refresh.workspace);
        const params = new URLSearchParams(searchParams.toString());
        params.set("comparison", result.entityId);
        router.replace(`${pathname}?${params.toString()}`, { scroll: false });
      }
      setFeedback(result.idempotentReplay ? "Mevcut müşteri karşılaştırması yeniden kullanıldı." : "Müşteri karşılaştırması oluşturuldu.");
      setFeedbackTone("success");
    });
  }, [approvedProfiles, effectiveEvalProfileId, evalWindowEnd, evalWindowStart, pathname, router, searchParams]);

  const onTabKeyDown = useCallback((event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const nextIndex = event.key === "ArrowRight"
      ? (index + 1) % COMMERCIAL_SALES_PRICE_TAB_OPTIONS.length
      : (index - 1 + COMMERCIAL_SALES_PRICE_TAB_OPTIONS.length) % COMMERCIAL_SALES_PRICE_TAB_OPTIONS.length;
    setActiveTab(COMMERCIAL_SALES_PRICE_TAB_OPTIONS[nextIndex].id);
  }, [setActiveTab]);

  const spreadMedians = useMemo(() => {
    const medians = workspace.customerComparison.rows
      .map((row) => row.customerMedianUnitPrice)
      .filter((value): value is number => value !== null && !Number.isNaN(value));
    if (medians.length === 0) return { lowest: null, highest: null };
    return { lowest: Math.min(...medians), highest: Math.max(...medians) };
  }, [workspace.customerComparison.rows]);

  const tiedCustomerCount = useMemo(
    () => workspace.customerComparison.rows.filter((row) => row.isTied).length,
    [workspace.customerComparison.rows],
  );

  return (
    <main className="commint-page" data-testid="commercial-sales-price-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Satış Fiyat Analizi</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_SALES_PRICE_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>{workspace.facilityLabel}</li>
              <li>Kanıt penceresi: {workspace.reportingPeriodLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Kanıt: {workspace.evidenceLabel}</li>
              <li>Kaynak: {COMMERCIAL_SALES_PRICE_EVIDENCE_SCOPE}</li>
            </ul>
          </div>
          <div className="commint-executive__actions">
            <button
              type="button"
              className="commint-button"
              onClick={() => refreshWorkspace(selectedComparisonId, null)}
              disabled={isPending}
              aria-label="Çalışma alanını yenile"
            >
              {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <RefreshCw size={14} />}
              Yenile
            </button>
          </div>
        </div>
        {workspace.hasDevelopmentPilotData ? (
          <p className="commint-provenance-strip" data-testid="commercial-sales-price-provenance-strip">
            <span className="commint-provenance-strip__badge">Geliştirme</span>
            {COMMERCIAL_SALES_PRICE_DEVELOPMENT_BANNER}
          </p>
        ) : null}
        {!workspace.realTransactionMappingComplete ? (
          <p className="commint-readonly-note" data-testid="commercial-sales-price-mapping-note">
            {REAL_TRANSACTION_MAPPING_NOTE}
          </p>
        ) : null}
        {!canManage ? (
          <p className="commint-readonly-note" data-testid="commercial-sales-price-readonly-note">
            {COMMERCIAL_INTELLIGENCE_READONLY_NOTE}
          </p>
        ) : null}
        {feedback ? <p className={`commint-feedback commint-feedback--${feedbackTone}`}>{feedback}</p> : null}
      </header>

      <section
        className="commint-kpi-grid"
        data-testid="commercial-sales-price-kpi-grid"
        aria-label="Satış fiyat göstergeleri"
      >
        {workspace.kpis.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id] ?? BarChart3;
          return (
            <article
              key={kpi.id}
              className={`commint-kpi-card ${resolveCommercialIntelligenceKpiToneClass(kpi.tone)}`}
              data-kpi-id={kpi.id}
              data-kpi-scope={kpi.scopeLabel}
              data-kpi-source={kpi.sourceDataset}
              aria-label={`${kpi.label}: ${kpi.accessibleSummary}`}
            >
              <div className="commint-kpi-card__header">
                <p className="commint-kpi-card__label">{kpi.label}</p>
                <span className="commint-kpi-card__icon" aria-hidden="true"><Icon size={16} /></span>
              </div>
              <p className="commint-kpi-card__value">{kpi.value}</p>
              <p className="commint-kpi-card__detail">{kpi.detail}</p>
              <p className="commint-kpi-card__summary">{kpi.accessibleSummary}</p>
              <div className="commint-kpi-card__footer">
                <span>{kpi.scopeLabel}</span>
                <span className="commint-kpi-card__badge">{kpi.stateBadge}</span>
                <span>{kpi.evidenceLabel}</span>
              </div>
            </article>
          );
        })}
      </section>

      <div
        className="commint-tab-rail"
        role="tablist"
        aria-label="Satış fiyat sekmeleri"
        data-testid="commercial-sales-price-tab-rail"
      >
        {COMMERCIAL_SALES_PRICE_TAB_OPTIONS.map((tab, index) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            className={`commint-tab${activeTab === tab.id ? " commint-tab--active" : ""}`}
            onClick={() => setActiveTab(tab.id)}
            onKeyDown={(event) => onTabKeyDown(event, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "overview" ? (
        <div className="commint-panel-grid commint-panel-grid--overview">
          <section className="commint-panel commint-panel--temporal" data-testid="commercial-sales-price-temporal-deviation">
            <h2 className="commint-panel__title">Satış fiyatı görünümü</h2>
            {workspace.temporalChart.points.length > 0 ? (
              <div className="commint-chart" role="img" aria-label="Dönemsel satış fiyat sapması grafiği">
                <div className="commint-chart__bars">
                  {workspace.temporalChart.points.map((point) => {
                    const maxValue = Math.max(point.currentMedian ?? 0, point.referenceMedian ?? 0, 1);
                    const currentWidth = point.currentMedian !== null
                      ? `${Math.min(100, (point.currentMedian / maxValue) * 100)}%`
                      : "0%";
                    const referenceWidth = point.referenceMedian !== null
                      ? `${Math.min(100, (point.referenceMedian / maxValue) * 100)}%`
                      : "0%";
                    return (
                      <div key={point.id} className="commint-chart__row" title={point.traceReference}>
                        <span className="commint-chart__label">
                          <span className="commint-chart__primary">{point.primaryLabel}</span>
                          <span className="commint-chart__secondary">{point.secondaryLabel}</span>
                        </span>
                        <span className="commint-chart__bar" title="Güncel medyan"><span style={{ width: currentWidth }} /></span>
                        <span className="commint-chart__bar" title="Referans medyan"><span style={{ width: referenceWidth, background: "#16a34a" }} /></span>
                        <span>{point.signedVarianceLabel}</span>
                      </div>
                    );
                  })}
                </div>
              </div>
            ) : (
              <p className="commint-empty">{workspace.temporalChart.unavailableReason}</p>
            )}
            {workspace.temporalComparison.comparisonReference ? (
              <p className="commint-supplier-row__meta">
                {workspace.temporalComparison.outcomeLabel} · {workspace.temporalComparison.signedVarianceLabel} · {workspace.temporalComparison.evaluatedLabel}
              </p>
            ) : null}
          </section>

          <section className="commint-panel commint-panel--supplier" data-testid="commercial-sales-price-customer-position">
            <div className="commint-panel__heading">
              <h2 className="commint-panel__title">Kanıt ve müşteri kapsamı</h2>
              {workspace.customerComparison.primaryLabel ? (
                <p className="commint-panel__subtitle">
                  <span className="commint-panel__primary">{workspace.customerComparison.primaryLabel}</span>
                  <span className="commint-panel__secondary">{workspace.customerComparison.secondaryLabel}</span>
                  {workspace.customerComparison.comparisonReference ? (
                    <span className="commint-panel__trace">{workspace.customerComparison.comparisonReference}</span>
                  ) : null}
                </p>
              ) : null}
            </div>
            {workspace.customerComparison.rows.length > 0 ? (
              <div className="commint-supplier-list" data-testid="commercial-sales-price-overview-rows">
                <p className="commint-supplier-context">
                  {workspace.customerComparison.outcomeLabel} ·
                  Karşılaştırılabilir: {formatCount(workspace.customerComparison.comparableObservationCount)} ·
                  Dışlanan: {formatCount(workspace.customerComparison.excludedObservationCount)} ·
                  {workspace.customerComparison.evaluatedLabel}
                </p>
                {workspace.customerComparison.rows.map((row) => (
                  <CommercialIntelligenceCustomerRow
                    key={row.id}
                    row={row}
                    currencyCode={workspace.customerComparison.currencyCode}
                    normalizedUnit={workspace.customerComparison.normalizedUnit}
                  />
                ))}
                <p className="commint-supplier-row__meta">Yayılım: {workspace.customerComparison.spreadLabel}</p>
              </div>
            ) : (
              <p className="commint-empty">{workspace.customerComparison.unavailableReason}</p>
            )}
            <div className="commint-evidence-summary">
              <p className="commint-priority-row__title">
                Aday: {formatCount(workspace.temporalComparison.candidateObservationCount || workspace.customerComparison.candidateObservationCount)} ·
                Karşılaştırılabilir: {formatCount(workspace.temporalComparison.comparableObservationCount || workspace.customerComparison.comparableObservationCount)} ·
                Dışlanan: {formatCount(workspace.temporalComparison.excludedObservationCount || workspace.customerComparison.excludedObservationCount)}
              </p>
              <p className="commint-supplier-row__meta">
                Eşit sıra: {formatCount(tiedCustomerCount)} · Yayılım: {workspace.customerComparison.spreadLabel} · {workspace.evidenceLabel}
              </p>
              <p className="commint-supplier-row__meta">{workspace.provenanceDisclaimer}</p>
            </div>
          </section>

          <SalesPriceExecutiveDriverSummary
            workspace={workspace}
            onOpenDetail={openExecutiveDetail}
          />
        </div>
      ) : null}

      {activeTab === "temporal" ? (
        <section className="commint-panel" data-testid="commercial-sales-price-temporal-workspace">
          <h2 className="commint-panel__title">Dönemsel sapma çalıştırmaları</h2>
          <div className="commint-table-wrap">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Analiz</th>
                  <th>Malzeme</th>
                  <th>Sonuç</th>
                  <th>Sapma</th>
                  <th>Kanıt</th>
                  <th>Değerlendirme</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {temporalComparisons.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <span className="commint-panel__primary">{item.comparisonTypeLabel}</span>
                      <span className="commint-panel__trace">{item.comparisonReference}</span>
                    </td>
                    <td>{item.materialLabel}</td>
                    <td>{item.outcomeLabel}</td>
                    <td>{item.signedVarianceLabel}</td>
                    <td>{formatCount(item.comparableObservationCount)} / {formatCount(item.excludedObservationCount)}</td>
                    <td>{item.evaluatedLabel}</td>
                    <td>
                      <button type="button" className="commint-button" onClick={() => openComparisonDetail(item.id)}>Detay</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {canManage ? (
            <section className="commint-composer commint-composer--comparison" data-testid="commercial-sales-price-temporal-evaluation-controls">
              <header className="commint-composer__header">
                <div>
                  <h3 className="commint-composer__title">Dönemsel karşılaştırma çalıştır</h3>
                  <p className="commint-composer__hint">Referans ve güncel pencere ile dönemsel satış fiyat sapması çalıştırın.</p>
                </div>
              </header>
              <div className="commint-composer__grid">
                <label className="commint-field">
                  <span className="commint-field__label">Profil</span>
                  <select value={effectiveEvalProfileId} onChange={(event) => setEvalProfileId(event.target.value)} aria-label="Karşılaştırma profili">
                    <option value="">Onaylı profil seçin</option>
                    {approvedProfiles.map((profile) => (
                      <option key={profile.id} value={profile.id}>{profile.displayName}</option>
                    ))}
                  </select>
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Referans başlangıç (UTC)</span>
                  <input type="datetime-local" value={evalReferenceStart} onChange={(event) => setEvalReferenceStart(event.target.value)} aria-label="Referans başlangıç" />
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Referans bitiş (UTC)</span>
                  <input type="datetime-local" value={evalReferenceEnd} onChange={(event) => setEvalReferenceEnd(event.target.value)} aria-label="Referans bitiş" />
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Güncel başlangıç (UTC)</span>
                  <input type="datetime-local" value={evalWindowStart} onChange={(event) => setEvalWindowStart(event.target.value)} aria-label="Güncel başlangıç" />
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Güncel bitiş (UTC)</span>
                  <input type="datetime-local" value={evalWindowEnd} onChange={(event) => setEvalWindowEnd(event.target.value)} aria-label="Güncel bitiş" />
                </label>
              </div>
              <div className="commint-composer__evidence">
                <p>Minimum gözlem: {formatCount(selectedProfile?.minimumComparableObservations ?? workspace.defaultMinimumComparableObservations)}</p>
                <p>Kanıt durumu: {workspace.evidenceLabel}</p>
              </div>
              <div className="commint-composer__actions">
                <button
                  type="button"
                  className="commint-button commint-button--primary"
                  onClick={handleEvaluateTemporalComparison}
                  disabled={isPending || !effectiveEvalProfileId || !evalWindowStart || !evalWindowEnd}
                >
                  {isPending ? <LoaderCircle className="animate-spin" size={14} /> : null}
                  Dönemsel karşılaştırma çalıştır
                </button>
              </div>
            </section>
          ) : null}
        </section>
      ) : null}

      {activeTab === "customers" ? (
        <section className="commint-panel" data-testid="commercial-sales-price-customers-workspace">
          <h2 className="commint-panel__title">Müşteri fiyat sıralaması</h2>
          <div className="commint-toolbar">
            <select
              value={workspace.selectedCustomerComparisonId ?? ""}
              onChange={(event) => setSelectedCustomerComparison(event.target.value)}
              aria-label="Müşteri karşılaştırması seç"
            >
              <option value="">Çalıştırma seçin</option>
              {customerComparisons.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.comparisonTypeLabel} · {buildCommercialSalesPriceComparisonSecondaryLabel(item)}
                </option>
              ))}
            </select>
          </div>
          <div className="commint-supplier-list" data-testid="commercial-sales-price-customer-ranking">
            {workspace.customerComparison.rows.length > 0 ? workspace.customerComparison.rows.map((row) => (
              <CommercialIntelligenceCustomerRow
                key={row.id}
                row={row}
                currencyCode={workspace.customerComparison.currencyCode}
                normalizedUnit={workspace.customerComparison.normalizedUnit}
                showExcludedCount
                excludedObservationCount={workspace.customerComparison.excludedObservationCount}
              />
            )) : <p className="commint-empty">Müşteri karşılaştırma sonucu yok</p>}
          </div>
          <section className="commint-panel commint-panel--spread" data-testid="commercial-sales-price-spread-panel">
            <h3 className="commint-panel__title">Fiyat yayılımı ve kanıt</h3>
            <div className="commint-spread-panel">
              <div className="commint-spread-meter" aria-hidden="true">
                <span className="commint-spread-meter__low">
                  {spreadMedians.lowest !== null && workspace.customerComparison.currencyCode
                    ? formatCurrency(spreadMedians.lowest, workspace.customerComparison.currencyCode)
                    : "—"}
                </span>
                <span className="commint-spread-meter__bar" />
                <span className="commint-spread-meter__high">
                  {spreadMedians.highest !== null && workspace.customerComparison.currencyCode
                    ? formatCurrency(spreadMedians.highest, workspace.customerComparison.currencyCode)
                    : "—"}
                </span>
              </div>
              <div className="commint-detail-grid">
                <DetailField label="Yayılım oranı" value={workspace.customerComparison.spreadLabel} />
                <DetailField label="Profil eşiği" value={workspace.customerSpreadThresholdLabel} />
                <DetailField label="Sonuç" value={workspace.customerComparison.outcomeLabel ?? "—"} />
                <DetailField label="Kanıt durumu" value={workspace.evidenceLabel} />
                <DetailField
                  label="Karşılaştırılabilir / dışlanan"
                  value={`${formatCount(workspace.customerComparison.comparableObservationCount)} / ${formatCount(workspace.customerComparison.excludedObservationCount)}`}
                />
                <DetailField label="Önde gelen sürücü" value={workspace.customerComparison.leadingDriverLabel ?? "—"} />
              </div>
            </div>
          </section>
          {canManage ? (
            <section className="commint-composer commint-composer--comparison" data-testid="commercial-sales-price-customer-evaluation-controls">
              <header className="commint-composer__header">
                <div>
                  <h3 className="commint-composer__title">Müşteri karşılaştırması çalıştır</h3>
                  <p className="commint-composer__hint">Uyumlu kanıt penceresi ile müşteri fiyat konumu çalıştırın.</p>
                </div>
              </header>
              <div className="commint-composer__grid">
                <label className="commint-field">
                  <span className="commint-field__label">Profil</span>
                  <select value={effectiveEvalProfileId} onChange={(event) => setEvalProfileId(event.target.value)} aria-label="Karşılaştırma profili">
                    <option value="">Onaylı profil seçin</option>
                    {approvedProfiles.map((profile) => (
                      <option key={profile.id} value={profile.id}>{profile.displayName}</option>
                    ))}
                  </select>
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Pencere başlangıcı (UTC)</span>
                  <input type="datetime-local" value={evalWindowStart} onChange={(event) => setEvalWindowStart(event.target.value)} aria-label="Pencere başlangıcı" />
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Pencere bitişi (UTC)</span>
                  <input type="datetime-local" value={evalWindowEnd} onChange={(event) => setEvalWindowEnd(event.target.value)} aria-label="Pencere bitişi" />
                </label>
              </div>
              <div className="commint-composer__evidence">
                <p>Minimum gözlem: {formatCount(selectedProfile?.minimumComparableObservations ?? workspace.defaultMinimumComparableObservations)}</p>
                <p>Kanıt durumu: {workspace.evidenceLabel}</p>
              </div>
              <div className="commint-composer__actions">
                <button
                  type="button"
                  className="commint-button commint-button--primary"
                  onClick={handleEvaluateCustomerComparison}
                  disabled={isPending || !effectiveEvalProfileId || !evalWindowStart || !evalWindowEnd}
                >
                  {isPending ? <LoaderCircle className="animate-spin" size={14} /> : null}
                  Müşteri karşılaştırması çalıştır
                </button>
              </div>
            </section>
          ) : null}
        </section>
      ) : null}

      {activeTab === "history" ? (
        <section className="commint-panel" data-testid="commercial-sales-price-history-workspace">
          <h2 className="commint-panel__title">Değerlendirme geçmişi</h2>
          <SalesPriceHistoryFilterRail
            workspace={workspace}
            filters={historyFilters}
            onFilterChange={updateHistoryFilters}
            onClearFilters={clearHistoryFilters}
          />
          <div className="commint-table-wrap">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Sonuç türü</th>
                  <th>Analiz</th>
                  <th>Malzeme</th>
                  <th>Sonuç</th>
                  <th>Sapma / yayılım</th>
                  <th>Kanıt</th>
                  <th>Değerlendirme</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {filteredHistoryRows.length > 0 ? filteredHistoryRows.map((item) => (
                  <tr key={`${item.kind}-${item.id}`}>
                    <td>{item.resultTypeLabel}</td>
                    <td>
                      <span className="commint-panel__primary">{item.primaryLabel}</span>
                      <span className="commint-panel__trace">{item.reference}</span>
                    </td>
                    <td>{item.materialLabel}</td>
                    <td>{item.outcomeLabel}</td>
                    <td>{item.varianceLabel}</td>
                    <td>{item.evidenceLabel}</td>
                    <td>{item.evaluatedLabel}</td>
                    <td>
                      <button
                        type="button"
                        className="commint-button"
                        onClick={() => item.kind === "assessment" ? openAssessmentDetail(item.id) : openComparisonDetail(item.id)}
                      >
                        Detay
                      </button>
                    </td>
                  </tr>
                )) : (
                  <tr>
                    <td colSpan={8}>
                      <p className="commint-empty" data-testid="commercial-sales-price-history-empty">
                        Seçili filtrelerle eşleşen değerlendirme kaydı bulunamadı.
                      </p>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      {selectedDetailId ? (
        <>
          <button type="button" className="commint-drawer-backdrop" aria-label="Detayı kapat" onClick={closeDetail} />
          <aside className="commint-drawer" aria-label="Analiz detayı">
            <div className="commint-drawer__header">
              <h2>
                {detailKind === "assessment"
                  ? assessmentDetail?.summary.materialLabel ?? "Fiyat analizi detayı"
                  : comparisonDetail?.summary.materialLabel ?? "Karşılaştırma detayı"}
              </h2>
              <p className="commint-supplier-row__meta">
                {detailKind === "assessment"
                  ? assessmentDetail?.summary.assessmentReference ?? "—"
                  : comparisonDetail?.summary.comparisonReference ?? "—"}
              </p>
            </div>
            <div className="commint-drawer__body">
              {detailStatus === "loading" ? (
                <div data-testid="commercial-sales-price-detail-loading">
                  <div className="commint-skeleton-line commint-skeleton-line--title" />
                  <div className="commint-skeleton-line" />
                </div>
              ) : null}
              {detailStatus === "ready" && detailKind ? (
                <SalesPriceDetailDrawerBody
                  detailKind={detailKind}
                  comparisonDetail={comparisonDetail}
                  assessmentDetail={assessmentDetail}
                />
              ) : null}
              {detailStatus === "error" ? (
                <p className="commint-empty" role="alert">Detay yüklenemedi.</p>
              ) : null}
            </div>
          </aside>
        </>
      ) : null}
    </main>
  );
}

function DetailField({ label, value }: Readonly<{ label: string; value: string }>) {
  return (
    <div className="commint-detail-field">
      <span className="commint-detail-field__label">{label}</span>
      <span className="commint-detail-field__value">{value}</span>
    </div>
  );
}

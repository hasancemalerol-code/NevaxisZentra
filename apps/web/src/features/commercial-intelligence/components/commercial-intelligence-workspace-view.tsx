"use client";

import {
  useCallback,
  useMemo,
  useState,
  useTransition,
  type KeyboardEvent as ReactKeyboardEvent,
  type ReactNode,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  BarChart3,
  LoaderCircle,
  RefreshCw,
  Scale,
  TrendingUp,
} from "lucide-react";

import {
  COMMERCIAL_INTELLIGENCE_PILOT_MATERIAL_ID,
  buildCommercialIntelligenceComparisonPrimaryLabel,
  buildCommercialIntelligenceComparisonSecondaryLabel,
  resolveCommercialIntelligenceKpiToneClass,
  selectLatestCompatibleSupplierComparison,
} from "./commercial-intelligence-presentation";
import {
  formatCount,
  formatCurrency,
} from "../domain/commercial-intelligence-formatters";
import {
  COMMERCIAL_INTELLIGENCE_CURRENT_EVIDENCE_SCOPE,
  COMMERCIAL_INTELLIGENCE_DEVELOPMENT_BANNER,
  COMMERCIAL_INTELLIGENCE_READONLY_NOTE,
  COMMERCIAL_INTELLIGENCE_TAB_OPTIONS,
  getCommercialIntelligenceOutcomeLabel,
} from "../domain/commercial-intelligence-labels";
import type {
  CommercialIntelligenceAssessmentDetail,
  CommercialIntelligenceComparisonDetail,
  CommercialIntelligenceComparisonSummary,
  CommercialIntelligenceProfileSummary,
  CommercialIntelligenceTabId,
  CommercialIntelligenceWorkspacePayload,
} from "../types/commercial-intelligence-types";
import {
  evaluateCommercialIntelligenceAssessmentAction,
  evaluateCommercialIntelligenceComparisonAction,
  loadCommercialIntelligenceAssessmentDetailAction,
  loadCommercialIntelligenceComparisonDetailAction,
  refreshCommercialIntelligenceWorkspaceAction,
  updateCommercialIntelligenceProfileAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";

type CommercialIntelligenceWorkspaceViewProps = Readonly<{
  workspace: CommercialIntelligenceWorkspacePayload;
  canManage: boolean;
}>;

const VALID_TAB_IDS = new Set<CommercialIntelligenceTabId>(
  COMMERCIAL_INTELLIGENCE_TAB_OPTIONS.map((tab) => tab.id),
);

const KPI_ICONS: Record<string, typeof BarChart3> = {
  "comparable-observations": BarChart3,
  "excluded-observations": AlertTriangle,
  "attention-analyses": AlertTriangle,
  "favorable-variances": TrendingUp,
  "supplier-spread": Scale,
  "insufficient-conflict": AlertTriangle,
};

const PILOT_MATERIAL_ID = COMMERCIAL_INTELLIGENCE_PILOT_MATERIAL_ID;

function outcomeBadgeClass(outcome: string): string {
  if (outcome === "attention") return "commint-badge commint-badge--attention";
  if (outcome === "favorable-variance") return "commint-badge commint-badge--favorable";
  if (outcome === "source-conflict") return "commint-badge commint-badge--conflict";
  return "commint-badge";
}

export function CommercialIntelligenceWorkspaceView({
  workspace: initialWorkspace,
  canManage,
}: CommercialIntelligenceWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [isPending, startTransition] = useTransition();
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");

  const [selectedAssessmentId, setSelectedAssessmentId] = useState<string | null>(null);
  const [assessmentDetail, setAssessmentDetail] = useState<CommercialIntelligenceAssessmentDetail | null>(null);
  const [assessmentDetailStatus, setAssessmentDetailStatus] = useState<"idle" | "loading" | "ready" | "error" | "not-found">("idle");
  const [assessmentDetailError, setAssessmentDetailError] = useState<string | null>(null);

  const [comparisonDetail, setComparisonDetail] = useState<CommercialIntelligenceComparisonDetail | null>(null);

  const [selectedProfileId, setSelectedProfileId] = useState<string | null>(null);
  const [profileEditId, setProfileEditId] = useState<string | null>(null);
  const [profileDisplayName, setProfileDisplayName] = useState("");
  const [profileStatus, setProfileStatus] = useState("approved");

  const [outcomeFilter, setOutcomeFilter] = useState("");
  const [materialFilter, setMaterialFilter] = useState("");
  const [supplierFilter, setSupplierFilter] = useState("");
  const [searchQuery, setSearchQuery] = useState("");

  const [evalProfileId, setEvalProfileId] = useState("");
  const [evalCurrentPrice, setEvalCurrentPrice] = useState("100");
  const [evalWindowStart, setEvalWindowStart] = useState("");
  const [evalWindowEnd, setEvalWindowEnd] = useState("");
  const [evalReferenceStart, setEvalReferenceStart] = useState("");
  const [evalReferenceEnd, setEvalReferenceEnd] = useState("");
  const [evalMode, setEvalMode] = useState<"temporal" | "supplier">("temporal");
  const [comparisonMaterialId] = useState(PILOT_MATERIAL_ID);
  const [comparisonRunId, setComparisonRunId] = useState<string | null>(null);

  const approvedProfiles = useMemo(
    () => workspace.profiles.filter((profile) => profile.isExecutable),
    [workspace.profiles],
  );

  const selectedProfile = useMemo(() => {
    if (selectedProfileId) {
      return workspace.profiles.find((profile) => profile.id === selectedProfileId)
        ?? workspace.profiles[0]
        ?? null;
    }
    return workspace.profiles[0] ?? null;
  }, [workspace.profiles, selectedProfileId]);

  const effectiveEvalProfileId = evalProfileId || approvedProfiles[0]?.id || "";

  const activeTab = useMemo<CommercialIntelligenceTabId>(() => {
    const tab = searchParams.get("tab");
    return tab && VALID_TAB_IDS.has(tab as CommercialIntelligenceTabId)
      ? (tab as CommercialIntelligenceTabId)
      : "overview";
  }, [searchParams]);

  const setActiveTab = useCallback((tabId: CommercialIntelligenceTabId) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", tabId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const filteredAssessments = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return workspace.assessments.filter((item) => {
      if (outcomeFilter && item.assessmentOutcome !== outcomeFilter) return false;
      if (materialFilter && item.materialId !== materialFilter) return false;
      if (supplierFilter && item.supplierReference !== supplierFilter) return false;
      if (!query) return true;
      return item.materialLabel.toLowerCase().includes(query)
        || item.assessmentReference.toLowerCase().includes(query)
        || item.supplierLabel.toLowerCase().includes(query);
    });
  }, [workspace.assessments, outcomeFilter, materialFilter, supplierFilter, searchQuery]);

  const supplierComparisons = useMemo(
    () => workspace.comparisons
      .filter((item) => item.comparisonType === "supplier-price-position")
      .slice()
      .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc)),
    [workspace.comparisons],
  );

  const defaultSupplierComparison = useMemo(
    () => selectLatestCompatibleSupplierComparison(workspace.comparisons),
    [workspace.comparisons],
  );

  const selectedSupplierComparison = useMemo<CommercialIntelligenceComparisonSummary | null>(() => {
    const effectiveId = comparisonRunId ?? defaultSupplierComparison?.id ?? null;
    if (!effectiveId) return null;
    return supplierComparisons.find((item) => item.id === effectiveId) ?? defaultSupplierComparison;
  }, [comparisonRunId, supplierComparisons, defaultSupplierComparison]);

  const refreshWorkspace = useCallback(() => {
    startTransition(async () => {
      const result = await refreshCommercialIntelligenceWorkspaceAction();
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

  const openAssessmentDetail = useCallback((assessmentId: string) => {
    setSelectedAssessmentId(assessmentId);
    setAssessmentDetail(null);
    setAssessmentDetailError(null);
    setAssessmentDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialIntelligenceAssessmentDetailAction(assessmentId);
      if (result.status === "failure") {
        if ("code" in result && result.code === "not-found") {
          setAssessmentDetailStatus("not-found");
          return;
        }
        setAssessmentDetailStatus("error");
        setAssessmentDetailError(result.message);
        return;
      }
      setAssessmentDetail(result.detail);
      setAssessmentDetailStatus("ready");
    })();
  }, []);

  const closeAssessmentDetail = useCallback(() => {
    setSelectedAssessmentId(null);
    setAssessmentDetail(null);
    setAssessmentDetailStatus("idle");
    setAssessmentDetailError(null);
  }, []);

  const loadSupplierComparisonDetail = useCallback((comparisonId: string) => {
    setComparisonRunId(comparisonId);
    setComparisonDetail(null);
    void (async () => {
      const result = await loadCommercialIntelligenceComparisonDetailAction(comparisonId);
      if (result.status === "failure") {
        return;
      }
      setComparisonDetail(result.detail);
    })();
  }, []);

  const handleProfileSave = useCallback(() => {
    const profile = workspace.profiles.find((item) => item.id === profileEditId);
    if (!profile) return;
    startTransition(async () => {
      const result = await updateCommercialIntelligenceProfileAction({
        profileId: profile.id,
        expectedVersion: profile.version,
        displayName: profileDisplayName,
        status: profileStatus,
        comparisonDimension: profile.comparisonDimension,
        referenceWindowDurationDays: profile.referenceWindowDurationDays,
        minimumComparableObservations: profile.minimumComparableObservations,
        maximumPositiveVarianceRatio: profile.maximumPositiveVarianceRatio,
        supplierSpreadThreshold: profile.supplierSpreadThreshold,
        allowedEvidenceStatuses: profile.allowedEvidenceStatuses,
        requiredTaxInclusion: profile.requiredTaxInclusion,
        requiredDiscountInclusion: profile.requiredDiscountInclusion,
        requiredFreightInclusion: profile.requiredFreightInclusion,
        sourceProvenance: profile.sourceProvenance,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setProfileEditId(null);
      setFeedback(result.idempotentReplay ? "Profil güncellemesi zaten uygulanmıştı." : "Profil güncellendi.");
      setFeedbackTone("success");
      refreshWorkspace();
    });
  }, [workspace.profiles, profileEditId, profileDisplayName, profileStatus, refreshWorkspace]);

  const handleEvaluateAssessment = useCallback(() => {
    const profile = workspace.profiles.find((item) => item.id === effectiveEvalProfileId);
    if (!profile || !evalWindowStart || !evalWindowEnd) return;
    startTransition(async () => {
      const result = await evaluateCommercialIntelligenceAssessmentAction({
        profileId: profile.id,
        materialId: PILOT_MATERIAL_ID,
        supplierReference: "SUP-001",
        comparisonDimension: profile.comparisonDimension,
        currentUnitPrice: Number(evalCurrentPrice),
        currencyCode: "TRY",
        normalizedUnit: "kg",
        windowStartUtc: new Date(evalWindowStart).toISOString(),
        windowEndUtc: new Date(evalWindowEnd).toISOString(),
        priceBasisTaxInclusion: profile.requiredTaxInclusion,
        priceBasisDiscountInclusion: profile.requiredDiscountInclusion,
        priceBasisFreightInclusion: profile.requiredFreightInclusion,
        sourceProvenance: workspace.provenanceDisclaimer,
        idempotencyKey: `COMMINT-UI1-ASSESS-${Date.now()}`,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setFeedback(result.idempotentReplay ? "Fiyat analizi zaten kayıtlıydı." : "Fiyat analizi tamamlandı.");
      setFeedbackTone("success");
      refreshWorkspace();
    });
  }, [workspace.profiles, workspace.provenanceDisclaimer, effectiveEvalProfileId, evalCurrentPrice, evalWindowStart, evalWindowEnd, refreshWorkspace]);

  const handleEvaluateComparison = useCallback(() => {
    const profile = workspace.profiles.find((item) => item.id === effectiveEvalProfileId);
    if (!profile || !evalWindowStart || !evalWindowEnd) return;
    const currentStart = new Date(evalWindowStart);
    const currentEnd = new Date(evalWindowEnd);
    const referenceStart = evalReferenceStart
      ? new Date(evalReferenceStart)
      : new Date(currentStart);
    const referenceEnd = evalReferenceEnd
      ? new Date(evalReferenceEnd)
      : new Date(currentStart);
    if (!evalReferenceStart) {
      referenceStart.setUTCMonth(referenceStart.getUTCMonth() - 3);
    }
    startTransition(async () => {
      const result = await evaluateCommercialIntelligenceComparisonAction({
        profileId: profile.id,
        expectedProfileVersion: profile.version,
        comparisonType: evalMode === "supplier" ? "supplier-price-position" : "temporal-price-deviation",
        materialId: comparisonMaterialId,
        currentWindowStartUtc: currentStart.toISOString(),
        currentWindowEndUtc: currentEnd.toISOString(),
        referenceWindowStartUtc: evalMode === "supplier" ? null : referenceStart.toISOString(),
        referenceWindowEndUtc: evalMode === "supplier" ? null : referenceEnd.toISOString(),
        currencyCode: "TRY",
        normalizedUnit: "kg",
        priceBasisTaxInclusion: profile.requiredTaxInclusion,
        priceBasisDiscountInclusion: profile.requiredDiscountInclusion,
        priceBasisFreightInclusion: profile.requiredFreightInclusion,
        sourceProvenance: workspace.provenanceDisclaimer,
        idempotencyKey: `COMMINT-UI1-COMPARE-${evalMode}-${Date.now()}`,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setFeedback(result.idempotentReplay ? "Karşılaştırma zaten kayıtlıydı." : "Karşılaştırma tamamlandı.");
      setFeedbackTone("success");
      refreshWorkspace();
    });
  }, [workspace.profiles, workspace.provenanceDisclaimer, effectiveEvalProfileId, evalWindowStart, evalWindowEnd, evalReferenceStart, evalReferenceEnd, evalMode, comparisonMaterialId, refreshWorkspace]);

  const onTabKeyDown = (event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const delta = event.key === "ArrowRight" ? 1 : -1;
    const next = (index + delta + COMMERCIAL_INTELLIGENCE_TAB_OPTIONS.length) % COMMERCIAL_INTELLIGENCE_TAB_OPTIONS.length;
    setActiveTab(COMMERCIAL_INTELLIGENCE_TAB_OPTIONS[next].id);
  };

  const supplierRows = useMemo(() => {
    if (
      comparisonDetail
      && selectedSupplierComparison?.id === comparisonDetail.summary.id
      && comparisonDetail.supplierResults.length > 0
    ) {
      return comparisonDetail.supplierResults;
    }
    if (selectedSupplierComparison?.id === workspace.supplierPosition.comparisonId) {
      return workspace.supplierPosition.rows;
    }
    return comparisonDetail?.supplierResults ?? [];
  }, [
    comparisonDetail,
    selectedSupplierComparison?.id,
    workspace.supplierPosition.comparisonId,
    workspace.supplierPosition.rows,
  ]);

  return (
    <main className="commint-page" data-testid="commercial-intelligence-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Satın Alma Fiyat Analizi</h1>
            <p className="commint-executive__subtitle">
              Satın alma fiyat kanıtını, dönemsel sapmaları ve tedarikçi konumlarını tek çalışma alanında izleyin.
            </p>
            <ul className="commint-executive__meta">
              <li>{workspace.facilityLabel}</li>
              <li>Dönem: {workspace.reportingPeriodLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Kanıt: {workspace.evidenceLabel}</li>
              <li>{COMMERCIAL_INTELLIGENCE_CURRENT_EVIDENCE_SCOPE}</li>
            </ul>
          </div>
          <div className="commint-executive__actions">
            <button
              type="button"
              className="commint-button"
              onClick={refreshWorkspace}
              disabled={isPending}
              aria-label="Çalışma alanını yenile"
            >
              {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <RefreshCw size={14} />}
              Yenile
            </button>
          </div>
        </div>
        {workspace.hasDevelopmentPilotData ? (
          <p className="commint-provenance-strip" data-testid="commercial-intelligence-provenance-strip">
            <span className="commint-provenance-strip__badge">Geliştirme</span>
            {COMMERCIAL_INTELLIGENCE_DEVELOPMENT_BANNER}
          </p>
        ) : null}
        {!canManage ? (
          <p className="commint-readonly-note" data-testid="commercial-intelligence-readonly-note">
            {COMMERCIAL_INTELLIGENCE_READONLY_NOTE}
          </p>
        ) : null}
        {feedback ? (
          <p className={`commint-feedback commint-feedback--${feedbackTone}`}>{feedback}</p>
        ) : null}
      </header>

      <section className="commint-kpi-grid" data-testid="commercial-intelligence-kpi-grid" aria-label="Satın alma fiyat göstergeleri">
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
                <span className="commint-kpi-card__icon" aria-hidden="true">
                  <Icon size={16} />
                </span>
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
        aria-label="Ticari zekâ sekmeleri"
        data-testid="commercial-intelligence-tab-rail"
      >
        {COMMERCIAL_INTELLIGENCE_TAB_OPTIONS.map((tab, index) => (
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
          <section className="commint-panel commint-panel--temporal" data-testid="commercial-intelligence-temporal-deviation">
            <h2 className="commint-panel__title">Dönemsel fiyat sapması</h2>
            {workspace.temporalChart.points.length > 0 ? (
              <div className="commint-chart" data-testid="commercial-intelligence-temporal-chart" role="img" aria-label="Dönemsel fiyat sapması grafiği">
                <div className="commint-chart__bars">
                  {workspace.temporalChart.points.map((point) => {
                    const maxValue = Math.max(
                      point.currentMedian ?? 0,
                      point.referenceMedian ?? 0,
                      1,
                    );
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
          </section>

          <section className="commint-panel commint-panel--supplier" data-testid="commercial-intelligence-supplier-position">
            <div className="commint-panel__heading">
              <h2 className="commint-panel__title">Tedarikçi fiyat konumu</h2>
              {workspace.supplierPosition.primaryLabel ? (
                <p className="commint-panel__subtitle">
                  <span className="commint-panel__primary">{workspace.supplierPosition.primaryLabel}</span>
                  <span className="commint-panel__secondary">{workspace.supplierPosition.secondaryLabel}</span>
                  {workspace.supplierPosition.comparisonReference ? (
                    <span className="commint-panel__trace">{workspace.supplierPosition.comparisonReference}</span>
                  ) : null}
                </p>
              ) : null}
            </div>
            {workspace.supplierPosition.rows.length > 0 ? (
              <div className="commint-supplier-list" data-testid="commercial-intelligence-supplier-overview-rows">
                <p className="commint-supplier-context">
                  {workspace.supplierPosition.outcomeLabel} ·
                  Karşılaştırılabilir: {formatCount(workspace.supplierPosition.comparableObservationCount)} ·
                  Dışlanan: {formatCount(workspace.supplierPosition.excludedObservationCount)} ·
                  {workspace.supplierPosition.evaluatedLabel}
                </p>
                {workspace.supplierPosition.rows.map((row) => (
                  <div
                    key={row.id}
                    className={`commint-supplier-row${row.positionState === "lowest-comparable-price" ? " commint-supplier-row--baseline" : ""}`}
                    data-supplier-rank={row.priceRank}
                    data-supplier-tied={row.isTied ? "true" : "false"}
                  >
                    <div className="commint-supplier-row__top">
                      <p className="commint-supplier-row__title">
                        {row.supplierLabel}
                        {row.isTied ? <span className="commint-badge commint-badge--tie">Eşit sıra</span> : null}
                      </p>
                      <span className="commint-badge">Sıra {row.priceRank}</span>
                    </div>
                    <p className="commint-supplier-row__meta">
                      Medyan: {row.supplierMedianLabel}
                      {workspace.supplierPosition.currencyCode && workspace.supplierPosition.normalizedUnit
                        ? ` / ${workspace.supplierPosition.normalizedUnit}`
                        : ""}
                      · Konum: {row.relativePositionLabel} ·
                      Karşılaştırılabilir: {formatCount(row.comparableObservationCount)} ·
                      {row.positionStateLabel}
                    </p>
                  </div>
                ))}
                <p className="commint-supplier-row__meta">Yayılım: {workspace.supplierPosition.spreadLabel}</p>
              </div>
            ) : (
              <p className="commint-empty">{workspace.supplierPosition.unavailableReason}</p>
            )}
          </section>

          <section className="commint-panel commint-panel--priority" data-testid="commercial-intelligence-priority-deviations">
            <h2 className="commint-panel__title">Öncelikli sapmalar</h2>
            <div className="commint-priority-list">
              {workspace.priorityDeviations.length > 0 ? workspace.priorityDeviations.map((item) => (
                <div key={`${item.kind}-${item.id}`} className="commint-priority-row">
                  <p className="commint-priority-row__title">{item.primaryLabel}</p>
                  <p className="commint-supplier-row__meta">{item.secondaryLabel}</p>
                  <p className="commint-panel__trace">{item.traceReference}</p>
                </div>
              )) : <p className="commint-empty">Öncelikli sapma kaydı yok</p>}
            </div>
          </section>

          <section className="commint-panel commint-panel--evidence" data-testid="commercial-intelligence-evidence-summary">
            <h2 className="commint-panel__title">Kanıt ve dışlama özeti</h2>
            <div className="commint-evidence-summary">
              <p className="commint-priority-row__title">{workspace.evidenceSummary.reconciliationLabel}</p>
              <p className="commint-supplier-row__meta">
                Dikkat: {formatCount(workspace.evidenceSummary.attentionCount)} ·
                Olumlu: {formatCount(workspace.evidenceSummary.favorableCount)} ·
                Yetersiz: {formatCount(workspace.evidenceSummary.insufficientCount)} ·
                Çelişki: {formatCount(workspace.evidenceSummary.conflictCount)}
              </p>
            </div>
          </section>
        </div>
      ) : null}

      {activeTab === "analyses" ? (
        <section className="commint-panel" data-testid="commercial-intelligence-analyses-workspace">
          <h2 className="commint-panel__title">Fiyat analizleri</h2>
          <div className="commint-toolbar">
            <input
              type="search"
              placeholder="Malzeme veya referans ara"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              aria-label="Analiz ara"
            />
            <select value={outcomeFilter} onChange={(event) => setOutcomeFilter(event.target.value)} aria-label="Sonuç filtresi">
              <option value="">Tüm sonuçlar</option>
              {[...new Set(workspace.assessments.map((item) => item.assessmentOutcome))].map((outcome) => (
                <option key={outcome} value={outcome}>{getCommercialIntelligenceOutcomeLabel(outcome)}</option>
              ))}
            </select>
            <select value={materialFilter} onChange={(event) => setMaterialFilter(event.target.value)} aria-label="Malzeme filtresi">
              <option value="">Tüm malzemeler</option>
              {[...new Set(workspace.assessments.map((item) => item.materialId))].map((materialId) => {
                const label = workspace.assessments.find((item) => item.materialId === materialId)?.materialLabel ?? "Malzeme";
                return <option key={materialId} value={materialId}>{label}</option>;
              })}
            </select>
            <select value={supplierFilter} onChange={(event) => setSupplierFilter(event.target.value)} aria-label="Tedarikçi filtresi">
              <option value="">Tüm tedarikçiler</option>
              {[...new Set(workspace.assessments.map((item) => item.supplierReference).filter(Boolean))].map((supplier) => (
                <option key={supplier} value={supplier ?? ""}>{supplier}</option>
              ))}
            </select>
          </div>
          {canManage ? (
            <section className="commint-composer" data-testid="commercial-intelligence-evaluation-controls">
              <header className="commint-composer__header">
                <div>
                  <h3 className="commint-composer__title">Yeni fiyat analizi</h3>
                  <p className="commint-composer__hint">
                    Onaylı profil, uyumlu kanıt bazı ve deterministik pencere ile satın alma fiyat analizi çalıştırın.
                  </p>
                </div>
                {selectedProfile ? (
                  <div className="commint-composer__context">
                    <span className="commint-badge">{selectedProfile.displayName}</span>
                    <span className="commint-badge">{selectedProfile.statusLabel}</span>
                    <span className="commint-badge">Sürüm {selectedProfile.version}</span>
                    <span className="commint-composer__basis">{selectedProfile.compatibilitySummary}</span>
                  </div>
                ) : null}
              </header>
              <div className="commint-composer__grid">
                <label className="commint-field">
                  <span className="commint-field__label">Profil</span>
                  <select value={effectiveEvalProfileId} onChange={(event) => setEvalProfileId(event.target.value)} aria-label="Onaylı profil seç">
                    <option value="">Onaylı profil seçin</option>
                    {approvedProfiles.map((profile) => (
                      <option key={profile.id} value={profile.id}>{profile.displayName}</option>
                    ))}
                  </select>
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Malzeme</span>
                  <input value={workspace.assessments[0]?.materialLabel ?? "PILOT-PP-001 · PP-H110"} readOnly aria-label="Malzeme bağlamı" />
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Güncel birim fiyat</span>
                  <input type="number" value={evalCurrentPrice} onChange={(event) => setEvalCurrentPrice(event.target.value)} aria-label="Güncel birim fiyat" />
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
                <p>{workspace.provenanceDisclaimer}</p>
              </div>
              <div className="commint-composer__actions">
                <button type="button" className="commint-button commint-button--primary" onClick={handleEvaluateAssessment} disabled={isPending || !effectiveEvalProfileId || !evalWindowStart || !evalWindowEnd}>
                  {isPending ? <LoaderCircle className="animate-spin" size={14} /> : null}
                  Analizi çalıştır
                </button>
              </div>
            </section>
          ) : null}
          <div className="commint-table-wrap">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Malzeme</th>
                  <th>Referans</th>
                  <th>Sonuç</th>
                  <th>Güncel</th>
                  <th>Referans medyan</th>
                  <th>Sapma</th>
                  <th>Kanıt</th>
                  <th>Sürücü</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {filteredAssessments.map((item) => (
                  <tr key={item.id}>
                    <td>{item.materialLabel}</td>
                    <td className="commint-table__secondary">{item.assessmentReference}</td>
                    <td><span className={outcomeBadgeClass(item.assessmentOutcome)}>{item.outcomeLabel}</span></td>
                    <td>{formatCurrency(item.currentUnitPrice, item.currencyCode)}</td>
                    <td>{formatCurrency(item.referenceMedianUnitPrice, item.currencyCode)}</td>
                    <td>{item.varianceLabel}</td>
                    <td>{formatCount(item.comparableObservationCount)} / {formatCount(item.excludedObservationCount)}</td>
                    <td>{item.leadingDriverLabel}</td>
                    <td>
                      <button type="button" className="commint-button" onClick={() => openAssessmentDetail(item.id)}>Detay</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      {activeTab === "suppliers" ? (
        <section className="commint-panel" data-testid="commercial-intelligence-suppliers-workspace">
          <h2 className="commint-panel__title">Tedarikçi karşılaştırması</h2>
          <div className="commint-toolbar">
            <select
              value={selectedSupplierComparison?.id ?? ""}
              onChange={(event) => loadSupplierComparisonDetail(event.target.value)}
              aria-label="Karşılaştırma çalıştırması seç"
            >
              <option value="">Çalıştırma seçin</option>
              {supplierComparisons.map((item) => (
                <option key={item.id} value={item.id}>
                  {buildCommercialIntelligenceComparisonPrimaryLabel(item)} · {buildCommercialIntelligenceComparisonSecondaryLabel(item)}
                </option>
              ))}
            </select>
          </div>
          {selectedSupplierComparison ? (
            <p className="commint-panel__hint">
              <span className="commint-panel__primary">{buildCommercialIntelligenceComparisonPrimaryLabel(selectedSupplierComparison)}</span>
              <span className="commint-panel__secondary">{buildCommercialIntelligenceComparisonSecondaryLabel(selectedSupplierComparison)}</span>
              <span className="commint-panel__trace">{selectedSupplierComparison.comparisonReference}</span>
              · Yayılım: {selectedSupplierComparison.supplierSpreadLabel}
            </p>
          ) : null}
          <div className="commint-supplier-list" data-testid="commercial-intelligence-supplier-ranking">
            {supplierRows.length > 0 ? supplierRows.map((row) => (
              <div
                key={row.id}
                className={`commint-supplier-row${row.positionState === "lowest-comparable-price" ? " commint-supplier-row--baseline" : ""}`}
              >
                <div className="commint-supplier-row__top">
                  <p className="commint-supplier-row__title">
                    {row.supplierLabel}
                    {row.isTied ? <span className="commint-badge commint-badge--tie">Eşit sıra</span> : null}
                  </p>
                  <span className="commint-badge">Sıra {row.priceRank}</span>
                </div>
                <p className="commint-supplier-row__meta">
                  Medyan: {row.supplierMedianLabel} · Konum: {row.relativePositionLabel} ·
                  Karşılaştırılabilir: {formatCount(row.comparableObservationCount)} · {row.positionStateLabel}
                </p>
              </div>
            )) : <p className="commint-empty">Tedarikçi karşılaştırma sonucu yok</p>}
          </div>
          {canManage ? (
            <section className="commint-composer commint-composer--comparison" data-testid="commercial-intelligence-comparison-controls">
              <header className="commint-composer__header">
                <div>
                  <h3 className="commint-composer__title">Yeni karşılaştırma</h3>
                  <p className="commint-composer__hint">Çakışmayan pencereler ve uyumlu kanıt bazı ile karşılaştırma çalıştırın.</p>
                </div>
              </header>
              <div className="commint-composer__modes" role="radiogroup" aria-label="Karşılaştırma modu">
                <button
                  type="button"
                  className={`commint-mode${evalMode === "temporal" ? " commint-mode--active" : ""}`}
                  aria-pressed={evalMode === "temporal"}
                  onClick={() => setEvalMode("temporal")}
                >
                  Dönem karşılaştırması
                </button>
                <button
                  type="button"
                  className={`commint-mode${evalMode === "supplier" ? " commint-mode--active" : ""}`}
                  aria-pressed={evalMode === "supplier"}
                  onClick={() => setEvalMode("supplier")}
                >
                  Tedarikçi karşılaştırması
                </button>
              </div>
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
                  <span className="commint-field__label">Malzeme</span>
                  <input value={workspace.comparisons[0]?.materialLabel ?? "PILOT-PP-001 · PP-H110"} readOnly aria-label="Malzeme bağlamı" />
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Güncel pencere başlangıcı (UTC)</span>
                  <input type="datetime-local" value={evalWindowStart} onChange={(event) => setEvalWindowStart(event.target.value)} aria-label="Güncel pencere başlangıcı" />
                </label>
                <label className="commint-field">
                  <span className="commint-field__label">Güncel pencere bitişi (UTC)</span>
                  <input type="datetime-local" value={evalWindowEnd} onChange={(event) => setEvalWindowEnd(event.target.value)} aria-label="Güncel pencere bitişi" />
                </label>
                {evalMode === "temporal" ? (
                  <>
                    <label className="commint-field">
                      <span className="commint-field__label">Referans pencere başlangıcı (UTC)</span>
                      <input type="datetime-local" value={evalReferenceStart} onChange={(event) => setEvalReferenceStart(event.target.value)} aria-label="Referans pencere başlangıcı" />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Referans pencere bitişi (UTC)</span>
                      <input type="datetime-local" value={evalReferenceEnd} onChange={(event) => setEvalReferenceEnd(event.target.value)} aria-label="Referans pencere bitişi" />
                    </label>
                  </>
                ) : null}
              </div>
              <div className="commint-composer__evidence">
                <p>Minimum gözlem: {formatCount(selectedProfile?.minimumComparableObservations ?? workspace.defaultMinimumComparableObservations)}</p>
                <p>Kanıt durumu: {workspace.evidenceLabel}</p>
              </div>
              <div className="commint-composer__actions">
                <button type="button" className="commint-button commint-button--primary" onClick={handleEvaluateComparison} disabled={isPending || !effectiveEvalProfileId || !evalWindowStart || !evalWindowEnd}>
                  {isPending ? <LoaderCircle className="animate-spin" size={14} /> : null}
                  Analizi çalıştır
                </button>
              </div>
            </section>
          ) : null}
        </section>
      ) : null}

      {activeTab === "profiles" ? (
        <section className="commint-panel commint-profile-workspace" data-testid="commercial-intelligence-profile-workspace">
          <h2 className="commint-panel__title">Analiz profilleri</h2>
          <div className="commint-profile-workspace__layout">
            <div className="commint-profile-rail" role="listbox" aria-label="Analiz profilleri">
              {workspace.profiles.map((profile) => (
                <button
                  key={profile.id}
                  type="button"
                  role="option"
                  aria-selected={selectedProfile?.id === profile.id}
                  className={`commint-profile-rail__item${selectedProfile?.id === profile.id ? " commint-profile-rail__item--active" : ""}`}
                  onClick={() => setSelectedProfileId(profile.id)}
                >
                  <span>{profile.displayName}</span>
                  <span className="commint-supplier-row__meta">{profile.profileCode}</span>
                  <span className="commint-badge">{profile.statusLabel}</span>
                </button>
              ))}
            </div>
            {selectedProfile ? (
              <ProfileHero
                profile={selectedProfile}
                canManage={canManage}
                onEdit={() => {
                  setProfileEditId(selectedProfile.id);
                  setProfileDisplayName(selectedProfile.displayName);
                  setProfileStatus(selectedProfile.status);
                }}
              />
            ) : (
              <p className="commint-empty">Profil bulunamadı</p>
            )}
          </div>
        </section>
      ) : null}

      {selectedAssessmentId ? (
        <>
          <button type="button" className="commint-drawer-backdrop" aria-label="Detayı kapat" onClick={closeAssessmentDetail} />
          <aside className="commint-drawer" aria-label="Fiyat analizi detayı">
            <div className="commint-drawer__header">
              <h2>{assessmentDetail?.summary.materialLabel ?? "Fiyat analizi detayı"}</h2>
              <p className="commint-supplier-row__meta">{assessmentDetail?.summary.assessmentReference ?? "—"}</p>
            </div>
            <div className="commint-drawer__body">
              {assessmentDetailStatus === "loading" ? (
                <div data-testid="commercial-intelligence-analysis-detail-loading">
                  <div className="commint-skeleton-line commint-skeleton-line--title" />
                  <div className="commint-skeleton-line" />
                  <div className="commint-skeleton-line" />
                </div>
              ) : null}
              {assessmentDetailStatus === "error" || assessmentDetailStatus === "not-found" ? (
                <div className="commint-error-banner" data-testid="commercial-intelligence-analysis-detail-error" role="alert">
                  <p>{assessmentDetailError ?? "Analiz detayı yüklenemedi."}</p>
                  <button type="button" className="commint-button" onClick={() => selectedAssessmentId && openAssessmentDetail(selectedAssessmentId)}>Tekrar dene</button>
                </div>
              ) : null}
              {assessmentDetailStatus === "ready" && assessmentDetail ? (
                <div data-testid="commercial-intelligence-analysis-detail-ready">
                  <DetailSection title="Özet">
                    <div className="commint-detail-grid">
                      <DetailField label="Malzeme" value={assessmentDetail.summary.materialLabel} />
                      <DetailField label="Tedarikçi" value={assessmentDetail.summary.supplierLabel} />
                      <DetailField label="Sonuç" value={assessmentDetail.summary.outcomeLabel} />
                      <DetailField label="Pencere" value={assessmentDetail.summary.windowLabel} />
                      <DetailField label="Güncel fiyat" value={formatCurrency(assessmentDetail.summary.currentUnitPrice, assessmentDetail.summary.currencyCode)} />
                      <DetailField label="Referans medyan" value={formatCurrency(assessmentDetail.summary.referenceMedianUnitPrice, assessmentDetail.summary.currencyCode)} />
                      <DetailField label="Sapma" value={assessmentDetail.summary.varianceLabel} />
                      <DetailField label="Karşılaştırılabilir" value={formatCount(assessmentDetail.summary.comparableObservationCount)} />
                      <DetailField label="Dışlanan" value={formatCount(assessmentDetail.summary.excludedObservationCount)} />
                      <DetailField label="Kanıt durumu" value={assessmentDetail.summary.evidenceLabel} />
                    </div>
                  </DetailSection>
                  <DetailSection title="Fiyat bazı">
                    <div className="commint-detail-grid">
                      <DetailField label="Vergi" value={assessmentDetail.priceBasisTaxLabel} />
                      <DetailField label="İskonto" value={assessmentDetail.priceBasisDiscountLabel} />
                      <DetailField label="Navlun" value={assessmentDetail.priceBasisFreightLabel} />
                    </div>
                  </DetailSection>
                  <DetailSection title="Sürücüler">
                    <ul className="commint-driver-list">
                      {assessmentDetail.drivers.map((driver) => (
                        <li key={driver.id}>
                          {driver.reasonLabel} · {driver.supportingText} ({formatCount(driver.affectedObservationCount)} gözlem)
                        </li>
                      ))}
                    </ul>
                  </DetailSection>
                  <p className="commint-supplier-row__meta">Kanıt kaynağı: {assessmentDetail.sourceProvenance}</p>
                </div>
              ) : null}
            </div>
          </aside>
        </>
      ) : null}

      {profileEditId ? (
        <>
          <button type="button" className="commint-drawer-backdrop" aria-label="Profil düzenlemeyi kapat" onClick={() => setProfileEditId(null)} />
          <aside className="commint-drawer" aria-label="Profil düzenleme">
            <div className="commint-drawer__header">
              <h2>Profil düzenle</h2>
            </div>
            <div className="commint-drawer__body" data-testid="commercial-intelligence-profile-edit-drawer">
              <input value={profileDisplayName} onChange={(event) => setProfileDisplayName(event.target.value)} aria-label="Görünen ad" />
              <select value={profileStatus} onChange={(event) => setProfileStatus(event.target.value)} aria-label="Profil durumu">
                <option value="draft">Taslak</option>
                <option value="approved">Onaylı</option>
                <option value="inactive">Pasif</option>
              </select>
              <button type="button" className="commint-button commint-button--primary" onClick={handleProfileSave} disabled={isPending}>
                Kaydet
              </button>
            </div>
          </aside>
        </>
      ) : null}
    </main>
  );
}

function DetailSection({ title, children }: Readonly<{ title: string; children: ReactNode }>) {
  return (
    <section className="commint-detail-section">
      <h3>{title}</h3>
      {children}
    </section>
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

function ProfileHero({
  profile,
  canManage,
  onEdit,
}: Readonly<{
  profile: CommercialIntelligenceProfileSummary;
  canManage: boolean;
  onEdit: () => void;
}>) {
  return (
    <div data-testid="commercial-intelligence-profile-hero">
      <h3 className="commint-panel__title">{profile.displayName}</h3>
      <p className="commint-panel__hint">{profile.profileCode} · Sürüm {profile.version}</p>
      <div className="commint-detail-grid">
        <DetailField label="Durum" value={profile.statusLabel} />
        <DetailField label="Boyut" value={profile.comparisonDimensionLabel} />
        <DetailField label="Min. gözlem" value={formatCount(profile.minimumComparableObservations)} />
        <DetailField label="Maks. pozitif sapma" value={profile.maximumPositiveVarianceLabel} />
        <DetailField label="Tedarikçi yayılım eşiği" value={profile.supplierSpreadThresholdLabel} />
        <DetailField label="Uyumluluk" value={profile.compatibilitySummary} />
      </div>
      {canManage ? (
        <button type="button" className="commint-button" onClick={onEdit}>Profili düzenle</button>
      ) : (
        <p className="commint-readonly-note">{COMMERCIAL_INTELLIGENCE_READONLY_NOTE}</p>
      )}
    </div>
  );
}

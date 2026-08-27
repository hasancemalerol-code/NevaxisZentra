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
} from "lucide-react";

import {
  buildQuotationRiskComparisonSecondaryLabel,
  filterQuotationRiskCustomerComparisons,
  filterQuotationRiskTemporalComparisons,
  resolveQuotationRiskKpiToneClass,
} from "./commercial-quotation-risk-presentation";
import {
  buildAssessmentTablePrimaryLabel,
  buildAssessmentTableSecondaryLabel,
  buildQuotationRiskAssessmentFilterParams,
  clearQuotationRiskAssessmentFilterParams,
  filterQuotationRiskAssessments,
  formatAssessmentEvidenceLabel,
  QuotationRiskAssessmentDetailDrawerBody,
  QuotationRiskAssessmentFilterRail,
  QuotationRiskComparisonDetailPanel,
  readQuotationRiskAssessmentFilters,
} from "./commercial-quotation-risk-workspace-sections";
import {
  formatCount,
  formatNullableCount,
} from "../domain/commercial-intelligence-formatters";
import {
  COMMERCIAL_QUOTATION_RISK_DEVELOPMENT_BANNER,
  COMMERCIAL_QUOTATION_RISK_EVIDENCE_SCOPE,
  COMMERCIAL_QUOTATION_RISK_READONLY_NOTE,
  COMMERCIAL_QUOTATION_RISK_SUBTITLE,
  COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS,
  REAL_QUOTATION_MAPPING_NOTE,
} from "../domain/commercial-quotation-risk-labels";
import type {
  CommercialQuotationRiskAssessmentDetail,
  CommercialQuotationRiskComparisonDetail,
  CommercialQuotationRiskProfileSummary,
  CommercialQuotationRiskTabId,
  CommercialQuotationRiskWorkspacePayload,
} from "../types/commercial-quotation-risk-types";
import {
  loadCommercialQuotationRiskAssessmentDetailAction,
  loadCommercialQuotationRiskComparisonDetailAction,
  refreshCommercialQuotationRiskWorkspaceAction,
  updateCommercialQuotationRiskProfileAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";

type CommercialQuotationRiskWorkspaceViewProps = Readonly<{
  workspace: CommercialQuotationRiskWorkspacePayload;
  canManage: boolean;
}>;

const VALID_TAB_IDS = new Set(
  COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS.map((tab) => tab.id),
);

const KPI_ICONS: Record<string, typeof BarChart3> = {
  "candidate-assessments": BarChart3,
  "attention-assessments": AlertTriangle,
  "within-threshold-assessments": Scale,
  "insufficient-or-conflict": ShieldAlert,
  "current-risk-incidence": BarChart3,
};

type ProfileEditState = Readonly<{
  name: string;
  status: string;
  referenceWindowDurationDays: string;
  minimumComparableObservations: string;
  maximumNegativeVariancePercent: string;
  maximumPositiveVariancePercent: string;
  maximumPaymentTermDays: string;
  expiryWarningDays: string;
  maximumEvidenceAgeDays: string;
  allowedEvidenceStatuses: string;
  allowedSalesEvidenceKinds: string;
  requiredTaxInclusion: string;
  requiredDiscountInclusion: string;
  requiredFreightInclusion: string;
  requiredGrossNetBasis: string;
  sourceProvenance: string;
}>;

function buildProfileEditState(profile: CommercialQuotationRiskProfileSummary): ProfileEditState {
  return {
    name: profile.displayName,
    status: profile.status,
    referenceWindowDurationDays: String(profile.referenceWindowDurationDays),
    minimumComparableObservations: String(profile.minimumComparableObservations),
    maximumNegativeVariancePercent: String(profile.maximumNegativePriceVarianceRatio * 100),
    maximumPositiveVariancePercent: String(profile.maximumPositivePriceVarianceRatio * 100),
    maximumPaymentTermDays: String(profile.maximumPaymentTermDays),
    expiryWarningDays: String(profile.expiryWarningDays),
    maximumEvidenceAgeDays: String(profile.maximumEvidenceAgeDays),
    allowedEvidenceStatuses: profile.allowedEvidenceStatuses,
    allowedSalesEvidenceKinds: profile.allowedSalesEvidenceKinds,
    requiredTaxInclusion: profile.requiredTaxInclusion,
    requiredDiscountInclusion: profile.requiredDiscountInclusion,
    requiredFreightInclusion: profile.requiredFreightInclusion,
    requiredGrossNetBasis: profile.requiredGrossNetBasis,
    sourceProvenance: profile.sourceProvenance,
  };
}

export function CommercialQuotationRiskWorkspaceView({
  workspace: initialWorkspace,
  canManage,
}: CommercialQuotationRiskWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");

  const [assessmentDetail, setAssessmentDetail] = useState<CommercialQuotationRiskAssessmentDetail | null>(null);
  const [detailStatus, setDetailStatus] = useState<"idle" | "loading" | "ready" | "error">("idle");
  const [selectedDetailId, setSelectedDetailId] = useState<string | null>(null);

  const [trendsDetail, setTrendsDetail] = useState<CommercialQuotationRiskComparisonDetail | null>(null);
  const [trendsDetailStatus, setTrendsDetailStatus] = useState<"idle" | "loading" | "ready" | "error">("idle");

  const [selectedProfileId, setSelectedProfileId] = useState(initialWorkspace.profiles[0]?.id ?? "");
  const [isEditingProfile, setIsEditingProfile] = useState(false);
  const [profileEdit, setProfileEdit] = useState<ProfileEditState | null>(null);

  const assessmentFilters = useMemo(
    () => readQuotationRiskAssessmentFilters(searchParams),
    [searchParams],
  );

  const activeTab = useMemo<CommercialQuotationRiskTabId>(() => {
    const tab = searchParams.get("tab");
    return tab && VALID_TAB_IDS.has(tab as CommercialQuotationRiskTabId)
      ? (tab as CommercialQuotationRiskTabId)
      : "overview";
  }, [searchParams]);

  const selectedComparisonId = searchParams.get("comparison")
    ?? workspace.selectedTemporalComparisonId
    ?? workspace.selectedCustomerComparisonId;

  const temporalComparisons = useMemo(
    () => filterQuotationRiskTemporalComparisons(workspace.comparisons),
    [workspace.comparisons],
  );
  const customerComparisons = useMemo(
    () => filterQuotationRiskCustomerComparisons(workspace.comparisons),
    [workspace.comparisons],
  );

  const filteredAssessments = useMemo(
    () => filterQuotationRiskAssessments(workspace.assessments, assessmentFilters),
    [assessmentFilters, workspace.assessments],
  );

  const selectedProfile = workspace.profiles.find((profile) => profile.id === selectedProfileId)
    ?? workspace.profiles[0]
    ?? null;

  const selectedTemporalComparison = temporalComparisons.find((item) => item.id === selectedComparisonId)
    ?? temporalComparisons[0]
    ?? null;

  const selectedCustomerComparison = customerComparisons.find((item) => item.id === selectedComparisonId)
    ?? customerComparisons.find((item) => item.id === workspace.selectedCustomerComparisonId)
    ?? customerComparisons[0]
    ?? null;

  const riskDistributionMax = useMemo(
    () => Math.max(...workspace.riskDistribution.map((slice) => slice.count), 1),
    [workspace.riskDistribution],
  );

  const customerPreviewRows = useMemo(
    () => workspace.customerComparison.rows.slice(0, 5),
    [workspace.customerComparison.rows],
  );

  const setActiveTab = useCallback((tabId: CommercialQuotationRiskTabId) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", tabId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const refreshWorkspace = useCallback((
    comparisonId?: string | null,
    assessmentId?: string | null,
  ) => {
    startTransition(async () => {
      const result = await refreshCommercialQuotationRiskWorkspaceAction(assessmentId, comparisonId);
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

  const updateAssessmentFilters = useCallback((
    updates: Parameters<typeof buildQuotationRiskAssessmentFilterParams>[1],
  ) => {
    const params = buildQuotationRiskAssessmentFilterParams(searchParams, updates);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });

    if (Object.prototype.hasOwnProperty.call(updates, "assessmentOutcome")) {
      refreshWorkspace(selectedComparisonId, null);
    }
  }, [pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const clearAssessmentFilters = useCallback(() => {
    const params = clearQuotationRiskAssessmentFilterParams(searchParams);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    refreshWorkspace(selectedComparisonId, null);
  }, [pathname, refreshWorkspace, router, searchParams, selectedComparisonId]);

  const setSelectedComparison = useCallback((comparisonId: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("comparison", comparisonId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    startTransition(async () => {
      const result = await refreshCommercialQuotationRiskWorkspaceAction(null, comparisonId);
      if (result.status === "success") setWorkspace(result.workspace);
    });
  }, [pathname, router, searchParams]);

  const openAssessmentDetail = useCallback((assessmentId: string) => {
    setSelectedDetailId(assessmentId);
    setAssessmentDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialQuotationRiskAssessmentDetailAction(assessmentId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setAssessmentDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const loadTrendsComparisonDetail = useCallback((comparisonId: string) => {
    setTrendsDetail(null);
    setTrendsDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialQuotationRiskComparisonDetailAction(comparisonId);
      if (result.status === "failure") {
        setTrendsDetailStatus("error");
        return;
      }
      setTrendsDetail(result.detail);
      setTrendsDetailStatus("ready");
    })();
  }, []);

  const closeDetail = useCallback(() => {
    setSelectedDetailId(null);
    setAssessmentDetail(null);
    setDetailStatus("idle");
  }, []);

  useEffect(() => {
    if (!selectedDetailId) return undefined;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") closeDetail();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [closeDetail, selectedDetailId]);

  useEffect(() => {
    if (activeTab !== "trends" || !selectedTemporalComparison) return;
    if (trendsDetail?.summary.id === selectedTemporalComparison.id) return;
    const comparisonId = selectedTemporalComparison.id;
    queueMicrotask(() => {
      loadTrendsComparisonDetail(comparisonId);
    });
  }, [activeTab, loadTrendsComparisonDetail, selectedTemporalComparison, trendsDetail?.summary.id]);

  const onTabKeyDown = useCallback((event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const nextIndex = event.key === "ArrowRight"
      ? (index + 1) % COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS.length
      : (index - 1 + COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS.length) % COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS.length;
    setActiveTab(COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS[nextIndex].id);
  }, [setActiveTab]);

  const handleProfileSave = useCallback(() => {
    if (!selectedProfile || !profileEdit) return;
    startTransition(async () => {
      const result = await updateCommercialQuotationRiskProfileAction({
        profileId: selectedProfile.id,
        expectedVersion: selectedProfile.version,
        name: profileEdit.name.trim(),
        status: profileEdit.status,
        referenceWindowDurationDays: Number(profileEdit.referenceWindowDurationDays),
        minimumComparableObservations: Number(profileEdit.minimumComparableObservations),
        maximumNegativePriceVarianceRatio: Number(profileEdit.maximumNegativeVariancePercent) / 100,
        maximumPositivePriceVarianceRatio: Number(profileEdit.maximumPositiveVariancePercent) / 100,
        maximumPaymentTermDays: Number(profileEdit.maximumPaymentTermDays),
        expiryWarningDays: Number(profileEdit.expiryWarningDays),
        maximumEvidenceAgeDays: Number(profileEdit.maximumEvidenceAgeDays),
        allowedEvidenceStatuses: profileEdit.allowedEvidenceStatuses,
        allowedSalesEvidenceKinds: profileEdit.allowedSalesEvidenceKinds,
        requiredTaxInclusion: profileEdit.requiredTaxInclusion,
        requiredDiscountInclusion: profileEdit.requiredDiscountInclusion,
        requiredFreightInclusion: profileEdit.requiredFreightInclusion,
        requiredGrossNetBasis: profileEdit.requiredGrossNetBasis,
        sourceProvenance: profileEdit.sourceProvenance,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setIsEditingProfile(false);
      setProfileEdit(null);
      setFeedback(result.idempotentReplay ? "Profil güncellemesi zaten uygulanmıştı." : "Profil güncellendi.");
      setFeedbackTone("success");
      refreshWorkspace(selectedComparisonId, null);
    });
  }, [profileEdit, refreshWorkspace, selectedComparisonId, selectedProfile]);

  const beginProfileEdit = useCallback(() => {
    if (!selectedProfile) return;
    setProfileEdit(buildProfileEditState(selectedProfile));
    setIsEditingProfile(true);
  }, [selectedProfile]);

  const cancelProfileEdit = useCallback(() => {
    setIsEditingProfile(false);
    setProfileEdit(null);
  }, []);

  const renderTrendUnavailable = (comparison: typeof selectedTemporalComparison) => {
    if (!comparison) return "Zamansal risk karşılaştırması yok";
    if (comparison.comparisonOutcome === "source-conflict") return "Kaynak çakışması";
    if (comparison.comparisonOutcome === "insufficient-data") return "Karşılaştırma için yeterli veri yok";
    return null;
  };

  return (
    <main className="commint-page" data-testid="commercial-quotation-risk-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Teklif Riskleri</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_QUOTATION_RISK_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>{workspace.facilityLabel}</li>
              <li>Kanıt penceresi: {workspace.reportingPeriodLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Kanıt: {workspace.evidenceLabel}</li>
              <li>Kaynak: {COMMERCIAL_QUOTATION_RISK_EVIDENCE_SCOPE}</li>
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
        {!workspace.realQuotationMappingComplete ? (
          <p
            className="commint-provenance-strip"
            data-testid="commercial-quotation-risk-development-banner"
          >
            <span className="commint-provenance-strip__badge">Geliştirme</span>
            {COMMERCIAL_QUOTATION_RISK_DEVELOPMENT_BANNER}
          </p>
        ) : null}
        {!workspace.realQuotationMappingComplete ? (
          <p className="commint-readonly-note" data-testid="commercial-quotation-risk-mapping-note">
            {REAL_QUOTATION_MAPPING_NOTE}
          </p>
        ) : null}
        {!canManage ? (
          <p className="commint-readonly-note" data-testid="commercial-quotation-risk-readonly-note">
            {COMMERCIAL_QUOTATION_RISK_READONLY_NOTE}
          </p>
        ) : null}
        {feedback ? <p className={`commint-feedback commint-feedback--${feedbackTone}`}>{feedback}</p> : null}
      </header>

      <section
        className="commint-kpi-grid"
        data-testid="commercial-quotation-risk-kpi-grid"
        aria-label="Teklif risk göstergeleri"
      >
        {workspace.kpis.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id] ?? BarChart3;
          return (
            <article
              key={kpi.id}
              className={`commint-kpi-card ${resolveQuotationRiskKpiToneClass(kpi.tone)}`}
              data-kpi-id={kpi.id}
              aria-label={`${kpi.label}: ${kpi.value}`}
            >
              <div className="commint-kpi-card__header">
                <p className="commint-kpi-card__label">{kpi.label}</p>
                <span className="commint-kpi-card__icon" aria-hidden="true"><Icon size={16} /></span>
              </div>
              <p className="commint-kpi-card__value">{kpi.value}</p>
              <p className="commint-kpi-card__detail">{kpi.detail}</p>
              {"accessibleSummary" in kpi && kpi.accessibleSummary ? (
                <p className="commint-kpi-card__summary">{kpi.accessibleSummary}</p>
              ) : null}
            </article>
          );
        })}
      </section>

      <div
        className="commint-tab-rail"
        role="tablist"
        aria-label="Teklif risk sekmeleri"
        data-testid="commercial-quotation-risk-tab-rail"
      >
        {COMMERCIAL_QUOTATION_RISK_TAB_OPTIONS.map((tab, index) => (
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
        <div className="commint-panel-grid commint-panel-grid--overview" data-testid="commercial-quotation-risk-overview">
          <section className="commint-panel commint-panel--distribution" data-testid="commercial-quotation-risk-risk-distribution">
            <h2 className="commint-panel__title">Risk dağılımı</h2>
            {workspace.riskDistribution.some((slice) => slice.count > 0) ? (
              <div className="commint-chart" role="img" aria-label="Teklif risk dağılım grafiği">
                <div className="commint-chart__bars">
                  {workspace.riskDistribution.map((slice) => {
                    const width = slice.count > 0
                      ? `${Math.min(100, (slice.count / riskDistributionMax) * 100)}%`
                      : "0%";
                    return (
                      <div key={slice.key} className="commint-chart__row">
                        <span className="commint-chart__label">
                          <span className="commint-chart__primary">{slice.label}</span>
                          <span className="commint-chart__secondary">{formatCount(slice.count)} değerlendirme</span>
                        </span>
                        <span className="commint-chart__bar" title={slice.label}>
                          <span style={{ width }} />
                        </span>
                        <span>{formatCount(slice.count)}</span>
                      </div>
                    );
                  })}
                </div>
              </div>
            ) : (
              <p className="commint-empty">Gösterilecek risk dağılımı yok</p>
            )}
          </section>

          <section className="commint-panel commint-panel--temporal" data-testid="commercial-quotation-risk-temporal-trend">
            <h2 className="commint-panel__title">Zamansal risk eğilimi özeti</h2>
            {workspace.temporalTrend.unavailableReason ? (
              <p className="commint-empty">{workspace.temporalTrend.unavailableReason}</p>
            ) : (
              <>
                {workspace.temporalChart.points.length > 0 ? (
                  <div className="commint-chart" role="img" aria-label="Zamansal risk sıklığı grafiği">
                    <div className="commint-chart__bars">
                      {workspace.temporalChart.points.map((point) => {
                        const maxValue = Math.max(
                          point.currentMedian ?? 0,
                          point.referenceMedian ?? 0,
                          0.0001,
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
                            <span className="commint-chart__bar" title="Güncel risk sıklığı">
                              <span style={{ width: currentWidth }} />
                            </span>
                            <span className="commint-chart__bar" title="Referans risk sıklığı">
                              <span style={{ width: referenceWidth, background: "#16a34a" }} />
                            </span>
                            <span>{point.signedVarianceLabel}</span>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ) : (
                  <p className="commint-empty">{workspace.temporalChart.unavailableReason ?? "Karşılaştırma için yeterli veri yok"}</p>
                )}
                <div className="commint-detail-grid">
                  <DetailField label="Referans penceresi" value={workspace.temporalTrend.referenceWindowLabel} />
                  <DetailField label="Güncel pencere" value={workspace.temporalTrend.currentWindowLabel} />
                  <DetailField label="Referans risk sıklığı" value={workspace.temporalTrend.referenceIncidenceLabel} />
                  <DetailField label="Güncel risk sıklığı" value={workspace.temporalTrend.currentIncidenceLabel} />
                  <DetailField
                    label="Risk sıklığı değişimi"
                    value={workspace.temporalTrend.incidenceDeltaLabel === "Hesaplanamadı"
                      ? "Hesaplanamadı"
                      : `Risk sıklığı değişimi: ${workspace.temporalTrend.incidenceDeltaLabel}`}
                  />
                  <DetailField
                    label="Referans karşılaştırılabilir"
                    value={formatNullableCount(workspace.temporalTrend.referenceComparableCount)}
                  />
                  <DetailField
                    label="Güncel karşılaştırılabilir"
                    value={formatNullableCount(workspace.temporalTrend.currentComparableCount)}
                  />
                </div>
                {workspace.temporalTrend.comparisonReference ? (
                  <p className="commint-supplier-row__meta">
                    {workspace.temporalTrend.outcomeLabel} · {workspace.temporalTrend.leadingDriverLabel ?? "—"} · {workspace.temporalTrend.comparisonReference}
                  </p>
                ) : null}
              </>
            )}
          </section>

          <section className="commint-panel commint-panel--supplier" data-testid="commercial-quotation-risk-customer-preview">
            <div className="commint-panel__heading">
              <h2 className="commint-panel__title">Müşteri risk konumu önizlemesi</h2>
              {workspace.customerComparison.comparisonReference ? (
                <p className="commint-panel__trace">{workspace.customerComparison.comparisonReference}</p>
              ) : null}
            </div>
            {workspace.customerComparison.unavailableReason ? (
              <p className="commint-empty">{workspace.customerComparison.unavailableReason}</p>
            ) : customerPreviewRows.length > 0 ? (
              <div className="commint-table-wrap">
                <table className="commint-table">
                  <thead>
                    <tr>
                      <th>Sıra</th>
                      <th>Müşteri</th>
                      <th>Risk sıklığı</th>
                      <th>Konum</th>
                    </tr>
                  </thead>
                  <tbody>
                    {customerPreviewRows.map((row) => (
                      <tr key={row.id}>
                        <td>{row.riskRank ?? "—"}</td>
                        <td>
                          <span className="commint-panel__primary">{row.customerLabel}</span>
                          <span className="commint-panel__trace">{row.customerReference}</span>
                        </td>
                        <td>{row.riskIncidenceLabel}</td>
                        <td>
                          {row.positionStateLabel}
                          {row.isTied ? <span className="commint-badge commint-badge--tie">Eşit risk konumu</span> : null}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="commint-empty">Müşteri risk konumu verisi yok</p>
            )}
            <p className="commint-supplier-row__meta">{workspace.provenanceDisclaimer}</p>
          </section>
        </div>
      ) : null}

      {activeTab === "assessments" ? (
        <section className="commint-panel" data-testid="commercial-quotation-risk-assessments">
          <h2 className="commint-panel__title">Teklif risk değerlendirmeleri</h2>
          <QuotationRiskAssessmentFilterRail
            workspace={workspace}
            filters={assessmentFilters}
            onFilterChange={updateAssessmentFilters}
            onClearFilters={clearAssessmentFilters}
            isRefreshing={isPending}
          />
          <div className="commint-table-wrap">
            <table className="commint-table" data-testid="commercial-quotation-risk-assessments-table">
              <thead>
                <tr>
                  <th>Değerlendirme</th>
                  <th>Malzeme</th>
                  <th>Sonuç</th>
                  <th>Sapma</th>
                  <th>Kanıt</th>
                  <th>Sürücü</th>
                  <th>Tarih</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {filteredAssessments.length > 0 ? filteredAssessments.map((item) => (
                  <tr key={item.id} data-testid={`commercial-quotation-risk-assessment-row-${item.id}`}>
                    <td>
                      <span className="commint-panel__primary">{buildAssessmentTablePrimaryLabel(item)}</span>
                      <span className="commint-panel__trace">{buildAssessmentTableSecondaryLabel(item)}</span>
                    </td>
                    <td>{item.materialLabel}</td>
                    <td>{item.outcomeLabel}</td>
                    <td>{item.varianceLabel}</td>
                    <td>{formatAssessmentEvidenceLabel(item)}</td>
                    <td>{item.leadingDriverLabel}</td>
                    <td>{item.evaluatedLabel}</td>
                    <td>
                      <button
                        type="button"
                        className="commint-button"
                        onClick={() => openAssessmentDetail(item.id)}
                        data-testid={`commercial-quotation-risk-assessment-detail-${item.id}`}
                      >
                        Detay
                      </button>
                    </td>
                  </tr>
                )) : (
                  <tr>
                    <td colSpan={8}>
                      <p className="commint-empty" data-testid="commercial-quotation-risk-assessments-empty">
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

      {activeTab === "trends" ? (
        <section className="commint-panel" data-testid="commercial-quotation-risk-trends">
          <h2 className="commint-panel__title">Risk eğilimi karşılaştırmaları</h2>
          <div className="commint-toolbar">
            <select
              value={selectedTemporalComparison?.id ?? ""}
              onChange={(event) => {
                if (event.target.value) {
                  setSelectedComparison(event.target.value);
                  loadTrendsComparisonDetail(event.target.value);
                }
              }}
              aria-label="Zamansal karşılaştırma seç"
              data-testid="commercial-quotation-risk-trends-selector"
            >
              <option value="">Çalıştırma seçin</option>
              {temporalComparisons.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.comparisonTypeLabel} · {buildQuotationRiskComparisonSecondaryLabel(item)}
                </option>
              ))}
            </select>
          </div>
          <div className="commint-table-wrap">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Karşılaştırma</th>
                  <th>Sonuç</th>
                  <th>Referans sıklığı</th>
                  <th>Güncel sıklık</th>
                  <th>Değişim</th>
                  <th>Kanıt</th>
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
                    <td>{item.outcomeLabel}</td>
                    <td>{item.referenceIncidenceLabel}</td>
                    <td>{item.currentIncidenceLabel}</td>
                    <td>
                      {item.incidenceDeltaLabel === "Hesaplanamadı"
                        ? "Hesaplanamadı"
                        : `Risk sıklığı değişimi: ${item.incidenceDeltaLabel}`}
                    </td>
                    <td>
                      {formatCount(item.comparableAssessmentCount)} / {formatCount(item.excludedAssessmentCount)}
                    </td>
                    <td>
                      <button
                        type="button"
                        className="commint-button"
                        onClick={() => {
                          setSelectedComparison(item.id);
                          loadTrendsComparisonDetail(item.id);
                        }}
                      >
                        Detay
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {selectedTemporalComparison ? (
            <section className="commint-panel commint-panel--detail" data-testid="commercial-quotation-risk-trends-detail">
              <h3 className="commint-panel__title">Seçili eğilim detayı</h3>
              {renderTrendUnavailable(selectedTemporalComparison) ? (
                <p className="commint-empty">{renderTrendUnavailable(selectedTemporalComparison)}</p>
              ) : null}
              {trendsDetailStatus === "loading" ? (
                <div data-testid="commercial-quotation-risk-trends-detail-loading">
                  <div className="commint-skeleton-line commint-skeleton-line--title" />
                  <div className="commint-skeleton-line" />
                </div>
              ) : null}
              {trendsDetailStatus === "ready" && trendsDetail && trendsDetail.summary.id === selectedTemporalComparison.id ? (
                <QuotationRiskComparisonDetailPanel comparisonDetail={trendsDetail} />
              ) : null}
              {trendsDetailStatus === "error" ? (
                <p className="commint-empty" role="alert">Karşılaştırma detayı yüklenemedi.</p>
              ) : null}
            </section>
          ) : (
            <p className="commint-empty">Zamansal risk karşılaştırması yok</p>
          )}
        </section>
      ) : null}

      {activeTab === "customers" ? (
        <section className="commint-panel" data-testid="commercial-quotation-risk-customers">
          <h2 className="commint-panel__title">Müşteri risk konumu</h2>
          <div className="commint-toolbar">
            <select
              value={selectedCustomerComparison?.id ?? ""}
              onChange={(event) => {
                if (event.target.value) setSelectedComparison(event.target.value);
              }}
              aria-label="Müşteri karşılaştırması seç"
              data-testid="commercial-quotation-risk-customers-selector"
            >
              <option value="">Çalıştırma seçin</option>
              {customerComparisons.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.comparisonTypeLabel} · {buildQuotationRiskComparisonSecondaryLabel(item)}
                </option>
              ))}
            </select>
          </div>
          {workspace.customerComparison.unavailableReason ? (
            <p className="commint-empty">{workspace.customerComparison.unavailableReason}</p>
          ) : (
            <div className="commint-table-wrap">
              <table className="commint-table" data-testid="commercial-quotation-risk-customers-table">
                <thead>
                  <tr>
                    <th>Sıra</th>
                    <th>Müşteri referansı</th>
                    <th>Aday</th>
                    <th>Karşılaştırılabilir</th>
                    <th>Hariç tutulan</th>
                    <th>Risk sıklığı</th>
                    <th>Konum durumu</th>
                  </tr>
                </thead>
                <tbody>
                  {workspace.customerComparison.rows.map((row) => (
                    <tr key={row.id}>
                      <td>{row.riskRank ?? "—"}</td>
                      <td>
                        <span className="commint-panel__primary">{row.customerLabel}</span>
                        <span className="commint-panel__trace">{row.customerReference}</span>
                      </td>
                      <td>{formatCount(row.candidateAssessmentCount)}</td>
                      <td>{formatCount(row.comparableAssessmentCount)}</td>
                      <td>{formatCount(row.excludedAssessmentCount)}</td>
                      <td>{row.riskIncidenceLabel}</td>
                      <td>
                        {row.positionStateLabel}
                        {row.isTied ? (
                          <span className="commint-badge commint-badge--tie" data-testid={`commercial-quotation-risk-customer-tie-${row.id}`}>
                            Eşit risk konumu
                          </span>
                        ) : null}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {selectedCustomerComparison ? (
            <p className="commint-supplier-row__meta">
              {selectedCustomerComparison.outcomeLabel} · {selectedCustomerComparison.leadingDriverLabel} · {selectedCustomerComparison.evaluatedLabel}
            </p>
          ) : null}
        </section>
      ) : null}

      {activeTab === "profile" ? (
        <section className="commint-panel commint-profile-workspace" data-testid="commercial-quotation-risk-profile">
          <h2 className="commint-panel__title">Risk profili</h2>
          <div className="commint-profile-workspace__layout">
            <div className="commint-profile-rail" role="listbox" aria-label="Risk profilleri">
              {workspace.profiles.map((profile) => (
                <button
                  key={profile.id}
                  type="button"
                  role="option"
                  aria-selected={selectedProfile?.id === profile.id}
                  className={`commint-profile-rail__item${selectedProfile?.id === profile.id ? " commint-profile-rail__item--active" : ""}`}
                  onClick={() => {
                    setSelectedProfileId(profile.id);
                    setIsEditingProfile(false);
                    setProfileEdit(null);
                  }}
                >
                  <span>{profile.displayName}</span>
                  <span className="commint-supplier-row__meta">{profile.profileCode}</span>
                  <span className="commint-badge">{profile.statusLabel}</span>
                </button>
              ))}
            </div>
            {selectedProfile ? (
              <div data-testid="commercial-quotation-risk-profile-hero">
                <h3 className="commint-panel__title">{selectedProfile.displayName}</h3>
                <p className="commint-panel__hint">
                  {selectedProfile.profileCode} · Sürüm {selectedProfile.version} · {selectedProfile.ruleSetVersion}
                </p>
                {!isEditingProfile ? (
                  <>
                    <div className="commint-detail-grid">
                      <DetailField label="Durum" value={selectedProfile.statusLabel} />
                      <DetailField label="Referans pencere süresi" value={`${formatCount(selectedProfile.referenceWindowDurationDays)} gün`} />
                      <DetailField label="Minimum karşılaştırılabilir gözlem" value={formatCount(selectedProfile.minimumComparableObservations)} />
                      <DetailField label="Maksimum negatif sapma eşiği" value={selectedProfile.maximumNegativeVarianceLabel} />
                      <DetailField label="Maksimum pozitif sapma eşiği" value={selectedProfile.maximumPositiveVarianceLabel} />
                      <DetailField label="Maksimum ödeme vadesi" value={`${formatCount(selectedProfile.maximumPaymentTermDays)} gün`} />
                      <DetailField label="Geçerlilik uyarısı" value={`${formatCount(selectedProfile.expiryWarningDays)} gün`} />
                      <DetailField label="Maksimum kanıt yaşı" value={`${formatCount(selectedProfile.maximumEvidenceAgeDays)} gün`} />
                      <DetailField label="Maksimum risk sıklığı değişimi" value={selectedProfile.maximumRiskIncidenceDeltaLabel} />
                      <DetailField label="İzin verilen kanıt durumları" value={selectedProfile.allowedEvidenceLabels.join(", ") || "—"} />
                      <DetailField label="İzin verilen satış kanıt türleri" value={selectedProfile.allowedSourceEvidenceLabels.join(", ") || "—"} />
                      <DetailField label="Vergi bazı" value={selectedProfile.requiredTaxLabel} />
                      <DetailField label="İskonto bazı" value={selectedProfile.requiredDiscountLabel} />
                      <DetailField label="Navlun bazı" value={selectedProfile.requiredFreightLabel} />
                      <DetailField label="Brüt/net bazı" value={selectedProfile.requiredGrossNetLabel} />
                      <DetailField label="Köken" value={selectedProfile.sourceProvenance} />
                      <DetailField label="Uyumluluk" value={selectedProfile.compatibilitySummary} />
                    </div>
                    {canManage ? (
                      <button
                        type="button"
                        className="commint-button"
                        onClick={beginProfileEdit}
                        data-testid="commercial-quotation-risk-profile-edit"
                      >
                        Profili düzenle
                      </button>
                    ) : (
                      <p className="commint-readonly-note">{COMMERCIAL_QUOTATION_RISK_READONLY_NOTE}</p>
                    )}
                  </>
                ) : profileEdit ? (
                  <form
                    className="commint-composer__grid"
                    onSubmit={(event) => {
                      event.preventDefault();
                      handleProfileSave();
                    }}
                    data-testid="commercial-quotation-risk-profile-edit-form"
                  >
                    <label className="commint-field">
                      <span className="commint-field__label">Görünen ad</span>
                      <input
                        value={profileEdit.name}
                        onChange={(event) => setProfileEdit({ ...profileEdit, name: event.target.value })}
                        aria-label="Profil görünen adı"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Durum</span>
                      <select
                        value={profileEdit.status}
                        onChange={(event) => setProfileEdit({ ...profileEdit, status: event.target.value })}
                        aria-label="Profil durumu"
                      >
                        <option value="draft">Taslak</option>
                        <option value="approved">Onaylı</option>
                        <option value="inactive">Pasif</option>
                      </select>
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Referans pencere süresi (gün)</span>
                      <input
                        type="number"
                        min={1}
                        value={profileEdit.referenceWindowDurationDays}
                        onChange={(event) => setProfileEdit({ ...profileEdit, referenceWindowDurationDays: event.target.value })}
                        aria-label="Referans pencere süresi"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Minimum karşılaştırılabilir gözlem</span>
                      <input
                        type="number"
                        min={1}
                        value={profileEdit.minimumComparableObservations}
                        onChange={(event) => setProfileEdit({ ...profileEdit, minimumComparableObservations: event.target.value })}
                        aria-label="Minimum karşılaştırılabilir gözlem"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Maksimum negatif sapma eşiği (%)</span>
                      <input
                        type="number"
                        step="0.1"
                        value={profileEdit.maximumNegativeVariancePercent}
                        onChange={(event) => setProfileEdit({ ...profileEdit, maximumNegativeVariancePercent: event.target.value })}
                        aria-label="Maksimum negatif sapma eşiği yüzdesi"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Maksimum pozitif sapma eşiği (%)</span>
                      <input
                        type="number"
                        step="0.1"
                        value={profileEdit.maximumPositiveVariancePercent}
                        onChange={(event) => setProfileEdit({ ...profileEdit, maximumPositiveVariancePercent: event.target.value })}
                        aria-label="Maksimum pozitif sapma eşiği yüzdesi"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Maksimum ödeme vadesi (gün)</span>
                      <input
                        type="number"
                        min={0}
                        value={profileEdit.maximumPaymentTermDays}
                        onChange={(event) => setProfileEdit({ ...profileEdit, maximumPaymentTermDays: event.target.value })}
                        aria-label="Maksimum ödeme vadesi"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Geçerlilik uyarısı (gün)</span>
                      <input
                        type="number"
                        min={0}
                        value={profileEdit.expiryWarningDays}
                        onChange={(event) => setProfileEdit({ ...profileEdit, expiryWarningDays: event.target.value })}
                        aria-label="Geçerlilik uyarısı"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Maksimum kanıt yaşı (gün)</span>
                      <input
                        type="number"
                        min={0}
                        value={profileEdit.maximumEvidenceAgeDays}
                        onChange={(event) => setProfileEdit({ ...profileEdit, maximumEvidenceAgeDays: event.target.value })}
                        aria-label="Maksimum kanıt yaşı"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">İzin verilen kanıt durumları</span>
                      <input
                        value={profileEdit.allowedEvidenceStatuses}
                        onChange={(event) => setProfileEdit({ ...profileEdit, allowedEvidenceStatuses: event.target.value })}
                        aria-label="İzin verilen kanıt durumları"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">İzin verilen satış kanıt türleri</span>
                      <input
                        value={profileEdit.allowedSalesEvidenceKinds}
                        onChange={(event) => setProfileEdit({ ...profileEdit, allowedSalesEvidenceKinds: event.target.value })}
                        aria-label="İzin verilen satış kanıt türleri"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Vergi bazı</span>
                      <input
                        value={profileEdit.requiredTaxInclusion}
                        onChange={(event) => setProfileEdit({ ...profileEdit, requiredTaxInclusion: event.target.value })}
                        aria-label="Vergi bazı"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">İskonto bazı</span>
                      <input
                        value={profileEdit.requiredDiscountInclusion}
                        onChange={(event) => setProfileEdit({ ...profileEdit, requiredDiscountInclusion: event.target.value })}
                        aria-label="İskonto bazı"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Navlun bazı</span>
                      <input
                        value={profileEdit.requiredFreightInclusion}
                        onChange={(event) => setProfileEdit({ ...profileEdit, requiredFreightInclusion: event.target.value })}
                        aria-label="Navlun bazı"
                      />
                    </label>
                    <label className="commint-field">
                      <span className="commint-field__label">Brüt/net bazı</span>
                      <input
                        value={profileEdit.requiredGrossNetBasis}
                        onChange={(event) => setProfileEdit({ ...profileEdit, requiredGrossNetBasis: event.target.value })}
                        aria-label="Brüt/net bazı"
                      />
                    </label>
                    <label className="commint-field commint-field--wide">
                      <span className="commint-field__label">Köken</span>
                      <input
                        value={profileEdit.sourceProvenance}
                        onChange={(event) => setProfileEdit({ ...profileEdit, sourceProvenance: event.target.value })}
                        aria-label="Köken"
                      />
                    </label>
                    <div className="commint-composer__actions">
                      <button type="submit" className="commint-button commint-button--primary" disabled={isPending}>
                        {isPending ? <LoaderCircle className="animate-spin" size={14} /> : null}
                        Kaydet
                      </button>
                      <button type="button" className="commint-button" onClick={cancelProfileEdit}>
                        İptal
                      </button>
                    </div>
                  </form>
                ) : null}
              </div>
            ) : (
              <p className="commint-empty">Profil bulunamadı</p>
            )}
          </div>
        </section>
      ) : null}

      {selectedDetailId ? (
        <>
          <button type="button" className="commint-drawer-backdrop" aria-label="Detayı kapat" onClick={closeDetail} />
          <aside
            className="commint-drawer"
            aria-label="Değerlendirme detayı"
            data-testid="commercial-quotation-risk-assessment-drawer"
          >
            <div className="commint-drawer__header">
              <h2>
                {assessmentDetail?.summary.quotationReference
                  ?? assessmentDetail?.summary.assessmentReference
                  ?? "Değerlendirme detayı"}
              </h2>
              <p className="commint-supplier-row__meta">
                {[
                  assessmentDetail?.summary.materialLabel,
                  assessmentDetail?.summary.assessmentReference,
                ].filter((value) => value && value !== "—").join(" · ") || "—"}
              </p>
            </div>
            <div className="commint-drawer__body">
              {detailStatus === "loading" ? (
                <div data-testid="commercial-quotation-risk-assessment-detail-loading">
                  <div className="commint-skeleton-line commint-skeleton-line--title" />
                  <div className="commint-skeleton-line" />
                </div>
              ) : null}
              {detailStatus === "ready" && assessmentDetail ? (
                <QuotationRiskAssessmentDetailDrawerBody assessmentDetail={assessmentDetail} />
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

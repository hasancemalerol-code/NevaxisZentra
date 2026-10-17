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
  UsersRound,
} from "lucide-react";

import {
  buildCommercialIntelligenceComparisonPrimaryLabel,
  buildCommercialIntelligenceComparisonSecondaryLabel,
  COMMERCIAL_INTELLIGENCE_PILOT_MATERIAL_ID,
  resolveCommercialIntelligenceKpiToneClass,
} from "./commercial-intelligence-presentation";
import { CommercialIntelligenceSupplierRow } from "./commercial-intelligence-supplier-row";
import { formatCount, formatCurrency } from "../domain/commercial-intelligence-formatters";
import {
  COMMERCIAL_INTELLIGENCE_DEVELOPMENT_BANNER,
  COMMERCIAL_INTELLIGENCE_READONLY_NOTE,
  COMMERCIAL_SUPPLIER_COMPARISON_EVIDENCE_SCOPE,
  COMMERCIAL_SUPPLIER_COMPARISON_SUBTITLE,
  COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS,
} from "../domain/commercial-intelligence-labels";
import type {
  CommercialIntelligenceComparisonDetail,
  CommercialSupplierComparisonTabId,
  CommercialSupplierComparisonWorkspacePayload,
} from "../types/commercial-intelligence-types";
import {
  evaluateCommercialIntelligenceComparisonAction,
  loadCommercialIntelligenceComparisonDetailAction,
  refreshCommercialSupplierComparisonWorkspaceAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";
import { localizeCommercialSalesPriceProvenance } from "../domain/commercial-intelligence-labels";
import { kpiValueClass, detailValueClass } from "./commercial-no-data";
import { CommercialKpiCompact, isKpiSetEmpty } from "./commercial-kpi-compact";

type CommercialSupplierComparisonWorkspaceViewProps = Readonly<{
  workspace: CommercialSupplierComparisonWorkspacePayload;
  canManage: boolean;
}>;

const VALID_TAB_IDS = new Set(
  COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS.map((tab) => tab.id),
);

const KPI_ICONS: Record<string, typeof BarChart3> = {
  "comparable-suppliers": UsersRound,
  "comparable-observations": BarChart3,
  "excluded-observations": AlertTriangle,
  "price-spread": Scale,
  "tied-positions": UsersRound,
  "attention-evidence-issues": AlertTriangle,
};

export function CommercialSupplierComparisonWorkspaceView({
  workspace: initialWorkspace,
  canManage,
}: CommercialSupplierComparisonWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");
  const [comparisonDetail, setComparisonDetail] = useState<CommercialIntelligenceComparisonDetail | null>(null);
  const [detailStatus, setDetailStatus] = useState<"idle" | "loading" | "ready" | "error">("idle");
  const [selectedDetailId, setSelectedDetailId] = useState<string | null>(null);
  const [evalProfileId, setEvalProfileId] = useState("");
  const [evalWindowStart, setEvalWindowStart] = useState("");
  const [evalWindowEnd, setEvalWindowEnd] = useState("");

  const approvedProfiles = useMemo(
    () => workspace.profiles.filter((profile) => profile.isExecutable),
    [workspace.profiles],
  );
  const effectiveEvalProfileId = evalProfileId || approvedProfiles[0]?.id || "";
  const selectedProfile = approvedProfiles.find((profile) => profile.id === effectiveEvalProfileId) ?? null;

  const activeTab = useMemo<CommercialSupplierComparisonTabId>(() => {
    const tab = searchParams.get("tab");
    return tab && VALID_TAB_IDS.has(tab as CommercialSupplierComparisonTabId)
      ? (tab as CommercialSupplierComparisonTabId)
      : "overview";
  }, [searchParams]);

  const selectedComparisonId = searchParams.get("comparison") ?? workspace.selectedComparisonId;

  const setActiveTab = useCallback((tabId: CommercialSupplierComparisonTabId) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", tabId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const setSelectedComparison = useCallback((comparisonId: string) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("comparison", comparisonId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    startTransition(async () => {
      const result = await refreshCommercialSupplierComparisonWorkspaceAction(comparisonId);
      if (result.status === "success") {
        setWorkspace(result.workspace);
      }
    });
  }, [pathname, router, searchParams]);

  const refreshWorkspace = useCallback(() => {
    startTransition(async () => {
      const result = await refreshCommercialSupplierComparisonWorkspaceAction(selectedComparisonId);
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setWorkspace(result.workspace);
      setFeedback("Çalışma alanı yenilendi.");
      setFeedbackTone("success");
    });
  }, [selectedComparisonId]);

  const openComparisonDetail = useCallback((comparisonId: string) => {
    setSelectedDetailId(comparisonId);
    setComparisonDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialIntelligenceComparisonDetailAction(comparisonId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setComparisonDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const closeComparisonDetail = useCallback(() => {
    setSelectedDetailId(null);
    setComparisonDetail(null);
    setDetailStatus("idle");
  }, []);

  const handleEvaluateComparison = useCallback(() => {
    const profile = approvedProfiles.find((item) => item.id === effectiveEvalProfileId);
    if (!profile || !evalWindowStart || !evalWindowEnd) return;

    startTransition(async () => {
      const result = await evaluateCommercialIntelligenceComparisonAction({
        profileId: profile.id,
        expectedProfileVersion: profile.version,
        comparisonType: "supplier-price-position",
        materialId: COMMERCIAL_INTELLIGENCE_PILOT_MATERIAL_ID,
        currentWindowStartUtc: new Date(evalWindowStart).toISOString(),
        currentWindowEndUtc: new Date(evalWindowEnd).toISOString(),
        referenceWindowStartUtc: null,
        referenceWindowEndUtc: null,
        currencyCode: "TRY",
        normalizedUnit: "kg",
        priceBasisTaxInclusion: profile.requiredTaxInclusion,
        priceBasisDiscountInclusion: profile.requiredDiscountInclusion,
        priceBasisFreightInclusion: profile.requiredFreightInclusion,
        sourceProvenance: profile.sourceProvenance,
        idempotencyKey: `COMMINT-SUPPLIER-UI-${Date.now()}`,
      });

      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }

      const refresh = await refreshCommercialSupplierComparisonWorkspaceAction(result.entityId);
      if (refresh.status === "success") {
        setWorkspace(refresh.workspace);
        const params = new URLSearchParams(searchParams.toString());
        params.set("comparison", result.entityId);
        router.replace(`${pathname}?${params.toString()}`, { scroll: false });
      }
      setFeedback(result.idempotentReplay ? "Mevcut karşılaştırma yeniden kullanıldı." : "Tedarikçi karşılaştırması oluşturuldu.");
      setFeedbackTone("success");
    });
  }, [
    approvedProfiles,
    effectiveEvalProfileId,
    evalWindowEnd,
    evalWindowStart,
    pathname,
    router,
    searchParams,
  ]);

  const onTabKeyDown = useCallback((event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const nextIndex = event.key === "ArrowRight"
      ? (index + 1) % COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS.length
      : (index - 1 + COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS.length) % COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS.length;
    setActiveTab(COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS[nextIndex].id);
  }, [setActiveTab]);

  const spreadMedians = useMemo(() => {
    const medians = workspace.supplierPosition.rows
      .map((row) => row.supplierMedianUnitPrice)
      .filter((value): value is number => value !== null && !Number.isNaN(value));
    if (medians.length === 0) return { lowest: null, highest: null };
    return { lowest: Math.min(...medians), highest: Math.max(...medians) };
  }, [workspace.supplierPosition.rows]);

  const selectedComparison = useMemo(
    () => workspace.comparisons.find((item) => item.id === workspace.selectedComparisonId) ?? null,
    [workspace.comparisons, workspace.selectedComparisonId],
  );

  return (
    <div className="commint-page" data-testid="commercial-supplier-comparison-workspace">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Tedarikçi Karşılaştırması</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_SUPPLIER_COMPARISON_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>Kapsam: {workspace.facilityLabel}</li>
              <li>Kanıt penceresi: {workspace.reportingPeriodLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Kanıt: {workspace.evidenceLabel}</li>
              <li>Kaynak: {COMMERCIAL_SUPPLIER_COMPARISON_EVIDENCE_SCOPE}</li>
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
        <details className="commint-notes">
          <summary>Kapsam ve sınırlar</summary>
          {workspace.hasDevelopmentPilotData ? (
            <p className="commint-provenance-strip" data-testid="commercial-supplier-comparison-provenance-strip">
              <span className="commint-provenance-strip__badge">Örnek veri</span>
              {COMMERCIAL_INTELLIGENCE_DEVELOPMENT_BANNER}
            </p>
          ) : null}
          {!canManage ? (
            <p className="commint-readonly-note" data-testid="commercial-supplier-comparison-readonly-note">
              {COMMERCIAL_INTELLIGENCE_READONLY_NOTE}
            </p>
          ) : null}
        </details>
        {feedback ? <p className={`commint-feedback commint-feedback--${feedbackTone}`}>{feedback}</p> : null}
      </header>

      {isKpiSetEmpty(workspace.kpis) ? (
        <CommercialKpiCompact
          ariaLabel="Tedarikçi karşılaştırma göstergeleri"
          testId="commercial-supplier-comparison-kpi-grid"
          items={workspace.kpis.map((kpi) => ({ id: kpi.id, label: kpi.label, value: kpi.value, caption: kpi.detail }))}
        />
      ) : (
        <section
          className="commint-kpi-grid"
          data-testid="commercial-supplier-comparison-kpi-grid"
          aria-label="Tedarikçi karşılaştırma göstergeleri"
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
                <p className={kpiValueClass(kpi.value)}>{kpi.value}</p>
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
      )}

      <div
        className="commint-tab-rail"
        role="tablist"
        aria-label="Tedarikçi karşılaştırma sekmeleri"
        data-testid="commercial-supplier-comparison-tab-rail"
      >
        {COMMERCIAL_SUPPLIER_COMPARISON_TAB_OPTIONS.map((tab, index) => (
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
        <div className="commint-panel-grid commint-panel-grid--supplier-comparison">
          <section className="commint-panel commint-panel--supplier" data-testid="commercial-supplier-comparison-ranking-overview">
            <div className="commint-panel__heading">
              <h2 className="commint-panel__title">Güncel tedarikçi fiyat sıralaması</h2>
              {workspace.supplierPosition.materialLabel ? (
                <p className="commint-panel__subtitle">
                  <span className="commint-panel__primary">{workspace.supplierPosition.materialLabel}</span>
                  <span className="commint-panel__secondary">{workspace.supplierPosition.secondaryLabel}</span>
                </p>
              ) : null}
            </div>
            {workspace.supplierPosition.rows.length > 0 ? (
              <div className="commint-supplier-list" data-testid="commercial-supplier-comparison-overview-rows">
                <p className="commint-supplier-context">
                  {workspace.supplierPosition.outcomeLabel} ·
                  Pencere: {selectedComparison?.currentWindowLabel ?? "—"} ·
                  {workspace.supplierPosition.evaluatedLabel}
                </p>
                {workspace.supplierPosition.rows.map((row) => (
                  <CommercialIntelligenceSupplierRow
                    key={row.id}
                    row={row}
                    currencyCode={workspace.supplierPosition.currencyCode}
                    normalizedUnit={workspace.supplierPosition.normalizedUnit}
                  />
                ))}
              </div>
            ) : (
              <p className="commint-empty">{workspace.supplierPosition.unavailableReason}</p>
            )}
          </section>

          <section className="commint-panel commint-panel--spread" data-testid="commercial-supplier-comparison-spread-panel">
            <h2 className="commint-panel__title">Fiyat yayılımı ve kanıt</h2>
            <div className="commint-spread-panel">
              {spreadMedians.lowest !== null && spreadMedians.highest !== null ? (
              <div className="commint-spread-meter" aria-hidden="true">
                <span className="commint-spread-meter__low">
                  {spreadMedians.lowest !== null && workspace.supplierPosition.currencyCode
                    ? formatCurrency(spreadMedians.lowest, workspace.supplierPosition.currencyCode)
                    : "—"}
                </span>
                <span className="commint-spread-meter__bar" />
                <span className="commint-spread-meter__high">
                  {spreadMedians.highest !== null && workspace.supplierPosition.currencyCode
                    ? formatCurrency(spreadMedians.highest, workspace.supplierPosition.currencyCode)
                    : "—"}
                </span>
              </div>
              ) : null}
              <ul className="commint-keyvalue commint-keyvalue--compact">
                <li><span>Yayılım oranı</span><span className={detailValueClass(workspace.supplierPosition.spreadLabel)}>{workspace.supplierPosition.spreadLabel}</span></li>
                <li><span>Profil eşiği</span><span className={detailValueClass(workspace.supplierSpreadThresholdLabel)}>{workspace.supplierSpreadThresholdLabel}</span></li>
                <li><span>Sonuç</span><span className={detailValueClass(workspace.supplierPosition.outcomeLabel ?? "—")}>{workspace.supplierPosition.outcomeLabel ?? "—"}</span></li>
                <li><span>Kanıt durumu</span><span className={detailValueClass(workspace.evidenceLabel)}>{workspace.evidenceLabel}</span></li>
                <li><span>Dahil / dışlanan</span><span className={detailValueClass(`${formatCount(workspace.supplierPosition.comparableObservationCount)} / ${formatCount(workspace.supplierPosition.excludedObservationCount)}`)}>{`${formatCount(workspace.supplierPosition.comparableObservationCount)} / ${formatCount(workspace.supplierPosition.excludedObservationCount)}`}</span></li>
                <li><span>Başlıca etken</span><span className={detailValueClass(workspace.supplierPosition.leadingDriverLabel ?? "—")}>{workspace.supplierPosition.leadingDriverLabel ?? "—"}</span></li>
              </ul>
              {workspace.supplierPosition.rows.length > 0 ? (
                <p className="commint-supplier-row__meta">{localizeCommercialSalesPriceProvenance(workspace.provenanceDisclaimer)}</p>
              ) : null}
            </div>
          </section>
        </div>
      ) : null}

      {activeTab === "ranking" ? (
        <section className="commint-panel" data-testid="commercial-supplier-comparison-ranking-workspace">
          <h2 className="commint-panel__title">Tedarikçi sıralaması</h2>
          <div className="commint-toolbar">
            <select
              value={workspace.selectedComparisonId ?? ""}
              onChange={(event) => setSelectedComparison(event.target.value)}
              aria-label="Karşılaştırma çalışması seç"
            >
              <option value="">Çalıştırma seçin</option>
              {workspace.comparisons.map((item) => (
                <option key={item.id} value={item.id}>
                  {buildCommercialIntelligenceComparisonPrimaryLabel(item)} · {buildCommercialIntelligenceComparisonSecondaryLabel(item)}
                </option>
              ))}
            </select>
          </div>
          <div className="commint-supplier-list" data-testid="commercial-supplier-comparison-ranking">
            {workspace.supplierPosition.rows.length > 0 ? workspace.supplierPosition.rows.map((row) => (
              <CommercialIntelligenceSupplierRow
                key={row.id}
                row={row}
                currencyCode={workspace.supplierPosition.currencyCode}
                normalizedUnit={workspace.supplierPosition.normalizedUnit}
                showExcludedCount
                excludedObservationCount={workspace.supplierPosition.excludedObservationCount}
              />
            )) : <p className="commint-empty">Tedarikçi karşılaştırma sonucu yok</p>}
          </div>
          {canManage ? (
            <section className="commint-composer commint-composer--comparison" data-testid="commercial-supplier-comparison-evaluation-controls">
              <header className="commint-composer__header">
                <div>
                  <h3 className="commint-composer__title">Yeni tedarikçi karşılaştırması</h3>
                  <p className="commint-composer__hint">Uyumlu kanıt penceresi ile tedarikçi fiyat konumu çalıştırın.</p>
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
                  onClick={handleEvaluateComparison}
                  disabled={isPending || !effectiveEvalProfileId || !evalWindowStart || !evalWindowEnd}
                >
                  {isPending ? <LoaderCircle className="animate-spin" size={14} /> : null}
                  Karşılaştırmayı çalıştır
                </button>
              </div>
            </section>
          ) : null}
        </section>
      ) : null}

      {activeTab === "history" ? (
        <section className="commint-panel" data-testid="commercial-supplier-comparison-history-workspace">
          <h2 className="commint-panel__title">Karşılaştırma geçmişi</h2>
          <div className="commint-table-wrap" tabIndex={0}>
            <table aria-label="Karşılaştırma geçmişi" className="commint-table">
              <thead>
                <tr>
                  <th>Analiz</th>
                  <th>Malzeme</th>
                  <th>Sonuç</th>
                  <th>Yayılım</th>
                  <th>Kanıt</th>
                  <th>Değerlendirme</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {workspace.comparisons.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <span className="commint-panel__primary">{buildCommercialIntelligenceComparisonPrimaryLabel(item)}</span>
                      <span className="commint-panel__trace">{item.comparisonReference}</span>
                    </td>
                    <td>{item.materialLabel}</td>
                    <td>{item.outcomeLabel}</td>
                    <td>{item.supplierSpreadLabel}</td>
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
        </section>
      ) : null}

      {selectedDetailId ? (
        <>
          <button type="button" className="commint-drawer-backdrop" aria-label="Detayı kapat" onClick={closeComparisonDetail} />
          <aside className="commint-drawer" aria-label="Karşılaştırma detayı">
            <div className="commint-drawer__header">
              <h2>{comparisonDetail?.summary.materialLabel ?? "Karşılaştırma detayı"}</h2>
              <p className="commint-supplier-row__meta">{comparisonDetail?.summary.comparisonReference ?? "—"}</p>
            </div>
            <div className="commint-drawer__body">
              {detailStatus === "loading" ? (
                <div data-testid="commercial-supplier-comparison-detail-loading">
                  <div className="commint-skeleton-line commint-skeleton-line--title" />
                  <div className="commint-skeleton-line" />
                </div>
              ) : null}
              {detailStatus === "ready" && comparisonDetail ? (
                <div data-testid="commercial-supplier-comparison-detail-ready">
                  <div className="commint-detail-grid">
                    <DetailField label="Sonuç" value={comparisonDetail.summary.outcomeLabel} />
                    <DetailField label="Pencere" value={comparisonDetail.summary.currentWindowLabel} />
                    <DetailField label="Yayılım" value={comparisonDetail.summary.supplierSpreadLabel} />
                    <DetailField label="Karşılaştırılabilir" value={formatCount(comparisonDetail.summary.comparableObservationCount)} />
                    <DetailField label="Dışlanan" value={formatCount(comparisonDetail.summary.excludedObservationCount)} />
                  </div>
                  <div className="commint-supplier-list">
                    {comparisonDetail.supplierResults.map((row) => (
                      <CommercialIntelligenceSupplierRow
                        key={row.id}
                        row={row}
                        currencyCode={comparisonDetail.summary.currencyCode}
                        normalizedUnit={comparisonDetail.summary.normalizedUnit}
                      />
                    ))}
                  </div>
                </div>
              ) : null}
            </div>
          </aside>
        </>
      ) : null}
    </div>
  );
}

function DetailField({ label, value }: Readonly<{ label: string; value: string }>) {
  return (
    <div className="commint-detail-field">
      <span className="commint-detail-field__label">{label}</span>
      <span className={detailValueClass(value)}>{value}</span>
    </div>
  );
}

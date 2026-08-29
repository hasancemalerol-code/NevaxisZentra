"use client";

import Link from "next/link";
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
  Ban,
  CheckCircle2,
  ClipboardList,
  FilePlus2,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  ShieldCheck,
} from "lucide-react";

import {
  buildCommercialPricingScenarioFilterParams,
  clearCommercialPricingScenarioFilterParams,
  readCommercialPricingScenarioFilters,
  toCommercialPricingScenarioApiFilters,
} from "../application/commercial-pricing-scenario-filter";
import {
  COMMERCIAL_PRICING_SCENARIO_AUTOMATION_DISCLAIMER,
  COMMERCIAL_PRICING_SCENARIO_EMPTY_NOTE,
  COMMERCIAL_PRICING_SCENARIO_NO_SELECTION_NOTE,
  COMMERCIAL_PRICING_SCENARIO_PILOT_PROFILE_ID,
  COMMERCIAL_PRICING_SCENARIO_PILOT_SALES_EVIDENCE_BELOW_ID,
  COMMERCIAL_PRICING_SCENARIO_PILOT_SALES_EVIDENCE_WITHIN_ID,
  COMMERCIAL_PRICING_SCENARIO_SCOPE_NOTE,
  COMMERCIAL_PRICING_SCENARIO_SUBTITLE,
} from "../domain/commercial-pricing-scenario-labels";
import { COMMERCIAL_PROFITABILITY_READONLY_NOTE } from "../domain/commercial-profitability-labels";
import type {
  CommercialPricingScenarioDetail,
  CommercialPricingScenarioEvaluationSnapshot,
  CommercialPricingScenarioTableRow,
  CommercialPricingScenarioWorkspacePayload,
} from "../types/commercial-pricing-scenario-types";
import { resolveProfitabilityKpiToneClass } from "./commercial-profitability-presentation";
import {
  createCommercialPricingScenarioAction,
  createCommercialPricingScenarioTaskDraftAction,
  evaluateCommercialPricingScenarioAction,
  loadCommercialPricingScenarioDetailAction,
  refreshCommercialPricingScenarioWorkspaceAction,
  transitionCommercialPricingScenarioAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";

type CommercialPricingScenarioWorkspaceViewProps = Readonly<{
  workspace: CommercialPricingScenarioWorkspacePayload;
  initialScenarioDetail?: CommercialPricingScenarioDetail | null;
  initialDetailStatus?: "idle" | "ready" | "error";
}>;

const KPI_ICONS: Record<string, typeof ShieldCheck> = {
  "scenario-draft": FilePlus2,
  "scenario-needs-review": AlertTriangle,
  "scenario-eligible": ShieldCheck,
  "scenario-blocked": Ban,
  "scenario-approved": CheckCircle2,
};

const DEFAULT_SOURCE_PROVENANCE =
  "Deterministic local Development pilot — pricing-scenario UI";

function guardrailToneClass(guardrail: string | null): string {
  if (guardrail === "blocked") return "commint-variance--negative";
  if (guardrail === "passes") return "commint-variance--positive";
  if (guardrail === "needs-review") return "commint-variance--warning";
  return "commint-variance--neutral";
}

type ScenarioFilterRailProps = Readonly<{
  filters: ReturnType<typeof readCommercialPricingScenarioFilters>;
  onFilterChange: (updates: Partial<Record<string, string>>) => void;
  onClear: () => void;
}>;

function CommercialPricingScenarioFilterRail({
  filters,
  onFilterChange,
  onClear,
}: ScenarioFilterRailProps) {
  return (
    <div className="commint-filter-rail" data-testid="commercial-pricing-scenario-filter-rail">
      <label className="commint-filter-field">
        <span>Yaşam döngüsü</span>
        <select
          value={filters.lifecycleStatus}
          onChange={(event) => onFilterChange({ lifecycleStatus: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="draft">Taslak</option>
          <option value="needs-review">İnceleme gerekli</option>
          <option value="eligible-for-approval">Onaya uygun</option>
          <option value="blocked">Engellendi</option>
          <option value="insufficient-data">Yetersiz veri</option>
          <option value="submitted-for-approval">Onaya gönderildi</option>
          <option value="approved">Onaylandı</option>
          <option value="rejected">Reddedildi</option>
          <option value="withdrawn">Geri çekildi</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Koruma sonucu</span>
        <select
          value={filters.guardrailResult}
          onChange={(event) => onFilterChange({ guardrailResult: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="passes">Geçti</option>
          <option value="needs-review">İnceleme gerekli</option>
          <option value="blocked">Engellendi</option>
          <option value="insufficient-data">Yetersiz veri</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Müşteri referansı</span>
        <input
          type="text"
          value={filters.customerReference}
          placeholder="Filtrele…"
          onChange={(event) => onFilterChange({ customerReference: event.target.value, page: "1" })}
        />
      </label>
      <button type="button" className="commint-button commint-button--ghost" onClick={onClear}>
        Filtreleri temizle
      </button>
    </div>
  );
}

type ScenarioTableProps = Readonly<{
  rows: readonly CommercialPricingScenarioTableRow[];
  selectedScenarioId: string | null;
  onSelect: (scenarioId: string) => void;
}>;

function CommercialPricingScenarioTable({
  rows,
  selectedScenarioId,
  onSelect,
}: ScenarioTableProps) {
  return (
    <div className="commint-table-wrap" data-testid="commercial-pricing-scenario-table">
      <table className="commint-table">
        <thead>
          <tr>
            <th>Referans</th>
            <th>Yaşam döngüsü</th>
            <th>Koruma</th>
            <th>Önerilen fiyat</th>
            <th>Referans fiyat</th>
            <th>Müşteri</th>
            <th>Malzeme</th>
            <th>Değerlendirme</th>
            <th>Güncellendi</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={row.id}
              className={selectedScenarioId === row.id ? "commint-table__row--selected" : undefined}
              data-testid={`commercial-pricing-scenario-row-${row.id}`}
            >
              <td>
                <button
                  type="button"
                  className="commint-table__link"
                  onClick={() => onSelect(row.id)}
                  aria-pressed={selectedScenarioId === row.id}
                >
                  {row.scenarioReference}
                </button>
              </td>
              <td>{row.lifecycleLabel}</td>
              <td className={guardrailToneClass(row.guardrailResult)}>{row.guardrailLabel}</td>
              <td>{row.proposedPriceLabel}</td>
              <td>{row.baselinePriceLabel}</td>
              <td>{row.customerReference ?? "—"}</td>
              <td>{row.materialLabel}</td>
              <td>{row.evaluationCount}</td>
              <td>{row.updatedLabel}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type EvaluationDetailPanelProps = Readonly<{
  evaluation: CommercialPricingScenarioEvaluationSnapshot;
}>;

function CommercialPricingScenarioEvaluationDetailPanel({ evaluation }: EvaluationDetailPanelProps) {
  return (
    <div className="commint-detail-grid" data-testid="commercial-pricing-scenario-evaluation-detail">
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Koruma sonucu</p>
        <p className="commint-detail-field__value">{evaluation.guardrailResultLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Değerlendirme sonucu</p>
        <p className="commint-detail-field__value">{evaluation.assessmentOutcomeLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Kanıt durumu</p>
        <p className="commint-detail-field__value">{evaluation.evidenceStatusLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Maliyet kapsamı</p>
        <p className="commint-detail-field__value">{evaluation.costCoverageStatusLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Önerilen birim fiyat</p>
        <p className="commint-detail-field__value">{evaluation.proposedUnitPriceLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Referans birim fiyat</p>
        <p className="commint-detail-field__value">{evaluation.baselineUnitPriceLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Birim katkı</p>
        <p className="commint-detail-field__value">{evaluation.expectedUnitContributionLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Katkı marjı</p>
        <p className="commint-detail-field__value">{evaluation.expectedContributionMarginRatioLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Marj farkı</p>
        <p className="commint-detail-field__value">{evaluation.contributionMarginDeltaLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Profil minimum marjı</p>
        <p className="commint-detail-field__value">{evaluation.profileMinimumContributionMarginRatioLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Önde gelen sürücü</p>
        <p className="commint-detail-field__value">{evaluation.leadingDriverLabel}</p>
      </div>
      <div className="commint-detail-field">
        <p className="commint-detail-field__label">Değerlendirme zamanı</p>
        <p className="commint-detail-field__value">{evaluation.evaluatedLabel}</p>
      </div>
      {evaluation.drivers.length > 0 ? (
        <div className="commint-detail-field" style={{ gridColumn: "1 / -1" }}>
          <p className="commint-detail-field__label">Sürücüler</p>
          <ul className="commint-driver-list">
            {evaluation.drivers.map((driver) => (
              <li key={`${driver.reasonCode}-${driver.supportingText}`}>
                <strong>{driver.reasonLabel}</strong>
                {" · "}
                {driver.severityLabel}
                {driver.normalizedVarianceLabel !== "—" ? ` · ${driver.normalizedVarianceLabel}` : ""}
                {driver.supportingText ? ` — ${driver.supportingText}` : ""}
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}

export function CommercialPricingScenarioWorkspaceView({
  workspace: initialWorkspace,
  initialScenarioDetail = null,
  initialDetailStatus = "idle",
}: CommercialPricingScenarioWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [isCreatingTask, setIsCreatingTask] = useState(false);
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");
  const [scenarioDetail, setScenarioDetail] = useState<CommercialPricingScenarioDetail | null>(initialScenarioDetail);
  const [detailStatus, setDetailStatus] = useState<"idle" | "loading" | "ready" | "error">(
    initialDetailStatus === "ready" && initialScenarioDetail ? "ready" : initialDetailStatus,
  );
  const [rejectNote, setRejectNote] = useState("");
  const [isHydrated, setIsHydrated] = useState(false);
  const [createForm, setCreateForm] = useState({
    profileId: COMMERCIAL_PRICING_SCENARIO_PILOT_PROFILE_ID,
    expectedProfileVersion: "1",
    salesEvidenceObservationId: COMMERCIAL_PRICING_SCENARIO_PILOT_SALES_EVIDENCE_WITHIN_ID,
    proposedUnitPrice: "125.00",
    proposedDiscountRatio: "",
    quantity: "1000",
    commercialRationale: "Pilot fiyat koruma senaryosu — eşik içi satış kanıtı ile değerlendirme.",
    sourceProvenance: DEFAULT_SOURCE_PROVENANCE,
  });

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  const filters = useMemo(
    () => readCommercialPricingScenarioFilters(searchParams),
    [searchParams],
  );

  const selectedScenarioId = searchParams.get("scenario") ?? workspace.selectedScenarioId;
  const selectedScenario = workspace.scenarios.find((item) => item.id === selectedScenarioId) ?? null;
  const isEmpty = workspace.tableRows.length === 0;

  const refreshWorkspace = useCallback((
    scenarioId?: string | null,
    nextFilters = filters,
  ) => {
    startTransition(async () => {
      const result = await refreshCommercialPricingScenarioWorkspaceAction(
        scenarioId,
        toCommercialPricingScenarioApiFilters(nextFilters),
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
    updates: Partial<Record<string, string>>,
  ) => {
    const params = buildCommercialPricingScenarioFilterParams(searchParams, updates);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    const nextFilters = readCommercialPricingScenarioFilters(params);
    refreshWorkspace(selectedScenarioId, nextFilters);
  }, [pathname, refreshWorkspace, router, searchParams, selectedScenarioId]);

  const clearFilters = useCallback(() => {
    const params = clearCommercialPricingScenarioFilterParams(searchParams);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    refreshWorkspace(selectedScenarioId, readCommercialPricingScenarioFilters(params));
  }, [pathname, refreshWorkspace, router, searchParams, selectedScenarioId]);

  const loadScenarioDetail = useCallback((scenarioId: string) => {
    setScenarioDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialPricingScenarioDetailAction(scenarioId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setScenarioDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const selectScenario = useCallback((scenarioId: string) => {
    const params = buildCommercialPricingScenarioFilterParams(searchParams, { scenario: scenarioId });
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    loadScenarioDetail(scenarioId);
    startTransition(async () => {
      const result = await refreshCommercialPricingScenarioWorkspaceAction(
        scenarioId,
        toCommercialPricingScenarioApiFilters(filters),
      );
      if (result.status === "success") setWorkspace(result.workspace);
    });
  }, [filters, loadScenarioDetail, pathname, router, searchParams]);

  const createScenario = useCallback(() => {
    startTransition(async () => {
      const proposedUnitPrice = Number(createForm.proposedUnitPrice);
      if (!Number.isFinite(proposedUnitPrice) || proposedUnitPrice <= 0) {
        setFeedback("Geçerli bir önerilen birim fiyat girin.");
        setFeedbackTone("error");
        return;
      }

      const result = await createCommercialPricingScenarioAction({
        profileId: createForm.profileId.trim(),
        expectedProfileVersion: Number(createForm.expectedProfileVersion) || 1,
        salesEvidenceObservationId: createForm.salesEvidenceObservationId,
        proposedUnitPrice,
        proposedDiscountRatio: createForm.proposedDiscountRatio
          ? Number(createForm.proposedDiscountRatio)
          : null,
        quantity: createForm.quantity ? Number(createForm.quantity) : null,
        commercialRationale: createForm.commercialRationale.trim(),
        sourceProvenance: createForm.sourceProvenance.trim(),
      });

      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }

      const scenarioId = result.entityId;
      setFeedback("Fiyat senaryosu oluşturuldu.");
      setFeedbackTone("success");
      if (scenarioId) {
        selectScenario(scenarioId);
      } else {
        refreshWorkspace(selectedScenarioId);
      }
    });
  }, [createForm, refreshWorkspace, selectScenario, selectedScenarioId]);

  const evaluateScenario = useCallback(() => {
    if (!scenarioDetail) return;
    startTransition(async () => {
      const result = await evaluateCommercialPricingScenarioAction({
        scenarioId: scenarioDetail.id,
        expectedVersion: scenarioDetail.version,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setFeedback("Senaryo değerlendirildi.");
      setFeedbackTone("success");
      loadScenarioDetail(scenarioDetail.id);
      refreshWorkspace(scenarioDetail.id);
    });
  }, [loadScenarioDetail, refreshWorkspace, scenarioDetail]);

  const transitionScenario = useCallback((
    targetLifecycleStatus: string,
    successMessage: string,
    note?: string | null,
  ) => {
    if (!scenarioDetail) return;
    startTransition(async () => {
      const result = await transitionCommercialPricingScenarioAction({
        scenarioId: scenarioDetail.id,
        expectedVersion: scenarioDetail.version,
        targetLifecycleStatus,
        note: note ?? null,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setFeedback(successMessage);
      setFeedbackTone("success");
      setRejectNote("");
      loadScenarioDetail(scenarioDetail.id);
      refreshWorkspace(scenarioDetail.id);
    });
  }, [loadScenarioDetail, refreshWorkspace, scenarioDetail]);

  const createTaskDraft = useCallback(() => {
    if (!scenarioDetail || isCreatingTask) return;
    setFeedback(null);
    setIsCreatingTask(true);
    void createCommercialPricingScenarioTaskDraftAction(scenarioDetail.id)
      .then((result) => {
        setIsCreatingTask(false);
        if (result.status === "failure") {
          setFeedback(result.message);
          setFeedbackTone("error");
          return;
        }
        router.push(`/ai-gorevleri/${result.taskId}`);
        router.refresh();
      })
      .catch((error: unknown) => {
        setIsCreatingTask(false);
        setFeedback(error instanceof Error ? error.message : "Görev taslağı oluşturulamadı.");
        setFeedbackTone("error");
      });
  }, [isCreatingTask, router, scenarioDetail]);

  useEffect(() => {
    if (!selectedScenarioId) {
      setScenarioDetail(null);
      setDetailStatus("idle");
      return;
    }
    if (!selectedScenario) {
      setScenarioDetail(null);
      setDetailStatus("idle");
      return;
    }
    if (scenarioDetail?.id === selectedScenarioId && detailStatus === "ready") return;
    loadScenarioDetail(selectedScenarioId);
  }, [detailStatus, loadScenarioDetail, scenarioDetail?.id, selectedScenario, selectedScenarioId]);

  const canEvaluate = workspace.canManage
    && scenarioDetail
    && scenarioDetail.lifecycleStatus === "draft";
  const canSubmit = workspace.canManage
    && scenarioDetail
    && ["needs-review", "eligible-for-approval"].includes(scenarioDetail.lifecycleStatus);
  const canApprove = workspace.canReview
    && scenarioDetail
    && scenarioDetail.lifecycleStatus === "submitted-for-approval";
  const canReject = canApprove;
  const canWithdraw = workspace.canManage
    && scenarioDetail
    && scenarioDetail.lifecycleStatus === "submitted-for-approval";
  const canReturnToDraft = workspace.canManage
    && scenarioDetail
    && ["needs-review", "eligible-for-approval", "blocked", "insufficient-data"].includes(scenarioDetail.lifecycleStatus);
  const canCreateTask = workspace.canCreateTask
    && scenarioDetail
    && !scenarioDetail.linkedAiTaskId
    && !["approved", "rejected", "withdrawn"].includes(scenarioDetail.lifecycleStatus);

  return (
    <main className="commint-page" data-testid="commercial-pricing-scenario-workspace" data-hydrated={isHydrated ? "true" : "false"}>
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Fiyat Koruma ve Onay</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_PRICING_SCENARIO_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>{workspace.facilityLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Sayfa sonucu: {workspace.returnedCount}</li>
              <li>{COMMERCIAL_PRICING_SCENARIO_SCOPE_NOTE}</li>
            </ul>
          </div>
          <div className="commint-executive__actions">
            <button
              type="button"
              className="commint-button"
              onClick={() => refreshWorkspace(selectedScenarioId)}
              disabled={isPending}
              aria-label="Çalışma alanını yenile"
            >
              {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <RefreshCw size={14} />}
              Yenile
            </button>
          </div>
        </div>
        <p className="commint-provenance-strip" data-testid="commercial-pricing-scenario-automation-disclaimer">
          <span className="commint-provenance-strip__badge">Otomasyon</span>
          {workspace.automationDisclaimer || COMMERCIAL_PRICING_SCENARIO_AUTOMATION_DISCLAIMER}
        </p>
        <p className="commint-readonly-note" data-testid="commercial-pricing-scenario-readonly-note">
          {COMMERCIAL_PROFITABILITY_READONLY_NOTE}
        </p>
        {feedback ? (
          <p
            className={`commint-feedback commint-feedback--${feedbackTone}`}
            data-testid="commercial-pricing-scenario-feedback"
          >
            {feedback}
          </p>
        ) : null}
      </header>

      <CommercialPricingScenarioFilterRail
        filters={filters}
        onFilterChange={updateFilters}
        onClear={clearFilters}
      />

      <section
        className="commint-kpi-grid"
        data-testid="commercial-pricing-scenario-kpi-grid"
        aria-label="Fiyat senaryosu göstergeleri"
      >
        {workspace.kpiCards.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id] ?? ShieldCheck;
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

      {workspace.canManage ? (
        <section className="commint-panel" data-testid="commercial-pricing-scenario-create-form">
          <header className="commint-panel__header">
            <h2>Yeni senaryo</h2>
            <p>Pilot profil ve satış kanıtı kimlikleri varsayılan olarak doldurulur.</p>
          </header>
          <div className="commint-detail-grid">
            <label className="commint-filter-field">
              <span>Profil kimliği</span>
              <input
                type="text"
                value={createForm.profileId}
                onChange={(event) => setCreateForm((current) => ({ ...current, profileId: event.target.value }))}
              />
            </label>
            <label className="commint-filter-field">
              <span>Profil sürümü</span>
              <input
                type="number"
                min={1}
                value={createForm.expectedProfileVersion}
                onChange={(event) => setCreateForm((current) => ({ ...current, expectedProfileVersion: event.target.value }))}
              />
            </label>
            <label className="commint-filter-field">
              <span>Satış kanıtı</span>
              <select
                value={createForm.salesEvidenceObservationId}
                onChange={(event) => setCreateForm((current) => ({ ...current, salesEvidenceObservationId: event.target.value }))}
              >
                <option value={COMMERCIAL_PRICING_SCENARIO_PILOT_SALES_EVIDENCE_WITHIN_ID}>
                  Eşik içi kanıt
                </option>
                <option value={COMMERCIAL_PRICING_SCENARIO_PILOT_SALES_EVIDENCE_BELOW_ID}>
                  Eşik altı kanıt
                </option>
              </select>
            </label>
            <label className="commint-filter-field">
              <span>Önerilen birim fiyat (TRY)</span>
              <input
                type="text"
                inputMode="decimal"
                value={createForm.proposedUnitPrice}
                onChange={(event) => setCreateForm((current) => ({ ...current, proposedUnitPrice: event.target.value }))}
              />
            </label>
            <label className="commint-filter-field">
              <span>İndirim oranı (0–1)</span>
              <input
                type="text"
                inputMode="decimal"
                value={createForm.proposedDiscountRatio}
                placeholder="Opsiyonel"
                onChange={(event) => setCreateForm((current) => ({ ...current, proposedDiscountRatio: event.target.value }))}
              />
            </label>
            <label className="commint-filter-field">
              <span>Miktar</span>
              <input
                type="text"
                inputMode="decimal"
                value={createForm.quantity}
                onChange={(event) => setCreateForm((current) => ({ ...current, quantity: event.target.value }))}
              />
            </label>
            <label className="commint-filter-field" style={{ gridColumn: "1 / -1" }}>
              <span>Ticari gerekçe</span>
              <textarea
                rows={2}
                value={createForm.commercialRationale}
                onChange={(event) => setCreateForm((current) => ({ ...current, commercialRationale: event.target.value }))}
              />
            </label>
          </div>
          <div className="commint-executive__actions" style={{ marginTop: "1rem" }}>
            <button
              type="button"
              className="commint-button"
              disabled={isPending}
              data-testid="commercial-pricing-scenario-create-button"
              onClick={createScenario}
            >
              {isPending ? "Oluşturuluyor…" : "Senaryo oluştur"}
            </button>
          </div>
        </section>
      ) : null}

      {isPending && workspace.tableRows.length === 0 ? (
        <div className="commint-loading-state" data-testid="commercial-pricing-scenario-loading" role="status">
          <LoaderCircle className="animate-spin" aria-hidden="true" />
          <p>Fiyat senaryoları yükleniyor…</p>
        </div>
      ) : null}

      {isEmpty && !isPending ? (
        <div className="commint-empty-state" data-testid="commercial-pricing-scenario-empty" role="status">
          <ShieldCheck aria-hidden="true" />
          <h2>Fiyat senaryosu yok</h2>
          <p>{COMMERCIAL_PRICING_SCENARIO_EMPTY_NOTE}</p>
        </div>
      ) : null}

      {!isEmpty ? (
        <section className="commint-panel" data-testid="commercial-pricing-scenario-list">
          <header className="commint-panel__header">
            <h2>Senaryo tablosu</h2>
            <p>Sayfa {workspace.page} · {workspace.returnedCount} kayıt (bu sayfa kümesi)</p>
          </header>
          <CommercialPricingScenarioTable
            rows={workspace.tableRows}
            selectedScenarioId={selectedScenarioId}
            onSelect={selectScenario}
          />
          <div className="commint-pagination" data-testid="commercial-pricing-scenario-pagination">
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

      <section className="commint-panel" data-testid="commercial-pricing-scenario-detail-panel">
        <header className="commint-panel__header">
          <h2>Senaryo detayı</h2>
        </header>
        {!selectedScenarioId ? (
          <p className="commint-supplier-row__meta" data-testid="commercial-pricing-scenario-no-selection" role="status">
            {COMMERCIAL_PRICING_SCENARIO_NO_SELECTION_NOTE}
          </p>
        ) : null}
        {selectedScenarioId && !selectedScenario ? (
          <div className="commint-insufficient-state" data-testid="commercial-pricing-scenario-insufficient" role="status">
            <AlertTriangle aria-hidden="true" />
            <p>Senaryo kaydı yok</p>
          </div>
        ) : null}
        {selectedScenarioId && (detailStatus === "loading" || isPending) ? (
          <div className="commint-loading-state" data-testid="commercial-pricing-scenario-detail-loading" role="status">
            <LoaderCircle className="animate-spin" aria-hidden="true" />
            <p>Senaryo detayı yükleniyor…</p>
          </div>
        ) : null}
        {detailStatus === "error" ? (
          <div className="commint-error-banner" data-testid="commercial-pricing-scenario-error" role="alert">
            <ShieldAlert aria-hidden="true" />
            <p>Senaryo detayı yüklenemedi.</p>
          </div>
        ) : null}
        {detailStatus === "ready" && scenarioDetail ? (
          <div data-testid="commercial-pricing-scenario-detail-ready">
            <div className="commint-detail-grid">
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Referans</p>
                <p className="commint-detail-field__value">{scenarioDetail.scenarioReference}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Yaşam döngüsü</p>
                <p className="commint-detail-field__value">{scenarioDetail.lifecycleStatusLabel}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Koruma sonucu</p>
                <p className="commint-detail-field__value">{scenarioDetail.guardrailResultLabel}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Önerilen fiyat</p>
                <p className="commint-detail-field__value">{scenarioDetail.proposedUnitPriceLabel}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Referans fiyat</p>
                <p className="commint-detail-field__value">{scenarioDetail.baselineUnitPriceLabel}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Ticari gerekçe</p>
                <p className="commint-detail-field__value">{scenarioDetail.commercialRationale}</p>
              </div>
            </div>

            <div className="commint-executive__actions" style={{ marginTop: "1rem" }}>
              {canEvaluate ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  data-testid="commercial-pricing-scenario-evaluate-button"
                  onClick={evaluateScenario}
                >
                  Değerlendir
                </button>
              ) : null}
              {canSubmit ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  data-testid="commercial-pricing-scenario-submit-button"
                  onClick={() => transitionScenario("submitted-for-approval", "Senaryo onaya gönderildi.")}
                >
                  Onaya gönder
                </button>
              ) : null}
              {canApprove ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  data-testid="commercial-pricing-scenario-approve-button"
                  onClick={() => transitionScenario("approved", "Senaryo onaylandı.")}
                >
                  Onayla
                </button>
              ) : null}
              {canReject ? (
                <>
                  <label className="commint-filter-field">
                    <span>Red gerekçesi</span>
                    <input
                      type="text"
                      value={rejectNote}
                      placeholder="Reddetme gerekçesi (zorunlu)"
                      onChange={(event) => setRejectNote(event.target.value)}
                    />
                  </label>
                  <button
                    type="button"
                    className="commint-button commint-button--ghost"
                    disabled={isPending || !rejectNote.trim()}
                    data-testid="commercial-pricing-scenario-reject-button"
                    onClick={() => transitionScenario("rejected", "Senaryo reddedildi.", rejectNote.trim())}
                  >
                    Reddet
                  </button>
                </>
              ) : null}
              {canWithdraw ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  data-testid="commercial-pricing-scenario-withdraw-button"
                  onClick={() => transitionScenario("withdrawn", "Senaryo geri çekildi.")}
                >
                  Geri çek
                </button>
              ) : null}
              {canReturnToDraft ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  onClick={() => transitionScenario("draft", "Senaryo taslağa döndü.")}
                >
                  Taslağa döndür
                </button>
              ) : null}
              {canCreateTask ? (
                <button
                  type="button"
                  className="commint-button"
                  disabled={isCreatingTask}
                  data-testid="commercial-pricing-scenario-create-task-button"
                  onClick={createTaskDraft}
                >
                  {isCreatingTask ? "Görev taslağı oluşturuluyor…" : "Görev taslağı oluştur"}
                </button>
              ) : null}
              {scenarioDetail.linkedAiTaskId ? (
                <Link
                  href={`/ai-gorevleri/${scenarioDetail.linkedAiTaskId}`}
                  className="commint-button commint-button--ghost"
                  data-testid="commercial-pricing-scenario-linked-task-link"
                >
                  <ClipboardList size={14} />
                  Bağlı görevi aç
                </Link>
              ) : null}
            </div>

            {scenarioDetail.latestEvaluation ? (
              <section
                className="commint-detail-section"
                data-testid="commercial-pricing-scenario-evaluation-panel"
                style={{ marginTop: "1.5rem" }}
              >
                <h3 className="commint-detail-section__title">Son değerlendirme</h3>
                <CommercialPricingScenarioEvaluationDetailPanel evaluation={scenarioDetail.latestEvaluation} />
              </section>
            ) : null}

            {scenarioDetail.auditEvents.length > 0 ? (
              <section
                className="commint-detail-section"
                data-testid="commercial-pricing-scenario-audit-timeline"
                style={{ marginTop: "1.5rem" }}
              >
                <h3 className="commint-detail-section__title">Denetim zaman çizelgesi</h3>
                <ul className="commint-driver-list">
                  {scenarioDetail.auditEvents.map((event) => (
                    <li key={event.id} className="commint-detail-field">
                      <p className="commint-detail-field__label">{event.occurredLabel}</p>
                      <p className="commint-detail-field__value">
                        {event.eventTypeLabel}
                        {event.actorSubjectId ? ` · ${event.actorSubjectId}` : ""}
                      </p>
                    </li>
                  ))}
                </ul>
              </section>
            ) : null}
          </div>
        ) : null}
      </section>
    </main>
  );
}

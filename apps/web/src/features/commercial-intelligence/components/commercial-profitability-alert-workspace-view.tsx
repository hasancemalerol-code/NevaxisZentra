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
  BellRing,
  CheckCircle2,
  ClipboardList,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  Sparkles,
} from "lucide-react";

import {
  buildCommercialProfitabilityAlertFilterParams,
  clearCommercialProfitabilityAlertFilterParams,
  readCommercialProfitabilityAlertFilters,
  toCommercialProfitabilityAlertApiFilters,
} from "../application/commercial-profitability-alert-filter";
import {
  COMMERCIAL_PROFITABILITY_ALERT_AUTOMATION_DISCLAIMER,
  COMMERCIAL_PROFITABILITY_ALERT_EMPTY_NOTE,
  COMMERCIAL_PROFITABILITY_ALERT_NO_SELECTION_NOTE,
  COMMERCIAL_PROFITABILITY_ALERT_SCOPE_NOTE,
  COMMERCIAL_PROFITABILITY_ALERT_SUBTITLE,
} from "../domain/commercial-profitability-alert-labels";
import { COMMERCIAL_PROFITABILITY_READONLY_NOTE } from "../domain/commercial-profitability-labels";
import type {
  CommercialProfitabilityAlertDetail,
  CommercialProfitabilityAlertTableRow,
  CommercialProfitabilityAlertWorkspacePayload,
} from "../types/commercial-profitability-alert-types";
import { resolveProfitabilityKpiToneClass } from "./commercial-profitability-presentation";
import { CommercialProfitabilityComparisonDetailPanel } from "./commercial-customer-profitability-workspace-sections";
import {
  createCommercialProfitabilityAlertTaskDraftAction,
  loadCommercialProfitabilityAlertDetailAction,
  materializeCommercialProfitabilityAlertsAction,
  refreshCommercialProfitabilityAlertWorkspaceAction,
  transitionCommercialProfitabilityAlertAction,
} from "../server/commercial-intelligence-actions";
import "./commercial-intelligence-workspace.css";

type CommercialProfitabilityAlertWorkspaceViewProps = Readonly<{
  workspace: CommercialProfitabilityAlertWorkspacePayload;
  initialAlertDetail?: CommercialProfitabilityAlertDetail | null;
  initialDetailStatus?: "idle" | "ready" | "error";
}>;

const KPI_ICONS: Record<string, typeof BellRing> = {
  "alert-active": BellRing,
  "alert-high-critical": AlertTriangle,
  "alert-acknowledged": CheckCircle2,
  "alert-task-created": ClipboardList,
  "alert-evidence-review": ShieldAlert,
};

function severityToneClass(severity: string): string {
  if (severity === "critical" || severity === "high") return "commint-variance--negative";
  if (severity === "low") return "commint-variance--neutral";
  return "commint-variance--warning";
}

type AlertFilterRailProps = Readonly<{
  filters: ReturnType<typeof readCommercialProfitabilityAlertFilters>;
  onFilterChange: (updates: Partial<Record<string, string>>) => void;
  onClear: () => void;
}>;

function CommercialProfitabilityAlertFilterRail({
  filters,
  onFilterChange,
  onClear,
}: AlertFilterRailProps) {
  return (
    <div className="commint-filter-rail" data-testid="commercial-profitability-alert-filter-rail">
      <label className="commint-filter-field">
        <span>Kategori</span>
        <select
          value={filters.category}
          onChange={(event) => onFilterChange({ category: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="product-contribution-deterioration">Ürün katkı bozulması</option>
          <option value="customer-contribution-deterioration">Müşteri katkı bozulması</option>
          <option value="material-contribution-deterioration">Malzeme katkı bozulması</option>
          <option value="cost-mix-deterioration">Maliyet karışımı bozulması</option>
          <option value="data-review">Veri incelemesi</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Önem</span>
        <select
          value={filters.severity}
          onChange={(event) => onFilterChange({ severity: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="critical">Kritik</option>
          <option value="high">Yüksek</option>
          <option value="medium">Orta</option>
          <option value="low">Düşük</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Yaşam döngüsü</span>
        <select
          value={filters.lifecycleStatus}
          onChange={(event) => onFilterChange({ lifecycleStatus: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="active">Aktif</option>
          <option value="acknowledged">Onaylandı</option>
          <option value="task-created">Görev oluşturuldu</option>
          <option value="dismissed">Reddedildi</option>
          <option value="resolved">Çözüldü</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Kanıt durumu</span>
        <select
          value={filters.evidenceStatus}
          onChange={(event) => onFilterChange({ evidenceStatus: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="complete-coverage">Tam kapsam</option>
          <option value="partial-coverage">Kısmi kapsam</option>
          <option value="insufficient-data">Yetersiz veri</option>
          <option value="source-conflict">Kaynak çakışması</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Karşılaştırma türü</span>
        <select
          value={filters.comparisonType}
          onChange={(event) => onFilterChange({ comparisonType: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="temporal-contribution-trend">Zamansal katkı eğilimi</option>
          <option value="cost-component-mix-change">Maliyet bileşeni karışımı</option>
          <option value="customer-contribution-position">Müşteri katkı konumu</option>
          <option value="material-contribution-position">Ürün katkı konumu</option>
        </select>
      </label>
      <button type="button" className="commint-button commint-button--ghost" onClick={onClear}>
        Filtreleri temizle
      </button>
    </div>
  );
}

type AlertTableProps = Readonly<{
  rows: readonly CommercialProfitabilityAlertTableRow[];
  selectedAlertId: string | null;
  onSelect: (alertId: string) => void;
}>;

function CommercialProfitabilityAlertTable({
  rows,
  selectedAlertId,
  onSelect,
}: AlertTableProps) {
  return (
    <div className="commint-table-wrap" data-testid="commercial-profitability-alert-table">
      <table className="commint-table">
        <thead>
          <tr>
            <th>Referans</th>
            <th>Kategori</th>
            <th>Önem</th>
            <th>Yaşam döngüsü</th>
            <th>Başlık</th>
            <th>Karşılaştırma</th>
            <th>Sonuç</th>
            <th>Kanıt</th>
            <th>Güncellendi</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={row.id}
              className={selectedAlertId === row.id ? "commint-table__row--selected" : undefined}
              data-testid={`commercial-profitability-alert-row-${row.id}`}
            >
              <td>
                <button
                  type="button"
                  className="commint-table__link"
                  onClick={() => onSelect(row.id)}
                  aria-pressed={selectedAlertId === row.id}
                >
                  {row.alertReference}
                </button>
              </td>
              <td>{row.categoryLabel}</td>
              <td className={severityToneClass(row.severity)}>{row.severityLabel}</td>
              <td>{row.lifecycleLabel}</td>
              <td>{row.title}</td>
              <td>{row.comparisonTypeLabel}</td>
              <td>{row.outcomeLabel}</td>
              <td>{row.evidenceLabel}</td>
              <td>{row.updatedLabel}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function CommercialProfitabilityAlertWorkspaceView({
  workspace: initialWorkspace,
  initialAlertDetail = null,
  initialDetailStatus = "idle",
}: CommercialProfitabilityAlertWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [isCreatingTask, setIsCreatingTask] = useState(false);
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");
  const [alertDetail, setAlertDetail] = useState<CommercialProfitabilityAlertDetail | null>(initialAlertDetail);
  const [detailStatus, setDetailStatus] = useState<"idle" | "loading" | "ready" | "error">(
    initialDetailStatus === "ready" && initialAlertDetail ? "ready" : initialDetailStatus,
  );
  const [isHydrated, setIsHydrated] = useState(false);

  useEffect(() => {
    setIsHydrated(true);
  }, []);

  const filters = useMemo(
    () => readCommercialProfitabilityAlertFilters(searchParams),
    [searchParams],
  );

  const selectedAlertId = searchParams.get("alert") ?? workspace.selectedAlertId;
  const selectedAlert = workspace.alerts.find((item) => item.id === selectedAlertId) ?? null;
  const isEmpty = workspace.tableRows.length === 0;

  const refreshWorkspace = useCallback((
    alertId?: string | null,
    nextFilters = filters,
  ) => {
    startTransition(async () => {
      const result = await refreshCommercialProfitabilityAlertWorkspaceAction(
        alertId,
        toCommercialProfitabilityAlertApiFilters(nextFilters),
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
    const params = buildCommercialProfitabilityAlertFilterParams(searchParams, updates);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    const nextFilters = readCommercialProfitabilityAlertFilters(params);
    refreshWorkspace(selectedAlertId, nextFilters);
  }, [pathname, refreshWorkspace, router, searchParams, selectedAlertId]);

  const clearFilters = useCallback(() => {
    const params = clearCommercialProfitabilityAlertFilterParams(searchParams);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    refreshWorkspace(selectedAlertId, readCommercialProfitabilityAlertFilters(params));
  }, [pathname, refreshWorkspace, router, searchParams, selectedAlertId]);

  const loadAlertDetail = useCallback((alertId: string) => {
    setAlertDetail(null);
    setDetailStatus("loading");
    void (async () => {
      const result = await loadCommercialProfitabilityAlertDetailAction(alertId);
      if (result.status === "failure") {
        setDetailStatus("error");
        return;
      }
      setAlertDetail(result.detail);
      setDetailStatus("ready");
    })();
  }, []);

  const selectAlert = useCallback((alertId: string) => {
    const params = buildCommercialProfitabilityAlertFilterParams(searchParams, { alert: alertId });
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    loadAlertDetail(alertId);
    startTransition(async () => {
      const result = await refreshCommercialProfitabilityAlertWorkspaceAction(
        alertId,
        toCommercialProfitabilityAlertApiFilters(filters),
      );
      if (result.status === "success") setWorkspace(result.workspace);
    });
  }, [filters, loadAlertDetail, pathname, router, searchParams]);

  const materializeAlerts = useCallback(() => {
    startTransition(async () => {
      const result = await materializeCommercialProfitabilityAlertsAction({
        idempotencyKey: `profitability-alert-ui:${Date.now()}`,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      const created = result.materialization?.createdCount ?? 0;
      const refreshed = result.materialization?.refreshedCount ?? 0;
      setFeedback(`Uyarılar materyalize edildi: ${created} yeni, ${refreshed} yenilendi.`);
      setFeedbackTone("success");
      refreshWorkspace(selectedAlertId);
    });
  }, [refreshWorkspace, selectedAlertId]);

  const transitionAlert = useCallback((
    targetLifecycleStatus: string,
    successMessage: string,
  ) => {
    if (!alertDetail) return;
    startTransition(async () => {
      const result = await transitionCommercialProfitabilityAlertAction({
        alertId: alertDetail.id,
        expectedVersion: alertDetail.version,
        targetLifecycleStatus,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setFeedback(successMessage);
      setFeedbackTone("success");
      loadAlertDetail(alertDetail.id);
      refreshWorkspace(alertDetail.id);
    });
  }, [alertDetail, loadAlertDetail, refreshWorkspace]);

  const createTaskDraft = useCallback(() => {
    if (!alertDetail || isCreatingTask) return;
    setFeedback(null);
    setIsCreatingTask(true);
    void createCommercialProfitabilityAlertTaskDraftAction(alertDetail.id)
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
  }, [alertDetail, isCreatingTask, router]);

  useEffect(() => {
    if (!selectedAlertId) {
      setAlertDetail(null);
      setDetailStatus("idle");
      return;
    }
    if (!selectedAlert) {
      setAlertDetail(null);
      setDetailStatus("idle");
      return;
    }
    if (alertDetail?.id === selectedAlertId && detailStatus === "ready") return;
    loadAlertDetail(selectedAlertId);
  }, [alertDetail?.id, detailStatus, loadAlertDetail, selectedAlert, selectedAlertId]);

  const canAcknowledge = workspace.canManage
    && alertDetail
    && (alertDetail.lifecycleStatus === "active");
  const canDismiss = workspace.canManage
    && alertDetail
    && ["active", "acknowledged", "task-created"].includes(alertDetail.lifecycleStatus);
  const canResolve = workspace.canManage
    && alertDetail
    && ["acknowledged", "task-created"].includes(alertDetail.lifecycleStatus);
  const canCreateTask = workspace.canCreateTask
    && alertDetail
    && !alertDetail.linkedAiTaskId
    && !["dismissed", "resolved"].includes(alertDetail.lifecycleStatus);

  return (
    <main className="commint-page" data-testid="commercial-profitability-alert-workspace" data-hydrated={isHydrated ? "true" : "false"}>
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">TİCARİ ZEKÂ</p>
            <h1>Kârlılık Uyarıları</h1>
            <p className="commint-executive__subtitle">{COMMERCIAL_PROFITABILITY_ALERT_SUBTITLE}</p>
            <ul className="commint-executive__meta">
              <li>{workspace.facilityLabel}</li>
              <li>Güncellik: {workspace.freshnessLabel}</li>
              <li>Sayfa sonucu: {workspace.returnedCount}</li>
              <li>{COMMERCIAL_PROFITABILITY_ALERT_SCOPE_NOTE}</li>
            </ul>
          </div>
          <div className="commint-executive__actions">
            {workspace.canManage ? (
              <button
                type="button"
                className="commint-button commint-button--ghost"
                onClick={materializeAlerts}
                disabled={isPending}
                aria-label="Uyarıları materyalize et"
              >
                {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <Sparkles size={14} />}
                Uyarıları materyalize et
              </button>
            ) : null}
            <button
              type="button"
              className="commint-button"
              onClick={() => refreshWorkspace(selectedAlertId)}
              disabled={isPending}
              aria-label="Çalışma alanını yenile"
            >
              {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <RefreshCw size={14} />}
              Yenile
            </button>
          </div>
        </div>
        <p className="commint-provenance-strip" data-testid="commercial-profitability-alert-automation-disclaimer">
          <span className="commint-provenance-strip__badge">Otomasyon</span>
          {workspace.automationDisclaimer || COMMERCIAL_PROFITABILITY_ALERT_AUTOMATION_DISCLAIMER}
        </p>
        <p className="commint-readonly-note" data-testid="commercial-profitability-alert-readonly-note">
          {COMMERCIAL_PROFITABILITY_READONLY_NOTE}
        </p>
        {feedback ? (
          <p
            className={`commint-feedback commint-feedback--${feedbackTone}`}
            data-testid="commercial-profitability-alert-feedback"
          >
            {feedback}
          </p>
        ) : null}
      </header>

      <CommercialProfitabilityAlertFilterRail
        filters={filters}
        onFilterChange={updateFilters}
        onClear={clearFilters}
      />

      <section
        className="commint-kpi-grid"
        data-testid="commercial-profitability-alert-kpi-grid"
        aria-label="Kârlılık uyarı göstergeleri"
      >
        {workspace.kpiCards.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id] ?? BellRing;
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
        <div className="commint-loading-state" data-testid="commercial-profitability-alert-loading" role="status">
          <LoaderCircle className="animate-spin" aria-hidden="true" />
          <p>Kârlılık uyarıları yükleniyor…</p>
        </div>
      ) : null}

      {isEmpty && !isPending ? (
        <div className="commint-empty-state" data-testid="commercial-profitability-alert-empty" role="status">
          <BellRing aria-hidden="true" />
          <h2>Kârlılık uyarısı yok</h2>
          <p>{COMMERCIAL_PROFITABILITY_ALERT_EMPTY_NOTE}</p>
        </div>
      ) : null}

      {!isEmpty ? (
        <section className="commint-panel" data-testid="commercial-profitability-alert-list">
          <header className="commint-panel__header">
            <h2>Uyarı tablosu</h2>
            <p>Sayfa {workspace.page} · {workspace.returnedCount} kayıt (bu sayfa kümesi)</p>
          </header>
          <CommercialProfitabilityAlertTable
            rows={workspace.tableRows}
            selectedAlertId={selectedAlertId}
            onSelect={selectAlert}
          />
          <div className="commint-pagination" data-testid="commercial-profitability-alert-pagination">
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

      <section className="commint-panel" data-testid="commercial-profitability-alert-detail-panel">
        <header className="commint-panel__header">
          <h2>Uyarı detayı</h2>
        </header>
        {!selectedAlertId ? (
          <p className="commint-supplier-row__meta" data-testid="commercial-profitability-alert-no-selection" role="status">
            {COMMERCIAL_PROFITABILITY_ALERT_NO_SELECTION_NOTE}
          </p>
        ) : null}
        {selectedAlertId && !selectedAlert ? (
          <div className="commint-insufficient-state" data-testid="commercial-profitability-alert-insufficient" role="status">
            <AlertTriangle aria-hidden="true" />
            <p>Uyarı kaydı yok</p>
          </div>
        ) : null}
        {selectedAlertId && (detailStatus === "loading" || isPending) ? (
          <div className="commint-loading-state" data-testid="commercial-profitability-alert-detail-loading" role="status">
            <LoaderCircle className="animate-spin" aria-hidden="true" />
            <p>Uyarı detayı yükleniyor…</p>
          </div>
        ) : null}
        {detailStatus === "error" ? (
          <div className="commint-error-banner" data-testid="commercial-profitability-alert-error" role="alert">
            <ShieldAlert aria-hidden="true" />
            <p>Uyarı detayı yüklenemedi.</p>
          </div>
        ) : null}
        {detailStatus === "ready" && alertDetail ? (
          <div data-testid="commercial-profitability-alert-detail-ready">
            <div className="commint-detail-grid">
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Referans</p>
                <p className="commint-detail-field__value">{alertDetail.alertReference}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Kategori</p>
                <p className="commint-detail-field__value">{alertDetail.alertCategoryLabel}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Önem</p>
                <p className="commint-detail-field__value">{alertDetail.severityLabel}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Yaşam döngüsü</p>
                <p className="commint-detail-field__value">{alertDetail.lifecycleStatusLabel}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Başlık</p>
                <p className="commint-detail-field__value">{alertDetail.title}</p>
              </div>
              <div className="commint-detail-field">
                <p className="commint-detail-field__label">Kanıt özeti</p>
                <p className="commint-detail-field__value">{alertDetail.evidenceSummary}</p>
              </div>
            </div>

            <div className="commint-executive__actions" style={{ marginTop: "1rem" }}>
              {canAcknowledge ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  onClick={() => transitionAlert("acknowledged", "Uyarı onaylandı.")}
                >
                  Onayla
                </button>
              ) : null}
              {canDismiss ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  onClick={() => transitionAlert("dismissed", "Uyarı reddedildi.")}
                >
                  Reddet
                </button>
              ) : null}
              {canResolve ? (
                <button
                  type="button"
                  className="commint-button commint-button--ghost"
                  disabled={isPending}
                  onClick={() => transitionAlert("resolved", "Uyarı çözüldü.")}
                >
                  Çöz
                </button>
              ) : null}
              {canCreateTask ? (
                <button
                  type="button"
                  className="commint-button"
                  disabled={isCreatingTask}
                  data-testid="commercial-profitability-alert-create-task-button"
                  onClick={createTaskDraft}
                >
                  {isCreatingTask ? "Görev taslağı oluşturuluyor…" : "Görev taslağı oluştur"}
                </button>
              ) : null}
              {alertDetail.linkedAiTaskId ? (
                <Link
                  href={`/ai-gorevleri/${alertDetail.linkedAiTaskId}`}
                  className="commint-button commint-button--ghost"
                  data-testid="commercial-profitability-alert-linked-task-link"
                >
                  Bağlı görevi aç
                </Link>
              ) : null}
            </div>

            {alertDetail.auditEvents.length > 0 ? (
              <section
                className="commint-detail-section"
                data-testid="commercial-profitability-alert-audit-timeline"
                style={{ marginTop: "1.5rem" }}
              >
                <h3 className="commint-detail-section__title">Denetim zaman çizelgesi</h3>
                <ul className="commint-driver-list">
                  {alertDetail.auditEvents.map((event) => (
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

            <section className="commint-detail-section" style={{ marginTop: "1.5rem" }}>
              <h3 className="commint-detail-section__title">Kaynak karşılaştırma</h3>
              <CommercialProfitabilityComparisonDetailPanel comparisonDetail={alertDetail.sourceComparison} />
            </section>
          </div>
        ) : null}
      </section>
    </main>
  );
}

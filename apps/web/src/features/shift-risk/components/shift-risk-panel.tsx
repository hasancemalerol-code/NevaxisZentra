"use client";

import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { AlertTriangle, LoaderCircle, ShieldAlert, X } from "lucide-react";

import { ShiftContinuityPanel } from "./shift-continuity-panel";

import {
  formatShiftRiskCount,
  formatShiftRiskDateTime,
  getShiftRiskOutcomeLabel,
  HUMAN_REVIEW_MESSAGE,
  LOCAL_PILOT_DISCLAIMER,
  outcomeBadgeClass,
  severityBadgeClass,
  SHIFT_RISK_CATEGORY_FILTER_OPTIONS,
  SHIFT_RISK_OUTCOME_FILTER_OPTIONS,
  SHIFT_RISK_RULE_SET_VERSION,
} from "../domain/shift-risk-labels";
import type {
  ShiftRiskAssessmentDetail,
  ShiftRiskAssessmentSummary,
  ShiftRiskOverview,
} from "../domain/shift-risk-types";
import {
  loadShiftRiskAssessmentDetailAction,
  loadShiftRiskAssessmentsAction,
  loadShiftRiskOverviewAction,
} from "../server/shift-risk-actions";

type ShiftRiskPanelProps = Readonly<{
  canManage: boolean;
}>;

type RiskSubView = "coverage" | "continuity";

const RISK_SUB_VIEWS = [
  { id: "coverage" as const, label: "Kapsama Riskleri" },
  { id: "continuity" as const, label: "Personel Sürekliliği" },
] as const;

const PAGE_SIZE = 20;

function ShiftCoveragePanel({ canManage }: ShiftRiskPanelProps) {
  const [overview, setOverview] = useState<ShiftRiskOverview | null>(null);
  const [assessments, setAssessments] = useState<readonly ShiftRiskAssessmentSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [outcomeFilter, setOutcomeFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [criticalOnly, setCriticalOnly] = useState(false);
  const [selectedId, setSelectedId] = useState("");
  const [detail, setDetail] = useState<ShiftRiskAssessmentDetail | undefined>();
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const filtersActive = Boolean(outcomeFilter || categoryFilter || criticalOnly);

  const reload = useCallback(() => {
    startTransition(async () => {
      setError(null);
      const [overviewResult, assessmentsResult] = await Promise.all([
        loadShiftRiskOverviewAction(),
        loadShiftRiskAssessmentsAction({
          outcome: outcomeFilter || undefined,
          driverCategory: categoryFilter || undefined,
          criticalOnly: criticalOnly || undefined,
          page,
          pageSize: PAGE_SIZE,
        }),
      ]);

      if (overviewResult.status !== "success" || !overviewResult.overview) {
        setError(
          overviewResult.status === "failure"
            ? overviewResult.message ?? "Özet yüklenemedi."
            : "Özet yüklenemedi.",
        );
        return;
      }
      if (assessmentsResult.status !== "success" || !assessmentsResult.assessments) {
        setError(
          assessmentsResult.status === "failure"
            ? assessmentsResult.message ?? "Değerlendirmeler yüklenemedi."
            : "Değerlendirmeler yüklenemedi.",
        );
        return;
      }

      setOverview(overviewResult.overview);
      setAssessments(assessmentsResult.assessments.items);
      setTotalCount(assessmentsResult.assessments.totalCount);
    });
  }, [outcomeFilter, categoryFilter, criticalOnly, page]);

  useEffect(() => {
    reload();
  }, [reload]);

  const selectAssessment = useCallback((id: string) => {
    setSelectedId(id);
    startTransition(async () => {
      setError(null);
      const result = await loadShiftRiskAssessmentDetailAction(id);
      if (result.status !== "success" || !result.assessment) {
        setError(result.status === "failure" ? (result.message ?? "Detay yüklenemedi.") : "Detay yüklenemedi.");
        setDetail(undefined);
        return;
      }
      setDetail(result.assessment);
    });
  }, []);

  const selectedSummary = assessments.find((item) => item.id === selectedId);
  const detailOpen = Boolean(selectedSummary);

  const kpiItems = useMemo(
    () => [
      { label: "Değerlendirilen", value: overview?.evaluatedContextCount },
      { label: "Temiz", value: overview?.clearCount },
      { label: "Kapsama açığı", value: overview?.coverageGapCount },
      { label: "Kritik rol açığı", value: overview?.criticalRoleGapCount },
      { label: "Çakışma", value: overview?.conflictCount },
      { label: "Yetersiz veri", value: overview?.insufficientDataCount },
    ],
    [overview],
  );

  return (
    <div className="shift-workforce-assignment">
      <p className="shift-workforce-disclaimer">{LOCAL_PILOT_DISCLAIMER}</p>
      <p className="shift-workforce-disclaimer">{HUMAN_REVIEW_MESSAGE}</p>

      <div className="shift-workforce-kpi-strip" aria-label="Vardiya risk KPI özeti">
        {kpiItems.map((item) => (
          <div key={item.label} className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">{item.label}</span>
            <strong className="shift-workforce-kpi__value">{formatShiftRiskCount(item.value)}</strong>
          </div>
        ))}
      </div>

      {error ? (
        <div role="alert" className="shift-workforce-error-inline">
          <AlertTriangle aria-hidden="true" size={16} />
          <span>{error}</span>
        </div>
      ) : null}

      <div className="shift-workforce-registry">
        <div className="shift-workforce-registry__header">
          <h2>Vardiya Risk Değerlendirmeleri</h2>
          <span className="shift-workforce-registry__count">{totalCount} kayıt</span>
        </div>

        <div className="shift-workforce-registry__filters">
          <div className="shift-workforce-filter">
            <label htmlFor="shift-risk-outcome">Sonuç</label>
            <select
              id="shift-risk-outcome"
              value={outcomeFilter}
              onChange={(e) => {
                setPage(1);
                setOutcomeFilter(e.target.value);
              }}
            >
              {SHIFT_RISK_OUTCOME_FILTER_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          <div className="shift-workforce-filter">
            <label htmlFor="shift-risk-category">Kategori</label>
            <select
              id="shift-risk-category"
              value={categoryFilter}
              onChange={(e) => {
                setPage(1);
                setCategoryFilter(e.target.value);
              }}
            >
              {SHIFT_RISK_CATEGORY_FILTER_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
          <label className="shift-workforce-filter shift-workforce-filter--checkbox">
            <input
              type="checkbox"
              checked={criticalOnly}
              onChange={(e) => {
                setPage(1);
                setCriticalOnly(e.target.checked);
              }}
            />
            Yalnızca kritik rol
          </label>
          {filtersActive ? (
            <button
              type="button"
              className="shift-workforce-btn shift-workforce-btn--ghost"
              onClick={() => {
                setOutcomeFilter("");
                setCategoryFilter("");
                setCriticalOnly(false);
                setPage(1);
              }}
            >
              Filtreleri temizle
            </button>
          ) : null}
        </div>

        <div className={`shift-workforce-workspace${detailOpen ? " shift-workforce-workspace--detail-open" : ""}`}>
          <div className="shift-workforce-registry__table-wrap">
            {isPending && !assessments.length ? (
              <div className="shift-workforce-loading">
                <LoaderCircle className="animate-spin" aria-hidden="true" />
                <span>Risk değerlendirmeleri yükleniyor…</span>
              </div>
            ) : assessments.length === 0 ? (
              <div className="shift-workforce-empty">
                <ShieldAlert aria-hidden="true" />
                <p>Kayıtlı risk değerlendirmesi bulunamadı.</p>
              </div>
            ) : (
              <table className="shift-workforce-table">
                <thead>
                  <tr>
                    <th>Vardiya</th>
                    <th>Üretim emri</th>
                    <th>Makine</th>
                    <th>Gerekli</th>
                    <th>Onaylı</th>
                    <th>Açık</th>
                    <th>Sonuç</th>
                    <th>Öncelikli risk</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {assessments.map((item) => (
                    <tr
                      key={item.id}
                      className={item.id === selectedId ? "shift-workforce-table__row--selected" : ""}
                      onClick={() => selectAssessment(item.id)}
                    >
                      <td>{item.shiftLabel}</td>
                      <td>{item.productionOrderReference ?? "—"}</td>
                      <td>{item.machineAssetLabel ?? "—"}</td>
                      <td>{formatShiftRiskCount(item.minimumRequiredCount)}</td>
                      <td>{formatShiftRiskCount(item.approvedEligibleCount)}</td>
                      <td>{formatShiftRiskCount(item.coverageGap)}</td>
                      <td>
                        <span className={outcomeBadgeClass(item.outcome)}>
                          {getShiftRiskOutcomeLabel(item.outcome, item.outcomeLabel)}
                        </span>
                      </td>
                      <td>{item.leadingReasonCode ?? "—"}</td>
                      <td>
                        <button
                          type="button"
                          className="shift-workforce-btn shift-workforce-btn--ghost"
                          onClick={(e) => {
                            e.stopPropagation();
                            selectAssessment(item.id);
                          }}
                        >
                          Detay
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {detailOpen && detail ? (
            <aside className="shift-workforce-detail" aria-label="Risk değerlendirme detayı">
              <header className="shift-workforce-detail__header">
                <div>
                  <h3>{detail.shiftLabel} · {detail.productionOrderReference ?? "Genel"}</h3>
                  <p>{formatShiftRiskDateTime(detail.evaluatedAtUtc)}</p>
                </div>
                <button
                  type="button"
                  className="shift-workforce-btn shift-workforce-btn--ghost"
                  aria-label="Detayı kapat"
                  onClick={() => {
                    setSelectedId("");
                    setDetail(undefined);
                  }}
                >
                  <X aria-hidden="true" size={18} />
                </button>
              </header>

              <div className="shift-workforce-detail__metrics">
                <span className={outcomeBadgeClass(detail.outcome)}>
                  {detail.outcomeLabel}
                </span>
                <span>Kural seti: {detail.ruleSetVersion || SHIFT_RISK_RULE_SET_VERSION}</span>
                <span>Gerekli: {formatShiftRiskCount(detail.minimumRequiredCount)}</span>
                <span>Onaylı: {formatShiftRiskCount(detail.approvedEligibleCount)}</span>
                <span>Açık: {formatShiftRiskCount(detail.coverageGap)}</span>
              </div>

              <section>
                <h4>Yapılandırılmış risk sürücüleri</h4>
                {detail.drivers.length === 0 ? (
                  <p className="shift-workforce-empty-inline">Sürücü kaydı yok.</p>
                ) : (
                  <ul className="shift-workforce-reason-list">
                    {detail.drivers.map((driver) => (
                      <li key={driver.id}>
                        <span className={severityBadgeClass(driver.severity)}>
                          {driver.severityLabel}
                        </span>
                        <strong>{driver.labelTr}</strong>
                        <span>
                          Gerekli: {driver.requiredValue ?? "—"} · Gözlenen: {driver.observedValue ?? "—"}
                          {driver.gapValue ? ` · Açık: ${driver.gapValue}` : ""}
                        </span>
                        {driver.isLeadingRisk ? <em>Öncelikli risk</em> : null}
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section>
                <h4>Kapsamaya dahil atamalar</h4>
                {detail.countedAssignments.length === 0 ? (
                  <p className="shift-workforce-empty-inline">—</p>
                ) : (
                  <ul className="shift-workforce-reason-list">
                    {detail.countedAssignments.map((a) => (
                      <li key={a.assignmentId}>
                        {a.displayName} ({a.employeeCode}) · {a.statusLabel}
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section>
                <h4>Hariç tutulan atamalar</h4>
                {detail.excludedAssignments.length === 0 ? (
                  <p className="shift-workforce-empty-inline">—</p>
                ) : (
                  <ul className="shift-workforce-reason-list">
                    {detail.excludedAssignments.map((a) => (
                      <li key={a.assignmentId}>
                        {a.displayName} ({a.employeeCode}) · {a.exclusionReasonLabel ?? a.exclusionReasonCode ?? "—"}
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              {detail.isPilotData ? (
                <p className="shift-workforce-disclaimer">{LOCAL_PILOT_DISCLAIMER}</p>
              ) : null}

              {!canManage ? (
                <p className="shift-workforce-readonly-notice">Salt okunur mod — değerlendirme tetiklenemez.</p>
              ) : null}
            </aside>
          ) : null}
        </div>
      </div>
    </div>
  );
}

export function ShiftRiskPanel({ canManage }: ShiftRiskPanelProps) {
  const [activeSubView, setActiveSubView] = useState<RiskSubView>("coverage");

  return (
    <>
      <div className="shift-workforce-workspace-tabs" role="tablist" aria-label="Vardiya risk alt görünümleri">
        {RISK_SUB_VIEWS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeSubView === tab.id}
            className={`shift-workforce-workspace-tab${activeSubView === tab.id ? " shift-workforce-workspace-tab--active" : ""}`}
            onClick={() => setActiveSubView(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeSubView === "coverage" ? (
        <ShiftCoveragePanel canManage={canManage} />
      ) : (
        <ShiftContinuityPanel canManage={canManage} />
      )}
    </>
  );
}

"use client";

import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { AlertTriangle, LoaderCircle, ShieldAlert, X } from "lucide-react";

import {
  availabilityBadgeClass,
  freshnessBadgeClass,
  verificationBadgeClass,
} from "../../shift-workforce/domain/shift-workforce-labels";
import {
  formatShiftContinuityCount,
  formatShiftContinuityDate,
  formatShiftContinuityDateTime,
  getShiftContinuityOutcomeLabel,
  getShiftContinuityReasonLabel,
  HUMAN_REVIEW_MESSAGE,
  LOCAL_PILOT_DISCLAIMER,
  outcomeBadgeClass,
  severityBadgeClass,
  SHIFT_CONTINUITY_AVAILABILITY_FILTER_OPTIONS,
  SHIFT_CONTINUITY_CATEGORY_FILTER_OPTIONS,
  SHIFT_CONTINUITY_COMPETENCY_FILTER_OPTIONS,
  SHIFT_CONTINUITY_FRESHNESS_FILTER_OPTIONS,
  SHIFT_CONTINUITY_OUTCOME_FILTER_OPTIONS,
  SHIFT_CONTINUITY_RULE_SET_VERSION,
  SHIFT_CONTINUITY_SHIFT_FILTER_OPTIONS,
} from "../domain/shift-continuity-labels";
import type {
  ShiftContinuityAssessmentDetail,
  ShiftContinuityAssessmentSummary,
  ShiftContinuityOverview,
} from "../domain/shift-continuity-types";
import {
  loadShiftContinuityAssessmentDetailAction,
  loadShiftContinuityAssessmentsAction,
  loadShiftContinuityOverviewAction,
} from "../server/shift-continuity-actions";

type ShiftContinuityPanelProps = Readonly<{
  canManage: boolean;
}>;

const PAGE_SIZE = 20;

function orderMachineLabel(item: ShiftContinuityAssessmentSummary): string {
  const parts = [item.productionOrderReference, item.machineAssetLabel].filter(Boolean);
  return parts.length > 0 ? parts.join(" · ") : "—";
}

function workloadLabel(count: number, minutes: number): string {
  return `${formatShiftContinuityCount(count)} atama · ${formatShiftContinuityCount(minutes)} dk`;
}

export function ShiftContinuityPanel({ canManage }: ShiftContinuityPanelProps) {
  const [overview, setOverview] = useState<ShiftContinuityOverview | null>(null);
  const [assessments, setAssessments] = useState<readonly ShiftContinuityAssessmentSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [shiftFilter, setShiftFilter] = useState("");
  const [shiftDateFilter, setShiftDateFilter] = useState("");
  const [outcomeFilter, setOutcomeFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [competencyFilter, setCompetencyFilter] = useState("");
  const [availabilityFilter, setAvailabilityFilter] = useState("");
  const [freshnessFilter, setFreshnessFilter] = useState("");
  const [criticalOnly, setCriticalOnly] = useState(false);
  const [selectedId, setSelectedId] = useState("");
  const [detail, setDetail] = useState<ShiftContinuityAssessmentDetail | undefined>();
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const filtersActive = Boolean(
    shiftFilter ||
      shiftDateFilter ||
      outcomeFilter ||
      categoryFilter ||
      competencyFilter ||
      availabilityFilter ||
      freshnessFilter ||
      criticalOnly,
  );

  const reload = useCallback(() => {
    startTransition(async () => {
      setError(null);
      const [overviewResult, assessmentsResult] = await Promise.all([
        loadShiftContinuityOverviewAction(),
        loadShiftContinuityAssessmentsAction({
          productionShiftId: shiftFilter || undefined,
          shiftDate: shiftDateFilter || undefined,
          outcome: outcomeFilter || undefined,
          driverCategory: categoryFilter || undefined,
          competencyCode: competencyFilter || undefined,
          availability: availabilityFilter || undefined,
          freshness: freshnessFilter || undefined,
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
  }, [
    availabilityFilter,
    categoryFilter,
    competencyFilter,
    criticalOnly,
    freshnessFilter,
    outcomeFilter,
    page,
    shiftDateFilter,
    shiftFilter,
  ]);

  useEffect(() => {
    reload();
  }, [reload]);

  const selectAssessment = useCallback((id: string) => {
    setSelectedId(id);
    startTransition(async () => {
      setError(null);
      const result = await loadShiftContinuityAssessmentDetailAction(id);
      if (result.status !== "success" || !result.assessment) {
        setError(
          result.status === "failure" ? (result.message ?? "Detay yüklenemedi.") : "Detay yüklenemedi.",
        );
        setDetail(undefined);
        return;
      }
      setDetail(result.assessment);
    });
  }, []);

  const selectedSummary = assessments.find((item) => item.id === selectedId);
  const detailOpen = Boolean(selectedSummary);
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const kpiItems = useMemo(
    () => [
      { label: "Değerlendirilen bağlam", value: overview?.evaluatedContextCount },
      { label: "İzin/yokluk etkisi", value: overview?.absenceImpactCount },
      { label: "Süresi dolmuş yetkinlik", value: overview?.competencyExpiredCount },
      { label: "Süresi yaklaşan yetkinlik", value: overview?.competencyExpiringCount },
      { label: "Bayat/bilinmeyen veri", value: overview?.staleOrUnknownSourceCount },
      { label: "Atama çakışması", value: overview?.assignmentConflictCount },
      { label: "Kapsama etkisi", value: overview?.coverageImpactCount },
      { label: "Yetersiz veri", value: overview?.insufficientDataCount },
    ],
    [overview],
  );

  function clearFilters() {
    setShiftFilter("");
    setShiftDateFilter("");
    setOutcomeFilter("");
    setCategoryFilter("");
    setCompetencyFilter("");
    setAvailabilityFilter("");
    setFreshnessFilter("");
    setCriticalOnly(false);
    setPage(1);
  }

  return (
    <div className="shift-workforce-assignment">
      <p className="shift-workforce-disclaimer">{LOCAL_PILOT_DISCLAIMER}</p>
      <p className="shift-workforce-disclaimer">{HUMAN_REVIEW_MESSAGE}</p>

      <div className="shift-workforce-kpi-strip" aria-label="Personel sürekliliği KPI özeti">
        {kpiItems.map((item) => (
          <div key={item.label} className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">{item.label}</span>
            <strong className="shift-workforce-kpi__value">{formatShiftContinuityCount(item.value)}</strong>
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
          <h2>Personel Sürekliliği Değerlendirmeleri</h2>
          <span className="shift-workforce-registry__count">{totalCount} kayıt</span>
        </div>

        <div className="shift-workforce-registry__filters">
          <div className="shift-workforce-filter">
            <label htmlFor="shift-continuity-date">Tarih</label>
            <input
              id="shift-continuity-date"
              type="date"
              value={shiftDateFilter}
              onChange={(event) => {
                setPage(1);
                setShiftDateFilter(event.target.value);
              }}
            />
          </div>
          <div className="shift-workforce-filter">
            <label htmlFor="shift-continuity-shift">Vardiya</label>
            <select
              id="shift-continuity-shift"
              value={shiftFilter}
              onChange={(event) => {
                setPage(1);
                setShiftFilter(event.target.value);
              }}
            >
              {SHIFT_CONTINUITY_SHIFT_FILTER_OPTIONS.map((option) => (
                <option key={option.value || "all"} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="shift-workforce-filter">
            <label htmlFor="shift-continuity-outcome">Sonuç</label>
            <select
              id="shift-continuity-outcome"
              value={outcomeFilter}
              onChange={(event) => {
                setPage(1);
                setOutcomeFilter(event.target.value);
              }}
            >
              {SHIFT_CONTINUITY_OUTCOME_FILTER_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="shift-workforce-filter">
            <label htmlFor="shift-continuity-category">Risk kategorisi</label>
            <select
              id="shift-continuity-category"
              value={categoryFilter}
              onChange={(event) => {
                setPage(1);
                setCategoryFilter(event.target.value);
              }}
            >
              {SHIFT_CONTINUITY_CATEGORY_FILTER_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="shift-workforce-filter">
            <label htmlFor="shift-continuity-competency">Yetkinlik</label>
            <select
              id="shift-continuity-competency"
              value={competencyFilter}
              onChange={(event) => {
                setPage(1);
                setCompetencyFilter(event.target.value);
              }}
            >
              {SHIFT_CONTINUITY_COMPETENCY_FILTER_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="shift-workforce-filter">
            <label htmlFor="shift-continuity-availability">Müsaitlik</label>
            <select
              id="shift-continuity-availability"
              value={availabilityFilter}
              onChange={(event) => {
                setPage(1);
                setAvailabilityFilter(event.target.value);
              }}
            >
              {SHIFT_CONTINUITY_AVAILABILITY_FILTER_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="shift-workforce-filter">
            <label htmlFor="shift-continuity-freshness">Tazelik</label>
            <select
              id="shift-continuity-freshness"
              value={freshnessFilter}
              onChange={(event) => {
                setPage(1);
                setFreshnessFilter(event.target.value);
              }}
            >
              {SHIFT_CONTINUITY_FRESHNESS_FILTER_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <label className="shift-workforce-filter shift-workforce-filter--checkbox">
            <input
              type="checkbox"
              checked={criticalOnly}
              onChange={(event) => {
                setPage(1);
                setCriticalOnly(event.target.checked);
              }}
            />
            Yalnızca kritik bağlam
          </label>
          {filtersActive ? (
            <button
              type="button"
              className="shift-workforce-btn shift-workforce-btn--ghost"
              onClick={clearFilters}
            >
              Filtreleri temizle
            </button>
          ) : null}
        </div>

        <div
          className={`shift-workforce-workspace${detailOpen ? " shift-workforce-workspace--detail-open" : ""}`}
        >
          <div className="shift-workforce-registry__table-wrap">
            {isPending && !assessments.length ? (
              <div className="shift-workforce-loading">
                <LoaderCircle className="animate-spin" aria-hidden="true" />
                <span>Personel sürekliliği değerlendirmeleri yükleniyor…</span>
              </div>
            ) : assessments.length === 0 ? (
              <div className="shift-workforce-empty">
                <ShieldAlert aria-hidden="true" />
                <p>Kayıtlı personel sürekliliği değerlendirmesi bulunamadı.</p>
              </div>
            ) : (
              <table className="shift-workforce-table">
                <thead>
                  <tr>
                    <th>Vardiya / tarih</th>
                    <th>Personel</th>
                    <th>Emir / makine</th>
                    <th>Müsaitlik</th>
                    <th>Yetkinlik</th>
                    <th>Tazelik</th>
                    <th>Atama</th>
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
                      <td>
                        <span className="shift-workforce-cell-primary">{item.shiftLabel}</span>
                        <span className="shift-workforce-cell-secondary">
                          {formatShiftContinuityDate(item.shiftDate)}
                        </span>
                      </td>
                      <td>
                        <span className="shift-workforce-cell-primary">{item.displayName}</span>
                        <span className="shift-workforce-cell-secondary">{item.employeeCode}</span>
                      </td>
                      <td>{orderMachineLabel(item)}</td>
                      <td>
                        <span className={availabilityBadgeClass(item.availabilityState)}>
                          {item.availabilityLabel}
                        </span>
                      </td>
                      <td>{item.competencyLabel}</td>
                      <td>
                        <span className={freshnessBadgeClass(item.freshnessState)}>
                          {item.freshnessLabel}
                        </span>
                      </td>
                      <td>
                        {workloadLabel(item.approvedAssignmentCount, item.plannedAssignmentMinutes)}
                      </td>
                      <td>
                        <span className={outcomeBadgeClass(item.outcome)}>
                          {getShiftContinuityOutcomeLabel(item.outcome, item.outcomeLabel)}
                        </span>
                      </td>
                      <td>{getShiftContinuityReasonLabel(item.leadingReasonCode)}</td>
                      <td>
                        <button
                          type="button"
                          className="shift-workforce-btn shift-workforce-btn--ghost"
                          onClick={(event) => {
                            event.stopPropagation();
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
            <aside className="shift-workforce-detail" aria-label="Personel sürekliliği detayı">
              <header className="shift-workforce-detail__header">
                <div>
                  <h3>
                    {detail.displayName} · {detail.employeeCode}
                  </h3>
                  <p>
                    {detail.shiftLabel} · {formatShiftContinuityDate(detail.shiftDate)} ·{" "}
                    {formatShiftContinuityDateTime(detail.evaluatedAtUtc)}
                  </p>
                </div>
                <button
                  type="button"
                  className="shift-workforce-btn shift-workforce-btn--ghost"
                  aria-label="Süreklilik risk detayını kapat"
                  onClick={() => {
                    setSelectedId("");
                    setDetail(undefined);
                  }}
                >
                  <X aria-hidden="true" size={18} />
                </button>
              </header>

              <div className="shift-workforce-detail__metrics">
                <span className={outcomeBadgeClass(detail.outcome)}>{detail.outcomeLabel}</span>
                <span>Kural seti: {detail.ruleSetVersion || SHIFT_CONTINUITY_RULE_SET_VERSION}</span>
                <span className={availabilityBadgeClass(detail.availabilityState)}>
                  {detail.availabilityLabel}
                </span>
                <span className={freshnessBadgeClass(detail.freshnessState)}>
                  {detail.freshnessLabel}
                </span>
              </div>

              <section>
                <h4>Personel ve vardiya bağlamı</h4>
                <dl className="shift-workforce-detail-grid">
                  <div>
                    <dt>Departman</dt>
                    <dd>{detail.department ?? "—"}</dd>
                  </div>
                  <div>
                    <dt>Görev</dt>
                    <dd>{detail.jobTitle ?? "—"}</dd>
                  </div>
                  <div>
                    <dt>İstihdam</dt>
                    <dd>{detail.isActive ? "Aktif" : "Pasif"}</dd>
                  </div>
                  <div>
                    <dt>Üretim emri</dt>
                    <dd>{detail.productionOrderReference ?? "—"}</dd>
                  </div>
                  <div>
                    <dt>Makine</dt>
                    <dd>{detail.machineAssetLabel ?? "—"}</dd>
                  </div>
                  <div>
                    <dt>Planlanan pencere</dt>
                    <dd>
                      {formatShiftContinuityDateTime(detail.plannedStartUtc)} –{" "}
                      {formatShiftContinuityDateTime(detail.plannedEndUtc)}
                    </dd>
                  </div>
                </dl>
              </section>

              <section>
                <h4>ERP/HR kaynağı (salt okunur)</h4>
                <dl className="shift-workforce-detail-grid shift-workforce-detail-grid--readonly">
                  <div>
                    <dt>Kaynak</dt>
                    <dd>{detail.sourceLabel}</dd>
                  </div>
                  <div>
                    <dt>Vardiya referansı</dt>
                    <dd>{detail.sourceShiftReference ?? "—"}</dd>
                  </div>
                  <div>
                    <dt>Takvim referansı</dt>
                    <dd>{detail.sourceCalendarReference ?? "—"}</dd>
                  </div>
                  <div>
                    <dt>Kaynak güncelleme</dt>
                    <dd>{formatShiftContinuityDateTime(detail.sourceUpdatedAtUtc)}</dd>
                  </div>
                  <div>
                    <dt>Senkronizasyon</dt>
                    <dd>{formatShiftContinuityDateTime(detail.synchronizedAtUtc)}</dd>
                  </div>
                </dl>
                {detail.sourceLimitations.length > 0 ? (
                  <ul className="shift-workforce-reason-list">
                    {detail.sourceLimitations.map((limitation) => (
                      <li key={limitation}>{limitation}</li>
                    ))}
                  </ul>
                ) : null}
              </section>

              <section>
                <h4>Yetkinlik geçerliliği</h4>
                {detail.competencies.length === 0 ? (
                  <p className="shift-workforce-empty-inline">Yetkinlik kaydı yok.</p>
                ) : (
                  <ul className="shift-workforce-reason-list">
                    {detail.competencies.map((competency) => (
                      <li key={competency.competencyCode}>
                        <span
                          className={verificationBadgeClass(competency.verificationStatus)}
                        >
                          {competency.verificationStatusLabel}
                        </span>
                        <strong>{competency.competencyLabel}</strong>
                        <span>
                          Geçerlilik: {formatShiftContinuityDate(competency.validFromUtc)} –{" "}
                          {formatShiftContinuityDate(competency.validUntilUtc)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section>
                <h4>Onaylı atamalar</h4>
                {detail.approvedAssignments.length === 0 ? (
                  <p className="shift-workforce-empty-inline">Onaylı atama yok.</p>
                ) : (
                  <ul className="shift-workforce-reason-list">
                    {detail.approvedAssignments.map((assignment) => (
                      <li key={assignment.assignmentId}>
                        <strong>
                          {assignment.shiftLabel} · {formatShiftContinuityDate(assignment.shiftDate)}
                        </strong>
                        <span>
                          {assignment.productionOrderReference ?? "—"} ·{" "}
                          {assignment.machineAssetLabel ?? "—"} · {assignment.statusLabel}
                        </span>
                        <span>
                          {formatShiftContinuityDateTime(assignment.plannedStartUtc)} –{" "}
                          {formatShiftContinuityDateTime(assignment.plannedEndUtc)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              <section>
                <h4>Atama yükü</h4>
                <dl className="shift-workforce-detail-grid">
                  <div>
                    <dt>Onaylı atama sayısı</dt>
                    <dd>{formatShiftContinuityCount(detail.workloadFacts.approvedAssignmentCount)}</dd>
                  </div>
                  <div>
                    <dt>Planlanan dakika</dt>
                    <dd>{formatShiftContinuityCount(detail.workloadFacts.plannedAssignmentMinutes)}</dd>
                  </div>
                  <div>
                    <dt>Yetkili eşik</dt>
                    <dd>
                      {detail.workloadFacts.authoritativeThresholdAvailable
                        ? detail.workloadFacts.thresholdValue ?? "Tanımlı"
                        : "Tanımlı değil"}
                    </dd>
                  </div>
                </dl>
                {detail.workloadFacts.conclusionRequiresThreshold ? (
                  <p className="shift-workforce-readonly-notice">
                    Yük sonucu için yetkili kapasite eşiği bulunmuyor; yalnızca atama gerçekleri
                    gösterilir.
                  </p>
                ) : null}
              </section>

              <section>
                <h4>Kapsama etkisi</h4>
                {!detail.coverageImpact.requirementConfigured ? (
                  <p className="shift-workforce-readonly-notice">
                    Yapılandırılmış kapsama gereksinimi bulunmuyor.
                  </p>
                ) : (
                  <dl className="shift-workforce-detail-grid">
                    <div>
                      <dt>Gerekli</dt>
                      <dd>{formatShiftContinuityCount(detail.coverageImpact.requiredCount)}</dd>
                    </div>
                    <div>
                      <dt>Güvenilir müsait</dt>
                      <dd>
                        {formatShiftContinuityCount(detail.coverageImpact.trustworthyAvailableCount)}
                      </dd>
                    </div>
                    <div>
                      <dt>Açık</dt>
                      <dd>{formatShiftContinuityCount(detail.coverageImpact.coverageGap)}</dd>
                    </div>
                    <div>
                      <dt>Kritik rol</dt>
                      <dd>
                        {detail.coverageImpact.isCriticalRole === null
                          ? "—"
                          : detail.coverageImpact.isCriticalRole
                            ? "Evet"
                            : "Hayır"}
                      </dd>
                    </div>
                  </dl>
                )}
              </section>

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
                          Gerekli: {driver.requiredValue ?? "—"} · Gözlenen:{" "}
                          {driver.observedValue ?? "—"}
                          {driver.gapValue ? ` · Açık: ${driver.gapValue}` : ""}
                        </span>
                        <span>
                          Kaynak: {driver.evidenceSource}
                          {driver.evidenceTimestampUtc
                            ? ` · ${formatShiftContinuityDateTime(driver.evidenceTimestampUtc)}`
                            : ""}
                          {driver.freshnessLabel ? ` · ${driver.freshnessLabel}` : ""}
                        </span>
                        {driver.isLeadingRisk ? <em>Öncelikli risk</em> : null}
                      </li>
                    ))}
                  </ul>
                )}
              </section>

              {detail.availabilityState === "unknown" ? (
                <p className="shift-workforce-readonly-notice">
                  Müsaitlik bilinmiyor; personel güvenilir şekilde müsait kabul edilmez.
                </p>
              ) : null}

              {detail.freshnessState === "stale" ? (
                <p className="shift-workforce-readonly-notice">
                  Kaynak verisi bayat; temiz sonuç güvenilir şekilde üretilemez.
                </p>
              ) : null}

              {detail.isPilotData ? (
                <p className="shift-workforce-disclaimer">{LOCAL_PILOT_DISCLAIMER}</p>
              ) : null}

              {!canManage ? (
                <p className="shift-workforce-readonly-notice">
                  Salt okunur mod — değerlendirme tetiklenemez.
                </p>
              ) : null}
            </aside>
          ) : null}
        </div>

        {totalCount > PAGE_SIZE ? (
          <div className="shift-workforce-pagination">
            <span className="shift-workforce-pagination__info">
              Sayfa {page} / {totalPages}
            </span>
            <div className="shift-workforce-pagination__actions">
              <button
                type="button"
                className="shift-workforce-btn shift-workforce-btn--sm"
                disabled={page <= 1 || isPending}
                onClick={() => setPage((current) => Math.max(1, current - 1))}
              >
                Önceki
              </button>
              <button
                type="button"
                className="shift-workforce-btn shift-workforce-btn--sm"
                disabled={page >= totalPages || isPending}
                onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
              >
                Sonraki
              </button>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

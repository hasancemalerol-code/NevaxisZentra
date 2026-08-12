"use client";

import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { CircleGauge, LoaderCircle, X } from "lucide-react";

import type {
  ProcessOeeAssessmentDetail,
  ProcessOeeAssessmentSummary,
  ProcessOeeWorkspaceSummary,
} from "../domain/process-oee";
import {
  formatDataSufficiencyPercent,
  formatMachineLabel,
  formatPercent,
  formatProcessOeeDateRange,
  formatProcessOeeDateTime,
  formatProcessOeeNumber,
  getDriverSeverityLabel,
  getProcessOeeStatusLabel,
  PROCESS_OEE_STATUS_FILTER_OPTIONS,
} from "../presentation/process-oee-labels";
import {
  evaluateProcessOeeAction,
  loadProcessOeeAssessmentDetailAction,
  loadProcessOeeAssessmentsAction,
} from "../server/process-oee-actions";
import "./process-oee.css";

type ProcessOeeViewProps = Readonly<{
  summary: ProcessOeeWorkspaceSummary;
  assessments: readonly ProcessOeeAssessmentSummary[];
  canManage: boolean;
}>;

type MachineOption = Readonly<{
  id: string;
  label: string;
}>;

function statusBadgeClass(status: string): string {
  switch (status) {
    case "calculated":
      return "process-oee-badge process-oee-badge--calculated";
    case "insufficient-data":
      return "process-oee-badge process-oee-badge--insufficient";
    case "review-required":
      return "process-oee-badge process-oee-badge--review";
    case "stale-data":
      return "process-oee-badge process-oee-badge--stale";
    default:
      return "process-oee-badge process-oee-badge--stale";
  }
}

function buildMachineOptions(assessments: readonly ProcessOeeAssessmentSummary[]): MachineOption[] {
  const map = new Map<string, string>();
  for (const item of assessments) {
    if (!map.has(item.machineAssetId)) {
      map.set(
        item.machineAssetId,
        formatMachineLabel(item.machineCode, item.machineDisplayName),
      );
    }
  }

  return [...map.entries()]
    .map(([id, label]) => ({ id, label }))
    .sort((a, b) => a.label.localeCompare(b.label, "tr"));
}

function toUtcIso(dateValue: string, endOfDay = false): string | undefined {
  if (!dateValue) {
    return undefined;
  }

  const suffix = endOfDay ? "T23:59:59.999Z" : "T00:00:00.000Z";
  return new Date(`${dateValue}${suffix}`).toISOString();
}

export function ProcessOeeView({
  summary,
  assessments: initialAssessments,
  canManage,
}: ProcessOeeViewProps) {
  const [assessments, setAssessments] = useState(initialAssessments);
  const [machineFilter, setMachineFilter] = useState("");
  const [productionOrderFilter, setProductionOrderFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [evaluatedFrom, setEvaluatedFrom] = useState("");
  const [evaluatedTo, setEvaluatedTo] = useState("");
  const [selectedId, setSelectedId] = useState("");
  const [detail, setDetail] = useState<ProcessOeeAssessmentDetail | undefined>();
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const filtersActive = Boolean(
    machineFilter || productionOrderFilter || statusFilter || evaluatedFrom || evaluatedTo,
  );

  const machineOptions = useMemo(() => buildMachineOptions(assessments), [assessments]);

  const activeDetail = detail && detail.id === selectedId ? detail : undefined;
  const selectedSummary =
    assessments.find((item) => item.id === selectedId) ??
    initialAssessments.find((item) => item.id === selectedId);

  const kpiOee = activeDetail?.oeePercent ?? summary.latestOeePercent;
  const kpiAvailability = activeDetail?.availabilityPercent ?? null;
  const kpiPerformance = activeDetail?.performancePercent ?? null;
  const kpiQuality = activeDetail?.qualityPercent ?? null;
  const kpiDataSufficiency = formatDataSufficiencyPercent(
    summary.calculatedCount,
    summary.assessmentCount,
  );

  const reloadAssessments = useCallback(() => {
    startTransition(async () => {
      setError(null);
      const result = await loadProcessOeeAssessmentsAction({
        machineAssetId: machineFilter || undefined,
        productionOrderReference: productionOrderFilter.trim() || undefined,
        status: statusFilter || undefined,
        evaluatedFromUtc: toUtcIso(evaluatedFrom),
        evaluatedToUtc: toUtcIso(evaluatedTo, true),
        page: 1,
        pageSize: 50,
      });

      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setAssessments(result.assessments);
      setSelectedId((currentId) => {
        if (currentId && !result.assessments.some((item) => item.id === currentId)) {
          setDetail(undefined);
          return "";
        }
        return currentId;
      });
    });
  }, [
    evaluatedFrom,
    evaluatedTo,
    machineFilter,
    productionOrderFilter,
    statusFilter,
  ]);

  useEffect(() => {
    reloadAssessments();
  }, [reloadAssessments]);

  function resetFilters() {
    setMachineFilter("");
    setProductionOrderFilter("");
    setStatusFilter("");
    setEvaluatedFrom("");
    setEvaluatedTo("");
  }

  function selectAssessment(item: ProcessOeeAssessmentSummary) {
    setSelectedId(item.id);
    setDetail(undefined);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadProcessOeeAssessmentDetailAction(item.id);
      if (result.status === "success") {
        setDetail(result.assessment);
      } else {
        setError(result.message);
      }
    });
  }

  function runEvaluate(windowId: string) {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const idempotencyKey = crypto.randomUUID();
      const result = await evaluateProcessOeeAction(windowId, idempotencyKey);
      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setMessage("OEE değerlendirmesi tamamlandı.");
      reloadAssessments();
      if (selectedId) {
        const refreshed = await loadProcessOeeAssessmentDetailAction(selectedId);
        if (refreshed.status === "success") {
          setDetail(refreshed.assessment);
        }
      }
    });
  }

  const evaluateWindowId = activeDetail?.evaluationWindowId;

  return (
    <main className="process-oee-page">
      <header className="process-oee-header">
        <div className="process-oee-header__intro">
          <p className="process-oee-header__eyebrow">PROSES VE VERİMLİLİK</p>
          <h1>Proses &amp; OEE</h1>
          <p className="process-oee-header__subtitle">
            Makine ve üretim kanıtlarına dayalı OEE değerlendirmelerini izleyin; veri yeterliliği
            ve hesaplama şeffaflığını kontrol edin.
          </p>
        </div>
        {canManage ? (
          <div className="process-oee-header__actions">
            <button
              type="button"
              className="process-oee-btn process-oee-btn--primary"
              disabled={isPending || !evaluateWindowId}
              onClick={() => evaluateWindowId && runEvaluate(evaluateWindowId)}
            >
              {isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" size={16} /> : null}
              OEE Değerlendir
            </button>
          </div>
        ) : null}
      </header>

      {!canManage ? (
        <div className="process-oee-alert process-oee-alert--info" role="status">
          <div>
            <h3>Salt okunur erişim</h3>
            <p>Bu modülde yalnızca görüntüleme yetkiniz var; değerlendirme işlemleri yapılamaz.</p>
          </div>
        </div>
      ) : null}

      {message ? <div className="process-oee-message" role="status">{message}</div> : null}
      {error ? (
        <div className="process-oee-alert process-oee-alert--error" role="alert">
          <div>
            <h3>İşlem hatası</h3>
            <p>{error}</p>
          </div>
        </div>
      ) : null}

      <section className="process-oee-kpis" aria-label="OEE göstergeleri">
        <article className="process-oee-kpi">
          <span className="process-oee-kpi__label">OEE</span>
          <strong className="process-oee-kpi__value">{formatPercent(kpiOee)}</strong>
        </article>
        <article className="process-oee-kpi">
          <span className="process-oee-kpi__label">Kullanılabilirlik</span>
          <strong className="process-oee-kpi__value">{formatPercent(kpiAvailability)}</strong>
        </article>
        <article className="process-oee-kpi">
          <span className="process-oee-kpi__label">Performans</span>
          <strong className="process-oee-kpi__value">{formatPercent(kpiPerformance)}</strong>
        </article>
        <article className="process-oee-kpi">
          <span className="process-oee-kpi__label">Kalite</span>
          <strong className="process-oee-kpi__value">{formatPercent(kpiQuality)}</strong>
        </article>
        <article className="process-oee-kpi">
          <span className="process-oee-kpi__label">Veri Yeterliliği</span>
          <strong className="process-oee-kpi__value">{kpiDataSufficiency}</strong>
        </article>
      </section>

      <section className="process-oee-filters" aria-label="Değerlendirme filtreleri">
        <div className="process-oee-filter process-oee-filter--wide">
          <label htmlFor="process-oee-machine">Makine</label>
          <select
            id="process-oee-machine"
            value={machineFilter}
            onChange={(event) => setMachineFilter(event.target.value)}
          >
            <option value="">Tüm makineler</option>
            {machineOptions.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div className="process-oee-filter process-oee-filter--wide">
          <label htmlFor="process-oee-order">Üretim emri</label>
          <input
            id="process-oee-order"
            type="text"
            placeholder="Emir referansı"
            value={productionOrderFilter}
            onChange={(event) => setProductionOrderFilter(event.target.value)}
          />
        </div>
        <div className="process-oee-filter process-oee-filter--date">
          <label htmlFor="process-oee-from">Başlangıç</label>
          <input
            id="process-oee-from"
            type="date"
            value={evaluatedFrom}
            onChange={(event) => setEvaluatedFrom(event.target.value)}
          />
        </div>
        <div className="process-oee-filter process-oee-filter--date">
          <label htmlFor="process-oee-to">Bitiş</label>
          <input
            id="process-oee-to"
            type="date"
            value={evaluatedTo}
            onChange={(event) => setEvaluatedTo(event.target.value)}
          />
        </div>
        <div className="process-oee-filter">
          <label htmlFor="process-oee-status">Durum</label>
          <select
            id="process-oee-status"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
          >
            {PROCESS_OEE_STATUS_FILTER_OPTIONS.map((option) => (
              <option key={option.value || "all"} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        {filtersActive ? (
          <button type="button" className="process-oee-btn process-oee-btn--ghost" onClick={resetFilters}>
            <X aria-hidden="true" size={14} />
            Filtreleri temizle
          </button>
        ) : null}
        <span className="process-oee-filters__meta">
          {isPending ? (
            <span className="process-oee-loading">
              <LoaderCircle aria-hidden="true" className="animate-spin" size={14} />
              Yükleniyor…
            </span>
          ) : (
            `${assessments.length} sonuç`
          )}
        </span>
      </section>

      <div className="process-oee-layout">
        <section className="process-oee-table-panel" aria-label="Değerlendirme listesi">
          {assessments.length === 0 && !isPending ? (
            <div className="process-oee-empty">
              <CircleGauge aria-hidden="true" size={28} />
              <h3>Değerlendirme bulunamadı</h3>
              <p>
                {filtersActive
                  ? "Seçili filtrelere uygun kayıt yok. Filtreleri genişletmeyi deneyin."
                  : "Henüz OEE değerlendirmesi oluşturulmamış."}
              </p>
            </div>
          ) : (
            <table className="process-oee-table">
              <thead>
                <tr>
                  <th>Makine</th>
                  <th>Üretim Emri</th>
                  <th>Değerlendirme Aralığı</th>
                  <th>Kullanılabilirlik</th>
                  <th>Performans</th>
                  <th>Kalite</th>
                  <th>OEE</th>
                  <th>Veri Durumu</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {assessments.map((item) => (
                  <tr
                    key={item.id}
                    data-selected={item.id === selectedId ? "true" : "false"}
                    onClick={() => selectAssessment(item)}
                  >
                    <td>{formatMachineLabel(item.machineCode, item.machineDisplayName)}</td>
                    <td>{item.productionOrderReference ?? "—"}</td>
                    <td>{formatProcessOeeDateRange(item.windowStartUtc, item.windowEndUtc)}</td>
                    <td>{formatPercent(item.availabilityPercent)}</td>
                    <td>{formatPercent(item.performancePercent)}</td>
                    <td>{formatPercent(item.qualityPercent)}</td>
                    <td>{formatPercent(item.oeePercent)}</td>
                    <td>
                      <span className={statusBadgeClass(item.status)}>
                        {getProcessOeeStatusLabel(item.status, item.statusLabel)}
                      </span>
                    </td>
                    <td>
                      <div className="process-oee-table-actions">
                        <button
                          type="button"
                          className="process-oee-btn process-oee-btn--sm"
                          onClick={(event) => {
                            event.stopPropagation();
                            selectAssessment(item);
                          }}
                        >
                          Detay
                        </button>
                        {canManage ? (
                          <button
                            type="button"
                            className="process-oee-btn process-oee-btn--sm process-oee-btn--primary"
                            disabled={isPending}
                            onClick={(event) => {
                              event.stopPropagation();
                              selectAssessment(item);
                              startTransition(async () => {
                                const loaded = await loadProcessOeeAssessmentDetailAction(item.id);
                                if (loaded.status === "success") {
                                  runEvaluate(loaded.assessment.evaluationWindowId);
                                }
                              });
                            }}
                          >
                            Değerlendir
                          </button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>

        <aside className="process-oee-detail-panel" aria-label="Değerlendirme detayı">
          <div className="process-oee-detail-panel__header">
            <h2>
              {selectedSummary
                ? formatMachineLabel(selectedSummary.machineCode, selectedSummary.machineDisplayName)
                : "Detay paneli"}
            </h2>
          </div>
          <div className="process-oee-detail-panel__body">
            {!selectedSummary ? (
              <div className="process-oee-empty">
                <p>Listeden bir değerlendirme seçin.</p>
              </div>
            ) : isPending && !activeDetail ? (
              <div className="process-oee-loading">
                <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
                Detay yükleniyor…
              </div>
            ) : activeDetail ? (
              <>
                {activeDetail.status === "insufficient-data" ? (
                  <div className="process-oee-alert process-oee-alert--warning" role="status">
                    <div>
                      <h3>Veri yetersiz</h3>
                      <p>Hesaplama için gerekli alanların bir kısmı eksik; sonuçlar güvenilir olmayabilir.</p>
                    </div>
                  </div>
                ) : null}
                {activeDetail.status === "stale-data" ? (
                  <div className="process-oee-alert process-oee-alert--warning" role="status">
                    <div>
                      <h3>Bayat veri</h3>
                      <p>Kaynak veriler güncel değil; yeniden değerlendirme önerilir.</p>
                    </div>
                  </div>
                ) : null}
                {activeDetail.status === "review-required" ? (
                  <div className="process-oee-alert process-oee-alert--warning" role="status">
                    <div>
                      <h3>İnceleme gerekli</h3>
                      <p>Bu değerlendirme operatör veya süreç sorumlusu incelemesi gerektiriyor.</p>
                    </div>
                  </div>
                ) : null}

                {activeDetail.calculationHonestyDisclaimer ? (
                  <div className="process-oee-disclaimer">{activeDetail.calculationHonestyDisclaimer}</div>
                ) : null}
                {activeDetail.simulatorHonestyDisclaimer ? (
                  <div className="process-oee-disclaimer">{activeDetail.simulatorHonestyDisclaimer}</div>
                ) : null}

                <section className="process-oee-detail-section">
                  <h3>Genel Bakış</h3>
                  <dl className="process-oee-detail-grid">
                    <div>
                      <dt>Makine</dt>
                      <dd>{formatMachineLabel(activeDetail.machineCode, activeDetail.machineDisplayName)}</dd>
                    </div>
                    <div>
                      <dt>Üretim emri</dt>
                      <dd>{activeDetail.productionOrderReference ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Değerlendirme aralığı</dt>
                      <dd>{formatProcessOeeDateRange(activeDetail.windowStartUtc, activeDetail.windowEndUtc)}</dd>
                    </div>
                    <div>
                      <dt>Veri durumu</dt>
                      <dd>
                        <span className={statusBadgeClass(activeDetail.status)}>
                          {getProcessOeeStatusLabel(activeDetail.status, activeDetail.statusLabel)}
                        </span>
                      </dd>
                    </div>
                    <div>
                      <dt>OEE</dt>
                      <dd>{formatPercent(activeDetail.oeePercent)}</dd>
                    </div>
                    <div>
                      <dt>Değerlendirme zamanı</dt>
                      <dd>{formatProcessOeeDateTime(activeDetail.evaluatedAtUtc)}</dd>
                    </div>
                  </dl>
                </section>

                <section className="process-oee-detail-section">
                  <h3>Hesaplama Kanıtları</h3>
                  {activeDetail.drivers.length > 0 ? (
                    <ul className="process-oee-driver-list">
                      {activeDetail.drivers.map((driver) => (
                        <li key={driver.code}>
                          <strong>{driver.label}</strong>
                          {" · "}
                          {driver.value ?? "—"}
                          {" · "}
                          {getDriverSeverityLabel(driver.severity)}
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p>—</p>
                  )}
                  {activeDetail.missingFields.length > 0 ? (
                    <p>Eksik alanlar: {activeDetail.missingFields.join(", ")}</p>
                  ) : null}
                  {activeDetail.conflicts.length > 0 ? (
                    <p>Çakışmalar: {activeDetail.conflicts.join(", ")}</p>
                  ) : null}
                  {activeDetail.evidence.length > 0 ? (
                    <ul className="process-oee-evidence-list">
                      {activeDetail.evidence.map((item) => (
                        <li
                          key={item.field}
                          data-missing={item.isMissing ? "true" : "false"}
                          data-conflict={item.isConflict ? "true" : "false"}
                        >
                          <strong>{item.field}</strong>: {item.value ?? "—"}
                          {item.source ? ` · Kaynak: ${item.source}` : ""}
                          {item.recordedAtUtc
                            ? ` · ${formatProcessOeeDateTime(item.recordedAtUtc)}`
                            : ""}
                          {item.conflictReason ? ` · ${item.conflictReason}` : ""}
                        </li>
                      ))}
                    </ul>
                  ) : null}
                </section>

                <section className="process-oee-detail-section">
                  <h3>Zaman ve Duruş</h3>
                  <dl className="process-oee-detail-grid">
                    <div>
                      <dt>Planlanan üretim süresi</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.plannedProductionSeconds)} sn</dd>
                    </div>
                    <div>
                      <dt>Çalışma süresi</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.operatingSeconds)} sn</dd>
                    </div>
                    <div>
                      <dt>Duruş süresi</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.downtimeSeconds)} sn</dd>
                    </div>
                    <div>
                      <dt>Bilinmeyen süre</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.unknownSeconds)} sn</dd>
                    </div>
                  </dl>
                </section>

                <section className="process-oee-detail-section">
                  <h3>Çevrim ve Üretim</h3>
                  <dl className="process-oee-detail-grid">
                    <div>
                      <dt>Gözlemlenen çevrim</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.observedCycleCount)}</dd>
                    </div>
                    <div>
                      <dt>Türetilen vuruş</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.derivedShotCount)}</dd>
                    </div>
                    <div>
                      <dt>Toplam parça</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.totalPieceCount)}</dd>
                    </div>
                    <div>
                      <dt>İyi parça</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.goodPieceCount)}</dd>
                    </div>
                    <div>
                      <dt>Hurda parça</dt>
                      <dd>{formatProcessOeeNumber(activeDetail.scrapPieceCount)}</dd>
                    </div>
                    <div>
                      <dt>Performans (ham)</dt>
                      <dd>{formatPercent(activeDetail.performanceRawPercent)}</dd>
                    </div>
                  </dl>
                </section>

                <section className="process-oee-detail-section">
                  <h3>Veri Kaynakları</h3>
                  <dl className="process-oee-detail-grid">
                    <div>
                      <dt>Kaynak sistem</dt>
                      <dd>{activeDetail.sourceSystem ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Veri tazeliği</dt>
                      <dd>{formatProcessOeeDateTime(activeDetail.sourceFreshnessUtc)}</dd>
                    </div>
                    <div>
                      <dt>Simülatör türevi</dt>
                      <dd>{activeDetail.isSimulatorDerived ? "Evet" : "Hayır"}</dd>
                    </div>
                    <div>
                      <dt>Kural seti</dt>
                      <dd>{activeDetail.ruleSetVersion}</dd>
                    </div>
                  </dl>
                </section>
              </>
            ) : (
              <div className="process-oee-empty">
                <p>Detay yüklenemedi.</p>
              </div>
            )}
          </div>
        </aside>
      </div>
    </main>
  );
}

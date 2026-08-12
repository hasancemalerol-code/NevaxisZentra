"use client";

import { useCallback, useEffect, useMemo, useRef, useState, useTransition } from "react";
import { CircleGauge, LoaderCircle, Search, X } from "lucide-react";

import type {
  ProcessOeeAssessmentDetail,
  ProcessOeeAssessmentSummary,
  ProcessOeeLossDetail,
  ProcessOeeShiftComparison,
  ProcessOeeStabilityDetail,
  ProcessOeeWorkspaceSummary,
} from "../domain/process-oee";
import {
  formatCycleMs,
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
  loadProcessOeeLossDetailAction,
  loadProcessOeeShiftComparisonAction,
  loadProcessOeeStabilityDetailAction,
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

type DetailTab =
  | "overview"
  | "loss"
  | "stability"
  | "downtime"
  | "shift"
  | "evidence";

type AttentionGroup = Readonly<{
  id: string;
  assessmentId: string;
  label: string;
  summary: string;
  meta: string;
  tone: "critical" | "warning" | "info";
  priority: number;
  tab: DetailTab;
  count: number;
}>;

const DETAIL_TABS: readonly { id: DetailTab; label: string; panelTitle?: string }[] = [
  { id: "overview", label: "Genel Bakış" },
  { id: "loss", label: "OEE Kayıp Analizi" },
  { id: "stability", label: "Proses Kararlılığı" },
  { id: "downtime", label: "Duruş Analizi" },
  { id: "shift", label: "Vardiya Karşılaştırması" },
  { id: "evidence", label: "Kanıtlar", panelTitle: "Hesaplama Kanıtları" },
];

const ATTENTION_INITIAL_LIMIT = 3;

function stabilityBadgeClass(state: string): string {
  switch (state) {
    case "stable":
      return "process-oee-badge process-oee-badge--calculated";
    case "variable":
    case "drifting":
    case "review-required":
      return "process-oee-badge process-oee-badge--review";
    case "insufficient-data":
      return "process-oee-badge process-oee-badge--insufficient";
    default:
      return "process-oee-badge process-oee-badge--stale";
  }
}

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
      map.set(item.machineAssetId, formatMachineLabel(item.machineCode, item.machineDisplayName));
    }
  }

  return [...map.entries()]
    .map(([id, label]) => ({ id, label }))
    .sort((a, b) => a.label.localeCompare(b.label, "tr"));
}

function formatCompactPeriod(startUtc: string, endUtc: string): string {
  const start = new Date(startUtc);
  const end = new Date(endUtc);
  const opts: Intl.DateTimeFormatOptions = { day: "2-digit", month: "short" };
  return `${start.toLocaleDateString("tr-TR", opts)} – ${end.toLocaleDateString("tr-TR", opts)}`;
}

function representativeMeta(item: ProcessOeeAssessmentSummary, extraCount: number): string {
  const machine = formatMachineLabel(item.machineCode, item.machineDisplayName);
  const period = formatCompactPeriod(item.windowStartUtc, item.windowEndUtc);
  if (extraCount > 0) {
    return `${machine} · ${period} · +${extraCount} diğer`;
  }
  return `${machine} · ${period}`;
}

function buildStatusGroup(
  key: string,
  items: readonly ProcessOeeAssessmentSummary[],
  label: string,
  countLabel: string,
  priority: number,
  tab: DetailTab,
  tone: AttentionGroup["tone"],
): AttentionGroup | null {
  if (items.length === 0) {
    return null;
  }

  const first = items[0];
  return {
    id: key,
    assessmentId: first.id,
    label,
    summary: items.length > 1 ? `${items.length} değerlendirmede ${countLabel}` : label,
    meta: representativeMeta(first, items.length - 1),
    tone,
    priority,
    tab,
    count: items.length,
  };
}

function buildAttentionGroups(
  assessments: readonly ProcessOeeAssessmentSummary[],
  selectedId: string,
  lossDetail?: ProcessOeeLossDetail,
  stabilityDetail?: ProcessOeeStabilityDetail,
): AttentionGroup[] {
  const groups: AttentionGroup[] = [];

  const review = assessments.filter((item) => item.status === "review-required");
  const stale = assessments.filter((item) => item.status === "stale-data");
  const insufficient = assessments.filter((item) => item.status === "insufficient-data");

  const reviewGroup = buildStatusGroup(
    "review",
    review,
    "İnceleme gerekli",
    "inceleme gerekli",
    10,
    "overview",
    "critical",
  );
  if (reviewGroup) {
    groups.push(reviewGroup);
  }

  if (
    selectedId
    && lossDetail?.drivers.some((driver) => driver.category === "source-conflict")
  ) {
    const selected = assessments.find((item) => item.id === selectedId);
    if (selected) {
      groups.push({
        id: "source-conflict-selected",
        assessmentId: selected.id,
        label: "Kaynak çelişkisi",
        summary: "Seçili değerlendirmede kaynak çelişkisi",
        meta: representativeMeta(selected, 0),
        tone: "critical",
        priority: 15,
        tab: "loss",
        count: 1,
      });
    }
  }

  if (
    selectedId
    && stabilityDetail
    && (stabilityDetail.resultState === "drifting" || stabilityDetail.resultState === "variable")
  ) {
    const selected = assessments.find((item) => item.id === selectedId);
    if (selected) {
      groups.push({
        id: "stability-drift-selected",
        assessmentId: selected.id,
        label: "Proses kararsızlığı",
        summary: stabilityDetail.resultStateLabel,
        meta: representativeMeta(selected, 0),
        tone: "warning",
        priority: 25,
        tab: "stability",
        count: 1,
      });
    }
  }

  const staleGroup = buildStatusGroup(
    "stale",
    stale,
    "Veri bayat",
    "veri bayat",
    40,
    "overview",
    "warning",
  );
  if (staleGroup) {
    groups.push(staleGroup);
  }

  const insufficientGroup = buildStatusGroup(
    "insufficient",
    insufficient,
    "Veri yetersiz",
    "veri yetersiz",
    50,
    "overview",
    "warning",
  );
  if (insufficientGroup) {
    groups.push(insufficientGroup);
  }

  return groups.sort((a, b) => a.priority - b.priority || a.label.localeCompare(b.label, "tr"));
}

function toUtcIso(dateValue: string, endOfDay = false): string | undefined {
  if (!dateValue) {
    return undefined;
  }

  const suffix = endOfDay ? "T23:59:59.999Z" : "T00:00:00.000Z";
  return new Date(`${dateValue}${suffix}`).toISOString();
}

function getTopBarPx(): number {
  const topBarRaw = getComputedStyle(document.documentElement).getPropertyValue("--top-bar-height").trim();
  return Number.parseFloat(topBarRaw) || 72;
}

export function ProcessOeeView({
  summary,
  assessments: initialAssessments,
  canManage,
}: ProcessOeeViewProps) {
  const [assessments, setAssessments] = useState(initialAssessments);
  const [search, setSearch] = useState("");
  const [machineFilter, setMachineFilter] = useState("");
  const [productionOrderFilter, setProductionOrderFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [evaluatedFrom, setEvaluatedFrom] = useState("");
  const [evaluatedTo, setEvaluatedTo] = useState("");
  const [selectedId, setSelectedId] = useState("");
  const [activeTab, setActiveTab] = useState<DetailTab>("overview");
  const [detail, setDetail] = useState<ProcessOeeAssessmentDetail | undefined>();
  const [lossDetail, setLossDetail] = useState<ProcessOeeLossDetail | undefined>();
  const [stabilityDetail, setStabilityDetail] = useState<ProcessOeeStabilityDetail | undefined>();
  const [shiftComparison, setShiftComparison] = useState<ProcessOeeShiftComparison | undefined>();
  const [shiftComparisonError, setShiftComparisonError] = useState<string | null>(null);
  const [stickyContextVisible, setStickyContextVisible] = useState(false);
  const [attentionExpanded, setAttentionExpanded] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();
  const detailHeroRef = useRef<HTMLElement | null>(null);

  const filtersActive = Boolean(
    search.trim() || machineFilter || productionOrderFilter || statusFilter || evaluatedFrom || evaluatedTo,
  );

  const machineOptions = useMemo(() => buildMachineOptions(assessments), [assessments]);
  const attentionGroups = useMemo(
    () => buildAttentionGroups(assessments, selectedId, lossDetail, stabilityDetail),
    [assessments, selectedId, lossDetail, stabilityDetail],
  );
  const visibleAttentionGroups = attentionExpanded
    ? attentionGroups
    : attentionGroups.slice(0, ATTENTION_INITIAL_LIMIT);

  const filteredAssessments = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return assessments;
    }

    return assessments.filter((item) => {
      const machine = formatMachineLabel(item.machineCode, item.machineDisplayName).toLowerCase();
      const order = (item.productionOrderReference ?? "").toLowerCase();
      return machine.includes(query) || order.includes(query);
    });
  }, [assessments, search]);

  const activeDetail = detail && detail.id === selectedId ? detail : undefined;
  const selectedSummary =
    assessments.find((item) => item.id === selectedId) ??
    initialAssessments.find((item) => item.id === selectedId);
  const detailOpen = Boolean(selectedSummary);

  const kpiOee = activeDetail?.oeePercent ?? summary.latestOeePercent;
  const kpiAvailability = activeDetail?.availabilityPercent ?? null;
  const kpiPerformance = activeDetail?.performancePercent ?? null;
  const kpiQuality = activeDetail?.qualityPercent ?? null;
  const kpiWindowLabel = selectedSummary
    ? formatProcessOeeDateRange(selectedSummary.windowStartUtc, selectedSummary.windowEndUtc)
    : `${summary.assessmentCount} kayıt`;
  const kpiDataSufficiency = formatDataSufficiencyPercent(
    summary.calculatedCount,
    summary.assessmentCount,
  );

  const activeTabLabel =
    DETAIL_TABS.find((tab) => tab.id === activeTab)?.panelTitle
    ?? DETAIL_TABS.find((tab) => tab.id === activeTab)?.label
    ?? "Genel Bakış";
  const freshnessLabel = activeDetail?.sourceFreshnessUtc
    ? formatProcessOeeDateTime(activeDetail.sourceFreshnessUtc)
    : null;

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
  }, [evaluatedFrom, evaluatedTo, machineFilter, productionOrderFilter, statusFilter]);

  useEffect(() => {
    reloadAssessments();
  }, [reloadAssessments]);

  useEffect(() => {
    const hero = detailHeroRef.current;
    if (!hero || !detailOpen) {
      setStickyContextVisible(false);
      return;
    }

    const updateStickyVisibility = () => {
      const rect = hero.getBoundingClientRect();
      setStickyContextVisible(rect.bottom <= getTopBarPx() + 1);
    };

    updateStickyVisibility();
    window.addEventListener("scroll", updateStickyVisibility, { passive: true });
    window.addEventListener("resize", updateStickyVisibility);
    return () => {
      window.removeEventListener("scroll", updateStickyVisibility);
      window.removeEventListener("resize", updateStickyVisibility);
    };
  }, [detailOpen, selectedId, activeTab]);

  function resetFilters() {
    setSearch("");
    setMachineFilter("");
    setProductionOrderFilter("");
    setStatusFilter("");
    setEvaluatedFrom("");
    setEvaluatedTo("");
  }

  function focusSelectedDetail() {
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        const target = document.getElementById("process-oee-detail-focus-target");
        if (!target) return;
        target.focus({ preventScroll: true });
        const nextTop = window.scrollY + target.getBoundingClientRect().top - getTopBarPx() - 8;
        window.scrollTo({ top: Math.max(0, nextTop), behavior: "auto" });
      });
    });
  }

  function clearSelection() {
    const restoreId = selectedId;
    setSelectedId("");
    setDetail(undefined);
    setLossDetail(undefined);
    setStabilityDetail(undefined);
    setShiftComparison(undefined);
    setShiftComparisonError(null);
    setActiveTab("overview");
    setStickyContextVisible(false);
    setMessage(null);
    setError(null);

    requestAnimationFrame(() => {
      const row = document.querySelector(
        `[data-assessment-id="${CSS.escape(restoreId)}"]`,
      ) as HTMLElement | null;
      row?.focus({ preventScroll: true });
    });
  }

  function selectAssessment(item: ProcessOeeAssessmentSummary, tab: DetailTab = "overview") {
    setSelectedId(item.id);
    setActiveTab(tab);
    setDetail(undefined);
    setLossDetail(undefined);
    setStabilityDetail(undefined);
    setShiftComparison(undefined);
    setShiftComparisonError(null);
    setMessage(null);
    setError(null);
    focusSelectedDetail();

    startTransition(async () => {
      const [result, lossResult, stabilityResult, shiftResult] = await Promise.all([
        loadProcessOeeAssessmentDetailAction(item.id),
        loadProcessOeeLossDetailAction(item.id),
        loadProcessOeeStabilityDetailAction(item.id),
        loadProcessOeeShiftComparisonAction({
          machineAssetId: item.machineAssetId,
          periodStartUtc: item.windowStartUtc,
          periodEndUtc: item.windowEndUtc,
        }),
      ]);
      if (result.status === "success") {
        setDetail(result.assessment);
      } else {
        setError(result.message);
      }
      if (lossResult.status === "success") {
        setLossDetail(lossResult.lossDetail);
      }
      if (stabilityResult.status === "success") {
        setStabilityDetail(stabilityResult.stabilityDetail);
      }
      if (shiftResult.status === "success") {
        setShiftComparison(shiftResult.shiftComparison);
      } else {
        setShiftComparisonError(shiftResult.message);
      }
    });
  }

  function selectTab(next: DetailTab) {
    setActiveTab(next);
    requestAnimationFrame(() => {
      const panel = document.getElementById(`process-oee-tab-panel-${next}`);
      if (!panel) return;
      const sticky = document.querySelector(".process-oee-sticky-context.is-visible") as HTMLElement | null;
      const stickyH = sticky?.getBoundingClientRect().height || 0;
      const offset = getTopBarPx() + stickyH + 8;
      const nextTop = window.scrollY + panel.getBoundingClientRect().top - offset;
      window.scrollTo({ top: Math.max(0, nextTop), behavior: "auto" });
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
        const selected = assessments.find((item) => item.id === selectedId);
        const [refreshed, refreshedLoss, refreshedStability, refreshedShift] = await Promise.all([
          loadProcessOeeAssessmentDetailAction(selectedId),
          loadProcessOeeLossDetailAction(selectedId),
          loadProcessOeeStabilityDetailAction(selectedId),
          selected
            ? loadProcessOeeShiftComparisonAction({
                machineAssetId: selected.machineAssetId,
                periodStartUtc: selected.windowStartUtc,
                periodEndUtc: selected.windowEndUtc,
              })
            : Promise.resolve({ status: "failure" as const, message: "" }),
        ]);
        if (refreshed.status === "success") {
          setDetail(refreshed.assessment);
        }
        if (refreshedLoss.status === "success") {
          setLossDetail(refreshedLoss.lossDetail);
        }
        if (refreshedStability.status === "success") {
          setStabilityDetail(refreshedStability.stabilityDetail);
        }
        if (refreshedShift.status === "success") {
          setShiftComparison(refreshedShift.shiftComparison);
        }
      }
    });
  }

  const evaluateWindowId = activeDetail?.evaluationWindowId;

  function renderDataContextNotice(detailItem: ProcessOeeAssessmentDetail) {
    const contextLines: string[] = [];

    if (detailItem.isSimulatorDerived) {
      contextLines.push("Simülatör türevi veri kullanılıyor.");
    }
    if (detailItem.calculationHonestyDisclaimer) {
      contextLines.push(detailItem.calculationHonestyDisclaimer);
    }
    if (detailItem.simulatorHonestyDisclaimer) {
      contextLines.push(detailItem.simulatorHonestyDisclaimer);
    }
    if (detailItem.status === "insufficient-data") {
      contextLines.push("Hesaplama için gerekli alanların bir kısmı eksik; sonuçlar güvenilir olmayabilir.");
    }
    if (detailItem.status === "stale-data") {
      contextLines.push("Kaynak veriler güncel değil; yeniden değerlendirme önerilir.");
    }
    if (detailItem.status === "review-required") {
      contextLines.push("Bu değerlendirme operasyonel inceleme gerektiriyor.");
    }
    if (lossDetail?.drivers.some((driver) => driver.category === "source-conflict")) {
      contextLines.push("Kaynak çakışması tespit edildi; otomatik uzlaştırma yapılmadı.");
    }
    if (stabilityDetail?.resultState === "insufficient-data") {
      contextLines.push("Yetersiz veya bayat kanıt; normal sonuç üretilmedi.");
    }

    if (contextLines.length === 0) {
      return null;
    }

    return (
      <div className="process-oee-data-context" role="status" aria-label="Veri Bağlamı">
        <h4 className="process-oee-data-context__title">Veri Bağlamı</h4>
        <ul className="process-oee-data-context__list">
          {contextLines.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      </div>
    );
  }

  function renderOverviewTab(detailItem: ProcessOeeAssessmentDetail) {
    return (
      <div className="process-oee-tab-panel" id="process-oee-tab-panel-overview" role="tabpanel">
        <section className="process-oee-detail-section" aria-label="OEE bileşen ilişkisi">
          <h3>OEE bileşen ilişkisi</h3>
          <div className="process-oee-overview-relationship">
            <article>
              <span>Kullanılabilirlik</span>
              <strong>{formatPercent(detailItem.availabilityPercent)}</strong>
            </article>
            <article>
              <span>Performans</span>
              <strong>{formatPercent(detailItem.performancePercent)}</strong>
            </article>
            <article>
              <span>Kalite</span>
              <strong>{formatPercent(detailItem.qualityPercent)}</strong>
            </article>
          </div>
        </section>
        <section className="process-oee-detail-section">
          <h3>Değerlendirme özeti</h3>
          <dl className="process-oee-detail-grid process-oee-detail-grid--3">
            <div>
              <dt>Üretim emri</dt>
              <dd>{detailItem.productionOrderReference ?? "—"}</dd>
            </div>
            <div>
              <dt>Kalıp referansı</dt>
              <dd>{detailItem.moldReference ?? "—"}</dd>
            </div>
            <div>
              <dt>Planlanan süre</dt>
              <dd>{formatProcessOeeNumber(detailItem.plannedProductionSeconds)} sn</dd>
            </div>
            <div>
              <dt>Çalışma süresi</dt>
              <dd>{formatProcessOeeNumber(detailItem.operatingSeconds)} sn</dd>
            </div>
            <div>
              <dt>Kaynak sistemi</dt>
              <dd>{detailItem.sourceSystem ?? "—"}</dd>
            </div>
            <div>
              <dt>Kural seti</dt>
              <dd>{detailItem.ruleSetVersion}</dd>
            </div>
          </dl>
        </section>
        {detailItem.missingFields.length > 0 ? (
          <p className="process-oee-empty-inline">
            Kaynak sınırlaması: eksik alanlar mevcut; ayrıntılar Hesaplama Kanıtları sekmesinde.
          </p>
        ) : null}
      </div>
    );
  }

  function renderLossTab() {
    return (
      <div className="process-oee-tab-panel" id="process-oee-tab-panel-loss" role="tabpanel">
        <section className="process-oee-detail-section" aria-label="OEE Kayıp Analizi">
          {!lossDetail ? (
            <p className="process-oee-empty-inline">Kayıp analizi yükleniyor veya mevcut değil.</p>
          ) : (
            <>
              <dl className="process-oee-loss-kpi-grid">
                <div>
                  <dt>Kullanılabilirlik kaybı</dt>
                  <dd>
                    {formatProcessOeeNumber(
                      lossDetail.drivers.find((d) => d.category === "availability-loss")
                        ?.affectedDurationSeconds ?? null,
                    )}{" "}
                    sn
                  </dd>
                </div>
                <div>
                  <dt>Performans kaybı</dt>
                  <dd>
                    {formatProcessOeeNumber(
                      lossDetail.drivers.find((d) => d.category === "performance-loss")
                        ?.affectedDurationSeconds ?? null,
                    )}{" "}
                    sn
                  </dd>
                </div>
                <div>
                  <dt>Kalite kaybı</dt>
                  <dd>
                    {formatProcessOeeNumber(
                      lossDetail.drivers.find((d) => d.category === "quality-loss")
                        ?.affectedQuantity ?? null,
                    )}{" "}
                    adet
                  </dd>
                </div>
              </dl>

              {lossDetail.leadingLoss ? (
                <div className="process-oee-loss-leading" role="status">
                  <h4>Öncelikli kayıp</h4>
                  <p>
                    <strong>{lossDetail.leadingLoss.categoryLabel}</strong>
                    {" · "}
                    {lossDetail.leadingLoss.explanationLabel}
                  </p>
                  {lossDetail.leadingLoss.normalizedImpactPoints != null ? (
                    <p>
                      Normalleştirilmiş OEE etkisi:{" "}
                      {formatPercent(lossDetail.leadingLoss.normalizedImpactPoints)}
                    </p>
                  ) : null}
                </div>
              ) : null}

              {lossDetail.drivers.some((d) => d.category === "insufficient-data") ? (
                <div className="process-oee-alert process-oee-alert--warning" role="status">
                  <p>Kayıp analizi için bazı kanıtlar yetersiz; sonuç uydurulmadı.</p>
                </div>
              ) : null}
              {lossDetail.drivers.some((d) => d.category === "source-conflict") ? (
                <div className="process-oee-alert process-oee-alert--warning" role="status">
                  <p>Kaynak çakışması tespit edildi; otomatik uzlaştırma yapılmadı.</p>
                </div>
              ) : null}
              {lossDetail.drivers.some((d) => d.freshnessState === "stale") ? (
                <div className="process-oee-alert process-oee-alert--warning" role="status">
                  <p>Bayat kanıt kullanıldı; kayıp analizi sınırlı güvenilirlikte.</p>
                </div>
              ) : null}

              {lossDetail.drivers.length > 0 ? (
                <ul className="process-oee-loss-list" aria-label="Katkı veren kayıplar">
                  {lossDetail.drivers.map((driver) => (
                    <li key={driver.reasonCode} data-leading={driver.isLeadingLoss ? "true" : "false"}>
                      <strong>{driver.categoryLabel}</strong>
                      {" · "}
                      {driver.explanationLabel}
                      {" · "}
                      {driver.severityLabel}
                      {driver.sourceReferences.length > 0
                        ? ` · Kaynak: ${driver.sourceReferences.join(", ")}`
                        : ""}
                      {driver.evidenceTimestampUtc
                        ? ` · ${formatProcessOeeDateTime(driver.evidenceTimestampUtc)}`
                        : ""}
                      {driver.freshnessLabel ? ` · Tazelik: ${driver.freshnessLabel}` : ""}
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="process-oee-empty-inline">—</p>
              )}
              <p className="process-oee-rule-set">Kural seti: {lossDetail.ruleSetVersion}</p>
            </>
          )}
        </section>
      </div>
    );
  }

  function renderStabilityTab() {
    return (
      <div className="process-oee-tab-panel" id="process-oee-tab-panel-stability" role="tabpanel">
        <section className="process-oee-detail-section" aria-label="Proses Kararlılığı">
          {!stabilityDetail ? (
            <p className="process-oee-empty-inline">
              {isPending
                ? "Kararlılık analizi yükleniyor…"
                : "Kararlılık analizi mevcut değil; önce değerlendirme gerekir."}
            </p>
          ) : (
            <>
              <dl className="process-oee-loss-kpi-grid">
                <div>
                  <dt>Kararlılık durumu</dt>
                  <dd>
                    <span className={stabilityBadgeClass(stabilityDetail.resultState)}>
                      {stabilityDetail.resultStateLabel}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt>Çevrim örneği</dt>
                  <dd>{formatProcessOeeNumber(stabilityDetail.sampleCount)}</dd>
                </div>
                <div>
                  <dt>Medyan çevrim</dt>
                  <dd>{formatCycleMs(stabilityDetail.medianCycleMs)}</dd>
                </div>
                <div>
                  <dt>Değişkenlik (MAD)</dt>
                  <dd>{formatCycleMs(stabilityDetail.madCycleMs)}</dd>
                </div>
                <div>
                  <dt>Kayma yönü</dt>
                  <dd>{stabilityDetail.driftDirectionLabel}</dd>
                </div>
                <div>
                  <dt>Veri tazeliği</dt>
                  <dd>{formatProcessOeeDateTime(stabilityDetail.sourceFreshnessUtc)}</dd>
                </div>
              </dl>

              {stabilityDetail.resultState === "insufficient-data" ? (
                <div className="process-oee-alert process-oee-alert--warning" role="status">
                  <p>Yetersiz veya bayat kanıt; normal sonuç üretilmedi.</p>
                </div>
              ) : null}

              {stabilityDetail.drivers.length > 0 ? (
                <ul className="process-oee-loss-list" aria-label="Kararlılık kanıtları">
                  {stabilityDetail.drivers.map((driver) => (
                    <li key={driver.reasonCode}>
                      <strong>{driver.categoryLabel}</strong>
                      {" · "}
                      {driver.explanationLabel}
                      {" · "}
                      {driver.resultStateLabel}
                      {driver.dataLimitations.length > 0
                        ? ` · ${driver.dataLimitations.join("; ")}`
                        : ""}
                    </li>
                  ))}
                </ul>
              ) : null}
              <p className="process-oee-rule-set">Kural seti: {stabilityDetail.ruleSetVersion}</p>
            </>
          )}
        </section>
      </div>
    );
  }

  function renderDowntimeTab() {
    return (
      <div className="process-oee-tab-panel" id="process-oee-tab-panel-downtime" role="tabpanel">
        <section className="process-oee-detail-section" aria-label="Duruş Analizi">
          {!stabilityDetail ? (
            <p className="process-oee-empty-inline">Duruş analizi yükleniyor veya mevcut değil.</p>
          ) : (
            <>
              <dl className="process-oee-loss-kpi-grid">
                <div>
                  <dt>Mikro duruş</dt>
                  <dd>
                    {formatProcessOeeNumber(stabilityDetail.microStopCount)} adet ·{" "}
                    {formatProcessOeeNumber(stabilityDetail.microStopTotalSeconds)} sn
                  </dd>
                </div>
                <div>
                  <dt>Medyan mikro duruş</dt>
                  <dd>{formatProcessOeeNumber(stabilityDetail.microStopMedianSeconds)} sn</dd>
                </div>
                <div>
                  <dt>Sınıflı / sınıfsız duruş</dt>
                  <dd>
                    {formatProcessOeeNumber(stabilityDetail.classifiedDowntimeSeconds)} /{" "}
                    {formatProcessOeeNumber(stabilityDetail.unclassifiedDowntimeSeconds)} sn
                  </dd>
                </div>
              </dl>

              {stabilityDetail.downtimeBreakdown.length > 0 ? (
                <div>
                  <h3>Duruş Pareto</h3>
                  <p className="process-oee-shift-period">
                    Analiz edilen toplam duruş:{" "}
                    {formatProcessOeeNumber(
                      stabilityDetail.classifiedDowntimeSeconds +
                        stabilityDetail.unclassifiedDowntimeSeconds,
                    )}{" "}
                    sn
                  </p>
                  <ul className="process-oee-pareto-list">
                    {stabilityDetail.downtimeBreakdown.map((row) => (
                      <li key={row.reasonCode} className="process-oee-pareto-row">
                        <div>
                          <strong>{row.reasonLabel}</strong>
                          {" · "}
                          {formatProcessOeeNumber(row.durationSeconds)} sn
                          {" · "}
                          {formatProcessOeeNumber(row.occurrenceCount)} olay
                          {row.durationPercent != null ? ` · %${row.durationPercent.toFixed(1)}` : ""}
                        </div>
                        <div className="process-oee-pareto-row__bar-wrap" aria-hidden="true">
                          <div
                            className="process-oee-pareto-row__bar"
                            style={{ width: `${row.durationPercent ?? 0}%` }}
                          />
                        </div>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : (
                <p className="process-oee-empty-inline">Duruş Pareto verisi mevcut değil.</p>
              )}
            </>
          )}
        </section>
      </div>
    );
  }

  function renderShiftTab() {
    return (
      <div className="process-oee-tab-panel" id="process-oee-tab-panel-shift" role="tabpanel">
        <section className="process-oee-detail-section" aria-label="Vardiya Karşılaştırması">
          {shiftComparisonError ? (
            <div className="process-oee-alert process-oee-alert--warning" role="status">
              <p>{shiftComparisonError}</p>
            </div>
          ) : null}
          {!shiftComparison ? (
            <p className="process-oee-empty-inline">
              {isPending
                ? "Vardiya karşılaştırması yükleniyor…"
                : "Karşılaştırılabilir vardiya penceresi bulunamadı."}
            </p>
          ) : (
            <>
              <p className="process-oee-shift-period">
                Dönem:{" "}
                {formatProcessOeeDateRange(
                  shiftComparison.periodStartUtc,
                  shiftComparison.periodEndUtc,
                )}
              </p>
              {!shiftComparison.isComparable && shiftComparison.comparabilityWarningLabel ? (
                <div className="process-oee-alert process-oee-alert--warning" role="status">
                  <p>{shiftComparison.comparabilityWarningLabel}</p>
                </div>
              ) : null}
              {shiftComparison.rows.length === 0 ? (
                <p className="process-oee-empty-inline">Vardiya satırı yok.</p>
              ) : (
                <div className="process-oee-shift-table-wrap">
                  <table className="process-oee-shift-table">
                    <thead>
                      <tr>
                        <th>Vardiya</th>
                        <th>OEE</th>
                        <th>Kullanılabilirlik</th>
                        <th>Performans</th>
                        <th>Kalite</th>
                        <th>Kararlılık</th>
                        <th>Duruş</th>
                        <th>Mikro duruş</th>
                      </tr>
                    </thead>
                    <tbody>
                      {shiftComparison.rows.map((row) => (
                        <tr key={`${row.shiftCode}-${row.windowStartUtc}`}>
                          <td>{row.shiftLabel}</td>
                          <td>{formatPercent(row.oeePercent)}</td>
                          <td>{formatPercent(row.availabilityPercent)}</td>
                          <td>{formatPercent(row.performancePercent)}</td>
                          <td>{formatPercent(row.qualityPercent)}</td>
                          <td>{row.stabilityResultStateLabel ?? "—"}</td>
                          <td>{formatProcessOeeNumber(row.downtimeSeconds)} sn</td>
                          <td>{formatProcessOeeNumber(row.microStopCount)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              {shiftComparison.summary ? (
                <dl className="process-oee-detail-grid">
                  {shiftComparison.summary.highestOeeShiftLabel ? (
                    <div>
                      <dt>En yüksek desteklenen OEE</dt>
                      <dd>{shiftComparison.summary.highestOeeShiftLabel}</dd>
                    </div>
                  ) : null}
                  {shiftComparison.summary.lowestAvailabilityShiftLabel ? (
                    <div>
                      <dt>En düşük kullanılabilirlik</dt>
                      <dd>{shiftComparison.summary.lowestAvailabilityShiftLabel}</dd>
                    </div>
                  ) : null}
                </dl>
              ) : null}
              <p className="process-oee-rule-set">Kural seti: {shiftComparison.ruleSetVersion}</p>
            </>
          )}
        </section>
      </div>
    );
  }

  function renderEvidenceTab(detailItem: ProcessOeeAssessmentDetail) {
    return (
      <div className="process-oee-tab-panel" id="process-oee-tab-panel-evidence" role="tabpanel">
        <section className="process-oee-detail-section">
          <h3>Hesaplama Kanıtları</h3>
          <h4 className="process-oee-detail-subheading">Hesaplama sürücüleri</h4>
          {detailItem.drivers.length > 0 ? (
            <ul className="process-oee-driver-list">
              {detailItem.drivers.map((driver) => (
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
            <p className="process-oee-empty-inline">—</p>
          )}
        </section>
        {detailItem.missingFields.length > 0 ? (
          <p className="process-oee-empty-inline">Eksik alanlar: {detailItem.missingFields.join(", ")}</p>
        ) : null}
        {detailItem.conflicts.length > 0 ? (
          <p className="process-oee-empty-inline">Çakışmalar: {detailItem.conflicts.join(", ")}</p>
        ) : null}
        {detailItem.evidence.length > 0 ? (
          <ul className="process-oee-evidence-list">
            {detailItem.evidence.map((item) => (
              <li
                key={item.field}
                data-missing={item.isMissing ? "true" : "false"}
                data-conflict={item.isConflict ? "true" : "false"}
              >
                <strong>{item.field}</strong>: {item.value ?? "—"}
                {item.source ? ` · Kaynak: ${item.source}` : ""}
                {item.recordedAtUtc ? ` · ${formatProcessOeeDateTime(item.recordedAtUtc)}` : ""}
                {item.conflictReason ? ` · ${item.conflictReason}` : ""}
              </li>
            ))}
          </ul>
        ) : null}
        <section className="process-oee-detail-section">
          <h3>Zaman ve üretim kanıtları</h3>
          <dl className="process-oee-detail-grid process-oee-detail-grid--3">
            <div>
              <dt>Duruş süresi</dt>
              <dd>{formatProcessOeeNumber(detailItem.downtimeSeconds)} sn</dd>
            </div>
            <div>
              <dt>Bilinmeyen süre</dt>
              <dd>{formatProcessOeeNumber(detailItem.unknownSeconds)} sn</dd>
            </div>
            <div>
              <dt>Gözlemlenen çevrim</dt>
              <dd>{formatProcessOeeNumber(detailItem.observedCycleCount)}</dd>
            </div>
            <div>
              <dt>Türetilen vuruş</dt>
              <dd>{formatProcessOeeNumber(detailItem.derivedShotCount)}</dd>
            </div>
            <div>
              <dt>Toplam parça</dt>
              <dd>{formatProcessOeeNumber(detailItem.totalPieceCount)}</dd>
            </div>
            <div>
              <dt>Kaynak sistemi</dt>
              <dd>{detailItem.sourceSystem ?? "—"}</dd>
            </div>
          </dl>
        </section>
      </div>
    );
  }

  return (
    <main className="process-oee-page">
      <section className="process-oee-executive" aria-label="Proses ve OEE yönetim komuta yüzeyi">
        <div className="process-oee-executive__top">
          <div className="process-oee-executive__intro">
            <p className="process-oee-executive__eyebrow">PROSES VE VERİMLİLİK</p>
            <h1>Proses &amp; OEE</h1>
            <p className="process-oee-executive__subtitle">
              Enjeksiyon hatları için makine ve üretim kanıtlarına dayalı OEE değerlendirmelerini
              operasyonel karar düzeyinde izleyin.
            </p>
            {freshnessLabel ? (
              <p className="process-oee-executive__meta">Kaynak tazeliği: {freshnessLabel}</p>
            ) : null}
          </div>
          {canManage ? (
            <div className="process-oee-executive__actions">
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
        </div>

        <div className="process-oee-executive__kpis" aria-label="OEE göstergeleri">
          <article className="process-oee-kpi process-oee-kpi--primary">
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
            <span className="process-oee-kpi__label">İncelenen Pencere</span>
            <strong className="process-oee-kpi__value" title={kpiWindowLabel}>
              {selectedSummary ? "Seçili" : formatProcessOeeNumber(summary.assessmentCount)}
            </strong>
            <span className="process-oee-kpi__hint">{kpiWindowLabel}</span>
          </article>
          <article className="process-oee-kpi">
            <span className="process-oee-kpi__label">Veri Yeterliliği</span>
            <strong className="process-oee-kpi__value">{kpiDataSufficiency}</strong>
            {attentionGroups.length > 0 ? (
              <span className="process-oee-kpi__hint">{attentionGroups.length} operasyonel dikkat</span>
            ) : null}
          </article>
        </div>

        {attentionGroups.length === 0 ? (
          <p className="process-oee-attention__quiet">Takip gerektiren operasyonel değerlendirme bulunmuyor.</p>
        ) : (
          <div className="process-oee-attention" aria-label="Operasyonel Dikkat">
            <div className="process-oee-attention__header">
              <h2 className="process-oee-attention__title">Operasyonel Dikkat</h2>
              {attentionGroups.length > ATTENTION_INITIAL_LIMIT ? (
                <button
                  type="button"
                  className="process-oee-attention__toggle"
                  onClick={() => setAttentionExpanded((current) => !current)}
                  aria-expanded={attentionExpanded}
                >
                  {attentionExpanded ? "Daha az göster" : "Tümünü göster"}
                </button>
              ) : null}
            </div>
            <ul className="process-oee-attention__list">
              {visibleAttentionGroups.map((group) => (
                <li key={group.id}>
                  <button
                    type="button"
                    className={`process-oee-attention__item process-oee-attention__item--${group.tone}`}
                    onClick={() => {
                      const assessment = assessments.find((row) => row.id === group.assessmentId);
                      if (assessment) {
                        selectAssessment(assessment, group.tab);
                      }
                    }}
                  >
                    <span className="process-oee-attention__label">{group.summary}</span>
                    <span className="process-oee-attention__meta" title={group.meta}>
                      {group.meta}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>

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

      <div className={`process-oee-workspace${detailOpen ? " process-oee-workspace--detail-open" : ""}`}>
        <section className="process-oee-registry" aria-label="OEE Değerlendirme Pencereleri">
          <div className="process-oee-registry__header">
            <h2 className="process-oee-registry__title">OEE Değerlendirme Pencereleri</h2>
            <span className="process-oee-registry__count">
              {isPending ? "Yükleniyor…" : `${filteredAssessments.length} sonuç`}
            </span>
          </div>

          <div className="process-oee-registry__toolbar">
            <div className="process-oee-registry__search-row">
              <div className="process-oee-search">
                <Search aria-hidden="true" className="process-oee-search__icon" size={14} />
                <input
                  id="process-oee-search"
                  type="search"
                  placeholder="Makine veya üretim emri ara"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                />
              </div>
              {filtersActive ? (
                <button type="button" className="process-oee-btn process-oee-btn--ghost" onClick={resetFilters}>
                  <X aria-hidden="true" size={14} />
                  Temizle
                </button>
              ) : null}
            </div>
            <div className="process-oee-registry__filters">
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
            </div>
          </div>

          <div className="process-oee-registry__table-wrap">
            {filteredAssessments.length === 0 && !isPending ? (
              <div className="process-oee-empty">
                <CircleGauge aria-hidden="true" size={28} />
                <h3>Değerlendirme bulunamadı</h3>
                <p>
                  {filtersActive
                    ? "Seçili filtrelere uygun kayıt yok. Filtreleri genişletmeyi deneyin."
                    : "Henüz OEE değerlendirmesi oluşturulmamış."}
                </p>
              </div>
            ) : detailOpen ? (
              <ul className="process-oee-compact-list" aria-label="Seçili değerlendirme listesi">
                {filteredAssessments.map((item) => (
                  <li key={item.id}>
                    <button
                      type="button"
                      className="process-oee-compact-row"
                      data-assessment-id={item.id}
                      data-selected={item.id === selectedId ? "true" : "false"}
                      onClick={() => selectAssessment(item)}
                      onKeyDown={(event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          selectAssessment(item);
                        }
                      }}
                    >
                      <div className="process-oee-compact-row__main">
                        <span className="process-oee-compact-row__machine">
                          {formatMachineLabel(item.machineCode, item.machineDisplayName)}
                        </span>
                        <span className="process-oee-compact-row__order">
                          {item.productionOrderReference ?? "—"}
                        </span>
                        <span className="process-oee-compact-row__period">
                          {formatCompactPeriod(item.windowStartUtc, item.windowEndUtc)}
                        </span>
                      </div>
                      <div className="process-oee-compact-row__aside">
                        <strong className="process-oee-compact-row__oee">
                          {formatPercent(item.oeePercent)}
                        </strong>
                        <span className={statusBadgeClass(item.status)}>
                          {getProcessOeeStatusLabel(item.status, item.statusLabel)}
                        </span>
                      </div>
                    </button>
                  </li>
                ))}
              </ul>
            ) : (
              <table className="process-oee-table">
                <thead>
                  <tr>
                    <th>Makine / Emir</th>
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
                  {filteredAssessments.map((item) => (
                    <tr
                      key={item.id}
                      data-assessment-id={item.id}
                      data-selected={item.id === selectedId ? "true" : "false"}
                      tabIndex={0}
                      onClick={() => selectAssessment(item)}
                      onKeyDown={(event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          selectAssessment(item);
                        }
                      }}
                    >
                      <td>
                        <span className="process-oee-cell-primary">
                          {formatMachineLabel(item.machineCode, item.machineDisplayName)}
                        </span>
                        <span className="process-oee-cell-secondary">
                          {item.productionOrderReference ?? "—"}
                        </span>
                      </td>
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
          </div>
        </section>

        {detailOpen && selectedSummary ? (
          <aside className="process-oee-detail-workspace" aria-label="Seçili değerlendirme detayı">
            <header
              ref={detailHeroRef}
              className="process-oee-detail-header"
              id="process-oee-detail-focus-target"
              tabIndex={-1}
            >
              <div className="process-oee-detail-header__row">
                <div className="process-oee-detail-header__identity">
                  <p className="process-oee-detail-breadcrumb">Proses &amp; OEE</p>
                  <h2>{formatMachineLabel(selectedSummary.machineCode, selectedSummary.machineDisplayName)}</h2>
                  <p className="process-oee-detail-header__order">
                    {selectedSummary.productionOrderReference ?? "Üretim emri yok"}
                  </p>
                  <p className="process-oee-detail-header__period">
                    {formatProcessOeeDateRange(selectedSummary.windowStartUtc, selectedSummary.windowEndUtc)}
                  </p>
                  <div className="process-oee-detail-header__chips">
                    <span className={statusBadgeClass(selectedSummary.status)}>
                      {getProcessOeeStatusLabel(selectedSummary.status, selectedSummary.statusLabel)}
                    </span>
                    {activeDetail?.sourceFreshnessUtc ? (
                      <span className="process-oee-detail-header__freshness">
                        Tazelik: {formatProcessOeeDateTime(activeDetail.sourceFreshnessUtc)}
                      </span>
                    ) : null}
                  </div>
                </div>
                <button
                  type="button"
                  className="process-oee-detail-close"
                  aria-label="Proses detayını kapat"
                  title="Proses detayını kapat"
                  onClick={clearSelection}
                >
                  <X aria-hidden="true" size={16} />
                </button>
              </div>
              <div className="process-oee-detail-header__summary" aria-label="OEE bileşen özeti">
                <div className="process-oee-detail-header__metric">
                  <span>OEE</span>
                  <strong>{formatPercent(activeDetail?.oeePercent ?? selectedSummary.oeePercent)}</strong>
                </div>
                <div className="process-oee-detail-header__metric">
                  <span>Kullanılabilirlik</span>
                  <strong>{formatPercent(activeDetail?.availabilityPercent ?? selectedSummary.availabilityPercent)}</strong>
                </div>
                <div className="process-oee-detail-header__metric">
                  <span>Performans</span>
                  <strong>{formatPercent(activeDetail?.performancePercent ?? selectedSummary.performancePercent)}</strong>
                </div>
                <div className="process-oee-detail-header__metric">
                  <span>Kalite</span>
                  <strong>{formatPercent(activeDetail?.qualityPercent ?? selectedSummary.qualityPercent)}</strong>
                </div>
              </div>
            </header>

            <div
              className={`process-oee-sticky-context${stickyContextVisible ? " is-visible" : ""}`}
              data-process-oee-sticky-context={stickyContextVisible ? "true" : "false"}
            >
              <div className="process-oee-sticky-context__identity">
                <p className="process-oee-sticky-context__name">
                  {formatMachineLabel(selectedSummary.machineCode, selectedSummary.machineDisplayName)}
                </p>
                <span className="process-oee-sticky-context__tab">{activeTabLabel}</span>
              </div>
              <button
                type="button"
                className="process-oee-detail-close"
                aria-label="Proses detayını kapat"
                title="Proses detayını kapat"
                onClick={clearSelection}
              >
                <X aria-hidden="true" size={16} />
              </button>
            </div>

            <div className="process-oee-tab-rail-wrap">
              <div className="process-oee-tab-rail" role="tablist" aria-label="Değerlendirme detay sekmeleri">
                {DETAIL_TABS.map((tab) => (
                  <button
                    key={tab.id}
                    type="button"
                    role="tab"
                    aria-selected={activeTab === tab.id}
                    className={`process-oee-tab-rail__tab${activeTab === tab.id ? " process-oee-tab-rail__tab--active" : ""}`}
                    onClick={() => selectTab(tab.id)}
                  >
                    {tab.label}
                  </button>
                ))}
              </div>
            </div>

            <div className="process-oee-detail-body">
              {isPending && !activeDetail ? (
                <div className="process-oee-loading">
                  <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
                  Detay yükleniyor…
                </div>
              ) : activeDetail ? (
                <>
                  {renderDataContextNotice(activeDetail)}
                  {activeTab === "overview" ? renderOverviewTab(activeDetail) : null}
                  {activeTab === "loss" ? renderLossTab() : null}
                  {activeTab === "stability" ? renderStabilityTab() : null}
                  {activeTab === "downtime" ? renderDowntimeTab() : null}
                  {activeTab === "shift" ? renderShiftTab() : null}
                  {activeTab === "evidence" ? renderEvidenceTab(activeDetail) : null}
                </>
              ) : (
                <div className="process-oee-empty">
                  <p>Detay yüklenemedi.</p>
                </div>
              )}
            </div>
          </aside>
        ) : null}
      </div>
    </main>
  );
}

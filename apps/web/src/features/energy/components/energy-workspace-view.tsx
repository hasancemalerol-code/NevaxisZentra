"use client";

import {
  useCallback,
  useMemo,
  useRef,
  useState,
  useTransition,
  type KeyboardEvent as ReactKeyboardEvent,
} from "react";
import { LoaderCircle, RefreshCw, Search, X, Zap } from "lucide-react";

import {
  allocationSliceColor,
  createSparklinePath,
  resolveKpiEvidenceRail,
  resolveKpiPresentation,
} from "./energy-presentation";

import type {
  EnergyAllocationSlice,
  EnergyAssessmentDetail,
  EnergyAssessmentSummary,
  EnergyBaselineSummary,
  EnergyMeasurementPointDetail,
  EnergyMeasurementPointSummary,
  EnergyTargetSummary,
  EnergyTrendPoint,
  EnergyWorkspacePayload,
} from "../domain/energy-types";
import {
  ENERGY_DEVELOPMENT_BANNER,
  ENERGY_EQUIPMENT_FILTER_OPTIONS,
  ENERGY_OPPORTUNITY_DISCLAIMER,
  ENERGY_OUTCOME_FILTER_OPTIONS,
  ENERGY_SCOPE_FILTER_OPTIONS,
  ENERGY_TAB_OPTIONS,
  type EnergyTabId,
  getEnergyAssessmentTitle,
  getEnergyDriverLabel,
  getEnergyEvidenceLabel,
  getEnergyOutcomeLabel,
  getEnergySeverityLabel,
  getEnergyTargetMetricLabel,
  getEnergyTargetScopeLabel,
  getEnergyTargetStatusLabel,
} from "../domain/energy-labels";
import {
  formatEnergyDateTime,
  formatEnergyKwh,
  formatEnergyNumber,
  formatEnergyPeriod,
  formatEnergyVariance,
} from "../domain/energy-formatters";
import {
  loadEnergyAssessmentDetailAction,
  loadEnergyMeasurementPointDetailAction,
  refreshEnergyWorkspaceAction,
  updateEnergyTargetAction,
} from "../server/energy-actions";
import "./energy-workspace.css";

type EnergyWorkspaceViewProps = Readonly<{
  workspace: EnergyWorkspacePayload;
  canManage: boolean;
}>;

const MEASUREMENT_STATUS_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "active", label: "Aktif" },
  { value: "inactive", label: "Pasif" },
] as const;

const MEASUREMENT_EVIDENCE_FILTER_OPTIONS = [
  { value: "", label: "Tüm kanıt türleri" },
  { value: "measured", label: "Ölçülmüş" },
  { value: "calculated", label: "Hesaplanmış" },
  { value: "estimated", label: "Tahmini" },
  { value: "simulated", label: "Simülasyon" },
  { value: "insufficient-data", label: "Veri yetersiz" },
  { value: "source-conflict", label: "Kaynak çelişkisi" },
] as const;

const ASSESSMENT_EVIDENCE_FILTER_OPTIONS = MEASUREMENT_EVIDENCE_FILTER_OPTIONS;

type TargetEditState = Readonly<{
  target: EnergyTargetSummary;
  targetValue: string;
  tolerance: string;
  effectiveFromUtc: string;
  effectiveToUtc: string;
  sourceProvenance: string;
  status: string;
  confirmOpen: boolean;
}>;

function outcomeBadgeClass(outcome: string): string {
  switch (outcome) {
    case "within-target":
      return "energy-badge energy-badge--approved";
    case "attention":
      return "energy-badge energy-badge--attention";
    case "source-conflict":
      return "energy-badge energy-badge--conflict";
    default:
      return "energy-badge energy-badge--insufficient";
  }
}

function toDateInputValue(utc: string): string {
  const date = new Date(utc);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(0, 10);
}

function toUtcIsoFromDateInput(value: string, endOfDay = false): string {
  if (!value) return "";
  const suffix = endOfDay ? "T23:59:59.999Z" : "T00:00:00.000Z";
  return new Date(`${value}${suffix}`).toISOString();
}

function selectChartTickIndices(pointCount: number, maxTicks: number): number[] {
  if (pointCount <= maxTicks) {
    return Array.from({ length: pointCount }, (_, index) => index);
  }

  const indices = [0];
  const interior = maxTicks - 2;
  for (let step = 1; step <= interior; step += 1) {
    indices.push(Math.round((step * (pointCount - 1)) / (interior + 1)));
  }
  indices.push(pointCount - 1);
  return [...new Set(indices)].sort((left, right) => left - right);
}

function buildTrendSummary(trend: readonly EnergyTrendPoint[]): string {
  if (trend.length === 0) {
    return "Trend verisi bulunmuyor.";
  }

  return trend
    .map((point) => {
      const parts = [
        point.label,
        point.consumedKwh !== null ? `tüketim ${formatEnergyKwh(point.consumedKwh)} kWh` : null,
        point.targetKwh !== null ? `hedef ${formatEnergyKwh(point.targetKwh)} kWh` : null,
        point.baselineKwh !== null ? `baz ${formatEnergyKwh(point.baselineKwh)} kWh` : null,
      ].filter(Boolean);
      return parts.join(", ");
    })
    .join("; ");
}

const DONUT_OUTER_RADIUS = 48;
const DONUT_INNER_RADIUS = 29;
const DONUT_CENTER = { x: 60, y: 60 } as const;

function describeDonutArc(
  cx: number,
  cy: number,
  outerRadius: number,
  innerRadius: number,
  startAngle: number,
  endAngle: number,
): string {
  const outerStartX = cx + outerRadius * Math.cos(startAngle);
  const outerStartY = cy + outerRadius * Math.sin(startAngle);
  const outerEndX = cx + outerRadius * Math.cos(endAngle);
  const outerEndY = cy + outerRadius * Math.sin(endAngle);
  const innerEndX = cx + innerRadius * Math.cos(endAngle);
  const innerEndY = cy + innerRadius * Math.sin(endAngle);
  const innerStartX = cx + innerRadius * Math.cos(startAngle);
  const innerStartY = cy + innerRadius * Math.sin(startAngle);
  const largeArc = endAngle - startAngle > Math.PI ? 1 : 0;
  return [
    `M ${outerStartX} ${outerStartY}`,
    `A ${outerRadius} ${outerRadius} 0 ${largeArc} 1 ${outerEndX} ${outerEndY}`,
    `L ${innerEndX} ${innerEndY}`,
    `A ${innerRadius} ${innerRadius} 0 ${largeArc} 0 ${innerStartX} ${innerStartY}`,
    "Z",
  ].join(" ");
}

function buildAllocationArcs(
  slices: readonly EnergyAllocationSlice[],
  totalKwh: number,
): ReadonlyArray<Readonly<{ id: string; path: string; color: string }>> {
  const { x: cx, y: cy } = DONUT_CENTER;
  let cursor = -Math.PI / 2;

  return slices.map((slice) => {
    const sweep = totalKwh > 0 ? (slice.kwh / totalKwh) * Math.PI * 2 : 0;
    const start = cursor;
    const end = cursor + sweep;
    cursor = end;
    return {
      id: slice.id,
      path:
        sweep > 0
          ? describeDonutArc(
              cx,
              cy,
              DONUT_OUTER_RADIUS,
              DONUT_INNER_RADIUS,
              start,
              end,
            )
          : "",
      color: allocationSliceColor(slice.tone),
    };
  });
}

function EnergyAllocationDonut({
  slices,
  totalKwh,
  coveragePercent,
}: {
  slices: readonly EnergyAllocationSlice[];
  totalKwh: number;
  coveragePercent: number;
}) {
  const { x: cx, y: cy } = DONUT_CENTER;
  const arcs = buildAllocationArcs(slices, totalKwh);

  return (
    <div className="energy-allocation-donut" data-testid="energy-allocation-donut">
      <svg viewBox="0 0 120 120" role="img" aria-label="Enerji dağılımı halka grafiği">
        {arcs.map((arc) =>
          arc.path ? (
            <path key={arc.id} d={arc.path} fill={arc.color} data-testid="energy-allocation-donut-segment" />
          ) : null,
        )}
        <circle
          cx={cx}
          cy={cy}
          r={DONUT_INNER_RADIUS}
          fill="#ffffff"
          data-testid="energy-allocation-donut-hole"
        />
      </svg>
      <div
        className="energy-allocation-donut__center"
        data-testid="energy-donut-center"
        aria-hidden="true"
      >
        <span
          className="energy-allocation-donut__label"
          data-testid="energy-donut-center-total-label"
        >
          Toplam tüketim
        </span>
        <span
          className="energy-allocation-donut__total"
          data-testid="energy-donut-center-total-value"
        >
          {formatEnergyKwh(totalKwh)}
        </span>
        <span className="energy-allocation-donut__unit">kWh</span>
        <span
          className="energy-allocation-donut__label"
          data-testid="energy-donut-center-coverage-label"
        >
          Dağıtım kapsamı
        </span>
        <span
          className="energy-allocation-donut__coverage"
          data-testid="energy-donut-center-coverage-value"
        >
          %{formatEnergyNumber(coveragePercent, 1)}
        </span>
      </div>
    </div>
  );
}

function deriveTrendCadenceMs(trend: readonly EnergyTrendPoint[]): number | null {
  if (trend.length < 2) return null;
  const deltas: number[] = [];
  for (let index = 1; index < trend.length; index += 1) {
    deltas.push(
      new Date(trend[index].windowStartUtc).getTime()
      - new Date(trend[index - 1].windowStartUtc).getTime(),
    );
  }
  deltas.sort((left, right) => left - right);
  return deltas[Math.floor(deltas.length / 2)] ?? null;
}

function EnergyTrendChart({
  trend,
  insufficientData,
  scope,
  bucketDurationMs,
  reportingTotalKwh,
  targetComparisonNote,
  baselineComparisonNote,
}: {
  trend: readonly EnergyTrendPoint[];
  insufficientData: boolean;
  scope: string;
  bucketDurationMs: number | null;
  reportingTotalKwh: number;
  targetComparisonNote: string | null;
  baselineComparisonNote: string | null;
}) {
  const hasActual = trend.some((point) => point.consumedKwh !== null);
  const hasTarget = trend.some((point) => point.targetKwh !== null);
  const hasBaseline = trend.some((point) => point.baselineKwh !== null);

  if (trend.length === 0 || insufficientData) {
    return (
      <p className="energy-empty" data-testid="energy-trend-insufficient">
        Seçili raporlama kapsamında en az sekiz karşılaştırılabilir tesis tüketim penceresi
        bulunamadı. Değerlendirme senaryoları veya uyumsuz ölçüm pencereleri tesis eğilimi
        olarak birleştirilmez.
      </p>
    );
  }

  const width = 800;
  const height = 244;
  const pad = { top: 16, right: 34, bottom: 36, left: 56 };
  const chartWidth = width - pad.left - pad.right;
  const chartHeight = height - pad.top - pad.bottom;
  const maxTicks = trend.length <= 6 ? trend.length : trend.length <= 12 ? 6 : 7;
  const tickIndices = selectChartTickIndices(trend.length, maxTicks);
  const yTickCount = 4;
  const timestamps = trend.map((point) => new Date(point.windowStartUtc).getTime());
  const minTime = Math.min(...timestamps);
  const maxTime = Math.max(...timestamps);
  const cadenceMs = deriveTrendCadenceMs(trend) ?? bucketDurationMs ?? 0;
  const cadenceTolerance = cadenceMs > 0 ? cadenceMs * 0.25 : 0;

  const values = trend.flatMap((point) =>
    [point.consumedKwh, point.targetKwh, point.baselineKwh].filter(
      (value): value is number => value !== null && !Number.isNaN(value),
    ),
  );
  const maxValue = Math.max(...values, 1);

  const xAt = (index: number) => {
    if (maxTime === minTime) return pad.left + chartWidth / 2;
    const time = timestamps[index];
    return pad.left + ((time - minTime) / (maxTime - minTime)) * chartWidth;
  };
  const yAt = (value: number) => pad.top + chartHeight - (value / maxValue) * chartHeight;
  const labelAnchor = (index: number): "start" | "middle" | "end" => {
    if (index === 0) return "start";
    if (index === trend.length - 1) return "end";
    return "middle";
  };

  const buildSegmentPaths = (
    getter: (point: EnergyTrendPoint) => number | null,
  ): string[] => {
    const segments: string[] = [];
    let current: Array<{ index: number; value: number }> = [];

    const flush = () => {
      if (current.length === 0) return;
      segments.push(
        current
          .map((segment, segmentIndex) => {
            const command = segmentIndex === 0 ? "M" : "L";
            return `${command}${xAt(segment.index)},${yAt(segment.value)}`;
          })
          .join(" "),
      );
      current = [];
    };

    for (let index = 0; index < trend.length; index += 1) {
      const value = getter(trend[index]);
      if (value === null || Number.isNaN(value)) {
        flush();
        continue;
      }

      if (current.length > 0) {
        const previousIndex = current[current.length - 1].index;
        const startGap = timestamps[index] - timestamps[previousIndex];
        if (cadenceMs > 0 && startGap > cadenceMs + cadenceTolerance) {
          flush();
        }
      }

      current.push({ index, value });
    }

    flush();
    return segments;
  };

  const buildAreaPaths = (): string[] =>
    buildSegmentPaths((point) => point.consumedKwh).map((linePath) => {
      const coordinates = linePath
        .replace(/[ML]/g, " ")
        .trim()
        .split(/\s+/)
        .map((pair) => pair.split(",").map(Number))
        .filter((pair) => pair.length === 2);
      if (coordinates.length === 0) return "";
      const first = coordinates[0];
      const last = coordinates[coordinates.length - 1];
      return `${linePath} L ${last[0]} ${yAt(0)} L ${first[0]} ${yAt(0)} Z`;
    });

  const actualSegments = hasActual ? buildSegmentPaths((point) => point.consumedKwh) : [];
  const actualAreaSegments = hasActual ? buildAreaPaths() : [];
  const targetSegments = hasTarget ? buildSegmentPaths((point) => point.targetKwh) : [];
  const baselineSegments = hasBaseline ? buildSegmentPaths((point) => point.baselineKwh) : [];
  const yTicks = Array.from({ length: yTickCount + 1 }, (_, index) => (maxValue / yTickCount) * index);

  return (
    <div className="energy-trend-chart" data-testid="energy-trend-chart">
      <p className="energy-panel__meta">{scope}</p>
      <div
        data-testid="energy-trend-metadata"
        data-bucket-count={trend.length}
        data-bucket-duration-ms={bucketDurationMs ?? 0}
        data-reconciliation-total={reportingTotalKwh}
        hidden
      />
      <div className="energy-chart-shell">
        <svg
          className="energy-chart"
          viewBox={`0 0 ${width} ${height}`}
          role="img"
          aria-label="Tüketim eğilimi grafiği"
          data-testid="energy-trend-svg"
          preserveAspectRatio="xMidYMid meet"
        >
          <defs>
            <linearGradient id="energyActualAreaGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#2563eb" stopOpacity="0.28" />
              <stop offset="100%" stopColor="#2563eb" stopOpacity="0.02" />
            </linearGradient>
          </defs>
          {yTicks.map((tick) => (
            <g key={tick}>
              <line
                className="energy-chart-grid-line"
                x1={pad.left}
                y1={yAt(tick)}
                x2={pad.left + chartWidth}
                y2={yAt(tick)}
              />
              <text
                className="energy-chart-y-label"
                x={pad.left - 8}
                y={yAt(tick) + 3}
                textAnchor="end"
              >
                {formatEnergyNumber(tick, 0)}
              </text>
            </g>
          ))}
          <line
            x1={pad.left}
            y1={pad.top + chartHeight}
            x2={pad.left + chartWidth}
            y2={pad.top + chartHeight}
            stroke="#cbd5e1"
            strokeWidth="1"
          />
          <text
            className="energy-chart-y-label"
            x={pad.left - 8}
            y={pad.top - 4}
            textAnchor="end"
          >
            kWh
          </text>
          {tickIndices.map((index) => {
            const point = trend[index];
            return (
              <text
                key={point.windowStartUtc}
                data-testid="energy-trend-axis-label"
                x={xAt(index)}
                y={height - 12}
                textAnchor={labelAnchor(index)}
                fontSize="10"
                fill="#64748b"
              >
                {point.label}
              </text>
            );
          })}
          {actualAreaSegments.map((segmentPath, segmentIndex) =>
            segmentPath ? (
              <path
                key={`area-${segmentIndex}`}
                d={segmentPath}
                className="energy-chart-area"
                data-testid="energy-trend-area"
              />
            ) : null,
          )}
          {baselineSegments.map((segmentPath, segmentIndex) =>
            segmentPath ? (
              <path
                key={`baseline-${segmentIndex}`}
                d={segmentPath}
                fill="none"
                stroke="#94a3b8"
                strokeWidth="2"
                strokeDasharray="5 4"
                data-testid="energy-trend-baseline"
              />
            ) : null,
          )}
          {targetSegments.map((segmentPath, segmentIndex) =>
            segmentPath ? (
              <path
                key={`target-${segmentIndex}`}
                d={segmentPath}
                fill="none"
                stroke="#16a34a"
                strokeWidth="2"
                strokeDasharray="6 4"
                data-testid="energy-trend-target"
              />
            ) : null,
          )}
          {actualSegments.map((segmentPath, segmentIndex) =>
            segmentPath ? (
              <path
                key={`actual-${segmentIndex}`}
                d={segmentPath}
                fill="none"
                stroke="#2563eb"
                strokeWidth="2.5"
                data-testid="energy-trend-actual"
              />
            ) : null,
          )}
          {hasActual
            ? trend.map((point, index) =>
                point.consumedKwh !== null ? (
                  <circle
                    key={`${point.windowStartUtc}-marker`}
                    className="energy-chart-marker"
                    cx={xAt(index)}
                    cy={yAt(point.consumedKwh)}
                    r="3.5"
                    data-testid="energy-trend-marker"
                    data-window-start-utc={point.windowStartUtc}
                  />
                ) : null,
              )
            : null}
        </svg>
      </div>
      <div className="energy-chart-legend" aria-hidden="true">
        {hasActual ? (
          <span className="energy-chart-legend__item">
            <span className="energy-chart-legend__swatch energy-chart-legend__swatch--actual" />
            Gerçekleşen tüketim
          </span>
        ) : null}
        {hasTarget ? (
          <span className="energy-chart-legend__item">
            <span className="energy-chart-legend__swatch energy-chart-legend__swatch--target" />
            Hedef
          </span>
        ) : null}
        {hasBaseline ? (
          <span className="energy-chart-legend__item">
            <span className="energy-chart-legend__swatch energy-chart-legend__swatch--baseline" />
            Baz
          </span>
        ) : null}
      </div>
      {targetComparisonNote ? (
        <p className="energy-chart-caption" data-testid="energy-trend-target-note">
          {targetComparisonNote}
        </p>
      ) : null}
      {baselineComparisonNote ? (
        <p className="energy-chart-caption" data-testid="energy-trend-baseline-note">
          {baselineComparisonNote}
        </p>
      ) : null}
      <p className="energy-chart-caption" data-testid="energy-trend-caption">
        {trend.length} zaman diliminde {scope.toLowerCase()}; noktalar gerçek UTC zamanına göre
        konumlandırılır.
      </p>
      <p className="energy-visually-hidden" data-testid="energy-trend-screen-reader">
        {buildTrendSummary(trend)}
      </p>
    </div>
  );
}

export function EnergyWorkspaceView({
  workspace: initialWorkspace,
  canManage: initialCanManage,
}: EnergyWorkspaceViewProps) {
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [canManage, setCanManage] = useState(initialCanManage);
  const [activeTab, setActiveTab] = useState<EnergyTabId>("overview");
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const [measurementSearch, setMeasurementSearch] = useState("");
  const [measurementScopeFilter, setMeasurementScopeFilter] = useState("");
  const [measurementEquipmentFilter, setMeasurementEquipmentFilter] = useState("");
  const [measurementStatusFilter, setMeasurementStatusFilter] = useState("");
  const [measurementEvidenceFilter, setMeasurementEvidenceFilter] = useState("");
  const [selectedMeasurementCode, setSelectedMeasurementCode] = useState("");
  const [measurementDetail, setMeasurementDetail] = useState<
    EnergyMeasurementPointDetail | undefined
  >();

  const [assessmentOutcomeFilter, setAssessmentOutcomeFilter] = useState("");
  const [assessmentDriverFilter, setAssessmentDriverFilter] = useState("");
  const [assessmentScopeFilter, setAssessmentScopeFilter] = useState("");
  const [assessmentEvidenceFilter, setAssessmentEvidenceFilter] = useState("");
  const [selectedAssessmentRef, setSelectedAssessmentRef] = useState("");
  const [assessmentDetail, setAssessmentDetail] = useState<EnergyAssessmentDetail | undefined>();

  const [targetEdit, setTargetEdit] = useState<TargetEditState | null>(null);
  const [targetDialogError, setTargetDialogError] = useState<string | null>(null);

  const tabListRef = useRef<HTMLDivElement | null>(null);
  const measurementRowRefs = useRef<Map<string, HTMLTableRowElement>>(new Map());
  const assessmentRowRefs = useRef<Map<string, HTMLTableRowElement>>(new Map());

  const { meta } = workspace;

  const measurementFiltersActive = Boolean(
    measurementSearch.trim()
    || measurementScopeFilter
    || measurementEquipmentFilter
    || measurementStatusFilter
    || measurementEvidenceFilter,
  );

  const assessmentFiltersActive = Boolean(
    assessmentOutcomeFilter
    || assessmentDriverFilter
    || assessmentScopeFilter
    || assessmentEvidenceFilter,
  );

  const driverFilterOptions = useMemo(() => {
    const unique = new Map<string, string>();
    for (const assessment of workspace.assessments) {
      unique.set(assessment.leadingDriverCode, assessment.leadingDriverLabel);
    }
    return [
      { value: "", label: "Tüm sürücüler" },
      ...[...unique.entries()]
        .sort((a, b) => a[1].localeCompare(b[1], "tr"))
        .map(([value, label]) => ({ value, label })),
    ];
  }, [workspace.assessments]);

  const filteredMeasurementPoints = useMemo(() => {
    const query = measurementSearch.trim().toLowerCase();
    return workspace.measurementPoints.filter((point) => {
      const matchesSearch =
        !query
        || point.displayName.toLowerCase().includes(query)
        || point.pointCode.toLowerCase().includes(query)
        || (point.machineAssetLabel?.toLowerCase().includes(query) ?? false);
      const matchesScope =
        !measurementScopeFilter || point.measurementScope === measurementScopeFilter;
      const matchesEquipment =
        !measurementEquipmentFilter || point.equipmentCategory === measurementEquipmentFilter;
      const matchesStatus = !measurementStatusFilter || point.status === measurementStatusFilter;
      const matchesEvidence =
        !measurementEvidenceFilter
        || point.evidenceLabel === getEnergyEvidenceLabel(measurementEvidenceFilter);
      return matchesSearch && matchesScope && matchesEquipment && matchesStatus && matchesEvidence;
    });
  }, [
    measurementEquipmentFilter,
    measurementEvidenceFilter,
    measurementScopeFilter,
    measurementSearch,
    measurementStatusFilter,
    workspace.measurementPoints,
  ]);

  const filteredAssessments = useMemo(() => {
    return workspace.assessments.filter((assessment) => {
      const matchesOutcome =
        !assessmentOutcomeFilter || assessment.outcome === assessmentOutcomeFilter;
      const matchesDriver =
        !assessmentDriverFilter || assessment.leadingDriverCode === assessmentDriverFilter;
      const matchesScope =
        !assessmentScopeFilter || assessment.comparisonDimension === assessmentScopeFilter;
      const matchesEvidence =
        !assessmentEvidenceFilter
        || assessment.evidenceLabel === getEnergyEvidenceLabel(assessmentEvidenceFilter);
      return matchesOutcome && matchesDriver && matchesScope && matchesEvidence;
    });
  }, [
    assessmentDriverFilter,
    assessmentEvidenceFilter,
    assessmentOutcomeFilter,
    assessmentScopeFilter,
    workspace.assessments,
  ]);

  const selectedMeasurement = useMemo(
    () =>
      filteredMeasurementPoints.find((point) => point.pointCode === selectedMeasurementCode)
      ?? workspace.measurementPoints.find((point) => point.pointCode === selectedMeasurementCode),
    [filteredMeasurementPoints, selectedMeasurementCode, workspace.measurementPoints],
  );

  const selectedAssessment = useMemo(
    () =>
      filteredAssessments.find(
        (assessment) => assessment.assessmentReference === selectedAssessmentRef,
      )
      ?? workspace.assessments.find(
        (assessment) => assessment.assessmentReference === selectedAssessmentRef,
      ),
    [filteredAssessments, selectedAssessmentRef, workspace.assessments],
  );

  const aboveTargetLabel = useMemo(() => {
    const varianceKpi = workspace.kpis.find((kpi) => kpi.id === "target-variance");
    if (varianceKpi?.state === "available" && varianceKpi.value !== "—") {
      return varianceKpi.value;
    }

    const assessment = workspace.assessments.find(
      (item) => item.varianceConsumedKwh !== null && item.varianceConsumedKwh > 0,
    );
    if (!assessment) {
      return "—";
    }

    return formatEnergyVariance(assessment.varianceConsumedKwh, assessment.variancePercent);
  }, [workspace.assessments, workspace.kpis]);

  const overviewIntelligenceRows = useMemo(() => {
    const priority: Record<string, number> = {
      attention: 0,
      "source-conflict": 1,
      "within-target": 2,
      "insufficient-data": 3,
    };

    return [...workspace.intelligenceRows]
      .sort(
        (left, right) =>
          (priority[left.outcome] ?? 4) - (priority[right.outcome] ?? 4),
      )
      .slice(0, 3);
  }, [workspace.intelligenceRows]);

  const targetComparison = useMemo(() => {
    const totalKpi = workspace.kpis.find((kpi) => kpi.id === "total-consumption");
    const varianceKpi = workspace.kpis.find((kpi) => kpi.id === "target-variance");
    const approvedTarget = workspace.targets.find(
      (target) => target.isApprovedTarget && target.targetMetric === "consumed-kwh",
    );
    const primaryAssessment = workspace.assessments[0];

    return {
      actualLabel:
        totalKpi?.state === "available" && totalKpi.value !== "—"
          ? `${totalKpi.value}${totalKpi.unit ? ` ${totalKpi.unit}` : ""}`
          : formatEnergyKwh(workspace.reportingTotalKwh),
      approvedTargetLabel: approvedTarget
        ? `${formatEnergyNumber(approvedTarget.targetValue)} ${approvedTarget.unit}`
        : "—",
      deviationLabel:
        varianceKpi?.state === "available" && varianceKpi.value !== "—"
          ? varianceKpi.value
          : aboveTargetLabel,
      evidenceLabel: meta.evidenceLabel,
      outcomeLabel: primaryAssessment?.outcomeLabel ?? varianceKpi?.meta ?? "—",
      hasApprovedTarget: Boolean(approvedTarget),
    };
  }, [aboveTargetLabel, meta.evidenceLabel, workspace.assessments, workspace.kpis, workspace.reportingTotalKwh, workspace.targets]);

  const handleRefresh = useCallback(() => {
    startTransition(async () => {
      setError(null);
      const result = await refreshEnergyWorkspaceAction();
      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setWorkspace(result.workspace);
      setCanManage(result.canManage);
      setMeasurementDetail(undefined);
      setAssessmentDetail(undefined);
      setSelectedMeasurementCode("");
      setSelectedAssessmentRef("");
    });
  }, []);

  const handleTabKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
      if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") {
        return;
      }

      event.preventDefault();
      const delta = event.key === "ArrowRight" ? 1 : -1;
      const nextIndex = (index + delta + ENERGY_TAB_OPTIONS.length) % ENERGY_TAB_OPTIONS.length;
      const buttons = tabListRef.current?.querySelectorAll<HTMLButtonElement>('[role="tab"]');
      buttons?.[nextIndex]?.focus();
      setActiveTab(ENERGY_TAB_OPTIONS[nextIndex].id);
    },
    [],
  );

  function resetMeasurementFilters() {
    setMeasurementSearch("");
    setMeasurementScopeFilter("");
    setMeasurementEquipmentFilter("");
    setMeasurementStatusFilter("");
    setMeasurementEvidenceFilter("");
  }

  function resetAssessmentFilters() {
    setAssessmentOutcomeFilter("");
    setAssessmentDriverFilter("");
    setAssessmentScopeFilter("");
    setAssessmentEvidenceFilter("");
  }

  function closeMeasurementDetail() {
    const restoreCode = selectedMeasurementCode;
    setSelectedMeasurementCode("");
    setMeasurementDetail(undefined);
    setError(null);

    requestAnimationFrame(() => {
      measurementRowRefs.current.get(restoreCode)?.focus({ preventScroll: true });
    });
  }

  function closeAssessmentDetail() {
    const restoreRef = selectedAssessmentRef;
    setSelectedAssessmentRef("");
    setAssessmentDetail(undefined);
    setError(null);

    requestAnimationFrame(() => {
      assessmentRowRefs.current.get(restoreRef)?.focus({ preventScroll: true });
    });
  }

  function selectMeasurementPoint(point: EnergyMeasurementPointSummary) {
    setSelectedMeasurementCode(point.pointCode);
    setMeasurementDetail(undefined);
    setError(null);

    startTransition(async () => {
      const result = await loadEnergyMeasurementPointDetailAction(point.pointCode);
      if (result.status === "success") {
        setMeasurementDetail(result.detail);
      } else {
        setError(result.message);
      }
    });
  }

  function selectAssessment(
    assessment: EnergyAssessmentSummary,
    options?: Readonly<{ switchTab?: boolean }>,
  ) {
    if (options?.switchTab) {
      setActiveTab("assessments");
    }

    setSelectedAssessmentRef(assessment.assessmentReference);
    setAssessmentDetail(undefined);
    setError(null);

    startTransition(async () => {
      const result = await loadEnergyAssessmentDetailAction(assessment.assessmentReference);
      if (result.status === "success") {
        setAssessmentDetail(result.detail);
      } else {
        setError(result.message);
      }
    });
  }

  function openTargetEdit(target: EnergyTargetSummary) {
    setTargetDialogError(null);
    setTargetEdit({
      target,
      targetValue: String(target.targetValue),
      tolerance: String(target.tolerance),
      effectiveFromUtc: toDateInputValue(target.effectiveFromUtc),
      effectiveToUtc: toDateInputValue(target.effectiveToUtc),
      sourceProvenance: target.sourceProvenance,
      status: target.status,
      confirmOpen: false,
    });
  }

  function submitTargetUpdate() {
    if (!targetEdit) {
      return;
    }

    const targetValue = Number.parseFloat(targetEdit.targetValue);
    const tolerance = Number.parseFloat(targetEdit.tolerance);
    if (Number.isNaN(targetValue) || Number.isNaN(tolerance)) {
      setTargetDialogError("Hedef değeri ve tolerans sayısal olmalıdır.");
      return;
    }

    startTransition(async () => {
      setTargetDialogError(null);
      const result = await updateEnergyTargetAction({
        targetReference: targetEdit.target.targetReference,
        targetValue,
        tolerance,
        effectiveFromUtc: toUtcIsoFromDateInput(targetEdit.effectiveFromUtc),
        effectiveToUtc: toUtcIsoFromDateInput(targetEdit.effectiveToUtc, true),
        sourceProvenance: targetEdit.sourceProvenance.trim(),
        status: targetEdit.status,
        expectedVersion: targetEdit.target.version,
      });

      if (result.status === "failure") {
        setTargetDialogError(result.message);
        return;
      }

      setTargetEdit(null);
      const refreshed = await refreshEnergyWorkspaceAction();
      if (refreshed.status === "success") {
        setWorkspace(refreshed.workspace);
        setCanManage(refreshed.canManage);
      }
    });
  }

  function renderBaselineCard(baseline: EnergyBaselineSummary) {
    return (
      <article key={baseline.id} className="energy-baseline-card">
        <div style={{ display: "flex", justifyContent: "space-between", gap: "0.5rem" }}>
          <strong>{baseline.baselineReference}</strong>
          <span className="energy-badge energy-badge--calculated">Hesaplanan</span>
        </div>
        <p className="energy-kpi-card__meta">{baseline.comparisonDimensionLabel}</p>
        <dl className="energy-detail-panel" style={{ marginTop: "0.5rem" }}>
          <dt>Örnek sayısı</dt>
          <dd>{formatEnergyNumber(baseline.sampleCount, 0)}</dd>
          <dt>Medyan merkez</dt>
          <dd>
            {baseline.medianCenter !== null
              ? `${formatEnergyKwh(baseline.medianCenter)} kWh`
              : "—"}
          </dd>
          <dt>Üst eşik</dt>
          <dd>
            {baseline.upperThresholdConsumedKwh !== null
              ? `${formatEnergyKwh(baseline.upperThresholdConsumedKwh)} kWh`
              : "—"}
          </dd>
          <dt>Kanıt</dt>
          <dd>{baseline.evidenceLabel}</dd>
        </dl>
      </article>
    );
  }

  function renderTargetCard(target: EnergyTargetSummary) {
    return (
      <article key={target.id} className="energy-target-card">
        <div style={{ display: "flex", justifyContent: "space-between", gap: "0.5rem" }}>
          <strong>{target.targetReference}</strong>
          {target.isApprovedTarget ? (
            <span className="energy-badge energy-badge--approved">Onaylı</span>
          ) : (
            <span className="energy-badge energy-badge--insufficient">
              {getEnergyTargetStatusLabel(target.status)}
            </span>
          )}
        </div>
        <p className="energy-kpi-card__meta">
          {getEnergyTargetScopeLabel(target.targetScope)} ·{" "}
          {getEnergyTargetMetricLabel(target.targetMetric)}
        </p>
        <dl className="energy-detail-panel" style={{ marginTop: "0.5rem" }}>
          <dt>Hedef değeri</dt>
          <dd>
            {formatEnergyNumber(target.targetValue)} {target.unit}
          </dd>
          <dt>Tolerans</dt>
          <dd>{formatEnergyNumber(target.tolerance)}</dd>
          <dt>Geçerlilik</dt>
          <dd>{formatEnergyPeriod(target.effectiveFromUtc, target.effectiveToUtc)}</dd>
          <dt>Sürüm</dt>
          <dd>{target.version}</dd>
        </dl>
        {canManage ? (
          <button
            type="button"
            className="energy-button energy-button--primary"
            style={{ marginTop: "0.55rem" }}
            disabled={isPending}
            onClick={() => openTargetEdit(target)}
          >
            Hedefi güncelle
          </button>
        ) : (
          <button
            type="button"
            className="energy-button"
            style={{ marginTop: "0.55rem" }}
            disabled
            title="Hedef güncellemek için energy.manage yetkisi gerekir."
          >
            Hedefi güncelle
          </button>
        )}
      </article>
    );
  }

  return (
    <main className="energy-page" data-testid="energy-workspace">
      <section className="energy-executive" aria-label="Enerji yönetim komuta yüzeyi">
        <div className="energy-executive__top">
          <div className="energy-executive__intro">
            <p className="energy-executive__eyebrow">ENERJİ YÖNETİMİ</p>
            <h1>Enerji</h1>
            <p className="energy-executive__subtitle">
              Enjeksiyon makinesi ve yardımcı ekipman tüketimini tek yüzeyde izleyin; hedef,
              baz ve değerlendirme kanıtlarını operasyonel karar düzeyinde görün.
            </p>
            <ul className="energy-executive__meta" aria-label="Raporlama meta verisi">
              <li>{meta.facilityLabel}</li>
              <li>
                Raporlama:{" "}
                {formatEnergyPeriod(meta.reportingPeriodStartUtc, meta.reportingPeriodEndUtc)}
              </li>
              <li>Tazelik: {formatEnergyDateTime(meta.latestFreshnessUtc)}</li>
              <li>Kanıt: {meta.evidenceLabel}</li>
            </ul>
          </div>
          <div className="energy-executive__actions">
            <button
              type="button"
              className="energy-button"
              disabled={isPending}
              onClick={handleRefresh}
              title={isPending ? "Çalışma alanı yenileniyor" : "Çalışma alanını yenile"}
            >
              {isPending ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" size={14} />
              ) : (
                <RefreshCw aria-hidden="true" size={14} />
              )}
              Yenile
            </button>
          </div>
        </div>

        {meta.hasDevelopmentPilotData ? (
          <p className="energy-provenance-strip" role="status" data-testid="energy-provenance-strip">
            <span className="energy-provenance-strip__badge">Simülasyon</span>
            <span>{ENERGY_DEVELOPMENT_BANNER}</span>
          </p>
        ) : null}
      </section>

      {workspace.partialErrors.length > 0 ? (
        <div className="energy-partial-error" role="status">
          <strong>Kısmi yükleme uyarısı:</strong>
          <ul style={{ margin: "0.35rem 0 0", paddingLeft: "1.1rem" }}>
            {workspace.partialErrors.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </div>
      ) : null}

      {error ? (
        <div className="energy-error-banner" role="alert">
          {error}
        </div>
      ) : null}

      {!canManage ? (
        <div className="energy-readonly-note" role="status" data-testid="energy-readonly-banner">
          Salt okunur erişim: hedef güncelleme ve yönetim işlemleri devre dışıdır.
        </div>
      ) : null}

      <div className="energy-kpi-grid" data-testid="energy-kpi-grid" aria-label="Enerji göstergeleri">
        {workspace.kpis.map((kpi) => {
          const presentation = resolveKpiPresentation(kpi, workspace.trend);
          const evidenceRail = resolveKpiEvidenceRail(kpi);
          const Icon = presentation.icon;
          const sparklinePath =
            presentation.sparklineValues !== null
              ? createSparklinePath(presentation.sparklineValues)
              : null;

          return (
            <article
              key={kpi.id}
              className={`energy-kpi-card energy-kpi-card--${presentation.tone}${kpi.state !== "available" ? " energy-kpi-card--unavailable" : ""}`}
              data-testid="energy-kpi-card"
              data-kpi-id={kpi.id}
              data-kpi-status={presentation.statusLabel}
              data-kpi-tone={presentation.tone}
              aria-label={presentation.accessibleSummary ?? kpi.label}
            >
              <div className="energy-kpi-card__top">
                <span className="energy-kpi-card__icon" aria-hidden="true">
                  <Icon size={16} strokeWidth={2} />
                </span>
                <span
                  className="energy-kpi-card__tone"
                  data-testid={`energy-kpi-status-${kpi.id}`}
                >
                  {presentation.statusLabel}
                </span>
              </div>
              <div className="energy-kpi-card__body">
                <p className="energy-kpi-card__label">{kpi.label}</p>
                <div className="energy-kpi-card__value-row">
                  <p className="energy-kpi-card__value" title={kpi.unavailableReason}>
                    {presentation.displayValue}
                  </p>
                  {presentation.displayUnit ? (
                    <span className="energy-kpi-card__unit">{presentation.displayUnit}</span>
                  ) : null}
                </div>
                <p className="energy-kpi-card__meta">
                  {presentation.scopeLabel && kpi.id !== "total-consumption" ? (
                    <>
                      <span className="energy-kpi-card__scope">{presentation.scopeLabel}</span>
                      {" · "}
                    </>
                  ) : null}
                  {kpi.id === "total-consumption" ? presentation.scopeLabel : kpi.meta}
                </p>
              </div>
              <div className="energy-kpi-card__footer">
                <div className="energy-kpi-card__footer-top">
                  <p className="energy-kpi-card__evidence">Kanıt: {kpi.evidenceLabel}</p>
                  {sparklinePath ? (
                    <svg
                      className="energy-kpi-card__sparkline"
                      viewBox="0 0 112 28"
                      aria-hidden="true"
                      data-testid="energy-kpi-sparkline"
                    >
                      <path d={sparklinePath} />
                    </svg>
                  ) : null}
                </div>
                {evidenceRail ? (
                  <div
                    className="energy-kpi-card__evidence-rail"
                    aria-label={evidenceRail.accessibleSummary ?? evidenceRail.railLabel}
                  >
                    <p className="energy-kpi-card__evidence-rail-label">{evidenceRail.railLabel}</p>
                    <div className="energy-kpi-card__evidence-rail-track">
                      <span
                        className="energy-kpi-card__evidence-rail-fill"
                        style={{ width: `${evidenceRail.progressPercent}%` }}
                      />
                      {evidenceRail.markerPercent !== null ? (
                        <span
                          className="energy-kpi-card__evidence-rail-marker"
                          style={{ left: `${evidenceRail.markerPercent}%` }}
                        />
                      ) : null}
                    </div>
                  </div>
                ) : null}
              </div>
            </article>
          );
        })}
      </div>

      <div
        ref={tabListRef}
        className="energy-tab-rail"
        data-testid="energy-tab-rail"
        role="tablist"
        aria-label="Enerji çalışma alanı sekmeleri"
      >
        {ENERGY_TAB_OPTIONS.map((tab, index) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`energy-tab-${tab.id}`}
            aria-selected={activeTab === tab.id}
            aria-controls={`energy-tab-panel-${tab.id}`}
            className="energy-tab"
            onClick={() => setActiveTab(tab.id)}
            onKeyDown={(event) => handleTabKeyDown(event, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "overview" ? (
        <div
          id="energy-tab-panel-overview"
          role="tabpanel"
          aria-labelledby="energy-tab-overview"
          className="energy-panel-grid energy-panel-grid--overview"
          data-testid="energy-overview-grid"
        >
          <section className="energy-panel energy-panel--chart" aria-label="Tüketim eğilimi">
            <div className="energy-panel__header">
              <h2 className="energy-panel__title">Tüketim eğilimi</h2>
            </div>
            {isPending && workspace.trend.length === 0 ? (
              <div className="energy-empty">
                <LoaderCircle aria-hidden="true" className="animate-spin" size={18} />
                <span>Trend verisi yükleniyor…</span>
              </div>
            ) : (
              <EnergyTrendChart
                trend={workspace.trend}
                insufficientData={workspace.trendInsufficientData}
                scope={workspace.trendScope}
                bucketDurationMs={workspace.trendBucketDurationMs}
                reportingTotalKwh={workspace.trendReportingTotalKwh}
                targetComparisonNote={workspace.trendTargetComparisonNote}
                baselineComparisonNote={workspace.trendBaselineComparisonNote}
              />
            )}
          </section>

          <section className="energy-panel energy-panel--distribution" aria-label="Enerji dağılımı">
            <div className="energy-panel__header">
              <h2 className="energy-panel__title">Enerji dağılımı</h2>
            </div>
            <p className="energy-panel__meta" data-testid="energy-allocation-reconciliation">
              Toplam: {formatEnergyKwh(workspace.reportingTotalKwh)} kWh · Dağıtılan:{" "}
              {formatEnergyKwh(workspace.allocatedConsumptionKwh)} kWh · Kapsama:{" "}
              {formatEnergyNumber(workspace.allocationCoveragePercent, 1)}%
              {workspace.residualConsumptionKwh > 0.0001
                ? ` · Dağıtılmamış: ${formatEnergyKwh(workspace.residualConsumptionKwh)} kWh`
                : ""}
            </p>
            {workspace.allocations.length === 0 ? (
              <p className="energy-empty">Dağılım verisi bulunmuyor.</p>
            ) : (
              <div className="energy-allocation-layout" data-testid="energy-allocation-layout">
                <EnergyAllocationDonut
                  slices={workspace.allocations}
                  totalKwh={workspace.reportingTotalKwh}
                  coveragePercent={workspace.allocationCoveragePercent}
                />
                <div className="energy-allocation-list" data-testid="energy-allocation-list">
                  {workspace.allocations.map((slice) => (
                    <div
                      key={slice.id}
                      className={`energy-allocation-row${slice.tone === "residual" ? " energy-allocation-row--residual" : ""}`}
                      data-testid="energy-allocation-row"
                    >
                      <span className="energy-allocation-row__label">{slice.label}</span>
                      <span className="energy-allocation-row__value">
                        {formatEnergyKwh(slice.kwh)} kWh · {formatEnergyNumber(slice.percent, 1)}%
                      </span>
                      <div className="energy-allocation-row__bar" aria-hidden="true">
                        <span style={{ width: `${Math.min(slice.percent, 100)}%` }} />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </section>

          <div
            className="energy-panel-grid energy-panel-grid--lower"
            data-testid="energy-overview-lower-grid"
          >
            <section
              className="energy-panel"
              aria-label="Enerji performansı"
              data-testid="energy-overview-intelligence"
            >
              <h2 className="energy-panel__title">Enerji performansı</h2>
              {overviewIntelligenceRows.length === 0 ? (
                <p className="energy-empty">Değerlendirme özeti bulunmuyor.</p>
              ) : (
                <div className="energy-intelligence-list">
                  {overviewIntelligenceRows.map((row) => {
                    const linkedAssessment = workspace.assessments.find(
                      (item) => item.assessmentReference === row.assessmentReference,
                    );
                    const assessmentTitle =
                      linkedAssessment?.assessmentTitle
                      ?? getEnergyAssessmentTitle(row.assessmentReference);

                    return (
                      <div key={row.id} className="energy-intelligence-row">
                        <div>
                          <p className="energy-intelligence-row__title">{assessmentTitle}</p>
                          <p className="energy-intelligence-row__meta">
                            <span className={outcomeBadgeClass(row.outcome)}>{row.outcomeLabel}</span>
                            {" · "}
                            {row.scopeLabel} · {row.evaluationPeriodLabel}
                          </p>
                          <p className="energy-intelligence-row__meta">
                            {row.leadingDriverLabel}
                            {row.comparisonLabel ? ` · ${row.comparisonLabel}` : ""}
                            {" · Kanıt: "}
                            {linkedAssessment?.evidenceLabel ?? row.freshnessLabel}
                          </p>
                        </div>
                        <button
                          type="button"
                          className="energy-button"
                          disabled={isPending || !linkedAssessment}
                          title={
                            linkedAssessment
                              ? "Değerlendirme detayını aç"
                              : "İlgili değerlendirme bulunamadı"
                          }
                          onClick={() => {
                            if (linkedAssessment) {
                              selectAssessment(linkedAssessment, { switchTab: true });
                            }
                          }}
                        >
                          Detay
                        </button>
                      </div>
                    );
                  })}
                </div>
              )}
              {workspace.intelligenceRows.length > overviewIntelligenceRows.length ? (
                <p className="energy-panel__meta" style={{ marginTop: "0.45rem" }}>
                  Tüm değerlendirmeler{" "}
                  <button
                    type="button"
                    className="energy-button"
                    style={{ padding: "0.2rem 0.45rem", fontSize: "0.68rem" }}
                    onClick={() => setActiveTab("assessments")}
                  >
                    Değerlendirmeler sekmesinde
                  </button>
                </p>
              ) : null}
            </section>

            <section
              className="energy-panel"
              aria-label="Hedef karşılaştırması"
              data-testid="energy-target-comparison"
            >
              <h2 className="energy-panel__title">Hedef karşılaştırması</h2>
              <div className="energy-target-comparison">
                {targetComparison.hasApprovedTarget ? (
                  <>
                    <p className="energy-target-comparison__summary">
                      Onaylı tüketim hedefi ile gerçekleşen tüketim karşılaştırması.
                    </p>
                    <dl>
                      <dt>Gerçekleşen</dt>
                      <dd>{targetComparison.actualLabel}</dd>
                      <dt>Onaylı hedef</dt>
                      <dd>{targetComparison.approvedTargetLabel}</dd>
                      <dt>Sapma</dt>
                      <dd>{targetComparison.deviationLabel}</dd>
                      <dt>Kanıt</dt>
                      <dd>{targetComparison.evidenceLabel}</dd>
                      <dt>Sonuç</dt>
                      <dd>{targetComparison.outcomeLabel}</dd>
                    </dl>
                  </>
                ) : (
                  <>
                    <p className="energy-target-comparison__summary">
                      Onaylı tüketim hedefi bulunamadı; karşılaştırma kanıtı yetersiz.
                    </p>
                    <dl>
                      <dt>Gerçekleşen</dt>
                      <dd>{targetComparison.actualLabel}</dd>
                      <dt>Onaylı hedef</dt>
                      <dd>—</dd>
                      <dt>Sapma</dt>
                      <dd>{targetComparison.deviationLabel}</dd>
                      <dt>Kanıt</dt>
                      <dd>{targetComparison.evidenceLabel}</dd>
                      <dt>Sonuç</dt>
                      <dd>{targetComparison.outcomeLabel}</dd>
                    </dl>
                  </>
                )}
                <p className="energy-panel__meta" style={{ margin: 0 }}>
                  {ENERGY_OPPORTUNITY_DISCLAIMER}
                </p>
              </div>
            </section>
          </div>
        </div>
      ) : null}

      {activeTab === "measurement-points" ? (
        <section
          id="energy-tab-panel-measurement-points"
          role="tabpanel"
          aria-labelledby="energy-tab-measurement-points"
          data-testid="energy-measurement-registry"
        >
          <div
            className={`energy-registry-layout${selectedMeasurement ? " energy-registry-layout--detail" : ""}`}
          >
            <div className="energy-panel">
              <h2 className="energy-panel__title">Ölçüm noktaları</h2>
              <div className="energy-registry-toolbar">
                <label style={{ display: "inline-flex", alignItems: "center", gap: "0.35rem" }}>
                  <Search aria-hidden="true" size={14} />
                  <span className="sr-only">Ölçüm noktası ara</span>
                  <input
                    type="search"
                    placeholder="Ad veya kod ara"
                    value={measurementSearch}
                    onChange={(event) => setMeasurementSearch(event.target.value)}
                  />
                </label>
                <select
                  aria-label="Kapsam filtresi"
                  value={measurementScopeFilter}
                  onChange={(event) => setMeasurementScopeFilter(event.target.value)}
                >
                  {ENERGY_SCOPE_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Ekipman filtresi"
                  value={measurementEquipmentFilter}
                  onChange={(event) => setMeasurementEquipmentFilter(event.target.value)}
                >
                  {ENERGY_EQUIPMENT_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Durum filtresi"
                  value={measurementStatusFilter}
                  onChange={(event) => setMeasurementStatusFilter(event.target.value)}
                >
                  {MEASUREMENT_STATUS_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Kanıt filtresi"
                  value={measurementEvidenceFilter}
                  onChange={(event) => setMeasurementEvidenceFilter(event.target.value)}
                >
                  {MEASUREMENT_EVIDENCE_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                {measurementFiltersActive ? (
                  <button type="button" className="energy-button" onClick={resetMeasurementFilters}>
                    <X aria-hidden="true" size={14} />
                    Temizle
                  </button>
                ) : null}
              </div>

              {filteredMeasurementPoints.length === 0 ? (
                <div className="energy-empty">
                  <Zap aria-hidden="true" size={22} />
                  <p>
                    {measurementFiltersActive
                      ? "Seçili filtrelere uygun ölçüm noktası yok."
                      : "Kayıtlı ölçüm noktası bulunmuyor."}
                  </p>
                </div>
              ) : (
                <table className="energy-registry-table">
                  <thead>
                    <tr>
                      <th>Kod</th>
                      <th>Ad</th>
                      <th>Kapsam</th>
                      <th>Ekipman</th>
                      <th>Durum</th>
                      <th>Kanıt</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredMeasurementPoints.map((point) => (
                      <tr
                        key={point.pointCode}
                        ref={(node) => {
                          if (node) {
                            measurementRowRefs.current.set(point.pointCode, node);
                          } else {
                            measurementRowRefs.current.delete(point.pointCode);
                          }
                        }}
                        tabIndex={0}
                        className={point.pointCode === selectedMeasurementCode ? "is-selected" : ""}
                        onClick={() => selectMeasurementPoint(point)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            selectMeasurementPoint(point);
                          }
                        }}
                      >
                        <td>{point.pointCode}</td>
                        <td>{point.displayName}</td>
                        <td>{point.measurementScopeLabel}</td>
                        <td>{point.equipmentCategoryLabel}</td>
                        <td>{point.statusLabel}</td>
                        <td>{point.evidenceLabel}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>

            {selectedMeasurement ? (
              <aside className="energy-detail-panel" aria-label="Ölçüm noktası detayı">
                <div className="energy-detail-panel__header">
                  <div>
                    <h3>{selectedMeasurement.displayName}</h3>
                    <p className="energy-kpi-card__meta">{selectedMeasurement.pointCode}</p>
                  </div>
                  <button
                    type="button"
                    className="energy-button"
                    aria-label="Ölçüm noktası detayını kapat"
                    onClick={closeMeasurementDetail}
                  >
                    <X aria-hidden="true" size={14} />
                  </button>
                </div>

                {isPending && !measurementDetail ? (
                  <div className="energy-empty">
                    <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
                    Detay yükleniyor…
                  </div>
                ) : measurementDetail ? (
                  <dl>
                    <dt>Kapsam</dt>
                    <dd>{measurementDetail.point.measurementScopeLabel}</dd>
                    <dt>Ekipman</dt>
                    <dd>{measurementDetail.point.equipmentCategoryLabel}</dd>
                    <dt>Makine</dt>
                    <dd>{measurementDetail.point.machineAssetLabel ?? "—"}</dd>
                    <dt>Durum</dt>
                    <dd>{measurementDetail.point.statusLabel}</dd>
                    <dt>Son okuma</dt>
                    <dd>{measurementDetail.point.latestReadingLabel ?? "—"}</dd>
                    <dt>Pencere</dt>
                    <dd>{measurementDetail.currentWindowLabel ?? "—"}</dd>
                    <dt>Güç</dt>
                    <dd>{measurementDetail.powerLabel ?? "—"}</dd>
                    <dt>Tepe talep</dt>
                    <dd>{measurementDetail.demandLabel ?? "—"}</dd>
                    <dt>Veri kalitesi</dt>
                    <dd>{measurementDetail.dataQualityLabel}</dd>
                    <dt>Hesaplama yöntemi</dt>
                    <dd>{measurementDetail.calculationMethodLabel ?? "—"}</dd>
                    <dt>Kaynak</dt>
                    <dd>{measurementDetail.sourceProvenance}</dd>
                    <dt>Dağılım</dt>
                    <dd>
                      {measurementDetail.allocationLabels.length > 0
                        ? measurementDetail.allocationLabels.join(" · ")
                        : "—"}
                    </dd>
                  </dl>
                ) : (
                  <p className="energy-empty">Detay yüklenemedi.</p>
                )}
              </aside>
            ) : null}
          </div>
        </section>
      ) : null}

      {activeTab === "targets-baselines" ? (
        <section
          id="energy-tab-panel-targets-baselines"
          role="tabpanel"
          aria-labelledby="energy-tab-targets-baselines"
          data-testid="energy-targets-panel"
        >
          {!canManage ? (
            <p className="energy-readonly-note" role="status">
              Hedefler salt okunur modda görüntülenir; güncelleme için energy.manage yetkisi gerekir.
            </p>
          ) : null}

          <div className="energy-panel-grid">
            <section className="energy-panel" aria-label="Baz değerler">
              <h2 className="energy-panel__title">Baz değerler</h2>
              {workspace.baselines.length === 0 ? (
                <p className="energy-empty">Hesaplanmış baz kaydı bulunmuyor.</p>
              ) : (
                <div style={{ display: "grid", gap: "0.65rem" }}>
                  {workspace.baselines.map(renderBaselineCard)}
                </div>
              )}
            </section>

            <section className="energy-panel" aria-label="Hedefler">
              <h2 className="energy-panel__title">Hedefler</h2>
              {workspace.targets.length === 0 ? (
                <p className="energy-empty">Onaylı hedef kaydı bulunmuyor.</p>
              ) : (
                <div style={{ display: "grid", gap: "0.65rem" }}>
                  {workspace.targets.map(renderTargetCard)}
                </div>
              )}
            </section>
          </div>
        </section>
      ) : null}

      {activeTab === "assessments" ? (
        <section
          id="energy-tab-panel-assessments"
          role="tabpanel"
          aria-labelledby="energy-tab-assessments"
          data-testid="energy-assessments-panel"
        >
          <div
            className={`energy-registry-layout${selectedAssessment ? " energy-registry-layout--detail" : ""}`}
          >
            <div className="energy-panel">
              <h2 className="energy-panel__title">Değerlendirmeler</h2>
              <div className="energy-registry-toolbar">
                <select
                  aria-label="Sonuç filtresi"
                  value={assessmentOutcomeFilter}
                  onChange={(event) => setAssessmentOutcomeFilter(event.target.value)}
                >
                  {ENERGY_OUTCOME_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Sürücü filtresi"
                  value={assessmentDriverFilter}
                  onChange={(event) => setAssessmentDriverFilter(event.target.value)}
                >
                  {driverFilterOptions.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Kapsam filtresi"
                  value={assessmentScopeFilter}
                  onChange={(event) => setAssessmentScopeFilter(event.target.value)}
                >
                  {ENERGY_SCOPE_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Kanıt filtresi"
                  value={assessmentEvidenceFilter}
                  onChange={(event) => setAssessmentEvidenceFilter(event.target.value)}
                >
                  {ASSESSMENT_EVIDENCE_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                {assessmentFiltersActive ? (
                  <button type="button" className="energy-button" onClick={resetAssessmentFilters}>
                    <X aria-hidden="true" size={14} />
                    Temizle
                  </button>
                ) : null}
              </div>

              {filteredAssessments.length === 0 ? (
                <div className="energy-empty">
                  <Zap aria-hidden="true" size={22} />
                  <p>
                    {assessmentFiltersActive
                      ? "Seçili filtrelere uygun değerlendirme yok."
                      : "Değerlendirme kaydı bulunmuyor."}
                  </p>
                </div>
              ) : (
                <table className="energy-registry-table">
                  <thead>
                    <tr>
                      <th>Değerlendirme</th>
                      <th>Dönem</th>
                      <th>Sonuç</th>
                      <th>Sürücü</th>
                      <th>Kanıt</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredAssessments.map((assessment) => (
                      <tr
                        key={assessment.assessmentReference}
                        ref={(node) => {
                          if (node) {
                            assessmentRowRefs.current.set(assessment.assessmentReference, node);
                          } else {
                            assessmentRowRefs.current.delete(assessment.assessmentReference);
                          }
                        }}
                        tabIndex={0}
                        className={
                          assessment.assessmentReference === selectedAssessmentRef
                            ? "is-selected"
                            : ""
                        }
                        onClick={() => selectAssessment(assessment)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            selectAssessment(assessment);
                          }
                        }}
                      >
                        <td>
                          <span className="energy-assessment-title">{assessment.assessmentTitle}</span>
                          <span className="energy-reference-meta">{assessment.assessmentReference}</span>
                        </td>
                        <td>{assessment.evaluationPeriodLabel}</td>
                        <td>
                          <span className={outcomeBadgeClass(assessment.outcome)}>
                            {assessment.outcomeLabel}
                          </span>
                        </td>
                        <td>{assessment.leadingDriverLabel}</td>
                        <td>{assessment.evidenceLabel}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>

            {selectedAssessment ? (
              <aside className="energy-detail-panel" aria-label="Değerlendirme detayı">
                <div className="energy-detail-panel__header">
                  <div>
                    <h3>{selectedAssessment.assessmentTitle}</h3>
                    <p className="energy-kpi-card__meta">
                      <span className="energy-reference-meta">
                        {selectedAssessment.assessmentReference}
                      </span>
                      {" · "}
                      {selectedAssessment.comparisonDimensionLabel} ·{" "}
                      {selectedAssessment.evaluationPeriodLabel}
                    </p>
                  </div>
                  <button
                    type="button"
                    className="energy-button"
                    aria-label="Değerlendirme detayını kapat"
                    onClick={closeAssessmentDetail}
                  >
                    <X aria-hidden="true" size={14} />
                  </button>
                </div>

                {isPending && !assessmentDetail ? (
                  <div className="energy-empty">
                    <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
                    Detay yükleniyor…
                  </div>
                ) : assessmentDetail ? (
                  <>
                    <dl>
                      <dt>Sonuç</dt>
                      <dd>
                        <span className={outcomeBadgeClass(assessmentDetail.summary.outcome)}>
                          {getEnergyOutcomeLabel(assessmentDetail.summary.outcome)}
                        </span>
                      </dd>
                      <dt>Sapma</dt>
                      <dd>{assessmentDetail.varianceLabel ?? "—"}</dd>
                      <dt>Baz</dt>
                      <dd>{assessmentDetail.baselineLabel ?? "—"}</dd>
                      <dt>Hedef</dt>
                      <dd>{assessmentDetail.targetLabel ?? "—"}</dd>
                      <dt>Öncelikli sürücü</dt>
                      <dd>{assessmentDetail.summary.leadingDriverLabel}</dd>
                      <dt>Değerlendirme zamanı</dt>
                      <dd>{formatEnergyDateTime(assessmentDetail.summary.evaluatedAtUtc)}</dd>
                      <dt>Kanıt</dt>
                      <dd>{assessmentDetail.summary.evidenceLabel}</dd>
                    </dl>

                    <section style={{ marginTop: "0.75rem" }} aria-label="Yapılandırılmış kanıtlar">
                      <h4 className="energy-panel__title">Sürücü kanıtları</h4>
                      {assessmentDetail.drivers.length === 0 ? (
                        <p className="energy-empty">Sürücü kanıtı bulunmuyor.</p>
                      ) : (
                        <ul style={{ margin: 0, paddingLeft: "1rem" }}>
                          {assessmentDetail.drivers.map((driver) => (
                            <li key={driver.id} style={{ marginBottom: "0.45rem" }}>
                              <strong>
                                {getEnergyDriverLabel(driver.reasonCode, driver.labelTr)}
                              </strong>
                              {" · "}
                              {getEnergySeverityLabel(driver.severity)}
                              {driver.isLeadingDriver ? " · Öncelikli" : ""}
                              {driver.explanation ? ` · ${driver.explanation}` : ""}
                            </li>
                          ))}
                        </ul>
                      )}
                    </section>

                    <details style={{ marginTop: "0.75rem" }}>
                      <summary>Teknik ayrıntılar</summary>
                      <dl style={{ marginTop: "0.55rem" }}>
                        <dt>Kural seti</dt>
                        <dd>{assessmentDetail.ruleSetVersion}</dd>
                        <dt>Kaynak</dt>
                        <dd>{assessmentDetail.provenanceLabel ?? "—"}</dd>
                        {assessmentDetail.drivers.map((driver) => (
                          <div key={`tech-${driver.id}`}>
                            <dt>{driver.labelTr}</dt>
                            <dd>
                              {[
                                driver.actualValue !== null
                                  ? `Gerçek: ${formatEnergyNumber(driver.actualValue)} ${driver.actualUnit ?? ""}`
                                  : null,
                                driver.comparisonValue !== null
                                  ? `Karşılaştırma: ${formatEnergyNumber(driver.comparisonValue)} ${driver.comparisonUnit ?? ""}`
                                  : null,
                                driver.normalizedVariance !== null
                                  ? `Normalize sapma: ${formatEnergyNumber(driver.normalizedVariance)}`
                                  : null,
                                driver.evidenceSource ? `Kaynak: ${driver.evidenceSource}` : null,
                                driver.freshnessLabel
                                  ? `Tazelik: ${driver.freshnessLabel}`
                                  : null,
                              ]
                                .filter(Boolean)
                                .join(" · ") || "—"}
                            </dd>
                          </div>
                        ))}
                      </dl>
                    </details>
                  </>
                ) : (
                  <p className="energy-empty">Detay yüklenemedi.</p>
                )}
              </aside>
            ) : null}
          </div>
        </section>
      ) : null}

      {targetEdit ? (
        <div className="energy-dialog-backdrop" role="presentation">
          <div
            className="energy-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="energy-target-dialog-title"
          >
            <h3 id="energy-target-dialog-title">
              {targetEdit.confirmOpen ? "Güncellemeyi onayla" : "Hedef güncelle"}
            </h3>
            <p className="energy-kpi-card__meta">{targetEdit.target.targetReference}</p>

            {targetDialogError ? (
              <div className="energy-error-banner" role="alert" style={{ marginBottom: "0.55rem" }}>
                {targetDialogError}
              </div>
            ) : null}

            {targetEdit.confirmOpen ? (
              <>
                <p>
                  {formatEnergyNumber(Number.parseFloat(targetEdit.targetValue))}{" "}
                  {targetEdit.target.unit} değeri ve tolerans {targetEdit.tolerance} olarak
                  kaydedilecek. Devam etmek istiyor musunuz?
                </p>
                <div style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
                  <button
                    type="button"
                    className="energy-button"
                    disabled={isPending}
                    onClick={() =>
                      setTargetEdit((current) =>
                        current ? { ...current, confirmOpen: false } : current,
                      )
                    }
                  >
                    Geri
                  </button>
                  <button
                    type="button"
                    className="energy-button energy-button--primary"
                    disabled={isPending}
                    onClick={submitTargetUpdate}
                  >
                    {isPending ? (
                      <LoaderCircle aria-hidden="true" className="animate-spin" size={14} />
                    ) : null}
                    Onayla ve kaydet
                  </button>
                </div>
              </>
            ) : (
              <>
                <label>
                  Hedef değeri
                  <input
                    type="number"
                    step="any"
                    value={targetEdit.targetValue}
                    onChange={(event) =>
                      setTargetEdit((current) =>
                        current ? { ...current, targetValue: event.target.value } : current,
                      )
                    }
                  />
                </label>
                <label>
                  Tolerans
                  <input
                    type="number"
                    step="any"
                    value={targetEdit.tolerance}
                    onChange={(event) =>
                      setTargetEdit((current) =>
                        current ? { ...current, tolerance: event.target.value } : current,
                      )
                    }
                  />
                </label>
                <label>
                  Başlangıç
                  <input
                    type="date"
                    value={targetEdit.effectiveFromUtc}
                    onChange={(event) =>
                      setTargetEdit((current) =>
                        current ? { ...current, effectiveFromUtc: event.target.value } : current,
                      )
                    }
                  />
                </label>
                <label>
                  Bitiş
                  <input
                    type="date"
                    value={targetEdit.effectiveToUtc}
                    onChange={(event) =>
                      setTargetEdit((current) =>
                        current ? { ...current, effectiveToUtc: event.target.value } : current,
                      )
                    }
                  />
                </label>
                <label>
                  Kaynak açıklaması
                  <textarea
                    rows={2}
                    value={targetEdit.sourceProvenance}
                    onChange={(event) =>
                      setTargetEdit((current) =>
                        current ? { ...current, sourceProvenance: event.target.value } : current,
                      )
                    }
                  />
                </label>
                <label>
                  Durum
                  <select
                    value={targetEdit.status}
                    onChange={(event) =>
                      setTargetEdit((current) =>
                        current ? { ...current, status: event.target.value } : current,
                      )
                    }
                  >
                    <option value="approved">{getEnergyTargetStatusLabel("approved")}</option>
                    <option value="inactive">{getEnergyTargetStatusLabel("inactive")}</option>
                  </select>
                </label>
                <div style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
                  <button
                    type="button"
                    className="energy-button"
                    disabled={isPending}
                    onClick={() => setTargetEdit(null)}
                  >
                    İptal
                  </button>
                  <button
                    type="button"
                    className="energy-button energy-button--primary"
                    disabled={isPending}
                    onClick={() =>
                      setTargetEdit((current) =>
                        current ? { ...current, confirmOpen: true } : current,
                      )
                    }
                  >
                    Devam
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      ) : null}
    </main>
  );
}

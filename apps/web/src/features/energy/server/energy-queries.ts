import "./energy-server-boundary";

import {
  formatEnergyCompactPeriod,
  formatEnergyCurrency,
  formatEnergyKwh,
  formatEnergyKw,
  formatEnergyTrendAxisLabel,
  formatEnergyVariance,
} from "../domain/energy-formatters";
import {
  ENERGY_ALLOCATION_CATEGORY_LABELS,
  ENERGY_OPPORTUNITY_DISCLAIMER,
  getEnergyAllocationCategoryLabel,
  getEnergyBaselineDimensionLabel,
  getEnergyCalculationMethodLabel,
  getEnergyDriverLabel,
  getEnergyEquipmentCategoryLabel,
  getEnergyEvidenceLabel,
  getEnergyFreshnessLabel,
  getEnergyMeasurementScopeLabel,
  getEnergyOutcomeLabel,
  getEnergyAssessmentTitle,
  getEnergySeverityLabel,
  getEnergyStatusLabel,
  getEnergyTargetMetricLabel,
  getEnergyTargetScopeLabel,
  getEnergyTargetStatusLabel,
  localizeEnergyUnit,
} from "../domain/energy-labels";
import type {
  EnergyAllocationSlice,
  EnergyAssessmentDetail,
  EnergyAssessmentSummary,
  EnergyBaselineSummary,
  EnergyIntelligenceRow,
  EnergyKpiCard,
  EnergyMeasurementPointDetail,
  EnergyMeasurementPointSummary,
  EnergyOpportunitySummary,
  EnergyTargetSummary,
  EnergyTrendPoint,
  EnergyWorkspacePayload,
} from "../domain/energy-types";
import {
  getEnergyAssessment,
  getEnergyIntelligenceWorkspace,
  getEnergyMeasurementPoint,
  getEnergyWorkspace,
  listEnergyAllocations,
  listEnergyAssessments,
  listEnergyBaselines,
  listEnergyConsumptionWindows,
  listEnergyMeasurementPoints,
  listEnergyTargets,
  type RawEnergyAllocation,
  type RawEnergyAssessmentSummary,
  type RawEnergyBaseline,
  type RawEnergyConsumptionWindow,
  type RawEnergyMeasurementPoint,
  type RawEnergyTarget,
} from "./energy-api-client";
import { resolveEnergyRequestContext } from "./energy-request-context";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
  code?: string;
}>;

export type EnergyWorkspaceResult =
  | Readonly<{ status: "success"; workspace: EnergyWorkspacePayload; canManage: boolean }>
  | ReadFailure;

export type EnergyMeasurementPointDetailResult =
  | Readonly<{ status: "success"; detail: EnergyMeasurementPointDetail }>
  | ReadFailure;

export type EnergyAssessmentDetailResult =
  | Readonly<{ status: "success"; detail: EnergyAssessmentDetail }>
  | ReadFailure;

function sortWindows(windows: readonly RawEnergyConsumptionWindow[]): RawEnergyConsumptionWindow[] {
  return [...windows].sort(
    (a, b) => new Date(a.windowStartUtc).getTime() - new Date(b.windowStartUtc).getTime(),
  );
}

function pickPrimaryWindow(
  windows: readonly RawEnergyConsumptionWindow[],
): RawEnergyConsumptionWindow | null {
  const valid = windows.filter(
    (window) =>
      window.evidenceStatus !== "source-conflict"
      && window.evidenceStatus !== "insufficient-data"
      && window.consumedKwh > 0,
  );
  if (valid.length === 0) {
    return windows[0] ?? null;
  }

  return [...valid].sort(
    (a, b) => new Date(b.windowEndUtc).getTime() - new Date(a.windowEndUtc).getTime(),
  )[0];
}

function mapMeasurementPoint(
  point: RawEnergyMeasurementPoint,
  latestWindow?: RawEnergyConsumptionWindow | null,
): EnergyMeasurementPointSummary {
  return {
    id: point.id,
    pointCode: point.pointCode,
    displayName: point.displayName,
    measurementScope: point.measurementScope,
    measurementScopeLabel: getEnergyMeasurementScopeLabel(point.measurementScope),
    equipmentCategory: point.equipmentCategory,
    equipmentCategoryLabel: getEnergyEquipmentCategoryLabel(point.equipmentCategory),
    machineAssetLabel: point.machineAssetLabel,
    status: point.status,
    statusLabel: getEnergyStatusLabel(point.status),
    evidenceLabel: getEnergyEvidenceLabel(latestWindow?.evidenceStatus ?? point.sourceProvenance),
    freshnessLabel: getEnergyFreshnessLabel(latestWindow?.freshnessState ?? "unknown"),
    latestReadingLabel: latestWindow
      ? `${formatEnergyKwh(latestWindow.consumedKwh)} kWh · ${formatEnergyCompactPeriod(latestWindow.windowStartUtc, latestWindow.windowEndUtc)}`
      : null,
    isPilotData: point.isPilotData,
  };
}

function mapBaseline(baseline: RawEnergyBaseline): EnergyBaselineSummary {
  return {
    id: baseline.id,
    baselineReference: baseline.baselineReference,
    comparisonDimension: baseline.comparisonDimension,
    comparisonDimensionLabel: getEnergyBaselineDimensionLabel(baseline.comparisonDimension),
    measurementPointCode: baseline.measurementPointCode,
    sampleCount: baseline.sampleCount,
    medianCenter: baseline.medianCenter,
    madDispersion: baseline.madDispersion,
    relativeTolerance: baseline.relativeTolerance,
    upperThresholdConsumedKwh: baseline.upperThresholdConsumedKwh,
    evidenceLabel: getEnergyEvidenceLabel(baseline.evidenceStatus),
    provenance: baseline.provenance,
    ruleSetVersion: baseline.ruleSetVersion,
    isPilotData: baseline.isPilotData,
    isCalculated: true,
  };
}

function mapTarget(target: RawEnergyTarget): EnergyTargetSummary {
  return {
    id: target.id,
    targetReference: target.targetReference,
    targetScope: target.targetScope,
    targetScopeLabel: getEnergyTargetScopeLabel(target.targetScope),
    targetMetric: target.targetMetric,
    targetMetricLabel: getEnergyTargetMetricLabel(target.targetMetric),
    targetValue: target.targetValue,
    unit: localizeEnergyUnit(target.unit, target.targetMetric),
    tolerance: target.tolerance,
    effectiveFromUtc: target.effectiveFromUtc,
    effectiveToUtc: target.effectiveToUtc,
    sourceProvenance: target.sourceProvenance,
    status: target.status,
    statusLabel: getEnergyTargetStatusLabel(target.status),
    version: target.version,
    isPilotData: target.isPilotData,
    isApprovedTarget: target.status === "approved",
  };
}

function mapAssessment(assessment: RawEnergyAssessmentSummary): EnergyAssessmentSummary {
  return {
    id: assessment.id,
    assessmentReference: assessment.assessmentReference,
    assessmentTitle: getEnergyAssessmentTitle(
      assessment.assessmentReference,
      assessment.leadingDriverCode,
    ),
    comparisonDimension: assessment.comparisonDimension,
    comparisonDimensionLabel: getEnergyBaselineDimensionLabel(assessment.comparisonDimension),
    measurementPointCode: assessment.measurementPointCode,
    evaluationWindowStartUtc: assessment.evaluationWindowStartUtc,
    evaluationWindowEndUtc: assessment.evaluationWindowEndUtc,
    evaluationPeriodLabel: formatEnergyCompactPeriod(
      assessment.evaluationWindowStartUtc,
      assessment.evaluationWindowEndUtc,
    ),
    actualConsumedKwh: assessment.actualConsumedKwh,
    actualMaximumDemandKw: assessment.actualMaximumDemandKw,
    varianceConsumedKwh: assessment.varianceConsumedKwh,
    variancePercent: assessment.variancePercent,
    outcome: assessment.outcome,
    outcomeLabel: getEnergyOutcomeLabel(assessment.outcome),
    leadingDriverCode: assessment.leadingDriverCode,
    leadingDriverLabel: getEnergyDriverLabel(assessment.leadingDriverCode),
    evidenceLabel: getEnergyEvidenceLabel(assessment.evidenceStatus),
    evaluatedAtUtc: assessment.evaluatedAtUtc,
    isPilotData: assessment.isPilotData,
  };
}

function deriveReportingPeriod(
  windows: readonly RawEnergyConsumptionWindow[],
): Readonly<{ startUtc: string | null; endUtc: string | null }> {
  const valid = windows.filter((window) => window.evidenceStatus !== "source-conflict");
  if (valid.length === 0) {
    return { startUtc: null, endUtc: null };
  }

  const starts = valid.map((window) => new Date(window.windowStartUtc).getTime());
  const ends = valid.map((window) => new Date(window.windowEndUtc).getTime());

  return {
    startUtc: new Date(Math.min(...starts)).toISOString(),
    endUtc: new Date(Math.max(...ends)).toISOString(),
  };
}

function resolveAllocationBucket(
  allocation: RawEnergyAllocation,
  pointByMachine: Map<string, RawEnergyMeasurementPoint>,
  pointById: Map<string, RawEnergyMeasurementPoint>,
  windowToPointId: Map<string, string>,
): Readonly<{ label: string; tone: EnergyAllocationSlice["tone"] }> {
  if (allocation.targetType === "unallocated") {
    return {
      label: "Dağıtılmamış / ilişkilendirilememiş tüketim",
      tone: "residual",
    };
  }

  const linkedPointId = windowToPointId.get(allocation.consumptionWindowId);
  const linkedPoint = linkedPointId ? pointById.get(linkedPointId) : undefined;

  if (allocation.targetType === "machine") {
    const point = allocation.targetReference
      ? pointByMachine.get(allocation.targetReference) ?? linkedPoint
      : linkedPoint;
    return {
      label: getEnergyAllocationCategoryLabel(point?.equipmentCategory ?? "injection-machine"),
      tone: "machine",
    };
  }

  if (allocation.targetType === "auxiliary-equipment") {
    const point = allocation.targetReference
      ? pointByMachine.get(allocation.targetReference) ?? linkedPoint
      : linkedPoint;
    const category = point?.equipmentCategory ?? "other";
    if (category === "injection-machine") {
      return {
        label: ENERGY_ALLOCATION_CATEGORY_LABELS["injection-machine"],
        tone: "machine",
      };
    }

    return {
      label: getEnergyAllocationCategoryLabel(category),
      tone: "auxiliary",
    };
  }

  return {
    label: getEnergyAllocationCategoryLabel(linkedPoint?.equipmentCategory),
    tone: "auxiliary",
  };
}

function normalizeAllocationPercents(
  slices: readonly EnergyAllocationSlice[],
  totalKwh: number,
): EnergyAllocationSlice[] {
  if (totalKwh <= 0 || slices.length === 0) {
    return [...slices];
  }

  const normalized = slices.map((slice) => ({
    ...slice,
    percent: (slice.kwh / totalKwh) * 100,
  }));

  const percentTotal = normalized.reduce((sum, slice) => sum + slice.percent, 0);
  if (normalized.length > 0 && Math.abs(percentTotal - 100) > 0.05) {
    const largestIndex = normalized.reduce(
      (bestIndex, slice, index, all) => (slice.kwh > all[bestIndex].kwh ? index : bestIndex),
      0,
    );
    normalized[largestIndex] = {
      ...normalized[largestIndex],
      percent: normalized[largestIndex].percent + (100 - percentTotal),
    };
  }

  return normalized;
}

const RESIDUAL_ALLOCATION_LABEL = "Dağıtılmamış / ilişkilendirilememiş tüketim";

function buildDistributionResult(
  allocations: readonly RawEnergyAllocation[],
  points: readonly RawEnergyMeasurementPoint[],
  windows: readonly RawEnergyConsumptionWindow[],
  totalKwh: number,
): Readonly<{
  slices: EnergyAllocationSlice[];
  totalKwh: number;
  distributedKwh: number;
  residualKwh: number;
  coveragePercent: number;
}> {
  const pointById = new Map(points.map((point) => [point.id, point]));
  const pointByMachine = new Map<string, RawEnergyMeasurementPoint>();
  for (const point of points) {
    if (point.machineAssetId) {
      pointByMachine.set(point.machineAssetId, point);
    }
  }

  const windowToPointId = new Map(
    windows.map((window) => [window.id, window.measurementPointId]),
  );

  const buckets = new Map<string, EnergyAllocationSlice>();

  for (const allocation of allocations) {
    if (allocation.targetType === "unallocated") {
      continue;
    }

    const bucket = resolveAllocationBucket(
      allocation,
      pointByMachine,
      pointById,
      windowToPointId,
    );
    const existing = buckets.get(bucket.label);
    if (existing) {
      buckets.set(bucket.label, {
        ...existing,
        kwh: existing.kwh + allocation.allocatedKwh,
      });
    } else {
      buckets.set(bucket.label, {
        id: bucket.label,
        label: bucket.label,
        kwh: allocation.allocatedKwh,
        percent: 0,
        tone: bucket.tone,
      });
    }
  }

  const distributedKwh = [...buckets.values()].reduce((sum, slice) => sum + slice.kwh, 0);
  const residualKwh = Math.max(0, totalKwh - distributedKwh);
  const slices: EnergyAllocationSlice[] = [...buckets.values()].sort((a, b) => b.kwh - a.kwh);

  if (residualKwh > 0.0001) {
    slices.push({
      id: RESIDUAL_ALLOCATION_LABEL,
      label: RESIDUAL_ALLOCATION_LABEL,
      kwh: residualKwh,
      percent: 0,
      tone: "residual",
    });
  }

  const normalizedSlices = normalizeAllocationPercents(slices, totalKwh);
  const coveragePercent = totalKwh > 0 ? (distributedKwh / totalKwh) * 100 : 0;

  return {
    slices: normalizedSlices,
    totalKwh,
    distributedKwh,
    residualKwh,
    coveragePercent,
  };
}

const MIN_CANONICAL_TREND_BUCKETS = 8;

const DEVELOPMENT_ASSESSMENT_SCENARIO_WINDOW_IDS = new Set([
  "bb000001-0167-4000-8000-000000000167",
  "bb000001-0175-4000-8000-000000000175",
  "bb000001-0178-4000-8000-000000000178",
  "bb000001-017b-4000-8000-00000000017b",
  "bb000001-0168-4000-8000-000000000168",
  "bb000001-0169-4000-8000-000000000169",
]);

const DEVELOPMENT_BASELINE_SAMPLE_WINDOW_IDS = new Set([
  "bb000001-017f-4000-8000-00000000017f",
  "bb000001-0180-4000-8000-000000000180",
  "bb000001-0181-4000-8000-000000000181",
  "bb000001-0182-4000-8000-000000000182",
  "bb000001-0183-4000-8000-000000000183",
]);

type CanonicalTrendBundle = Readonly<{
  trend: EnergyTrendPoint[];
  insufficientData: boolean;
  scope: string;
  bucketDurationMs: number | null;
  reportingTotalKwh: number;
  contributingWindowIds: readonly string[];
  targetComparisonNote: string | null;
  baselineComparisonNote: string | null;
  targetCompatible: boolean;
  baselineCompatible: boolean;
}>;

function sumDecimal(values: readonly number[]): number {
  return values.reduce((total, value) => total + value, 0);
}

function roundEnergy(value: number): number {
  return Math.round(value * 10) / 10;
}

function windowDurationMs(window: RawEnergyConsumptionWindow): number {
  return new Date(window.windowEndUtc).getTime() - new Date(window.windowStartUtc).getTime();
}

function resolveCanonicalWindowDurationMs(
  windows: readonly RawEnergyConsumptionWindow[],
): number | null {
  const candidates = windows.filter(
    (window) => window.evidenceStatus !== "source-conflict" && windowDurationMs(window) > 0,
  );
  if (candidates.length === 0) {
    return null;
  }

  const durationCounts = new Map<number, number>();
  for (const window of candidates) {
    const duration = windowDurationMs(window);
    durationCounts.set(duration, (durationCounts.get(duration) ?? 0) + 1);
  }

  let bestDuration = 0;
  let bestCount = 0;
  for (const [duration, count] of durationCounts) {
    if (count > bestCount) {
      bestCount = count;
      bestDuration = duration;
    }
  }

  return bestDuration > 0 ? bestDuration : null;
}

function filterCompatibleWindows(
  windows: readonly RawEnergyConsumptionWindow[],
  canonicalDurationMs: number,
): RawEnergyConsumptionWindow[] {
  return windows.filter(
    (window) =>
      window.evidenceStatus !== "source-conflict"
      && window.evidenceStatus !== "insufficient-data"
      && window.consumedKwh > 0
      && window.readingCount > 0
      && window.calculationMethod !== "unavailable"
      && windowDurationMs(window) === canonicalDurationMs,
  );
}

function isTrendEligibleWindow(window: RawEnergyConsumptionWindow): boolean {
  if (window.evidenceStatus === "source-conflict" || window.evidenceStatus === "insufficient-data") {
    return false;
  }
  if (window.consumedKwh <= 0 || window.readingCount <= 0) {
    return false;
  }
  if (window.calculationMethod === "unavailable") {
    return false;
  }
  if (DEVELOPMENT_ASSESSMENT_SCENARIO_WINDOW_IDS.has(window.id)) {
    return false;
  }
  if (DEVELOPMENT_BASELINE_SAMPLE_WINDOW_IDS.has(window.id)) {
    return false;
  }
  return true;
}

function bucketKey(window: RawEnergyConsumptionWindow): string {
  return `${window.windowStartUtc}|${window.windowEndUtc}`;
}

function resolveTrendComparison(
  approvedConsumptionTarget: RawEnergyTarget | null,
  baseline: RawEnergyBaseline | null,
  bucketDurationMs: number,
): Readonly<{
  targetKwh: number | null;
  baselineKwh: number | null;
  targetComparisonNote: string | null;
  baselineComparisonNote: string | null;
  targetCompatible: boolean;
  baselineCompatible: boolean;
}> {
  let targetKwh: number | null = null;
  let baselineKwh: number | null = null;
  let targetComparisonNote: string | null = null;
  let baselineComparisonNote: string | null = null;
  let targetCompatible = false;
  let baselineCompatible = false;

  if (
    approvedConsumptionTarget
    && approvedConsumptionTarget.status === "approved"
    && approvedConsumptionTarget.targetMetric === "consumed-kwh"
    && approvedConsumptionTarget.targetScope === "facility"
  ) {
    targetKwh = approvedConsumptionTarget.targetValue;
    targetCompatible = true;
  } else if (approvedConsumptionTarget?.targetMetric === "consumed-kwh") {
    targetComparisonNote = "Karşılaştırılabilir hedef yok";
  } else {
    targetComparisonNote = "Karşılaştırılabilir hedef yok";
  }

  const baselineDurationMs =
    baseline?.reportingWindowDurationSeconds !== undefined
    && baseline?.reportingWindowDurationSeconds !== null
      ? baseline.reportingWindowDurationSeconds * 1000
      : null;

  if (
    baseline
    && baseline.comparisonDimension === "facility"
    && baseline.evidenceStatus !== "source-conflict"
    && baseline.evidenceStatus !== "insufficient-data"
    && (baselineDurationMs === null || baselineDurationMs === bucketDurationMs)
  ) {
    baselineKwh = baseline.medianCenter ?? baseline.baselineConsumedKwh ?? null;
    baselineCompatible = baselineKwh !== null;
  } else if (baseline) {
    baselineComparisonNote = "Karşılaştırılabilir baz yok";
  } else {
    baselineComparisonNote = "Karşılaştırılabilir baz yok";
  }

  return {
    targetKwh,
    baselineKwh,
    targetComparisonNote,
    baselineComparisonNote,
    targetCompatible,
    baselineCompatible,
  };
}

function buildFacilityAggregateTrend(
  windows: readonly RawEnergyConsumptionWindow[],
  approvedConsumptionTarget: RawEnergyTarget | null,
  baseline: RawEnergyBaseline | null,
): CanonicalTrendBundle {
  const canonicalDurationMs = resolveCanonicalWindowDurationMs(
    windows.filter((window) => isTrendEligibleWindow(window)),
  );
  if (canonicalDurationMs === null) {
    return {
      trend: [],
      insufficientData: true,
      scope: "Tesis toplamı",
      bucketDurationMs: null,
      reportingTotalKwh: 0,
      contributingWindowIds: [],
      targetComparisonNote: "Karşılaştırılabilir hedef yok",
      baselineComparisonNote: "Karşılaştırılabilir baz yok",
      targetCompatible: false,
      baselineCompatible: false,
    };
  }

  const eligibleWindows = filterCompatibleWindows(
    windows.filter((window) => isTrendEligibleWindow(window)),
    canonicalDurationMs,
  );

  const buckets = new Map<
    string,
    Readonly<{
      startUtc: string;
      endUtc: string;
      consumedKwh: number;
      averagePowerKw: number | null;
      maximumDemandKw: number | null;
      windowIds: string[];
    }>
  >();

  for (const window of eligibleWindows) {
    const key = bucketKey(window);
    const existing = buckets.get(key);
    if (existing) {
      buckets.set(key, {
        startUtc: window.windowStartUtc,
        endUtc: window.windowEndUtc,
        consumedKwh: roundEnergy(existing.consumedKwh + window.consumedKwh),
        averagePowerKw:
          existing.averagePowerKw !== null && window.averagePowerKw !== null
            ? Math.max(existing.averagePowerKw, window.averagePowerKw)
            : existing.averagePowerKw ?? window.averagePowerKw,
        maximumDemandKw:
          existing.maximumDemandKw !== null && window.maximumDemandKw !== null
            ? Math.max(existing.maximumDemandKw, window.maximumDemandKw)
            : existing.maximumDemandKw ?? window.maximumDemandKw,
        windowIds: [...existing.windowIds, window.id],
      });
    } else {
      buckets.set(key, {
        startUtc: window.windowStartUtc,
        endUtc: window.windowEndUtc,
        consumedKwh: roundEnergy(window.consumedKwh),
        averagePowerKw: window.averagePowerKw,
        maximumDemandKw: window.maximumDemandKw,
        windowIds: [window.id],
      });
    }
  }

  const sortedBuckets = [...buckets.values()].sort(
    (left, right) => new Date(left.startUtc).getTime() - new Date(right.startUtc).getTime(),
  );

  for (let index = 1; index < sortedBuckets.length; index += 1) {
    const previousEnd = new Date(sortedBuckets[index - 1].endUtc).getTime();
    const currentStart = new Date(sortedBuckets[index].startUtc).getTime();
    if (currentStart < previousEnd) {
      return {
        trend: [],
        insufficientData: true,
        scope: "Tesis toplamı",
        bucketDurationMs: canonicalDurationMs,
        reportingTotalKwh: 0,
        contributingWindowIds: [],
        targetComparisonNote: "Karşılaştırılabilir hedef yok",
        baselineComparisonNote: "Karşılaştırılabilir baz yok",
        targetCompatible: false,
        baselineCompatible: false,
      };
    }
  }

  const comparison = resolveTrendComparison(
    approvedConsumptionTarget,
    baseline,
    canonicalDurationMs,
  );
  const durationHours = Math.round(canonicalDurationMs / (60 * 60 * 1000));
  const scope = `Tesis toplamı · ${durationHours} saatlik karşılaştırılabilir tüketim pencereleri`;

  const trend = sortedBuckets.map((bucket) => ({
    label: formatEnergyTrendAxisLabel(bucket.startUtc),
    consumedKwh: bucket.consumedKwh,
    averagePowerKw: bucket.averagePowerKw,
    maximumDemandKw: bucket.maximumDemandKw,
    targetKwh: comparison.targetKwh,
    baselineKwh: comparison.baselineKwh,
    windowStartUtc: bucket.startUtc,
    windowEndUtc: bucket.endUtc,
  }));

  const reportingTotalKwh = roundEnergy(
    sumDecimal(trend.map((point) => point.consumedKwh ?? 0)),
  );
  const contributingWindowIds = sortedBuckets.flatMap((bucket) => bucket.windowIds);

  return {
    trend,
    insufficientData: trend.length < MIN_CANONICAL_TREND_BUCKETS,
    scope,
    bucketDurationMs: canonicalDurationMs,
    reportingTotalKwh,
    contributingWindowIds,
    targetComparisonNote: comparison.targetComparisonNote,
    baselineComparisonNote: comparison.baselineComparisonNote,
    targetCompatible: comparison.targetCompatible,
    baselineCompatible: comparison.baselineCompatible,
  };
}

function buildKpis(params: {
  reportingTotalKwh: number;
  reportingPeriodLabel: string;
  windows: readonly RawEnergyConsumptionWindow[];
  assessments: readonly RawEnergyAssessmentSummary[];
  targets: readonly RawEnergyTarget[];
  primaryWindow: RawEnergyConsumptionWindow | null;
}): EnergyKpiCard[] {
  const {
    reportingTotalKwh,
    reportingPeriodLabel,
    windows,
    assessments,
    targets,
    primaryWindow,
  } = params;

  const totalConsumption = reportingTotalKwh;

  const peakPower = windows.reduce<number | null>((max, window) => {
    if (window.maximumDemandKw === null) return max;
    return max === null ? window.maximumDemandKw : Math.max(max, window.maximumDemandKw);
  }, null);

  const approvedConsumptionTarget = targets.find(
    (target) => target.status === "approved" && target.targetMetric === "consumed-kwh",
  );
  const approvedDemandTarget = targets.find(
    (target) => target.status === "approved" && target.targetMetric === "maximum-demand-kw",
  );
  const approvedIntensityTarget = targets.find(
    (target) =>
      target.status === "approved"
      && (target.targetMetric === "kwh-per-1000-good-parts"
        || target.targetMetric === "kwh-per-1000-parts"),
  );
  const approvedIdleTarget = targets.find(
    (target) => target.status === "approved" && target.targetMetric === "idle-consumption-kwh",
  );

  const primaryAssessment =
    assessments.find((item) => item.varianceConsumedKwh !== null)
    ?? assessments.find((item) => item.outcome === "within-target")
    ?? assessments[0]
    ?? null;

  const intensityAssessment = assessments.find(
    (item) => item.leadingDriverCode === "energy-intensity-elevated",
  );
  const idleAssessment = assessments.find(
    (item) => item.leadingDriverCode === "idle-consumption-elevated",
  );

  const costWindows = windows.filter(
    (window) => window.costAmount !== null && window.costCurrencyCode,
  );
  const totalCost = costWindows.reduce((sum, window) => sum + (window.costAmount ?? 0), 0);
  const costCurrency = costWindows[0]?.costCurrencyCode ?? null;

  const periodLabel = reportingPeriodLabel;

  const intensityUnit = approvedIntensityTarget
    ? getEnergyTargetMetricLabel(approvedIntensityTarget.targetMetric)
    : "kWh/1.000 iyi parça";

  return [
    {
      id: "total-consumption",
      label: "Toplam tüketim",
      value: formatEnergyKwh(totalConsumption),
      unit: "kWh",
      meta: periodLabel,
      evidenceLabel: getEnergyEvidenceLabel(primaryWindow?.evidenceStatus ?? "calculated"),
      state: totalConsumption > 0 ? "available" : "insufficient-data",
      unavailableReason: totalConsumption > 0 ? undefined : "Seçili pencerede tüketim kanıtı yok",
    },
    {
      id: "peak-power",
      label: "Tepe güç",
      value: formatEnergyKw(peakPower),
      unit: "kW",
      meta: approvedDemandTarget
        ? `Hedef: ${formatEnergyKw(approvedDemandTarget.targetValue)} kW`
        : "Onaylı tepe güç hedefi yok",
      evidenceLabel: getEnergyEvidenceLabel(primaryWindow?.evidenceStatus ?? "calculated"),
      state: peakPower !== null ? "available" : "unavailable",
      unavailableReason: peakPower === null ? "Tepe güç kanıtı bulunamadı" : undefined,
    },
    {
      id: "energy-intensity",
      label: "Enerji yoğunluğu",
      value:
        intensityAssessment?.actualConsumedKwh !== null
        && intensityAssessment?.actualConsumedKwh !== undefined
        && approvedIntensityTarget
          ? formatEnergyKwh(intensityAssessment.actualConsumedKwh)
          : "—",
      unit: intensityUnit,
      meta: approvedIntensityTarget
        ? `Hedef: ${formatEnergyKwh(approvedIntensityTarget.targetValue)} ${intensityUnit}`
        : "Payda veya onaylı yoğunluk hedefi yok",
      evidenceLabel: getEnergyEvidenceLabel(intensityAssessment?.evidenceStatus ?? "insufficient-data"),
      state:
        approvedIntensityTarget && intensityAssessment?.actualConsumedKwh !== null
          ? "available"
          : "insufficient-data",
      unavailableReason: "Üretim paydası veya yoğunluk hedefi eksik",
    },
    {
      id: "target-variance",
      label: "Hedef sapması",
      value:
        primaryAssessment?.varianceConsumedKwh !== null
          ? formatEnergyVariance(
              primaryAssessment.varianceConsumedKwh,
              primaryAssessment.variancePercent,
            )
          : "—",
      unit: null,
      meta: approvedConsumptionTarget
        ? `Onaylı hedef: ${formatEnergyKwh(approvedConsumptionTarget.targetValue)} kWh`
        : "Onaylı tüketim hedefi yok",
      evidenceLabel: getEnergyEvidenceLabel(primaryAssessment?.evidenceStatus ?? "insufficient-data"),
      state:
        approvedConsumptionTarget && primaryAssessment?.varianceConsumedKwh !== null
          ? "available"
          : "unavailable",
      unavailableReason: "Onaylı hedef olmadan sapma gösterilmez",
    },
    {
      id: "idle-consumption",
      label: "Boşta tüketim",
      value:
        idleAssessment?.actualConsumedKwh !== null
        && idleAssessment?.actualConsumedKwh !== undefined
          ? formatEnergyKwh(idleAssessment.actualConsumedKwh)
          : "—",
      unit: "kWh",
      meta: approvedIdleTarget
        ? `Hedef: ${formatEnergyKwh(approvedIdleTarget.targetValue)} kWh`
        : "Üretim durumu veya boşta hedef kanıtı yok",
      evidenceLabel: getEnergyEvidenceLabel(idleAssessment?.evidenceStatus ?? "insufficient-data"),
      state:
        idleAssessment?.actualConsumedKwh !== null
          ? "available"
          : "insufficient-data",
      unavailableReason: "Üretim durumu kanıtı olmadan boşta tüketim hesaplanmaz",
    },
    {
      id: "estimated-cost",
      label: "Tahmini enerji maliyeti",
      value:
        costWindows.length > 0
          ? formatEnergyCurrency(totalCost, costCurrency)
          : "Tarife tanımlı değil",
      unit: null,
      meta:
        costWindows.length > 0
          ? "Geliştirme tarifesi — gerçek fatura değildir"
          : "Tarife kanıtı bulunamadı",
      evidenceLabel:
        costWindows.length > 0
          ? getEnergyEvidenceLabel("simulated")
          : getEnergyEvidenceLabel("insufficient-data"),
      state: costWindows.length > 0 ? "available" : "unavailable",
      unavailableReason: costWindows.length === 0 ? "Tarife tanımlı değil" : undefined,
    },
  ];
}

function buildIntelligenceRows(
  assessments: readonly EnergyAssessmentSummary[],
): EnergyIntelligenceRow[] {
  return assessments.slice(0, 6).map((assessment) => ({
    id: assessment.id,
    assessmentReference: assessment.assessmentReference,
    outcome: assessment.outcome,
    outcomeLabel: assessment.outcomeLabel,
    scopeLabel: assessment.comparisonDimensionLabel,
    evaluationPeriodLabel: assessment.evaluationPeriodLabel,
    leadingDriverLabel: assessment.leadingDriverLabel,
    comparisonLabel:
      assessment.actualConsumedKwh !== null
        ? `${formatEnergyKwh(assessment.actualConsumedKwh)} kWh`
        : assessment.actualMaximumDemandKw !== null
          ? `${formatEnergyKw(assessment.actualMaximumDemandKw)} kW`
          : null,
    freshnessLabel: getEnergyFreshnessLabel("fresh"),
    evaluatedAtUtc: assessment.evaluatedAtUtc,
  }));
}

function buildOpportunity(
  assessments: readonly RawEnergyAssessmentSummary[],
  targets: readonly RawEnergyTarget[],
): EnergyOpportunitySummary | null {
  const approvedTarget = targets.find(
    (target) => target.status === "approved" && target.targetMetric === "consumed-kwh",
  );
  if (!approvedTarget) {
    return null;
  }

  const candidate = assessments.find(
    (item) =>
      item.potentialReducibleKwh !== null
      && item.potentialReducibleKwh > 0
      && item.varianceConsumedKwh !== null
      && item.varianceConsumedKwh > 0,
  );

  if (!candidate) {
    return {
      reducibleKwhLabel: null,
      costImpactLabel: null,
      hasApprovedTarget: true,
      disclaimer: ENERGY_OPPORTUNITY_DISCLAIMER,
    };
  }

  return {
    reducibleKwhLabel: `${formatEnergyKwh(candidate.potentialReducibleKwh)} kWh`,
    costImpactLabel:
      candidate.estimatedCostImpact !== null
        ? formatEnergyCurrency(candidate.estimatedCostImpact, candidate.costCurrencyCode)
        : null,
    hasApprovedTarget: true,
    disclaimer: ENERGY_OPPORTUNITY_DISCLAIMER,
  };
}

export async function readEnergyWorkspace(): Promise<EnergyWorkspaceResult> {
  const resolved = resolveEnergyRequestContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const context = resolved.context;
  const partialErrors: string[] = [];

  const [
    workspaceResult,
    intelligenceResult,
    pointsResult,
    windowsResult,
    targetsResult,
    baselinesResult,
    assessmentsResult,
  ] = await Promise.all([
    getEnergyWorkspace(context),
    getEnergyIntelligenceWorkspace(context),
    listEnergyMeasurementPoints(context),
    listEnergyConsumptionWindows(context),
    listEnergyTargets(context),
    listEnergyBaselines(context),
    listEnergyAssessments(context),
  ]);

  if (workspaceResult.status === "failure") {
    return {
      status: "failure",
      message: workspaceResult.message,
      code: workspaceResult.code,
    };
  }

  if (intelligenceResult.status === "failure") {
    partialErrors.push(intelligenceResult.message);
  }

  const windows =
    windowsResult.status === "success" ? windowsResult.value : [];
  if (windowsResult.status === "failure") {
    partialErrors.push(windowsResult.message);
  }

  const points =
    pointsResult.status === "success" ? pointsResult.value : [];
  if (pointsResult.status === "failure") {
    partialErrors.push(pointsResult.message);
  }

  const targets =
    targetsResult.status === "success" ? targetsResult.value : [];
  if (targetsResult.status === "failure") {
    partialErrors.push(targetsResult.message);
  }

  const baselines =
    baselinesResult.status === "success" ? baselinesResult.value : [];
  if (baselinesResult.status === "failure") {
    partialErrors.push(baselinesResult.message);
  }

  const rawAssessments =
    assessmentsResult.status === "success" ? assessmentsResult.value : [];
  if (assessmentsResult.status === "failure") {
    partialErrors.push(assessmentsResult.message);
  }

  const reportingPeriod = deriveReportingPeriod(windows);
  const reportingPeriodLabel =
    reportingPeriod.startUtc && reportingPeriod.endUtc
      ? formatEnergyCompactPeriod(reportingPeriod.startUtc, reportingPeriod.endUtc)
      : "Seçili raporlama penceresi yok";

  const approvedConsumptionTarget =
    targets.find(
      (target) => target.status === "approved" && target.targetMetric === "consumed-kwh",
    ) ?? null;
  const primaryBaseline =
    baselines.find((baseline) => baseline.comparisonDimension === "machine") ?? baselines[0] ?? null;

  const trendBundle = buildFacilityAggregateTrend(
    windows,
    approvedConsumptionTarget,
    primaryBaseline,
  );
  const reportingTotalKwh = trendBundle.reportingTotalKwh;
  const primaryWindow = pickPrimaryWindow(windows);

  const reportableWindows = windows.filter(
    (window) => window.evidenceStatus !== "source-conflict",
  );
  const trendWindowIdSet = new Set(trendBundle.contributingWindowIds);
  const trendAllocationsSource = trendWindowIdSet.size > 0
    ? reportableWindows.filter((window) => trendWindowIdSet.has(window.id))
    : reportableWindows;
  const allocationBatches = await Promise.all(
    trendAllocationsSource.map((window) =>
      listEnergyAllocations(context, { consumptionWindowId: window.id }),
    ),
  );
  const allocations = allocationBatches.flatMap((result) =>
    result.status === "success" ? result.value : [],
  );
  for (const result of allocationBatches) {
    if (result.status === "failure") {
      partialErrors.push(result.message);
    }
  }

  const distribution = buildDistributionResult(
    allocations,
    points,
    windows,
    reportingTotalKwh,
  );

  const windowByPoint = new Map<string, RawEnergyConsumptionWindow>();
  for (const window of windows) {
    const existing = windowByPoint.get(window.measurementPointId);
    if (!existing || new Date(window.windowEndUtc) > new Date(existing.windowEndUtc)) {
      windowByPoint.set(window.measurementPointId, window);
    }
  }

  const mappedPoints = points.map((point) =>
    mapMeasurementPoint(point, windowByPoint.get(point.id)),
  );
  const mappedBaselines = baselines.map(mapBaseline);
  const mappedTargets = targets.map(mapTarget);
  const mappedAssessments = rawAssessments.map(mapAssessment);

  const effectiveAccess = await fetchCurrentEffectiveAccess();
  const canManage = hasGrantedPermission(effectiveAccess, "energy.manage");

  const latestFreshnessUtc =
    primaryWindow?.calculatedAtUtc
    ?? mappedAssessments[0]?.evaluatedAtUtc
    ?? null;

  const hasPilot =
    workspaceResult.value.hasDevelopmentPilotData
    || (intelligenceResult.status === "success"
      ? intelligenceResult.value.hasDevelopmentPilotData
      : false);

  const workspace: EnergyWorkspacePayload = {
    meta: {
      activeMeasurementPointCount: workspaceResult.value.activeMeasurementPointCount,
      recentWindowCount: workspaceResult.value.recentWindowCount,
      baselineCount:
        intelligenceResult.status === "success" ? intelligenceResult.value.baselineCount : 0,
      activeTargetCount:
        intelligenceResult.status === "success" ? intelligenceResult.value.activeTargetCount : 0,
      recentAssessmentCount:
        intelligenceResult.status === "success"
          ? intelligenceResult.value.recentAssessmentCount
          : 0,
      ruleSetVersion: workspaceResult.value.ruleSetVersion,
      intelligenceRuleSetVersion:
        intelligenceResult.status === "success"
          ? intelligenceResult.value.ruleSetVersion
          : "",
      provenanceDisclaimer: workspaceResult.value.provenanceDisclaimer,
      intelligenceProvenanceDisclaimer:
        intelligenceResult.status === "success"
          ? intelligenceResult.value.provenanceDisclaimer
          : "",
      hasDevelopmentPilotData: hasPilot,
      capabilities: {
        canRead: true,
        canManage,
      },
      facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
      reportingPeriodStartUtc: reportingPeriod.startUtc,
      reportingPeriodEndUtc: reportingPeriod.endUtc,
      latestFreshnessUtc,
      evidenceLabel: getEnergyEvidenceLabel(primaryWindow?.evidenceStatus ?? "calculated"),
    },
    kpis: buildKpis({
      reportingTotalKwh,
      reportingPeriodLabel,
      windows,
      assessments: rawAssessments,
      targets,
      primaryWindow,
    }),
    trend: trendBundle.trend,
    trendScope: trendBundle.scope,
    trendInsufficientData: trendBundle.insufficientData,
    trendBucketDurationMs: trendBundle.bucketDurationMs,
    trendReportingTotalKwh: trendBundle.reportingTotalKwh,
    trendTargetComparisonNote: trendBundle.targetComparisonNote,
    trendBaselineComparisonNote: trendBundle.baselineComparisonNote,
    trendTargetCompatible: trendBundle.targetCompatible,
    trendBaselineCompatible: trendBundle.baselineCompatible,
    allocations: distribution.slices,
    allocationTotalKwh: distribution.totalKwh,
    windowConsumptionKwh: reportingTotalKwh,
    reportingTotalKwh,
    allocatedConsumptionKwh: distribution.distributedKwh,
    residualConsumptionKwh: distribution.residualKwh,
    allocationCoveragePercent: distribution.coveragePercent,
    intelligenceRows: buildIntelligenceRows(mappedAssessments),
    opportunity: buildOpportunity(rawAssessments, targets),
    measurementPoints: mappedPoints,
    baselines: mappedBaselines,
    targets: mappedTargets,
    assessments: mappedAssessments,
    partialErrors,
  };

  return {
    status: "success",
    workspace,
    canManage,
  };
}

export async function readEnergyMeasurementPointDetail(
  reference: string,
): Promise<EnergyMeasurementPointDetailResult> {
  const resolved = resolveEnergyRequestContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [pointResult, windowsResult] = await Promise.all([
    getEnergyMeasurementPoint(resolved.context, reference),
    listEnergyConsumptionWindows(resolved.context, {
      measurementPointReference: reference,
      pageSize: "5",
    }),
  ]);

  if (pointResult.status === "failure") {
    return {
      status: "failure",
      message: pointResult.message,
      code: pointResult.code,
    };
  }

  const latestWindow =
    windowsResult.status === "success"
      ? sortWindows(windowsResult.value).at(-1) ?? null
      : null;

  const allocationResult = latestWindow
    ? await listEnergyAllocations(resolved.context, {
        consumptionWindowId: latestWindow.id,
      })
    : { status: "success" as const, value: [] as RawEnergyAllocation[] };

  const allocations =
    allocationResult.status === "success" ? allocationResult.value : [];

  const point = mapMeasurementPoint(pointResult.value, latestWindow);

  return {
    status: "success",
    detail: {
      point,
      sourceProvenance: pointResult.value.sourceProvenance,
      measurementUnit: pointResult.value.measurementUnit,
      ruleSetVersion: latestWindow?.ruleSetVersion ?? "ENERGY-1.0",
      currentWindowLabel: latestWindow
        ? formatEnergyCompactPeriod(latestWindow.windowStartUtc, latestWindow.windowEndUtc)
        : null,
      powerLabel:
        latestWindow !== null && latestWindow.averagePowerKw !== null
          ? `${formatEnergyKw(latestWindow.averagePowerKw)} kW ortalama`
          : null,
      demandLabel:
        latestWindow !== null && latestWindow.maximumDemandKw !== null
          ? `${formatEnergyKw(latestWindow.maximumDemandKw)} kW tepe`
          : null,
      allocationLabels: allocations.map(
        (allocation) =>
          `${allocation.targetType === "unallocated" ? "Dağıtılmamış" : allocation.targetType}: ${formatEnergyKwh(allocation.allocatedKwh)} kWh`,
      ),
      dataQualityLabel: getEnergyEvidenceLabel(latestWindow?.evidenceStatus ?? "insufficient-data"),
      calculationMethodLabel: getEnergyCalculationMethodLabel(latestWindow?.calculationMethod),
    },
  };
}

export async function readEnergyAssessmentDetail(
  reference: string,
): Promise<EnergyAssessmentDetailResult> {
  const resolved = resolveEnergyRequestContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const assessmentResult = await getEnergyAssessment(resolved.context, reference);
  if (assessmentResult.status === "failure") {
    return {
      status: "failure",
      message: assessmentResult.message,
      code: assessmentResult.code,
    };
  }

  const summary = mapAssessment(assessmentResult.value.summary);
  const drivers = assessmentResult.value.drivers.map((driver) => ({
    id: driver.id,
    reasonCode: driver.reasonCode,
    labelTr: driver.labelTr,
    severity: driver.severity,
    severityLabel: getEnergySeverityLabel(driver.severity),
    category: driver.category,
    categoryLabel: getEnergyBaselineDimensionLabel(driver.category),
    actualValue: driver.actualValue,
    actualUnit: driver.actualUnit ? localizeEnergyUnit(driver.actualUnit) : null,
    comparisonValue: driver.comparisonValue,
    comparisonUnit: driver.comparisonUnit ? localizeEnergyUnit(driver.comparisonUnit) : null,
    normalizedVariance: driver.normalizedVariance,
    explanation: driver.explanation,
    evidenceSource: driver.evidenceSource,
    freshnessState: driver.freshnessState,
    freshnessLabel: getEnergyFreshnessLabel(driver.freshnessState),
    isLeadingDriver: driver.isLeadingDriver,
  }));

  return {
    status: "success",
    detail: {
      summary,
      baselineReference: assessmentResult.value.baselineReference,
      targetReference: assessmentResult.value.targetReference,
      baselineLabel: assessmentResult.value.baselineReference
        ? `Hesaplanan baz: ${assessmentResult.value.baselineReference}`
        : null,
      targetLabel: assessmentResult.value.targetReference
        ? `Onaylı hedef: ${assessmentResult.value.targetReference}`
        : null,
      varianceLabel: formatEnergyVariance(
        summary.varianceConsumedKwh,
        summary.variancePercent,
      ),
      drivers,
      ruleSetVersion: "ENERGY-2.0",
      provenanceLabel: summary.isPilotData
        ? "Yerel geliştirme / simülasyon kanıtı"
        : null,
    },
  };
}

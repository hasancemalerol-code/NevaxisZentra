import type { IotPlcHealthSegment, IotPlcKpiCard, IotPlcQualityDistributionSlice } from "../domain/iot-plc-types";

const DONUT_OUTER_RADIUS = 52;
const DONUT_INNER_RADIUS = 32;
const DONUT_CENTER = { x: 60, y: 60 } as const;

/** API `*Ratio` fields must be 0–1 fractions. Values outside this range violate the IOT-PLC-2 contract. */
export function isIotPlcRatioFraction(value: number | null | undefined): boolean {
  if (value === null || value === undefined || Number.isNaN(value)) return true;
  return value >= 0 && value <= 1;
}

export function validateIotPlcSignalAssessmentRatioContract(assessment: Readonly<{
  actualObservationCount: number;
  goodRatio: number | null;
  uncertainRatio: number | null;
  badRatio: number | null;
  staleRatio: number | null;
  sourceConflictRatio: number | null;
}>): Readonly<{ compliant: boolean; reason: string | null }> {
  if (assessment.actualObservationCount <= 0) {
    return { compliant: false, reason: "no-observations" };
  }

  const ratios = [
    assessment.goodRatio,
    assessment.uncertainRatio,
    assessment.badRatio,
    assessment.staleRatio,
    assessment.sourceConflictRatio,
  ];

  if (!ratios.every(isIotPlcRatioFraction)) {
    return { compliant: false, reason: "ratio-out-of-range" };
  }

  let sum = 0;
  for (const ratio of ratios) {
    sum += ratio ?? 0;
  }
  if (Math.abs(sum - 1) > 0.0001) {
    return { compliant: false, reason: "ratio-sum-invalid" };
  }

  return { compliant: true, reason: null };
}

function toStrictRatioFraction(value: number | null | undefined): number | null {
  if (!isIotPlcRatioFraction(value) || value === null || value === undefined) return null;
  return value;
}

export type IotPlcReconciledQualitySlice = Readonly<{
  key: string;
  label: string;
  count: number;
  ratio: number | null;
  color: string;
}>;

/**
 * Reconcile integer observation counts and display ratios from API fractions.
 * Count sum equals denominator; each ratio is count/denominator (0–1).
 */
export function reconcileIotPlcQualityDistribution(
  denominator: number,
  entries: ReadonlyArray<Readonly<{ key: string; label: string; rawRatio: number | null; color: string }>>,
): readonly IotPlcReconciledQualitySlice[] {
  if (denominator <= 0) return [];

  const prepared = entries.map((entry) => {
    const fraction = toStrictRatioFraction(entry.rawRatio);
    return {
      ...entry,
      fraction,
      exactCount: fraction === null ? 0 : fraction * denominator,
    };
  });

  const floorCounts = prepared.map((entry) => ({
    ...entry,
    count: entry.fraction === null ? 0 : Math.floor(entry.exactCount),
  }));

  let remainder = denominator - floorCounts.reduce((sum, entry) => sum + entry.count, 0);
  const ranked = floorCounts
    .map((entry, index) => ({
      index,
      fraction: entry.fraction ?? 0,
      remainder: entry.exactCount - entry.count,
    }))
    .filter((entry) => entry.fraction > 0)
    .sort((left, right) => right.remainder - left.remainder);

  const adjustedCounts = floorCounts.map((entry) => entry.count);
  for (let index = 0; index < ranked.length && remainder > 0; index += 1) {
    adjustedCounts[ranked[index].index] += 1;
    remainder -= 1;
  }

  return prepared.map((entry, index) => {
    const count = adjustedCounts[index];
    return {
      key: entry.key,
      label: entry.label,
      count,
      ratio: count > 0 ? count / denominator : null,
      color: entry.color,
    };
  });
}

export function formatIotPlcQualitySliceLine(
  count: number,
  ratio: number | null,
  formatCount: (value: number) => string,
  formatRatioPercent: (value: number | null | undefined) => string,
): string {
  if (ratio === null || count <= 0) return `${formatCount(count)} · —`;
  return `${formatCount(count)} · ${formatRatioPercent(ratio)}`;
}

export function isIotPlcHealthCompositionAllUnknown(segments: readonly IotPlcHealthSegment[]): boolean {
  const unknown = segments.find((segment) => segment.bucket === "unknown");
  const total = segments.reduce((sum, segment) => sum + segment.count, 0);
  return total > 0 && (unknown?.count ?? 0) === total;
}

export function describeDonutArc(
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

export function buildHealthDonutArcs(
  segments: readonly IotPlcHealthSegment[],
  total: number,
): ReadonlyArray<Readonly<{ id: string; path: string; color: string }>> {
  const { x: cx, y: cy } = DONUT_CENTER;
  const active = segments.filter((segment) => segment.count > 0);
  if (total <= 0 || active.length === 0) return [];

  if (active.length === 1 && active[0].count === total) {
    const segment = active[0];
    const start = -Math.PI / 2;
    const mid = start + Math.PI;
    const end = start + Math.PI * 2 - 0.0001;
    return [{
      id: segment.bucket,
      path: [
        describeDonutArc(cx, cy, DONUT_OUTER_RADIUS, DONUT_INNER_RADIUS, start, mid),
        describeDonutArc(cx, cy, DONUT_OUTER_RADIUS, DONUT_INNER_RADIUS, mid, end),
      ].join(" "),
      color: segment.color,
    }];
  }

  let cursor = -Math.PI / 2;
  return active.map((segment) => {
    const sweep = (segment.count / total) * Math.PI * 2;
    const start = cursor;
    const end = cursor + sweep;
    cursor = end;
    return {
      id: segment.bucket,
      path: describeDonutArc(cx, cy, DONUT_OUTER_RADIUS, DONUT_INNER_RADIUS, start, end),
      color: segment.color,
    };
  });
}

/** A zero or empty value carries no status: it is neutral, never a warning colour. */
export function isIotPlcKpiValueEmpty(value: string): boolean {
  const text = value.trim();
  return text === "" || text === "—" || /^%?0([.,]0+)?$/.test(text);
}

export function resolveKpiToneClass(tone: IotPlcKpiCard["tone"], value?: string): string {
  if (value !== undefined && tone !== "danger" && tone !== "unavailable" && isIotPlcKpiValueEmpty(value)) {
    return "iot-plc-kpi-card--neutral";
  }
  switch (tone) {
    case "success":
      return "iot-plc-kpi-card--success";
    case "warning":
      return "iot-plc-kpi-card--warning";
    case "danger":
      return "iot-plc-kpi-card--danger";
    case "unavailable":
      return "iot-plc-kpi-card--unavailable";
    default:
      return "iot-plc-kpi-card--info";
  }
}

export function buildQualityBarSegments(
  slices: readonly IotPlcQualityDistributionSlice[],
  denominator: number | null,
): ReadonlyArray<Readonly<{ id: string; widthPercent: number; color: string; label: string }>> {
  if (!denominator || denominator <= 0) return [];
  return slices
    .filter((slice) => slice.count > 0)
    .map((slice) => ({
      id: slice.key,
      widthPercent: (slice.count / denominator) * 100,
      color: slice.color,
      label: slice.label,
    }));
}

export function healthTimelineTone(healthState: string): string {
  switch (healthState) {
    case "online":
      return "iot-plc-timeline__point--online";
    case "degraded":
      return "iot-plc-timeline__point--degraded";
    case "offline":
      return "iot-plc-timeline__point--offline";
    default:
      return "iot-plc-timeline__point--unknown";
  }
}

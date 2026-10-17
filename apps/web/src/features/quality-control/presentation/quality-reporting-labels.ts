import { formatDate } from "@/lib/presentation/format";

export type QualityTrendChartPoint = Readonly<{
  passed: number;
  failed: number;
  marker?: number;
}>;

/** Compact x-axis label: week keys become "H42", day and month periods become a short date without the year. */
export function formatQualityTrendLabel(
  period: "day" | "week" | "month",
  periodKey: string,
  periodStartUtc: string,
): string {
  if (period === "week") {
    const match = /^(\d{4})-W(\d{1,2})$/.exec(periodKey.trim());
    if (match) {
      return `H${Number(match[2])}`;
    }
  }

  return formatDate(periodStartUtc).replace(/ \d{4}$/, "");
}

/**
 * A trend chart needs at least two periods and a peak of two so the integer value axis has distinct ticks; below that the
 * screen states that there is not enough data and the table stays the source of truth.
 */
export function hasEnoughQualityTrendData(points: readonly QualityTrendChartPoint[]): boolean {
  if (points.length < 2) {
    return false;
  }

  const peak = Math.max(...points.map((point) => Math.max(point.passed + point.failed, point.marker ?? 0)));
  return peak >= 2;
}

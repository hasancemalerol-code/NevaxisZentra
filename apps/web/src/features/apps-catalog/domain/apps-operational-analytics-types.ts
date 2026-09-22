export type AppsOperationalKpi = Readonly<{
  label: string;
  value: string;
}>;

export type AppsOperationalTrendPoint = Readonly<{
  label: string;
  goodQuantity: number;
  targetQuantity: number | null;
}>;

export type AppsOperationalQuantityBar = Readonly<{
  label: string;
  count: number;
}>;

export type AppsOperationalAnalyticsModel = Readonly<{
  state: "empty" | "ready";
  kpis: readonly AppsOperationalKpi[];
  outputTrend: readonly AppsOperationalTrendPoint[];
  resourceTypeBars: readonly AppsOperationalQuantityBar[];
  resourceStateBars: readonly AppsOperationalQuantityBar[];
  scrapSummary: Readonly<{
    goodQuantity: number;
    scrapQuantity: number;
    scrapRatePercent: number | null;
  }> | null;
  downtimeSummary: Readonly<{
    activeCount: number;
    totalMinutes: number;
  }> | null;
  riskHighlights: readonly string[];
}>;

export const EMPTY_APPS_OPERATIONAL_ANALYTICS: AppsOperationalAnalyticsModel = {
  state: "empty",
  kpis: [],
  outputTrend: [],
  resourceTypeBars: [],
  resourceStateBars: [],
  scrapSummary: null,
  downtimeSummary: null,
  riskHighlights: [],
};

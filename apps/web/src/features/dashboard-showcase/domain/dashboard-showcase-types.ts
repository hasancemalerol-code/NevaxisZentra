export type DashboardShowcasePresentationMode = "live" | "development-showcase";

export type DashboardShowcaseTone = "info" | "success" | "warning" | "quality";

export type DashboardShowcaseSparklineSeries = Readonly<{
  values: readonly number[];
  variant: "solid" | "dashed";
}>;

export type DashboardShowcaseKpiCard = Readonly<{
  id: string;
  label: string;
  value: string;
  unit?: string;
  comparisonLabel?: string;
  supportingText?: string;
  tone: DashboardShowcaseTone;
  sparklineTone?: DashboardShowcaseTone;
  iconKey: "production" | "oee" | "scrap" | "cycle" | "quality" | "target";
  sparklines: readonly DashboardShowcaseSparklineSeries[];
  showSparklineLegend?: boolean;
  pending: boolean;
}>;

export type DashboardShowcaseHourlyPoint = Readonly<{
  hourLabel: string;
  actual: number | null;
  target: number | null;
}>;

export type DashboardShowcaseOeeComponent = Readonly<{
  label: string;
  percent: number | null;
}>;

export type DashboardShowcaseDistributionRow = Readonly<{
  label: string;
  quantity: number;
  percent: number;
  tone: DashboardShowcaseTone;
}>;

export type DashboardShowcaseViewModel = Readonly<{
  facilityDisplayName: string;
  developmentPilotBanner: boolean;
  presentationMode: DashboardShowcasePresentationMode;
  lastRefreshedAtUtc: string;
  kpiCards: readonly DashboardShowcaseKpiCard[];
  hourlyProduction: readonly DashboardShowcaseHourlyPoint[];
  hourlyPending: boolean;
  oeeComponents: readonly DashboardShowcaseOeeComponent[];
  oeeComponentsPending: boolean;
  distributionRows: readonly DashboardShowcaseDistributionRow[];
  distributionTotal: number | null;
  distributionPending: boolean;
}>;

import { DISTRIBUTION_GROUP_LABELS, formatShowcaseNumber, formatShowcasePercent } from "./dashboard-showcase-labels";
import type {
  DashboardShowcaseDistributionRow,
  DashboardShowcaseHourlyPoint,
  DashboardShowcaseKpiCard,
  DashboardShowcaseOeeComponent,
  DashboardShowcaseViewModel,
} from "./dashboard-showcase-types";

const SPARKLINE_POINTS = 12;
const HOURLY_POINTS = 24;

const DEV_GOOD_QUANTITY = 38_420;
const DEV_PLANNED_QUANTITY = 45_600;
const DEV_COMPLETION_PERCENT = (DEV_GOOD_QUANTITY / DEV_PLANNED_QUANTITY) * 100;
const DEV_SCRAP_RATE = 2.8;
const DEV_CYCLE_MS = 24_600;
const DEV_AVAILABILITY = 84.2;
const DEV_PERFORMANCE = 88.5;
const DEV_QUALITY = 92.8;
const DEV_OEE = (DEV_AVAILABILITY * DEV_PERFORMANCE * DEV_QUALITY) / 10_000;

const DEV_DISTRIBUTION_QUANTITIES = [18_240, 11_520, 5_760, 2_900] as const;
const DEV_DISTRIBUTION_TONES = ["info", "success", "warning", "quality"] as const;

function buildDeterministicSeries(
  seed: number,
  base: number,
  amplitude: number,
  minimum: number,
): readonly number[] {
  return Array.from({ length: SPARKLINE_POINTS }, (_, index) => {
    const wave = Math.sin((index + seed) * 0.72) * amplitude;
    const trend = (index - SPARKLINE_POINTS / 2) * (amplitude * 0.08);
    return Math.max(minimum, base + wave + trend);
  });
}

function buildHourlySeries(): readonly DashboardShowcaseHourlyPoint[] {
  const hourlyActualPattern = [
    420, 680, 1240, 2180, 3120, 4680, 5520, 4980, 4360, 3890, 3520, 3180,
    2940, 3360, 4120, 5280, 5740, 5120, 4460, 3980, 3340, 2460, 1540, 860,
  ];
  const hourlyTargetPattern = hourlyActualPattern.map((value) => Math.round(value * 1.08));

  return Array.from({ length: HOURLY_POINTS }, (_, hour) => ({
    hourLabel: `${String(hour).padStart(2, "0")}:00`,
    actual: hourlyActualPattern[hour] ?? 0,
    target: hourlyTargetPattern[hour] ?? 0,
  }));
}

function buildDistributionRows(): readonly DashboardShowcaseDistributionRow[] {
  const total = DEV_DISTRIBUTION_QUANTITIES.reduce((sum, quantity) => sum + quantity, 0);

  return DEV_DISTRIBUTION_QUANTITIES.map((quantity, index) => ({
    label: DISTRIBUTION_GROUP_LABELS[index] ?? "Diğer",
    quantity,
    percent: total > 0 ? (quantity / total) * 100 : 0,
    tone: DEV_DISTRIBUTION_TONES[index] ?? "info",
  }));
}

function buildKpiCards(): readonly DashboardShowcaseKpiCard[] {
  const productionSeries = buildDeterministicSeries(1, DEV_GOOD_QUANTITY / SPARKLINE_POINTS, 180, 120);
  const oeeSeries = buildDeterministicSeries(2, DEV_OEE, 2.4, 55);
  const scrapSeries = buildDeterministicSeries(3, DEV_SCRAP_RATE, 0.35, 0.8);
  const cycleSeries = buildDeterministicSeries(4, DEV_CYCLE_MS / 1000, 1.4, 18);
  const qualitySeries = buildDeterministicSeries(5, DEV_QUALITY, 1.8, 80);
  const actualAchievementSeries = buildDeterministicSeries(6, DEV_COMPLETION_PERCENT, 3.2, 55);
  const targetAchievementSeries = buildDeterministicSeries(7, DEV_COMPLETION_PERCENT + 4.5, 2.1, 60);

  return [
    {
      id: "actual-production",
      label: "Gerçekleşen Üretim",
      value: formatShowcaseNumber(DEV_GOOD_QUANTITY),
      unit: "adet",
      supportingText: `Hedef ${formatShowcaseNumber(DEV_PLANNED_QUANTITY)}`,
      tone: "info",
      iconKey: "production",
      sparklines: [{ values: productionSeries, variant: "solid" }],
      pending: false,
    },
    {
      id: "oee",
      label: "OEE",
      value: formatShowcasePercent(DEV_OEE),
      comparisonLabel: "↑ %2,1",
      tone: "success",
      iconKey: "oee",
      sparklines: [{ values: oeeSeries, variant: "solid" }],
      pending: false,
    },
    {
      id: "scrap-rate",
      label: "Fire Oranı",
      value: formatShowcasePercent(DEV_SCRAP_RATE),
      comparisonLabel: "↓ %0,3",
      tone: "warning",
      iconKey: "scrap",
      sparklines: [{ values: scrapSeries, variant: "solid" }],
      pending: false,
    },
    {
      id: "cycle-time",
      label: "Çevrim Süresi",
      value: `${(DEV_CYCLE_MS / 1000).toLocaleString("tr-TR", { minimumFractionDigits: 1, maximumFractionDigits: 1 })} sn`,
      comparisonLabel: "↓ %1,1",
      tone: "info",
      iconKey: "cycle",
      sparklines: [{ values: cycleSeries, variant: "solid" }],
      pending: false,
    },
    {
      id: "quality-compliance",
      label: "Kalite Uygunluğu",
      value: formatShowcasePercent(DEV_QUALITY),
      tone: "success",
      iconKey: "quality",
      sparklineTone: "quality",
      sparklines: [{ values: qualitySeries, variant: "solid" }],
      pending: false,
    },
    {
      id: "target-achievement",
      label: "Hedef Gerçekleşmesi",
      value: formatShowcasePercent(DEV_COMPLETION_PERCENT),
      tone: "info",
      iconKey: "target",
      sparklines: [
        { values: actualAchievementSeries, variant: "solid" },
        { values: targetAchievementSeries, variant: "dashed" },
      ],
      showSparklineLegend: true,
      pending: false,
    },
  ];
}

function buildOeeComponents(): readonly DashboardShowcaseOeeComponent[] {
  return [
    { label: "Kullanılabilirlik", percent: DEV_AVAILABILITY },
    { label: "Performans", percent: DEV_PERFORMANCE },
    { label: "Kalite", percent: DEV_QUALITY },
  ];
}

export function buildDevelopmentShowcaseViewModel(
  facilityDisplayName: string,
  lastRefreshedAtUtc: string,
): DashboardShowcaseViewModel {
  const distributionRows = buildDistributionRows();
  const distributionTotal = distributionRows.reduce((sum, row) => sum + row.quantity, 0);

  return {
    facilityDisplayName,
    developmentPilotBanner: true,
    presentationMode: "development-showcase",
    lastRefreshedAtUtc,
    kpiCards: buildKpiCards(),
    hourlyProduction: buildHourlySeries(),
    hourlyPending: false,
    oeeComponents: buildOeeComponents(),
    oeeComponentsPending: false,
    distributionRows,
    distributionTotal,
    distributionPending: false,
  };
}

export const DEVELOPMENT_SHOWCASE_RECONCILIATION = {
  goodQuantity: DEV_GOOD_QUANTITY,
  plannedQuantity: DEV_PLANNED_QUANTITY,
  distributionTotal: DEV_DISTRIBUTION_QUANTITIES.reduce((sum, quantity) => sum + quantity, 0),
  oeePercent: DEV_OEE,
  oeeComponents: {
    availability: DEV_AVAILABILITY,
    performance: DEV_PERFORMANCE,
    quality: DEV_QUALITY,
  },
} as const;

"use client";

import {
  Activity,
  Clock3,
  Gauge,
  Percent,
  ShieldCheck,
  Target,
} from "lucide-react";

import type {
  DashboardShowcaseDistributionRow,
  DashboardShowcaseHourlyPoint,
  DashboardShowcaseKpiCard,
  DashboardShowcaseOeeComponent,
  DashboardShowcaseSparklineSeries,
  DashboardShowcaseTone,
  DashboardShowcaseViewModel,
} from "@/features/dashboard-showcase/domain/dashboard-showcase-types";
import {
  formatShowcaseNumber,
  formatShowcasePercent,
} from "@/features/dashboard-showcase/domain/dashboard-showcase-labels";

import "@/features/dashboard-showcase/components/factory-showcase.css";

const KPI_CARD_ORDER = [
  "actual-production",
  "oee",
  "scrap-rate",
  "cycle-time",
  "quality-compliance",
  "target-achievement",
] as const;

const SPARKLINE_WIDTH = 120;
const SPARKLINE_HEIGHT = 36;

type FactoryOperationsAnalyticsGridProps = Readonly<{
  viewModel: DashboardShowcaseViewModel;
  pendingLabel: string;
  rootClassName?: string;
  testId?: string;
}>;

function createSparklinePath(values: readonly number[]): string {
  if (values.length < 2) {
    return "";
  }

  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = Math.max(max - min, 1);

  return values
    .map((value, index) => {
      const x = (index / (values.length - 1)) * SPARKLINE_WIDTH;
      const y = SPARKLINE_HEIGHT - ((value - min) / range) * SPARKLINE_HEIGHT;
      return `${index === 0 ? "M" : "L"} ${x.toFixed(2)} ${y.toFixed(2)}`;
    })
    .join(" ");
}

function createAreaPath(values: readonly number[]): string {
  const line = createSparklinePath(values);
  if (!line) {
    return "";
  }

  return `${line} L ${SPARKLINE_WIDTH} ${SPARKLINE_HEIGHT} L 0 ${SPARKLINE_HEIGHT} Z`;
}

function hasNumeric(value: number | null | undefined): value is number {
  return value !== null && value !== undefined && !Number.isNaN(value);
}

function ShowcaseSparkline({
  series,
  tone,
  sparklineTone,
}: Readonly<{
  series: readonly DashboardShowcaseSparklineSeries[];
  tone: DashboardShowcaseKpiCard["tone"];
  sparklineTone?: DashboardShowcaseTone;
}>) {
  if (series.length === 0) {
    return <div className="factory-showcase-kpi__sparkline-empty" data-chart-empty="true" />;
  }

  const toneClass = sparklineTone ?? tone;

  return (
    <div
      aria-hidden="true"
      className={`factory-showcase-kpi__sparkline factory-showcase-kpi__sparkline--${toneClass}`}
      data-chart-kind="kpi-sparkline"
    >
      <svg preserveAspectRatio="none" viewBox={`0 0 ${SPARKLINE_WIDTH} ${SPARKLINE_HEIGHT}`}>
        {series.map((entry, index) => {
          const areaPath = entry.variant === "solid" ? createAreaPath(entry.values) : "";
          const linePath = createSparklinePath(entry.values);
          if (!linePath) {
            return null;
          }

          return (
            <g key={`${entry.variant}-${index}`}>
              {areaPath ? <path className="factory-showcase-kpi__sparkline-area" d={areaPath} /> : null}
              <path
                className={`factory-showcase-kpi__sparkline-line factory-showcase-kpi__sparkline-line--${entry.variant}`}
                d={linePath}
                fill="none"
              />
            </g>
          );
        })}
      </svg>
    </div>
  );
}

function KpiIcon({ iconKey }: Readonly<{ iconKey: DashboardShowcaseKpiCard["iconKey"] }>) {
  switch (iconKey) {
    case "production":
      return <Activity size={18} strokeWidth={1.85} />;
    case "oee":
      return <Gauge size={18} strokeWidth={1.85} />;
    case "scrap":
      return <Percent size={18} strokeWidth={1.85} />;
    case "cycle":
      return <Clock3 size={18} strokeWidth={1.85} />;
    case "quality":
      return <ShieldCheck size={18} strokeWidth={1.85} />;
    case "target":
      return <Target size={18} strokeWidth={1.85} />;
    default:
      return <Activity size={18} strokeWidth={1.85} />;
  }
}

function KpiCard({
  card,
  pendingLabel,
}: Readonly<{ card: DashboardShowcaseKpiCard; pendingLabel: string }>) {
  return (
    <article className={`factory-showcase-kpi factory-showcase-kpi--${card.tone}`} data-kpi-id={card.id}>
      <div className="factory-showcase-kpi__header">
        <div className="factory-showcase-kpi__copy">
          <p className="factory-showcase-kpi__label">{card.label}</p>
          <div className="factory-showcase-kpi__value-row">
            <strong className="factory-showcase-kpi__value">{card.pending ? "—" : card.value}</strong>
            {card.unit ? <span className="factory-showcase-kpi__unit">{card.unit}</span> : null}
          </div>
          {card.comparisonLabel ? (
            <span className="factory-showcase-kpi__comparison">{card.comparisonLabel}</span>
          ) : null}
          {card.supportingText ? (
            <span className="factory-showcase-kpi__supporting">{card.supportingText}</span>
          ) : null}
        </div>
        <span aria-hidden="true" className="factory-showcase-kpi__icon">
          <KpiIcon iconKey={card.iconKey} />
        </span>
      </div>
      <div className="factory-showcase-kpi__chart-area">
        {card.pending ? (
          <p className="factory-showcase-kpi__pending">{pendingLabel}</p>
        ) : (
          <ShowcaseSparkline series={card.sparklines} sparklineTone={card.sparklineTone} tone={card.tone} />
        )}
        {card.showSparklineLegend && !card.pending ? (
          <div className="factory-showcase-kpi__sparkline-legend">
            <span><i />Gerçekleşen</span>
            <span><i className="factory-showcase-kpi__sparkline-legend-target" />Hedef</span>
          </div>
        ) : null}
      </div>
    </article>
  );
}

function HourlyProductionChart({
  points,
  pending,
  pendingLabel,
}: Readonly<{
  points: readonly DashboardShowcaseHourlyPoint[];
  pending: boolean;
  pendingLabel: string;
}>) {
  const chartWidth = 1000;
  const chartHeight = 180;
  const padding = { top: 16, right: 18, bottom: 34, left: 52 };
  const innerWidth = chartWidth - padding.left - padding.right;
  const innerHeight = chartHeight - padding.top - padding.bottom;

  if (pending || points.length === 0) {
    return (
      <p className="factory-showcase-chart__pending" data-chart-empty="true" data-testid="apps-hourly-production-empty">
        {pendingLabel}
      </p>
    );
  }

  const actualValues = points.map((point) => point.actual ?? 0);
  const targetValues = points.map((point) => point.target ?? 0);
  const maxValue = Math.max(...actualValues, ...targetValues, 1);
  const yMax = Math.ceil(maxValue / 1000) * 1000 || 1000;
  const yTicks = [0, 0.25, 0.5, 0.75, 1].map((position) => Math.round(yMax * position));

  const getX = (index: number) => padding.left + (index / Math.max(points.length - 1, 1)) * innerWidth;
  const getY = (value: number) => padding.top + innerHeight - (value / yMax) * innerHeight;

  const createSeriesPath = (key: "actual" | "target") =>
    points
      .map((point, index) => {
        const value = key === "actual" ? point.actual ?? 0 : point.target ?? 0;
        return `${index === 0 ? "M" : "L"} ${getX(index)} ${getY(value)}`;
      })
      .join(" ");

  const actualPath = createSeriesPath("actual");
  const targetPath = createSeriesPath("target");

  return (
    <div className="factory-showcase-chart__canvas" data-chart-kind="hourly-production" data-testid="apps-hourly-production-chart">
      <div className="factory-showcase-chart__plot">
        <svg
          aria-label="Saatlik üretim performansı grafiği"
          preserveAspectRatio="xMidYMid meet"
          role="img"
          viewBox={`0 0 ${chartWidth} ${chartHeight}`}
        >
          {yTicks.map((tick) => {
            const y = getY(tick);
            return (
              <g key={tick}>
                <line
                  x1={padding.left}
                  x2={chartWidth - padding.right}
                  y1={y}
                  y2={y}
                  className="factory-showcase-chart__grid-line"
                />
                <text className="factory-showcase-chart__axis-label" x={padding.left - 8} y={y + 4} textAnchor="end">
                  {tick.toLocaleString("tr-TR")}
                </text>
              </g>
            );
          })}

          <path className="factory-showcase-chart__line factory-showcase-chart__line--target" d={targetPath} fill="none" data-series="target" />
          <path className="factory-showcase-chart__line factory-showcase-chart__line--actual" d={actualPath} fill="none" data-series="actual" />

          {points.map((point, index) => {
            if (!hasNumeric(point.actual)) {
              return null;
            }

            return (
              <circle
                key={point.hourLabel}
                className="factory-showcase-chart__point"
                cx={getX(index)}
                cy={getY(point.actual)}
                data-series="actual-point"
                r="3.5"
              />
            );
          })}

          {points.filter((_, index) => index % 2 === 0).map((point) => {
            const index = points.indexOf(point);
            return (
              <text
                key={`${point.hourLabel}-label`}
                className="factory-showcase-chart__axis-label factory-showcase-chart__axis-label--x"
                x={getX(index)}
                y={chartHeight - 10}
                textAnchor="middle"
              >
                {point.hourLabel}
              </text>
            );
          })}
        </svg>
      </div>
      <div className="factory-showcase-chart__legend">
        <span><i className="factory-showcase-chart__legend-line factory-showcase-chart__legend-line--actual" />Gerçekleşen Üretim</span>
        <span><i className="factory-showcase-chart__legend-line factory-showcase-chart__legend-line--target" />Hedef Üretim</span>
      </div>
    </div>
  );
}

function OeeComponentsChart({
  components,
  pending,
  pendingLabel,
}: Readonly<{
  components: readonly DashboardShowcaseOeeComponent[];
  pending: boolean;
  pendingLabel: string;
}>) {
  if (pending) {
    return (
      <p className="factory-showcase-chart__pending" data-chart-empty="true" data-testid="apps-oee-components-empty">
        {pendingLabel}
      </p>
    );
  }

  const chartWidth = 360;
  const chartHeight = 200;
  const padding = { top: 18, right: 12, bottom: 34, left: 42 };
  const innerHeight = chartHeight - padding.top - padding.bottom;
  const barWidth = 56;
  const gap = (chartWidth - padding.left - padding.right - barWidth * components.length)
    / Math.max(components.length - 1, 1);

  return (
    <div className="factory-showcase-chart__canvas" data-chart-kind="oee-components" data-testid="apps-oee-components-chart">
      <div className="factory-showcase-chart__plot">
        <svg aria-label="OEE bileşenleri grafiği" preserveAspectRatio="xMidYMid meet" role="img" viewBox={`0 0 ${chartWidth} ${chartHeight}`}>
          {[0, 25, 50, 75, 100].map((tick) => {
            const y = padding.top + innerHeight - (tick / 100) * innerHeight;
            return (
              <g key={tick}>
                <line
                  x1={padding.left}
                  x2={chartWidth - padding.right}
                  y1={y}
                  y2={y}
                  className="factory-showcase-chart__grid-line"
                />
                <text className="factory-showcase-chart__axis-label" x={padding.left - 8} y={y + 4} textAnchor="end">
                  {tick}%
                </text>
              </g>
            );
          })}

          {components.map((component, index) => {
            const percent = component.percent ?? 0;
            const x = padding.left + index * (barWidth + gap);
            const barHeight = (percent / 100) * innerHeight;
            const y = padding.top + innerHeight - barHeight;
            const toneClass = index === 0 ? "info" : index === 1 ? "success" : "quality";

            return (
              <g key={component.label}>
                <rect
                  className={`factory-showcase-bar factory-showcase-bar--${toneClass}`}
                  data-series="oee-bar"
                  height={barHeight}
                  rx="4"
                  width={barWidth}
                  x={x}
                  y={y}
                />
                <text className="factory-showcase-bar__value" x={x + barWidth / 2} y={y - 6} textAnchor="middle">
                  {formatShowcasePercent(component.percent)}
                </text>
                <text
                  className="factory-showcase-chart__axis-label factory-showcase-chart__axis-label--x"
                  x={x + barWidth / 2}
                  y={chartHeight - 10}
                  textAnchor="middle"
                >
                  {component.label}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
    </div>
  );
}

function DistributionChart({
  rows,
  total,
  pending,
  pendingLabel,
}: Readonly<{
  rows: readonly DashboardShowcaseDistributionRow[];
  total: number | null;
  pending: boolean;
  pendingLabel: string;
}>) {
  if (pending || rows.length === 0 || !hasNumeric(total)) {
    return (
      <p className="factory-showcase-chart__pending" data-chart-empty="true" data-testid="apps-production-distribution-empty">
        {pendingLabel}
      </p>
    );
  }

  const radius = 54;
  const circumference = 2 * Math.PI * radius;
  const segments = rows.reduce(
    (state, row) => {
      const length = (row.percent / 100) * circumference;
      state.items.push({
        row,
        dashArray: `${length} ${circumference - length}`,
        dashOffset: -state.offset,
      });
      state.offset += length;
      return state;
    },
    {
      offset: 0,
      items: [] as Array<{
        row: DashboardShowcaseDistributionRow;
        dashArray: string;
        dashOffset: number;
      }>,
    },
  ).items;

  return (
    <div className="factory-showcase-distribution" data-chart-kind="distribution" data-testid="apps-production-distribution-chart">
      <div className="factory-showcase-distribution__chart">
        <svg aria-label="Üretim dağılımı grafiği" preserveAspectRatio="xMidYMid meet" role="img" viewBox="0 0 160 160">
          <g transform="translate(80 80)">
            {segments.map((segment) => (
              <circle
                key={segment.row.label}
                className={`factory-showcase-distribution__ring factory-showcase-distribution__ring--${segment.row.tone}`}
                cx="0"
                cy="0"
                data-series="distribution-segment"
                fill="none"
                r={radius}
                strokeDasharray={segment.dashArray}
                strokeDashoffset={segment.dashOffset}
                strokeWidth="18"
                transform="rotate(-90)"
              />
            ))}
          </g>
        </svg>
        <div className="factory-showcase-distribution__center">
          <strong>{formatShowcaseNumber(total)}</strong>
          <span>adet</span>
        </div>
      </div>

      <div className="factory-showcase-distribution__table">
        {rows.map((row) => (
          <div className="factory-showcase-distribution__row" key={row.label}>
            <span className={`factory-showcase-distribution__dot factory-showcase-distribution__dot--${row.tone}`} />
            <span className="factory-showcase-distribution__label">{row.label}</span>
            <span className="factory-showcase-distribution__bar-track">
              <span
                className={`factory-showcase-distribution__bar factory-showcase-distribution__bar--${row.tone}`}
                style={{ width: `${Math.max(row.percent, 4)}%` }}
              />
            </span>
            <span className="factory-showcase-distribution__quantity">{formatShowcaseNumber(row.quantity)}</span>
            <span className="factory-showcase-distribution__percent">{formatShowcasePercent(row.percent)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

export function FactoryOperationsAnalyticsGrid({
  viewModel,
  pendingLabel,
  rootClassName = "apps-factory-ops-dashboard factory-showcase",
  testId = "apps-factory-operations-analytics",
}: FactoryOperationsAnalyticsGridProps) {
  const orderedCards = KPI_CARD_ORDER
    .map((id) => viewModel.kpiCards.find((card) => card.id === id))
    .filter((card): card is DashboardShowcaseKpiCard => Boolean(card));

  return (
    <div className={rootClassName} data-testid={testId}>
      <section aria-label="Temel operasyon göstergeleri" className="factory-showcase__kpi-grid">
        {orderedCards.map((card) => (
          <KpiCard card={card} key={card.id} pendingLabel={pendingLabel} />
        ))}
      </section>

      <section aria-label="Saatlik üretim performansı" className="factory-showcase__panel factory-showcase__hourly">
        <h2>Saatlik Üretim Performansı</h2>
        <HourlyProductionChart pending={viewModel.hourlyPending} pendingLabel={pendingLabel} points={viewModel.hourlyProduction} />
      </section>

      <section aria-label="Alt grafikler" className="factory-showcase__bottom-grid">
        <article className="factory-showcase__panel">
          <h2>OEE Bileşenleri</h2>
          <OeeComponentsChart
            components={viewModel.oeeComponents}
            pending={viewModel.oeeComponentsPending}
            pendingLabel={pendingLabel}
          />
        </article>

        <article className="factory-showcase__panel">
          <h2>Üretim Dağılımı</h2>
          <DistributionChart
            pending={viewModel.distributionPending}
            pendingLabel={pendingLabel}
            rows={viewModel.distributionRows}
            total={viewModel.distributionTotal}
          />
        </article>
      </section>
    </div>
  );
}

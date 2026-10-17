"use client";

import Link from "next/link";
import { displayTenantName } from "@/lib/presentation/display-name";
import { useCallback, useEffect, useRef, useState, useTransition } from "react";
import {
  Activity,
  BarChart3,
  Building2,
  Clock3,
  Gauge,
  Maximize2,
  Percent,
  Radio,
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
} from "../domain/dashboard-showcase-types";
import {
  formatShowcaseNumber,
  formatShowcasePercent,
  formatShowcaseRefreshLabel,
} from "../domain/dashboard-showcase-labels";
import { FactosBrandLockup } from "@/components/brand/factos-brand-lockup";
import { refreshDashboardShowcaseAction } from "../server/dashboard-showcase-actions";
import "./factory-showcase.css";

const KPI_CARD_ORDER = [
  "actual-production",
  "oee",
  "scrap-rate",
  "cycle-time",
  "quality-compliance",
  "target-achievement",
] as const;

const REFRESH_INTERVAL_MS = 60_000;
const SPARKLINE_WIDTH = 120;
const SPARKLINE_HEIGHT = 36;

type FactoryShowcaseViewProps = Readonly<{
  initialViewModel: DashboardShowcaseViewModel;
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

function formatClockTime(date: Date): string {
  return date.toLocaleTimeString("tr-TR", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function formatClockDate(date: Date): string {
  return date.toLocaleDateString("tr-TR", {
    day: "numeric",
    month: "long",
    year: "numeric",
    weekday: "long",
  });
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
    return null;
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

function KpiIcon({
  iconKey,
}: Readonly<{ iconKey: DashboardShowcaseKpiCard["iconKey"] }>) {
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

function KpiCard({ card: source }: Readonly<{ card: DashboardShowcaseKpiCard }>) {
  /* A zero figure with no series is the same "no data" state as a pending card, so all six cards share one layout. */
  const card = !source.pending && source.sparklines.length === 0 && /^0([.,]0+)?$/.test(String(source.value))
    ? { ...source, pending: true }
    : source;
  return (
    <article className={`factory-showcase-kpi factory-showcase-kpi--${card.tone}`} data-kpi-id={card.id}>
      <div className="factory-showcase-kpi__header">
        <div className="factory-showcase-kpi__copy">
          <p className="factory-showcase-kpi__label">{card.label}</p>
          <div className="factory-showcase-kpi__value-row">
            <strong className="factory-showcase-kpi__value">{card.pending ? "—" : card.value}</strong>
            {card.unit && !card.pending ? <span className="factory-showcase-kpi__unit">{card.unit}</span> : null}
          </div>
          <div className="factory-showcase-kpi__meta">
            {card.comparisonLabel && !card.pending ? (
              <span className="factory-showcase-kpi__comparison">{card.comparisonLabel}</span>
            ) : null}
            {card.supportingText && !card.pending ? (
              <span className="factory-showcase-kpi__supporting">{card.supportingText}</span>
            ) : null}
          </div>
        </div>
        <span aria-hidden="true" className="factory-showcase-kpi__icon">
          <KpiIcon iconKey={card.iconKey} />
        </span>
      </div>
      <div className="factory-showcase-kpi__chart-area">
        {card.pending ? null : (
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
}: Readonly<{ points: readonly DashboardShowcaseHourlyPoint[]; pending: boolean }>) {
  const chartWidth = 1000;
  const chartHeight = 180;
  const padding = { top: 16, right: 18, bottom: 34, left: 52 };
  const innerWidth = chartWidth - padding.left - padding.right;
  const innerHeight = chartHeight - padding.top - padding.bottom;

  if (pending || points.length === 0) {
    return <p className="factory-showcase-chart__pending" data-chart-empty="true"><BarChart3 aria-hidden="true" size={18} strokeWidth={1.6} />Henüz veri yok</p>;
  }

  const actualValues = points.map((point) => point.actual ?? 0);
  const targetValues = points.map((point) => point.target ?? 0);
  const maxValue = Math.max(...actualValues, ...targetValues, 1);
  const yMax = Math.ceil(maxValue / 1000) * 1000 || 1000;
  const yTicks = [0, 0.25, 0.5, 0.75, 1].map((position) => Math.round(yMax * position));

  const getX = (index: number) => padding.left + (index / Math.max(points.length - 1, 1)) * innerWidth;
  const getY = (value: number) => padding.top + innerHeight - (value / yMax) * innerHeight;

  const createSeriesPath = (key: "actual" | "target") => points
    .map((point, index) => {
      const value = key === "actual" ? point.actual ?? 0 : point.target ?? 0;
      return `${index === 0 ? "M" : "L"} ${getX(index)} ${getY(value)}`;
    })
    .join(" ");

  const actualPath = createSeriesPath("actual");
  const targetPath = createSeriesPath("target");

  return (
    <div className="factory-showcase-chart__canvas" data-chart-kind="hourly-production">
      <div className="factory-showcase-chart__plot">
        <svg aria-label="Saatlik üretim performansı grafiği" preserveAspectRatio="xMidYMid meet" role="img" viewBox={`0 0 ${chartWidth} ${chartHeight}`}>
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

function hasNumeric(value: number | null | undefined): value is number {
  return value !== null && value !== undefined && !Number.isNaN(value);
}

function OeeComponentsChart({
  components,
  pending,
}: Readonly<{ components: readonly DashboardShowcaseOeeComponent[]; pending: boolean }>) {
  if (pending) {
    return <p className="factory-showcase-chart__pending" data-chart-empty="true"><BarChart3 aria-hidden="true" size={18} strokeWidth={1.6} />Henüz veri yok</p>;
  }

  const chartWidth = 360;
  const chartHeight = 200;
  const padding = { top: 18, right: 12, bottom: 34, left: 42 };
  const innerHeight = chartHeight - padding.top - padding.bottom;
  const barWidth = 56;
  const gap = (chartWidth - padding.left - padding.right - barWidth * components.length)
    / Math.max(components.length - 1, 1);

  return (
    <div className="factory-showcase-chart__canvas" data-chart-kind="oee-components">
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
                  %{tick}
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
}: Readonly<{
  rows: readonly DashboardShowcaseDistributionRow[];
  total: number | null;
  pending: boolean;
}>) {
  if (pending || rows.length === 0 || !hasNumeric(total)) {
    return <p className="factory-showcase-chart__pending" data-chart-empty="true"><BarChart3 aria-hidden="true" size={18} strokeWidth={1.6} />Henüz veri yok</p>;
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
    <div className="factory-showcase-distribution" data-chart-kind="distribution">
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

export function FactoryShowcaseView({ initialViewModel }: FactoryShowcaseViewProps) {
  const [viewModel, setViewModel] = useState(initialViewModel);
  const [now, setNow] = useState(() => new Date());
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [, startTransition] = useTransition();
  const refreshInFlightRef = useRef(false);

  const isCardEmpty = (card: DashboardShowcaseKpiCard) =>
    card.pending || (card.sparklines.length === 0 && /^0([.,]0+)?$/.test(String(card.value)));
  const orderedCards = KPI_CARD_ORDER
    .map((id) => viewModel.kpiCards.find((card) => card.id === id))
    .filter((card): card is DashboardShowcaseKpiCard => Boolean(card));

  const kpiSetEmpty = orderedCards.length > 0 && orderedCards.every(isCardEmpty);
  const hasStreamData = !kpiSetEmpty
    || (!viewModel.hourlyPending && viewModel.hourlyProduction.some((point) => (point.actual ?? 0) > 0))
    || (!viewModel.distributionPending && viewModel.distributionRows.some((row) => row.quantity > 0))
    || (!viewModel.oeeComponentsPending && viewModel.oeeComponents.some((component) => (component.percent ?? 0) > 0));
  const refreshData = useCallback(async () => {
    if (refreshInFlightRef.current) {
      return;
    }

    refreshInFlightRef.current = true;
    try {
      const result = await refreshDashboardShowcaseAction();
      if (result.status === "success") {
        setViewModel(result.viewModel);
      }
    } finally {
      refreshInFlightRef.current = false;
    }
  }, []);

  useEffect(() => {
    const clockTimer = window.setInterval(() => setNow(new Date()), 1000);
    const refreshTimer = window.setInterval(() => {
      startTransition(() => {
        void refreshData();
      });
    }, REFRESH_INTERVAL_MS);

    return () => {
      window.clearInterval(clockTimer);
      window.clearInterval(refreshTimer);
    };
  }, [refreshData, startTransition]);

  useEffect(() => {
    function handleFullscreenChange() {
      setIsFullscreen(Boolean(document.fullscreenElement));
    }

    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () => document.removeEventListener("fullscreenchange", handleFullscreenChange);
  }, []);

  const toggleFullscreen = useCallback(async () => {
    if (document.fullscreenElement) {
      await document.exitFullscreen();
      return;
    }

    await document.documentElement.requestFullscreen();
  }, []);

  return (
    <div className="factory-showcase" data-presentation-mode={viewModel.presentationMode}>
      <header className="factory-showcase__header">
        <div className="factory-showcase__brand">
          <FactosBrandLockup className="factory-showcase__brand-lockup" />
          <span aria-hidden="true" className="factory-showcase__brand-divider" />
          <h1 className="factory-showcase__title">Fabrika Operasyon Görünümü</h1>
        </div>

        <div className="factory-showcase__facility">
          <Building2 aria-hidden="true" size={16} strokeWidth={1.8} />
          <span>{displayTenantName(viewModel.facilityDisplayName)}</span>
        </div>

        <div className="factory-showcase__status">
          <div className="factory-showcase__clock">
            <Clock3 aria-hidden="true" size={16} strokeWidth={1.8} />
            <div>
              <strong suppressHydrationWarning>{formatClockTime(now)}</strong>
              <span suppressHydrationWarning>{formatClockDate(now)}</span>
            </div>
          </div>

          <div className="factory-showcase__refresh">
            <span>{hasStreamData ? `Son güncelleme: ${formatShowcaseRefreshLabel(viewModel.lastRefreshedAtUtc)}` : "Veri bekleniyor"}</span>
            <span aria-hidden="true" className={hasStreamData ? "factory-showcase__status-dot" : "factory-showcase__status-dot factory-showcase__status-dot--idle"} />
          </div>

          <div className="factory-showcase__live">
            <Radio aria-hidden="true" size={15} strokeWidth={1.8} />
            <span>{hasStreamData ? "Operasyon akışı izleniyor" : "Operasyon akışı bekleniyor"}</span>
          </div>

          <Link className="factory-showcase__back" href="/fabrika">Fabrikaya dön</Link>

          <button
            aria-label={isFullscreen ? "Tam ekrandan çık" : "Tam ekrana geç"}
            className="factory-showcase__fullscreen"
            onClick={() => {
              void toggleFullscreen();
            }}
            type="button"
          >
            <Maximize2 aria-hidden="true" size={16} strokeWidth={1.8} />
          </button>
        </div>
      </header>

      {viewModel.developmentPilotBanner ? (
        <p className="factory-showcase__pilot-banner">Örnek veri</p>
      ) : null}

      {kpiSetEmpty ? (
        <section aria-label="Temel operasyon göstergeleri" className="factory-showcase__panel factory-showcase-kpi-compact">
          <h2>Göstergeler</h2>
          <ul className="factory-showcase-kpi-compact__list">
            {orderedCards.map((card) => (
              <li className="factory-showcase-kpi-compact__row" data-kpi-id={card.id} key={card.id}>
                <span>{card.label}</span>
                <span>—</span>
              </li>
            ))}
          </ul>
        </section>
      ) : (
        <section aria-label="Temel operasyon göstergeleri" className="factory-showcase__kpi-grid">
          {orderedCards.map((card) => (
            <KpiCard card={card} key={card.id} />
          ))}
        </section>
      )}

      <section aria-label="Saatlik üretim performansı" className="factory-showcase__panel factory-showcase__hourly">
        <h2>Saatlik üretim performansı</h2>
        <HourlyProductionChart pending={viewModel.hourlyPending} points={viewModel.hourlyProduction} />
      </section>

      <section aria-label="Alt grafikler" className="factory-showcase__bottom-grid">
        <article className="factory-showcase__panel">
          <h2>OEE bileşenleri</h2>
          <OeeComponentsChart components={viewModel.oeeComponents} pending={viewModel.oeeComponentsPending} />
        </article>

        <article className="factory-showcase__panel">
          <h2>Üretim dağılımı</h2>
          <DistributionChart
            pending={viewModel.distributionPending}
            rows={viewModel.distributionRows}
            total={viewModel.distributionTotal}
          />
        </article>
      </section>
    </div>
  );
}

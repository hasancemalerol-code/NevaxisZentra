import type { LucideIcon } from "lucide-react";
import {
  Activity,
  AlertTriangle,
  Boxes,
  Clock3,
  Factory,
  Gauge,
  PackageSearch,
  Percent,
  ShoppingCart,
  TimerReset,
  Wrench,
} from "lucide-react";

import type {
  DashboardMetric,
  DashboardTone,
} from "@/lib/mock-data/dashboard";

interface DashboardMetricsProps {
  metrics: DashboardMetric[];
}

interface LockedDashboardMetric {
  id: string;
  label: string;
  value: string;
  unit?: string;
  changeLabel?: string;
  statusLabel?: string;
  supportingText?: string;
  tone: DashboardTone;
  icon: LucideIcon;
  sparkline?: number[];
}

const lockedMetricDefinitions: LockedDashboardMetric[] = [
  {
    id: "daily-production",
    label: "Gerçekleşen Üretim",
    value: "42.680",
    unit: "adet",
    changeLabel: "↑ %12,5",
    supportingText: "Günlük üretim",
    tone: "info",
    icon: Activity,
    sparkline: [28, 31, 29, 34, 32, 38, 35, 41, 39, 46, 43, 51],
  },
  {
    id: "oee",
    label: "OEE",
    value: "%78,5",
    changeLabel: "↑ %2,3",
    supportingText: "Genel ekipman etkinliği",
    tone: "success",
    icon: Gauge,
    sparkline: [42, 41, 46, 43, 49, 47, 53, 50, 57, 54, 61, 58],
  },
  {
    id: "scrap-rate",
    label: "Fire Oranı",
    value: "%2,35",
    changeLabel: "↓ %0,4",
    supportingText: "Hedefin altında",
    tone: "warning",
    icon: Percent,
    sparkline: [54, 50, 53, 48, 46, 49, 43, 45, 39, 41, 35, 31],
  },
  {
    id: "cycle-time",
    label: "Çevrim Süresi",
    value: "22,8",
    unit: "sn",
    changeLabel: "↓ %1,2",
    supportingText: "Ortalama çevrim",
    tone: "info",
    icon: Clock3,
    sparkline: [48, 45, 49, 44, 46, 40, 42, 37, 40, 34, 36, 31],
  },
  {
    id: "open-orders",
    label: "Açık Sipariş",
    value: "32",
    changeLabel: "↑ %8,3",
    supportingText: "Aktif müşteri siparişi",
    tone: "info",
    icon: ShoppingCart,
  },
  {
    id: "critical-breakdowns",
    label: "Kritik Arıza",
    value: "2",
    statusLabel: "Acil",
    supportingText: "Müdahale gerekiyor",
    tone: "danger",
    icon: Wrench,
  },
  {
    id: "delivery-risk",
    label: "Termin Riski",
    value: "3",
    statusLabel: "Dikkat",
    supportingText: "Etkilenen sipariş",
    tone: "warning",
    icon: TimerReset,
  },
  {
    id: "material-risk",
    label: "Malzeme Riski",
    value: "7",
    statusLabel: "Düşük",
    supportingText: "İncelenen malzeme",
    tone: "success",
    icon: PackageSearch,
  },
];

const metricIconFallbacks = {
  "active-production-orders": Boxes,
  "plan-realization": Gauge,
  "critical-risks": AlertTriangle,
  "machine-utilization": Factory,
  "delivery-risk": TimerReset,
  "daily-production": Activity,
} as const;

function getToneLabel(tone: DashboardTone) {
  switch (tone) {
    case "success":
      return "Olumlu";
    case "warning":
      return "Dikkat";
    case "danger":
      return "Kritik";
    case "info":
      return "Bilgi";
    default:
      return "Nötr";
  }
}

function createSparklinePath(values: number[]) {
  if (values.length < 2) {
    return "";
  }

  const width = 120;
  const height = 28;
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = Math.max(max - min, 1);

  return values
    .map((value, index) => {
      const x = (index / (values.length - 1)) * width;
      const y = height - ((value - min) / range) * height;

      return `${index === 0 ? "M" : "L"} ${x.toFixed(2)} ${y.toFixed(2)}`;
    })
    .join(" ");
}

function mergeMetricData(
  definition: LockedDashboardMetric,
  sourceMetrics: DashboardMetric[],
): LockedDashboardMetric {
  const directMatch = sourceMetrics.find(
    (metric) => metric.id === definition.id,
  );

  if (!directMatch) {
    return definition;
  }

  return {
    ...definition,
    value: directMatch.value,
    supportingText:
      directMatch.supportingText || definition.supportingText,
    changeLabel:
      directMatch.changeLabel || definition.changeLabel,
    tone: directMatch.tone,
  };
}

export function DashboardMetrics({
  metrics,
}: DashboardMetricsProps) {
  const displayMetrics = lockedMetricDefinitions.map((definition) =>
    mergeMetricData(definition, metrics),
  );

  return (
    <section
      aria-label="Temel operasyon göstergeleri"
      className="locked-kpi-strip"
    >
      {displayMetrics.map((metric) => {
        const Icon =
          metric.icon ??
          metricIconFallbacks[
            metric.id as keyof typeof metricIconFallbacks
          ] ??
          Activity;

        const sparklinePath = metric.sparkline
          ? createSparklinePath(metric.sparkline)
          : null;

        return (
          <article
            className={`locked-kpi-card locked-kpi-card--${metric.tone}`}
            key={metric.id}
          >
            <div className="locked-kpi-card__top">
              <span
                aria-hidden="true"
                className="locked-kpi-card__icon"
              >
                <Icon size={17} strokeWidth={1.85} />
              </span>

              <div className="locked-kpi-card__heading">
                <p className="locked-kpi-card__label">
                  {metric.label}
                </p>

                <div className="locked-kpi-card__value-row">
                  <strong className="locked-kpi-card__value">
                    {metric.value}
                  </strong>

                  {metric.unit ? (
                    <span className="locked-kpi-card__unit">
                      {metric.unit}
                    </span>
                  ) : null}
                </div>
              </div>
            </div>

            {sparklinePath ? (
              <div
                aria-hidden="true"
                className="locked-kpi-card__sparkline"
              >
                <svg
                  preserveAspectRatio="none"
                  viewBox="0 0 120 28"
                >
                  <path
                    className="locked-kpi-card__sparkline-path"
                    d={sparklinePath}
                    fill="none"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                  />
                </svg>
              </div>
            ) : (
              <div className="locked-kpi-card__status-area">
                <span className="locked-kpi-card__status">
                  {metric.statusLabel ?? getToneLabel(metric.tone)}
                </span>

                {metric.supportingText ? (
                  <span className="locked-kpi-card__supporting">
                    {metric.supportingText}
                  </span>
                ) : null}
              </div>
            )}

            {metric.changeLabel ? (
              <div className="locked-kpi-card__footer">
                <span>{metric.changeLabel}</span>

                {metric.supportingText ? (
                  <small>{metric.supportingText}</small>
                ) : null}
              </div>
            ) : null}
          </article>
        );
      })}
    </section>
  );
}
import {
  AlertTriangle,
  Boxes,
  CheckCircle2,
  Clock3,
  Factory,
  PackageCheck,
  Sparkles,
  type LucideIcon,
} from "lucide-react";

import type { CSSProperties } from "react";

import { DashboardNavigationAction } from "./dashboard-navigation-action";
import {
  DASHBOARD_OPERATIONAL_SUMMARY_TARGET,
  resolveDailyOperationMetricTarget,
} from "./dashboard-navigation";

interface DailyOperationsSummaryProps {
  metrics: readonly unknown[];
  shifts: readonly unknown[];
  grantedPermissionKeys?: readonly string[];
}

interface NormalizedOperationMetric {
  id: string;
  label: string;
  value: string;
  tone: "primary" | "success" | "warning" | "danger";
  icon: LucideIcon;
}

const fallbackMetrics: NormalizedOperationMetric[] = [
  {
    id: "planned-production",
    label: "Planlanan üretim",
    value: "50.800 adet",
    tone: "primary",
    icon: Factory,
  },
  {
    id: "completed-production",
    label: "Tamamlanan",
    value: "42.680 adet",
    tone: "success",
    icon: PackageCheck,
  },
  {
    id: "remaining-production",
    label: "Kalan",
    value: "8.120 adet",
    tone: "warning",
    icon: Clock3,
  },
  {
    id: "quality-exceptions",
    label: "Kalite istisnası",
    value: "2",
    tone: "danger",
    icon: AlertTriangle,
  },
  {
    id: "critical-machines",
    label: "Kritik makineler",
    value: "2",
    tone: "danger",
    icon: Factory,
  },
  {
    id: "pending-deliveries",
    label: "Termin riski olan siparişler",
    value: "3",
    tone: "warning",
    icon: Boxes,
  },
];

const styles: Record<string, CSSProperties> = {
  panel: {
    display: "flex",
    minWidth: 0,
    flexDirection: "column",
    overflow: "hidden",
  },
  list: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    flex: "1 1 auto",
    padding: "4px 12px 2px",
    overflow: "hidden",
  },
  row: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "28px minmax(0, 1fr) auto",
    alignItems: "center",
    gap: 8,
    minHeight: 39,
    padding: "6px 0",
    borderBottom: "1px solid #dce5f0",
  },
  icon: {
    display: "grid",
    width: 28,
    height: 28,
    placeItems: "center",
    borderRadius: 8,
  },
  label: {
    overflow: "hidden",
    color: "#52627a",
    fontSize: "0.43rem",
    fontWeight: 690,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  value: {
    color: "#10213d",
    fontSize: "0.47rem",
    fontWeight: 820,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  footer: {
    display: "flex",
    minHeight: 29,
    flex: "0 0 29px",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8,
    padding: "0 12px",
    borderTop: "1px solid #dce5f0",
    color: "#75839a",
    fontSize: "0.38rem",
  },
  footerStatus: {
    display: "inline-flex",
    alignItems: "center",
    gap: 4,
    color: "#08945f",
    fontWeight: 720,
  },
  footerAction: {
    color: "#1769e0",
    fontWeight: 750,
    border: 0,
    background: "transparent",
    font: "inherit",
    fontSize: "inherit",
    padding: 0,
    cursor: "pointer",
    textDecoration: "none",
  },
  metricLink: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "28px minmax(0, 1fr) auto",
    alignItems: "center",
    gap: 8,
    minHeight: 39,
    padding: "6px 0",
    borderBottom: "1px solid #dce5f0",
    color: "inherit",
    textDecoration: "none",
    cursor: "pointer",
  },
  metricButton: {
    display: "grid",
    width: "100%",
    minWidth: 0,
    gridTemplateColumns: "28px minmax(0, 1fr) auto",
    alignItems: "center",
    gap: 8,
    minHeight: 39,
    padding: "6px 0",
    border: 0,
    borderBottom: "1px solid #dce5f0",
    background: "transparent",
    color: "inherit",
    font: "inherit",
    textAlign: "left",
    cursor: "not-allowed",
  },
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null
    ? (value as Record<string, unknown>)
    : {};
}

function readString(
  source: Record<string, unknown>,
  keys: string[],
  fallback: string,
) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === "string" && value.trim().length > 0) {
      return value;
    }

    if (typeof value === "number" && Number.isFinite(value)) {
      return new Intl.NumberFormat("tr-TR").format(value);
    }
  }

  return fallback;
}

function readTone(
  source: Record<string, unknown>,
  fallback: NormalizedOperationMetric["tone"],
): NormalizedOperationMetric["tone"] {
  const rawValue = readString(
    source,
    ["tone", "status", "statusType", "severity"],
    fallback,
  ).toLocaleLowerCase("tr-TR");

  if (
    rawValue.includes("danger") ||
    rawValue.includes("critical") ||
    rawValue.includes("kritik")
  ) {
    return "danger";
  }

  if (
    rawValue.includes("warning") ||
    rawValue.includes("attention") ||
    rawValue.includes("dikkat")
  ) {
    return "warning";
  }

  if (
    rawValue.includes("success") ||
    rawValue.includes("healthy") ||
    rawValue.includes("tamam")
  ) {
    return "success";
  }

  return "primary";
}

function getIcon(label: string): LucideIcon {
  const normalizedLabel = label.toLocaleLowerCase("tr-TR");

  if (
    normalizedLabel.includes("kalite") ||
    normalizedLabel.includes("kritik")
  ) {
    return AlertTriangle;
  }

  if (
    normalizedLabel.includes("tamam") ||
    normalizedLabel.includes("gerçekleş")
  ) {
    return PackageCheck;
  }

  if (
    normalizedLabel.includes("kalan") ||
    normalizedLabel.includes("termin")
  ) {
    return Clock3;
  }

  if (
    normalizedLabel.includes("sipariş") ||
    normalizedLabel.includes("teslimat")
  ) {
    return Boxes;
  }

  return Factory;
}

function normalizeMetric(
  value: unknown,
  index: number,
): NormalizedOperationMetric {
  const source = asRecord(value);
  const fallback =
    fallbackMetrics[index] ??
    fallbackMetrics[fallbackMetrics.length - 1];

  const label = readString(
    source,
    ["label", "title", "name", "metricLabel"],
    fallback.label,
  );

  return {
    id: readString(
      source,
      ["id", "code", "key"],
      `daily-operation-${index + 1}`,
    ),
    label,
    value: readString(
      source,
      [
        "formattedValue",
        "valueLabel",
        "displayValue",
        "value",
        "count",
      ],
      fallback.value,
    ),
    tone: readTone(source, fallback.tone),
    icon: getIcon(label),
  };
}

function getToneStyle(
  tone: NormalizedOperationMetric["tone"],
): CSSProperties {
  switch (tone) {
    case "success":
      return {
        background: "#e8f8f1",
        color: "#08945f",
      };
    case "warning":
      return {
        background: "#fff3df",
        color: "#dc7a00",
      };
    case "danger":
      return {
        background: "#ffecef",
        color: "#dc3545",
      };
    case "primary":
      return {
        background: "#eaf2ff",
        color: "#1769e0",
      };
  }
}

export function DailyOperationsSummary({
  metrics,
  shifts,
  grantedPermissionKeys = [],
}: DailyOperationsSummaryProps) {
  const sourceMetrics =
    metrics.length > 0 ? metrics : fallbackMetrics;

  const normalizedMetrics = sourceMetrics
    .slice(0, 6)
    .map(normalizeMetric);

  const visibleMetrics =
    normalizedMetrics.length >= 4
      ? normalizedMetrics
      : fallbackMetrics.slice(0, 6);

  return (
    <section
      aria-labelledby="daily-operations-title"
      className="dashboard-panel dashboard-panel--daily-operations"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Günlük üretim görünümü
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="daily-operations-title"
          >
            Günlük Operasyon Özeti
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <Sparkles
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          Sabit önizleme verisi
        </div>
      </div>

      <div style={styles.list}>
        {visibleMetrics.map((metric, index) => {
          const Icon = metric.icon;
          const target = resolveDailyOperationMetricTarget(metric.id);
          const rowStyle = {
            ...(index === visibleMetrics.length - 1
              ? { borderBottom: 0 }
              : {}),
          };

          return (
            <DashboardNavigationAction
              ariaLabel={`${metric.label} özeti: ${metric.value}`}
              grantedPermissionKeys={grantedPermissionKeys}
              key={metric.id}
              style={{
                ...styles.metricLink,
                ...rowStyle,
              }}
              target={target}
            >
              <span
                aria-hidden="true"
                style={{
                  ...styles.icon,
                  ...getToneStyle(metric.tone),
                }}
              >
                <Icon size={14} strokeWidth={1.8} />
              </span>

              <span style={styles.label}>
                {metric.label}
              </span>

              <strong style={styles.value}>
                {metric.value}
              </strong>
            </DashboardNavigationAction>
          );
        })}
      </div>

      <div style={styles.footer}>
        <span style={styles.footerStatus}>
          <CheckCircle2
            aria-hidden="true"
            size={11}
            strokeWidth={1.8}
          />
          {shifts.length > 0
            ? `${shifts.length} vardiya izleniyor`
            : "Operasyon akışı izleniyor"}
        </span>

        <DashboardNavigationAction
          ariaLabel="Detaylı operasyon özetini görüntüle"
          grantedPermissionKeys={grantedPermissionKeys}
          style={styles.footerAction}
          target={DASHBOARD_OPERATIONAL_SUMMARY_TARGET}
        >
          Detaylı özeti görüntüle
        </DashboardNavigationAction>
      </div>
    </section>
  );
}
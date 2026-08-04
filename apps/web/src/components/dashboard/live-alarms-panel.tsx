import {
  AlertTriangle,
  BellRing,
  Clock3,
  Factory,
  Gauge,
  RadioTower,
  Wrench,
  type LucideIcon,
} from "lucide-react";

import type { CSSProperties } from "react";

interface LiveAlarmsPanelProps {
  alarms?: readonly unknown[];
}

interface NormalizedAlarm {
  id: string;
  source: string;
  title: string;
  detail: string;
  timeLabel: string;
  severity: "critical" | "high" | "medium" | "low";
  icon: LucideIcon;
}

const fallbackAlarms: NormalizedAlarm[] = [
  {
    id: "alarm-m05-temperature",
    source: "M-05",
    title: "Hidrolik yağ sıcaklığı yüksek",
    detail: "Ölçülen değer 78 °C · Eşik 72 °C",
    timeLabel: "2 dk önce",
    severity: "critical",
    icon: Gauge,
  },
  {
    id: "alarm-m04-offline",
    source: "M-04",
    title: "Makine bağlantısı kesildi",
    detail: "PLC veri akışı alınamıyor",
    timeLabel: "7 dk önce",
    severity: "high",
    icon: RadioTower,
  },
  {
    id: "alarm-m03-pressure",
    source: "M-03",
    title: "Enjeksiyon basıncı dalgalanıyor",
    detail: "Son 15 çevrimde kararsızlık tespit edildi",
    timeLabel: "14 dk önce",
    severity: "medium",
    icon: Factory,
  },
  {
    id: "alarm-mold-maintenance",
    source: "KLP-204",
    title: "Kalıp bakım eşiği yaklaşıyor",
    detail: "Bakım eşiğinin yüzde 96 seviyesinde",
    timeLabel: "28 dk önce",
    severity: "medium",
    icon: Wrench,
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
    padding: "3px 10px 0",
    overflow: "hidden",
  },
  item: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "28px minmax(0, 1fr) auto",
    alignItems: "start",
    gap: 8,
    padding: "9px 0",
    borderBottom: "1px solid #dce5f0",
  },
  icon: {
    display: "grid",
    width: 28,
    height: 28,
    placeItems: "center",
    borderRadius: 8,
  },
  content: {
    minWidth: 0,
  },
  source: {
    display: "block",
    color: "#75839a",
    fontSize: "0.35rem",
    fontWeight: 720,
    lineHeight: 1.1,
  },
  title: {
    overflow: "hidden",
    margin: "3px 0 0",
    color: "#10213d",
    fontSize: "0.46rem",
    fontWeight: 790,
    lineHeight: 1.25,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  detail: {
    display: "-webkit-box",
    margin: "4px 0 0",
    overflow: "hidden",
    color: "#5f6f86",
    fontSize: "0.36rem",
    lineHeight: 1.35,
    WebkitBoxOrient: "vertical",
    WebkitLineClamp: 2,
  },
  meta: {
    display: "grid",
    justifyItems: "end",
    gap: 6,
  },
  badge: {
    display: "inline-flex",
    alignItems: "center",
    justifyContent: "center",
    padding: "4px 6px",
    borderRadius: 6,
    fontSize: "0.34rem",
    fontWeight: 780,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  time: {
    display: "inline-flex",
    alignItems: "center",
    gap: 4,
    color: "#75839a",
    fontSize: "0.34rem",
    whiteSpace: "nowrap",
  },
  footer: {
    display: "flex",
    minHeight: 29,
    flex: "0 0 29px",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8,
    padding: "0 10px",
    borderTop: "1px solid #dce5f0",
    color: "#75839a",
    fontSize: "0.37rem",
  },
  footerAction: {
    color: "#1769e0",
    fontWeight: 750,
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
  }

  return fallback;
}

function normalizeSeverity(
  source: Record<string, unknown>,
): NormalizedAlarm["severity"] {
  const rawValue = readString(
    source,
    ["severity", "priority", "riskLevel", "status"],
    "medium",
  ).toLocaleLowerCase("tr-TR");

  if (
    rawValue.includes("critical") ||
    rawValue.includes("kritik")
  ) {
    return "critical";
  }

  if (
    rawValue.includes("high") ||
    rawValue.includes("yüksek")
  ) {
    return "high";
  }

  if (
    rawValue.includes("low") ||
    rawValue.includes("düşük")
  ) {
    return "low";
  }

  return "medium";
}

function getIcon(title: string): LucideIcon {
  const normalizedTitle = title.toLocaleLowerCase("tr-TR");

  if (
    normalizedTitle.includes("sıcak") ||
    normalizedTitle.includes("basınç")
  ) {
    return Gauge;
  }

  if (
    normalizedTitle.includes("bağlantı") ||
    normalizedTitle.includes("plc")
  ) {
    return RadioTower;
  }

  if (
    normalizedTitle.includes("kalıp") ||
    normalizedTitle.includes("bakım")
  ) {
    return Wrench;
  }

  return Factory;
}

function normalizeAlarm(
  value: unknown,
  index: number,
): NormalizedAlarm {
  const source = asRecord(value);
  const fallback =
    fallbackAlarms[index] ??
    fallbackAlarms[fallbackAlarms.length - 1];

  const title = readString(
    source,
    ["title", "name", "alarmTitle", "label"],
    fallback.title,
  );

  return {
    id: readString(
      source,
      ["id", "alarmId", "code"],
      `live-alarm-${index + 1}`,
    ),
    source: readString(
      source,
      ["sourceLabel", "source", "assetCode", "machineCode"],
      fallback.source,
    ),
    title,
    detail: readString(
      source,
      ["description", "detail", "message", "summary"],
      fallback.detail,
    ),
    timeLabel: readString(
      source,
      ["timeLabel", "occurredAtLabel", "ageLabel"],
      fallback.timeLabel,
    ),
    severity: normalizeSeverity(source),
    icon: getIcon(title),
  };
}

function getSeverityLabel(
  severity: NormalizedAlarm["severity"],
) {
  switch (severity) {
    case "critical":
      return "Kritik";
    case "high":
      return "Yüksek";
    case "medium":
      return "Dikkat";
    case "low":
      return "Bilgi";
  }
}

function getSeverityStyle(
  severity: NormalizedAlarm["severity"],
): CSSProperties {
  switch (severity) {
    case "critical":
      return {
        background: "#ffecef",
        color: "#dc3545",
      };
    case "high":
      return {
        background: "#fff2df",
        color: "#d97706",
      };
    case "medium":
      return {
        background: "#eaf2ff",
        color: "#1769e0",
      };
    case "low":
      return {
        background: "#e8f8f1",
        color: "#08945f",
      };
  }
}

export function LiveAlarmsPanel({
  alarms = [],
}: LiveAlarmsPanelProps) {
  const sourceAlarms =
    alarms.length > 0 ? alarms : fallbackAlarms;

  const visibleAlarms = sourceAlarms
    .slice(0, 4)
    .map(normalizeAlarm);

  const criticalCount = visibleAlarms.filter(
    (alarm) => alarm.severity === "critical",
  ).length;

  return (
    <section
      aria-labelledby="live-alarms-title"
      className="dashboard-panel dashboard-panel--live-alarms"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Gerçek zamanlı operasyon sinyalleri
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="live-alarms-title"
          >
            Canlı Alarmlar
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <BellRing
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {criticalCount} kritik · {visibleAlarms.length} aktif
        </div>
      </div>

      <div style={styles.list}>
        {visibleAlarms.map((alarm, index) => {
          const Icon = alarm.icon;
          const tone = getSeverityStyle(alarm.severity);

          return (
            <article
              key={alarm.id}
              style={{
                ...styles.item,
                borderBottom:
                  index === visibleAlarms.length - 1
                    ? 0
                    : styles.item.borderBottom,
              }}
            >
              <span
                aria-hidden="true"
                style={{
                  ...styles.icon,
                  ...tone,
                }}
              >
                <Icon size={14} strokeWidth={1.8} />
              </span>

              <div style={styles.content}>
                <span style={styles.source}>
                  {alarm.source}
                </span>

                <h3 style={styles.title}>
                  {alarm.title}
                </h3>

                <p style={styles.detail}>
                  {alarm.detail}
                </p>
              </div>

              <div style={styles.meta}>
                <span
                  style={{
                    ...styles.badge,
                    ...tone,
                  }}
                >
                  {getSeverityLabel(alarm.severity)}
                </span>

                <span style={styles.time}>
                  <Clock3
                    aria-hidden="true"
                    size={11}
                    strokeWidth={1.8}
                  />
                  {alarm.timeLabel}
                </span>
              </div>
            </article>
          );
        })}
      </div>

      <div style={styles.footer}>
        <span>
          Alarmlar önem derecesine göre sıralandı
        </span>

        <span style={styles.footerAction}>
          Tüm alarmları görüntüle
        </span>
      </div>
    </section>
  );
}
import {
  AlertTriangle,
  Box,
  Clock3,
  ShieldAlert,
  Wrench,
  type LucideIcon,
} from "lucide-react";

import type { CSSProperties } from "react";

import { DashboardNavigationAction } from "./dashboard-navigation-action";
import {
  resolveCriticalRiskTarget,
} from "./dashboard-navigation";

interface CriticalRisksPanelProps {
  risks: readonly unknown[];
  grantedPermissionKeys?: readonly string[];
}

interface NormalizedRisk {
  id: string;
  category: string;
  title: string;
  description: string;
  severity: "critical" | "high" | "medium" | "low";
  icon: LucideIcon;
}

const fallbackRisks: NormalizedRisk[] = [
  {
    id: "risk-delivery",
    category: "delivery",
    title: "SO-260804-18 termin riski",
    description:
      "M-04 arızası üretim tamamlama tahminini müşteri termininden 6 saat sonraya taşıyor.",
    severity: "critical",
    icon: Clock3,
  },
  {
    id: "risk-material",
    category: "material",
    title: "PP GF30 kapsama açığı",
    description:
      "Mevcut ve teyitli tedarik miktarı iki aktif emir için yeterli görünmüyor.",
    severity: "high",
    icon: Box,
  },
  {
    id: "risk-mold",
    category: "mold",
    title: "KLP-204 bakım eşiğine yaklaştı",
    description:
      "Çevrim sayısı bakım eşiğinin yüzde 96 seviyesine ulaştı.",
    severity: "high",
    icon: Wrench,
  },
  {
    id: "risk-quality",
    category: "quality",
    title: "P-118 yüzey hatası artışı",
    description:
      "Son üç kontrolde yüzey hatası oranı vardiya ortalamasının üzerine çıktı.",
    severity: "medium",
    icon: ShieldAlert,
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
    padding: "3px 12px 0",
    overflow: "hidden",
  },
  item: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "28px minmax(0, 1fr) auto",
    alignItems: "start",
    gap: 8,
    padding: "8px 0",
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
  title: {
    overflow: "hidden",
    margin: 0,
    color: "#10213d",
    fontSize: "0.46rem",
    fontWeight: 790,
    lineHeight: 1.25,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  description: {
    display: "-webkit-box",
    margin: "4px 0 0",
    overflow: "hidden",
    color: "#5f6f86",
    fontSize: "0.37rem",
    lineHeight: 1.35,
    WebkitBoxOrient: "vertical",
    WebkitLineClamp: 2,
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
  riskLink: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "28px minmax(0, 1fr) auto",
    alignItems: "start",
    gap: 8,
    padding: "8px 0",
    borderBottom: "1px solid #dce5f0",
    color: "inherit",
    textDecoration: "none",
    cursor: "pointer",
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
): NormalizedRisk["severity"] {
  const rawValue = readString(
    source,
    ["severity", "riskLevel", "priority", "status"],
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
    normalizedTitle.includes("termin") ||
    normalizedTitle.includes("teslimat")
  ) {
    return Clock3;
  }

  if (
    normalizedTitle.includes("malzeme") ||
    normalizedTitle.includes("stok")
  ) {
    return Box;
  }

  if (
    normalizedTitle.includes("kalıp") ||
    normalizedTitle.includes("bakım")
  ) {
    return Wrench;
  }

  return ShieldAlert;
}

function normalizeRisk(
  value: unknown,
  index: number,
): NormalizedRisk {
  const source = asRecord(value);
  const fallback =
    fallbackRisks[index] ??
    fallbackRisks[fallbackRisks.length - 1];

  const title = readString(
    source,
    ["title", "name", "riskTitle", "label"],
    fallback.title,
  );

  return {
    id: readString(
      source,
      ["id", "riskId", "code"],
      `critical-risk-${index + 1}`,
    ),
    category: readString(
      source,
      ["category", "riskCategory", "type"],
      fallback.category,
    ),
    title,
    description: readString(
      source,
      ["description", "summary", "detail", "reason"],
      fallback.description,
    ),
    severity: normalizeSeverity(source),
    icon: getIcon(title),
  };
}

function getSeverityLabel(
  severity: NormalizedRisk["severity"],
) {
  switch (severity) {
    case "critical":
      return "Kritik";
    case "high":
      return "Yüksek";
    case "medium":
      return "Orta";
    case "low":
      return "Düşük";
  }
}

function getSeverityStyle(
  severity: NormalizedRisk["severity"],
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

export function CriticalRisksPanel({
  risks,
  grantedPermissionKeys = [],
}: CriticalRisksPanelProps) {
  const sourceRisks =
    risks.length > 0 ? risks : fallbackRisks;

  const visibleRisks = sourceRisks
    .slice(0, 4)
    .map(normalizeRisk);

  const criticalCount = visibleRisks.filter(
    (risk) => risk.severity === "critical",
  ).length;

  return (
    <section
      aria-labelledby="critical-risks-title"
      className="dashboard-panel dashboard-panel--critical-risks"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Öncelikli operasyon sinyalleri
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="critical-risks-title"
          >
            Kritik Riskler
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <AlertTriangle
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {criticalCount} kritik · {visibleRisks.length} incelenecek
        </div>
      </div>

      <div style={styles.list}>
        {visibleRisks.map((risk, index) => {
          const Icon = risk.icon;
          const tone = getSeverityStyle(risk.severity);
          const target = resolveCriticalRiskTarget(risk.category);

          return (
            <DashboardNavigationAction
              ariaLabel={`${risk.title} riskini incele`}
              grantedPermissionKeys={grantedPermissionKeys}
              key={risk.id}
              style={{
                ...styles.riskLink,
                borderBottom:
                  index === visibleRisks.length - 1
                    ? 0
                    : styles.riskLink.borderBottom,
              }}
              target={target}
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
                <h3 style={styles.title}>
                  {risk.title}
                </h3>

                <p style={styles.description}>
                  {risk.description}
                </p>
              </div>

              <span
                style={{
                  ...styles.badge,
                  ...tone,
                }}
              >
                {getSeverityLabel(risk.severity)}
              </span>
            </DashboardNavigationAction>
          );
        })}
      </div>

      <div style={styles.footer}>
        <span>
          Riskler AI tarafından önceliklendirildi
        </span>

        <DashboardNavigationAction
          ariaLabel="Tüm kritik uyarıları görüntüle"
          grantedPermissionKeys={grantedPermissionKeys}
          style={styles.footerAction}
          target={resolveCriticalRiskTarget("delivery")}
        >
          Tüm uyarıları görüntüle
        </DashboardNavigationAction>
      </div>
    </section>
  );
}
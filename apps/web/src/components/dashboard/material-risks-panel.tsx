import {
  AlertTriangle,
  Boxes,
  PackageSearch,
  Truck,
  type LucideIcon,
} from "lucide-react";

import Link from "next/link";

import type { CSSProperties } from "react";


import {
  canOpenDashboardTarget,
  DASHBOARD_MATERIAL_WORKSPACE_TARGET,
} from "./dashboard-navigation";
import { DashboardNavigationAction } from "./dashboard-navigation-action";

interface MaterialRisksPanelProps {
  risks: readonly unknown[];
  grantedPermissionKeys?: readonly string[];
}

type MaterialRiskLevel =
  | "critical"
  | "high"
  | "medium"
  | "low";

interface NormalizedMaterialRisk {
  id: string;
  materialCode: string;
  materialName: string;
  stockLabel: string;
  thresholdLabel: string;
  shortageLabel: string;
  riskLevel: MaterialRiskLevel;
  icon: LucideIcon;
}

const fallbackRisks: NormalizedMaterialRisk[] = [
  {
    id: "material-risk-1",
    materialCode: "PP-GF30-BLK",
    materialName: "PP GF30 Siyah",
    stockLabel: "1,8 gün",
    thresholdLabel: "3,0 gün",
    shortageLabel: "-420 kg",
    riskLevel: "critical",
    icon: AlertTriangle,
  },
  {
    id: "material-risk-2",
    materialCode: "ABS-NAT-01",
    materialName: "ABS Natural",
    stockLabel: "2,6 gün",
    thresholdLabel: "3,0 gün",
    shortageLabel: "-180 kg",
    riskLevel: "high",
    icon: Boxes,
  },
  {
    id: "material-risk-3",
    materialCode: "PA66-GF15",
    materialName: "PA66 GF15",
    stockLabel: "4,2 gün",
    thresholdLabel: "3,0 gün",
    shortageLabel: "+240 kg",
    riskLevel: "medium",
    icon: Boxes,
  },
  {
    id: "material-risk-4",
    materialCode: "HDPE-BLU-04",
    materialName: "HDPE Mavi",
    stockLabel: "7,1 gün",
    thresholdLabel: "3,0 gün",
    shortageLabel: "+680 kg",
    riskLevel: "low",
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
  wrapper: {
    minWidth: 0,
    minHeight: 0,
    flex: "1 1 auto",
    overflowX: "auto",
    overflowY: "hidden",
  },
  table: {
    width: "100%",
    minWidth: 560,
    borderCollapse: "collapse",
    tableLayout: "fixed",
  },
  headerCell: {
    padding: "7px 7px",
    borderBottom: "1px solid #dce5f0",
    background: "#f8fafc",
    color: "#75839a",
    fontSize: "0.39rem",
    fontWeight: 760,
    letterSpacing: "0.02em",
    textAlign: "left",
    whiteSpace: "nowrap",
  },
  bodyCell: {
    height: 44,
    padding: "7px 7px",
    overflow: "hidden",
    borderBottom: "1px solid #dce5f0",
    color: "#52627a",
    fontSize: "0.41rem",
    textAlign: "left",
    textOverflow: "ellipsis",
    verticalAlign: "middle",
    whiteSpace: "nowrap",
  },
  identity: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    gap: 7,
  },
  icon: {
    display: "grid",
    width: 24,
    height: 24,
    flex: "0 0 auto",
    placeItems: "center",
    borderRadius: 7,
  },
  identityText: {
    display: "grid",
    minWidth: 0,
    gap: 2,
  },
  code: {
    overflow: "hidden",
    color: "#10213d",
    fontSize: "0.43rem",
    fontWeight: 780,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  name: {
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.34rem",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  stock: {
    color: "#10213d",
    fontSize: "0.4rem",
    fontWeight: 760,
  },
  threshold: {
    color: "#52627a",
    fontSize: "0.39rem",
    fontWeight: 650,
  },
  shortage: {
    fontSize: "0.39rem",
    fontWeight: 760,
  },
  badge: {
    display: "inline-flex",
    alignItems: "center",
    gap: 4,
    padding: "4px 5px",
    borderRadius: 6,
    fontSize: "0.35rem",
    fontWeight: 750,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  dot: {
    width: 5,
    height: 5,
    borderRadius: "50%",
    background: "currentColor",
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
  footerInfo: {
    display: "inline-flex",
    minWidth: 0,
    alignItems: "center",
    gap: 5,
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  footerAction: {
    color: "#1769e0",
    fontWeight: 750,
    whiteSpace: "nowrap",
    border: 0,
    background: "transparent",
    font: "inherit",
    fontSize: "inherit",
    padding: 0,
    cursor: "pointer",
    textDecoration: "none",
  },
  rowLink: {
    display: "grid",
    minWidth: 0,
    gap: 2,
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

    if (typeof value === "number" && Number.isFinite(value)) {
      return new Intl.NumberFormat("tr-TR").format(value);
    }
  }

  return fallback;
}

function normalizeRiskLevel(
  source: Record<string, unknown>,
  fallback: MaterialRiskLevel,
): MaterialRiskLevel {
  const rawValue = readString(
    source,
    ["riskLevel", "severity", "priority", "status"],
    fallback,
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
    rawValue.includes("düşük") ||
    rawValue.includes("normal")
  ) {
    return "low";
  }

  return "medium";
}

function normalizeRisk(
  value: unknown,
  index: number,
): NormalizedMaterialRisk {
  const source = asRecord(value);
  const fallback =
    fallbackRisks[index] ??
    fallbackRisks[fallbackRisks.length - 1];

  const riskLevel = normalizeRiskLevel(
    source,
    fallback.riskLevel,
  );

  return {
    id: readString(
      source,
      ["id", "materialId", "materialCode", "code"],
      `material-risk-${index + 1}`,
    ),
    materialCode: readString(
      source,
      ["materialCode", "code", "sku"],
      fallback.materialCode,
    ),
    materialName: readString(
      source,
      ["materialName", "name", "description"],
      fallback.materialName,
    ),
    stockLabel: readString(
      source,
      [
        "coverageLabel",
        "stockLabel",
        "availableStockLabel",
        "currentStockLabel",
      ],
      fallback.stockLabel,
    ),
    thresholdLabel: readString(
      source,
      [
        "thresholdLabel",
        "criticalLevelLabel",
        "minimumStockLabel",
      ],
      fallback.thresholdLabel,
    ),
    shortageLabel: readString(
      source,
      [
        "expectedShortageLabel",
        "shortageLabel",
        "differenceLabel",
      ],
      fallback.shortageLabel,
    ),
    riskLevel,
    icon:
      riskLevel === "critical"
        ? AlertTriangle
        : Boxes,
  };
}

function getRiskLabel(level: MaterialRiskLevel) {
  switch (level) {
    case "critical":
      return "Kritik";
    case "high":
      return "Yüksek";
    case "medium":
      return "Dikkat";
    case "low":
      return "Normal";
  }
}

function getRiskStyle(
  level: MaterialRiskLevel,
): CSSProperties {
  switch (level) {
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

function getIconStyle(
  level: MaterialRiskLevel,
): CSSProperties {
  switch (level) {
    case "critical":
      return {
        background: "#ffecef",
        color: "#dc3545",
      };
    case "high":
      return {
        background: "#fff3df",
        color: "#dc7a00",
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

export function MaterialRisksPanel({
  risks,
  grantedPermissionKeys = [],
}: MaterialRisksPanelProps) {
  const sourceRisks =
    risks.length > 0 ? risks : fallbackRisks;

  const visibleRisks = sourceRisks
    .slice(0, 4)
    .map(normalizeRisk);

  const canOpenMaterials = canOpenDashboardTarget(
    grantedPermissionKeys,
    DASHBOARD_MATERIAL_WORKSPACE_TARGET,
  );

  return (
    <section
      aria-labelledby="material-risks-title"
      className="dashboard-panel dashboard-panel--material-risks"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Stok ve tedarik görünümü
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="material-risks-title"
          >
            Malzeme Riskleri
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <PackageSearch
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {visibleRisks.length} malzeme
        </div>
      </div>

      <div style={styles.wrapper}>
        <table aria-label="Malzeme Riskleri" style={styles.table}>
          <thead>
            <tr>
              <th
                style={{
                  ...styles.headerCell,
                  width: "38%",
                }}
              >
                Malzeme
              </th>

              <th
                style={{
                  ...styles.headerCell,
                  width: "18%",
                }}
              >
                Kapsama
              </th>

              <th
                style={{
                  ...styles.headerCell,
                  width: "18%",
                }}
              >
                Kritik Seviye
              </th>

              <th
                style={{
                  ...styles.headerCell,
                  width: "14%",
                }}
              >
                Fark
              </th>

              <th
                style={{
                  ...styles.headerCell,
                  width: "12%",
                }}
              >
                Risk
              </th>
            </tr>
          </thead>

          <tbody>
            {visibleRisks.map((risk, index) => {
              const Icon = risk.icon;
              const tone = getRiskStyle(risk.riskLevel);

              return (
                <tr key={risk.id}>
                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleRisks.length - 1
                          ? 0
                          : "1px solid #dce5f0",
                    }}
                  >
                    <div style={styles.identity}>
                      <span
                        aria-hidden="true"
                        style={{
                          ...styles.icon,
                          ...getIconStyle(risk.riskLevel),
                        }}
                      >
                        <Icon size={13} strokeWidth={1.8} />
                      </span>

                      <div style={styles.identityText}>
                        {canOpenMaterials ? (
                          <Link
                            aria-label={`${risk.materialName} malzeme riskini incele`}
                            href={DASHBOARD_MATERIAL_WORKSPACE_TARGET.href}
                            style={styles.rowLink}
                          >
                            <strong style={styles.code}>
                              {risk.materialCode}
                            </strong>
                            <span style={styles.name}>
                              {risk.materialName}
                            </span>
                          </Link>
                        ) : (
                          <>
                            <strong style={styles.code}>
                              {risk.materialCode}
                            </strong>
                            <span style={styles.name}>
                              {risk.materialName}
                            </span>
                          </>
                        )}
                      </div>
                    </div>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleRisks.length - 1
                          ? 0
                          : "1px solid #dce5f0",
                    }}
                  >
                    <span style={styles.stock}>
                      {risk.stockLabel}
                    </span>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleRisks.length - 1
                          ? 0
                          : "1px solid #dce5f0",
                    }}
                  >
                    <span style={styles.threshold}>
                      {risk.thresholdLabel}
                    </span>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleRisks.length - 1
                          ? 0
                          : "1px solid #dce5f0",
                    }}
                  >
                    <span
                      style={{
                        ...styles.shortage,
                        color:
                          risk.riskLevel === "critical" ||
                          risk.riskLevel === "high"
                            ? "#dc3545"
                            : "#08945f",
                      }}
                    >
                      {risk.shortageLabel}
                    </span>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleRisks.length - 1
                          ? 0
                          : "1px solid #dce5f0",
                    }}
                  >
                    <span
                      style={{
                        ...styles.badge,
                        ...tone,
                      }}
                    >
                      <span
                        aria-hidden="true"
                        style={styles.dot}
                      />

                      {getRiskLabel(risk.riskLevel)}
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <div style={styles.footer}>
        <span style={styles.footerInfo}>
          <Truck
            aria-hidden="true"
            size={12}
            strokeWidth={1.8}
          />
          ERP ve tedarik verisi önizlemesi
        </span>

        <DashboardNavigationAction
          ariaLabel="Malzeme risk analizini görüntüle"
          grantedPermissionKeys={grantedPermissionKeys}
          style={styles.footerAction}
          target={DASHBOARD_MATERIAL_WORKSPACE_TARGET}
        >
          Risk analizini görüntüle
        </DashboardNavigationAction>
      </div>
    </section>
  );
}
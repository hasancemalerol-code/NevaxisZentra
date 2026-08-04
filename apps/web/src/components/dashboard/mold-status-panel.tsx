import {
  CircleGauge,
  Factory,
  Wrench,
} from "lucide-react";

import type { CSSProperties } from "react";

import type {
  MoldStatusItem,
  RiskLevel,
} from "@/lib/mock-data/dashboard";

interface MoldStatusPanelProps {
  molds: MoldStatusItem[];
}

const styles: Record<string, CSSProperties> = {
  panel: {
    display: "flex",
    minWidth: 0,
    flexDirection: "column",
    overflow: "hidden",
  },
  body: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    flex: "1 1 auto",
    overflow: "hidden",
  },
  overview: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "92px minmax(0, 1fr)",
    alignItems: "center",
    gap: 10,
    padding: "9px 10px 8px",
    borderBottom: "1px solid #dce5f0",
  },
  chart: {
    display: "grid",
    placeItems: "center",
  },
  donut: {
    position: "relative",
    display: "grid",
    width: 78,
    height: 78,
    placeItems: "center",
    borderRadius: "50%",
    background:
      "conic-gradient(#08945f 0deg 150deg, #dc7a00 150deg 255deg, #7c3aed 255deg 360deg)",
  },
  donutHole: {
    position: "absolute",
    inset: 14,
    border: "1px solid #dce5f0",
    borderRadius: "50%",
    background: "#ffffff",
  },
  donutCenter: {
    position: "relative",
    zIndex: 1,
    display: "grid",
    placeItems: "center",
    textAlign: "center",
  },
  donutLabel: {
    color: "#75839a",
    fontSize: "0.35rem",
    fontWeight: 650,
  },
  donutValue: {
    marginTop: 2,
    color: "#10213d",
    fontSize: "0.88rem",
    fontWeight: 840,
    lineHeight: 1,
  },
  legend: {
    display: "grid",
    minWidth: 0,
    gap: 7,
  },
  legendRow: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "7px minmax(0, 1fr) auto",
    alignItems: "center",
    gap: 6,
  },
  legendDot: {
    width: 7,
    height: 7,
    borderRadius: 2,
  },
  legendLabel: {
    overflow: "hidden",
    color: "#52627a",
    fontSize: "0.41rem",
    fontWeight: 650,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  legendValue: {
    color: "#10213d",
    fontSize: "0.43rem",
    fontWeight: 800,
  },
  list: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    flex: "1 1 auto",
    padding: "2px 10px 4px",
    overflow: "hidden",
  },
  row: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns:
      "minmax(0, 1.3fr) 48px minmax(45px, 0.7fr) auto",
    alignItems: "center",
    gap: 7,
    padding: "6px 0",
    borderBottom: "1px solid #dce5f0",
  },
  identity: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    gap: 7,
  },
  icon: {
    display: "grid",
    width: 22,
    height: 22,
    flex: "0 0 auto",
    placeItems: "center",
    borderRadius: 6,
    background: "#f2ebff",
    color: "#7c3aed",
  },
  identityText: {
    display: "grid",
    minWidth: 0,
    gap: 2,
  },
  code: {
    overflow: "hidden",
    color: "#10213d",
    fontSize: "0.41rem",
    fontWeight: 780,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  family: {
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.33rem",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  percentage: {
    color: "#10213d",
    fontSize: "0.4rem",
    fontWeight: 780,
    textAlign: "right",
  },
  track: {
    overflow: "hidden",
    height: 4,
    borderRadius: 999,
    background: "#dce5f0",
  },
  bar: {
    display: "block",
    height: "100%",
    borderRadius: 999,
    background: "#7c3aed",
  },
  badge: {
    display: "inline-flex",
    alignItems: "center",
    justifyContent: "center",
    padding: "4px 5px",
    borderRadius: 6,
    fontSize: "0.34rem",
    fontWeight: 750,
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
    padding: "0 10px",
    borderTop: "1px solid #dce5f0",
    color: "#75839a",
    fontSize: "0.36rem",
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
  },
};

function formatQuantity(value: number) {
  return new Intl.NumberFormat("tr-TR").format(value);
}

function getRiskLabel(risk: RiskLevel) {
  switch (risk) {
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

function getRiskStyle(risk: RiskLevel): CSSProperties {
  switch (risk) {
    case "critical":
    case "high":
      return {
        background: "#ffecef",
        color: "#dc3545",
      };
    case "medium":
      return {
        background: "#fff3df",
        color: "#dc7a00",
      };
    case "low":
      return {
        background: "#e8f8f1",
        color: "#08945f",
      };
  }
}

function calculateMaintenanceProgress(mold: MoldStatusItem) {
  if (mold.maintenanceThreshold <= 0) {
    return 0;
  }

  return Math.min(
    100,
    Math.round(
      (mold.cycleCount / mold.maintenanceThreshold) * 100,
    ),
  );
}

export function MoldStatusPanel({
  molds,
}: MoldStatusPanelProps) {
  const visibleMolds = molds.slice(0, 4);

  const productionCount = visibleMolds.filter(
    (mold) => mold.riskLevel === "low",
  ).length;

  const maintenanceCount = visibleMolds.filter(
    (mold) =>
      mold.riskLevel === "critical" ||
      mold.riskLevel === "high",
  ).length;

  const waitingCount = Math.max(
    visibleMolds.length -
      productionCount -
      maintenanceCount,
    0,
  );

  const totalCycles = visibleMolds.reduce(
    (total, mold) => total + mold.cycleCount,
    0,
  );

  return (
    <section
      aria-labelledby="mold-status-title"
      className="dashboard-panel dashboard-panel--mold-status"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Kalıp yaşam döngüsü
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="mold-status-title"
          >
            Kalıp Durumu
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <Wrench
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {visibleMolds.length} kalıp
        </div>
      </div>

      <div style={styles.body}>
        <div style={styles.overview}>
          <div style={styles.chart}>
            <div
              aria-label={`${visibleMolds.length} kalıbın durum dağılımı`}
              role="img"
              style={styles.donut}
            >
              <span
                aria-hidden="true"
                style={styles.donutHole}
              />

              <div style={styles.donutCenter}>
                <span style={styles.donutLabel}>
                  Toplam
                </span>

                <strong style={styles.donutValue}>
                  {visibleMolds.length}
                </strong>
              </div>
            </div>
          </div>

          <div style={styles.legend}>
            <div style={styles.legendRow}>
              <span
                aria-hidden="true"
                style={{
                  ...styles.legendDot,
                  background: "#08945f",
                }}
              />
              <span style={styles.legendLabel}>
                Üretimde
              </span>
              <strong style={styles.legendValue}>
                {productionCount}
              </strong>
            </div>

            <div style={styles.legendRow}>
              <span
                aria-hidden="true"
                style={{
                  ...styles.legendDot,
                  background: "#dc7a00",
                }}
              />
              <span style={styles.legendLabel}>
                Bakımda
              </span>
              <strong style={styles.legendValue}>
                {maintenanceCount}
              </strong>
            </div>

            <div style={styles.legendRow}>
              <span
                aria-hidden="true"
                style={{
                  ...styles.legendDot,
                  background: "#7c3aed",
                }}
              />
              <span style={styles.legendLabel}>
                Beklemede
              </span>
              <strong style={styles.legendValue}>
                {waitingCount}
              </strong>
            </div>
          </div>
        </div>

        <div style={styles.list}>
          {visibleMolds.map((mold, index) => {
            const progress =
              calculateMaintenanceProgress(mold);

            return (
              <article
                key={mold.id}
                style={{
                  ...styles.row,
                  borderBottom:
                    index === visibleMolds.length - 1
                      ? 0
                      : styles.row.borderBottom,
                }}
              >
                <div style={styles.identity}>
                  <span
                    aria-hidden="true"
                    style={styles.icon}
                  >
                    <Factory
                      size={12}
                      strokeWidth={1.8}
                    />
                  </span>

                  <div style={styles.identityText}>
                    <strong style={styles.code}>
                      {mold.moldCode}
                    </strong>

                    <span style={styles.family}>
                      {mold.productFamily}
                    </span>
                  </div>
                </div>

                <strong style={styles.percentage}>
                  %{progress}
                </strong>

                <div
                  aria-label={`${mold.moldCode} bakım eşiği yüzde ${progress}`}
                  aria-valuemax={100}
                  aria-valuemin={0}
                  aria-valuenow={progress}
                  role="progressbar"
                  style={styles.track}
                >
                  <span
                    style={{
                      ...styles.bar,
                      width: `${progress}%`,
                    }}
                  />
                </div>

                <span
                  style={{
                    ...styles.badge,
                    ...getRiskStyle(mold.riskLevel),
                  }}
                >
                  {getRiskLabel(mold.riskLevel)}
                </span>
              </article>
            );
          })}
        </div>
      </div>

      <div style={styles.footer}>
        <span style={styles.footerInfo}>
          <CircleGauge
            aria-hidden="true"
            size={12}
            strokeWidth={1.8}
          />
          Toplam çevrim: {formatQuantity(totalCycles)}
        </span>

        <span style={styles.footerAction}>
          Tüm kalıpları görüntüle
        </span>
      </div>
    </section>
  );
}
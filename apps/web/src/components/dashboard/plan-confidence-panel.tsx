import {
  AlertTriangle,
  CheckCircle2,
  Clock3,
  ShieldCheck,
} from "lucide-react";

import type { CSSProperties } from "react";
import Link from "next/link";

import { hasDashboardPermission } from "@/components/dashboard/dashboard-permission";
import type {
  PlanConfidenceData,
} from "@/lib/mock-data/dashboard";

interface PlanConfidencePanelProps {
  data: PlanConfidenceData;
  grantedPermissionKeys?: readonly string[];
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
    gridTemplateColumns: "118px minmax(0, 1fr)",
    gridTemplateRows: "1fr auto",
    alignItems: "center",
    gap: "10px 12px",
    flex: "1 1 auto",
    padding: "12px 12px 9px",
  },
  visual: {
    display: "grid",
    width: 118,
    minWidth: 118,
    placeItems: "center",
  },
  donut: {
    position: "relative",
    display: "grid",
    width: 112,
    height: 112,
    minWidth: 112,
    minHeight: 112,
    placeItems: "center",
    borderRadius: "50%",
    boxShadow: "0 7px 18px rgb(15 23 42 / 7%)",
  },
  donutHole: {
    position: "absolute",
    inset: 18,
    zIndex: 1,
    display: "block",
    border: "1px solid #dce5f0",
    borderRadius: "50%",
    background: "#ffffff",
  },
  center: {
    position: "relative",
    zIndex: 2,
    display: "grid",
    placeItems: "center",
    textAlign: "center",
  },
  score: {
    margin: 0,
    color: "#10213d",
    fontSize: "1.05rem",
    fontWeight: 850,
    lineHeight: 1,
    letterSpacing: "-0.05em",
  },
  scoreLabel: {
    display: "block",
    maxWidth: 72,
    marginTop: 4,
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.4rem",
    fontWeight: 680,
    lineHeight: 1.15,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  details: {
    display: "grid",
    minWidth: 0,
    gap: 7,
  },
  detailRow: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "8px minmax(0, 1fr) auto",
    alignItems: "center",
    gap: 7,
  },
  dot: {
    display: "block",
    width: 8,
    height: 8,
    borderRadius: 2,
  },
  detailLabel: {
    overflow: "hidden",
    color: "#52627a",
    fontSize: "0.45rem",
    fontWeight: 680,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  detailValue: {
    color: "#10213d",
    fontSize: "0.45rem",
    fontWeight: 820,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  factors: {
    display: "grid",
    minWidth: 0,
    gap: 4,
    marginTop: 2,
    paddingTop: 7,
    borderTop: "1px solid #dce5f0",
  },
  factor: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    justifyContent: "space-between",
    gap: 7,
    color: "#75839a",
    fontSize: "0.38rem",
    lineHeight: 1.2,
  },
  factorLabel: {
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  factorValue: {
    flex: "0 0 auto",
    fontWeight: 720,
    whiteSpace: "nowrap",
  },
  footer: {
    display: "flex",
    minWidth: 0,
    gridColumn: "1 / -1",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8,
    paddingTop: 8,
    borderTop: "1px solid #dce5f0",
  },
  footerItem: {
    display: "inline-flex",
    minWidth: 0,
    alignItems: "center",
    gap: 4,
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.38rem",
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  footerPositive: {
    display: "inline-flex",
    minWidth: 0,
    alignItems: "center",
    gap: 4,
    overflow: "hidden",
    color: "#08945f",
    fontSize: "0.38rem",
    fontWeight: 720,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  action: {
    display: "flex",
    minHeight: 30,
    flex: "0 0 30px",
    alignItems: "center",
    justifyContent: "center",
    padding: "0 10px",
    border: 0,
    borderTop: "1px solid #dce5f0",
    background: "transparent",
    color: "#1769e0",
    font: "inherit",
    fontSize: "0.45rem",
    fontWeight: 750,
    cursor: "not-allowed",
    opacity: 0.9,
  },
};

export function PlanConfidencePanel({
  data,
  grantedPermissionKeys = [],
}: PlanConfidencePanelProps) {
  const canOpenPlanning = hasDashboardPermission(
    grantedPermissionKeys,
    "production.planning.read",
  );
  const normalizedScore = Math.max(
    0,
    Math.min(100, data.score),
  );

  const remainingScore = 100 - normalizedScore;

  const donutBackground = `conic-gradient(
    #119c68 0% ${normalizedScore}%,
    #e7edf5 ${normalizedScore}% 100%
  )`;

  const positiveFactorCount = data.factors.filter(
    (factor) => factor.impact === "positive",
  ).length;

  const negativeFactorCount = data.factors.filter(
    (factor) => factor.impact === "negative",
  ).length;

  const visibleFactors = data.factors.slice(0, 3);

  return (
    <section
      aria-labelledby="plan-confidence-title"
      className="dashboard-panel dashboard-panel--plan-confidence"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Üretim planı değerlendirmesi
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="plan-confidence-title"
          >
            Plan Güvenilirliği
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <ShieldCheck
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {data.humanReviewRequired
            ? "İnsan denetimli"
            : "Otomatik onaylı"}
        </div>
      </div>

      <div
        className="locked-plan-confidence"
        style={styles.body}
      >
        <div style={styles.visual}>
          <div
            aria-label={`Genel plan güveni yüzde ${normalizedScore}`}
            role="img"
            style={{
              ...styles.donut,
              background: donutBackground,
            }}
          >
            <span
              aria-hidden="true"
              style={styles.donutHole}
            />

            <div style={styles.center}>
              <strong style={styles.score}>
                %{normalizedScore}
              </strong>

              <span style={styles.scoreLabel}>
                {data.classification}
              </span>
            </div>
          </div>
        </div>

        <div style={styles.details}>
          <div style={styles.detailRow}>
            <span
              aria-hidden="true"
              style={{
                ...styles.dot,
                background: "#119c68",
              }}
            />

            <span style={styles.detailLabel}>
              Güven skoru
            </span>

            <strong style={styles.detailValue}>
              %{normalizedScore}
            </strong>
          </div>

          <div style={styles.detailRow}>
            <span
              aria-hidden="true"
              style={{
                ...styles.dot,
                background: "#e7edf5",
              }}
            />

            <span style={styles.detailLabel}>
              Kalan belirsizlik
            </span>

            <strong style={styles.detailValue}>
              %{remainingScore}
            </strong>
          </div>

          <div style={styles.detailRow}>
            <span
              aria-hidden="true"
              style={{
                ...styles.dot,
                background: "#1769e0",
              }}
            />

            <span style={styles.detailLabel}>
              Pozitif faktör
            </span>

            <strong style={styles.detailValue}>
              {positiveFactorCount}
            </strong>
          </div>

          <div style={styles.detailRow}>
            <span
              aria-hidden="true"
              style={{
                ...styles.dot,
                background: "#ef4444",
              }}
            />

            <span style={styles.detailLabel}>
              Negatif faktör
            </span>

            <strong style={styles.detailValue}>
              {negativeFactorCount}
            </strong>
          </div>

          <div style={styles.factors}>
            {visibleFactors.map((factor) => (
              <div key={factor.id} style={styles.factor}>
                <span style={styles.factorLabel}>
                  {factor.label}
                </span>

                <span
                  style={{
                    ...styles.factorValue,
                    color:
                      factor.impact === "positive"
                        ? "#08945f"
                        : "#dc3545",
                  }}
                >
                  {factor.value}
                </span>
              </div>
            ))}
          </div>
        </div>

        <div style={styles.footer}>
          <span style={styles.footerPositive}>
            <CheckCircle2
              aria-hidden="true"
              size={12}
              strokeWidth={1.8}
            />
            {data.classification}
          </span>

          <span style={styles.footerItem}>
            {data.unresolvedConstraintCount > 0 ? (
              <AlertTriangle
                aria-hidden="true"
                size={12}
                strokeWidth={1.8}
              />
            ) : (
              <Clock3
                aria-hidden="true"
                size={12}
                strokeWidth={1.8}
              />
            )}

            {data.unresolvedConstraintCount} açık kısıt
          </span>
        </div>
      </div>

      {canOpenPlanning ? (
        <Link
          href="/planlama"
          style={{
            ...styles.action,
            cursor: "pointer",
            textDecoration: "none",
          }}
        >
          {data.expectedCompletionLabel}
        </Link>
      ) : (
        <button
          disabled
          style={styles.action}
          title="Üretim planlarını görüntüleme yetkiniz yok."
          type="button"
        >
          {data.expectedCompletionLabel}
        </button>
      )}
    </section>
  );
}
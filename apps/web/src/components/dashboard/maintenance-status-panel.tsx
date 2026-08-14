import {
  AlertTriangle,
  CalendarClock,
  Clock3,
  Wrench,
} from "lucide-react";

import type { CSSProperties } from "react";
import Link from "next/link";

import { hasDashboardPermission } from "@/components/dashboard/dashboard-permission";
import type {
  MaintenanceStatusItem,
  OperationalStatus,
} from "@/lib/mock-data/dashboard";

interface MaintenanceStatusPanelProps {
  grantedPermissionKeys?: readonly string[];
  items: MaintenanceStatusItem[];
}

const previewMaintenanceItems: MaintenanceStatusItem[] = [
  {
    id: "maintenance-preview-1",
    type: "active",
    typeLabel: "Aktif müdahale",
    status: "critical",
    assetCode: "M-05",
    title: "Hidrolik yağ sıcaklığı yüksek",
    scheduledLabel: "Aktif müdahale",
    owner: "Teknik bakım",
  },
  {
    id: "maintenance-preview-2",
    type: "unplanned",
    typeLabel: "Plansız bakım",
    status: "attention",
    assetCode: "M-03",
    title: "Enjeksiyon basıncı kontrolü",
    scheduledLabel: "Bugün 23:00",
    owner: "Bakım ekibi",
  },
  {
    id: "maintenance-preview-3",
    type: "planned",
    typeLabel: "Planlı bakım",
    status: "healthy",
    assetCode: "KLP-204",
    title: "Kalıp periyodik bakımı",
    scheduledLabel: "Yarın 09:30",
    owner: "Kalıp bakım",
  },
  {
    id: "maintenance-preview-4",
    type: "overdue",
    typeLabel: "Gecikmiş bakım",
    status: "pending",
    assetCode: "M-07",
    title: "Hava hattı filtre değişimi",
    scheduledLabel: "2 saat gecikmiş",
    owner: "Mekanik ekip",
  },
];

const styles: Record<string, CSSProperties> = {
  panel: {
    display: "flex",
    minWidth: 0,
    flexDirection: "column",
    overflow: "hidden",
  },
  summary: {
    display: "grid",
    gridTemplateColumns: "repeat(3, minmax(0, 1fr))",
    gap: 7,
    padding: "9px 10px",
    borderBottom: "1px solid #dce5f0",
  },
  summaryItem: {
    minWidth: 0,
    padding: "7px 8px",
    border: "1px solid #dce5f0",
    borderRadius: 8,
    background: "#f8fafc",
  },
  summaryLabel: {
    display: "block",
    overflow: "hidden",
    color: "#75839a",
    fontSize: 10,
    fontWeight: 650,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  summaryValue: {
    display: "block",
    marginTop: 4,
    color: "#10213d",
    fontSize: 16,
    fontWeight: 820,
    lineHeight: 1,
  },
  list: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    flex: "1 1 auto",
    padding: "2px 11px 4px",
    overflow: "hidden",
  },
  row: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "30px minmax(0, 1fr) auto",
    alignItems: "center",
    gap: 9,
    padding: "9px 0",
    borderBottom: "1px solid #dce5f0",
  },
  icon: {
    display: "grid",
    width: 30,
    height: 30,
    placeItems: "center",
    borderRadius: 8,
  },
  content: {
    minWidth: 0,
  },
  asset: {
    margin: 0,
    color: "#75839a",
    fontSize: 10,
    fontWeight: 720,
    lineHeight: 1.1,
  },
  title: {
    overflow: "hidden",
    margin: "3px 0 0",
    color: "#10213d",
    fontSize: 12,
    fontWeight: 780,
    lineHeight: 1.25,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  meta: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    gap: 12,
    marginTop: 6,
    color: "#75839a",
    fontSize: 10,
    lineHeight: 1.2,
  },
  metaItem: {
    display: "inline-flex",
    minWidth: 0,
    alignItems: "center",
    gap: 4,
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  side: {
    display: "grid",
    minWidth: 70,
    justifyItems: "end",
    gap: 7,
  },
  status: {
    display: "inline-flex",
    alignItems: "center",
    justifyContent: "center",
    padding: "5px 7px",
    borderRadius: 7,
    fontSize: 10,
    fontWeight: 760,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  type: {
    overflow: "hidden",
    color: "#75839a",
    fontSize: 9,
    lineHeight: 1.15,
    textAlign: "right",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  action: {
    display: "flex",
    minHeight: 32,
    flex: "0 0 32px",
    alignItems: "center",
    justifyContent: "center",
    border: 0,
    borderTop: "1px solid #dce5f0",
    background: "transparent",
    color: "#1769e0",
    font: "inherit",
    fontSize: 10,
    fontWeight: 750,
    cursor: "not-allowed",
  },
};

function getStatusLabel(status: OperationalStatus) {
  switch (status) {
    case "healthy":
      return "Planlı";
    case "attention":
      return "Dikkat";
    case "critical":
      return "Kritik";
    case "offline":
      return "Devre dışı";
    case "pending":
      return "Bekliyor";
  }
}

function getStatusTone(status: OperationalStatus) {
  switch (status) {
    case "healthy":
      return {
        accent: "#08945f",
        soft: "#e8f8f1",
      };
    case "attention":
    case "pending":
      return {
        accent: "#dc7a00",
        soft: "#fff3df",
      };
    case "critical":
    case "offline":
      return {
        accent: "#dc3545",
        soft: "#ffecef",
      };
  }
}

export function MaintenanceStatusPanel({
  grantedPermissionKeys = [],
  items,
}: MaintenanceStatusPanelProps) {
  const canOpenMaintenance = hasDashboardPermission(
    grantedPermissionKeys,
    "maintenance.read",
  );
  const sourceItems =
    items.length > 0 ? items : previewMaintenanceItems;

  const visibleItems = sourceItems.slice(0, 4);

  const plannedCount = visibleItems.filter(
    (item) => item.type === "planned",
  ).length;

  const activeCount = visibleItems.filter(
    (item) => item.type === "active",
  ).length;

  const urgentCount = visibleItems.filter(
    (item) =>
      item.status === "critical" ||
      item.type === "unplanned" ||
      item.type === "overdue",
  ).length;

  return (
    <section
      aria-labelledby="maintenance-status-title"
      className="dashboard-panel dashboard-panel--maintenance"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Teknik bakım görünümü
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="maintenance-status-title"
          >
            Bakım &amp; Arıza
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <Wrench
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {urgentCount} öncelikli
        </div>
      </div>

      <div style={styles.summary}>
        <div style={styles.summaryItem}>
          <span style={styles.summaryLabel}>
            Planlı bakım
          </span>
          <strong style={styles.summaryValue}>
            {plannedCount}
          </strong>
        </div>

        <div style={styles.summaryItem}>
          <span style={styles.summaryLabel}>
            Aktif müdahale
          </span>
          <strong style={styles.summaryValue}>
            {activeCount}
          </strong>
        </div>

        <div style={styles.summaryItem}>
          <span style={styles.summaryLabel}>
            Kritik durum
          </span>
          <strong style={styles.summaryValue}>
            {urgentCount}
          </strong>
        </div>
      </div>

      <div style={styles.list}>
        {visibleItems.map((item, index) => {
          const tone = getStatusTone(item.status);

          return (
            <article
              key={item.id}
              style={{
                ...styles.row,
                borderBottom:
                  index === visibleItems.length - 1
                    ? 0
                    : styles.row.borderBottom,
              }}
            >
              <span
                aria-hidden="true"
                style={{
                  ...styles.icon,
                  background: tone.soft,
                  color: tone.accent,
                }}
              >
                {item.type === "planned" ? (
                  <CalendarClock size={16} strokeWidth={1.8} />
                ) : item.status === "critical" ? (
                  <AlertTriangle size={16} strokeWidth={1.8} />
                ) : (
                  <Wrench size={16} strokeWidth={1.8} />
                )}
              </span>

              <div style={styles.content}>
                <p style={styles.asset}>{item.assetCode}</p>

                <h3 style={styles.title}>{item.title}</h3>

                <div style={styles.meta}>
                  <span style={styles.metaItem}>
                    <Clock3
                      aria-hidden="true"
                      size={12}
                      strokeWidth={1.8}
                    />
                    {item.scheduledLabel}
                  </span>

                  <span style={styles.metaItem}>
                    {item.owner}
                  </span>
                </div>
              </div>

              <div style={styles.side}>
                <span
                  style={{
                    ...styles.status,
                    background: tone.soft,
                    color: tone.accent,
                  }}
                >
                  {getStatusLabel(item.status)}
                </span>

                <span style={styles.type}>
                  {item.typeLabel}
                </span>
              </div>
            </article>
          );
        })}
      </div>

      {canOpenMaintenance ? (
        <Link
          href="/bakim"
          style={{
            ...styles.action,
            cursor: "pointer",
            textDecoration: "none",
          }}
        >
          Bakım planını görüntüle
        </Link>
      ) : null}
    </section>
  );
}
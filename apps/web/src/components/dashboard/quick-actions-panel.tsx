import {
  Boxes,
  CalendarPlus,
  ClipboardPlus,
  FilePlus2,
  PackagePlus,
  ShoppingCart,
  Sparkles,
  UserRoundPlus,
  Wrench,
  type LucideIcon,
} from "lucide-react";

import type { CSSProperties } from "react";

interface QuickActionDefinition {
  id: string;
  label: string;
  description: string;
  icon: LucideIcon;
  accent: string;
  soft: string;
}

const quickActions: QuickActionDefinition[] = [
  {
    id: "production-order",
    label: "Üretim Emri",
    description: "Yeni üretim emri oluştur",
    icon: ClipboardPlus,
    accent: "#1769e0",
    soft: "#eaf2ff",
  },
  {
    id: "sales-order",
    label: "Satış Siparişi",
    description: "Müşteri siparişi aç",
    icon: ShoppingCart,
    accent: "#08945f",
    soft: "#e8f8f1",
  },
  {
    id: "purchase-request",
    label: "Satın Alma",
    description: "Satın alma talebi oluştur",
    icon: PackagePlus,
    accent: "#dc7a00",
    soft: "#fff3df",
  },
  {
    id: "maintenance-request",
    label: "Bakım Talebi",
    description: "Makine veya kalıp kaydı aç",
    icon: Wrench,
    accent: "#7c3aed",
    soft: "#f2ebff",
  },
  {
    id: "stock-movement",
    label: "Stok Hareketi",
    description: "Malzeme giriş veya çıkışı yap",
    icon: Boxes,
    accent: "#536176",
    soft: "#eef2f6",
  },
  {
    id: "customer",
    label: "Yeni Müşteri",
    description: "CRM müşteri kartı oluştur",
    icon: UserRoundPlus,
    accent: "#1769e0",
    soft: "#eaf2ff",
  },
  {
    id: "quality-record",
    label: "Kalite Kaydı",
    description: "Kontrol veya uygunsuzluk aç",
    icon: FilePlus2,
    accent: "#08945f",
    soft: "#e8f8f1",
  },
  {
    id: "planning",
    label: "Planlama",
    description: "Yeni planlama görevi oluştur",
    icon: CalendarPlus,
    accent: "#dc7a00",
    soft: "#fff3df",
  },
];

const styles: Record<string, CSSProperties> = {
  panel: {
    display: "flex",
    minWidth: 0,
    flexDirection: "column",
    overflow: "hidden",
  },
  grid: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
    gridTemplateRows: "repeat(4, minmax(0, 1fr))",
    gap: 7,
    flex: "1 1 auto",
    padding: 9,
    overflow: "hidden",
  },
  button: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    gridTemplateColumns: "28px minmax(0, 1fr)",
    alignItems: "center",
    gap: 7,
    padding: 7,
    overflow: "hidden",
    border: "1px solid #dce5f0",
    borderRadius: 8,
    background: "#f8fafc",
    color: "#10213d",
    font: "inherit",
    textAlign: "left",
    cursor: "not-allowed",
    opacity: 1,
  },
  icon: {
    display: "grid",
    width: 28,
    height: 28,
    placeItems: "center",
    borderRadius: 7,
  },
  content: {
    display: "grid",
    minWidth: 0,
    gap: 2,
  },
  label: {
    display: "block",
    overflow: "hidden",
    color: "#10213d",
    fontSize: "0.43rem",
    fontWeight: 780,
    lineHeight: 1.15,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  description: {
    display: "block",
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.34rem",
    fontWeight: 560,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
};

export function QuickActionsPanel() {
  return (
    <section
      aria-labelledby="quick-actions-title"
      className="dashboard-panel dashboard-panel--quick-actions"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Operasyon kısayolları
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="quick-actions-title"
          >
            Hızlı İşlemler
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <Sparkles
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          8 işlem
        </div>
      </div>

      <div
        className="locked-quick-actions"
        style={styles.grid}
      >
        {quickActions.map((action) => {
          const Icon = action.icon;

          return (
            <button
              className="locked-quick-action"
              disabled
              key={action.id}
              style={styles.button}
              title={`${action.label} işlemi sonraki geliştirme paketlerinde etkinleştirilecektir.`}
              type="button"
            >
              <span
                aria-hidden="true"
                style={{
                  ...styles.icon,
                  background: action.soft,
                  color: action.accent,
                }}
              >
                <Icon size={14} strokeWidth={1.8} />
              </span>

              <span style={styles.content}>
                <strong style={styles.label}>
                  {action.label}
                </strong>

                <small style={styles.description}>
                  {action.description}
                </small>
              </span>
            </button>
          );
        })}
      </div>
    </section>
  );
}
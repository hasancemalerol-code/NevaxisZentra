import {
  Activity,
  CheckCircle2,
  Cloud,
  Database,
  RadioTower,
  Server,
  ShieldCheck,
  Wifi,
  type LucideIcon,
} from "lucide-react";

import type { CSSProperties } from "react";


type HealthStatus = "healthy" | "attention";

interface HealthItem {
  id: string;
  label: string;
  detail: string;
  value: string;
  icon: LucideIcon;
  status: HealthStatus;
}

const healthItems: HealthItem[] = [
  {
    id: "cloud",
    label: "Bulut Servisleri",
    detail: "Tüm servisler çalışıyor",
    value: "%99,98",
    icon: Cloud,
    status: "healthy",
  },
  {
    id: "database",
    label: "Veritabanı",
    detail: "Son eşitleme 1 dk önce",
    value: "Aktif",
    icon: Database,
    status: "healthy",
  },
  {
    id: "iot",
    label: "IoT Bağlantıları",
    detail: "12 cihazdan 11'i çevrimiçi",
    value: "11/12",
    icon: RadioTower,
    status: "attention",
  },
  {
    id: "network",
    label: "Fabrika Ağı",
    detail: "Gecikme normal seviyede",
    value: "18 ms",
    icon: Wifi,
    status: "healthy",
  },
  {
    id: "security",
    label: "Güvenlik",
    detail: "Kritik güvenlik olayı yok",
    value: "Güvenli",
    icon: ShieldCheck,
    status: "healthy",
  },
  {
    id: "api",
    label: "API Servisleri",
    detail: "Tüm uç noktalar erişilebilir",
    value: "24/24",
    icon: Server,
    status: "healthy",
  },
];

const styles: Record<string, CSSProperties> = {
  panel: {
    display: "flex",
    minWidth: 0,
    flexDirection: "column",
    overflow: "hidden",
  },
  heading: {
    display: "flex",
    minHeight: 46,
    flex: "0 0 46px",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12,
    padding: "8px 11px",
    borderBottom: "1px solid #dce5f0",
  },
  titleArea: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    gap: 8,
  },
  titleIcon: {
    display: "grid",
    width: 27,
    height: 27,
    flex: "0 0 auto",
    placeItems: "center",
    borderRadius: 8,
    background: "#e8f8f1",
    color: "#08945f",
  },
  eyebrow: {
    margin: "0 0 2px",
    color: "#75839a",
    fontSize: "0.36rem",
    fontWeight: 650,
    lineHeight: 1.1,
  },
  title: {
    margin: 0,
    color: "#10213d",
    fontSize: "0.55rem",
    fontWeight: 800,
    lineHeight: 1.2,
  },
  summary: {
    display: "inline-flex",
    flex: "0 0 auto",
    alignItems: "center",
    gap: 5,
    padding: "5px 7px",
    borderRadius: 7,
    background: "#e8f8f1",
    color: "#08945f",
    fontSize: "0.4rem",
    fontWeight: 760,
    whiteSpace: "nowrap",
  },
  grid: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "repeat(6, minmax(0, 1fr))",
    flex: "1 1 auto",
  },
  item: {
    display: "grid",
    minWidth: 0,
    gridTemplateColumns: "25px minmax(0, 1fr) auto",
    alignItems: "center",
    gap: 7,
    minHeight: 54,
    padding: "8px 9px",
    overflow: "hidden",
    borderRight: "1px solid #dce5f0",
    background: "#ffffff",
  },
  icon: {
    display: "grid",
    width: 25,
    height: 25,
    placeItems: "center",
    borderRadius: 7,
  },
  content: {
    display: "grid",
    minWidth: 0,
    gap: 2,
  },
  label: {
    overflow: "hidden",
    color: "#10213d",
    fontSize: "0.42rem",
    fontWeight: 760,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  detail: {
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.33rem",
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  valueArea: {
    display: "flex",
    flex: "0 0 auto",
    alignItems: "center",
    gap: 4,
  },
  dot: {
    width: 5,
    height: 5,
    borderRadius: "50%",
  },
  value: {
    fontSize: "0.37rem",
    fontWeight: 800,
    whiteSpace: "nowrap",
  },
};

function getTone(status: HealthStatus) {
  if (status === "attention") {
    return {
      accent: "#dc7a00",
      soft: "#fff3df",
    };
  }

  return {
    accent: "#08945f",
    soft: "#e8f8f1",
  };
}

export function SystemHealthPanel() {
  const healthyCount = healthItems.filter(
    (item) => item.status === "healthy",
  ).length;

  return (
    <section
      aria-labelledby="system-health-title"
      className="dashboard-panel dashboard-panel--system-health"
      style={styles.panel}
    >
      <div style={styles.heading}>
        <div style={styles.titleArea}>
          <span aria-hidden="true" style={styles.titleIcon}>
            <Activity size={15} strokeWidth={1.8} />
          </span>

          <div>
            <p style={styles.eyebrow}>
              Sistem altyapısı
            </p>

            <h2
              id="system-health-title"
              style={styles.title}
            >
              Sistem Sağlığı
            </h2>
          </div>
        </div>

        <div style={styles.summary}>
          <CheckCircle2
            aria-hidden="true"
            size={13}
            strokeWidth={1.9}
          />

          <span>
            {healthyCount}/{healthItems.length} sistem sağlıklı
          </span>
        </div>
      </div>

      <div style={styles.grid}>
        {healthItems.map((item, index) => {
          const Icon = item.icon;
          const tone = getTone(item.status);

          return (
            <article
              key={item.id}
              style={{
                ...styles.item,
                borderRight:
                  index === healthItems.length - 1
                    ? 0
                    : "1px solid #dce5f0",
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
                <Icon size={14} strokeWidth={1.8} />
              </span>

              <div style={styles.content}>
                <strong style={styles.label}>
                  {item.label}
                </strong>

                <span style={styles.detail}>
                  {item.detail}
                </span>
              </div>

              <div style={styles.valueArea}>
                <span
                  aria-hidden="true"
                  style={{
                    ...styles.dot,
                    background: tone.accent,
                    boxShadow: `0 0 0 3px ${tone.soft}`,
                  }}
                />

                <strong
                  style={{
                    ...styles.value,
                    color: tone.accent,
                  }}
                >
                  {item.value}
                </strong>
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}
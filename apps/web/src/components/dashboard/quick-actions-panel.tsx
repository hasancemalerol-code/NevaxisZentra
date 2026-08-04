import {
  Bot,
  CalendarClock,
  ClipboardCheck,
  Factory,
  RefreshCw,
  ShieldAlert,
  TriangleAlert,
  Wrench,
  type LucideIcon,
} from "lucide-react";

type QuickAction = Readonly<{
  label: string;
  description: string;
  href: string;
  icon: LucideIcon;
  tone:
    | "blue"
    | "green"
    | "orange"
    | "purple"
    | "slate";
}>;

const quickActions: readonly QuickAction[] = [
  {
    label: "AI Görevi",
    description: "Yeni AI inceleme görevi oluştur",
    href: "/",
    icon: Bot,
    tone: "blue",
  },
  {
    label: "Üretim Emri",
    description: "Aktif üretim emirlerini incele",
    href: "/uretim",
    icon: Factory,
    tone: "blue",
  },
  {
    label: "Plan Revizyonu",
    description: "Üretim planı revizyonu başlat",
    href: "/planlama",
    icon: CalendarClock,
    tone: "orange",
  },
  {
    label: "Bakım Talebi",
    description: "Makine veya kalıp talebi aç",
    href: "/bakim",
    icon: Wrench,
    tone: "purple",
  },
  {
    label: "Kalite Kaydı",
    description: "Kontrol veya uygunsuzluk aç",
    href: "/kalite",
    icon: ClipboardCheck,
    tone: "green",
  },
  {
    label: "Malzeme Riski",
    description: "Malzeme uygunluğunu incele",
    href: "/riskler/malzeme-uygunlugu",
    icon: TriangleAlert,
    tone: "slate",
  },
  {
    label: "Termin Riski",
    description: "Sipariş ve termin risklerini değerlendir",
    href: "/riskler/siparis-termin",
    icon: ShieldAlert,
    tone: "orange",
  },
  {
    label: "ERP Senkronizasyonu",
    description: "Entegrasyon durumunu kontrol et",
    href: "/erp-entegrasyonlari/senkronizasyonlar",
    icon: RefreshCw,
    tone: "green",
  },
];

function getToneStyles(tone: QuickAction["tone"]) {
  switch (tone) {
    case "green":
      return {
        background: "#e8f8f1",
        color: "#079765",
      };

    case "orange":
      return {
        background: "#fff3df",
        color: "#d97706",
      };

    case "purple":
      return {
        background: "#f2ebff",
        color: "#7c3aed",
      };

    case "slate":
      return {
        background: "#eef3f8",
        color: "#52647d",
      };

    case "blue":
    default:
      return {
        background: "#eaf2ff",
        color: "#1769e0",
      };
  }
}

export function QuickActionsPanel() {
  return (
    <section className="dashboard-panel dashboard-panel--quick-actions">
      <header className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Operasyon kısayolları
          </p>

          <h2 className="dashboard-section-heading__title">
            Hızlı İşlemler
          </h2>
        </div>

        <span className="dashboard-section-heading__meta">
          {quickActions.length} işlem
        </span>
      </header>

      <div className="locked-quick-actions">
        {quickActions.map((action) => {
          const Icon = action.icon;
          const toneStyles = getToneStyles(action.tone);

          return (
            <a
              className="locked-quick-action"
              href={action.href}
              key={action.href}
            >
              <span
                aria-hidden="true"
                style={toneStyles}
              >
                <Icon />
              </span>

              <span>
                <strong>{action.label}</strong>
                <small>{action.description}</small>
              </span>
            </a>
          );
        })}
      </div>
    </section>
  );
}
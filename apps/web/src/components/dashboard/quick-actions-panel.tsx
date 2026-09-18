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
  permissionKey?: string;
  tone:
    | "blue"
    | "green"
    | "orange"
    | "purple"
    | "slate";
}>;

const quickActions: readonly QuickAction[] = [
  {
    label: "Görev",
    description: "Bekleyen görevleri incele",
    href: "/",
    icon: Bot,
    tone: "blue",
  },
  {
    label: "Üretim Emri",
    description: "Aktif üretim emirlerini incele",
    href: "/uretim",
    icon: Factory,
    permissionKey: "production.execution.read",
    tone: "blue",
  },
  {
    label: "Plan Revizyonu",
    description: "Üretim planlarını incele",
    href: "/planlama",
    icon: CalendarClock,
    permissionKey: "production.planning.read",
    tone: "orange",
  },
  {
    label: "Bakım Talebi",
    description: "Bakım emirlerini incele",
    href: "/bakim",
    icon: Wrench,
    permissionKey: "maintenance.read",
    tone: "purple",
  },
  {
    label: "Kalite Kaydı",
    description: "Kalite kayıtlarını incele",
    href: "/kalite",
    icon: ClipboardCheck,
    permissionKey: "quality.read",
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
    permissionKey: "erp.read",
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

type QuickActionsPanelProps = Readonly<{
  grantedPermissionKeys?: readonly string[];
}>;

export function QuickActionsPanel({
  grantedPermissionKeys = [],
}: QuickActionsPanelProps) {
  const visibleActions = quickActions.filter((action) => {
    if (!action.permissionKey) {
      return true;
    }

    return grantedPermissionKeys.includes(action.permissionKey);
  });

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
          {visibleActions.length} işlem
        </span>
      </header>

      <div className="locked-quick-actions">
        {visibleActions.map((action) => {
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
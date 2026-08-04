import {
  Boxes,
  Cable,
  CalendarClock,
  CalendarRange,
  ChartNoAxesCombined,
  Cpu,
  Factory,
  LayoutDashboard,
  ListTodo,
  MessagesSquare,
  PackageCheck,
  Settings,
  ShieldCheck,
  TriangleAlert,
  Wrench,
  type LucideIcon,
} from "lucide-react";

export type NavigationItem = Readonly<{
  label: string;
  href: string;
  icon: LucideIcon;
  exact?: boolean;
}>;

export type NavigationGroup = Readonly<{
  label: string;
  items: readonly NavigationItem[];
}>;

export const primaryNavigation: readonly NavigationGroup[] = [
  {
    label: "Çalışma alanı",
    items: [
      { label: "AI Görev Merkezi", href: "/", icon: ListTodo, exact: true },
      { label: "Dashboard / Operasyon Merkezi", href: "/dashboard", icon: LayoutDashboard },
    ],
  },
  {
    label: "Fabrika operasyonları",
    items: [
      { label: "Üretim", href: "/uretim", icon: Factory },
      { label: "Planlama", href: "/planlama", icon: CalendarRange },
      { label: "Vardiyalar", href: "/vardiyalar", icon: CalendarClock },
      { label: "Makineler", href: "/makineler", icon: Cpu },
      { label: "Kalıplar", href: "/kaliplar", icon: Boxes },
      { label: "Bakım", href: "/bakim", icon: Wrench },
      { label: "Kalite", href: "/kalite", icon: ShieldCheck },
      { label: "Malzeme Riskleri", href: "/malzeme-riskleri", icon: TriangleAlert },
      { label: "Sipariş Operasyonları", href: "/siparis-operasyonlari", icon: PackageCheck },
    ],
  },
  {
    label: "İletişim ve yönetim",
    items: [
      { label: "CrevaFactOS Teams", href: "/teams", icon: MessagesSquare },
      { label: "Raporlar", href: "/raporlar", icon: ChartNoAxesCombined },
      { label: "Entegrasyonlar", href: "/entegrasyonlar", icon: Cable },
      { label: "Ayarlar", href: "/ayarlar", icon: Settings },
    ],
  },
] as const;

export const navigationItems = primaryNavigation.flatMap((group) => group.items);

export function isNavigationItemActive(item: NavigationItem, pathname: string) {
  return item.exact ? pathname === item.href : pathname === item.href || pathname.startsWith(`${item.href}/`);
}

import {
  BadgeDollarSign,
  Boxes,
  Cable,
  CalendarClock,
  CalendarRange,
  ChartNoAxesCombined,
  CircleGauge,
  CirclePercent,
  Cpu,
  Factory,
  FileWarning,
  Gauge,
  LayoutDashboard,
  ListTodo,
  MessagesSquare,
  PackageSearch,
  Settings,
  ShieldCheck,
  ShoppingBasket,
  TriangleAlert,
  UsersRound,
  Wrench,
  Zap,
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
      {
        label: "AI Görev Merkezi",
        href: "/",
        icon: ListTodo,
        exact: true,
      },
      {
        label: "Dashboard / Operasyon Merkezi",
        href: "/dashboard",
        icon: LayoutDashboard,
      },
      {
        label: "CrevaFactOS Teams",
        href: "/teams",
        icon: MessagesSquare,
      },
    ],
  },
  {
    label: "Fabrika operasyonları",
    items: [
      {
        label: "Siparişler",
        href: "/siparisler",
        icon: ShoppingBasket,
      },
      {
        label: "Planlama",
        href: "/planlama",
        icon: CalendarRange,
      },
      {
        label: "Üretim",
        href: "/uretim",
        icon: Factory,
      },
      {
        label: "Vardiyalar",
        href: "/vardiyalar",
        icon: CalendarClock,
      },
      {
        label: "Makineler",
        href: "/makineler",
        icon: Cpu,
      },
      {
        label: "Kalıplar",
        href: "/kaliplar",
        icon: Boxes,
      },
      {
        label: "Bakım",
        href: "/bakim",
        icon: Wrench,
      },
      {
        label: "Kalite Kontrol",
        href: "/kalite",
        icon: ShieldCheck,
      },
      {
        label: "Enerji",
        href: "/enerji",
        icon: Zap,
      },
      {
        label: "IoT & PLC",
        href: "/iot-plc",
        icon: Cable,
      },
    ],
  },
  {
    label: "Operasyonel riskler",
    items: [
      {
        label: "Malzeme Uygunluğu",
        href: "/riskler/malzeme-uygunlugu",
        icon: PackageSearch,
      },
      {
        label: "Tedarik Riskleri",
        href: "/riskler/tedarik",
        icon: ShoppingBasket,
      },
      {
        label: "Sipariş & Termin Riskleri",
        href: "/riskler/siparis-termin",
        icon: TriangleAlert,
      },
      {
        label: "Maliyet Sapmaları",
        href: "/riskler/maliyet-sapmalari",
        icon: CirclePercent,
      },
      {
        label: "Kapasite Riskleri",
        href: "/riskler/kapasite",
        icon: Gauge,
      },
      {
        label: "Kalite Riskleri",
        href: "/riskler/kalite",
        icon: FileWarning,
      },
    ],
  },
  {
    label: "Ticari zekâ",
    items: [
      {
        label: "Satın Alma Fiyat Analizi",
        href: "/ticari-zeka/satin-alma-fiyat-analizi",
        icon: ShoppingBasket,
      },
      {
        label: "Tedarikçi Karşılaştırması",
        href: "/ticari-zeka/tedarikci-karsilastirmasi",
        icon: UsersRound,
      },
      {
        label: "Satış Fiyat Analizi",
        href: "/ticari-zeka/satis-fiyat-analizi",
        icon: BadgeDollarSign,
      },
      {
        label: "Ürün Kârlılığı",
        href: "/ticari-zeka/urun-karliligi",
        icon: CircleGauge,
      },
      {
        label: "Müşteri Kârlılığı",
        href: "/ticari-zeka/musteri-karliligi",
        icon: UsersRound,
      },
      {
        label: "Teklif Riskleri",
        href: "/ticari-zeka/teklif-riskleri",
        icon: FileWarning,
      },
    ],
  },
  {
    label: "ERP entegrasyonları",
    items: [
      {
        label: "ERP Bağlantısı",
        href: "/erp-entegrasyonlari/baglanti",
        icon: Cable,
      },
      {
        label: "Veri Eşleştirme",
        href: "/erp-entegrasyonlari/veri-eslestirme",
        icon: Boxes,
      },
      {
        label: "Senkronizasyonlar",
        href: "/erp-entegrasyonlari/senkronizasyonlar",
        icon: CircleGauge,
      },
      {
        label: "Hata Kuyruğu",
        href: "/erp-entegrasyonlari/hata-kuyrugu",
        icon: TriangleAlert,
      },
      {
        label: "Entegrasyon Sağlığı",
        href: "/erp-entegrasyonlari/saglik",
        icon: Gauge,
      },
    ],
  },
  {
    label: "Yönetim",
    items: [
      {
        label: "Raporlama & Analitik",
        href: "/raporlar",
        icon: ChartNoAxesCombined,
      },
      {
        label: "Ayarlar",
        href: "/ayarlar",
        icon: Settings,
      },
    ],
  },
] as const;

export const navigationItems = primaryNavigation.flatMap(
  (group) => group.items,
);

export function isNavigationItemActive(
  item: NavigationItem,
  pathname: string,
) {
  return item.exact
    ? pathname === item.href
    : pathname === item.href ||
        pathname.startsWith(`${item.href}/`);
}
import type {
  SerializableNavigationGroup,
  SerializableNavigationItem,
} from "./navigation-types";

export type NavigationItem = SerializableNavigationItem;
export type NavigationGroup = SerializableNavigationGroup;

export const primaryNavigation: readonly NavigationGroup[] = [
  {
    label: "Çalışma alanı",
    items: [
      {
        label: "AI Görev Merkezi",
        href: "/",
        iconKey: "list-todo",
        exact: true,
      },
      {
        label: "Dashboard / Operasyon Merkezi",
        href: "/dashboard",
        iconKey: "layout-dashboard",
      },
      {
        label: "CrevaFactOS Teams",
        href: "/teams",
        iconKey: "messages-square",
      },
    ],
  },
  {
    label: "Fabrika operasyonları",
    items: [
      {
        label: "Siparişler",
        href: "/siparisler",
        iconKey: "shopping-basket",
        subscriptionModulePath: "/siparisler",
      },
      {
        label: "Planlama",
        href: "/planlama",
        iconKey: "calendar-range",
        subscriptionModulePath: "/planlama",
      },
      {
        label: "Üretim",
        href: "/uretim",
        iconKey: "factory",
        subscriptionModulePath: "/uretim",
      },
      {
        label: "İhracat",
        href: "/ihracat",
        iconKey: "ship",
        subscriptionModulePath: "/ihracat",
      },
      {
        label: "Vardiyalar",
        href: "/vardiyalar",
        iconKey: "calendar-clock",
        permissionKey: "shift-workforce.read",
        subscriptionModulePath: "/vardiyalar",
      },
      {
        label: "Proses & OEE",
        href: "/proses-oee",
        iconKey: "circle-gauge",
        permissionKey: "process-oee.read",
        subscriptionModulePath: "/proses-oee",
      },
      {
        label: "Makineler",
        href: "/makineler",
        iconKey: "cpu",
        permissionKey: "edge.read",
        subscriptionModulePath: "/makineler",
      },
      {
        label: "Kalıplar",
        href: "/kaliplar",
        iconKey: "molds",
        permissionKey: "mold.read",
        subscriptionModulePath: "/kaliplar",
      },
      {
        label: "Malzemeler",
        href: "/malzemeler",
        iconKey: "package",
        permissionKey: "material.read",
        subscriptionModulePath: "/malzemeler",
      },
      {
        label: "Bakım",
        href: "/bakim",
        iconKey: "wrench",
        permissionKey: "maintenance.read",
        subscriptionModulePath: "/bakim",
      },
      {
        label: "Kalite Kontrol",
        href: "/kalite",
        iconKey: "shield-check",
        subscriptionModulePath: "/kalite",
      },
      {
        label: "Enerji",
        href: "/enerji",
        iconKey: "zap",
        permissionKey: "energy.read",
        subscriptionModulePath: "/enerji",
      },
      {
        label: "IoT & PLC",
        href: "/iot-plc",
        iconKey: "cable",
        permissionKey: "iot-plc.read",
        subscriptionModulePath: "/iot-plc",
      },
    ],
  },
  {
    label: "Operasyonel riskler",
    items: [
      {
        label: "Operasyonel Riskler",
        href: "/operasyonel-riskler",
        iconKey: "triangle-alert",
        permissionKey: "operational-risk.read",
        subscriptionModulePath: "/riskler",
      },
      {
        label: "Malzeme Uygunluğu",
        href: "/riskler/malzeme-uygunlugu",
        iconKey: "package-search",
      },
      {
        label: "Tedarik Riskleri",
        href: "/riskler/tedarik",
        iconKey: "shopping-basket",
      },
      {
        label: "Sipariş & Termin Riskleri",
        href: "/riskler/siparis-termin",
        iconKey: "triangle-alert",
      },
      {
        label: "Maliyet Sapmaları",
        href: "/riskler/maliyet-sapmalari",
        iconKey: "circle-percent",
      },
      {
        label: "Kapasite Riskleri",
        href: "/riskler/kapasite",
        iconKey: "gauge",
      },
      {
        label: "Kalite Riskleri",
        href: "/riskler/kalite",
        iconKey: "file-warning",
      },
    ],
  },
  {
    label: "TİCARİ ZEKÂ",
    items: [
      {
        label: "Satın Alma Fiyat Analizi",
        href: "/ticari-zeka/satin-alma-fiyat-analizi",
        iconKey: "shopping-basket",
        permissionKey: "commercial-intelligence.read",
        subscriptionModulePath: "/ticari-zeka",
      },
      {
        label: "Tedarikçi Karşılaştırması",
        href: "/ticari-zeka/tedarikci-karsilastirmasi",
        iconKey: "users-round",
      },
      {
        label: "Satış Fiyat Analizi",
        href: "/ticari-zeka/satis-fiyat-analizi",
        iconKey: "badge-dollar-sign",
      },
      {
        label: "Ürün Kârlılığı",
        href: "/ticari-zeka/urun-karliligi",
        iconKey: "circle-gauge",
      },
      {
        label: "Müşteri Kârlılığı",
        href: "/ticari-zeka/musteri-karliligi",
        iconKey: "users-round",
      },
      {
        label: "Teklif Riskleri",
        href: "/ticari-zeka/teklif-riskleri",
        iconKey: "file-warning",
      },
    ],
  },
  {
    label: "ERP entegrasyonları",
    items: [
      {
        label: "ERP Bağlantısı",
        href: "/erp-entegrasyonlari/baglanti",
        iconKey: "cable",
      },
      {
        label: "Veri Eşleştirme",
        href: "/erp-entegrasyonlari/veri-eslestirme",
        iconKey: "boxes",
      },
      {
        label: "Senkronizasyonlar",
        href: "/erp-entegrasyonlari/senkronizasyonlar",
        iconKey: "circle-gauge",
      },
      {
        label: "Hata Kuyruğu",
        href: "/erp-entegrasyonlari/hata-kuyrugu",
        iconKey: "triangle-alert",
      },
      {
        label: "Entegrasyon Sağlığı",
        href: "/erp-entegrasyonlari/saglik",
        iconKey: "gauge",
      },
    ],
  },
  {
    label: "Yönetim",
    items: [
      {
        label: "Raporlama & Analitik",
        href: "/raporlar",
        iconKey: "chart-no-axes-combined",
      },
      {
        label: "Ayarlar",
        href: "/ayarlar",
        iconKey: "settings",
        permissionKey: "admin-access.read",
      },
    ],
  },
] as const;

export const navigationItems = primaryNavigation.flatMap(
  (group) => group.items,
);

export { isNavigationItemActive } from "./navigation-types";

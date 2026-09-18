import type {
  SerializableNavigationGroup,
  SerializableNavigationItem,
} from "./navigation-types";

export type NavigationItem = SerializableNavigationItem;
export type NavigationGroup = SerializableNavigationGroup;

export const primaryNavigation: readonly NavigationGroup[] = [
  {
    label: "Çalışma Alanı",
    items: [
      {
        label: "Görev Merkezi",
        href: "/",
        iconKey: "list-todo",
        exact: true,
      },
      {
        label: "Dijital İş Gücü",
        href: "/dijital-is-gucu",
        iconKey: "users-round",
      },
      {
        label: "Operasyon Merkezi",
        href: "/dashboard",
        iconKey: "layout-dashboard",
      },
      {
        label: "Operasyon Yönetim Merkezi",
        href: "/operasyon/yonetim-merkezi",
        iconKey: "layout-dashboard",
        subscriptionModulePath: "/operasyon/yonetim-merkezi",
      },
      {
        label: "Fabrika Karar Merkezi",
        href: "/fabrika-karar-merkezi",
        iconKey: "gauge",
        permissionKey: "factory-case.read",
        subscriptionModulePath: "/operasyon/yonetim-merkezi",
      },
      {
        label: "Yönetici Komuta Merkezi",
        href: "/yonetici-komuta-merkezi",
        iconKey: "gauge",
        permissionKey: "factory-case.read",
        subscriptionModulePath: "/operasyon/yonetim-merkezi",
      },
      {
        label: "Fabrika Zekâsı",
        href: "/fabrika-zekasi",
        iconKey: "circle-gauge",
        permissionKey: "factory-intelligence.read",
      },
      {
        label: "FactOS Teams",
        href: "/teams",
        iconKey: "messages-square",
      },
      {
        label: "Sunum Modu",
        href: "/sunum-modu",
        iconKey: "activity",
        permissionKey: "presentation-mode.access",
      },
    ],
  },
  {
    label: "Üretim",
    items: [
      {
        label: "Genel Bakış",
        href: "/uretim",
        iconKey: "factory",
        subscriptionModulePath: "/uretim",
      },
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
        label: "Hatlar",
        href: "/uretim/hatlar",
        iconKey: "factory",
        permissionKey: "mixed-production.read",
        subscriptionModulePath: "/uretim",
      },
      {
        label: "Üretim Kaynakları",
        href: "/uretim/kaynaklar",
        iconKey: "cpu",
        permissionKey: "mixed-production.read",
        subscriptionModulePath: "/uretim",
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
        label: "Fabrika Ağı",
        href: "/fabrika-agi",
        iconKey: "factory",
        permissionKey: "factory-network.read",
        subscriptionModulePath: "/planlama",
      },
    ],
  },
  {
    label: "Kaynaklar",
    items: [
      {
        label: "Makineler",
        href: "/makineler",
        iconKey: "cpu",
        permissionKey: "edge.read",
        subscriptionModulePath: "/makineler",
      },
      {
        label: "Kalıp & Takımlar",
        href: "/kaliplar",
        iconKey: "molds",
        permissionKey: "mold.read",
        subscriptionModulePath: "/kaliplar",
      },
    ],
  },
  {
    label: "Operasyon",
    items: [
      {
        label: "Bakım",
        href: "/bakim",
        iconKey: "wrench",
        permissionKey: "maintenance.read",
        subscriptionModulePath: "/bakim",
      },
      {
        label: "Kalite",
        href: "/kalite",
        iconKey: "shield-check",
        subscriptionModulePath: "/kalite",
      },
      {
        label: "Malzemeler",
        href: "/malzemeler",
        iconKey: "package",
        permissionKey: "material.read",
        subscriptionModulePath: "/malzemeler",
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
    label: "Risk & Tedarik",
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
      {
        label: "Satın Alma Tedarik Riskleri",
        href: "/satin-alma/tedarik-riskleri",
        iconKey: "shopping-basket",
        permissionKey: "erp.read",
        subscriptionModulePath: "/satin-alma",
      },
    ],
  },
  {
    label: "Dış Operasyonlar",
    items: [
      {
        label: "İhracat",
        href: "/ihracat",
        iconKey: "ship",
        subscriptionModulePath: "/ihracat",
      },
    ],
  },
  {
    label: "Ticari Zekâ",
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
        permissionKey: "commercial-intelligence.read",
        subscriptionModulePath: "/ticari-zeka",
      },
      {
        label: "Satış Fiyat Analizi",
        href: "/ticari-zeka/satis-fiyat-analizi",
        iconKey: "badge-dollar-sign",
        permissionKey: "commercial-intelligence.read",
        subscriptionModulePath: "/ticari-zeka",
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
        label: "Malzeme ve Maliyet Kârlılığı",
        href: "/ticari-zeka/malzeme-ve-maliyet-karliligi",
        iconKey: "boxes",
      },
      {
        label: "Kârlılık Uyarıları",
        href: "/ticari-zeka/karlilik-uyarilari",
        iconKey: "triangle-alert",
        permissionKey: "commercial-intelligence.read",
        subscriptionModulePath: "/ticari-zeka",
      },
      {
        label: "Fiyat Koruma ve Onay",
        href: "/ticari-zeka/fiyat-koruma-ve-onay",
        iconKey: "shield-check",
        permissionKey: "commercial-intelligence.read",
        subscriptionModulePath: "/ticari-zeka",
      },
      {
        label: "Kârlılık Yönetim Merkezi",
        href: "/ticari-zeka/karlilik-yonetim-merkezi",
        iconKey: "layout-dashboard",
        permissionKey: "commercial-intelligence.read",
        subscriptionModulePath: "/ticari-zeka",
      },
      {
        label: "Teklif Riskleri",
        href: "/ticari-zeka/teklif-riskleri",
        iconKey: "file-warning",
      },
    ],
  },
  {
    label: "ERP & Entegrasyon",
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
    label: "Sistem",
    items: [
      {
        label: "Raporlama & Analitik",
        href: "/raporlar",
        iconKey: "chart-no-axes-combined",
      },
      {
        label: "Entegrasyon Merkezi",
        href: "/ayarlar/entegrasyonlar",
        iconKey: "cable",
        permissionKey: "integration.read",
      },
      {
        label: "Veri Aktarım Merkezi",
        href: "/ayarlar/veri-aktarim-merkezi",
        iconKey: "boxes",
        permissionKey: "bulk-onboarding.read",
      },
      {
        label: "Sistem Durumu",
        href: "/ayarlar/sistem-durumu",
        iconKey: "activity",
        permissionKey: "operability.read",
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

export const PRESENTATION_MODE_ROUTE = "/sunum-modu";
export const PRESENTATION_MODE_TITLE = "SUNUM MODU";
export const PRESENTATION_MODE_SUBTITLE =
  "FactOS'un dijital iş gücü, karar ve doğrulama yeteneklerini uçtan uca senaryolar üzerinden gösterin.";

export const FACTORY_DECISION_CENTER_ROUTE = "/fabrika-karar-merkezi";
export const CRITICAL_ORDER_RECOVERY_ORDER_REFERENCE = "ORD-2026-0915-047";

export type PresentationScenarioStatus =
  | "Hazır"
  | "Devam Ediyor"
  | "İnsan Onayı Bekliyor"
  | "Doğrulama"
  | "VERIFIED";

export type PresentationScenarioKey =
  | "production-recovery"
  | "customs-file"
  | "critical-order-recovery";

export type PresentationScenarioDefinition = Readonly<{
  key: PresentationScenarioKey;
  title: string;
  description: string;
  outcomeTag: string;
  capabilities: readonly string[];
  narrativeSteps: readonly string[];
  demoHref: string;
  factoryDecisionHref?: string;
}>;

export const PRESENTATION_SCENARIOS: readonly PresentationScenarioDefinition[] = [
  {
    key: "production-recovery",
    title: "Üretim Kurtarma",
    description:
      "Makine performansındaki sapmayı tespit eder, kurtarma fırsatını hesaplar, insan onayı ile müdahaleyi yürütür ve gerçek üretim sonucunu doğrular.",
    outcomeTag: "ÜRETİM SONUCU",
    capabilities: [
      "Makine Zekâsı",
      "Dijital Çalışan",
      "İnsan Onayı",
      "Kanıt Paketi",
      "VERIFIED Kapanış",
    ],
    narrativeSteps: ["TESPİT", "KARAR", "AKSİYON", "DOĞRULAMA"],
    demoHref: "/golden-demo/uretim-kurtarma",
  },
  {
    key: "customs-file",
    title: "Gümrük Dosyası",
    description:
      "İhracat sevkiyatı için gerekli belgeleri toplar, eksik ve tutarsızlıkları yönetir, dosyayı müşavir devrine hazır hale getirir ve sonucu doğrular.",
    outcomeTag: "BELGE / İŞ SONUCU",
    capabilities: [
      "Belge Kontrolü",
      "Tutarlılık",
      "Dijital Çalışan",
      "İnsan Onayı",
      "Kanıt Paketi",
      "VERIFIED Kapanış",
    ],
    narrativeSteps: [
      "BELGE KONTROLÜ",
      "EKSİK / TUTARSIZLIK",
      "DOSYA HAZIRLAMA",
      "DEVİR / DOĞRULAMA",
    ],
    demoHref: "/golden-demo/gumruk-dosyasi",
  },
  {
    key: "critical-order-recovery",
    title: "Kritik Sipariş Kurtarma",
    description:
      "Risk altındaki müşteri siparişini, birden fazla Dijital Çalışanı tek sonuç altında koordine ederek kurtarır ve teslimat taahhüdünü doğrular.",
    outcomeTag: "FABRİKA ORKESTRASYONU",
    capabilities: [
      "Fabrika Karar Merkezi",
      "Cross-module correlation",
      "Fabrika Operasyon Koordinatörü",
      "Çoklu Dijital Çalışan",
      "İnsan Kararı",
      "VERIFIED Outcome",
    ],
    narrativeSteps: [
      "RİSK",
      "KARAR VAKASI",
      "DİJİTAL İŞ GÜCÜ",
      "İNSAN KARARI",
      "YÜRÜTME",
      "VERIFIED",
    ],
    demoHref: FACTORY_DECISION_CENTER_ROUTE,
    factoryDecisionHref: FACTORY_DECISION_CENTER_ROUTE,
  },
];

export function getPresentationStatusTone(
  status: PresentationScenarioStatus,
): "neutral" | "active" | "warning" | "success" {
  switch (status) {
    case "VERIFIED":
      return "success";
    case "İnsan Onayı Bekliyor":
      return "warning";
    case "Doğrulama":
    case "Devam Ediyor":
      return "active";
    default:
      return "neutral";
  }
}

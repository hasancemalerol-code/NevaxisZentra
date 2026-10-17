import type { OperationsCommandCenterModuleCode } from "../types/operations-command-center-types";

export const OPERATIONS_COMMAND_CENTER_ROUTE_PATH = "/operasyon/yonetim-merkezi";

export const OPERATIONS_COMMAND_CENTER_SUBTITLE =
  "İnsan incelemesi gerektiren operasyonel kayıtları tek ekranda birleştirir.";

export const OPERATIONS_COMMAND_CENTER_AUTOMATION_DISCLAIMER =
  "Bu merkez yalnızca insan incelemesi gerektiren operasyonel kayıtları bir araya getirir; otomatik üretim, stok, satın alma, bakım veya kalite işlemi yapmaz.";

export const OPERATIONS_COMMAND_CENTER_READONLY_NOTE =
  "Bu merkez yalnızca okuma amaçlıdır ve otomatik işlem yapmaz; inceleme ve görev aksiyonları ilgili çalışma alanlarından yürütülür.";

export const OPERATIONS_COMMAND_CENTER_EMPTY_QUEUE_NOTE =
  "Seçili filtrelerle eşleşen öncelikli kayıt bulunamadı.";

export const OPERATIONS_COMMAND_CENTER_NO_ACCESS_NOTE =
  "Erişilebilir operasyon modülü bulunamadı. Yetki ve abonelik kapsamınızı kontrol edin.";

export const OPERATIONS_COMMAND_CENTER_PARTIAL_NOTE =
  "Bazı kaynaklar sınırlı sayıda kayıt döndürdü; kapsam Kısmi olarak işaretlendi.";

export const OPERATIONS_COMMAND_CENTER_COMMERCIAL_LINK_LABEL =
  "Ticari kârlılık yönetim merkezi";

export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_ROUTE_PATH =
  "/ticari-zeka/karlilik-yonetim-merkezi";

const MODULE_LABELS: Record<OperationsCommandCenterModuleCode, string> = {
  factory: "Fabrika vakası",
  "production-manager": "Üretim Yöneticisi",
  commercial: "Ticari zekâ",
  production: "Üretim",
  quality: "Kalite",
  maintenance: "Bakım",
  mold: "Kalıp",
  material: "Malzeme",
  procurement: "Satın alma",
  "ai-task": "Görev",
};

export function getOperationsCommandCenterModuleLabel(
  moduleCode: OperationsCommandCenterModuleCode,
): string {
  return MODULE_LABELS[moduleCode];
}

export function getOperationsCommandCenterAvailabilityLabel(
  availability: string,
): string {
  switch (availability) {
    case "available":
      return "Erişilebilir";
    case "partial":
      return "Kısmi";
    case "unavailable-permission":
      return "Erişim yok";
    case "unavailable-subscription":
      return "Abonelik dışı";
    case "unavailable-error":
      return "Kaynak hatası";
    case "insufficient-data":
      return "Yetersiz veri";
    default:
      return "—";
  }
}

const ENGLISH_STATUS_TOKENS: Readonly<Record<string, string>> = {
  "action required": "Aksiyon Gerekiyor",
  "action-required": "Aksiyon Gerekiyor",
  actionrequired: "Aksiyon Gerekiyor",
  open: "Açık",
  closed: "Kapalı",
  critical: "Kritik",
  high: "Yüksek",
  medium: "Orta",
  low: "Düşük",
  major: "Majör",
  minor: "Minör",
  "in progress": "Devam Ediyor",
  "in-progress": "Devam Ediyor",
  "pending approval": "Onay Bekliyor",
  "pending-approval": "Onay Bekliyor",
  "review required": "İnceleme Gerekiyor",
  "review-required": "İnceleme Gerekiyor",
};

export type OperationsCommandCenterStatusPart = Readonly<{ label: string; tone: "critical" | "warning" | "neutral" }>;

/** Splits a "Önem · Durum" label into Turkish pill parts; unknown English tokens are mapped, never shown raw. */
export function getOperationsCommandCenterStatusParts(statusLabel: string): readonly OperationsCommandCenterStatusPart[] {
  return statusLabel
    .split("·")
    .map((part) => part.trim())
    .filter((part) => part.length > 0)
    .map((part) => {
      const label = ENGLISH_STATUS_TOKENS[part.toLowerCase()] ?? part;
      const lowered = label.toLocaleLowerCase("tr-TR");
      const tone =
        lowered === "kritik" || lowered === "majör" || lowered === "aksiyon gerekiyor"
          ? "critical"
          : lowered === "yüksek" || lowered === "orta" || lowered === "inceleme gerekiyor" || lowered === "onay bekliyor"
            ? "warning"
            : "neutral";
      return { label, tone } as const;
    });
}

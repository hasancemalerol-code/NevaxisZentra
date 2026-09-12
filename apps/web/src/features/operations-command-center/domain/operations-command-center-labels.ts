import type { OperationsCommandCenterModuleCode } from "../types/operations-command-center-types";

export const OPERATIONS_COMMAND_CENTER_ROUTE_PATH = "/operasyon/yonetim-merkezi";

export const OPERATIONS_COMMAND_CENTER_SUBTITLE =
  "İnsan incelemesi gerektiren operasyonel kayıtları tek ekranda birleştirir.";

export const OPERATIONS_COMMAND_CENTER_AUTOMATION_DISCLAIMER =
  "Bu merkez yalnızca insan incelemesi gerektiren operasyonel kayıtları bir araya getirir; otomatik üretim, stok, satın alma, bakım veya kalite işlemi yapmaz.";

export const OPERATIONS_COMMAND_CENTER_READONLY_NOTE =
  "Bu merkez yalnızca okuma amaçlıdır; kaynak modüllerdeki inceleme ve görev aksiyonları ilgili çalışma alanlarından yürütülür.";

export const OPERATIONS_COMMAND_CENTER_EMPTY_QUEUE_NOTE =
  "Seçili filtrelerle eşleşen öncelikli kayıt bulunamadı.";

export const OPERATIONS_COMMAND_CENTER_NO_ACCESS_NOTE =
  "Erişilebilir operasyon modülü bulunamadı. Yetki ve abonelik kapsamınızı kontrol edin.";

export const OPERATIONS_COMMAND_CENTER_PARTIAL_NOTE =
  "Bazı kaynaklar sınırlı sayıda kayıt döndürdü; kapsam Kısmi olarak işaretlendi.";

export const OPERATIONS_COMMAND_CENTER_COMMERCIAL_LINK_LABEL =
  "Ticari Kârlılık Yönetim Merkezi";

export const COMMERCIAL_PROFITABILITY_COMMAND_CENTER_ROUTE_PATH =
  "/ticari-zeka/karlilik-yonetim-merkezi";

const MODULE_LABELS: Record<OperationsCommandCenterModuleCode, string> = {
  factory: "Fabrika vakası",
  "production-manager": "AI Üretim Yöneticisi",
  commercial: "Ticari zekâ",
  production: "Üretim",
  quality: "Kalite",
  maintenance: "Bakım",
  mold: "Kalıp",
  material: "Malzeme",
  procurement: "Satın alma",
  "ai-task": "AI Görev",
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

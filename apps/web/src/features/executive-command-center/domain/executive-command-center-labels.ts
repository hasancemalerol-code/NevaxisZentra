export const EXECUTIVE_COMMAND_CENTER_ROUTE_PATH = "/yonetici-komuta-merkezi";

export const EXECUTIVE_COMMAND_CENTER_TITLE = "Yönetici Komuta Merkezi";

export const EXECUTIVE_COMMAND_CENTER_SUBTITLE =
  "Operasyonel risk, kurtarma, insan kararı ve doğrulanmış sonuçların yönetici özet katmanı.";

export const EXECUTIVE_COMMAND_CENTER_EMPTY_RISKS =
  "Şu anda kritik bir operasyonel risk bulunmuyor.";

export const EXECUTIVE_PRIORITY_EMPTY =
  "Öncelik yığınında bekleyen vaka yok.";

export const EXECUTIVE_COMMAND_CENTER_EMPTY_APPROVALS =
  "Bekleyen insan kararı yok.";

export const EXECUTIVE_COMMAND_CENTER_EMPTY_VERIFIED =
  "Bugün henüz doğrulanmış kapanış yok.";

export const EXECUTIVE_COMMAND_CENTER_EMPTY_THROUGHPUT =
  "Aktif hat verimliliği iyileştirme vakası bulunmuyor.";

export const EXECUTIVE_COMMAND_CENTER_PARTIAL_NOTE =
  "Bazı bölümler hesabınızın yetkisi, aboneliği veya kaynak durumu nedeniyle gösterilemiyor.";

export const EXECUTIVE_PRIORITY_SECTION_LABELS: Record<string, string> = {
  "critical-now": "KRİTİK ŞİMDİ",
  "decision-today": "BUGÜN KARAR GEREKTİREN",
  "active-recovery": "AKTİF KURTARMA",
  "pending-verification": "DOĞRULAMA BEKLEYEN",
  "verified-today": "BUGÜN DOĞRULANAN",
};

export const EXECUTIVE_COMMAND_CENTER_FILTER_SEVERITY_OPTIONS = [
  { value: "", label: "Tümü" },
  { value: "critical", label: "Kritik" },
  { value: "high", label: "Yüksek" },
  { value: "medium", label: "Orta" },
  { value: "low", label: "Düşük" },
] as const;

export const EXECUTIVE_COMMAND_CENTER_FILTER_TIME_SCOPE_OPTIONS = [
  { value: "today", label: "Bugün" },
  { value: "shift", label: "Vardiya" },
  { value: "recent", label: "Son dönem" },
] as const;

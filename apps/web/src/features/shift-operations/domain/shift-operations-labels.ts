import type { ShiftInstanceStatus } from "./shift-operations-types";

export const SHIFT_OPERATIONS_SUBNAV = [
  { href: "/vardiyalar", label: "Genel Bakış", exact: true },
  { href: "/vardiyalar/plan", label: "Vardiya Planı" },
  { href: "/vardiyalar/aktif", label: "Aktif Vardiya" },
  { href: "/vardiyalar/kaynak", label: "Ekip ve Kaynak" },
  { href: "/vardiyalar/teslim", label: "Teslim Hazırlığı" },
  { href: "/vardiya-devir", label: "Vardiya Devri" },
  { href: "/vardiyalar/acik-konular", label: "Açık Konular" },
  { href: "/vardiyalar/performans", label: "Performans" },
  { href: "/vardiyalar/gecmis", label: "Geçmiş" },
] as const;

export const SHIFT_STATUS_LABELS: Record<ShiftInstanceStatus, string> = {
  planned: "Planlandı",
  ready: "Hazır",
  active: "Aktif",
  closing: "Kapanıyor",
  "handover-pending": "Teslim Bekliyor",
  "handed-over": "Teslim Edildi",
  closed: "Kapandı",
  exception: "Sorunlu",
};

export const HANDOVER_PRIORITY_LABELS: Record<string, string> = {
  critical: "Kritik",
  high: "Yüksek",
  normal: "Normal",
  info: "Bilgi",
};

export const OPEN_ITEM_CATEGORY_LABELS: Record<string, string> = {
  production: "Üretim",
  machine: "Makine",
  maintenance: "Bakım",
  quality: "Kalite",
  material: "Malzeme",
  supplier: "Tedarikçi",
  delivery: "Termin / Sipariş",
  mold: "Kalıp",
  approval: "Onay",
  "digital-workforce": "Asistan önerisi",
  task: "Görev",
  other: "Diğer",
};

export const SHIFT_OPERATIONS_EYEBROW = "Vardiya Operasyonları";

/** One header (eyebrow, title, one-line description) per Vardiyalar page; the shell renders it above the tab bar. */
export const SHIFT_OPERATIONS_PAGE_HEADERS = [
  { href: "/vardiyalar/plan", title: "Vardiya Planı", description: "Planlanan vardiyalar, amirler ve ekipler." },
  { href: "/vardiyalar/aktif", title: "Aktif Vardiya", description: "Çalışan vardiyanın üretim, ekip ve açık konu özeti." },
  { href: "/vardiyalar/kaynak", title: "Ekip ve Kaynak Dağılımı", description: "Hat, makine ve görev sorumlulukları vardiya bağlamında gösterilir." },
  { href: "/vardiyalar/teslim", title: "Teslim Hazırlığı", description: "Vardiya kapanmadan önce devredilecek kalemleri ve hazırlık durumunu inceleyin." },
  { href: "/vardiyalar/acik-konular", title: "Açık Konular", description: "Vardiya boyunca açılan, devredilmesi veya kapatılması gereken konular." },
  { href: "/vardiyalar/performans", title: "Vardiya Performansı", description: "Açık konu, devir ve teslim hazırlığı göstergeleri." },
  { href: "/vardiyalar/gecmis", title: "Vardiya Geçmişi", description: "Tamamlanan vardiyalar, teslim durumları ve teslim hazırlık skorları." },
  { href: "/vardiyalar", title: "Genel Bakış", description: "Planlama, aktif vardiya, teslim ve açık konuların tek merkezden izlenmesi." },
] as const;

export function resolveShiftOperationsPageHeader(pathname: string) {
  const normalized = pathname.length > 1 ? pathname.replace(/\/+$/, "") : pathname;
  return (
    SHIFT_OPERATIONS_PAGE_HEADERS.find((entry) => normalized === entry.href || normalized.startsWith(`${entry.href}/`))
    ?? SHIFT_OPERATIONS_PAGE_HEADERS[SHIFT_OPERATIONS_PAGE_HEADERS.length - 1]
  );
}

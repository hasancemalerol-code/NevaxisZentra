import type { ShiftInstanceStatus } from "./shift-operations-types";

export const SHIFT_OPERATIONS_SUBNAV = [
  { href: "/vardiyalar", label: "Genel Bakış", exact: true },
  { href: "/vardiyalar/plan", label: "Vardiya Planı" },
  { href: "/vardiyalar/aktif", label: "Aktif Vardiya" },
  { href: "/vardiyalar/kaynak", label: "Ekip & Kaynak" },
  { href: "/vardiyalar/teslim", label: "Vardiya Teslimi" },
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
  "digital-workforce": "Dijital İş Gücü",
  task: "Görev",
  other: "Diğer",
};

export type ProductionExecutionEntryType = "good-production" | "scrap" | "reversal";

export type ProductionExecutionEntry = Readonly<{
  id: string;
  entryType: ProductionExecutionEntryType;
  quantity: number;
  unit: string;
  reasonCode?: string;
  comment?: string;
  recordedAtUtc: string;
  recordedBySubjectId: string;
  isReversed: boolean;
  reversesEntryId?: string;
}>;

export const SCRAP_REASON_CODES = [
  "short-shot",
  "flash",
  "sink-mark",
  "burn-mark",
  "warpage",
  "discoloration",
  "dimensional",
  "contamination",
  "startup-scrap",
  "process-instability",
  "material",
  "mold",
  "machine",
  "operator",
  "other",
] as const;

export type ScrapReasonCode = (typeof SCRAP_REASON_CODES)[number];

export function getScrapReasonLabel(code: string): string {
  const labels: Record<string, string> = {
    "short-shot": "Eksik Dolum",
    flash: "Çapak",
    "sink-mark": "Çöküntü İzi",
    "burn-mark": "Yanık İzi",
    warpage: "Çarpılma",
    discoloration: "Renk Bozukluğu",
    dimensional: "Boyutsal Hata",
    contamination: "Kirlilik",
    "startup-scrap": "Başlangıç Firesi",
    "process-instability": "Proses Kararsızlığı",
    material: "Malzeme Hatası",
    mold: "Kalıp Hatası",
    machine: "Makine Hatası",
    operator: "Operatör Hatası",
    other: "Diğer",
  };
  return labels[code] ?? code;
}

export function getEntryTypeLabel(type: ProductionExecutionEntryType): string {
  switch (type) {
    case "good-production":
      return "İyi Üretim";
    case "scrap":
      return "Fire";
    case "reversal":
      return "İptal";
  }
}

export function requiresComment(reasonCode: string): boolean {
  return reasonCode === "other";
}

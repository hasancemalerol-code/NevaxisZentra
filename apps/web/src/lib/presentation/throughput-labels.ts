import { enumLabel, humanizeEnum, knownStateLabel } from "./enum-labels";

/**
 * Turkish customer labels for the line / throughput detail screens. Canonical API values stay untouched;
 * these helpers only decide what the customer reads.
 */

const DATA_QUALITY: Readonly<Record<string, string>> = {
  measured: "Ölçülen",
  partial: "Kısmi",
  estimated: "Tahmini",
  inferred: "Çıkarım",
  derived: "Hesaplanan",
  complete: "Tam",
  unknown: "Bilinmiyor",
  insufficient: "Yetersiz",
  insufficient_data: "Yetersiz veri",
  insufficient_inputs: "Yetersiz girdi",
  stale: "Eski",
  strong: "Güçlü",
  weak: "Zayıf",
  unattributed: "Atanmamış",
  attributed: "Atanmış",
  strongly_attributed: "Güçlü ilişkilendirme",
};

const CONSTRAINT_TYPE: Readonly<Record<string, string>> = {
  cycle_time: "Çevrim süresi",
  cycle: "Çevrim süresi",
  capacity: "Kapasite",
  starvation: "Besleme eksikliği",
  starved: "Besleme eksikliği",
  blocking: "Çıkış tıkanıklığı",
  blocked: "Çıkış tıkanıklığı",
  quality: "Kalite kaybı",
  downtime: "Duruş",
  changeover: "Ürün değişimi",
  labor: "İş gücü",
  manning: "İş gücü",
  operator: "Operatör",
  material: "Malzeme",
  machine: "Makine",
  buffer: "Ara stok",
  unknown: "Belirsiz",
};

const ELIGIBILITY: Readonly<Record<string, string>> = {
  eligible: "Uygun",
  ineligible: "Uygun değil",
  not_eligible: "Uygun değil",
  requires_approval: "Onay gerekli",
  requires_review: "İnceleme gerekli",
  blocked: "Engelli",
  advisory: "Yalnızca öneri",
};

const EVENT_CATEGORY: Readonly<Record<string, string>> = {
  machine: "Makine",
  operator: "Operatör",
  quality: "Kalite",
  material: "Malzeme",
  maintenance: "Bakım",
  production: "Üretim",
  buffer: "Ara stok",
  system: "Sistem",
  manual: "Manuel",
  integration: "Entegrasyon",
};

function lookup(table: Readonly<Record<string, string>>, value: string | null | undefined): string | null {
  const key = (value ?? "").trim().toLowerCase().replace(/[\s-]+/g, "_");
  return key && Object.prototype.hasOwnProperty.call(table, key) ? table[key] : null;
}

/** Line or station state ("STOPPED" becomes "Durdu"); unknown values are humanized, never printed raw. */
export function lineStateLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return knownStateLabel(value) ?? enumLabel(value);
}

export function dataQualityLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return lookup(DATA_QUALITY, value) ?? enumLabel(value);
}

export function constraintTypeLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return lookup(CONSTRAINT_TYPE, value) ?? humanizeEnum(value);
}

export function scenarioEligibilityLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return lookup(ELIGIBILITY, value) ?? enumLabel(value);
}

export function eventCategoryLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return lookup(EVENT_CATEGORY, value) ?? enumLabel(value);
}

const WORKER_PHASE: Readonly<Record<string, string>> = {
  observe: "Gözlem",
  diagnose: "Teşhis",
  simulate: "Simülasyon",
  decide: "Karar",
  approval: "İnsan Onayı",
  execute: "Uygulama",
  observe_result: "Sonuç gözlemi",
  verify: "Doğrulama",
  closed: "Kapandı",
};

export function optimizerPhaseLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return lookup(WORKER_PHASE, value) ?? humanizeEnum(value);
}

const CASE_STATUS: Readonly<Record<string, string>> = {
  open: "Açık",
  closed: "Kapandı",
  observing: "İzleniyor",
  awaiting_approval: "Onay Bekliyor",
  pending_approval: "Onay Bekliyor",
  in_progress: "Devam Ediyor",
  verified: "Doğrulandı",
  none: "Vaka yok",
};

export function optimizerCaseStatusLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return lookup(CASE_STATUS, value) ?? enumLabel(value);
}

const INTELLIGENCE_PATH: Readonly<Record<string, string>> = {
  deterministic: "Kural tabanlı",
  nevaxis_model: "Zentra modeli",
  frontier_model: "Harici model",
  solver: "Optimizasyon",
};

export function intelligencePathLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return lookup(INTELLIGENCE_PATH, value) ?? enumLabel(value);
}

/** Header meta for a line: only facts that exist, shift stated explicitly when unassigned. */
export function lineHeaderFacts(line: Readonly<{ code: string; currentProductCode?: string; currentOrderReference?: string; currentShiftCode?: string }>): string {
  const parts = [line.code];
  if (line.currentProductCode) parts.push(`Ürün: ${line.currentProductCode}`);
  if (line.currentOrderReference) parts.push(`Emir: ${line.currentOrderReference}`);
  parts.push(`Vardiya: ${line.currentShiftCode || "Atanmadı"}`);
  return parts.join(" · ");
}

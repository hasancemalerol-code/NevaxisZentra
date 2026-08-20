export const ENERGY_DEVELOPMENT_BANNER =
  "Yerel geliştirme / simülasyon verisi — gerçek müşteri sayaç verisi değildir.";

export const ENERGY_OPPORTUNITY_DISCLAIMER =
  "Bu değer garanti edilen tasarruf değildir; mevcut hedefe göre hesaplanan açıklanabilir bir karşılaştırmadır.";

export const ENERGY_MEASUREMENT_SCOPE_LABELS: Readonly<Record<string, string>> = {
  facility: "Tesis",
  line: "Hat",
  machine: "Makine",
  "auxiliary-equipment": "Yardımcı ekipman",
};

export const ENERGY_EQUIPMENT_CATEGORY_LABELS: Readonly<Record<string, string>> = {
  "injection-machine": "Enjeksiyon makinesi",
  chiller: "Soğutma grubu (chiller)",
  compressor: "Hava kompresörü",
  "material-dryer": "Malzeme kurutucu",
  robot: "Robot",
  "mold-temperature-controller": "Kalıp sıcaklık kontrolü",
  other: "Diğer",
};

export const ENERGY_EVIDENCE_LABELS: Readonly<Record<string, string>> = {
  measured: "Ölçülmüş",
  calculated: "Hesaplanmış",
  estimated: "Tahmini",
  simulated: "Simülasyon",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak çelişkisi",
};

export const ENERGY_FRESHNESS_LABELS: Readonly<Record<string, string>> = {
  fresh: "Güncel",
  stale: "Bayat",
  unknown: "Bilinmiyor",
};

export const ENERGY_STATUS_LABELS: Readonly<Record<string, string>> = {
  active: "Aktif",
  inactive: "Pasif",
};

export const ENERGY_OUTCOME_LABELS: Readonly<Record<string, string>> = {
  "within-target": "Hedef içinde",
  attention: "Dikkat gerekli",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak çelişkisi",
};

export const ENERGY_DRIVER_LABELS: Readonly<Record<string, string>> = {
  "peak-demand-exceeded": "Tepe güç hedefi aşıldı",
  "idle-consumption-elevated": "Boşta tüketim yüksek",
  "energy-intensity-elevated": "Enerji yoğunluğu yüksek",
  "consumption-above-target": "Tüketim hedefin üzerinde",
  "consumption-above-baseline": "Tüketim baz değerin üzerinde",
  "auxiliary-load-elevated": "Yardımcı yük yüksek",
  "source-stale": "Kaynak bayat",
  "source-conflict": "Kaynak çelişkisi",
  "insufficient-comparable-windows": "Karşılaştırılabilir pencere yetersiz",
  "missing-production-denominator": "Üretim paydası eksik",
  "missing-production-state": "Üretim durumu eksik",
  "missing-target": "Hedef tanımlı değil",
  "missing-tariff": "Tarife tanımlı değil",
  "within-target": "Hedef içinde",
};

export const ENERGY_TARGET_METRIC_LABELS: Readonly<Record<string, string>> = {
  "consumed-kwh": "Tüketim (kWh)",
  "maximum-demand-kw": "Tepe güç (kW)",
  "kwh-per-kg": "kWh/kg",
  "kwh-per-1000-parts": "kWh/1.000 parça",
  "kwh-per-1000-good-parts": "kWh/1.000 iyi parça",
  "idle-consumption-kwh": "Boşta tüketim (kWh)",
};

export const ENERGY_TARGET_SCOPE_LABELS: Readonly<Record<string, string>> = {
  facility: "Tesis",
  machine: "Makine",
  "auxiliary-equipment": "Yardımcı ekipman",
  "production-context": "Üretim bağlamı",
};

export const ENERGY_TARGET_STATUS_LABELS: Readonly<Record<string, string>> = {
  approved: "Onaylı",
  inactive: "Pasif",
};

export const ENERGY_BASELINE_DIMENSION_LABELS: Readonly<Record<string, string>> = {
  facility: "Tesis",
  machine: "Makine",
  "auxiliary-equipment": "Yardımcı ekipman",
  shift: "Vardiya",
  "production-order": "Üretim emri",
  "production-context": "Üretim bağlamı",
};

export const ENERGY_CALCULATION_METHOD_LABELS: Readonly<Record<string, string>> = {
  "interval-sum": "Aralık toplamı",
  "cumulative-delta": "Kümülatif fark",
  mixed: "Karma",
  unavailable: "Kullanılamaz",
};

export const ENERGY_ALLOCATION_CATEGORY_LABELS: Readonly<Record<string, string>> = {
  "injection-machine": "Enjeksiyon makineleri",
  chiller: "Soğutma grubu",
  compressor: "Kompresör",
  "material-dryer": "Malzeme kurutucu",
  unallocated: "Dağıtılmamış / diğer",
};

export const ENERGY_TAB_OPTIONS = [
  { id: "overview", label: "Genel Bakış" },
  { id: "measurement-points", label: "Ölçüm Noktaları" },
  { id: "targets-baselines", label: "Hedefler ve Bazlar" },
  { id: "assessments", label: "Değerlendirmeler" },
] as const;

export type EnergyTabId = (typeof ENERGY_TAB_OPTIONS)[number]["id"];

export const ENERGY_OUTCOME_FILTER_OPTIONS = [
  { value: "", label: "Tüm sonuçlar" },
  { value: "within-target", label: "Hedef içinde" },
  { value: "attention", label: "Dikkat gerekli" },
  { value: "insufficient-data", label: "Veri yetersiz" },
  { value: "source-conflict", label: "Kaynak çelişkisi" },
] as const;

export const ENERGY_SCOPE_FILTER_OPTIONS = [
  { value: "", label: "Tüm kapsamlar" },
  { value: "machine", label: "Makine" },
  { value: "auxiliary-equipment", label: "Yardımcı ekipman" },
  { value: "facility", label: "Tesis" },
  { value: "production-context", label: "Üretim bağlamı" },
] as const;

export const ENERGY_EQUIPMENT_FILTER_OPTIONS = [
  { value: "", label: "Tüm ekipmanlar" },
  { value: "injection-machine", label: "Enjeksiyon makinesi" },
  { value: "chiller", label: "Soğutma grubu" },
  { value: "compressor", label: "Kompresör" },
  { value: "material-dryer", label: "Malzeme kurutucu" },
  { value: "other", label: "Diğer" },
] as const;

export function getEnergyMeasurementScopeLabel(value: string): string {
  return ENERGY_MEASUREMENT_SCOPE_LABELS[value] ?? "Bilinmeyen kapsam";
}

export function getEnergyEquipmentCategoryLabel(value: string): string {
  return ENERGY_EQUIPMENT_CATEGORY_LABELS[value] ?? "Diğer ekipman";
}

export function getEnergyEvidenceLabel(value: string | null | undefined): string {
  if (!value) return "Bilinmiyor";
  return ENERGY_EVIDENCE_LABELS[value] ?? "Bilinmiyor";
}

export function getEnergyFreshnessLabel(value: string | null | undefined): string {
  if (!value) return "Bilinmiyor";
  return ENERGY_FRESHNESS_LABELS[value] ?? "Bilinmiyor";
}

export function getEnergyStatusLabel(value: string): string {
  return ENERGY_STATUS_LABELS[value] ?? "Bilinmiyor";
}

export function getEnergyOutcomeLabel(value: string): string {
  return ENERGY_OUTCOME_LABELS[value] ?? "Bilinmeyen sonuç";
}

export function getEnergyDriverLabel(value: string, fallback?: string | null): string {
  return ENERGY_DRIVER_LABELS[value] ?? fallback ?? "Bilinmeyen sürücü";
}

export function getEnergyTargetMetricLabel(value: string): string {
  return ENERGY_TARGET_METRIC_LABELS[value] ?? value;
}

export function localizeEnergyUnit(unit: string, targetMetric?: string): string {
  if (targetMetric) {
    return getEnergyTargetMetricLabel(targetMetric);
  }

  const normalized = unit.trim().toLowerCase();
  if (
    normalized === "kwh/1000-good-parts"
    || normalized.includes("1000-good-parts")
  ) {
    return "kWh/1.000 iyi parça";
  }

  if (normalized === "kwh/1000-parts" || normalized.includes("1000-parts")) {
    return "kWh/1.000 parça";
  }

  return unit;
}

export function getEnergyAllocationCategoryLabel(
  equipmentCategory: string | null | undefined,
): string {
  if (!equipmentCategory) {
    return ENERGY_ALLOCATION_CATEGORY_LABELS.unallocated;
  }

  return ENERGY_ALLOCATION_CATEGORY_LABELS[equipmentCategory as keyof typeof ENERGY_ALLOCATION_CATEGORY_LABELS]
    ?? getEnergyEquipmentCategoryLabel(equipmentCategory);
}

export function getEnergyTargetScopeLabel(value: string): string {
  return ENERGY_TARGET_SCOPE_LABELS[value] ?? getEnergyMeasurementScopeLabel(value);
}

export function getEnergyTargetStatusLabel(value: string): string {
  return ENERGY_TARGET_STATUS_LABELS[value] ?? value;
}

export function getEnergyBaselineDimensionLabel(value: string): string {
  return ENERGY_BASELINE_DIMENSION_LABELS[value] ?? value;
}

export function getEnergyCalculationMethodLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return ENERGY_CALCULATION_METHOD_LABELS[value] ?? value;
}

export function getEnergySeverityLabel(value: string): string {
  switch (value) {
    case "pass":
      return "Uygun";
    case "warn":
      return "Uyarı";
    case "fail":
      return "Kritik";
    case "info":
      return "Bilgi";
    default:
      return "Bilinmiyor";
  }
}

export const ENERGY_ASSESSMENT_TITLE_LABELS: Readonly<Record<string, string>> = {
  "ASSESS-WITHIN-TARGET": "Hedef içinde değerlendirme",
  "ASSESS-PEAK-DEMAND": "Tepe güç değerlendirmesi",
  "ASSESS-IDLE-CONSUMPTION": "Boşta tüketim değerlendirmesi",
  "ASSESS-ENERGY-INTENSITY": "Enerji yoğunluğu değerlendirmesi",
  "ASSESS-INSUFFICIENT-DATA": "Veri yetersizliği değerlendirmesi",
  "ASSESS-SOURCE-CONFLICT": "Kaynak çelişkisi değerlendirmesi",
};

export function getEnergyAssessmentTitle(
  assessmentReference: string,
  leadingDriverCode?: string | null,
): string {
  const mapped = ENERGY_ASSESSMENT_TITLE_LABELS[assessmentReference];
  if (mapped) {
    return mapped;
  }

  if (leadingDriverCode) {
    return `${getEnergyDriverLabel(leadingDriverCode)} değerlendirmesi`;
  }

  return "Enerji değerlendirmesi";
}

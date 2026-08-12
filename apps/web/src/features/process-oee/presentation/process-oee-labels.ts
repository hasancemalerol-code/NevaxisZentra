export const PROCESS_OEE_STATUS_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "calculated", label: "Hesaplandı" },
  { value: "insufficient-data", label: "Veri yetersiz" },
  { value: "review-required", label: "İnceleme gerekli" },
  { value: "stale-data", label: "Bayat veri" },
] as const;

export function getProcessOeeStatusLabel(status: string, statusLabel?: string): string {
  if (statusLabel) {
    return statusLabel;
  }

  switch (status) {
    case "calculated":
      return "Hesaplandı";
    case "insufficient-data":
      return "Veri yetersiz";
    case "review-required":
      return "İnceleme gerekli";
    case "stale-data":
      return "Bayat veri";
    default:
      return "Bilinmiyor";
  }
}

export function formatPercent(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return `${value.toLocaleString("tr-TR", {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  })}%`;
}

export function formatProcessOeeNumber(value?: number | null): string {
  if (value === undefined || value === null) {
    return "—";
  }

  return value.toLocaleString("tr-TR");
}

export function formatProcessOeeDateTime(value?: string | null): string {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatProcessOeeDateRange(startUtc: string, endUtc: string): string {
  const start = formatProcessOeeDateTime(startUtc);
  const end = formatProcessOeeDateTime(endUtc);
  if (start === "—" || end === "—") {
    return "—";
  }

  return `${start} – ${end}`;
}

export function formatMachineLabel(
  machineCode?: string,
  machineDisplayName?: string,
): string {
  const parts = [machineCode, machineDisplayName].filter(Boolean);
  return parts.length > 0 ? parts.join(" · ") : "—";
}

export function formatDataSufficiencyPercent(
  calculatedCount: number,
  assessmentCount: number,
): string {
  if (assessmentCount <= 0) {
    return "—";
  }

  const percent = (calculatedCount / assessmentCount) * 100;
  return formatPercent(percent);
}

export function getDriverSeverityLabel(severity: string): string {
  switch (severity) {
    case "pass":
      return "Geçti";
    case "warn":
      return "Uyarı";
    case "fail":
      return "Başarısız";
    case "critical":
    case "high":
      return "Yüksek";
    case "warning":
    case "medium":
      return "Orta";
    case "info":
    case "informational":
    case "low":
      return "Bilgi";
    default:
      return "Bilinmiyor";
  }
}

export function getLossCategoryLabel(category: string): string {
  switch (category) {
    case "availability-loss":
      return "Kullanılabilirlik kaybı";
    case "performance-loss":
      return "Performans kaybı";
    case "quality-loss":
      return "Kalite kaybı";
    case "insufficient-data":
      return "Veri yetersiz";
    case "source-conflict":
      return "Kaynak çakışması";
    default:
      return "Bilinmiyor";
  }
}

export function getLossFreshnessLabel(state: string): string {
  switch (state) {
    case "fresh":
      return "Güncel";
    case "stale":
      return "Bayat";
    case "unknown":
      return "Bilinmiyor";
    default:
      return "Bilinmiyor";
  }
}

export function getLossExplanationLabel(code: string): string {
  switch (code) {
    case "downtime-reduces-operating-time":
      return "Duruş süreleri planlı çalışma süresinden düşülerek kullanılabilirlik kaybını açıklar.";
    case "downtime-evidence-missing":
      return "Duruş kanıtı eksik; sıfır duruş varsayılmaz.";
    case "planned-production-time-missing":
      return "Planlı üretim süresi olmadan kullanılabilirlik kaybı hesaplanamaz.";
    case "performance-loss-slow-cycle":
      return "Gözlemlenen çevrimler ideal çevrimden yavaş kaldığı için performans kaybı oluştu.";
    case "performance-loss-from-derived-shots":
      return "Performans kaybı türetilmiş vuruş sayısına dayanır.";
    case "performance-above-100-review":
      return "Ham performans %100 üzerinde; inceleme gerekir.";
    case "performance-evidence-insufficient":
      return "Performans kaybı için yeterli çevrim ve çalışma süresi kanıtı yok.";
    case "scrap-reduces-quality":
      return "Hurda/adet kaybı kalite bileşenini düşürür.";
    case "quality-quantity-reconciliation-conflict":
      return "İyi + hurda toplam parça ile uyuşmuyor.";
    case "quality-evidence-insufficient":
      return "Kalite miktar kanıtı eksik; sıfır hurda varsayılmaz.";
    case "source-evidence-conflict":
      return "Kaynak sistemleri çelişkili değerler üretti.";
    case "missing-required-evidence":
      return "Zorunlu kanıt alanları eksik.";
    case "stale-evidence":
      return "Kaynak veriler bayat; sonuçlar sınırlı güvenilirlikte.";
    default:
      return "Kayıp sürücüsü mevcut kanıtlara dayanır.";
  }
}

export function getStabilityResultStateLabel(state: string): string {
  switch (state) {
    case "stable":
      return "Kararlı";
    case "variable":
      return "Değişken";
    case "drifting":
      return "Kayıyor";
    case "review-required":
      return "İnceleme gerekli";
    case "insufficient-data":
      return "Yetersiz veri";
    default:
      return "Bilinmiyor";
  }
}

export function getStabilityCategoryLabel(category: string): string {
  switch (category) {
    case "cycle-variability":
      return "Çevrim değişkenliği";
    case "cycle-drift":
      return "Çevrim kayması";
    case "micro-stop":
      return "Mikro duruş";
    case "downtime-concentration":
      return "Duruş yoğunlaşması";
    case "shift-comparison":
      return "Vardiya karşılaştırması";
    case "insufficient-data":
      return "Yetersiz veri";
    case "source-conflict":
      return "Kaynak çakışması";
    default:
      return "Proses kararlılığı";
  }
}

export function getDriftDirectionLabel(direction: string): string {
  switch (direction) {
    case "increasing":
      return "Artan";
    case "decreasing":
      return "Azalan";
    case "none":
      return "Belirgin kayma yok";
    default:
      return "Bilinmiyor";
  }
}

export function getDowntimeReasonLabel(reasonCode: string): string {
  if (reasonCode === "unknown") {
    return "Bilinmeyen";
  }
  return reasonCode.replaceAll("-", " ");
}

export function getStabilityExplanationLabel(code: string): string {
  switch (code) {
    case "robust-dispersion-stable":
      return "Medyan ve MAD oranı kararlı eşiğin altında.";
    case "robust-dispersion-variable":
      return "Medyan ve MAD oranı yüksek değişkenlik gösteriyor.";
    case "early-late-median-increase":
      return "Geç pencere medyanı erken pencereye göre belirgin şekilde arttı.";
    case "early-late-median-decrease":
      return "Geç pencere medyanı erken pencereye göre belirgin şekilde azaldı.";
    case "insufficient-cycle-samples":
      return "Kararlılık için yeterli iyi kalite çevrim örneği yok.";
    case "stale-cycle-evidence":
      return "Çevrim kanıtı bayat; normal sonuç üretilmedi.";
    case "micro-stop-episodes-detected":
      return "Mikro duruşlar güvenilir duruş kanıtından tespit edildi.";
    case "source-evidence-conflict":
      return "Kaynak çakışması nedeniyle inceleme gerekli.";
    default:
      return "Sonuç mevcut operasyonel kanıta dayanır.";
  }
}

export function getComparabilityWarningLabel(code: string | null | undefined): string | null {
  if (!code) {
    return null;
  }
  switch (code) {
    case "facility-or-evidence-incomparable":
      return "Tesis veya kanıt yeterliliği farklı; sıralama yapılmadı.";
    case "no-shift-windows":
      return "Karşılaştırılabilir vardiya penceresi bulunamadı.";
    default:
      return "Karşılaştırma sınırlı güvenilirlikte.";
  }
}

export function getDataSufficiencyLabel(value: string): string {
  switch (value) {
    case "sufficient":
      return "Yeterli";
    case "insufficient":
      return "Yetersiz";
    default:
      return "Bilinmiyor";
  }
}

export function formatCycleMs(value: number | null): string {
  if (value == null) {
    return "—";
  }
  if (value >= 1000) {
    return `${(value / 1000).toFixed(1)} sn`;
  }
  return `${value} ms`;
}

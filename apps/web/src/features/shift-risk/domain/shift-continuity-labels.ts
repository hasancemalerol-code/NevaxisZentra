export const SHIFT_CONTINUITY_RULE_SET_VERSION = "SHIFT-WORKFORCE-CONTINUITY-RISK-V1";

export const LOCAL_PILOT_DISCLAIMER =
  "Örnek veri. Gerçek ERP/HR veya canlı personel sürekliliği doğrulaması değildir.";

export const HUMAN_REVIEW_MESSAGE =
  "Bu değerlendirme yalnızca karar desteği sağlar. Operatör atama veya üretim planı değişikliği için yetkili insan onayı gereklidir.";

export const SHIFT_CONTINUITY_OUTCOME_FILTER_OPTIONS = [
  { value: "", label: "Tüm sonuçlar" },
  { value: "clear", label: "Temiz" },
  { value: "attention", label: "Dikkat" },
  { value: "critical", label: "Kritik" },
  { value: "insufficient-data", label: "Yetersiz veri" },
] as const;

export const SHIFT_CONTINUITY_CATEGORY_FILTER_OPTIONS = [
  { value: "", label: "Tüm kategoriler" },
  { value: "absence-impact", label: "İzin/yokluk etkisi" },
  { value: "availability-unknown", label: "Müsaitlik bilinmiyor" },
  { value: "competency-expired", label: "Süresi dolmuş yetkinlik" },
  { value: "competency-expiring", label: "Yaklaşan yetkinlik süresi" },
  { value: "competency-pending-verification", label: "Doğrulama bekleyen yetkinlik" },
  { value: "source-stale", label: "Bayat kaynak" },
  { value: "assignment-overlap", label: "Atama çakışması" },
  { value: "assignment-load-pressure", label: "Atama yükü" },
  { value: "coverage-impact", label: "Kapsama etkisi" },
  { value: "evidence-conflict", label: "Çelişkili kanıt" },
] as const;

export const SHIFT_CONTINUITY_AVAILABILITY_FILTER_OPTIONS = [
  { value: "", label: "Tüm müsaitlik" },
  { value: "available", label: "Müsait" },
  { value: "leave", label: "İzinli" },
  { value: "absent", label: "Devamsız" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const SHIFT_CONTINUITY_FRESHNESS_FILTER_OPTIONS = [
  { value: "", label: "Tüm tazelik" },
  { value: "fresh", label: "Güncel" },
  { value: "stale", label: "Bayat" },
  { value: "unknown", label: "Bilinmiyor" },
] as const;

export const SHIFT_CONTINUITY_COMPETENCY_FILTER_OPTIONS = [
  { value: "", label: "Tüm yetkinlikler" },
  { value: "injection-machine-operation", label: "Enjeksiyon makinesi operasyonu" },
  { value: "mold-change-setup", label: "Kalıp değişimi ve kurulum" },
  { value: "process-parameter-review", label: "Proses parametre inceleme" },
  { value: "material-handling", label: "Malzeme taşıma" },
  { value: "robot-auxiliary-equipment", label: "Robot ve yardımcı ekipman" },
  { value: "quality-check", label: "Kalite kontrol" },
  { value: "maintenance-support", label: "Bakım desteği" },
] as const;

export const SHIFT_CONTINUITY_SHIFT_FILTER_OPTIONS = [
  { value: "", label: "Tüm vardiyalar" },
  { value: "bb000001-0040-4000-8000-000000000040", label: "Gündüz Vardiyası (V1)" },
] as const;

export function getShiftContinuityOutcomeLabel(outcome: string, outcomeLabel?: string): string {
  if (outcomeLabel) return outcomeLabel;
  switch (outcome) {
    case "clear":
      return "Temiz";
    case "attention":
      return "Dikkat";
    case "critical":
      return "Kritik";
    case "insufficient-data":
      return "Yetersiz veri";
    default:
      return "Bilinmiyor";
  }
}

export function getShiftContinuityCategoryLabel(category: string, categoryLabel?: string): string {
  if (categoryLabel) return categoryLabel;
  const match = SHIFT_CONTINUITY_CATEGORY_FILTER_OPTIONS.find((item) => item.value === category);
  return match?.label ?? "Risk kategorisi";
}

export function getShiftContinuitySeverityLabel(severity: string, severityLabel?: string): string {
  if (severityLabel) return severityLabel;
  switch (severity) {
    case "pass":
      return "Uygun";
    case "warn":
      return "Uyarı";
    case "fail":
      return "Başarısız";
    case "info":
      return "Bilgi";
    default:
      return "—";
  }
}

export function getShiftContinuityReasonLabel(reasonCode?: string, label?: string): string {
  if (label) return label;
  if (!reasonCode) return "—";
  const match = SHIFT_CONTINUITY_CATEGORY_FILTER_OPTIONS.find((item) => item.value === reasonCode);
  if (match) return match.label;
  switch (reasonCode) {
    case "continuity-clear":
      return "Süreklilik kanıtı yeterli";
    case "competency-expiring-urgent":
      return "Yakında süresi dolacak yetkinlik (14 gün)";
    case "competency-expiring-upcoming":
      return "Yaklaşan yetkinlik süresi (30 gün)";
    case "inactive-employment":
      return "Pasif istihdam";
    default:
      return "Personel sürekliliği risk nedeni";
  }
}

export function formatShiftContinuityCount(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }
  return value.toLocaleString("tr-TR");
}

export function formatShiftContinuityDateTime(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatShiftContinuityDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

export function outcomeBadgeClass(outcome: string): string {
  switch (outcome) {
    case "clear":
      return "shift-workforce-badge shift-workforce-badge--active";
    case "attention":
      return "shift-workforce-badge shift-workforce-badge--warning";
    case "critical":
      return "shift-workforce-badge shift-workforce-badge--inactive";
    case "insufficient-data":
      return "shift-workforce-badge shift-workforce-badge--unknown";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}

export function severityBadgeClass(severity: string): string {
  switch (severity) {
    case "pass":
      return "shift-workforce-badge shift-workforce-badge--active";
    case "warn":
      return "shift-workforce-badge shift-workforce-badge--warning";
    case "fail":
      return "shift-workforce-badge shift-workforce-badge--inactive";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}

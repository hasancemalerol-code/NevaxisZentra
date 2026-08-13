export const SHIFT_RISK_RULE_SET_VERSION = "SHIFT-COVERAGE-RISK-V1";

export const LOCAL_PILOT_DISCLAIMER =
  "Deterministik yerel pilot verisi. Gerçek ERP/HR veya canlı vardiya riski doğrulaması değildir.";

export const HUMAN_REVIEW_MESSAGE =
  "Bu değerlendirme yalnızca karar desteği sağlar. Operatör atama veya üretim planı değişikliği için yetkili insan onayı gereklidir.";

export const SHIFT_RISK_OUTCOME_FILTER_OPTIONS = [
  { value: "", label: "Tüm sonuçlar" },
  { value: "clear", label: "Temiz" },
  { value: "attention", label: "Dikkat" },
  { value: "critical", label: "Kritik" },
  { value: "insufficient-data", label: "Yetersiz veri" },
] as const;

export const SHIFT_RISK_CATEGORY_FILTER_OPTIONS = [
  { value: "", label: "Tüm kategoriler" },
  { value: "coverage-gap", label: "Kapsama açığı" },
  { value: "critical-role-gap", label: "Kritik rol açığı" },
  { value: "assignment-conflict", label: "Atama çakışması" },
  { value: "assignment-ineligible", label: "Uygun olmayan atama" },
  { value: "requirement-missing", label: "Gereksinim eksik" },
  { value: "source-stale", label: "Bayat kaynak" },
  { value: "evidence-conflict", label: "Kanıt çakışması" },
] as const;

export function getShiftRiskOutcomeLabel(outcome: string, outcomeLabel?: string): string {
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

export function getShiftRiskCategoryLabel(category: string, categoryLabel?: string): string {
  if (categoryLabel) return categoryLabel;
  const match = SHIFT_RISK_CATEGORY_FILTER_OPTIONS.find((item) => item.value === category);
  return match?.label ?? "Risk kategorisi";
}

export function getShiftRiskSeverityLabel(severity: string, severityLabel?: string): string {
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

export function formatShiftRiskCount(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }
  return value.toLocaleString("tr-TR");
}

export function formatShiftRiskDateTime(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
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

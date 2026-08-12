import { formatMoldDateTime, formatMoldNumber } from "./mold-labels";

export function getMoldCompatibilityStatusLabel(status: string): string {
  switch (status) {
    case "compatible":
      return "Uyumlu";
    case "conditional":
      return "Koşullu";
    case "incompatible":
      return "Uyumsuz";
    case "insufficient-data":
      return "Yetersiz Veri";
    default:
      return "Bilinmiyor";
  }
}

export function getMoldCompatibilityRuleStatusLabel(status: string): string {
  switch (status) {
    case "pass":
      return "Geçti";
    case "warn":
      return "Uyarı";
    case "fail":
      return "Başarısız";
    case "unknown":
      return "Bilinmiyor";
    default:
      return "Bilinmiyor";
  }
}

export function formatTheoreticalPartsPerHour(value?: number): string {
  if (value === undefined || value === null || Number.isNaN(value)) {
    return "—";
  }

  return `${formatMoldNumber(Math.round(value))} parça/saat`;
}

export function formatCompatibilityTimestamp(value?: string): string {
  if (!value) {
    return "—";
  }

  return formatMoldDateTime(value);
}

export const MOLD_COMPATIBILITY_THEORETICAL_CAPACITY_DISCLAIMER =
  "Gerçek üretim kapasitesi veya OEE değildir.";

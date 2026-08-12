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
    case "critical":
      return "Kritik";
    case "warning":
      return "Uyarı";
    case "info":
      return "Bilgi";
    default:
      return "Bilinmiyor";
  }
}

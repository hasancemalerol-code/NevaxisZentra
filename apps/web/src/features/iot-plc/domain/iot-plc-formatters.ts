import { formatDateTime } from "@/lib/presentation/format";

const TR_LOCALE = "tr-TR";

/** Timestamps are UTC instants; they are shown in facility local time through the central formatter. */
export function formatIotPlcDateTime(utc: string | null | undefined): string {
  if (!utc) return "—";
  // A UTC field without a zone designator would otherwise be read as server-local time.
  const instant = /T\d{2}:\d{2}(:\d{2}(\.\d+)?)?$/.test(utc) ? `${utc}Z` : utc;
  return formatDateTime(instant);
}

export function formatIotPlcCompactPeriod(startUtc: string, endUtc: string): string {
  return `${formatIotPlcDateTime(startUtc)} – ${formatIotPlcDateTime(endUtc)}`;
}

export function formatIotPlcCount(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  return new Intl.NumberFormat(TR_LOCALE, { maximumFractionDigits: 0 }).format(value);
}

/** API ratios are 0–1 fractions; presentation uses percent labels. */
export function formatIotPlcRatioPercent(ratio: number | null | undefined): string {
  if (ratio === null || ratio === undefined || Number.isNaN(ratio)) return "—";
  return new Intl.NumberFormat(TR_LOCALE, {
    style: "percent",
    maximumFractionDigits: 1,
    minimumFractionDigits: 0,
  }).format(ratio);
}

export function formatIotPlcSeconds(seconds: number | null | undefined): string {
  if (seconds === null || seconds === undefined || Number.isNaN(seconds)) return "—";
  if (seconds < 60) return `${Math.round(seconds)} sn`;
  if (seconds < 3600) return `${Math.round(seconds / 60)} dk`;
  return `${(seconds / 3600).toFixed(1)} sa`;
}

const DATE_TIME_LOCAL_FORMATTER = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeStyle: "short",
  timeZone: "Europe/Istanbul",
});

const DATE_LOCAL_FORMATTER = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeZone: "Europe/Istanbul",
});

const COUNT_FORMATTER = new Intl.NumberFormat("tr-TR");

const CURRENCY_FORMATTER_CACHE = new Map<string, Intl.NumberFormat>();

function currencyFormatter(currencyCode: string): Intl.NumberFormat {
  const cached = CURRENCY_FORMATTER_CACHE.get(currencyCode);
  if (cached) return cached;
  const formatter = new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: currencyCode,
    maximumFractionDigits: 2,
  });
  CURRENCY_FORMATTER_CACHE.set(currencyCode, formatter);
  return formatter;
}

export function formatCount(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  return COUNT_FORMATTER.format(value);
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "—";
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) return "—";
  return DATE_TIME_LOCAL_FORMATTER.format(parsed);
}

export function formatDate(value: string | null | undefined): string {
  if (!value) return "—";
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) return "—";
  return DATE_LOCAL_FORMATTER.format(parsed);
}

export function formatCompactPeriod(
  startUtc: string | null | undefined,
  endUtc: string | null | undefined,
): string {
  if (!startUtc || !endUtc) return "—";
  return `${formatDate(startUtc)} – ${formatDate(endUtc)}`;
}

/** Decimal ratio from API; multiply by 100 exactly once for display. Never clamps. */
export function formatRatioAsPercent(ratio: number | null | undefined): string {
  if (ratio === null || ratio === undefined || Number.isNaN(ratio)) return "—";
  const percent = ratio * 100;
  const formatted = new Intl.NumberFormat("tr-TR", {
    style: "decimal",
    maximumFractionDigits: 1,
    signDisplay: "exceptZero",
  }).format(percent);
  const sign = formatted.startsWith("+") || formatted.startsWith("-") ? formatted[0] : "";
  return `${sign}%${sign ? formatted.slice(1) : formatted}`;
}

/** Incidence fraction 0–1 → display percent without inventing zero for null. */
export function formatIncidenceFraction(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "Hesaplanamadı";
  const percent = value * 100;
  return `%${new Intl.NumberFormat("tr-TR", {
    style: "decimal",
    maximumFractionDigits: 0,
  }).format(percent)}`;
}

/** Signed incidence delta −1..1 as percentage points for display only. */
export function formatIncidenceDeltaPoints(delta: number | null | undefined): string {
  if (delta === null || delta === undefined || Number.isNaN(delta)) return "Hesaplanamadı";
  const points = delta * 100;
  const formatted = new Intl.NumberFormat("tr-TR", {
    style: "decimal",
    maximumFractionDigits: 1,
    signDisplay: "exceptZero",
  }).format(points);
  return `${formatted} yüzde puan`;
}

export function formatNullableCount(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "Hesaplanamadı";
  return formatCount(value);
}

export function formatRemainingValidityDays(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "Belirtilmemiş";
  return `${formatCount(value)} gün`;
}

export function formatCurrency(
  value: number | null | undefined,
  currencyCode: string | null | undefined,
): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  if (!currencyCode?.trim()) return COUNT_FORMATTER.format(value);
  try {
    return currencyFormatter(currencyCode).format(value);
  } catch {
    return `${COUNT_FORMATTER.format(value)} ${currencyCode}`;
  }
}

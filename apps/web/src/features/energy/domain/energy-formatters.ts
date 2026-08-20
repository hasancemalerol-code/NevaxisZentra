const MISSING = "—";

export function formatEnergyNumber(
  value: number | null | undefined,
  fractionDigits = 1,
): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return MISSING;
  }

  return new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: 0,
    maximumFractionDigits: fractionDigits,
  }).format(value);
}

export function formatEnergyKwh(value: number | null | undefined): string {
  return formatEnergyNumber(value, 1);
}

export function formatEnergyKw(value: number | null | undefined): string {
  return formatEnergyNumber(value, 1);
}

export function formatEnergyPercent(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return MISSING;
  }

  const formatted = new Intl.NumberFormat("tr-TR", {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
    signDisplay: "exceptZero",
  }).format(value);

  return `${formatted}%`;
}

export function formatEnergyCurrency(
  amount: number | null | undefined,
  currencyCode: string | null | undefined,
): string {
  if (amount === null || amount === undefined || Number.isNaN(amount) || !currencyCode) {
    return MISSING;
  }

  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: currencyCode,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

export function formatEnergyDateTime(value: string | null | undefined): string {
  if (!value) return MISSING;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return MISSING;
  return date.toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatEnergyPeriod(
  startUtc: string | null | undefined,
  endUtc: string | null | undefined,
): string {
  if (!startUtc || !endUtc) return MISSING;
  const start = new Date(startUtc);
  const end = new Date(endUtc);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return MISSING;

  const opts: Intl.DateTimeFormatOptions = {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  };

  return `${start.toLocaleString("tr-TR", opts)} – ${end.toLocaleString("tr-TR", opts)}`;
}

export function formatEnergyCompactPeriod(
  startUtc: string,
  endUtc: string,
): string {
  return formatEnergyPeriod(startUtc, endUtc);
}

export function formatEnergyTrendAxisLabel(startUtc: string): string {
  const date = new Date(startUtc);
  if (Number.isNaN(date.getTime())) {
    return MISSING;
  }

  return date.toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatEnergyVariance(
  varianceKwh: number | null | undefined,
  variancePercent: number | null | undefined,
): string {
  if (varianceKwh === null || varianceKwh === undefined) {
    return MISSING;
  }

  const kwh = `${formatEnergyKwh(varianceKwh)} kWh`;
  if (variancePercent === null || variancePercent === undefined) {
    return kwh;
  }

  return `${kwh} (${formatEnergyPercent(variancePercent)})`;
}

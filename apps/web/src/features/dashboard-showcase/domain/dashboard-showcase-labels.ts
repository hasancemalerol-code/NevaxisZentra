export function formatShowcaseNumber(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return "—";
  }

  return value.toLocaleString("tr-TR", {
    maximumFractionDigits: 0,
  });
}

export function formatShowcasePercent(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return "—";
  }

  return `%${value.toLocaleString("tr-TR", {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  })}`;
}

export function formatShowcaseCycleSeconds(milliseconds: number | null | undefined): string {
  if (milliseconds === null || milliseconds === undefined || Number.isNaN(milliseconds)) {
    return "—";
  }

  const seconds = milliseconds / 1000;
  return `${seconds.toLocaleString("tr-TR", {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  })} sn`;
}

export function formatShowcaseRefreshLabel(isoTimestamp: string | null): string {
  if (!isoTimestamp) {
    return "—";
  }

  const refreshedAt = new Date(isoTimestamp);
  if (Number.isNaN(refreshedAt.getTime())) {
    return "—";
  }

  const diffMs = Date.now() - refreshedAt.getTime();
  if (diffMs < 15_000) {
    return "şimdi";
  }

  return refreshedAt.toLocaleTimeString("tr-TR", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

export const DISTRIBUTION_GROUP_LABELS = [
  "Ürün A",
  "Ürün B",
  "Ürün C",
  "Diğer",
] as const;

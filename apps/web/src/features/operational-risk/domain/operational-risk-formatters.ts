const DATE_TIME_FORMATTER = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeStyle: "short",
});

const DATE_FORMATTER = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
});

const COUNT_FORMATTER = new Intl.NumberFormat("tr-TR");

export function formatOperationalRiskCount(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "—";
  return COUNT_FORMATTER.format(value);
}

export function formatOperationalRiskDateTime(value: string | null | undefined): string {
  if (!value) return "—";
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) return "—";
  return DATE_TIME_FORMATTER.format(parsed);
}

export function formatOperationalRiskDate(value: string | null | undefined): string {
  if (!value) return "—";
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) return "—";
  return DATE_FORMATTER.format(parsed);
}

export function formatOperationalRiskCompactPeriod(
  startUtc: string | null | undefined,
  endUtc: string | null | undefined,
): string {
  if (!startUtc || !endUtc) return "—";
  return `${formatOperationalRiskDate(startUtc)} – ${formatOperationalRiskDate(endUtc)}`;
}

export function formatOperationalRiskPercent(ratio: number | null | undefined): string {
  if (ratio === null || ratio === undefined || Number.isNaN(ratio)) return "—";
  return new Intl.NumberFormat("tr-TR", {
    style: "percent",
    maximumFractionDigits: 0,
  }).format(ratio);
}

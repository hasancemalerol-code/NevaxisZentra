import { formatDateTime } from "@/lib/presentation/format";
const INTEGER_FORMATTER = new Intl.NumberFormat(
  "tr-TR",
  {
    maximumFractionDigits: 0,
  },
);

const PERCENT_FORMATTER = new Intl.NumberFormat(
  "tr-TR",
  {
    style: "percent",
    maximumFractionDigits: 0,
  },
);

export function formatAiTaskDateTime(
  value: string | undefined,
): string {
  if (!value) {
    return "Tanımlanmadı";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Geçersiz tarih";
  }

  return formatDateTime(date);
}

export function formatAiTaskShortDateTime(
  value: string | undefined,
): string {
  if (!value) {
    return "Tanımlanmadı";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Geçersiz tarih";
  }

  return formatDateTime(date);
}

export function formatAiTaskInteger(
  value: number | undefined,
): string {
  if (value === undefined) {
    return "Tanımlanmadı";
  }

  return INTEGER_FORMATTER.format(value);
}

export function formatAiTaskPercentage(
  value: number | undefined,
): string {
  if (value === undefined) {
    return "Tanımlanmadı";
  }

  return PERCENT_FORMATTER.format(value);
}

export function formatAiTaskMoney(
  amount: number | undefined,
  currency: string | undefined,
): string {
  if (
    amount === undefined ||
    !currency
  ) {
    return "Tanımlanmadı";
  }

  try {
    return new Intl.NumberFormat(
      "tr-TR",
      {
        style: "currency",
        currency,
        maximumFractionDigits: 0,
      },
    ).format(amount);
  } catch {
    return `${INTEGER_FORMATTER.format(
      amount,
    )} ${currency}`;
  }
}

export function formatAiTaskDurationMinutes(
  minutes: number | undefined,
): string {
  if (minutes === undefined) {
    return "Tanımlanmadı";
  }

  if (minutes < 60) {
    return `${INTEGER_FORMATTER.format(
      minutes,
    )} dakika`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  if (remainingMinutes === 0) {
    return `${INTEGER_FORMATTER.format(
      hours,
    )} saat`;
  }

  return `${INTEGER_FORMATTER.format(
    hours,
  )} saat ${INTEGER_FORMATTER.format(
    remainingMinutes,
  )} dakika`;
}

export function formatAiTaskQuantity(
  quantity: number | undefined,
  unit: string | undefined,
): string {
  if (
    quantity === undefined ||
    !unit
  ) {
    return "Tanımlanmadı";
  }

  return `${INTEGER_FORMATTER.format(
    quantity,
  )} ${unit}`;
}
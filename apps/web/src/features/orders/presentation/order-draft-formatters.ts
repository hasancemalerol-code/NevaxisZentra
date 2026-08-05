export function formatOrderConfidence(
  value: number,
): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "percent",
    maximumFractionDigits: 0,
  }).format(value);
}

export function formatOrderDateTime(
  value: string | undefined,
): string {
  if (!value) {
    return "Tanımlanmadı";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatOrderShortDateTime(
  value: string | undefined,
): string {
  if (!value) {
    return "Tanımlanmadı";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatOrderDate(
  value: string | undefined,
): string {
  if (!value) {
    return "Tanımlanmadı";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
  }).format(new Date(value));
}

export function formatOrderMoney(
  value: number | undefined,
  currency: string | undefined,
): string {
  if (value === undefined) {
    return "Tanımlanmadı";
  }

  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency: currency ?? "TRY",
    maximumFractionDigits: 2,
  }).format(value);
}

export function formatOrderQuantity(
  value: number | undefined,
  unit: string | undefined,
): string {
  if (value === undefined) {
    return "Tanımlanmadı";
  }

  const formattedValue = new Intl.NumberFormat(
    "tr-TR",
    {
      maximumFractionDigits: 2,
    },
  ).format(value);

  return unit
    ? `${formattedValue} ${unit}`
    : formattedValue;
}
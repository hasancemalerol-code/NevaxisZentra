export type CommercialProfitabilityCommandCenterFilters = Readonly<{
  windowFrom: string;
  windowTo: string;
}>;

const FILTER_KEYS = ["windowFrom", "windowTo"] as const;

export function readCommercialProfitabilityCommandCenterFilters(
  searchParams: URLSearchParams,
): CommercialProfitabilityCommandCenterFilters {
  return {
    windowFrom: searchParams.get("windowFrom") ?? "",
    windowTo: searchParams.get("windowTo") ?? "",
  };
}

export function buildCommercialProfitabilityCommandCenterFilterParams(
  current: URLSearchParams,
  updates: Partial<Record<(typeof FILTER_KEYS)[number], string>>,
): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const [key, value] of Object.entries(updates)) {
    if (!value) params.delete(key);
    else params.set(key, value);
  }
  return params;
}

export function clearCommercialProfitabilityCommandCenterFilterParams(
  current: URLSearchParams,
): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const key of FILTER_KEYS) params.delete(key);
  return params;
}

export function buildCommercialProfitabilityCommandCenterTimeWindowLabel(
  filters: CommercialProfitabilityCommandCenterFilters,
): string {
  if (filters.windowFrom && filters.windowTo) {
    return `${filters.windowFrom} – ${filters.windowTo}`;
  }
  if (filters.windowFrom) return `${filters.windowFrom} – …`;
  if (filters.windowTo) return `… – ${filters.windowTo}`;
  return "Tüm dönem";
}

export function toCommercialProfitabilityCommandCenterComparisonFilters(
  filters: CommercialProfitabilityCommandCenterFilters,
): Readonly<{
  windowFromUtc?: string;
  windowToUtc?: string;
}> {
  return {
    windowFromUtc: filters.windowFrom ? `${filters.windowFrom}T00:00:00.000Z` : undefined,
    windowToUtc: filters.windowTo ? `${filters.windowTo}T23:59:59.999Z` : undefined,
  };
}

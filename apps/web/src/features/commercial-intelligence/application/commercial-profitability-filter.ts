export type CommercialProfitabilityComparisonFilters = Readonly<{
  comparisonType: string;
  profileId: string;
  materialId: string;
  customerReference: string;
  outcome: string;
  evidenceStatus: string;
  windowFrom: string;
  windowTo: string;
}>;

const FILTER_KEYS = [
  "comparisonType",
  "profileId",
  "materialId",
  "customerReference",
  "outcome",
  "evidenceStatus",
  "windowFrom",
  "windowTo",
] as const;

export function readCommercialProfitabilityComparisonFilters(
  searchParams: URLSearchParams,
): CommercialProfitabilityComparisonFilters {
  return {
    comparisonType: searchParams.get("comparisonType") ?? "",
    profileId: searchParams.get("profileId") ?? "",
    materialId: searchParams.get("materialId") ?? "",
    customerReference: searchParams.get("customerReference") ?? "",
    outcome: searchParams.get("outcome") ?? "",
    evidenceStatus: searchParams.get("evidenceStatus") ?? "",
    windowFrom: searchParams.get("windowFrom") ?? "",
    windowTo: searchParams.get("windowTo") ?? "",
  };
}

export function buildCommercialProfitabilityFilterParams(
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

export function clearCommercialProfitabilityFilterParams(current: URLSearchParams): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const key of FILTER_KEYS) params.delete(key);
  return params;
}

export function toCommercialProfitabilityApiFilters(
  filters: CommercialProfitabilityComparisonFilters,
): Readonly<{
  comparisonType?: string;
  profileId?: string;
  materialId?: string;
  customerReference?: string;
  outcome?: string;
  evidenceStatus?: string;
  windowFromUtc?: string;
  windowToUtc?: string;
}> {
  return {
    comparisonType: filters.comparisonType || undefined,
    profileId: filters.profileId || undefined,
    materialId: filters.materialId || undefined,
    customerReference: filters.customerReference || undefined,
    outcome: filters.outcome || undefined,
    evidenceStatus: filters.evidenceStatus || undefined,
    windowFromUtc: filters.windowFrom ? `${filters.windowFrom}T00:00:00.000Z` : undefined,
    windowToUtc: filters.windowTo ? `${filters.windowTo}T23:59:59.999Z` : undefined,
  };
}

export type CommercialProfitabilityComparisonApiFilters = ReturnType<typeof toCommercialProfitabilityApiFilters>;

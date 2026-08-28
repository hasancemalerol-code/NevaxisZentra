export type CommercialCustomerProfitabilityFilters = Readonly<{
  customerReference: string;
  profileId: string;
  outcome: string;
  evidenceStatus: string;
  windowFrom: string;
  windowTo: string;
  page: string;
}>;

const FILTER_KEYS = [
  "customerReference",
  "profileId",
  "outcome",
  "evidenceStatus",
  "windowFrom",
  "windowTo",
  "page",
] as const;

export function readCommercialCustomerProfitabilityFilters(
  searchParams: URLSearchParams,
): CommercialCustomerProfitabilityFilters {
  return {
    customerReference: searchParams.get("customerReference") ?? "",
    profileId: searchParams.get("profileId") ?? "",
    outcome: searchParams.get("outcome") ?? "",
    evidenceStatus: searchParams.get("evidenceStatus") ?? "",
    windowFrom: searchParams.get("windowFrom") ?? "",
    windowTo: searchParams.get("windowTo") ?? "",
    page: searchParams.get("page") ?? "1",
  };
}

export function buildCommercialCustomerProfitabilityFilterParams(
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

export function clearCommercialCustomerProfitabilityFilterParams(current: URLSearchParams): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const key of FILTER_KEYS) params.delete(key);
  return params;
}

export function toCommercialCustomerProfitabilityApiFilters(
  filters: CommercialCustomerProfitabilityFilters,
): CommercialCustomerProfitabilityApiFilters {
  const page = Number(filters.page);
  return {
    customerReference: filters.customerReference || undefined,
    profileId: filters.profileId || undefined,
    outcome: filters.outcome || undefined,
    evidenceStatus: filters.evidenceStatus || undefined,
    windowFromUtc: filters.windowFrom ? `${filters.windowFrom}T00:00:00.000Z` : undefined,
    windowToUtc: filters.windowTo ? `${filters.windowTo}T23:59:59.999Z` : undefined,
    page: Number.isFinite(page) && page > 0 ? page : 1,
    pageSize: 25,
  };
}

export type CommercialCustomerProfitabilityApiFilters = Readonly<{
  customerReference?: string;
  profileId?: string;
  outcome?: string;
  evidenceStatus?: string;
  windowFromUtc?: string;
  windowToUtc?: string;
  page: number;
  pageSize: number;
}>;

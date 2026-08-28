export type CommercialProfitabilityAlertFilters = Readonly<{
  category: string;
  severity: string;
  lifecycleStatus: string;
  evidenceStatus: string;
  comparisonType: string;
  page: string;
  alert: string;
}>;

const FILTER_KEYS = [
  "category",
  "severity",
  "lifecycleStatus",
  "evidenceStatus",
  "comparisonType",
  "page",
] as const;

export function readCommercialProfitabilityAlertFilters(
  searchParams: URLSearchParams,
): CommercialProfitabilityAlertFilters {
  return {
    category: searchParams.get("category") ?? "",
    severity: searchParams.get("severity") ?? "",
    lifecycleStatus: searchParams.get("lifecycleStatus") ?? "",
    evidenceStatus: searchParams.get("evidenceStatus") ?? "",
    comparisonType: searchParams.get("comparisonType") ?? "",
    page: searchParams.get("page") ?? "1",
    alert: searchParams.get("alert") ?? "",
  };
}

export function buildCommercialProfitabilityAlertFilterParams(
  current: URLSearchParams,
  updates: Partial<Record<(typeof FILTER_KEYS)[number] | "alert", string>>,
): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const [key, value] of Object.entries(updates)) {
    if (!value) params.delete(key);
    else params.set(key, value);
  }
  return params;
}

export function clearCommercialProfitabilityAlertFilterParams(current: URLSearchParams): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const key of FILTER_KEYS) params.delete(key);
  return params;
}

export function toCommercialProfitabilityAlertApiFilters(
  filters: CommercialProfitabilityAlertFilters,
): CommercialProfitabilityAlertApiFilters {
  const page = Number(filters.page);
  return {
    category: filters.category || undefined,
    severity: filters.severity || undefined,
    lifecycleStatus: filters.lifecycleStatus || undefined,
    evidenceStatus: filters.evidenceStatus || undefined,
    comparisonType: filters.comparisonType || undefined,
    page: Number.isFinite(page) && page > 0 ? page : 1,
    pageSize: 25,
  };
}

export type CommercialProfitabilityAlertApiFilters = Readonly<{
  category?: string;
  severity?: string;
  lifecycleStatus?: string;
  evidenceStatus?: string;
  comparisonType?: string;
  page: number;
  pageSize: number;
}>;

export type CommercialPricingScenarioFilters = Readonly<{
  lifecycleStatus: string;
  guardrailResult: string;
  profileId: string;
  materialId: string;
  customerReference: string;
  page: string;
  scenario: string;
}>;

const FILTER_KEYS = [
  "lifecycleStatus",
  "guardrailResult",
  "profileId",
  "materialId",
  "customerReference",
  "page",
] as const;

export function readCommercialPricingScenarioFilters(
  searchParams: URLSearchParams,
): CommercialPricingScenarioFilters {
  return {
    lifecycleStatus: searchParams.get("lifecycleStatus") ?? "",
    guardrailResult: searchParams.get("guardrailResult") ?? "",
    profileId: searchParams.get("profileId") ?? "",
    materialId: searchParams.get("materialId") ?? "",
    customerReference: searchParams.get("customerReference") ?? "",
    page: searchParams.get("page") ?? "1",
    scenario: searchParams.get("scenario") ?? "",
  };
}

export function buildCommercialPricingScenarioFilterParams(
  current: URLSearchParams,
  updates: Partial<Record<(typeof FILTER_KEYS)[number] | "scenario", string>>,
): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const [key, value] of Object.entries(updates)) {
    if (!value) params.delete(key);
    else params.set(key, value);
  }
  return params;
}

export function clearCommercialPricingScenarioFilterParams(current: URLSearchParams): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  for (const key of FILTER_KEYS) params.delete(key);
  return params;
}

export function toCommercialPricingScenarioApiFilters(
  filters: CommercialPricingScenarioFilters,
): CommercialPricingScenarioApiFilters {
  const page = Number(filters.page);
  return {
    lifecycleStatus: filters.lifecycleStatus || undefined,
    guardrailResult: filters.guardrailResult || undefined,
    profileId: filters.profileId || undefined,
    materialId: filters.materialId || undefined,
    customerReference: filters.customerReference || undefined,
    page: Number.isFinite(page) && page > 0 ? page : 1,
    pageSize: 25,
  };
}

export type CommercialPricingScenarioApiFilters = Readonly<{
  lifecycleStatus?: string;
  guardrailResult?: string;
  profileId?: string;
  materialId?: string;
  customerReference?: string;
  page: number;
  pageSize: number;
}>;

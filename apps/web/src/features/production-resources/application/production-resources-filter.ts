import type { ProductionResourceRowApi } from "../server/production-resources-api-client";

export type ProductionResourcesFilters = Readonly<{
  lineId?: string;
  resourceType?: string;
  state?: string;
  search?: string;
}>;

export function readProductionResourcesFilters(searchParams: URLSearchParams): ProductionResourcesFilters {
  return {
    lineId: searchParams.get("lineId") ?? undefined,
    resourceType: searchParams.get("resourceType") ?? undefined,
    state: searchParams.get("state") ?? undefined,
    search: searchParams.get("search") ?? undefined,
  };
}

export function buildProductionResourcesFilterParams(
  searchParams: URLSearchParams,
  updates: Partial<Record<keyof ProductionResourcesFilters, string>>,
): URLSearchParams {
  const params = new URLSearchParams(searchParams.toString());
  for (const [key, value] of Object.entries(updates)) {
    if (!value) {
      params.delete(key);
    } else {
      params.set(key, value);
    }
  }
  return params;
}

export function clearProductionResourcesFilterParams(searchParams: URLSearchParams): URLSearchParams {
  const params = new URLSearchParams(searchParams.toString());
  for (const key of ["lineId", "resourceType", "state", "search"]) {
    params.delete(key);
  }
  return params;
}

function matchesStateFilter(row: ProductionResourceRowApi, state?: string): boolean {
  if (!state) return true;
  const normalized = row.state.toUpperCase();
  if (state === "running") return normalized.includes("RUN");
  if (state === "idle") return normalized.includes("IDLE") || normalized.includes("STOP");
  if (state === "waiting") return normalized.includes("STARV") || normalized.includes("BLOCK");
  if (state === "stopped") return normalized.includes("STOP");
  if (state === "fault") return normalized.includes("FAULT");
  if (state === "risk") return row.isStarved || row.isBlocked || normalized.includes("FAULT");
  return true;
}

export function filterProductionResources(
  resources: readonly ProductionResourceRowApi[],
  filters: ProductionResourcesFilters,
): readonly ProductionResourceRowApi[] {
  const search = filters.search?.trim().toLowerCase();

  return resources.filter((row) => {
    if (filters.lineId && row.lineId !== filters.lineId) return false;
    if (filters.resourceType && row.resourceType !== filters.resourceType) return false;
    if (!matchesStateFilter(row, filters.state)) return false;
    if (search) {
      const haystack = [
        row.code,
        row.name,
        row.stationCode,
        row.lineCode,
        row.orderReference ?? "",
        row.operatorCode ?? "",
      ]
        .join(" ")
        .toLowerCase();
      if (!haystack.includes(search)) return false;
    }
    return true;
  });
}

export function hasActiveProductionResourcesFilters(filters: ProductionResourcesFilters): boolean {
  return Boolean(filters.lineId || filters.resourceType || filters.state || filters.search);
}

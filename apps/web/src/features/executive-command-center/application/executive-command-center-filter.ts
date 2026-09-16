import type {
  ExecutiveCommandCenterFilters,
  ExecutiveCommandCenterTimeScope,
} from "../types/executive-command-center-types";

export function readExecutiveCommandCenterFilters(
  searchParams: URLSearchParams,
): ExecutiveCommandCenterFilters {
  const timeScope = searchParams.get("timeScope");
  const normalizedScope: ExecutiveCommandCenterTimeScope =
    timeScope === "shift" || timeScope === "recent" ? timeScope : "today";

  return {
    severity: searchParams.get("severity") ?? undefined,
    outcomeType: searchParams.get("outcomeType") ?? undefined,
    timeScope: normalizedScope,
  };
}

export function buildExecutiveCommandCenterFilterParams(
  filters: ExecutiveCommandCenterFilters,
): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.severity) params.set("severity", filters.severity);
  if (filters.outcomeType) params.set("outcomeType", filters.outcomeType);
  if (filters.timeScope !== "today") params.set("timeScope", filters.timeScope);
  return params;
}

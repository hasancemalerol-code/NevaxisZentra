import type {
  ExecutiveCommandCenterFilters,
  ExecutiveCommandCenterTimeScope,
} from "../types/executive-command-center-types";

const EXECUTIVE_FILTER_KEYS = ["severity", "timeScope", "outcomeType"] as const;

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
  searchParams: URLSearchParams,
  updates: Partial<Record<"severity" | "timeScope" | "outcomeType", string>>,
): URLSearchParams {
  const params = new URLSearchParams(searchParams.toString());
  for (const [key, value] of Object.entries(updates)) {
    if (!value || (key === "timeScope" && value === "today")) {
      params.delete(key);
    } else {
      params.set(key, value);
    }
  }
  return params;
}

export function clearExecutiveCommandCenterFilterParams(
  searchParams: URLSearchParams,
): URLSearchParams {
  const params = new URLSearchParams(searchParams.toString());
  for (const key of EXECUTIVE_FILTER_KEYS) {
    if (key === "outcomeType") {
      continue;
    }
    params.delete(key);
  }
  return params;
}

export function hasActiveExecutiveCommandCenterFilters(
  filters: ExecutiveCommandCenterFilters,
): boolean {
  return Boolean(filters.severity) || filters.timeScope !== "today";
}

export function serializeExecutiveCommandCenterFilters(
  filters: ExecutiveCommandCenterFilters,
): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.severity) params.set("severity", filters.severity);
  if (filters.outcomeType) params.set("outcomeType", filters.outcomeType);
  if (filters.timeScope !== "today") params.set("timeScope", filters.timeScope);
  return params;
}

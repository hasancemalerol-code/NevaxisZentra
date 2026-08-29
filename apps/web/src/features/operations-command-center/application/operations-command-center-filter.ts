import type { OperationsCommandCenterFilters } from "../types/operations-command-center-types";

export function readOperationsCommandCenterFilters(
  searchParams: URLSearchParams,
): OperationsCommandCenterFilters {
  return {
    windowFrom: searchParams.get("windowFrom")?.trim() ?? "",
    windowTo: searchParams.get("windowTo")?.trim() ?? "",
    module: searchParams.get("module")?.trim() ?? "",
    state: searchParams.get("state")?.trim() ?? "",
  };
}

export function buildOperationsCommandCenterFilterParams(
  searchParams: URLSearchParams,
  updates: Partial<Record<keyof OperationsCommandCenterFilters, string>>,
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

export function clearOperationsCommandCenterFilterParams(
  searchParams: URLSearchParams,
): URLSearchParams {
  const params = new URLSearchParams(searchParams.toString());
  for (const key of ["windowFrom", "windowTo", "module", "state"]) {
    params.delete(key);
  }
  return params;
}

export function buildOperationsCommandCenterTimeWindowLabel(
  filters: OperationsCommandCenterFilters,
): string {
  if (filters.windowFrom && filters.windowTo) {
    return `${filters.windowFrom} – ${filters.windowTo}`;
  }
  if (filters.windowFrom) {
    return `${filters.windowFrom} sonrası`;
  }
  if (filters.windowTo) {
    return `${filters.windowTo} öncesi`;
  }
  return "Tüm zaman";
}

export function filterOperationsCommandCenterQueueItems<
  T extends Readonly<{ moduleCode: string; sortTimestampUtc: string; humanReviewRequired: boolean; itemState: string; linkedTaskId?: string }>,
>(
  items: readonly T[],
  filters: OperationsCommandCenterFilters,
): readonly T[] {
  const windowFromMs = filters.windowFrom ? Date.parse(filters.windowFrom) : Number.NaN;
  const windowToMs = filters.windowTo ? Date.parse(filters.windowTo) : Number.NaN;

  return items.filter((item) => {
    if (filters.module && item.moduleCode !== filters.module) {
      return false;
    }

    if (filters.state === "review" && !item.humanReviewRequired) {
      return false;
    }
    if (filters.state === "insufficient-data" && item.itemState !== "insufficient-data") {
      return false;
    }
    if (filters.state === "linked-task" && !item.linkedTaskId) {
      return false;
    }

    const timestampMs = Date.parse(item.sortTimestampUtc);
    if (Number.isFinite(windowFromMs) && Number.isFinite(timestampMs) && timestampMs < windowFromMs) {
      return false;
    }
    if (Number.isFinite(windowToMs) && Number.isFinite(timestampMs) && timestampMs > windowToMs) {
      return false;
    }

    return true;
  });
}

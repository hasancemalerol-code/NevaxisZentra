"use client";

import {
  useCallback,
  useMemo,
} from "react";
import {
  usePathname,
  useRouter,
  useSearchParams,
} from "next/navigation";

import type { ProductionPlanFilter } from "./production-plan-query";
import {
  createProductionPlanFilterUrl,
  parseProductionPlanFilter,
} from "../presentation/production-plan-filter-url";

export type UpdateProductionPlanFilterInput =
  Partial<ProductionPlanFilter>;

export type ProductionPlanFilterState =
  Readonly<{
    filter: ProductionPlanFilter;
    updateFilter(
      updates: UpdateProductionPlanFilterInput,
    ): void;
    replaceFilter(
      nextFilter: ProductionPlanFilter,
    ): void;
    clearFilter(): void;
  }>;

function createSearchParamsRecord(
  searchParams: ReturnType<typeof useSearchParams>,
): Readonly<
  Record<
    string,
    string | string[] | undefined
  >
> {
  const record: Record<
    string,
    string | string[]
  > = {};

  for (const [key, value] of searchParams.entries()) {
    const existingValue = record[key];

    if (existingValue === undefined) {
      record[key] = value;
      continue;
    }

    if (Array.isArray(existingValue)) {
      record[key] = [
        ...existingValue,
        value,
      ];
      continue;
    }

    record[key] = [
      existingValue,
      value,
    ];
  }

  return record;
}

function normalizeFilter(
  filter: ProductionPlanFilter,
): ProductionPlanFilter {
  return {
    search:
      filter.search?.trim() || undefined,
    status: filter.status,
    origin: filter.origin,
    priority: filter.priority,
    confidence: filter.confidence,
    targetSystem: filter.targetSystem,
    facilityId:
      filter.facilityId?.trim() ||
      undefined,
    blockingOnly:
      filter.blockingOnly || undefined,
    approvalPendingOnly:
      filter.approvalPendingOnly ||
      undefined,
    transferableOnly:
      filter.transferableOnly ||
      undefined,
  };
}

export function useProductionPlanFilterState(): ProductionPlanFilterState {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const filter = useMemo(
    () =>
      parseProductionPlanFilter(
        createSearchParamsRecord(
          searchParams,
        ),
      ),
    [searchParams],
  );

  const replaceFilter = useCallback(
    (
      nextFilter: ProductionPlanFilter,
    ) => {
      const nextUrl =
        createProductionPlanFilterUrl(
          normalizeFilter(nextFilter),
          pathname,
        );

      router.replace(nextUrl, {
        scroll: false,
      });
    },
    [pathname, router],
  );

  const updateFilter = useCallback(
    (
      updates: UpdateProductionPlanFilterInput,
    ) => {
      replaceFilter({
        ...filter,
        ...updates,
      });
    },
    [filter, replaceFilter],
  );

  const clearFilter = useCallback(() => {
    router.replace(pathname, {
      scroll: false,
    });
  }, [pathname, router]);

  return {
    filter,
    updateFilter,
    replaceFilter,
    clearFilter,
  };
}
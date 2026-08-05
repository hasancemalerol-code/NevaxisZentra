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

import type { OrderDraftFilter } from "./order-draft-query";
import {
  createOrderDraftFilterUrl,
  parseOrderDraftFilterFromUrl,
} from "./order-draft-filter-url";

type UseOrderDraftFilterStateResult = Readonly<{
  filter: OrderDraftFilter;
  setFilter: (
    filter: OrderDraftFilter,
  ) => void;
  clearFilter: () => void;
}>;

export function useOrderDraftFilterState():
  UseOrderDraftFilterStateResult {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const filter = useMemo(
    () =>
      parseOrderDraftFilterFromUrl(
        new URLSearchParams(
          searchParams.toString(),
        ),
      ),
    [searchParams],
  );

  const setFilter = useCallback(
    (nextFilter: OrderDraftFilter) => {
      const nextUrl =
        createOrderDraftFilterUrl(
          nextFilter,
          pathname,
        );

      router.replace(nextUrl, {
        scroll: false,
      });
    },
    [
      pathname,
      router,
    ],
  );

  const clearFilter = useCallback(() => {
    router.replace(pathname, {
      scroll: false,
    });
  }, [
    pathname,
    router,
  ]);

  return {
    filter,
    setFilter,
    clearFilter,
  };
}
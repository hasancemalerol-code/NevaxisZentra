"use client";

import { useMemo } from "react";

import {
  filterOrderDrafts,
  sortOrderDraftsByUpdatedAt,
} from "../application/order-draft-query";
import { useOrderDraftFilterState } from "../application/use-order-draft-filter-state";
import type { OrderDraft } from "../domain/order-draft";
import { OrderDraftFilterBar } from "./order-draft-filter-bar";
import { OrderDraftFilterPresets } from "./order-draft-filter-presets";
import { OrderDraftWorkspace } from "./order-draft-workspace";

type OrderDraftFilteredWorkspaceProps = Readonly<{
  orderDrafts: readonly OrderDraft[];
}>;

export function OrderDraftFilteredWorkspace({
  orderDrafts,
}: OrderDraftFilteredWorkspaceProps) {
  const {
    filter,
    setFilter,
  } = useOrderDraftFilterState();

  const filteredOrderDrafts = useMemo(
    () =>
      sortOrderDraftsByUpdatedAt(
        filterOrderDrafts(
          orderDrafts,
          filter,
        ),
      ),
    [
      filter,
      orderDrafts,
    ],
  );

  return (
    <div className="order-draft-filtered-workspace">
      <OrderDraftFilterPresets
        filter={filter}
        onFilterChange={setFilter}
      />

      <OrderDraftFilterBar
        filter={filter}
        onFilterChange={setFilter}
      />

      {orderDrafts.length > 0 ? (
        <div className="order-draft-filtered-workspace__result">
          <span>
            {filteredOrderDrafts.length} sonuç
          </span>

          {filteredOrderDrafts.length !==
          orderDrafts.length ? (
            <span>
              Toplam {orderDrafts.length} taslak
            </span>
          ) : null}
        </div>
      ) : null}

      <OrderDraftWorkspace
        orderDrafts={filteredOrderDrafts}
      />
    </div>
  );
}
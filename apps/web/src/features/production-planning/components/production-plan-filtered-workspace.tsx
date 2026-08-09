"use client";

import { useMemo } from "react";

import { queryProductionPlans } from "../application/production-plan-query";
import { useProductionPlanFilterState } from "../application/use-production-plan-filter-state";
import type { ProductionPlan } from "../domain/production-plan";
import type { PlanningLogisticsCardSummary } from "../presentation/production-plan-logistics-formatters";
import { ProductionPlanFilterBar } from "./production-plan-filter-bar";
import { ProductionPlanWorkspace } from "./production-plan-workspace";

type ProductionPlanFilteredWorkspaceProps =
  Readonly<{
    productionPlans: readonly ProductionPlan[];
    logisticsSummaries?: ReadonlyMap<string, PlanningLogisticsCardSummary>;
  }>;

export function ProductionPlanFilteredWorkspace({
  productionPlans,
  logisticsSummaries,
}: ProductionPlanFilteredWorkspaceProps) {
  const {
    filter,
    updateFilter,
    clearFilter,
  } = useProductionPlanFilterState();

  const filteredProductionPlans = useMemo(
    () =>
      queryProductionPlans(
        productionPlans,
        filter,
      ),
    [filter, productionPlans],
  );

  return (
    <section className="production-plan-filtered-workspace">
      <ProductionPlanFilterBar
        filter={filter}
        onChange={updateFilter}
        onClear={clearFilter}
      />

      <ProductionPlanWorkspace
        productionPlans={
          filteredProductionPlans
        }
        totalCount={productionPlans.length}
        logisticsSummaries={logisticsSummaries}
      />
    </section>
  );
}
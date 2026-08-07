"use server";

import type { ProductionPlanScheduleResult } from "./production-plan-scheduling-api-contracts";
import { writeScheduleProductionPlan } from "./production-plan-scheduling-writer";

export async function scheduleAction(
  productionPlanId: string,
  expectedVersion: number,
  plannedStartAtUtc: string,
  plannedEndAtUtc: string,
  shiftName?: string,
  schedulingComment?: string,
): Promise<ProductionPlanScheduleResult> {
  return writeScheduleProductionPlan(
    productionPlanId,
    expectedVersion,
    plannedStartAtUtc,
    plannedEndAtUtc,
    shiftName,
    schedulingComment,
  );
}

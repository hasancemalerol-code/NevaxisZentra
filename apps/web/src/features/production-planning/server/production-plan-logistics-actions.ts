"use server";

import { revalidatePath } from "next/cache";

import { evaluatePlanningLogisticsForPlan } from "./production-plan-logistics-reader";

export async function evaluatePlanningLogisticsAction(
  planId: string,
): Promise<
  | Readonly<{ status: "success" }>
  | Readonly<{ status: "failure"; message: string }>
> {
  const result = await evaluatePlanningLogisticsForPlan(planId);

  if (result.status === "failure") {
    return {
      status: "failure",
      message: result.message,
    };
  }

  revalidatePath("/planlama");
  revalidatePath(`/planlama/${planId}`);

  return { status: "success" };
}

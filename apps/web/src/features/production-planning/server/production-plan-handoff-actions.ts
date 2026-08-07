"use server";

import type { ProductionPlanHandoffResult } from "./production-plan-handoff-api-contracts";
import { writePrepareHandoff } from "./production-plan-handoff-writer";

export async function prepareHandoffAction(
  productionPlanId: string,
  expectedVersion: number,
): Promise<ProductionPlanHandoffResult> {
  return writePrepareHandoff(productionPlanId, expectedVersion);
}

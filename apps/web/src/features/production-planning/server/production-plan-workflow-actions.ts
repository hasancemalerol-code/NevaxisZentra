"use server";

import type { ProductionPlanWorkflowResult } from "./production-plan-workflow-api-contracts";
import {
  writeApproveProductionPlan,
  writeRejectProductionPlan,
  writeRequestProductionPlanRevision,
  writeSubmitProductionPlanForReview,
} from "./production-plan-workflow-writer";

export async function submitForReviewAction(
  productionPlanId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ProductionPlanWorkflowResult> {
  return writeSubmitProductionPlanForReview(productionPlanId, expectedVersion, comment);
}

export async function requestRevisionAction(
  productionPlanId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionPlanWorkflowResult> {
  return writeRequestProductionPlanRevision(productionPlanId, expectedVersion, reason);
}

export async function approveAction(
  productionPlanId: string,
  expectedVersion: number,
  note?: string,
): Promise<ProductionPlanWorkflowResult> {
  return writeApproveProductionPlan(productionPlanId, expectedVersion, note);
}

export async function rejectAction(
  productionPlanId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionPlanWorkflowResult> {
  return writeRejectProductionPlan(productionPlanId, expectedVersion, reason);
}

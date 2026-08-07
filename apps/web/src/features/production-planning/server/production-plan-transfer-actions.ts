"use server";

import type {
  ProductionPlanTransferExecuteResult,
  ProductionPlanTransferRequestResult,
} from "./production-plan-transfer-api-contracts";
import {
  writeCreateTransferRequest,
  writeExecuteTransfer,
} from "./production-plan-transfer-writer";

export async function createTransferRequestAction(
  productionPlanId: string,
  expectedVersion: number,
  targetSystem: string,
): Promise<ProductionPlanTransferRequestResult> {
  return writeCreateTransferRequest(productionPlanId, expectedVersion, targetSystem);
}

export async function executeTransferAction(
  productionPlanId: string,
  transferRequestId: string,
  expectedPlanVersion: number,
): Promise<ProductionPlanTransferExecuteResult> {
  return writeExecuteTransfer(productionPlanId, transferRequestId, expectedPlanVersion);
}

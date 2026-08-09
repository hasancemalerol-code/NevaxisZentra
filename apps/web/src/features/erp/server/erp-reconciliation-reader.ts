import "./erp-server-boundary";

import {
  listErpCustomerReconciliation,
  listErpProductReconciliation,
} from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { ErpCustomerReconciliation, ErpProductReconciliation } from "../domain/erp";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type ErpReconciliationResult =
  | Readonly<{
      status: "success";
      customers: readonly ErpCustomerReconciliation[];
      products: readonly ErpProductReconciliation[];
    }>
  | ReadFailure;

export async function readErpReconciliation(): Promise<ErpReconciliationResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [customersResult, productsResult] = await Promise.all([
    listErpCustomerReconciliation(context),
    listErpProductReconciliation(context),
  ]);

  if (customersResult.status === "failure") {
    return { status: "failure", message: customersResult.message };
  }

  if (productsResult.status === "failure") {
    return { status: "failure", message: productsResult.message };
  }

  return {
    status: "success",
    customers: customersResult.value,
    products: productsResult.value,
  };
}

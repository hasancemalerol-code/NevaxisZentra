import "./erp-server-boundary";

import {
  getErpWorkspaceSummary,
  listErpConnections,
  listErpCustomers,
  listErpProducts,
  listErpSyncRuns,
} from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  CanonicalErpCustomer,
  CanonicalErpProduct,
  ErpConnection,
  ErpSyncRun,
  ErpWorkspaceSummary,
} from "../domain/erp";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type ErpWorkspaceResult =
  | Readonly<{
      status: "success";
      summary: ErpWorkspaceSummary;
      connections: readonly ErpConnection[];
      syncRuns: readonly ErpSyncRun[];
      customers: readonly CanonicalErpCustomer[];
      products: readonly CanonicalErpProduct[];
    }>
  | ReadFailure;

export async function readErpWorkspace(): Promise<ErpWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [summaryResult, connectionsResult, syncRunsResult, customersResult, productsResult] =
    await Promise.all([
      getErpWorkspaceSummary(context),
      listErpConnections(context),
      listErpSyncRuns(context),
      listErpCustomers(context),
      listErpProducts(context),
    ]);

  if (summaryResult.status === "failure") {
    return { status: "failure", message: summaryResult.message };
  }

  if (connectionsResult.status === "failure") {
    return { status: "failure", message: connectionsResult.message };
  }

  if (syncRunsResult.status === "failure") {
    return { status: "failure", message: syncRunsResult.message };
  }

  if (customersResult.status === "failure") {
    return { status: "failure", message: customersResult.message };
  }

  if (productsResult.status === "failure") {
    return { status: "failure", message: productsResult.message };
  }

  return {
    status: "success",
    summary: summaryResult.value,
    connections: connectionsResult.value,
    syncRuns: syncRunsResult.value,
    customers: customersResult.value,
    products: productsResult.value,
  };
}

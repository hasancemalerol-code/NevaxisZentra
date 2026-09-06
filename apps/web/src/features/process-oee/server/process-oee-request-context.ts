import "./process-oee-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import { resolveSharedProductionApiRequestContext } from "../../production-execution/server/shared-production-api-request-context";

export type ProcessOeeRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionOrderRequestContext;
    }>
  | Readonly<{
      status: "failure";
      message: string;
      code: string;
    }>;

export async function resolveProcessOeeRequestContext(): Promise<ProcessOeeRequestContextResult> {
  return resolveSharedProductionApiRequestContext(
    "Process OEE request context could not be resolved from configured runtime modes.",
  );
}

import "./operational-risk-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import { resolveSharedProductionApiRequestContext } from "../../production-execution/server/shared-production-api-request-context";

export type OperationalRiskRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionOrderRequestContext;
    }>
  | Readonly<{
      status: "failure";
      message: string;
      code: string;
    }>;

export async function resolveOperationalRiskRequestContext(): Promise<OperationalRiskRequestContextResult> {
  return resolveSharedProductionApiRequestContext(
    "Operasyonel risk istek bağlamı yapılandırılmış çalışma modlarından çözümlenemedi.",
  );
}

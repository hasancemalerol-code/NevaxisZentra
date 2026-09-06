import "./commercial-intelligence-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import { resolveSharedProductionApiRequestContext } from "../../production-execution/server/shared-production-api-request-context";

export type CommercialIntelligenceRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionOrderRequestContext;
    }>
  | Readonly<{
      status: "failure";
      message: string;
      code: string;
    }>;

export async function resolveCommercialIntelligenceRequestContext(): Promise<CommercialIntelligenceRequestContextResult> {
  return resolveSharedProductionApiRequestContext(
    "Ticari zekâ istek bağlamı yapılandırılmış çalışma modlarından çözümlenemedi.",
  );
}

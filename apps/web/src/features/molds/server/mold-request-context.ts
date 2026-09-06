import "./mold-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import { resolveSharedProductionApiRequestContext } from "../../production-execution/server/shared-production-api-request-context";

export type MoldRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionOrderRequestContext;
    }>
  | Readonly<{
      status: "failure";
      message: string;
      code: string;
    }>;

export async function resolveMoldRequestContext(): Promise<MoldRequestContextResult> {
  return resolveSharedProductionApiRequestContext(
    "Kalıp istek bağlamı yapılandırılmış çalışma modlarından çözümlenemedi.",
  );
}

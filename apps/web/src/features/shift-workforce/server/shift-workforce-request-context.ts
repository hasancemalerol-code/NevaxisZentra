import "./shift-workforce-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import { resolveSharedProductionApiRequestContext } from "../../production-execution/server/shared-production-api-request-context";

export type ShiftWorkforceRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionOrderRequestContext;
    }>
  | Readonly<{
      status: "failure";
      message: string;
      code: string;
    }>;

export async function resolveShiftWorkforceRequestContext(): Promise<ShiftWorkforceRequestContextResult> {
  return resolveSharedProductionApiRequestContext(
    "Vardiya personel isteği bağlamı yapılandırılmış çalışma modlarından çözümlenemedi.",
  );
}

import "./iot-plc-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import { resolveSharedProductionApiRequestContext } from "../../production-execution/server/shared-production-api-request-context";

export type IotPlcRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionOrderRequestContext;
    }>
  | Readonly<{
      status: "failure";
      message: string;
      code: string;
    }>;

export async function resolveIotPlcRequestContext(): Promise<IotPlcRequestContextResult> {
  return resolveSharedProductionApiRequestContext(
    "IoT/PLC istek bağlamı yapılandırılmış çalışma modlarından çözümlenemedi.",
  );
}

import "./iot-plc-server-boundary";

import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import {
  resolveProductionOrderRequestContext,
  type ProductionOrderRequestContext,
} from "../../production-execution/server/production-order-request-context";
import { parseProductionPlanRuntimeConfig } from "../../production-planning/server/production-plan-runtime-config";
import { resolveProductionPlanRequestContext } from "../../production-planning/server/production-plan-request-context";
import { resolveProductionPlanningServerScope } from "../../production-planning/server/production-plan-scope";

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

function mapPlanningContext(
  context: Readonly<{
    runtimeMode: "api" | "development-preview" | "development-api";
    apiBaseUrl: string;
    facilityId: string;
    requestHeaders: Readonly<Record<string, string>>;
    correlationId: string;
  }>,
): ProductionOrderRequestContext {
  return {
    runtimeMode: context.runtimeMode,
    apiBaseUrl: context.apiBaseUrl,
    facilityId: context.facilityId,
    requestHeaders: context.requestHeaders,
    correlationId: context.correlationId,
  };
}

export function resolveIotPlcRequestContext(): IotPlcRequestContextResult {
  const executionConfig = parseProductionExecutionRuntimeConfig();
  if (executionConfig.status === "success") {
    const executionContext = resolveProductionOrderRequestContext(
      executionConfig.config,
    );

    if (executionContext.status === "success") {
      return {
        status: "success",
        context: executionContext.context,
      };
    }
  }

  const planningConfig = parseProductionPlanRuntimeConfig();
  if (planningConfig.status !== "success") {
    return {
      status: "failure",
      message: executionConfig.status === "success"
        ? "IoT/PLC istek bağlamı yapılandırılmış çalışma modlarından çözümlenemedi."
        : planningConfig.message,
      code: planningConfig.code,
    };
  }

  const scopeResult = resolveProductionPlanningServerScope(planningConfig.config);
  if (scopeResult.status === "failure") {
    return {
      status: "failure",
      message: scopeResult.message,
      code: scopeResult.code,
    };
  }

  const planningContext = resolveProductionPlanRequestContext(
    planningConfig.config,
    scopeResult.scope,
  );

  if (planningContext.status === "failure") {
    return {
      status: "failure",
      message: planningContext.message,
      code: planningContext.code,
    };
  }

  return {
    status: "success",
    context: mapPlanningContext(planningContext.context),
  };
}

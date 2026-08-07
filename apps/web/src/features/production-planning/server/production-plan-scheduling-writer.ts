import "./production-plan-server-boundary";

import { scheduleProductionPlan } from "./production-plan-scheduling-api-client";
import type { ProductionPlanScheduleResult } from "./production-plan-scheduling-api-contracts";
import { parseProductionPlanRuntimeConfig } from "./production-plan-runtime-config";
import { resolveProductionPlanRequestContext } from "./production-plan-request-context";
import { resolveProductionPlanningServerScope } from "./production-plan-scope";

const PREVIEW_WRITE_BLOCKED_RESULT: ProductionPlanScheduleResult = {
  status: "failure",
  code: "preview-write-blocked",
  message: "Yazma işlemleri geliştirme önizleme modunda devre dışı bırakılmıştır.",
};

const CONFIGURATION_ERROR_RESULT: ProductionPlanScheduleResult = {
  status: "failure",
  code: "configuration-error",
  message: "Üretim planlama servisi yapılandırılamadı.",
};

function resolveSchedulingContext():
  | { context: (ReturnType<typeof resolveProductionPlanRequestContext> & { status: "success" })["context"] }
  | { error: ProductionPlanScheduleResult } {
  const configResult = parseProductionPlanRuntimeConfig();

  if (configResult.status === "failure") {
    return { error: CONFIGURATION_ERROR_RESULT };
  }

  const config = configResult.config;

  if (config.mode === "development-preview") {
    return { error: PREVIEW_WRITE_BLOCKED_RESULT };
  }

  const scopeResult = resolveProductionPlanningServerScope(config);

  if (scopeResult.status === "failure") {
    return {
      error: {
        status: "failure" as const,
        code: scopeResult.code,
        message: scopeResult.message,
      },
    };
  }

  const contextResult = resolveProductionPlanRequestContext(
    config,
    scopeResult.scope,
  );

  if (contextResult.status === "failure") {
    return {
      error: {
        status: "failure" as const,
        code: contextResult.code,
        message: contextResult.message,
      },
    };
  }

  return { context: contextResult.context };
}

export async function writeScheduleProductionPlan(
  productionPlanId: string,
  expectedVersion: number,
  plannedStartAtUtc: string,
  plannedEndAtUtc: string,
  shiftName?: string,
  schedulingComment?: string,
): Promise<ProductionPlanScheduleResult> {
  const resolved = resolveSchedulingContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return scheduleProductionPlan(resolved.context, productionPlanId, {
    expectedVersion,
    plannedStartAtUtc,
    plannedEndAtUtc,
    shiftName: shiftName?.trim() || undefined,
    schedulingComment: schedulingComment?.trim() || undefined,
  });
}

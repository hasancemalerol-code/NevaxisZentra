import "./production-plan-server-boundary";

import {
  approveProductionPlan,
  rejectProductionPlan,
  requestProductionPlanRevision,
  submitProductionPlanForReview,
} from "./production-plan-workflow-api-client";
import type { ProductionPlanWorkflowResult } from "./production-plan-workflow-api-contracts";
import {
  parseProductionPlanRuntimeConfig,
} from "./production-plan-runtime-config";
import {
  resolveProductionPlanRequestContext,
  resolveProductionPlanRequestContextForIncomingRequest,
} from "./production-plan-request-context";
import {
  resolveProductionPlanningServerScope,
} from "./production-plan-scope";

const PREVIEW_WRITE_BLOCKED_RESULT: ProductionPlanWorkflowResult = {
  status: "failure",
  code: "preview-write-blocked",
  message: "Yazma işlemleri geliştirme önizleme modunda devre dışı bırakılmıştır.",
};

const CONFIGURATION_ERROR_RESULT: ProductionPlanWorkflowResult = {
  status: "failure",
  code: "configuration-error",
  message: "Üretim planlama servisi yapılandırılamadı.",
};

async function resolveWorkflowContext(): Promise<
  | { context: Awaited<ReturnType<typeof resolveProductionPlanRequestContext> & { status: "success" }>["context"] }
  | { error: ProductionPlanWorkflowResult }> {
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

  const contextResult = await resolveProductionPlanRequestContextForIncomingRequest(
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

export async function writeSubmitProductionPlanForReview(
  productionPlanId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ProductionPlanWorkflowResult> {
  const resolved = await resolveWorkflowContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return submitProductionPlanForReview(resolved.context, productionPlanId, {
    expectedVersion,
    comment: comment?.trim() || undefined,
  });
}

export async function writeRequestProductionPlanRevision(
  productionPlanId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionPlanWorkflowResult> {
  const resolved = await resolveWorkflowContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return requestProductionPlanRevision(resolved.context, productionPlanId, {
    expectedVersion,
    reason,
  });
}

export async function writeApproveProductionPlan(
  productionPlanId: string,
  expectedVersion: number,
  note?: string,
): Promise<ProductionPlanWorkflowResult> {
  const resolved = await resolveWorkflowContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return approveProductionPlan(resolved.context, productionPlanId, {
    expectedVersion,
    note: note?.trim() || undefined,
  });
}

export async function writeRejectProductionPlan(
  productionPlanId: string,
  expectedVersion: number,
  reason: string,
): Promise<ProductionPlanWorkflowResult> {
  const resolved = await resolveWorkflowContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return rejectProductionPlan(resolved.context, productionPlanId, {
    expectedVersion,
    reason,
  });
}

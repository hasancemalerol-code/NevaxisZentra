import "./production-plan-server-boundary";

import {
  createProductionPlanDraft,
  updateProductionPlanDraft,
} from "./production-plan-draft-api-client";
import type {
  ProductionPlanDraftUpdateRequestBody,
  ProductionPlanDraftWriteRequestBody,
  ProductionPlanDraftWriteResult,
} from "./production-plan-draft-api-contracts";
import {
  parseProductionPlanRuntimeConfig,
} from "./production-plan-runtime-config";
import {
  resolveProductionPlanRequestContextForIncomingRequest,
} from "./production-plan-request-context";
import {
  resolveProductionPlanningServerScope,
} from "./production-plan-scope";

const PREVIEW_WRITE_BLOCKED_RESULT: ProductionPlanDraftWriteResult = {
  status: "failure",
  code: "preview-write-blocked",
  message: "Yazma işlemleri geliştirme önizleme modunda devre dışı bırakılmıştır.",
};

const CONFIGURATION_ERROR_RESULT: ProductionPlanDraftWriteResult = {
  status: "failure",
  code: "configuration-error",
  message: "Üretim planlama servisi yapılandırılamadı.",
};

async function resolveWriteContext() {
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

export async function writeCreateProductionPlanDraft(
  body: ProductionPlanDraftWriteRequestBody,
): Promise<ProductionPlanDraftWriteResult> {
  const resolved = await resolveWriteContext();

  if (resolved.error) {
    return resolved.error;
  }

  return createProductionPlanDraft(resolved.context, body);
}

export async function writeUpdateProductionPlanDraft(
  productionPlanId: string,
  body: ProductionPlanDraftUpdateRequestBody,
): Promise<ProductionPlanDraftWriteResult> {
  const resolved = await resolveWriteContext();

  if (resolved.error) {
    return resolved.error;
  }

  return updateProductionPlanDraft(resolved.context, productionPlanId, body);
}

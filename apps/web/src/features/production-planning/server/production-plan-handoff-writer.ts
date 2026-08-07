import "./production-plan-server-boundary";

import { prepareHandoff } from "./production-plan-handoff-api-client";
import type { ProductionPlanHandoffResult } from "./production-plan-handoff-api-contracts";
import { parseProductionPlanRuntimeConfig } from "./production-plan-runtime-config";
import { resolveProductionPlanRequestContext } from "./production-plan-request-context";
import { resolveProductionPlanningServerScope } from "./production-plan-scope";

type HandoffContext = (ReturnType<typeof resolveProductionPlanRequestContext> & {
  status: "success";
})["context"];

function resolveHandoffContext():
  | { context: HandoffContext }
  | { error: { status: "failure"; code: string; message: string } } {
  const configResult = parseProductionPlanRuntimeConfig();

  if (configResult.status === "failure") {
    return {
      error: {
        status: "failure" as const,
        code: "configuration-error",
        message: "Üretim planlama servisi yapılandırılamadı.",
      },
    };
  }

  const config = configResult.config;

  if (config.mode === "development-preview") {
    return {
      error: {
        status: "failure" as const,
        code: "preview-write-blocked",
        message: "Yazma işlemleri geliştirme önizleme modunda devre dışı bırakılmıştır.",
      },
    };
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

export async function writePrepareHandoff(
  productionPlanId: string,
  expectedVersion: number,
): Promise<ProductionPlanHandoffResult> {
  const resolved = resolveHandoffContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return prepareHandoff(resolved.context, productionPlanId, {
    expectedVersion,
  });
}

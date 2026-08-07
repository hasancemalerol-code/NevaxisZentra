import "./production-plan-server-boundary";

import {
  createTransferRequest,
  executeTransfer,
} from "./production-plan-transfer-api-client";
import type {
  ProductionPlanTransferExecuteResult,
  ProductionPlanTransferRequestResult,
} from "./production-plan-transfer-api-contracts";
import { parseProductionPlanRuntimeConfig } from "./production-plan-runtime-config";
import { resolveProductionPlanRequestContext } from "./production-plan-request-context";
import { resolveProductionPlanningServerScope } from "./production-plan-scope";


type TransferContext = (ReturnType<typeof resolveProductionPlanRequestContext> & { status: "success" })["context"];

function resolveTransferContext():
  | { context: TransferContext }
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

export async function writeCreateTransferRequest(
  productionPlanId: string,
  expectedVersion: number,
  targetSystem: string,
): Promise<ProductionPlanTransferRequestResult> {
  const resolved = resolveTransferContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return createTransferRequest(resolved.context, productionPlanId, {
    expectedVersion,
    targetSystem,
  });
}

export async function writeExecuteTransfer(
  productionPlanId: string,
  transferRequestId: string,
  expectedPlanVersion: number,
): Promise<ProductionPlanTransferExecuteResult> {
  const resolved = resolveTransferContext();

  if ("error" in resolved) {
    return resolved.error;
  }

  return executeTransfer(resolved.context, productionPlanId, transferRequestId, {
    expectedPlanVersion,
  });
}

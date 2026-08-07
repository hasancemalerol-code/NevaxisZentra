import "./production-order-server-boundary";

import type { ProductionExecutionRuntimeConfig } from "./production-order-runtime-config";

export type ProductionOrderRequestContext = Readonly<{
  runtimeMode: ProductionExecutionRuntimeConfig["mode"];
  apiBaseUrl: string;
  facilityId: string;
  requestHeaders: Readonly<Record<string, string>>;
  correlationId: string;
}>;

export type ProductionOrderRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionOrderRequestContext;
    }>
  | Readonly<{
      status: "failure";
      code: "configuration-error" | "unauthenticated" | "invalid-scope";
      message: string;
      retryable: false;
    }>;

function createCorrelationId(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `correlation-${Date.now()}`;
}

export function resolveProductionOrderRequestContext(
  config: ProductionExecutionRuntimeConfig,
): ProductionOrderRequestContextResult {
  if (config.mode === "development-preview") {
    return {
      status: "failure",
      code: "configuration-error",
      message: "Request context is not used in development preview mode.",
      retryable: false,
    };
  }

  const apiBaseUrl = config.apiBaseUrl?.trim() ?? "";
  const facilityId = config.facilityId?.trim() ?? "";

  if (!apiBaseUrl) {
    return {
      status: "failure",
      code: "configuration-error",
      message: "API base URL is not configured.",
      retryable: false,
    };
  }

  if (!facilityId) {
    return {
      status: "failure",
      code: "invalid-scope",
      message: "facilityId is required.",
      retryable: false,
    };
  }

  if (config.mode === "development-api") {
    const developmentAuth = config.developmentAuth;

    if (!developmentAuth) {
      return {
        status: "failure",
        code: "configuration-error",
        message: "Development API authentication is not configured.",
        retryable: false,
      };
    }

    return {
      status: "success",
      context: {
        runtimeMode: config.mode,
        apiBaseUrl,
        facilityId,
        correlationId: createCorrelationId(),
        requestHeaders: {
          "X-CrevaFactOS-Dev-Issuer": developmentAuth.issuer,
          "X-CrevaFactOS-Dev-Subject": developmentAuth.subject,
          "X-CrevaFactOS-Tenant": developmentAuth.tenantKey,
        },
      },
    };
  }

  return {
    status: "failure",
    code: "unauthenticated",
    message: "Production API mode requires a trusted server-side authentication bridge.",
    retryable: false,
  };
}

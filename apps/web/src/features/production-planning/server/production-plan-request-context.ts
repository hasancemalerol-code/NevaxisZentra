import "./production-plan-server-boundary";

import type { ProductionPlanRuntimeConfig } from "./production-plan-runtime-config";
import type { ProductionPlanningServerScope } from "./production-plan-scope";

export type ProductionPlanAuthenticationMethod =
  | "development-headers"
  | "session-cookie";

export type ProductionPlanRequestContext =
  Readonly<{
    runtimeMode: ProductionPlanRuntimeConfig["mode"];
    apiBaseUrl: string;
    facilityId: string;
    authenticationMethod: ProductionPlanAuthenticationMethod;
    requestHeaders: Readonly<Record<string, string>>;
    correlationId: string;
  }>;

export type ProductionPlanRequestContextFailureCode =
  | "configuration-error"
  | "unauthenticated"
  | "invalid-scope";

export type ProductionPlanRequestContextFailure =
  Readonly<{
    status: "failure";
    code: ProductionPlanRequestContextFailureCode;
    message: string;
    retryable: false;
  }>;

export type ProductionPlanRequestContextResult =
  | Readonly<{
      status: "success";
      context: ProductionPlanRequestContext;
    }>
  | ProductionPlanRequestContextFailure;

function createFailure(
  code: ProductionPlanRequestContextFailureCode,
  message: string,
): ProductionPlanRequestContextFailure {
  return {
    status: "failure",
    code,
    message,
    retryable: false,
  };
}

function createCorrelationId(): string {
  if (
    typeof crypto !== "undefined" &&
    typeof crypto.randomUUID === "function"
  ) {
    return crypto.randomUUID();
  }

  return `correlation-${Date.now()}`;
}

export function resolveProductionPlanRequestContext(
  config: ProductionPlanRuntimeConfig,
  scope: ProductionPlanningServerScope,
): ProductionPlanRequestContextResult {
  if (config.mode === "development-preview") {
    return createFailure(
      "configuration-error",
      "Request context is not used in development preview mode.",
    );
  }

  const apiBaseUrl = config.apiBaseUrl?.trim();
  const facilityId =
    scope.facilityId.trim() ||
    config.facilityId?.trim() ||
    "";

  if (!apiBaseUrl) {
    return createFailure(
      "configuration-error",
      "API base URL is not configured.",
    );
  }

  if (!facilityId) {
    return createFailure(
      "invalid-scope",
      "facilityId is required.",
    );
  }

  if (config.mode === "development-api") {
    const developmentAuth =
      config.developmentAuth;

    if (!developmentAuth) {
      return createFailure(
        "configuration-error",
        "Development API authentication is not configured.",
      );
    }

    return {
      status: "success",
      context: {
        runtimeMode: config.mode,
        apiBaseUrl,
        facilityId,
        authenticationMethod:
          "development-headers",
        correlationId:
          createCorrelationId(),
        requestHeaders: {
          "X-CrevaFactOS-Dev-Issuer":
            developmentAuth.issuer,
          "X-CrevaFactOS-Dev-Subject":
            developmentAuth.subject,
          "X-CrevaFactOS-Tenant":
            developmentAuth.tenantKey,
        },
      },
    };
  }

  return createFailure(
    "unauthenticated",
    "Production API mode requires a trusted server-side authentication bridge.",
  );
}

export function serializeProductionPlanRequestContext(
  context: ProductionPlanRequestContext,
): string {
  return JSON.stringify({
    runtimeMode: context.runtimeMode,
    facilityId: context.facilityId,
    authenticationMethod:
      context.authenticationMethod,
    correlationId: context.correlationId,
  });
}

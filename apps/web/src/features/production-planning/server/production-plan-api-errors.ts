import "./production-plan-server-boundary";

import { refusalCode, SUBSCRIPTION_REQUIRED_MESSAGE } from "@/lib/api/api-failure-classifier";

export type ProductionPlanApiFailureCode =
  | "configuration-error"
  | "unauthenticated"
  | "invalid-request"
  | "permission-denied"
  | "subscription-required"
  | "not-found"
  | "source-unavailable"
  | "invalid-response"
  | "unexpected-error";

export type ProductionPlanApiFailure =
  Readonly<{
    status: "failure";
    code: ProductionPlanApiFailureCode;
    message: string;
    retryable: boolean;
  }>;

export type ProductionPlanApiProblemDetails =
  Readonly<{
    status?: number;
    title?: string;
    failureCode?: string;
  }>;

function createFailure(
  code: ProductionPlanApiFailureCode,
  message: string,
  retryable: boolean,
): ProductionPlanApiFailure {
  return {
    status: "failure",
    code,
    message,
    retryable,
  };
}

function isRecord(
  value: unknown,
): value is Record<string, unknown> {
  return (
    typeof value === "object" &&
    value !== null &&
    !Array.isArray(value)
  );
}

export function parseProductionPlanApiProblemDetails(
  value: unknown,
): ProductionPlanApiProblemDetails | null {
  if (!isRecord(value)) {
    return null;
  }

  const extensions = isRecord(value.extensions)
    ? value.extensions
    : null;

  const failureCode =
    typeof extensions?.failureCode ===
    "string"
      ? extensions.failureCode
      : typeof value.failureCode ===
          "string"
        ? value.failureCode
        : undefined;

  return {
    status:
      typeof value.status === "number"
        ? value.status
        : undefined,
    title:
      typeof value.title === "string"
        ? value.title
        : undefined,
    failureCode,
  };
}

export function mapHttpStatusToProductionPlanApiFailure(
  statusCode: number,
  problemDetails: ProductionPlanApiProblemDetails | null,
): ProductionPlanApiFailure {
  const failureCode =
    problemDetails?.failureCode;

  switch (statusCode) {
    case 400:
      return createFailure(
        "invalid-request",
        "The production planning request is invalid.",
        false,
      );

    case 401:
      return createFailure(
        "unauthenticated",
        "Authentication is required.",
        false,
      );

    case 403: {
      // The backend failure code is authoritative: a licence or session cause is not "permission denied".
      const refusal = refusalCode(statusCode, { failureCode });
      if (refusal === "subscription-required") {
        return createFailure(
          refusal,
          SUBSCRIPTION_REQUIRED_MESSAGE,
          false,
        );
      }

      if (refusal === "unauthenticated") {
        return createFailure(
          refusal,
          "Authentication is required.",
          false,
        );
      }

      return createFailure(
        "permission-denied",
        "Production planning access is denied.",
        false,
      );
    }

    case 404:
      return createFailure(
        "not-found",
        "The requested production plan was not found.",
        false,
      );

    case 503:
      return createFailure(
        "source-unavailable",
        "The production planning service is unavailable.",
        true,
      );

    default:
      if (
        failureCode === "invalid-row" ||
        failureCode === "data-integrity" ||
        failureCode === "invalid-response"
      ) {
        return createFailure(
          "invalid-response",
          "The production planning response is invalid.",
          false,
        );
      }

      if (statusCode >= 500) {
        return createFailure(
          "unexpected-error",
          "An unexpected production planning error occurred.",
          true,
        );
      }

      return createFailure(
        "unexpected-error",
        "An unexpected production planning error occurred.",
        false,
      );
  }
}

export function mapProductionPlanApiFailureToRepositoryFailure(
  failure: ProductionPlanApiFailure,
): Readonly<{
  code:
    | "invalid-scope"
    | "unauthenticated"
    | "configuration-error"
    | "permission-denied"
    | "source-unavailable"
    | "invalid-response"
    | "unexpected-error";
  message: string;
  retryable: boolean;
}> {
  switch (failure.code) {
    case "invalid-request":
      return {
        code: "invalid-scope",
        message: failure.message,
        retryable: false,
      };

    case "unauthenticated":
      return {
        code: "unauthenticated",
        message: failure.message,
        retryable: false,
      };

    case "configuration-error":
      return {
        code: "configuration-error",
        message: failure.message,
        retryable: false,
      };

    case "permission-denied":
      return {
        code: "permission-denied",
        message: failure.message,
        retryable: false,
      };

    case "not-found":
      return {
        code: "invalid-scope",
        message: failure.message,
        retryable: false,
      };

    case "source-unavailable":
      return {
        code: "source-unavailable",
        message: failure.message,
        retryable: failure.retryable,
      };

    case "invalid-response":
      return {
        code: "invalid-response",
        message: failure.message,
        retryable: false,
      };

    case "unexpected-error":
    default:
      return {
        code: "unexpected-error",
        message: failure.message,
        retryable: failure.retryable,
      };
  }
}

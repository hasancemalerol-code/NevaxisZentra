import "./production-plan-server-boundary";

import type {
  ProductionPlanDetailApiResponse,
  ProductionPlanListApiResponse,
} from "./production-plan-api-contracts";
import {
  normalizeProductionPlanListLimit,
  normalizeProductionPlanListQueryValue,
  parseProductionPlanDetailApiResponse,
  parseProductionPlanListApiResponse,
} from "./production-plan-api-contracts";
import {
  mapHttpStatusToProductionPlanApiFailure,
  parseProductionPlanApiProblemDetails,
  type ProductionPlanApiFailure,
} from "./production-plan-api-errors";
import type { ProductionPlanRequestContext } from "./production-plan-request-context";

export type ProductionPlanApiListQuery =
  Readonly<{
    facilityId: string;
    status?: string;
    search?: string;
    limit?: number;
  }>;

export type ProductionPlanApiClientResult<T> =
  | Readonly<{
      status: "success";
      value: T;
    }>
  | ProductionPlanApiFailure;

function createApiFailure(
  code: ProductionPlanApiFailure["code"],
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

function trimTrailingSlash(
  baseUrl: string,
): string {
  return baseUrl.replace(/\/+$/, "");
}

function buildListUrl(
  context: ProductionPlanRequestContext,
  query: ProductionPlanApiListQuery,
): string {
  const searchParams = new URLSearchParams();

  searchParams.set(
    "facilityId",
    query.facilityId.trim(),
  );

  const status = normalizeProductionPlanListQueryValue(
    query.status,
  );
  if (status) {
    searchParams.set("status", status);
  }

  const search = normalizeProductionPlanListQueryValue(
    query.search,
  );
  if (search) {
    searchParams.set("search", search);
  }

  const limit = normalizeProductionPlanListLimit(
    query.limit,
  );
  if (limit !== undefined) {
    searchParams.set(
      "limit",
      String(limit),
    );
  }

  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans?${searchParams.toString()}`;
}

function buildDetailUrl(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  facilityId: string,
): string {
  const searchParams = new URLSearchParams();
  searchParams.set(
    "facilityId",
    facilityId.trim(),
  );

  const normalizedProductionPlanId =
    productionPlanId.trim();

  return `${trimTrailingSlash(context.apiBaseUrl)}/api/production-planning/plans/${encodeURIComponent(normalizedProductionPlanId)}?${searchParams.toString()}`;
}

async function readResponseBody(
  response: Response,
): Promise<unknown> {
  const responseText =
    await response.text();

  if (!responseText.trim()) {
    return null;
  }

  try {
    return JSON.parse(responseText);
  } catch {
    return null;
  }
}

async function executeProductionPlanApiGet<T>(
  context: ProductionPlanRequestContext,
  url: string,
  parseValue: (
    value: unknown,
  ) => ProductionPlanApiClientResult<T>,
): Promise<ProductionPlanApiClientResult<T>> {
  if (!context.apiBaseUrl.trim()) {
    return createApiFailure(
      "configuration-error",
      "API base URL is not configured.",
      false,
    );
  }

  let response: Response;

  try {
    response = await fetch(url, {
      method: "GET",
      headers: {
        Accept: "application/json",
        ...context.requestHeaders,
      },
      cache: "no-store",
    });
  } catch {
    return createApiFailure(
      "source-unavailable",
      "The production planning service is unavailable.",
      true,
    );
  }

  const responseBody =
    await readResponseBody(response);

  if (!response.ok) {
    const problemDetails =
      parseProductionPlanApiProblemDetails(
        responseBody,
      );

    return mapHttpStatusToProductionPlanApiFailure(
      response.status,
      problemDetails,
    );
  }

  return parseValue(responseBody);
}

export async function listProductionPlans(
  context: ProductionPlanRequestContext,
  query: ProductionPlanApiListQuery,
): Promise<
  ProductionPlanApiClientResult<ProductionPlanListApiResponse>
> {
  const url = buildListUrl(context, query);

  const result =
    await executeProductionPlanApiGet(
      context,
      url,
      (responseBody) => {
        const parsedResponse =
          parseProductionPlanListApiResponse(
            responseBody,
          );

        if (
          parsedResponse.status === "failure"
        ) {
          return createApiFailure(
            "invalid-response",
            parsedResponse.message,
            false,
          );
        }

        return {
          status: "success",
          value: parsedResponse.value,
        };
      },
    );

  return result;
}

export async function getProductionPlanById(
  context: ProductionPlanRequestContext,
  productionPlanId: string,
  facilityId: string,
): Promise<
  ProductionPlanApiClientResult<ProductionPlanDetailApiResponse>
> {
  const normalizedProductionPlanId =
    productionPlanId.trim();

  if (!normalizedProductionPlanId) {
    return createApiFailure(
      "invalid-request",
      "productionPlanId is required.",
      false,
    );
  }

  const url = buildDetailUrl(
    context,
    normalizedProductionPlanId,
    facilityId,
  );

  const result =
    await executeProductionPlanApiGet(
      context,
      url,
      (responseBody) => {
        const parsedResponse =
          parseProductionPlanDetailApiResponse(
            responseBody,
          );

        if (
          parsedResponse.status === "failure"
        ) {
          return createApiFailure(
            "invalid-response",
            parsedResponse.message,
            false,
          );
        }

        return {
          status: "success",
          value: parsedResponse.value,
        };
      },
    );

  return result;
}

export {
  buildDetailUrl,
  buildListUrl,
};

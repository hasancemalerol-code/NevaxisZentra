import "./material-server-boundary";

import type {
  MaterialDetail,
  MaterialLifecycleEvent,
  MaterialSummary,
  MaterialTechnicalSpecification,
  MaterialWorkspaceSummary,
  MaterialWriteResult,
} from "../domain/materials";
import {
  parseMaterialDetail,
  parseMaterialLifecycleEventList,
  parseMaterialSummaryList,
  parseMaterialTechnicalSpecification,
  parseMaterialWorkspaceSummary,
  parseMaterialWriteApiResponse,
} from "./material-api-contracts";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type MaterialApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type MaterialApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | MaterialApiFailure;

export type MaterialListFilters = Readonly<{
  search?: string;
  lifecycleStatus?: string;
  materialCategory?: string;
  polymerFamily?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}>;

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

function mapFailureStatus(status: number): MaterialApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<MaterialApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status),
        message: `Malzeme isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Malzeme API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

async function mutateJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  method: "POST" | "PUT",
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<MaterialApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method,
      headers: {
        ...context.requestHeaders,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    if (!response.ok) {
      let message = `Malzeme işlemi başarısız (${response.status}).`;
      try {
        const problem = (await response.json()) as { title?: string };
        if (problem.title) {
          message = problem.title;
        }
      } catch {
        // ignore parse errors
      }

      return {
        status: "failure",
        code: mapFailureStatus(response.status),
        message,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Malzeme API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

export function listMaterials(
  context: ProductionOrderRequestContext,
  filters: MaterialListFilters = {},
): Promise<MaterialApiResult<readonly MaterialSummary[]>> {
  return getJson(
    context,
    "/api/materials",
    {
      search: filters.search,
      lifecycleStatus: filters.lifecycleStatus,
      materialCategory: filters.materialCategory,
      polymerFamily: filters.polymerFamily,
      isActive: filters.isActive === undefined ? undefined : String(filters.isActive),
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseMaterialSummaryList,
  );
}

export function getMaterialWorkspaceSummary(
  context: ProductionOrderRequestContext,
): Promise<MaterialApiResult<MaterialWorkspaceSummary>> {
  return getJson(context, "/api/materials/workspace-summary", undefined, parseMaterialWorkspaceSummary);
}

export function getMaterial(
  context: ProductionOrderRequestContext,
  materialId: string,
): Promise<MaterialApiResult<MaterialDetail>> {
  return getJson(context, `/api/materials/${encodeURIComponent(materialId)}`, undefined, parseMaterialDetail);
}

export function listMaterialLifecycleHistory(
  context: ProductionOrderRequestContext,
  materialId: string,
): Promise<MaterialApiResult<readonly MaterialLifecycleEvent[]>> {
  return getJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/lifecycle-history`,
    undefined,
    parseMaterialLifecycleEventList,
  );
}

export function getMaterialTechnicalSpecification(
  context: ProductionOrderRequestContext,
  materialId: string,
): Promise<MaterialApiResult<MaterialTechnicalSpecification | null>> {
  return getJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/technical-specification`,
    undefined,
    (payload) => {
      if (payload === null || payload === undefined) {
        return null;
      }
      return parseMaterialTechnicalSpecification(payload);
    },
  );
}

export function createMaterial(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(context, "/api/materials", "POST", body, parseMaterialWriteApiResponse);
}

export function updateMaterial(
  context: ProductionOrderRequestContext,
  materialId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}`,
    "PUT",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function updateMaterialTechnicalSpecification(
  context: ProductionOrderRequestContext,
  materialId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/technical-specification`,
    "PUT",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function transitionMaterialLifecycle(
  context: ProductionOrderRequestContext,
  materialId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/lifecycle-transitions`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export async function probeMaterialReadPermission(
  context: ProductionOrderRequestContext,
): Promise<boolean> {
  const maxAttempts = 3;

  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    const result = await getMaterialWorkspaceSummary(context);
    if (result.status === "success") {
      return result.value.capabilities.canRead;
    }

    if (result.code === "permission-denied" || result.code === "unauthenticated") {
      return false;
    }

    if (!result.retryable || attempt === maxAttempts - 1) {
      return false;
    }

    await new Promise((resolve) => {
      setTimeout(resolve, 250 * (attempt + 1));
    });
  }

  return false;
}

export async function probeMaterialManagePermission(
  context: ProductionOrderRequestContext,
): Promise<boolean> {
  const maxAttempts = 3;

  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    const result = await getMaterialWorkspaceSummary(context);
    if (result.status === "success") {
      return result.value.capabilities.canManage;
    }

    if (result.code === "permission-denied" || result.code === "unauthenticated") {
      return false;
    }

    if (!result.retryable || attempt === maxAttempts - 1) {
      return false;
    }

    await new Promise((resolve) => {
      setTimeout(resolve, 250 * (attempt + 1));
    });
  }

  return false;
}

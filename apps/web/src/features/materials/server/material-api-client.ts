import { stripFixtureRecords } from "@/lib/runtime/fixture-data";
import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./material-server-boundary";

import type {
  MaterialConsumptionObservation,
  MaterialDetail,
  MaterialLifecycleEvent,
  MaterialLotDetail,
  MaterialLotSummary,
  MaterialLotTimelineEntry,
  MaterialLotTraceability,
  MaterialLotWorkspaceSummary,
  MaterialRequirementOverview,
  MaterialRequirementProfileDetail,
  MaterialRequirementProfileSummary,
  MaterialStorageLocation,
  MaterialSummary,
  MaterialTechnicalSpecification,
  MaterialWorkspaceSummary,
  MaterialWriteResult,
} from "../domain/materials";
import {
  parseMaterialConsumptionObservationList,
  parseMaterialDetail,
  parseMaterialLifecycleEventList,
  parseMaterialLotDetail,
  parseMaterialLotSummaryList,
  parseMaterialLotTimelineEntryList,
  parseMaterialLotTraceability,
  parseMaterialLotWorkspaceSummary,
  parseMaterialRequirementOverview,
  parseMaterialRequirementProfileDetail,
  parseMaterialRequirementProfileSummaryList,
  parseMaterialStorageLocationList,
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
    | "subscription-required"
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

export type MaterialLotListFilters = Readonly<{
  materialId?: string;
  search?: string;
  supplierLotSearch?: string;
  warehouseReference?: string;
  locationReference?: string;
  status?: string;
  qualityStatus?: string;
  expiryState?: string;
  page?: number;
  pageSize?: number;
}>;

export type MaterialRequirementProfileListFilters = Readonly<{
  materialId?: string;
  requirementType?: string;
  page?: number;
  pageSize?: number;
}>;

export type MaterialConsumptionObservationListFilters = Readonly<{
  materialId?: string;
  requirementProfileId?: string;
  observationType?: string;
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

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): MaterialApiFailure["code"] {
  if (failure?.kind === "subscription-required") return "subscription-required";
  if (failure?.kind === "unauthenticated") return "unauthenticated";
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
      const failure = await classifyFailedResponse(response);
      return {
        status: "failure",
        code: mapFailureStatus(response.status, failure),
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `Malzeme isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const guarded = stripFixtureRecords(parser(payload));
    if (guarded === null) {
      // A fixture record is not a customer record outside isolated environments.
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;
    }
    return { status: "success", value: guarded };
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
      const failure = await classifyFailedResponse(response);
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
        code: mapFailureStatus(response.status, failure),
        message,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const guarded = stripFixtureRecords(parser(payload));
    if (guarded === null) {
      // A fixture record is not a customer record outside isolated environments.
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;
    }
    return { status: "success", value: guarded };
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

export function getMaterialLotWorkspaceSummary(
  context: ProductionOrderRequestContext,
  filters: Pick<MaterialLotListFilters, "materialId"> = {},
): Promise<MaterialApiResult<MaterialLotWorkspaceSummary>> {
  return getJson(
    context,
    "/api/materials/lots/workspace-summary",
    { materialId: filters.materialId },
    parseMaterialLotWorkspaceSummary,
  );
}

export function listMaterialLots(
  context: ProductionOrderRequestContext,
  filters: MaterialLotListFilters = {},
): Promise<MaterialApiResult<readonly MaterialLotSummary[]>> {
  return getJson(
    context,
    "/api/materials/lots",
    {
      materialId: filters.materialId,
      search: filters.search,
      supplierLotSearch: filters.supplierLotSearch,
      warehouseReference: filters.warehouseReference,
      locationReference: filters.locationReference,
      status: filters.status,
      qualityStatus: filters.qualityStatus,
      expiryState: filters.expiryState,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseMaterialLotSummaryList,
  );
}

export function getMaterialLot(
  context: ProductionOrderRequestContext,
  lotId: string,
): Promise<MaterialApiResult<MaterialLotDetail>> {
  return getJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}`,
    undefined,
    parseMaterialLotDetail,
  );
}

export function listMaterialStorageLocations(
  context: ProductionOrderRequestContext,
): Promise<MaterialApiResult<readonly MaterialStorageLocation[]>> {
  return getJson(context, "/api/materials/storage-locations", undefined, parseMaterialStorageLocationList);
}

export function getMaterialLotTimeline(
  context: ProductionOrderRequestContext,
  lotId: string,
): Promise<MaterialApiResult<readonly MaterialLotTimelineEntry[]>> {
  return getJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}/timeline`,
    undefined,
    parseMaterialLotTimelineEntryList,
  );
}

export function getMaterialLotTraceability(
  context: ProductionOrderRequestContext,
  lotId: string,
): Promise<MaterialApiResult<MaterialLotTraceability>> {
  return getJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}/traceability`,
    undefined,
    parseMaterialLotTraceability,
  );
}

export function createMaterialLot(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(context, "/api/materials/lots", "POST", body, parseMaterialWriteApiResponse);
}

export function observeMaterialLotReceipt(
  context: ProductionOrderRequestContext,
  lotId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}/receipt-observations`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function transferMaterialLot(
  context: ProductionOrderRequestContext,
  lotId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}/transfers`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function transitionMaterialLotStatus(
  context: ProductionOrderRequestContext,
  lotId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}/status-transitions`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function recordMaterialLotQualityStatus(
  context: ProductionOrderRequestContext,
  lotId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}/quality-status`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function linkMaterialLotCertificate(
  context: ProductionOrderRequestContext,
  lotId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/lots/${encodeURIComponent(lotId)}/certificates`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function getMaterialRequirementOverview(
  context: ProductionOrderRequestContext,
  materialId: string,
): Promise<MaterialApiResult<MaterialRequirementOverview>> {
  return getJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/requirement-overview`,
    undefined,
    parseMaterialRequirementOverview,
  );
}

export function listMaterialRequirementProfiles(
  context: ProductionOrderRequestContext,
  filters: MaterialRequirementProfileListFilters = {},
): Promise<MaterialApiResult<readonly MaterialRequirementProfileSummary[]>> {
  return getJson(
    context,
    "/api/materials/requirement-profiles",
    {
      materialId: filters.materialId,
      requirementType: filters.requirementType,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseMaterialRequirementProfileSummaryList,
  );
}

export function getMaterialRequirementProfile(
  context: ProductionOrderRequestContext,
  profileId: string,
): Promise<MaterialApiResult<MaterialRequirementProfileDetail>> {
  return getJson(
    context,
    `/api/materials/requirement-profiles/${encodeURIComponent(profileId)}`,
    undefined,
    parseMaterialRequirementProfileDetail,
  );
}

export function createMaterialRequirementProfile(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(context, "/api/materials/requirement-profiles", "POST", body, parseMaterialWriteApiResponse);
}

export function updateMaterialRequirementProfile(
  context: ProductionOrderRequestContext,
  profileId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/requirement-profiles/${encodeURIComponent(profileId)}`,
    "PUT",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function listMaterialConsumptionObservations(
  context: ProductionOrderRequestContext,
  filters: MaterialConsumptionObservationListFilters = {},
): Promise<MaterialApiResult<readonly MaterialConsumptionObservation[]>> {
  return getJson(
    context,
    "/api/materials/consumption-observations",
    {
      materialId: filters.materialId,
      requirementProfileId: filters.requirementProfileId,
      observationType: filters.observationType,
      page: filters.page?.toString(),
      pageSize: filters.pageSize?.toString(),
    },
    parseMaterialConsumptionObservationList,
  );
}

export function createMaterialConsumptionObservation(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    "/api/materials/consumption-observations",
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function evaluateMaterialRequirement(
  context: ProductionOrderRequestContext,
  materialId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/requirement-evaluations`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function evaluateMaterialVariance(
  context: ProductionOrderRequestContext,
  materialId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/variance-evaluations`,
    "POST",
    body,
    parseMaterialWriteApiResponse,
  );
}

export function evaluateMaterialRegrindPolicy(
  context: ProductionOrderRequestContext,
  materialId: string,
  body: Record<string, unknown>,
): Promise<MaterialApiResult<MaterialWriteResult>> {
  return mutateJson(
    context,
    `/api/materials/${encodeURIComponent(materialId)}/regrind-policy-evaluations`,
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

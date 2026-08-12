import "./material-server-boundary";

import {
  getMaterial,
  getMaterialLot,
  getMaterialLotTimeline,
  getMaterialLotTraceability,
  getMaterialLotWorkspaceSummary,
  getMaterialWorkspaceSummary,
  listMaterialLifecycleHistory,
  listMaterialLots,
  listMaterials,
  listMaterialStorageLocations,
  probeMaterialManagePermission,
  probeMaterialReadPermission,
} from "./material-api-client";
import { resolveMaterialRequestContext } from "./material-request-context";
import type {
  MaterialDetail,
  MaterialLifecycleEvent,
  MaterialLotDetail,
  MaterialLotSummary,
  MaterialLotTimelineEntry,
  MaterialLotTraceability,
  MaterialLotWorkspaceSummary,
  MaterialStorageLocation,
  MaterialSummary,
  MaterialWorkspaceSummary,
} from "../domain/materials";
import type { MaterialApiFailure, MaterialListFilters, MaterialLotListFilters } from "./material-api-client";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
  code?: MaterialApiFailure["code"] | "invalid-scope";
}>;

export type MaterialRegistryResult =
  | Readonly<{
      status: "success";
      summary: MaterialWorkspaceSummary;
      materials: readonly MaterialSummary[];
      canManage: boolean;
    }>
  | ReadFailure;

export type MaterialDetailResult =
  | Readonly<{
      status: "success";
      material: MaterialDetail;
      lifecycleHistory: readonly MaterialLifecycleEvent[];
      canManage: boolean;
    }>
  | ReadFailure;

export type MaterialLotsResult =
  | Readonly<{
      status: "success";
      summary: MaterialLotWorkspaceSummary;
      lots: readonly MaterialLotSummary[];
      storageLocations: readonly MaterialStorageLocation[];
      canManage: boolean;
    }>
  | ReadFailure;

export type MaterialLotDetailResult =
  | Readonly<{
      status: "success";
      lot: MaterialLotDetail;
      timeline: readonly MaterialLotTimelineEntry[];
      traceability: MaterialLotTraceability;
      storageLocations: readonly MaterialStorageLocation[];
      canManage: boolean;
    }>
  | ReadFailure;

async function resolveContext() {
  const contextResult = resolveMaterialRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure" as const,
      message: contextResult.message,
      code: contextResult.code as "configuration-error" | "unauthenticated" | "invalid-scope",
    };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readMaterialRegistry(
  filters: MaterialListFilters = {},
): Promise<MaterialRegistryResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [summaryResult, materialsResult] = await Promise.all([
    getMaterialWorkspaceSummary(resolved.context),
    listMaterials(resolved.context, filters),
  ]);

  if (summaryResult.status === "failure") {
    return {
      status: "failure",
      message: summaryResult.message,
      code: summaryResult.code,
    };
  }

  if (materialsResult.status === "failure") {
    return {
      status: "failure",
      message: materialsResult.message,
      code: materialsResult.code,
    };
  }

  const canManage = summaryResult.value.capabilities.canManage;

  return {
    status: "success",
    summary: summaryResult.value,
    materials: materialsResult.value,
    canManage,
  };
}

export async function readMaterialDetail(materialId: string): Promise<MaterialDetailResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [materialResult, historyResult] = await Promise.all([
    getMaterial(resolved.context, materialId),
    listMaterialLifecycleHistory(resolved.context, materialId),
  ]);

  if (materialResult.status === "failure") {
    return {
      status: "failure",
      message: materialResult.message,
      code: materialResult.code,
    };
  }

  if (historyResult.status === "failure") {
    return {
      status: "failure",
      message: historyResult.message,
      code: historyResult.code,
    };
  }

  return {
    status: "success",
    material: materialResult.value,
    lifecycleHistory: historyResult.value,
    canManage: await probeMaterialManagePermission(resolved.context),
  };
}

export async function readMaterialLotsForMaterial(
  materialId: string,
  filters: MaterialLotListFilters = {},
): Promise<MaterialLotsResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const lotFilters = { ...filters, materialId };
  const [summaryResult, lotsResult, locationsResult] = await Promise.all([
    getMaterialLotWorkspaceSummary(resolved.context, { materialId }),
    listMaterialLots(resolved.context, lotFilters),
    listMaterialStorageLocations(resolved.context),
  ]);

  if (summaryResult.status === "failure") {
    return {
      status: "failure",
      message: summaryResult.message,
      code: summaryResult.code,
    };
  }

  if (lotsResult.status === "failure") {
    return {
      status: "failure",
      message: lotsResult.message,
      code: lotsResult.code,
    };
  }

  if (locationsResult.status === "failure") {
    return {
      status: "failure",
      message: locationsResult.message,
      code: locationsResult.code,
    };
  }

  return {
    status: "success",
    summary: summaryResult.value,
    lots: lotsResult.value,
    storageLocations: locationsResult.value,
    canManage: await probeMaterialManagePermission(resolved.context),
  };
}

export async function readMaterialLotDetail(lotId: string): Promise<MaterialLotDetailResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [lotResult, timelineResult, traceabilityResult, locationsResult] = await Promise.all([
    getMaterialLot(resolved.context, lotId),
    getMaterialLotTimeline(resolved.context, lotId),
    getMaterialLotTraceability(resolved.context, lotId),
    listMaterialStorageLocations(resolved.context),
  ]);

  if (lotResult.status === "failure") {
    return {
      status: "failure",
      message: lotResult.message,
      code: lotResult.code,
    };
  }

  if (timelineResult.status === "failure") {
    return {
      status: "failure",
      message: timelineResult.message,
      code: timelineResult.code,
    };
  }

  if (traceabilityResult.status === "failure") {
    return {
      status: "failure",
      message: traceabilityResult.message,
      code: traceabilityResult.code,
    };
  }

  if (locationsResult.status === "failure") {
    return {
      status: "failure",
      message: locationsResult.message,
      code: locationsResult.code,
    };
  }

  return {
    status: "success",
    lot: lotResult.value,
    timeline: timelineResult.value,
    traceability: traceabilityResult.value,
    storageLocations: locationsResult.value,
    canManage: await probeMaterialManagePermission(resolved.context),
  };
}

export async function resolveMaterialNavigationVisibility(): Promise<boolean> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return false;
  }

  return probeMaterialReadPermission(resolved.context);
}

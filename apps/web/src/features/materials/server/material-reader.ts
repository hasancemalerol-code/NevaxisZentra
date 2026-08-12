import "./material-server-boundary";

import {
  getMaterial,
  getMaterialWorkspaceSummary,
  listMaterialLifecycleHistory,
  listMaterials,
  probeMaterialManagePermission,
  probeMaterialReadPermission,
} from "./material-api-client";
import { resolveMaterialRequestContext } from "./material-request-context";
import type {
  MaterialDetail,
  MaterialLifecycleEvent,
  MaterialSummary,
  MaterialWorkspaceSummary,
} from "../domain/materials";
import type { MaterialApiFailure, MaterialListFilters } from "./material-api-client";

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

export async function resolveMaterialNavigationVisibility(): Promise<boolean> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return false;
  }

  return probeMaterialReadPermission(resolved.context);
}

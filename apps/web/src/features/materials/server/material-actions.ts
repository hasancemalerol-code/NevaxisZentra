"use server";

import { readMaterialDetail, readMaterialLotDetail, readMaterialLotsForMaterial } from "./material-reader";
import type { MaterialLotListFilters } from "./material-api-client";

export async function loadMaterialDetailAction(materialId: string) {
  const result = await readMaterialDetail(materialId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    material: result.material,
    lifecycleHistory: result.lifecycleHistory,
    canManage: result.canManage,
  };
}

export async function loadMaterialLotsAction(materialId: string, filters: MaterialLotListFilters = {}) {
  const result = await readMaterialLotsForMaterial(materialId, filters);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    summary: result.summary,
    lots: result.lots,
    storageLocations: result.storageLocations,
    canManage: result.canManage,
  };
}

export async function loadMaterialLotDetailAction(lotId: string) {
  const result = await readMaterialLotDetail(lotId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    lot: result.lot,
    timeline: result.timeline,
    traceability: result.traceability,
    storageLocations: result.storageLocations,
    canManage: result.canManage,
  };
}

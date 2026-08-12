"use server";

import { readMaterialDetail } from "./material-reader";

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

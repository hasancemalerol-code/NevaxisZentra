"use server";

import { readMoldDetail } from "./mold-reader";

export async function loadMoldDetailAction(moldId: string) {
  const result = await readMoldDetail(moldId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    mold: result.mold,
    lifecycleHistory: result.lifecycleHistory,
    maintenanceHistory: result.maintenanceHistory,
    revisionHistory: result.revisionHistory,
    trialHistory: result.trialHistory,
    timeline: result.timeline,
    canManage: result.canManage,
  };
}

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
    canManage: result.canManage,
  };
}

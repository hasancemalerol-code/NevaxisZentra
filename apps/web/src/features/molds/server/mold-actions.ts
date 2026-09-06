"use server";

import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { readMoldDetail } from "./mold-reader";
import { evaluateMoldCompatibilityAll, getMoldCompatibilityOverview } from "./mold-api-client";
import { resolveMoldRequestContext } from "./mold-request-context";

export async function loadMoldDetailAction(moldId: string) {
  const authorization = await denyUnlessGranted("mold.read");
  if (authorization.status === "failure") {
    return authorization;
  }

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
    compatibilityOverview: result.compatibilityOverview,
    canManage: result.canManage,
  };
}

export async function evaluateMoldCompatibilityAllAction(moldId: string) {
  const authorization = await denyUnlessGranted("mold.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const contextResult = await resolveMoldRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const evaluateResult = await evaluateMoldCompatibilityAll(contextResult.context, moldId);
  if (evaluateResult.status === "failure") {
    return { status: "failure" as const, message: evaluateResult.message };
  }

  const overviewResult = await getMoldCompatibilityOverview(contextResult.context, moldId);
  if (overviewResult.status === "failure") {
    return {
      status: "success" as const,
      assessments: evaluateResult.value,
      compatibilityOverview: undefined,
    };
  }

  return {
    status: "success" as const,
    assessments: evaluateResult.value,
    compatibilityOverview: overviewResult.value,
  };
}

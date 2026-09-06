"use server";

import "./process-oee-server-boundary";

import { revalidatePath } from "next/cache";

import {
  evaluateProcessOeeWindow,
} from "./process-oee-api-client";
import type { ProcessOeeAssessmentListFilters } from "./process-oee-api-client";
import {
  readProcessOeeAssessmentDetail,
  readProcessOeeAssessments,
  readProcessOeeLossDetail,
  readProcessOeeShiftComparison,
  readProcessOeeStabilityDetail,
} from "./process-oee-reader";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { resolveProcessOeeRequestContext } from "./process-oee-request-context";

type ActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{ status: "failure"; message: string }>;

export async function loadProcessOeeAssessmentsAction(
  filters: ProcessOeeAssessmentListFilters = {},
) {
  const authorization = await denyUnlessGranted("process-oee.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readProcessOeeAssessments(filters);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    assessments: result.assessments,
  };
}

export async function loadProcessOeeAssessmentDetailAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("process-oee.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readProcessOeeAssessmentDetail(assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    assessment: result.assessment,
    canManage: result.canManage,
  };
}

export async function loadProcessOeeLossDetailAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("process-oee.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readProcessOeeLossDetail(assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    lossDetail: result.lossDetail,
  };
}

export async function loadProcessOeeStabilityDetailAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("process-oee.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readProcessOeeStabilityDetail(assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    stabilityDetail: result.stabilityDetail,
  };
}

export async function loadProcessOeeShiftComparisonAction(params: {
  machineAssetId?: string;
  periodStartUtc: string;
  periodEndUtc: string;
}) {
  const authorization = await denyUnlessGranted("process-oee.read");
  if (authorization.status === "failure") {
    return authorization;
  }

  const result = await readProcessOeeShiftComparison(params);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    shiftComparison: result.shiftComparison,
  };
}

export async function evaluateProcessOeeAction(
  windowId: string,
  idempotencyKey: string,
): Promise<ActionResult> {
  const authorization = await denyUnlessGranted("process-oee.manage");
  if (authorization.status === "failure") {
    return authorization;
  }

  const contextResult = await resolveProcessOeeRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await evaluateProcessOeeWindow(
    contextResult.context,
    windowId,
    idempotencyKey,
  );

  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  revalidatePath("/proses-oee");
  return {
    status: "success",
    id: result.value.id,
    version: result.value.version,
  };
}

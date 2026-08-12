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
} from "./process-oee-reader";
import { resolveProcessOeeRequestContext } from "./process-oee-request-context";

type ActionResult =
  | Readonly<{ status: "success"; id: string; version: number }>
  | Readonly<{ status: "failure"; message: string }>;

export async function loadProcessOeeAssessmentsAction(
  filters: ProcessOeeAssessmentListFilters = {},
) {
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
  const result = await readProcessOeeLossDetail(assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    lossDetail: result.lossDetail,
  };
}

export async function evaluateProcessOeeAction(
  windowId: string,
  idempotencyKey: string,
): Promise<ActionResult> {
  const contextResult = resolveProcessOeeRequestContext();
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

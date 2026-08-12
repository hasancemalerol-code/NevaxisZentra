import "./process-oee-server-boundary";

import {
  getProcessOeeAssessment,
  getProcessOeeWorkspaceSummary,
  listProcessOeeAssessments,
} from "./process-oee-api-client";
import type { ProcessOeeApiFailure, ProcessOeeAssessmentListFilters } from "./process-oee-api-client";
import { resolveProcessOeeRequestContext } from "./process-oee-request-context";
import type {
  ProcessOeeAssessmentDetail,
  ProcessOeeAssessmentSummary,
  ProcessOeeWorkspaceSummary,
} from "../domain/process-oee";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
  code?: ProcessOeeApiFailure["code"] | "invalid-scope";
}>;

export type ProcessOeeWorkspaceResult =
  | Readonly<{
      status: "success";
      summary: ProcessOeeWorkspaceSummary;
      assessments: readonly ProcessOeeAssessmentSummary[];
      canManage: boolean;
    }>
  | ReadFailure;

export type ProcessOeeAssessmentDetailResult =
  | Readonly<{
      status: "success";
      assessment: ProcessOeeAssessmentDetail;
      canManage: boolean;
    }>
  | ReadFailure;

async function resolveContext() {
  const contextResult = resolveProcessOeeRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure" as const,
      message: contextResult.message,
      code: contextResult.code as "configuration-error" | "unauthenticated" | "invalid-scope",
    };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readProcessOeeWorkspace(
  filters: ProcessOeeAssessmentListFilters = {},
): Promise<ProcessOeeWorkspaceResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [summaryResult, assessmentsResult] = await Promise.all([
    getProcessOeeWorkspaceSummary(resolved.context),
    listProcessOeeAssessments(resolved.context, filters),
  ]);

  if (summaryResult.status === "failure") {
    return {
      status: "failure",
      message: summaryResult.message,
      code: summaryResult.code,
    };
  }

  if (assessmentsResult.status === "failure") {
    return {
      status: "failure",
      message: assessmentsResult.message,
      code: assessmentsResult.code,
    };
  }

  return {
    status: "success",
    summary: summaryResult.value,
    assessments: assessmentsResult.value,
    canManage: summaryResult.value.capabilities.canManage,
  };
}

export async function readProcessOeeAssessmentDetail(
  assessmentId: string,
): Promise<ProcessOeeAssessmentDetailResult> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const [summaryResult, assessmentResult] = await Promise.all([
    getProcessOeeWorkspaceSummary(resolved.context),
    getProcessOeeAssessment(resolved.context, assessmentId),
  ]);

  if (assessmentResult.status === "failure") {
    return {
      status: "failure",
      message: assessmentResult.message,
      code: assessmentResult.code,
    };
  }

  if (summaryResult.status === "failure") {
    return {
      status: "failure",
      message: summaryResult.message,
      code: summaryResult.code,
    };
  }

  return {
    status: "success",
    assessment: assessmentResult.value,
    canManage: summaryResult.value.capabilities.canManage,
  };
}

export async function readProcessOeeAssessments(
  filters: ProcessOeeAssessmentListFilters = {},
): Promise<
  | Readonly<{ status: "success"; assessments: readonly ProcessOeeAssessmentSummary[] }>
  | ReadFailure
> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const assessmentsResult = await listProcessOeeAssessments(resolved.context, filters);
  if (assessmentsResult.status === "failure") {
    return {
      status: "failure",
      message: assessmentsResult.message,
      code: assessmentsResult.code,
    };
  }

  return {
    status: "success",
    assessments: assessmentsResult.value,
  };
}

export async function resolveProcessOeeNavigationVisibility(): Promise<boolean> {
  const resolved = await resolveContext();
  if (resolved.status === "failure") {
    return false;
  }

  const summaryResult = await getProcessOeeWorkspaceSummary(resolved.context);
  if (summaryResult.status === "success") {
    return summaryResult.value.capabilities.canRead;
  }

  return false;
}

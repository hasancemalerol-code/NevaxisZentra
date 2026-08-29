import "../../materials/server/material-server-boundary";

import { resolveMaterialRequestContext } from "../../materials/server/material-request-context";
import type {
  MaterialAvailabilityRiskDetail,
  MaterialAvailabilityRiskHistoryEntry,
  MaterialAvailabilityRiskWorkspacePayload,
} from "../domain/material-availability-risk";
import type { MaterialAvailabilityRiskListFilters } from "./material-availability-risk-api-client";
import {
  getMaterialAvailabilityRiskDetail,
  getMaterialAvailabilityRiskHistory,
  getMaterialAvailabilityRiskWorkspace,
} from "./material-availability-risk-api-client";

export type MaterialAvailabilityRiskWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: MaterialAvailabilityRiskWorkspacePayload;
      assessment?: MaterialAvailabilityRiskDetail;
      assessmentError?: string;
      history?: readonly MaterialAvailabilityRiskHistoryEntry[];
      historyError?: string;
    }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readMaterialAvailabilityRiskWorkspace(
  assessmentId?: string,
  filters: MaterialAvailabilityRiskListFilters = {},
): Promise<MaterialAvailabilityRiskWorkspaceResult> {
  const contextResult = resolveMaterialRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const workspaceResult = await getMaterialAvailabilityRiskWorkspace(contextResult.context, filters);
  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message };
  }

  const selectedAssessmentId =
    assessmentId ?? workspaceResult.value.assessments[0]?.id;
  let assessment: MaterialAvailabilityRiskDetail | undefined;
  let assessmentError: string | undefined;
  let history: readonly MaterialAvailabilityRiskHistoryEntry[] | undefined;
  let historyError: string | undefined;

  if (selectedAssessmentId) {
    const detailResult = await getMaterialAvailabilityRiskDetail(
      contextResult.context,
      selectedAssessmentId,
    );
    if (detailResult.status === "success") {
      assessment = detailResult.value;
    } else {
      assessmentError = detailResult.message;
    }

    const historyResult = await getMaterialAvailabilityRiskHistory(
      contextResult.context,
      selectedAssessmentId,
    );
    if (historyResult.status === "success") {
      history = historyResult.value;
    } else {
      historyError = historyResult.message;
    }
  }

  return {
    status: "success",
    workspace: workspaceResult.value,
    assessment,
    assessmentError,
    history,
    historyError,
  };
}

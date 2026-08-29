import "../../erp/server/erp-server-boundary";

import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  ProcurementDeliveryRiskDetail,
  ProcurementDeliveryRiskHistoryEntry,
  ProcurementDeliveryRiskWorkspacePayload,
} from "../domain/procurement-delivery-risk";
import type { ProcurementDeliveryRiskListFilters } from "./procurement-delivery-risk-api-client";
import {
  getProcurementDeliveryRiskDetail,
  getProcurementDeliveryRiskHistory,
  getProcurementDeliveryRiskWorkspace,
} from "./procurement-delivery-risk-api-client";

export type ProcurementDeliveryRiskWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: ProcurementDeliveryRiskWorkspacePayload;
      assessment?: ProcurementDeliveryRiskDetail;
      assessmentError?: string;
      history?: readonly ProcurementDeliveryRiskHistoryEntry[];
      historyError?: string;
    }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readProcurementDeliveryRiskWorkspace(
  assessmentId?: string,
  filters: ProcurementDeliveryRiskListFilters = {},
): Promise<ProcurementDeliveryRiskWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const workspaceResult = await getProcurementDeliveryRiskWorkspace(contextResult.context, filters);
  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message };
  }

  const selectedAssessmentId =
    assessmentId ?? workspaceResult.value.assessments[0]?.id;
  let assessment: ProcurementDeliveryRiskDetail | undefined;
  let assessmentError: string | undefined;
  let history: readonly ProcurementDeliveryRiskHistoryEntry[] | undefined;
  let historyError: string | undefined;

  if (selectedAssessmentId) {
    const detailResult = await getProcurementDeliveryRiskDetail(
      contextResult.context,
      selectedAssessmentId,
    );
    if (detailResult.status === "success") {
      assessment = detailResult.value;
    } else {
      assessmentError = detailResult.message;
    }

    const historyResult = await getProcurementDeliveryRiskHistory(
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

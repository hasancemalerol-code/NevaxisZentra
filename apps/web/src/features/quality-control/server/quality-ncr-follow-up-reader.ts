import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type {
  QualityNcrFollowUpFinding,
  QualityNcrFollowUpWorkspacePayload,
} from "../domain/quality-ncr-follow-up";
import {
  getQualityNcrFollowUpDetail,
  getQualityNcrFollowUpWorkspace,
} from "./quality-ncr-follow-up-api-client";

export type QualityNcrFollowUpWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: QualityNcrFollowUpWorkspacePayload;
      finding?: QualityNcrFollowUpFinding;
      findingError?: string;
    }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readQualityNcrFollowUpWorkspace(
  findingId?: string,
): Promise<QualityNcrFollowUpWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const workspaceResult = await getQualityNcrFollowUpWorkspace(context);
  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message };
  }

  const selectedFindingId =
    findingId ?? workspaceResult.value.findings[0]?.id;
  let finding: QualityNcrFollowUpFinding | undefined;
  let findingError: string | undefined;

  if (selectedFindingId) {
    const detailResult = await getQualityNcrFollowUpDetail(context, selectedFindingId);
    if (detailResult.status === "success") {
      finding = detailResult.value;
    } else {
      findingError = detailResult.message;
    }
  }

  return {
    status: "success",
    workspace: workspaceResult.value,
    finding,
    findingError,
  };
}

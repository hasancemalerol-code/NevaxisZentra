import { resolveMoldRequestContext } from "./mold-request-context";
import type {
  MoldFollowUpDetail,
  MoldFollowUpWorkspacePayload,
} from "../domain/mold-follow-up";
import {
  getMoldFollowUpDetail,
  getMoldFollowUpWorkspace,
} from "./mold-follow-up-api-client";

export type MoldFollowUpWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: MoldFollowUpWorkspacePayload;
      mold?: MoldFollowUpDetail;
      moldError?: string;
    }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readMoldFollowUpWorkspace(
  moldId?: string,
): Promise<MoldFollowUpWorkspaceResult> {
  const contextResult = await resolveMoldRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const workspaceResult = await getMoldFollowUpWorkspace(context);
  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message };
  }

  const selectedMoldId =
    moldId ?? workspaceResult.value.molds[0]?.id;
  let mold: MoldFollowUpDetail | undefined;
  let moldError: string | undefined;

  if (selectedMoldId) {
    const detailResult = await getMoldFollowUpDetail(context, selectedMoldId);
    if (detailResult.status === "success") {
      mold = detailResult.value;
    } else {
      moldError = detailResult.message;
    }
  }

  return {
    status: "success",
    workspace: workspaceResult.value,
    mold,
    moldError,
  };
}

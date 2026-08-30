import "./teams-server-boundary";

import {
  getTeamsApproval,
  getTeamsChannelDetail,
  getTeamsMessageSourceContext,
  getTeamsTaskContextChannel,
  listTeamsApprovals,
  listTeamsChannels,
  type RawTeamsApprovalDetail,
  type RawTeamsApprovalListResponse,
  type RawTeamsChannelDetail,
  type RawTeamsChannelListResponse,
  type RawTeamsMessageSourceContext,
  type RawTeamsTaskContextChannel,
  type TeamsApiResult,
} from "./teams-api-client";
import { resolveTeamsRequestContext } from "./teams-request-context";

export type TeamsWorkspaceReadResult =
  | Readonly<{
      status: "success";
      channels: RawTeamsChannelListResponse;
      selectedChannel: RawTeamsChannelDetail | null;
      canWrite: boolean;
    }>
  | Readonly<{ status: "failure"; message: string; code: string }>;

export async function readTeamsWorkspace(
  selectedChannelId?: string | null,
): Promise<TeamsWorkspaceReadResult> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const channelsResult = await listTeamsChannels(contextResult.context);
  if (channelsResult.status === "failure") {
    return { status: "failure", message: channelsResult.message, code: channelsResult.code };
  }

  const channelId = selectedChannelId
    ?? channelsResult.value.items[0]?.id
    ?? null;

  if (!channelId) {
    return {
      status: "success",
      channels: channelsResult.value,
      selectedChannel: null,
      canWrite: channelsResult.value.capabilities.canWrite,
    };
  }

  const detailResult = await getTeamsChannelDetail(contextResult.context, channelId);
  if (detailResult.status === "failure") {
    return { status: "failure", message: detailResult.message, code: detailResult.code };
  }

  return {
    status: "success",
    channels: channelsResult.value,
    selectedChannel: detailResult.value,
    canWrite: channelsResult.value.capabilities.canWrite,
  };
}

export async function readTeamsTaskContextLink(
  taskId: string,
): Promise<TeamsApiResult<RawTeamsTaskContextChannel>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return getTeamsTaskContextChannel(contextResult.context, taskId);
}

export async function readTeamsApprovals(
  channelId?: string | null,
): Promise<TeamsApiResult<RawTeamsApprovalListResponse>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return listTeamsApprovals(contextResult.context, {
    channelId: channelId ?? undefined,
  });
}

export async function readTeamsApproval(
  requestId: string,
): Promise<TeamsApiResult<RawTeamsApprovalDetail>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return getTeamsApproval(contextResult.context, requestId);
}

export async function readTeamsMessageSourceContext(
  messageId: string,
): Promise<TeamsApiResult<RawTeamsMessageSourceContext>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return getTeamsMessageSourceContext(contextResult.context, messageId);
}

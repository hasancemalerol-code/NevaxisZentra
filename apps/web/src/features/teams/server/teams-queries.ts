import "./teams-server-boundary";

import { cache } from "react";

import {
  getTeamsApproval,
  getTeamsChannelDetail,
  getTeamsHandover,
  getTeamsMessageSourceContext,
  getTeamsWorkInbox,
  getTeamsNotifications,
  getTeamsDirectConversation,
  listTeamsDirectConversations,
  listTeamsDirectRecipients,
  type RawTeamsDirectConversationDetail,
  type RawTeamsDirectConversationListResponse,
  type RawTeamsDirectRecipientListResponse,
  getTeamsTaskContextChannel,
  listTeamsApprovals,
  listTeamsChannels,
  listTeamsHandovers,
  type RawTeamsApprovalDetail,
  type RawTeamsApprovalListResponse,
  type RawTeamsHandoverDetail,
  type RawTeamsHandoverListResponse,
  type RawTeamsWorkInboxResponse,
  type RawTeamsNotificationListResponse,
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

export async function readTeamsHandovers(
  channelId?: string | null,
): Promise<TeamsApiResult<RawTeamsHandoverListResponse>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return listTeamsHandovers(contextResult.context, {
    channelId: channelId ?? undefined,
  });
}

export async function readTeamsHandover(
  handoverId: string,
): Promise<TeamsApiResult<RawTeamsHandoverDetail>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return getTeamsHandover(contextResult.context, handoverId);
}

export async function readTeamsWorkInbox(): Promise<TeamsApiResult<RawTeamsWorkInboxResponse>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return getTeamsWorkInbox(contextResult.context);
}

export async function readTeamsDirectConversations(): Promise<TeamsApiResult<RawTeamsDirectConversationListResponse>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return listTeamsDirectConversations(contextResult.context);
}

export async function readTeamsDirectRecipients(): Promise<TeamsApiResult<RawTeamsDirectRecipientListResponse>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return listTeamsDirectRecipients(contextResult.context);
}

export async function readTeamsDirectConversation(
  conversationId: string,
): Promise<TeamsApiResult<RawTeamsDirectConversationDetail>> {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return getTeamsDirectConversation(contextResult.context, conversationId);
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

export async function readTeamsNotifications(options?: Readonly<{
  unreadOnly?: boolean;
  page?: number;
  pageSize?: number;
}>): Promise<TeamsApiResult<RawTeamsNotificationListResponse>> {
  return readTeamsNotificationsCached(options);
}

const readTeamsNotificationsCached = cache(async (options?: Readonly<{
  unreadOnly?: boolean;
  page?: number;
  pageSize?: number;
}>): Promise<TeamsApiResult<RawTeamsNotificationListResponse>> => {
  const contextResult = await resolveTeamsRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      code: "source-unavailable",
      message: contextResult.message,
      retryable: false,
    };
  }

  return getTeamsNotifications(contextResult.context, options);
});

import "./teams-server-boundary";

import { fetchTrustedApi } from "@/features/saas-identity/server/authenticated-server-fetch";
import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { TeamsRequestContext } from "./teams-request-context";

export type TeamsApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "invalid-request"
    | "not-found"
    | "idempotency-conflict";
  message: string;
  retryable: boolean;
}>;

export type TeamsApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | TeamsApiFailure;

export type RawTeamsChannelSummary = Readonly<{
  id: string;
  name: string;
  channelType: string;
  aiTaskId: string | null;
  memberCount: number;
  unreadCount: number;
  latestMessage: Readonly<{
    messageId: string;
    authorLabel: string;
    bodyPreview: string;
    createdAt: string;
  }> | null;
  lastReadAt: string | null;
  currentMemberRole: string;
}>;

export type RawTeamsMessage = Readonly<{
  id: string;
  channelId: string;
  authorTenantMembershipId: string;
  authorLabel: string;
  body: string;
  sourceContext: Readonly<{
    contextType: string;
    label: string | null;
    deepLinkPath: string | null;
    aiTaskId: string | null;
    sourceModule: string | null;
    sourceEntityId: string | null;
    canOpen: boolean;
  }> | null;
  mentions: ReadonlyArray<Readonly<{
    tenantMembershipId: string;
    displayLabel: string;
  }>>;
  linkedTask: Readonly<{
    taskId: string;
    taskReference: string;
    taskStatus: string;
    taskDeepLinkPath: string;
    canOpenTask: boolean;
  }> | null;
  createdAt: string;
}>;

export type RawTeamsChannelDetail = Readonly<{
  id: string;
  name: string;
  channelType: string;
  aiTaskId: string | null;
  taskContext: Readonly<{
    contextType: string;
    label: string | null;
    deepLinkPath: string | null;
    aiTaskId: string | null;
    canOpen: boolean;
  }> | null;
  members: ReadonlyArray<Readonly<{
    tenantMembershipId: string;
    displayLabel: string;
    role: string;
  }>>;
  messages: readonly RawTeamsMessage[];
  totalMessageCount: number;
  page: number;
  pageSize: number;
  lastReadAt: string | null;
  lastReadMessageId: string | null;
  currentMemberRole: string;
}>;

export type RawTeamsChannelListResponse = Readonly<{
  items: readonly RawTeamsChannelSummary[];
  capabilities: Readonly<{
    canRead: boolean;
    canWrite: boolean;
    canManage: boolean;
  }>;
  provenanceDisclaimer: string;
}>;

export type RawTeamsTaskContextChannel = Readonly<{
  channelId: string;
  channelName: string;
  teamsDeepLinkPath: string;
  canOpenTask: boolean;
  taskDeepLinkPath: string | null;
  taskTitle: string | null;
}>;

export type RawTeamsMessageSourceContext = Readonly<{
  messageId: string;
  channelId: string;
  channelName: string;
  messageBodyPreview: string;
  teamsDeepLinkPath: string;
  canOpen: boolean;
  unavailableReason: string | null;
}>;

export type RawTeamsMessageTaskDraftResult = Readonly<{
  taskId: string;
  taskReference: string;
  taskStatus: string;
  idempotentReplay: boolean;
  disclaimer: string;
}>;

export type RawTeamsApprovalSummary = Readonly<{
  id: string;
  title: string;
  status: string;
  channelName: string;
  channelId: string;
  requestedByLabel: string;
  createdAt: string;
  sourceKind: string;
  canOpen: boolean;
}>;

export type RawTeamsApprovalListResponse = Readonly<{
  items: readonly RawTeamsApprovalSummary[];
  capabilities: Readonly<{
    canCreate: boolean;
    canReview: boolean;
    canManage: boolean;
  }>;
  disclaimer: string;
}>;

export type RawTeamsApprovalDetail = Readonly<{
  id: string;
  title: string;
  rationale: string;
  status: string;
  channelId: string;
  channelName: string;
  requestedByTenantMembershipId: string;
  requestedByLabel: string;
  source: Readonly<{
    sourceKind: string;
    messageId: string | null;
    aiTaskId: string | null;
    channelName: string;
    label: string | null;
    deepLinkPath: string | null;
    canOpen: boolean;
    unavailableReason: string | null;
  }>;
  reviewers: ReadonlyArray<Readonly<{
    tenantMembershipId: string;
    displayLabel: string;
    role: string;
  }>>;
  decision: Readonly<{
    id: string;
    reviewerTenantMembershipId: string;
    reviewerLabel: string;
    decision: string;
    note: string | null;
    createdAt: string;
  }> | null;
  auditEvents: ReadonlyArray<Readonly<{
    id: string;
    eventType: string;
    actorLabel: string;
    occurredAt: string;
  }>>;
  createdAt: string;
  resolvedAt: string | null;
  cancelledAt: string | null;
  canDecide: boolean;
  canCancel: boolean;
  canManageReviewers: boolean;
  disclaimer: string;
}>;

function buildDevelopmentUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

function buildSessionPath(
  facilityId: string,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const params = new URLSearchParams();
  params.set("facilityId", facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        params.set(key, value);
      }
    }
  }
  return `${path}?${params.toString()}`;
}

function mapFailureStatus(status: number, _payload: unknown): TeamsApiFailure["code"] {
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "idempotency-conflict";
  return "source-unavailable";
}

function failureMessage(status: number, payload: unknown): string {
  if (typeof payload === "object" && payload) {
    const title = "title" in payload ? String((payload as { title?: string }).title ?? "") : "";
    return title || `Teams isteği başarısız (${status}).`;
  }
  return `Teams isteği başarısız (${status}).`;
}

function parseChannelList(payload: unknown): RawTeamsChannelListResponse {
  const raw = payload as RawTeamsChannelListResponse;
  return {
    ...raw,
    items: raw.items ?? [],
    provenanceDisclaimer: raw.provenanceDisclaimer ?? "",
    capabilities: {
      canRead: raw.capabilities?.canRead === true,
      canWrite: raw.capabilities?.canWrite === true,
      canManage: raw.capabilities?.canManage === true,
    },
  };
}

async function getJson<T>(
  context: TeamsRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<TeamsApiResult<T>> {
  try {
    if (context.mode === "session") {
      const fetchResult = await fetchTrustedApi(
        buildSessionPath(context.facilityId, path, query),
        { method: "GET" },
      );
      if (fetchResult.status === "failure") {
        return {
          status: "failure",
          code: "source-unavailable",
          message: fetchResult.message,
          retryable: fetchResult.code === "network-error",
        };
      }

      const response = fetchResult.response;
      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        return {
          status: "failure",
          code: mapFailureStatus(response.status, payload),
          message: failureMessage(response.status, payload),
          retryable: response.status >= 500,
        };
      }

      return { status: "success", value: parser(payload) };
    }

    const response = await fetch(buildDevelopmentUrl(context.context, path, query), {
      headers: context.context.requestHeaders,
      cache: "no-store",
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status, payload),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Teams API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

async function writeJson<T>(
  context: TeamsRequestContext,
  method: "POST" | "DELETE",
  path: string,
  body: unknown | undefined,
  parser: (payload: unknown) => T,
): Promise<TeamsApiResult<T>> {
  try {
    if (context.mode === "session") {
      const fetchResult = await fetchTrustedApi(
        buildSessionPath(context.facilityId, path),
        {
          method,
          headers: { "Content-Type": "application/json" },
          body: body === undefined ? undefined : JSON.stringify(body),
        },
      );
      if (fetchResult.status === "failure") {
        return {
          status: "failure",
          code: "source-unavailable",
          message: fetchResult.message,
          retryable: fetchResult.code === "network-error",
        };
      }

      const response = fetchResult.response;
      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        return {
          status: "failure",
          code: mapFailureStatus(response.status, payload),
          message: failureMessage(response.status, payload),
          retryable: response.status >= 500,
        };
      }

      return { status: "success", value: parser(payload) };
    }

    const response = await fetch(buildDevelopmentUrl(context.context, path), {
      method,
      headers: {
        ...context.context.requestHeaders,
        "Content-Type": "application/json",
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      cache: "no-store",
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return {
        status: "failure",
        code: mapFailureStatus(response.status, payload),
        message: failureMessage(response.status, payload),
        retryable: response.status >= 500,
      };
    }
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Teams API'sine ulaşılamadı.",
      retryable: true,
    };
  }
}

export async function listTeamsChannels(
  context: TeamsRequestContext,
): Promise<TeamsApiResult<RawTeamsChannelListResponse>> {
  return getJson(context, "/api/teams/channels", undefined, parseChannelList);
}

export async function getTeamsChannelDetail(
  context: TeamsRequestContext,
  channelId: string,
  page = 1,
  pageSize = 25,
): Promise<TeamsApiResult<RawTeamsChannelDetail>> {
  return getJson(
    context,
    `/api/teams/channels/${channelId}`,
    { page: String(page), pageSize: String(pageSize) },
    (payload) => payload as RawTeamsChannelDetail,
  );
}

export async function getTeamsTaskContextChannel(
  context: TeamsRequestContext,
  taskId: string,
): Promise<TeamsApiResult<RawTeamsTaskContextChannel>> {
  return getJson(
    context,
    `/api/teams/channels/by-task/${taskId}`,
    undefined,
    (payload) => payload as RawTeamsTaskContextChannel,
  );
}

export async function postTeamsMessage(
  context: TeamsRequestContext,
  channelId: string,
  body: Readonly<{
    body: string;
    idempotencyKey: string;
    mentionedMembershipIds?: readonly string[];
    aiTaskId?: string | null;
  }>,
): Promise<TeamsApiResult<RawTeamsMessage>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/channels/${channelId}/messages`,
    {
      body: body.body,
      idempotencyKey: body.idempotencyKey,
      mentionedMembershipIds: body.mentionedMembershipIds ?? [],
      aiTaskId: body.aiTaskId ?? null,
      sourceModule: null,
      sourceEntityId: null,
    },
    (payload) => payload as RawTeamsMessage,
  );
}

export async function markTeamsChannelRead(
  context: TeamsRequestContext,
  channelId: string,
  lastReadMessageId: string | null,
): Promise<TeamsApiResult<{ id: string; idempotentReplay: boolean }>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/channels/${channelId}/read`,
    { lastReadMessageId },
    (payload) => payload as { id: string; idempotentReplay: boolean },
  );
}

export async function createTeamsChannel(
  context: TeamsRequestContext,
  payload: Readonly<{
    name: string;
    channelType: string;
    aiTaskId?: string | null;
    initialMemberMembershipIds?: readonly string[];
  }>,
): Promise<TeamsApiResult<{ id: string; idempotentReplay: boolean }>> {
  return writeJson(
    context,
    "POST",
    "/api/teams/channels",
    payload,
    (value) => value as { id: string; idempotentReplay: boolean },
  );
}

export async function createMessageTaskDraft(
  context: TeamsRequestContext,
  messageId: string,
  body: Readonly<{
    title: string;
    category: string;
    priority: string;
    humanNote?: string | null;
  }>,
): Promise<TeamsApiResult<RawTeamsMessageTaskDraftResult>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/messages/${messageId}/task-draft`,
    {
      title: body.title,
      category: body.category,
      priority: body.priority,
      humanNote: body.humanNote ?? null,
    },
    (payload) => payload as RawTeamsMessageTaskDraftResult,
  );
}

export async function listTeamsApprovals(
  context: TeamsRequestContext,
  query?: Readonly<{ channelId?: string; status?: string }>,
): Promise<TeamsApiResult<RawTeamsApprovalListResponse>> {
  return getJson(
    context,
    "/api/teams/approvals",
    {
      channelId: query?.channelId,
      status: query?.status,
    },
    (payload) => payload as RawTeamsApprovalListResponse,
  );
}

export async function getTeamsApproval(
  context: TeamsRequestContext,
  requestId: string,
): Promise<TeamsApiResult<RawTeamsApprovalDetail>> {
  return getJson(
    context,
    `/api/teams/approvals/${requestId}`,
    undefined,
    (payload) => payload as RawTeamsApprovalDetail,
  );
}

export async function createTeamsApproval(
  context: TeamsRequestContext,
  body: Readonly<{
    channelId: string;
    messageId?: string | null;
    aiTaskId?: string | null;
    title: string;
    rationale: string;
    idempotencyKey: string;
    reviewers: ReadonlyArray<Readonly<{ tenantMembershipId: string; role: string }>>;
  }>,
): Promise<TeamsApiResult<RawTeamsApprovalDetail>> {
  return writeJson(
    context,
    "POST",
    "/api/teams/approvals",
    body,
    (payload) => payload as RawTeamsApprovalDetail,
  );
}

export async function decideTeamsApproval(
  context: TeamsRequestContext,
  requestId: string,
  body: Readonly<{ decision: string; note?: string | null }>,
): Promise<TeamsApiResult<RawTeamsApprovalDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/approvals/${requestId}/decisions`,
    body,
    (payload) => payload as RawTeamsApprovalDetail,
  );
}

export async function cancelTeamsApproval(
  context: TeamsRequestContext,
  requestId: string,
): Promise<TeamsApiResult<RawTeamsApprovalDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/approvals/${requestId}/cancel`,
    undefined,
    (payload) => payload as RawTeamsApprovalDetail,
  );
}

export async function assignTeamsApprovalReviewer(
  context: TeamsRequestContext,
  requestId: string,
  body: Readonly<{ tenantMembershipId: string; role: string }>,
): Promise<TeamsApiResult<RawTeamsApprovalDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/approvals/${requestId}/reviewers`,
    body,
    (payload) => payload as RawTeamsApprovalDetail,
  );
}

export type RawTeamsHandoverSummary = Readonly<{
  id: string;
  channelId: string;
  channelName: string;
  shiftDate: string;
  shiftType: string;
  status: string;
  authorLabel: string;
  createdAt: string;
  publishedAt: string | null;
  itemCount: number;
  acknowledgementCount: number;
}>;

export type RawTeamsHandoverListResponse = Readonly<{
  items: readonly RawTeamsHandoverSummary[];
  capabilities: Readonly<{
    canCreate: boolean;
    canAcknowledge: boolean;
    canManage: boolean;
  }>;
  disclaimer: string;
}>;

export type RawTeamsHandoverItem = Readonly<{
  id: string;
  itemKind: string;
  title: string;
  body: string;
  messageId: string | null;
  aiTaskId: string | null;
  sourceKind: string | null;
  sourceLabel: string | null;
  deepLinkPath: string | null;
  canOpen: boolean;
  unavailableReason: string | null;
}>;

export type RawTeamsHandoverDetail = Readonly<{
  id: string;
  channelId: string;
  channelName: string;
  authorTenantMembershipId: string;
  authorLabel: string;
  shiftDate: string;
  shiftType: string;
  status: string;
  summary: string;
  safetyNote: string;
  items: readonly RawTeamsHandoverItem[];
  acknowledgements: ReadonlyArray<Readonly<{
    tenantMembershipId: string;
    displayLabel: string;
    acknowledgedAt: string;
  }>>;
  auditEvents: ReadonlyArray<Readonly<{
    id: string;
    eventType: string;
    actorLabel: string;
    occurredAt: string;
  }>>;
  createdAt: string;
  publishedAt: string | null;
  canEdit: boolean;
  canPublish: boolean;
  canAcknowledge: boolean;
  disclaimer: string;
}>;

export type TeamsHandoverItemWriteBody = Readonly<{
  itemKind: string;
  title: string;
  body: string;
  messageId?: string | null;
  aiTaskId?: string | null;
}>;

export async function listTeamsHandovers(
  context: TeamsRequestContext,
  query?: Readonly<{ channelId?: string; status?: string }>,
): Promise<TeamsApiResult<RawTeamsHandoverListResponse>> {
  return getJson(
    context,
    "/api/teams/handovers",
    {
      channelId: query?.channelId,
      status: query?.status,
    },
    (payload) => payload as RawTeamsHandoverListResponse,
  );
}

export async function getTeamsHandover(
  context: TeamsRequestContext,
  handoverId: string,
): Promise<TeamsApiResult<RawTeamsHandoverDetail>> {
  return getJson(
    context,
    `/api/teams/handovers/${handoverId}`,
    undefined,
    (payload) => payload as RawTeamsHandoverDetail,
  );
}

export async function createTeamsHandover(
  context: TeamsRequestContext,
  body: Readonly<{
    channelId: string;
    shiftDate: string;
    shiftType: string;
    summary: string;
    safetyNote: string;
    idempotencyKey: string;
    items: readonly TeamsHandoverItemWriteBody[];
  }>,
): Promise<TeamsApiResult<RawTeamsHandoverDetail>> {
  return writeJson(
    context,
    "POST",
    "/api/teams/handovers",
    body,
    (payload) => payload as RawTeamsHandoverDetail,
  );
}

export async function updateTeamsHandoverDraft(
  context: TeamsRequestContext,
  handoverId: string,
  body: Readonly<{
    shiftDate: string;
    shiftType: string;
    summary: string;
    safetyNote: string;
    items: readonly TeamsHandoverItemWriteBody[];
  }>,
): Promise<TeamsApiResult<RawTeamsHandoverDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/handovers/${handoverId}`,
    body,
    (payload) => payload as RawTeamsHandoverDetail,
  );
}

export async function publishTeamsHandover(
  context: TeamsRequestContext,
  handoverId: string,
): Promise<TeamsApiResult<RawTeamsHandoverDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/handovers/${handoverId}/publish`,
    undefined,
    (payload) => payload as RawTeamsHandoverDetail,
  );
}

export async function acknowledgeTeamsHandover(
  context: TeamsRequestContext,
  handoverId: string,
): Promise<TeamsApiResult<RawTeamsHandoverDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/handovers/${handoverId}/acknowledgements`,
    undefined,
    (payload) => payload as RawTeamsHandoverDetail,
  );
}

export type RawTeamsWorkInboxItem = Readonly<{
  sourceKind: string;
  sourceId: string;
  title: string;
  reason: string;
  occurredAt: string;
  deepLinkPath: string;
}>;

export type RawTeamsWorkInboxResponse = Readonly<{
  items: readonly RawTeamsWorkInboxItem[];
  sources: Readonly<{
    aiTasks: boolean;
    approvals: boolean;
    handovers: boolean;
    mentions: boolean;
  }>;
  disclaimer: string;
}>;

export async function getTeamsWorkInbox(
  context: TeamsRequestContext,
): Promise<TeamsApiResult<RawTeamsWorkInboxResponse>> {
  return getJson(
    context,
    "/api/teams/work-inbox",
    undefined,
    (payload) => payload as RawTeamsWorkInboxResponse,
  );
}

export type RawTeamsDirectRecipient = Readonly<{
  tenantMembershipId: string;
  displayLabel: string;
}>;

export type RawTeamsDirectConversationSummary = Readonly<{
  id: string;
  otherParticipantMembershipId: string;
  otherParticipantLabel: string;
  latestMessagePreview: string | null;
  latestMessageAt: string | null;
  unreadCount: number;
  createdAt: string;
}>;

export type RawTeamsDirectMessage = Readonly<{
  id: string;
  authorTenantMembershipId: string;
  authorLabel: string;
  body: string;
  createdAt: string;
  isOwnMessage: boolean;
}>;

export type RawTeamsDirectConversationDetail = Readonly<{
  id: string;
  otherParticipantMembershipId: string;
  otherParticipantLabel: string;
  messages: readonly RawTeamsDirectMessage[];
  totalMessageCount: number;
  page: number;
  pageSize: number;
  lastReadMessageId: string | null;
  lastReadAt: string | null;
  canWrite: boolean;
  disclaimer: string;
}>;

export type RawTeamsDirectConversationListResponse = Readonly<{
  items: readonly RawTeamsDirectConversationSummary[];
  capabilities: Readonly<{ canWrite: boolean }>;
  disclaimer: string;
}>;

export type RawTeamsDirectRecipientListResponse = Readonly<{
  items: readonly RawTeamsDirectRecipient[];
}>;

export async function listTeamsDirectRecipients(
  context: TeamsRequestContext,
): Promise<TeamsApiResult<RawTeamsDirectRecipientListResponse>> {
  return getJson(
    context,
    "/api/teams/direct-messages/recipients",
    undefined,
    (payload) => payload as RawTeamsDirectRecipientListResponse,
  );
}

export async function listTeamsDirectConversations(
  context: TeamsRequestContext,
): Promise<TeamsApiResult<RawTeamsDirectConversationListResponse>> {
  return getJson(
    context,
    "/api/teams/direct-messages",
    undefined,
    (payload) => payload as RawTeamsDirectConversationListResponse,
  );
}

export async function getTeamsDirectConversation(
  context: TeamsRequestContext,
  conversationId: string,
  query?: Readonly<{ page?: string; pageSize?: string }>,
): Promise<TeamsApiResult<RawTeamsDirectConversationDetail>> {
  return getJson(
    context,
    `/api/teams/direct-messages/${conversationId}`,
    query,
    (payload) => payload as RawTeamsDirectConversationDetail,
  );
}

export async function createTeamsDirectConversation(
  context: TeamsRequestContext,
  body: Readonly<{ recipientTenantMembershipId: string; idempotencyKey: string }>,
): Promise<TeamsApiResult<RawTeamsDirectConversationDetail>> {
  return writeJson(
    context,
    "POST",
    "/api/teams/direct-messages",
    body,
    (payload) => payload as RawTeamsDirectConversationDetail,
  );
}

export async function postTeamsDirectMessage(
  context: TeamsRequestContext,
  conversationId: string,
  body: Readonly<{ body: string; idempotencyKey: string }>,
): Promise<TeamsApiResult<RawTeamsDirectConversationDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/direct-messages/${conversationId}/messages`,
    body,
    (payload) => payload as RawTeamsDirectConversationDetail,
  );
}

export async function markTeamsDirectConversationRead(
  context: TeamsRequestContext,
  conversationId: string,
  body: Readonly<{ lastReadMessageId: string | null }>,
): Promise<TeamsApiResult<RawTeamsDirectConversationDetail>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/direct-messages/${conversationId}/read`,
    body,
    (payload) => payload as RawTeamsDirectConversationDetail,
  );
}

export async function getTeamsMessageSourceContext(
  context: TeamsRequestContext,
  messageId: string,
): Promise<TeamsApiResult<RawTeamsMessageSourceContext>> {
  return getJson(
    context,
    `/api/teams/messages/${messageId}/source-context`,
    undefined,
    (payload) => payload as RawTeamsMessageSourceContext,
  );
}

export type RawTeamsNotificationItem = Readonly<{
  id: string;
  notificationType: string;
  sourceModule: string;
  sourceEntityId: string;
  deepLinkPath: string;
  title: string;
  reason: string;
  sourcePreview: string | null;
  createdAt: string;
  readAt: string | null;
  canOpen: boolean;
  unavailableReason: string | null;
}>;

export type RawTeamsNotificationListResponse = Readonly<{
  items: readonly RawTeamsNotificationItem[];
  unreadCount: number;
  disclaimer: string;
}>;

export async function getTeamsNotifications(
  context: TeamsRequestContext,
  query?: Readonly<{ unreadOnly?: boolean; page?: number; pageSize?: number }>,
): Promise<TeamsApiResult<RawTeamsNotificationListResponse>> {
  const params = new URLSearchParams();
  if (query?.unreadOnly) params.set("unreadOnly", "true");
  if (query?.page) params.set("page", String(query.page));
  if (query?.pageSize) params.set("pageSize", String(query.pageSize));
  const suffix = params.size > 0 ? `?${params.toString()}` : "";
  return getJson(
    context,
    `/api/teams/notifications${suffix}`,
    undefined,
    (payload) => payload as RawTeamsNotificationListResponse,
  );
}

export async function markTeamsNotificationRead(
  context: TeamsRequestContext,
  notificationId: string,
): Promise<TeamsApiResult<{ id: string; idempotentReplay: boolean }>> {
  return writeJson(
    context,
    "POST",
    `/api/teams/notifications/${notificationId}/read`,
    undefined,
    (payload) => payload as { id: string; idempotentReplay: boolean },
  );
}

// Kept for callers that need the API base URL in session mode.
export function resolveTeamsApiBaseUrl(): string | null {
  const config = resolveSaasIdentityRuntimeConfig();
  return config.status === "success" ? config.config.apiBaseUrl : null;
}

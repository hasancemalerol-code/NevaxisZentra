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

function mapFailureStatus(status: number, payload: unknown): TeamsApiFailure["code"] {
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

// Kept for callers that need the API base URL in session mode.
export function resolveTeamsApiBaseUrl(): string | null {
  const config = resolveSaasIdentityRuntimeConfig();
  return config.status === "success" ? config.config.apiBaseUrl : null;
}

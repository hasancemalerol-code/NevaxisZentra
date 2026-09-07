import "./ai-task-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type RawAiTaskDelivery = Readonly<{
  id: string;
  aiTaskId: string;
  channel: string;
  recipientKind: string;
  recipientUserId: string | null;
  maskedRecipient: string | null;
  status: string;
  idempotencyKey: string;
  attemptCount: number;
  maxAttempts: number;
  nextAttemptAtUtc: string | null;
  lastErrorCode: string | null;
  lastErrorCategory: string | null;
  requestedAtUtc: string;
  sentAtUtc: string | null;
  cancelledAtUtc: string | null;
  secureAccessGrantId: string | null;
  provider: string | null;
  providerMessageId: string | null;
  providerStatus: string | null;
  providerStatusAtUtc: string | null;
  consentStatus: string | null;
  createdAtUtc: string;
}>;

export type RawAiTaskSecureAccessGrant = Readonly<{
  id: string;
  aiTaskId: string;
  scope: string;
  issuedAtUtc: string;
  expiresAtUtc: string;
  revokedAtUtc: string | null;
  accessCount: number;
  lastAccessedAtUtc: string | null;
  isActive: boolean;
}>;

export type RawAiTaskDeliveryChannel = Readonly<{
  channelKey: string;
  displayName: string;
  configured: boolean;
  available: boolean;
  reasonCode: string | null;
}>;

async function fetchAiTaskDeliveryApi<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<T> {
  const response = await fetch(`${context.apiBaseUrl}${path}`, {
    ...init,
    headers: {
      ...context.requestHeaders,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`Delivery API failed: ${response.status}`);
  }

  return (await response.json()) as T;
}

export async function listAiTaskDeliveries(
  context: ProductionOrderRequestContext,
  taskId: string,
): Promise<readonly RawAiTaskDelivery[]> {
  const payload = await fetchAiTaskDeliveryApi<{ items: RawAiTaskDelivery[] }>(
    context,
    `/api/ai-tasks/${taskId}/deliveries?facilityId=${context.facilityId}`,
  );
  return payload.items;
}

export async function listAiTaskDeliveryChannels(
  context: ProductionOrderRequestContext,
): Promise<readonly RawAiTaskDeliveryChannel[]> {
  const payload = await fetchAiTaskDeliveryApi<{ channels: RawAiTaskDeliveryChannel[] }>(
    context,
    `/api/ai-task-delivery/channels?facilityId=${context.facilityId}`,
  );
  return payload.channels;
}

export async function listAiTaskSecureAccessGrants(
  context: ProductionOrderRequestContext,
  taskId: string,
): Promise<readonly RawAiTaskSecureAccessGrant[]> {
  const payload = await fetchAiTaskDeliveryApi<{ items: RawAiTaskSecureAccessGrant[] }>(
    context,
    `/api/ai-tasks/${taskId}/secure-access?facilityId=${context.facilityId}`,
  );
  return payload.items;
}

export async function issueAiTaskSecureAccess(
  context: ProductionOrderRequestContext,
  taskId: string,
  ttlPolicy: string,
): Promise<Readonly<{ grant: RawAiTaskSecureAccessGrant; secureLinkPath: string; accessToken: string }>> {
  return fetchAiTaskDeliveryApi(context, `/api/ai-tasks/${taskId}/secure-access?facilityId=${context.facilityId}`, {
    method: "POST",
    body: JSON.stringify({ ttlPolicy, scope: "read-only" }),
  });
}

export async function revokeAiTaskSecureAccess(
  context: ProductionOrderRequestContext,
  grantId: string,
): Promise<RawAiTaskSecureAccessGrant> {
  return fetchAiTaskDeliveryApi(context, `/api/ai-task-secure-access/${grantId}/revoke?facilityId=${context.facilityId}`, {
    method: "POST",
    body: JSON.stringify({}),
  });
}

export async function rotateAiTaskSecureAccess(
  context: ProductionOrderRequestContext,
  grantId: string,
  ttlPolicy: string,
): Promise<Readonly<{ grant: RawAiTaskSecureAccessGrant; secureLinkPath: string; accessToken: string }>> {
  return fetchAiTaskDeliveryApi(context, `/api/ai-task-secure-access/${grantId}/rotate?facilityId=${context.facilityId}`, {
    method: "POST",
    body: JSON.stringify({ ttlPolicy }),
  });
}

export async function enqueueAiTaskDelivery(
  context: ProductionOrderRequestContext,
  taskId: string,
  body: Readonly<{
    channel: string;
    recipientKind: string;
    idempotencyKey: string;
    secureAccessGrantId?: string;
    maskedRecipient?: string;
    recipientE164?: string;
    consentStatus?: string;
    secureTaskUrl?: string;
  }>,
): Promise<RawAiTaskDelivery> {
  return fetchAiTaskDeliveryApi(context, `/api/ai-tasks/${taskId}/deliveries?facilityId=${context.facilityId}`, {
    method: "POST",
    headers: { "Idempotency-Key": body.idempotencyKey },
    body: JSON.stringify(body),
  });
}

export async function listDeliveryManagement(
  context: ProductionOrderRequestContext,
  query: string,
): Promise<Readonly<{ items: RawAiTaskDelivery[]; totalCount: number }>> {
  const separator = query ? "&" : "";
  return fetchAiTaskDeliveryApi(
    context,
    `/api/ai-task-deliveries-management?facilityId=${context.facilityId}${separator}${query}`,
  );
}

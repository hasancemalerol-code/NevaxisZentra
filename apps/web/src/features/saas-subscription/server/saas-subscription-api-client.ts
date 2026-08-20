import "./saas-subscription-server-boundary";

import { fetchTrustedApi } from "@/features/saas-identity/server/authenticated-server-fetch";

import type { TenantSubscriptionCurrent } from "../domain/saas-subscription-types";

type ApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string; code?: string; httpStatus?: number }>;

async function requestJson<T>(
  path: string,
  init: RequestInit = {},
): Promise<ApiResult<T>> {
  const requestHeaders = new Headers(init.headers ?? undefined);
  if (init.body !== undefined && !requestHeaders.has("Content-Type")) {
    requestHeaders.set("Content-Type", "application/json");
  }

  const fetchResult = await fetchTrustedApi(path, {
    ...init,
    headers: requestHeaders,
  });
  if (fetchResult.status === "failure") {
    return {
      status: "failure",
      message: fetchResult.message,
      code: fetchResult.code,
    };
  }

  try {
    const response = fetchResult.response;

    if (!response.ok) {
      let message = "İstek başarısız oldu.";
      try {
        const payload = (await response.json()) as { title?: string; code?: string };
        message = payload.title ?? message;
        return {
          status: "failure",
          message,
          code: payload.code,
          httpStatus: response.status,
        };
      } catch {
        return {
          status: "failure",
          message,
          httpStatus: response.status,
        };
      }
    }

    const payload = (await response.json()) as T;
    return { status: "success", value: payload };
  } catch {
    return { status: "failure", message: "API bağlantısı kurulamadı.", code: "network-error" };
  }
}

export async function fetchCurrentSubscription(): Promise<ApiResult<TenantSubscriptionCurrent>> {
  return requestJson<TenantSubscriptionCurrent>("/api/saas-subscription/current");
}

export type SubscriptionAccessStatus = Readonly<{
  allowed: boolean;
  denialReason: "paid-application" | "module" | null;
  graceWarning: boolean;
  evaluatedAtUtc: string;
}>;

export async function fetchSubscriptionAccessStatus(
  routePath: string,
): Promise<ApiResult<SubscriptionAccessStatus>> {
  const encoded = encodeURIComponent(routePath);
  return requestJson<SubscriptionAccessStatus>(`/api/saas-subscription/access-status?routePath=${encoded}`);
}

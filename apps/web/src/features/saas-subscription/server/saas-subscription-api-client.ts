import "./saas-subscription-server-boundary";

import { cookies } from "next/headers";

import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

import type { TenantSubscriptionCurrent } from "../domain/saas-subscription-types";

type ApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string; code?: string; httpStatus?: number }>;

async function readCookieHeader(): Promise<string> {
  const cookieStore = await cookies();
  return cookieStore
    .getAll()
    .map((entry) => `${entry.name}=${entry.value}`)
    .join("; ");
}

async function requestJson<T>(
  path: string,
  init: RequestInit = {},
): Promise<ApiResult<T>> {
  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return { status: "failure", message: config.message, code: "configuration-error" };
  }

  try {
    const response = await fetch(`${config.config.apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        Cookie: await readCookieHeader(),
        ...(init.headers ?? {}),
      },
      cache: "no-store",
    });

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

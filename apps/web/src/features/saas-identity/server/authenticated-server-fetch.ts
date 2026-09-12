import "./saas-identity-server-boundary";

import { headers } from "next/headers";

import { resolveSaasIdentityRuntimeConfig } from "./saas-identity-runtime-config";

export type TrustedApiFetchResult =
  | Readonly<{ status: "success"; response: Response }>
  | Readonly<{
      status: "failure";
      code: "configuration-error" | "network-error";
      message: string;
    }>;

function normalizeBaseUrl(value: string): string {
  return value.replace(/\/$/, "");
}

function resolveTrustedApiBaseUrl():
  | Readonly<{ status: "success"; apiBaseUrl: string }>
  | Readonly<{ status: "failure"; message: string }> {
  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return { status: "failure", message: config.message };
  }

  return {
    status: "success",
    apiBaseUrl: normalizeBaseUrl(config.config.apiBaseUrl),
  };
}

export function buildTrustedApiUrl(path: string):
  | Readonly<{ status: "success"; url: string }>
  | Readonly<{ status: "failure"; message: string }> {
  const base = resolveTrustedApiBaseUrl();
  if (base.status === "failure") {
    return base;
  }

  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  const url = `${base.apiBaseUrl}${normalizedPath}`;

  try {
    const parsed = new URL(url);
    const trusted = new URL(base.apiBaseUrl);
    if (parsed.origin !== trusted.origin) {
      return {
        status: "failure",
        message: "Untrusted API base URL.",
      };
    }
  } catch {
    return {
      status: "failure",
      message: "Invalid API base URL.",
    };
  }

  return { status: "success", url };
}

export async function readIncomingRequestCookieHeader(): Promise<string> {
  const headerList = await headers();
  return headerList.get("cookie") ?? "";
}

export function hasIncomingSessionCookie(cookieHeader: string): boolean {
  if (!cookieHeader) {
    return false;
  }

  return cookieHeader
    .split(";")
    .some((part) => part.trim().startsWith("NevaxisFactOS.session="));
}

export async function fetchTrustedApi(
  path: string,
  init: RequestInit = {},
): Promise<TrustedApiFetchResult> {
  const urlResult = buildTrustedApiUrl(path);
  if (urlResult.status === "failure") {
    return {
      status: "failure",
      code: "configuration-error",
      message: urlResult.message,
    };
  }

  const cookieHeader = await readIncomingRequestCookieHeader();
  const requestHeaders = new Headers(init.headers ?? undefined);
  requestHeaders.set("Accept", requestHeaders.get("Accept") ?? "application/json");

  if (cookieHeader) {
    requestHeaders.set("Cookie", cookieHeader);
  }

  try {
    const response = await fetch(urlResult.url, {
      ...init,
      headers: requestHeaders,
      cache: "no-store",
    });

    return { status: "success", response };
  } catch {
    return {
      status: "failure",
      code: "network-error",
      message: "API bağlantısı kurulamadı.",
    };
  }
}

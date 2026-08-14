"use client";

import type {
  SaasIdentityApiFailure,
  SaasIdentityApiResult,
  SaasIdentityForgotPasswordRequest,
  SaasIdentityInvitationAcceptRequest,
  SaasIdentityMfaVerifyRequest,
  SaasIdentityResetPasswordRequest,
  SaasIdentitySignInRequest,
} from "../domain/saas-identity";
import {
  SAAS_IDENTITY_API_PREFIX,
} from "../lib/saas-identity-constants";
import {
  buildCsrfHeaders,
  readBrowserCsrfToken,
} from "../lib/saas-identity-csrf";
import {
  parseSaasIdentityFailureCode,
  parseSaasIdentitySignInResponse,
} from "../lib/saas-identity-api-contracts";

function resolveBrowserApiBaseUrl(): string {
  if (typeof window !== "undefined") {
    return window.location.origin.replace(/\/$/, "");
  }

  const configured = process.env.NEXT_PUBLIC_CREVAFACTOS_API_BASE_URL?.trim();
  if (configured) {
    return configured.replace(/\/$/, "");
  }

  return "";
}

function createFailure(
  code: SaasIdentityApiFailure["code"],
  message: string,
  retryable = false,
): SaasIdentityApiFailure {
  return {
    status: "failure",
    code,
    message,
    retryable,
  };
}

async function browserIdentityFetch<T>(
  path: string,
  options: Readonly<{
    method?: "GET" | "POST";
    body?: unknown;
    mutation?: boolean;
  }>,
  parser: (payload: unknown) => T | null,
): Promise<SaasIdentityApiResult<T>> {
  const apiBaseUrl = resolveBrowserApiBaseUrl();
  if (!apiBaseUrl) {
    return createFailure(
      "configuration-error",
      "API base URL is not configured.",
    );
  }

  const headers: Record<string, string> = {
    Accept: "application/json",
  };

  if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (options.mutation) {
    Object.assign(headers, buildCsrfHeaders(readBrowserCsrfToken()));
  }

  try {
    const response = await fetch(
      `${apiBaseUrl}${SAAS_IDENTITY_API_PREFIX}${path}`,
      {
        method: options.method ?? "GET",
        headers,
        body:
          options.body !== undefined
            ? JSON.stringify(options.body)
            : undefined,
        credentials: "include",
        cache: "no-store",
      },
    );

    if (response.status === 401) {
      return createFailure(
        "unauthenticated",
        "Authentication required.",
      );
    }

    if (response.status === 403) {
      let payload: unknown = null;
      try {
        payload = await response.json();
      } catch {
        payload = null;
      }

      const code = parseSaasIdentityFailureCode(payload);
      if (code === "csrf-denied") {
        return createFailure(
          "csrf-denied",
          "Security validation failed.",
        );
      }

      return createFailure(
        "membership-denied",
        "Authorization denied.",
      );
    }

    let payload: unknown = null;
    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      payload = await response.json();
    }

    if (!response.ok) {
      const code = parseSaasIdentityFailureCode(payload);
      return createFailure(
        mapHttpFailureCode(code, response.status),
        "Request failed.",
        response.status >= 500,
      );
    }

    const parsed = parser(payload);
    if (!parsed) {
      return createFailure(
        "source-unavailable",
        "Unexpected identity response.",
      );
    }

    return { status: "success", value: parsed };
  } catch {
    return createFailure(
      "source-unavailable",
      "Identity service is unavailable.",
      true,
    );
  }
}

function mapHttpFailureCode(
  code: string | undefined,
  status: number,
): SaasIdentityApiFailure["code"] {
  switch (code) {
    case "invalid-credentials":
      return "invalid-credentials";
    case "account-locked":
      return "account-locked";
    case "account-deactivated":
      return "account-deactivated";
    case "membership-denied":
      return "membership-denied";
    case "invalid-token":
      return "invalid-token";
    case "token-expired":
      return "token-expired";
    case "invalid-request":
      return "invalid-request";
    default:
      return status >= 500 ? "source-unavailable" : "invalid-request";
  }
}

export async function signInSaasIdentityBrowser(
  request: SaasIdentitySignInRequest,
): Promise<SaasIdentityApiResult<"authenticated" | "mfa_required">> {
  return browserIdentityFetch(
    "/sign-in",
    { method: "POST", body: request, mutation: true },
    parseSaasIdentitySignInResponse,
  );
}

export async function verifySaasIdentityMfaBrowser(
  request: SaasIdentityMfaVerifyRequest,
): Promise<SaasIdentityApiResult<"authenticated">> {
  return browserIdentityFetch(
    "/mfa/verify",
    { method: "POST", body: request, mutation: true },
    (payload) =>
      parseSaasIdentitySignInResponse(payload) === "authenticated"
        ? "authenticated"
        : null,
  );
}

export async function signOutSaasIdentityBrowser(): Promise<
  SaasIdentityApiResult<"signed-out">
> {
  return browserIdentityFetch(
    "/sign-out",
    { method: "POST", mutation: true },
    () => "signed-out" as const,
  );
}

export async function forgotSaasIdentityPasswordBrowser(
  request: SaasIdentityForgotPasswordRequest,
): Promise<SaasIdentityApiResult<"submitted">> {
  return browserIdentityFetch(
    "/password/forgot",
    { method: "POST", body: request, mutation: true },
    () => "submitted" as const,
  );
}

export async function resetSaasIdentityPasswordBrowser(
  request: SaasIdentityResetPasswordRequest,
): Promise<SaasIdentityApiResult<"reset">> {
  return browserIdentityFetch(
    "/password/reset",
    { method: "POST", body: request, mutation: true },
    () => "reset" as const,
  );
}

export async function acceptSaasIdentityInvitationBrowser(
  request: SaasIdentityInvitationAcceptRequest,
): Promise<SaasIdentityApiResult<"accepted">> {
  return browserIdentityFetch(
    "/invitations/accept",
    { method: "POST", body: request, mutation: true },
    () => "accepted" as const,
  );
}

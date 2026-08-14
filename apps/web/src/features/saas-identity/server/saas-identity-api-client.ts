import "./saas-identity-server-boundary";

import { cookies } from "next/headers";

import type {
  SaasIdentityAccountSummary,
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
  SAAS_IDENTITY_CSRF_COOKIE,
  SAAS_IDENTITY_CSRF_HEADER,
} from "../lib/saas-identity-constants";
import {
  parseSaasIdentityFailureCode,
  parseSaasIdentityMeResponse,
  parseSaasIdentitySignInResponse,
} from "../lib/saas-identity-api-contracts";
import { resolveSaasIdentityRuntimeConfig } from "./saas-identity-runtime-config";

type ServerFetchOptions = Readonly<{
  method?: "GET" | "POST";
  body?: unknown;
  mutation?: boolean;
}>;

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

async function readServerCookieHeader(): Promise<string> {
  const cookieStore = await cookies();
  return cookieStore
    .getAll()
    .map((entry) => `${entry.name}=${entry.value}`)
    .join("; ");
}

async function readServerCsrfToken(): Promise<string | null> {
  const cookieStore = await cookies();
  return cookieStore.get(SAAS_IDENTITY_CSRF_COOKIE)?.value ?? null;
}

async function serverIdentityFetch<T>(
  path: string,
  options: ServerFetchOptions,
  parser: (payload: unknown) => T | null,
): Promise<SaasIdentityApiResult<T>> {
  const configResult = resolveSaasIdentityRuntimeConfig();
  if (configResult.status === "failure") {
    return createFailure(
      "configuration-error",
      configResult.message,
    );
  }

  const headers: Record<string, string> = {
    Accept: "application/json",
    Cookie: await readServerCookieHeader(),
  };

  if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (options.mutation) {
    const csrfToken = await readServerCsrfToken();
    if (csrfToken) {
      headers[SAAS_IDENTITY_CSRF_HEADER] = csrfToken;
    }
  }

  try {
    const response = await fetch(
      `${configResult.config.apiBaseUrl}${SAAS_IDENTITY_API_PREFIX}${path}`,
      {
        method: options.method ?? "GET",
        headers,
        body:
          options.body !== undefined
            ? JSON.stringify(options.body)
            : undefined,
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

      const record =
        payload && typeof payload === "object"
          ? (payload as Record<string, unknown>)
          : null;
      const title = record?.title;
      if (title === "Multi-factor authentication is required.") {
        return createFailure(
          "mfa-required",
          "Multi-factor authentication is required.",
        );
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

export async function fetchSaasIdentityMeServer(): Promise<
  SaasIdentityApiResult<SaasIdentityAccountSummary>
> {
  return serverIdentityFetch(
    "/me",
    { method: "GET" },
    parseSaasIdentityMeResponse,
  );
}

export async function signInSaasIdentityServer(
  request: SaasIdentitySignInRequest,
): Promise<SaasIdentityApiResult<"authenticated" | "mfa_required">> {
  return serverIdentityFetch(
    "/sign-in",
    { method: "POST", body: request, mutation: true },
    parseSaasIdentitySignInResponse,
  );
}

export async function verifySaasIdentityMfaServer(
  request: SaasIdentityMfaVerifyRequest,
): Promise<SaasIdentityApiResult<"authenticated">> {
  return serverIdentityFetch(
    "/mfa/verify",
    { method: "POST", body: request, mutation: true },
    (payload) =>
      parseSaasIdentitySignInResponse(payload) === "authenticated"
        ? "authenticated"
        : null,
  );
}

export async function signOutSaasIdentityServer(): Promise<
  SaasIdentityApiResult<"signed-out">
> {
  return serverIdentityFetch(
    "/sign-out",
    { method: "POST", mutation: true },
    () => "signed-out" as const,
  );
}

export async function forgotSaasIdentityPasswordServer(
  request: SaasIdentityForgotPasswordRequest,
): Promise<SaasIdentityApiResult<"submitted">> {
  return serverIdentityFetch(
    "/password/forgot",
    { method: "POST", body: request, mutation: true },
    () => "submitted" as const,
  );
}

export async function resetSaasIdentityPasswordServer(
  request: SaasIdentityResetPasswordRequest,
): Promise<SaasIdentityApiResult<"reset">> {
  return serverIdentityFetch(
    "/password/reset",
    { method: "POST", body: request, mutation: true },
    () => "reset" as const,
  );
}

export async function acceptSaasIdentityInvitationServer(
  request: SaasIdentityInvitationAcceptRequest,
): Promise<SaasIdentityApiResult<"accepted">> {
  return serverIdentityFetch(
    "/invitations/accept",
    { method: "POST", body: request, mutation: true },
    () => "accepted" as const,
  );
}

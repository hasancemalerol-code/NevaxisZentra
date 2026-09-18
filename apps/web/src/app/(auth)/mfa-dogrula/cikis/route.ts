import { NextResponse } from "next/server";

import {
  SAAS_IDENTITY_API_PREFIX,
  SAAS_IDENTITY_CSRF_COOKIE,
  SAAS_IDENTITY_CSRF_HEADER,
  SAAS_IDENTITY_SESSION_COOKIE,
} from "@/features/saas-identity/lib/saas-identity-constants";
import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";

function redirectWithClearedCookies(
  request: Request,
  path: string,
  setCookies: readonly string[],
): NextResponse {
  const response = NextResponse.redirect(new URL(path, request.url), 303);

  for (const cookie of setCookies) {
    response.headers.append("Set-Cookie", cookie);
  }

  expireIdentityCookie(response, SAAS_IDENTITY_SESSION_COOKIE);
  expireIdentityCookie(response, SAAS_IDENTITY_CSRF_COOKIE);
  return response;
}

function expireIdentityCookie(response: NextResponse, name: string): void {
  response.cookies.set({
    name,
    value: "",
    path: "/",
    maxAge: 0,
    sameSite: "lax",
  });
}

function readIncomingCookie(request: Request, name: string): string | null {
  const header = request.headers.get("cookie");
  if (!header) {
    return null;
  }

  for (const part of header.split(";")) {
    const separator = part.indexOf("=");
    if (separator <= 0) {
      continue;
    }

    const cookieName = part.slice(0, separator).trim();
    if (cookieName === name) {
      return part.slice(separator + 1).trim();
    }
  }

  return null;
}

export async function POST(request: Request): Promise<NextResponse> {
  const session = await resolveSaasIdentitySession();
  if (session.status === "authenticated") {
    return NextResponse.redirect(new URL("/", request.url), 303);
  }

  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return NextResponse.redirect(new URL("/mfa-dogrula?cikis=hata", request.url), 303);
  }

  const cookieHeader = request.headers.get("cookie") ?? "";
  const csrfToken = readIncomingCookie(request, SAAS_IDENTITY_CSRF_COOKIE);
  const headers: Record<string, string> = {
    Accept: "application/json",
  };

  if (cookieHeader) {
    headers.Cookie = cookieHeader;
  }

  if (csrfToken) {
    headers[SAAS_IDENTITY_CSRF_HEADER] = csrfToken;
  }

  let apiResponse: Response | null = null;
  try {
    apiResponse = await fetch(
      `${config.config.apiBaseUrl}${SAAS_IDENTITY_API_PREFIX}/sign-out`,
      {
        method: "POST",
        headers,
        cache: "no-store",
      },
    );
  } catch {
    return NextResponse.redirect(new URL("/mfa-dogrula?cikis=hata", request.url), 303);
  }

  if (apiResponse.status >= 500) {
    return NextResponse.redirect(new URL("/mfa-dogrula?cikis=hata", request.url), 303);
  }

  const setCookies =
    typeof apiResponse.headers.getSetCookie === "function"
      ? apiResponse.headers.getSetCookie()
      : [];

  return redirectWithClearedCookies(request, "/giris", setCookies);
}

export function GET(request: Request): NextResponse {
  return NextResponse.redirect(new URL("/mfa-dogrula", request.url), 303);
}

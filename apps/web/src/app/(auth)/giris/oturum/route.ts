import { NextResponse } from "next/server";

import {
  SAAS_IDENTITY_API_PREFIX,
} from "@/features/saas-identity/lib/saas-identity-constants";
import {
  buildCleanSignInPath,
  isSafeAppReturnUrl,
} from "@/features/saas-identity/lib/saas-identity-return-url";
import { parseSaasIdentitySignInResponse } from "@/features/saas-identity/lib/saas-identity-api-contracts";
import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

function readFormText(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}

function deniedSignInRedirect(
  request: Request,
  returnUrl: string | null,
): NextResponse {
  const target = new URL(buildCleanSignInPath(returnUrl), request.url);
  target.searchParams.set("auth", "denied");
  return NextResponse.redirect(target, 303);
}

function safeRedirect(
  request: Request,
  path: string,
  setCookies: string[],
): NextResponse {
  const response = NextResponse.redirect(new URL(path, request.url), 303);
  for (const cookie of setCookies) {
    response.headers.append("Set-Cookie", cookie);
  }
  return response;
}

export async function POST(request: Request): Promise<NextResponse> {
  const formData = await request.formData();
  const organizationContext = readFormText(formData, "organizationContext");
  const email = readFormText(formData, "email");
  const password = typeof formData.get("password") === "string"
    ? String(formData.get("password"))
    : "";
  const returnUrlValue = readFormText(formData, "returnUrl");
  const returnUrl = isSafeAppReturnUrl(returnUrlValue) ? returnUrlValue : null;

  if (!organizationContext || !email || !password) {
    return deniedSignInRedirect(request, returnUrl);
  }

  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return deniedSignInRedirect(request, returnUrl);
  }

  let apiResponse: Response;
  try {
    apiResponse = await fetch(
      `${config.config.apiBaseUrl}${SAAS_IDENTITY_API_PREFIX}/sign-in`,
      {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          organizationContext,
          email,
          password,
          deviceLabel: "crevafactos-web",
        }),
        cache: "no-store",
      },
    );
  } catch {
    return deniedSignInRedirect(request, returnUrl);
  }

  const setCookies =
    typeof apiResponse.headers.getSetCookie === "function"
      ? apiResponse.headers.getSetCookie()
      : [];

  let payload: unknown = null;
  const contentType = apiResponse.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    try {
      payload = await apiResponse.json();
    } catch {
      payload = null;
    }
  }

  if (!apiResponse.ok) {
    return deniedSignInRedirect(request, returnUrl);
  }

  const outcome = parseSaasIdentitySignInResponse(payload);
  if (outcome === "mfa_required") {
    return safeRedirect(request, "/mfa-dogrula", setCookies);
  }

  if (outcome === "authenticated") {
    return safeRedirect(request, returnUrl ?? "/dashboard", setCookies);
  }

  return deniedSignInRedirect(request, returnUrl);
}

export function GET(request: Request): NextResponse {
  return NextResponse.redirect(new URL("/giris", request.url), 303);
}

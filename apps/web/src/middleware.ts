import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

import {
  SAAS_IDENTITY_AUTH_ROUTES,
  SAAS_IDENTITY_LEGACY_SESSION_COOKIES,
  SAAS_IDENTITY_SESSION_COOKIE,
} from "@/features/saas-identity/lib/saas-identity-constants";
import { readZentraEnv } from "@/lib/config/zentra-env";
import { deferredSurfacesEnabled, isDeferredSurfaceHref } from "@/lib/runtime/deferred-surfaces";

/** A path no route matches: the rewrite renders the root not-found page with a real 404 status. */
const DEFERRED_NOT_FOUND_REWRITE = "/__deferred-surface-not-found";

function isAuthRoute(pathname: string): boolean {
  return SAAS_IDENTITY_AUTH_ROUTES.some(
    (route) => pathname === route || pathname.startsWith(`${route}/`),
  );
}

function isPublicRoute(pathname: string): boolean {
  return (
    pathname.startsWith("/health") ||
    pathname.startsWith("/api/identity") ||
    pathname.startsWith("/_next") ||
    pathname === "/favicon.ico" ||
    pathname === "/gorev" ||
    pathname.startsWith("/gorev/")
  );
}

function isDevelopmentApiBypass(): boolean {
  return (
    readZentraEnv("PRODUCTION_EXECUTION_RUNTIME_MODE") === "development-api" &&
    Boolean(readZentraEnv("DEV_AUTH_ISSUER")) &&
    Boolean(readZentraEnv("DEV_TENANT_KEY"))
  );
}

function hasIdentitySession(request: NextRequest): boolean {
  const cookieNames = [
    SAAS_IDENTITY_SESSION_COOKIE,
    ...SAAS_IDENTITY_LEGACY_SESSION_COOKIES,
  ];
  return cookieNames.some((name) => Boolean(request.cookies.get(name)?.value));
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Deferred demo surfaces answer a real 404 before any streaming starts (a layout notFound() can only produce 200),
  // identically for signed-in and signed-out visitors so their existence is not disclosed.
  if (isDeferredSurfaceHref(pathname) && !deferredSurfacesEnabled(process.env)) {
    const url = request.nextUrl.clone();
    url.pathname = DEFERRED_NOT_FOUND_REWRITE;
    url.search = "";
    const response = NextResponse.rewrite(url, { status: 404 });
    response.headers.set("X-Robots-Tag", "noindex, nofollow");
    response.headers.set("Cache-Control", "no-store");
    return response;
  }

  if (isPublicRoute(pathname)) {
    return NextResponse.next();
  }

  const hasSession = hasIdentitySession(request);

  if (isAuthRoute(pathname)) {
    return NextResponse.next();
  }

  if (!hasSession) {
    if (isDevelopmentApiBypass()) {
      const requestHeaders = new Headers(request.headers);
      requestHeaders.set("x-pathname", pathname);
      return NextResponse.next({
        request: {
          headers: requestHeaders,
        },
      });
    }

    const signInUrl = new URL("/giris", request.url);
    if (pathname !== "/") {
      signInUrl.searchParams.set("returnUrl", pathname);
    }
    return NextResponse.redirect(signInUrl);
  }

  const requestHeaders = new Headers(request.headers);
  requestHeaders.set("x-pathname", pathname);

  return NextResponse.next({
    request: {
      headers: requestHeaders,
    },
  });
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};

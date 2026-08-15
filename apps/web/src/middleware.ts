import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

import {
  SAAS_IDENTITY_AUTH_ROUTES,
  SAAS_IDENTITY_SESSION_COOKIE,
} from "@/features/saas-identity/lib/saas-identity-constants";

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
    pathname === "/favicon.ico"
  );
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (isPublicRoute(pathname)) {
    return NextResponse.next();
  }

  const hasSession = Boolean(
    request.cookies.get(SAAS_IDENTITY_SESSION_COOKIE)?.value,
  );

  if (isAuthRoute(pathname)) {
    return NextResponse.next();
  }

  if (!hasSession) {
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

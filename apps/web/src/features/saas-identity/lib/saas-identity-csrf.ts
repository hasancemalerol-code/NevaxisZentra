import {
  SAAS_IDENTITY_CSRF_COOKIE,
  SAAS_IDENTITY_CSRF_HEADER,
} from "./saas-identity-constants";

export function readBrowserCsrfToken(): string | null {
  if (typeof document === "undefined") {
    return null;
  }

  const escapedName = SAAS_IDENTITY_CSRF_COOKIE.replace(
    /[.*+?^${}()|[\]\\]/g,
    "\\$&",
  );
  const match = document.cookie.match(
    new RegExp(`(?:^|;\\s*)${escapedName}=([^;]*)`),
  );

  if (!match?.[1]) {
    return null;
  }

  try {
    return decodeURIComponent(match[1]);
  } catch {
    return match[1];
  }
}

export function buildCsrfHeaders(
  csrfToken: string | null | undefined,
): Record<string, string> {
  if (!csrfToken) {
    return {};
  }

  return {
    [SAAS_IDENTITY_CSRF_HEADER]: csrfToken,
  };
}

export function stripSensitiveQueryParam(
  paramName: string,
): void {
  if (typeof window === "undefined") {
    return;
  }

  const url = new URL(window.location.href);
  if (!url.searchParams.has(paramName)) {
    return;
  }

  url.searchParams.delete(paramName);
  window.history.replaceState({}, "", url.toString());
}

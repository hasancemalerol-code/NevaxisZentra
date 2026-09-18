export const SAAS_IDENTITY_SESSION_COOKIE = "NevaxisZentra.session";

export const SAAS_IDENTITY_CSRF_COOKIE = "NevaxisZentra.csrf";

export const SAAS_IDENTITY_CSRF_HEADER = "X-NevaxisZentra-Csrf";

/** Legacy session cookies accepted during upgrade (read-only compatibility). */
export const SAAS_IDENTITY_LEGACY_SESSION_COOKIES = [
  "NevaxisFactOS.session",
  "crevafactos.session",
] as const;

export const SAAS_IDENTITY_LEGACY_CSRF_COOKIES = [
  "NevaxisFactOS.csrf",
  "crevafactos.csrf",
] as const;

export const SAAS_IDENTITY_API_PREFIX = "/api/identity";

export const SAAS_IDENTITY_AUTH_ROUTES = [
  "/giris",
  "/sifremi-unuttum",
  "/sifre-sifirla",
  "/davet/kabul",
  "/mfa-dogrula",
] as const;

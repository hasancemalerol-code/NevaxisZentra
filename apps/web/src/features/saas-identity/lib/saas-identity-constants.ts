export const SAAS_IDENTITY_SESSION_COOKIE = "crevafactos.session";

export const SAAS_IDENTITY_CSRF_COOKIE = "crevafactos.csrf";

export const SAAS_IDENTITY_CSRF_HEADER = "X-CrevaFactOS-Csrf";

export const SAAS_IDENTITY_API_PREFIX = "/api/identity";

export const SAAS_IDENTITY_AUTH_ROUTES = [
  "/giris",
  "/sifremi-unuttum",
  "/sifre-sifirla",
  "/davet/kabul",
  "/mfa-dogrula",
] as const;

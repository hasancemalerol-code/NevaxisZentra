const SENSITIVE_SIGN_IN_QUERY_KEYS = [
  "password",
  "email",
  "organizationContext",
] as const;

export function isSafeAppReturnUrl(
  value: string | null | undefined,
): value is string {
  if (!value) {
    return false;
  }

  return value.startsWith("/") && !value.startsWith("//") && !value.includes("\\");
}

export function hasSensitiveSignInQueryKeys(
  keys: readonly string[],
): boolean {
  return SENSITIVE_SIGN_IN_QUERY_KEYS.some((key) => keys.includes(key));
}

export function buildCleanSignInPath(
  returnUrl: string | null | undefined,
): string {
  if (!isSafeAppReturnUrl(returnUrl)) {
    return "/giris";
  }

  return `/giris?returnUrl=${encodeURIComponent(returnUrl)}`;
}

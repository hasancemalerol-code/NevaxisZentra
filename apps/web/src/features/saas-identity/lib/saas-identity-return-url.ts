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

  // URL parsers strip tab/CR/LF, so "/\t/host" would resolve to "//host" and
  // redirect off-site. Reject every control character.
  if (/[\u0000-\u001f\u007f]/.test(value)) {
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

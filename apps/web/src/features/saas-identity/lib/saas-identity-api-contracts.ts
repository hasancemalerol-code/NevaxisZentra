import type { SaasIdentityAccountSummary } from "../domain/saas-identity";

function readString(
  record: Record<string, unknown>,
  key: string,
): string | null {
  const value = record[key];
  return typeof value === "string" && value.trim().length > 0
    ? value.trim()
    : null;
}

function readBoolean(
  record: Record<string, unknown>,
  key: string,
): boolean {
  return record[key] === true;
}

function maskEmailAddress(email: string): string {
  const atIndex = email.indexOf("@");
  if (atIndex <= 0) {
    return email;
  }

  const localPart = email.slice(0, atIndex);
  const domain = email.slice(atIndex + 1);
  const visible = localPart.slice(0, Math.min(1, localPart.length));
  return `${visible}***@${domain}`;
}

export function parseSaasIdentityMeResponse(
  payload: unknown,
): SaasIdentityAccountSummary | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }

  const record = payload as Record<string, unknown>;
  const accountReference = readString(record, "accountReference");
  const displayName = readString(record, "displayName");
  const emailMasked =
    readString(record, "emailMasked") ??
    (readString(record, "email")
      ? maskEmailAddress(readString(record, "email")!)
      : null);
  const organizationLabel =
    readString(record, "organizationLabel") ??
    readString(record, "tenantDisplayName");
  const sessionReference = readString(record, "sessionReference");

  if (
    !accountReference ||
    !displayName ||
    !emailMasked ||
    !organizationLabel ||
    !sessionReference
  ) {
    return null;
  }

  return {
    accountReference,
    displayName,
    emailMasked,
    organizationLabel,
    sessionReference,
    mfaPending: readBoolean(record, "mfaPending"),
  };
}

export function parseSaasIdentitySignInResponse(
  payload: unknown,
): "authenticated" | "mfa_required" | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }

  const record = payload as Record<string, unknown>;
  const outcome = record.outcome;
  if (outcome === "authenticated" || outcome === "mfa_required") {
    return outcome;
  }

  if (record.requiresMfa === true) {
    return "mfa_required";
  }

  if (record.requiresMfa === false || readString(record, "sessionReference")) {
    return "authenticated";
  }

  return null;
}

export function parseSaasIdentityFailureCode(
  payload: unknown,
): string | undefined {
  if (!payload || typeof payload !== "object") {
    return undefined;
  }

  const code = (payload as Record<string, unknown>).code;
  if (typeof code === "string") {
    return code;
  }

  const extensions = (payload as Record<string, unknown>).extensions;
  if (extensions && typeof extensions === "object") {
    const extensionCode = (extensions as Record<string, unknown>).code;
    if (typeof extensionCode === "string") {
      return extensionCode;
    }
  }

  return undefined;
}

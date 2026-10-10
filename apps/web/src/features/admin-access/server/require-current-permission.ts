import { forbidden, notFound } from "next/navigation";

import {
  fetchCurrentEffectiveAccessResult,
  hasGrantedPermission,
  type AdminAccessEffectiveAccess,
  type AdminAccessEffectiveAccessResult,
} from "./admin-access-effective-access";

export const AUTHORIZATION_DENIED_MESSAGE = "Authorization denied.";
export const AUTHENTICATION_REQUIRED_MESSAGE = "Authentication required.";
export const AUTHORIZATION_UNAVAILABLE_MESSAGE =
  "Authorization could not be verified: the access service is unavailable.";

export type PermissionCheckResult =
  | Readonly<{ status: "granted" }>
  | Readonly<{
      status: "failure";
      /** "authorization-denied" is the only code that means the caller lacks the permission. */
      code: "authorization-denied" | "unauthenticated" | "authorization-unavailable";
      message: string;
    }>;

function evaluate(
  result: AdminAccessEffectiveAccessResult,
  permissionKeys: readonly string[],
): PermissionCheckResult {
  if (result.status === "unauthenticated") {
    return { status: "failure", code: "unauthenticated", message: AUTHENTICATION_REQUIRED_MESSAGE };
  }

  if (result.status === "unavailable") {
    return { status: "failure", code: "authorization-unavailable", message: AUTHORIZATION_UNAVAILABLE_MESSAGE };
  }

  if (!permissionKeys.some((key) => hasGrantedPermission(result.access, key))) {
    return { status: "failure", code: "authorization-denied", message: AUTHORIZATION_DENIED_MESSAGE };
  }

  return { status: "granted" };
}

export async function forbidUnlessGranted(
  permissionKey: string,
): Promise<void> {
  const check = evaluate(await fetchCurrentEffectiveAccessResult(), [permissionKey]);
  if (check.status === "granted") {
    return;
  }

  if (check.code === "authorization-unavailable") {
    // A runtime failure is not a 403: surface it as a server error instead of claiming the caller lacks access.
    throw new Error(check.message);
  }

  forbidden();
}

export async function denyUnlessGranted(
  permissionKey: string,
): Promise<PermissionCheckResult> {
  return evaluate(await fetchCurrentEffectiveAccessResult(), [permissionKey]);
}

export async function denyUnlessAnyGranted(
  permissionKeys: readonly string[],
): Promise<PermissionCheckResult> {
  return evaluate(await fetchCurrentEffectiveAccessResult(), permissionKeys);
}

export type AccessGateResult =
  | Readonly<{ status: "granted"; access: AdminAccessEffectiveAccess }>
  | Extract<PermissionCheckResult, { status: "failure" }>;

/**
 * Gate for a loader that needs the effective-access object afterwards (to derive capability flags). Grants when ANY of
 * the keys is held; otherwise reports why: a real denial, a missing session, or an unavailable access service.
 */
export async function gateAccess(permissionKeys: readonly string[]): Promise<AccessGateResult> {
  const result = await fetchCurrentEffectiveAccessResult();
  const check = evaluate(result, permissionKeys);
  if (check.status === "failure") {
    return check;
  }

  return { status: "granted", access: (result as Extract<AdminAccessEffectiveAccessResult, { status: "ok" }>).access };
}

/** The customer-facing message for a failed gate: the caller's denial wording only for a real denial. */
export function accessFailureMessage(
  failure: Extract<PermissionCheckResult, { status: "failure" }>,
  deniedMessage: string,
): string {
  return failure.code === "authorization-denied" ? deniedMessage : failure.message;
}

/** For pages that answer 404 (not 403) to callers without the permission; runtime failures still surface as errors. */
export async function notFoundUnlessGranted(permissionKey: string): Promise<void> {
  const check = evaluate(await fetchCurrentEffectiveAccessResult(), [permissionKey]);
  if (check.status === "granted") {
    return;
  }

  if (check.code === "authorization-unavailable") {
    throw new Error(check.message);
  }

  notFound();
}

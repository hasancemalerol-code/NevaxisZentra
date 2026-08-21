import "./saas-identity-server-boundary";

import { cookies } from "next/headers";

import type { SaasIdentitySessionState } from "../domain/saas-identity";
import { SAAS_IDENTITY_SESSION_COOKIE } from "../lib/saas-identity-constants";
import { fetchSaasIdentityMeServer } from "./saas-identity-api-client";

export async function resolveSaasIdentitySession(): Promise<SaasIdentitySessionState> {
  const cookieStore = await cookies();
  const sessionCookie = cookieStore.get(SAAS_IDENTITY_SESSION_COOKIE);

  if (!sessionCookie?.value) {
    return { status: "unauthenticated" };
  }

  const meResult = await fetchSaasIdentityMeServer();
  if (meResult.status === "failure") {
    if (meResult.code === "mfa-required") {
      return { status: "mfa_pending" };
    }

    if (meResult.code === "unauthenticated") {
      return { status: "session_expired" };
    }

    return { status: "unauthenticated" };
  }

  if (meResult.value.mfaPending) {
    return {
      status: "mfa_pending",
      displayName: meResult.value.displayName,
    };
  }

  return {
    status: "authenticated",
    account: meResult.value,
  };
}

export async function readSaasIdentityAccountDisplayName(): Promise<
  string | null
> {
  const session = await resolveSaasIdentitySession();
  if (session.status !== "authenticated") {
    return null;
  }

  return session.account.displayName;
}

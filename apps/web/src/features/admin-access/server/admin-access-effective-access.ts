import "./admin-access-server-boundary";

import { cookies } from "next/headers";

import { SAAS_IDENTITY_SESSION_COOKIE } from "@/features/saas-identity/lib/saas-identity-constants";
import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

export type AdminAccessNavigationVisibility = Readonly<{
  machines: boolean;
  materials: boolean;
  maintenance: boolean;
}>;

export type AdminAccessEffectiveAccess = Readonly<{
  memberReference: string | null;
  grantedPermissionKeys: readonly string[];
  navigation: AdminAccessNavigationVisibility;
}>;

async function readCookieHeader(): Promise<string> {
  const cookieStore = await cookies();
  return cookieStore
    .getAll()
    .map((entry) => `${entry.name}=${entry.value}`)
    .join("; ");
}

function hasSessionCookie(cookieHeader: string): boolean {
  return cookieHeader
    .split(";")
    .some((part) => part.trim().startsWith(`${SAAS_IDENTITY_SESSION_COOKIE}=`));
}

function readDevelopmentIdentityHeaders(): Readonly<Record<string, string>> | null {
  if (process.env.NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE !== "development-api") {
    return null;
  }

  const issuer = process.env.NEVAXIS_FACTOS_DEV_AUTH_ISSUER?.trim();
  const subject = process.env.NEVAXIS_FACTOS_DEV_AUTH_SUBJECT?.trim();
  const tenantKey = process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY?.trim();
  if (!issuer || !subject || !tenantKey) {
    return null;
  }

  return {
    "X-crevafactos-dev-Issuer": issuer,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisZentra-Tenant": tenantKey,
  };
}

function buildEffectiveAccessHeaders(cookieHeader: string): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
  };

  if (hasSessionCookie(cookieHeader)) {
    headers.Cookie = cookieHeader;
    return headers;
  }

  const developmentHeaders = readDevelopmentIdentityHeaders();
  if (developmentHeaders) {
    return { ...headers, ...developmentHeaders };
  }

  if (cookieHeader) {
    headers.Cookie = cookieHeader;
  }

  return headers;
}

export async function fetchCurrentEffectiveAccess(): Promise<AdminAccessEffectiveAccess | null> {
  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return null;
  }

  try {
    const response = await fetch(`${config.config.apiBaseUrl}/api/admin-access/me/effective-access`, {
      method: "GET",
      headers: buildEffectiveAccessHeaders(await readCookieHeader()),
      cache: "no-store",
    });

    if (!response.ok) {
      return null;
    }

    const payload = (await response.json()) as {
      memberReference?: string;
      grantedPermissionKeys?: string[];
      navigation?: {
        machines?: boolean;
        materials?: boolean;
        maintenance?: boolean;
      };
    };

    return {
      memberReference: payload.memberReference ?? null,
      grantedPermissionKeys: payload.grantedPermissionKeys ?? [],
      navigation: {
        machines: payload.navigation?.machines === true,
        materials: payload.navigation?.materials === true,
        maintenance: payload.navigation?.maintenance === true,
      },
    };
  } catch {
    return null;
  }
}

export function hasGrantedPermission(
  access: AdminAccessEffectiveAccess | null,
  permissionKey: string,
): boolean {
  return access?.grantedPermissionKeys.includes(permissionKey) === true;
}

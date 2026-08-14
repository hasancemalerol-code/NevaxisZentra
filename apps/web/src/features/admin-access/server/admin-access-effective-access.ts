import "./admin-access-server-boundary";

import { cookies } from "next/headers";

import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

export type AdminAccessNavigationVisibility = Readonly<{
  machines: boolean;
  materials: boolean;
  maintenance: boolean;
}>;

export type AdminAccessEffectiveAccess = Readonly<{
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

export async function fetchCurrentEffectiveAccess(): Promise<AdminAccessEffectiveAccess | null> {
  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return null;
  }

  try {
    const response = await fetch(`${config.config.apiBaseUrl}/api/admin-access/me/effective-access`, {
      method: "GET",
      headers: {
        Accept: "application/json",
        Cookie: await readCookieHeader(),
      },
      cache: "no-store",
    });

    if (!response.ok) {
      return null;
    }

    const payload = (await response.json()) as {
      grantedPermissionKeys?: string[];
      navigation?: {
        machines?: boolean;
        materials?: boolean;
        maintenance?: boolean;
      };
    };

    return {
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

import "./saas-admin-server-boundary";

import { cookies } from "next/headers";

import { resolveSaasIdentityRuntimeConfig } from "@/features/saas-identity/server/saas-identity-runtime-config";

import type {
  SaasAdminAuditEvent,
  SaasAdminCommercialCatalog,
  SaasAdminEffectiveAccess,
  SaasAdminFacility,
  SaasAdminInvitation,
  SaasAdminMember,
  SaasAdminPermission,
  SaasAdminRole,
  SaasAdminSession,
  SaasAdminWorkspaceSummary,
} from "../domain/saas-admin-types";

type ApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string; code?: string; httpStatus?: number }>;

async function readCookieHeader(): Promise<string> {
  const cookieStore = await cookies();
  return cookieStore
    .getAll()
    .map((entry) => `${entry.name}=${entry.value}`)
    .join("; ");
}

async function requestJson<T>(
  path: string,
  init: RequestInit = {},
): Promise<ApiResult<T>> {
  const config = resolveSaasIdentityRuntimeConfig();
  if (config.status === "failure") {
    return { status: "failure", message: config.message, code: "configuration-error" };
  }

  try {
    const response = await fetch(`${config.config.apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
        Cookie: await readCookieHeader(),
        ...(init.headers ?? {}),
      },
      cache: "no-store",
    });

    if (!response.ok) {
      let message = "İstek başarısız oldu.";
      try {
        const payload = (await response.json()) as { title?: string; code?: string };
        message = payload.title ?? message;
        return {
          status: "failure",
          message,
          code: payload.code,
          httpStatus: response.status,
        };
      } catch {
        return {
          status: "failure",
          message,
          httpStatus: response.status,
        };
      }
    }

    const payload = (await response.json()) as T;
    return { status: "success", value: payload };
  } catch {
    return { status: "failure", message: "API bağlantısı kurulamadı.", code: "network-error" };
  }
}

export async function fetchWorkspaceSummary(): Promise<ApiResult<SaasAdminWorkspaceSummary>> {
  const result = await requestJson<SaasAdminWorkspaceSummary>("/api/admin-access/workspace-summary");
  return result;
}

export async function fetchMembers(): Promise<ApiResult<readonly SaasAdminMember[]>> {
  const result = await requestJson<{ members?: SaasAdminMember[] }>("/api/admin-access/members");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: result.value.members ?? [] };
}

export async function fetchInvitations(): Promise<ApiResult<readonly SaasAdminInvitation[]>> {
  const result = await requestJson<{ invitations?: SaasAdminInvitation[] }>("/api/admin-access/invitations");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: result.value.invitations ?? [] };
}

export async function fetchRoles(): Promise<ApiResult<readonly SaasAdminRole[]>> {
  const result = await requestJson<{ roles?: SaasAdminRole[] }>("/api/admin-access/roles");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: result.value.roles ?? [] };
}

export async function fetchPermissions(): Promise<ApiResult<readonly SaasAdminPermission[]>> {
  const result = await requestJson<{ permissions?: SaasAdminPermission[] }>("/api/admin-access/permissions");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: result.value.permissions ?? [] };
}

export async function fetchFacilities(): Promise<ApiResult<readonly SaasAdminFacility[]>> {
  const result = await requestJson<{ facilities?: SaasAdminFacility[] }>("/api/admin-access/facilities");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: result.value.facilities ?? [] };
}

export async function fetchSessions(): Promise<ApiResult<readonly SaasAdminSession[]>> {
  const result = await requestJson<{ sessions?: SaasAdminSession[] }>("/api/admin-access/sessions");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: result.value.sessions ?? [] };
}

export async function fetchCommercialCatalog(): Promise<ApiResult<SaasAdminCommercialCatalog>> {
  const result = await requestJson<{ plans?: SaasAdminCommercialCatalog["plans"] }>("/api/saas-commercial/catalog");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: { plans: result.value.plans ?? [] } };
}

export async function fetchAuditEvents(): Promise<ApiResult<readonly SaasAdminAuditEvent[]>> {
  const result = await requestJson<{ events?: SaasAdminAuditEvent[] }>("/api/admin-access/audit-events?limit=50");
  if (result.status === "failure") {
    return result;
  }
  return { status: "success", value: result.value.events ?? [] };
}

export async function fetchMemberEffectiveAccess(
  memberReference: string,
): Promise<ApiResult<SaasAdminEffectiveAccess>> {
  return requestJson<SaasAdminEffectiveAccess>(
    `/api/admin-access/members/${encodeURIComponent(memberReference)}/effective-access`,
  );
}

export async function mutateAdminAccess(
  path: string,
  body: Record<string, unknown>,
  method: "POST" | "PUT" | "PATCH" = "POST",
): Promise<ApiResult<Record<string, unknown>>> {
  return requestJson<Record<string, unknown>>(path, {
    method,
    body: JSON.stringify(body),
  });
}

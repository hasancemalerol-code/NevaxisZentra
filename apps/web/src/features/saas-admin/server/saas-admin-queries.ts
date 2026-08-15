import "./saas-admin-server-boundary";

import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";

import type { SaasAdminCapabilities } from "../domain/saas-admin-types";
import {
  fetchAuditEvents,
  fetchCommercialCatalog,
  fetchFacilities,
  fetchInvitations,
  fetchMembers,
  fetchPermissions,
  fetchRoles,
  fetchSessions,
  fetchWorkspaceSummary,
} from "./saas-admin-api-client";

export async function resolveSaasAdminCapabilities(): Promise<SaasAdminCapabilities> {
  const access = await fetchCurrentEffectiveAccess();
  return {
    canRead: hasGrantedPermission(access, "admin-access.read"),
    canManage: hasGrantedPermission(access, "admin-access.manage"),
  };
}

export async function readSaasAdminWorkspace() {
  const capabilities = await resolveSaasAdminCapabilities();
  if (!capabilities.canRead) {
    return {
      status: "failure" as const,
      code: "permission-denied" as const,
      message: "Bu yönetim alanına erişim yetkiniz yok.",
    };
  }

  const [
    summaryResult,
    membersResult,
    invitationsResult,
    rolesResult,
    permissionsResult,
    facilitiesResult,
    sessionsResult,
    auditResult,
    catalogResult,
  ] = await Promise.all([
    fetchWorkspaceSummary(),
    fetchMembers(),
    fetchInvitations(),
    fetchRoles(),
    fetchPermissions(),
    fetchFacilities(),
    fetchSessions(),
    fetchAuditEvents(),
    fetchCommercialCatalog(),
  ]);

  if (summaryResult.status === "failure") {
    return {
      status: "failure" as const,
      code: summaryResult.code ?? "load-failed",
      message: summaryResult.message,
    };
  }

  return {
    status: "success" as const,
    capabilities,
    summary: summaryResult.value,
    members: membersResult.status === "success" ? membersResult.value : [],
    invitations: invitationsResult.status === "success" ? invitationsResult.value : [],
    roles: rolesResult.status === "success" ? rolesResult.value : [],
    permissions: permissionsResult.status === "success" ? permissionsResult.value : [],
    facilities: facilitiesResult.status === "success" ? facilitiesResult.value : [],
    sessions: sessionsResult.status === "success" ? sessionsResult.value : [],
    auditEvents: auditResult.status === "success" ? auditResult.value : [],
    commercialCatalog: catalogResult.status === "success" ? catalogResult.value : null,
    partialErrors: [
      membersResult.status === "failure" ? membersResult.message : null,
      invitationsResult.status === "failure" ? invitationsResult.message : null,
      rolesResult.status === "failure" ? rolesResult.message : null,
      permissionsResult.status === "failure" ? permissionsResult.message : null,
      facilitiesResult.status === "failure" ? facilitiesResult.message : null,
      sessionsResult.status === "failure" ? sessionsResult.message : null,
      auditResult.status === "failure" ? auditResult.message : null,
      catalogResult.status === "failure" ? catalogResult.message : null,
    ].filter((item): item is string => Boolean(item)),
  };
}

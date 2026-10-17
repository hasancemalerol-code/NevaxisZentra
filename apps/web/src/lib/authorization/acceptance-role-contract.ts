import contractJson from "../../../../../packages/NevaxisZentra.Persistence/Authorization/acceptance-role-contract.json";

export type AcceptanceRoleRoute = Readonly<{
  id: string;
  label: string;
  path: string | null;
  permissionKey: string | null;
  queryPermissionKey?: string;
  simulatePermissionKey?: string;
  forbiddenPermissionKey?: string;
  preparePermissionKeys?: readonly string[];
  nav: "primary" | "contextual" | "apps" | "shell" | "direct" | "deferred";
  acceptance: "required" | "deferred";
  api: string | null;
}>;

export type AcceptanceRoleContract = Readonly<{
  catalogVersion: string;
  roleKey: string;
  subject: string;
  restrictedRole: Readonly<{ roleKey: string; subject: string }>;
  requiredPermissionKeys: readonly string[];
  forbiddenPermissionKeys: readonly string[];
  subscriptionExemptPaths: readonly string[];
  routes: readonly AcceptanceRoleRoute[];
}>;

export const acceptanceRoleContract = contractJson as AcceptanceRoleContract;

const exemptPaths = new Set(acceptanceRoleContract.subscriptionExemptPaths);

export function acceptanceRoleRoute(routeId: string): AcceptanceRoleRoute {
  const route = acceptanceRoleContract.routes.find((item) => item.id === routeId);
  if (!route) {
    throw new Error(`Acceptance role contract is missing route '${routeId}'.`);
  }

  return route;
}

export function requiredRoutePermission(routeId: string): string {
  const permissionKey = acceptanceRoleRoute(routeId).permissionKey;
  if (!permissionKey) {
    throw new Error(`Acceptance role contract route '${routeId}' has no permission key.`);
  }

  return permissionKey;
}

export function requiredRouteQueryPermission(routeId: string): string {
  const permissionKey = acceptanceRoleRoute(routeId).queryPermissionKey;
  if (!permissionKey) {
    throw new Error(`Acceptance role contract route '${routeId}' has no query permission key.`);
  }

  return permissionKey;
}

export function isSubscriptionExemptCustomerWorkflowPath(routePath: string): boolean {
  const normalized = routePath.trim().replace(/\/+$/, "") || "/";
  return exemptPaths.has(normalized);
}

export function routeAccessForPermissions(
  routeId: string,
  grantedPermissionKeys: readonly string[],
): "allow" | "deny" {
  const route = acceptanceRoleRoute(routeId);
  if (!route.permissionKey) {
    return "allow";
  }

  return grantedPermissionKeys.includes(route.permissionKey) ? "allow" : "deny";
}

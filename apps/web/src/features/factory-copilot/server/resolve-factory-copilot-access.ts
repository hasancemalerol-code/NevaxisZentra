import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";

export async function resolveFactoryCopilotAccess(): Promise<boolean> {
  const access = await fetchCurrentEffectiveAccess();
  return hasGrantedPermission(access, requiredRoutePermission("factory-copilot"));
}

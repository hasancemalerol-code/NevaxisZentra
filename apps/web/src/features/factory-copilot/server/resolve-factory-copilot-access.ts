import { fetchEffectiveAccessCapabilities, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";

export async function resolveFactoryCopilotAccess(): Promise<boolean> {
  const access = await fetchEffectiveAccessCapabilities();
  return hasGrantedPermission(access, requiredRoutePermission("factory-copilot"));
}

import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";

export async function resolveFactoryCopilotAccess(): Promise<boolean> {
  const access = await fetchCurrentEffectiveAccess();
  return hasGrantedPermission(access, "factory-copilot.read");
}

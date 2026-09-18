import "server-only";

import { fetchCurrentEffectiveAccess } from "@/features/admin-access/server/admin-access-effective-access";
import { resolveRoleLandingPath } from "@/lib/navigation/role-landing";

export async function resolvePostAuthLandingPath(): Promise<string> {
  const access = await fetchCurrentEffectiveAccess();
  return resolveRoleLandingPath(access);
}

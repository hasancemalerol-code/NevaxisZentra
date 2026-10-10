import "server-only";

import { fetchEffectiveAccessCapabilities } from "@/features/admin-access/server/admin-access-effective-access";
import { resolveRoleLandingPath } from "@/lib/navigation/role-landing";

export async function resolvePostAuthLandingPath(): Promise<string> {
  const access = await fetchEffectiveAccessCapabilities();
  return resolveRoleLandingPath(access);
}

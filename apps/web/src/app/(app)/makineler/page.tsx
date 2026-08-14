import { forbidden } from "next/navigation";

import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { readEdgeWorkspace } from "@/features/edge/server/edge-reader";
import { EdgeWorkspaceView } from "@/features/edge/components/edge-workspace-view";

export default async function MakinelerPage() {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, "edge.read")) {
    forbidden();
  }

  const data = await readEdgeWorkspace();
  return <EdgeWorkspaceView {...data} />;
}

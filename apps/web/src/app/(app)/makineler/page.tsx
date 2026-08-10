import { readEdgeWorkspace } from "@/features/edge/server/edge-reader";
import { EdgeWorkspaceView } from "@/features/edge/components/edge-workspace-view";

export default async function MakinelerPage() {
  const data = await readEdgeWorkspace();
  return <EdgeWorkspaceView {...data} />;
}

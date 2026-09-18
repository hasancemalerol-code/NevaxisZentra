import { EmptyState } from "@/components/ui/states";

import { readLivePlanWorkspace } from "../server/live-plan-workspace-queries";
import { LivePlanWorkspaceShell } from "./live-plan-workspace-shell";

type LivePlanWorkspacePageContentProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export async function LivePlanWorkspacePageContent({ searchParams }: LivePlanWorkspacePageContentProps) {
  const params = await searchParams;
  const result = await readLivePlanWorkspace(params);

  if (result.status === "failure") {
    return (
      <main className="fact-workspace">
        <EmptyState
          data-testid="live-plan-workspace-error"
          description={result.message}
          title="Canlı plan çalışma alanı yüklenemedi"
        />
      </main>
    );
  }

  return <LivePlanWorkspaceShell workspace={result.workspace} />;
}

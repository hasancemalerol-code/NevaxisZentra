

import { readLivePlanWorkspace } from "../server/live-plan-workspace-queries";
import { LivePlanWorkspaceShell } from "./live-plan-workspace-shell";
import { FailureStatePage } from "@/components/ui/failure-state";

type LivePlanWorkspacePageContentProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export async function LivePlanWorkspacePageContent({ searchParams }: LivePlanWorkspacePageContentProps) {
  const params = await searchParams;
  const result = await readLivePlanWorkspace(params);

  if (result.status === "failure") {
    return (
      <div className="fact-workspace">
        <div data-testid="live-plan-workspace-error"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Canlı plan çalışma alanı yüklenemedi" inline /></div>
      </div>
    );
  }

  return <LivePlanWorkspaceShell workspace={result.workspace} />;
}

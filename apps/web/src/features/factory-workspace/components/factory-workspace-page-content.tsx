
import { readFactoryWorkspace } from "../server/factory-workspace-queries";
import { FactoryWorkspaceShell } from "./factory-workspace-shell";
import { FailureStatePage } from "@/components/ui/failure-state";

type FactoryWorkspacePageContentProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export async function FactoryWorkspacePageContent({ searchParams }: FactoryWorkspacePageContentProps) {
  const params = await searchParams;
  const result = await readFactoryWorkspace(params);

  if (result.status === "failure") {
    return (
      <div className="fact-workspace">
        <div data-testid="factory-workspace-error">
          <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Fabrika çalışma alanı şu anda alınamıyor" inline />
        </div>
      </div>
    );
  }

  return <FactoryWorkspaceShell workspace={result.workspace} />;
}

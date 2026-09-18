import { EmptyState } from "@/components/ui/states";
import { readFactoryWorkspace } from "../server/factory-workspace-queries";
import { FactoryWorkspaceShell } from "./factory-workspace-shell";

type FactoryWorkspacePageContentProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export async function FactoryWorkspacePageContent({ searchParams }: FactoryWorkspacePageContentProps) {
  const params = await searchParams;
  const result = await readFactoryWorkspace(params);

  if (result.status === "failure") {
    return (
      <main className="fact-workspace">
        <EmptyState
          data-testid="factory-workspace-error"
          description={result.message}
          title="Fabrika çalışma alanı yüklenemedi"
        />
      </main>
    );
  }

  return <FactoryWorkspaceShell workspace={result.workspace} />;
}

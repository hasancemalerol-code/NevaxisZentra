import { ShieldAlert } from "lucide-react";

import { WorksWorkspaceView } from "../components/works-workspace-view";
import { readWorksWorkspacePage } from "../server/works-queries";
import "../works-workspace.css";

type WorksPageContentProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

function readParam(value: string | string[] | undefined): string | undefined {
  return typeof value === "string" ? value : undefined;
}

export async function WorksPageContent({ searchParams }: WorksPageContentProps) {
  const params = await searchParams;
  const result = await readWorksWorkspacePage({
    initialQueue: readParam(params?.kuyruk),
    selectedCaseId: readParam(params?.is),
    search: readParam(params?.ara),
  });

  if (result.status === "failure") {
    const isUnauthorized = result.code === "permission-denied";

    return (
      <main className="works-page" data-testid="works-page-error">
        <div
          role="alert"
          className={isUnauthorized ? "works-error-banner" : "works-partial-error"}
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h1>İşler</h1>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="fact-workspace works-page" data-testid="works-page">
      <WorksWorkspaceView page={result.page} />
    </main>
  );
}

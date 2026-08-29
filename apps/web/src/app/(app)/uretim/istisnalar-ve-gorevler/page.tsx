import Link from "next/link";
import { ClipboardList } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ProductionFollowUpWorkspaceView } from "@/features/production-execution/components/production-follow-up-workspace-view";
import "@/features/quality-control/components/quality-workspace.css";
import { readProductionFollowUpWorkspace } from "@/features/production-execution/server/production-follow-up-reader";

type ProductionFollowUpPageProps = Readonly<{
  searchParams: Promise<Readonly<{ exception?: string }>>;
}>;

export default async function ProductionFollowUpPage({ searchParams }: ProductionFollowUpPageProps) {
  await forbidUnlessGranted("production.execution.read");

  const params = await searchParams;
  const result = await readProductionFollowUpWorkspace(params.exception);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <div>
            <p>ÜRETİM YÖNETİMİ</p>
            <h1>İstisnalar ve Görevler</h1>
          </div>
        </header>
        <div role="alert" className="state-panel error-state" data-testid="production-follow-up-server-error">
          <p>{result.message}</p>
        </div>
      </main>
    );
  }

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <div>
          <p>ÜRETİM YÖNETİMİ</p>
          <h1>İstisnalar ve Görevler</h1>
          <p className="production-execution-header__subtitle">
            Üretim istisnaları için insan incelemeli AI görev taslağı akışı.
          </p>
        </div>
        <div className="production-execution-header__actions">
          <Link href="/uretim" className="production-execution-header__link">
            <ClipboardList aria-hidden="true" />
            Üretim emirleri
          </Link>
        </div>
      </header>

      <ProductionFollowUpWorkspaceView
        workspace={result.workspace}
        initialException={result.exception}
        exceptionError={result.exceptionError}
      />
    </main>
  );
}

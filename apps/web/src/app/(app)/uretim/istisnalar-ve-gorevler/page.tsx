import Link from "next/link";
import { ClipboardList } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ProductionFollowUpWorkspaceView } from "@/features/production-execution/components/production-follow-up-workspace-view";
import "@/features/quality-control/components/quality-workspace.css";
import { readProductionFollowUpWorkspace } from "@/features/production-execution/server/production-follow-up-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

type ProductionFollowUpPageProps = Readonly<{
  searchParams: Promise<Readonly<{ exception?: string }>>;
}>;

export default async function ProductionFollowUpPage({ searchParams }: ProductionFollowUpPageProps) {
  await forbidUnlessGranted("production.execution.read");

  const params = await searchParams;
  const result = await readProductionFollowUpWorkspace(params.exception);

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <header className="production-execution-header">
          <div>
            <p>ÜRETİM</p>
            <h1>İstisnalar ve Görevler</h1>
          </div>
        </header>
        <div data-testid="production-follow-up-server-error"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline /></div>
      </div>
    );
  }

  return (
    <div className="production-execution-page">
      <header className="production-execution-header">
        <div>
          <p>ÜRETİM</p>
          <h1>İstisnalar ve Görevler</h1>
          <p className="production-execution-header__subtitle">
            Üretim istisnaları için insan onayıyla çalışan görev taslakları.
          </p>
        </div>
        <div className="production-execution-header__actions">
          <Link href="/uretim" className="production-execution-header__link">
            <ClipboardList aria-hidden="true" />
            Üretim Emirleri
          </Link>
        </div>
      </header>

      <ProductionFollowUpWorkspaceView
        workspace={result.workspace}
        initialException={result.exception}
        exceptionError={result.exceptionError}
      />
    </div>
  );
}

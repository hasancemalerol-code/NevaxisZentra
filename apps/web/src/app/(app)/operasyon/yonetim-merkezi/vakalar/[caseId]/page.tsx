import { ShieldAlert } from "lucide-react";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { FactoryCaseDetailView } from "@/features/factory-orchestration/components/factory-case-detail-view";
import { readFactoryCaseDetail } from "@/features/factory-orchestration/server/factory-orchestration-reader";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

export default async function FactoryCaseDetailPage({
  params,
}: Readonly<{
  params: Promise<{ caseId: string }>;
}>) {
  const authorization = await denyUnlessGranted("factory-case.read");
  if (authorization.status === "failure") {
    return (
      <main className="commint-page">
        <div role="alert" className="commint-error-banner" data-testid="factory-case-forbidden">
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Bu vakaya erişim yetkiniz yok</h3>
            <p>{authorization.message}</p>
          </div>
        </div>
      </main>
    );
  }

  const { caseId } = await params;
  const result = await readFactoryCaseDetail(caseId);
  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <div role="alert" className="commint-partial-error" data-testid="factory-case-not-found">
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Fabrika vakası yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <FactoryCaseDetailView detail={result.detail} />;
}

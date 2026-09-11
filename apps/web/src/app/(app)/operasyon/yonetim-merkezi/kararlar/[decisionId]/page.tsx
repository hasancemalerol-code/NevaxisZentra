import { ShieldAlert } from "lucide-react";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { FactoryDecisionDetailView } from "@/features/production-manager/components/factory-decision-detail-view";
import { readProductionManagerDecisionDetail } from "@/features/production-manager/server/production-manager-reader";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

export default async function FactoryDecisionDetailPage({
  params,
}: Readonly<{
  params: Promise<{ decisionId: string }>;
}>) {
  const authorization = await denyUnlessGranted("production-manager.read");
  if (authorization.status === "failure") {
    return (
      <main className="commint-page">
        <div role="alert" className="commint-error-banner" data-testid="factory-decision-forbidden">
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Bu karara erişim yetkiniz yok</h3>
            <p>{authorization.message}</p>
          </div>
        </div>
      </main>
    );
  }

  const { decisionId } = await params;
  const result = await readProductionManagerDecisionDetail(decisionId);
  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <div role="alert" className="commint-error-banner">
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Karar yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <FactoryDecisionDetailView decision={result.decision} />;
}

import { ShieldAlert } from "lucide-react";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { FactoryNetworkWorkspacePanel } from "@/features/factory-network/components/factory-network-workspace-panel";
import {
  FACTORY_NETWORK_SUBTITLE,
  FACTORY_NETWORK_TITLE,
} from "@/features/factory-network/domain/factory-network-labels";

export const dynamic = "force-dynamic";

export default async function FactoryNetworkPage() {
  const authorization = await denyUnlessGranted("factory-network.read");
  if (authorization.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">OPERASYON</p>
            <h1>{FACTORY_NETWORK_TITLE}</h1>
            <p className="commint-executive__subtitle">{FACTORY_NETWORK_SUBTITLE}</p>
          </div>
        </header>
        <div
          role="alert"
          className="commint-error-banner"
          data-testid="factory-network-forbidden"
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Bu merkeze erişim yetkiniz yok</h3>
            <p>{authorization.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="commint-page">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">OPERASYON</p>
          <h1>{FACTORY_NETWORK_TITLE}</h1>
          <p className="commint-executive__subtitle">{FACTORY_NETWORK_SUBTITLE}</p>
        </div>
      </header>
      <FactoryNetworkWorkspacePanel />
    </main>
  );
}

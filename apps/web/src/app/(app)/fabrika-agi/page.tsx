import Link from "next/link";

import { FailureStatePage } from "@/components/ui/failure-state";
import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { FactoryNetworkWorkspacePanel } from "@/features/factory-network/components/factory-network-workspace-panel";
import {
  FACTORY_NETWORK_SUBTITLE,
  FACTORY_NETWORK_TITLE,
} from "@/features/factory-network/domain/factory-network-labels";

import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import "@/features/factory-network/components/factory-network.css";

export const dynamic = "force-dynamic";

export default async function FactoryNetworkPage() {
  const authorization = await denyUnlessGranted("factory-network.read");
  if (authorization.status === "failure") {
    return (
      <div className="commint-page factory-network-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">PLAN</p>
            <h1>{FACTORY_NETWORK_TITLE}</h1>
            <p className="commint-executive__subtitle">{FACTORY_NETWORK_SUBTITLE}</p>
          </div>
        </header>
        <FailureStatePage
          code={authorization.code === "authorization-denied" ? "permission-denied" : "source-unavailable"}
          failedTitle="Yetki doğrulanamadı"
          backHref="/dashboard"
          backLabel="Ana sayfaya dön"
          inline
        />
      </div>
    );
  }

  return (
    <div className="commint-page factory-network-page">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">PLAN</p>
          <h1>{FACTORY_NETWORK_TITLE}</h1>
          <p className="commint-executive__subtitle">{FACTORY_NETWORK_SUBTITLE}</p>
        </div>
        <div className="commint-executive__actions">
          <Link className="button button-primary button-medium" href="/ayarlar">
            Tesis ve ağ ayarları
          </Link>
        </div>
      </header>
      <FactoryNetworkWorkspacePanel />
    </div>
  );
}

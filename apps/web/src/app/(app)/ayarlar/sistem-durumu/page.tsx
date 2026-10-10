import { ShieldAlert } from "lucide-react";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { SystemStatusWorkspacePanel } from "@/features/enterprise-reliability/components/system-status-workspace-panel";
import {
  ENTERPRISE_RELIABILITY_SUBTITLE,
  ENTERPRISE_RELIABILITY_TITLE,
} from "@/features/enterprise-reliability/domain/enterprise-reliability-labels";

export const dynamic = "force-dynamic";

export default async function SistemDurumuPage() {
  const authorization = await denyUnlessGranted("operability.read");
  if (authorization.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">YÖNETİM</p>
            <h1>{ENTERPRISE_RELIABILITY_TITLE}</h1>
            <p className="commint-executive__subtitle">{ENTERPRISE_RELIABILITY_SUBTITLE}</p>
          </div>
        </header>
        <div
          role="alert"
          className="commint-error-banner"
          data-testid="system-status-forbidden"
        >
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>{authorization.code === "authorization-denied" ? "Bu merkeze erişim yetkiniz yok" : "Yetki doğrulanamadı"}</h3>
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
          <p className="commint-executive__eyebrow">YÖNETİM</p>
          <h1>{ENTERPRISE_RELIABILITY_TITLE}</h1>
          <p className="commint-executive__subtitle">{ENTERPRISE_RELIABILITY_SUBTITLE}</p>
        </div>
      </header>
      <SystemStatusWorkspacePanel />
    </main>
  );
}

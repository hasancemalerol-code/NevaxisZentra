import { AuthorizationGateState } from "@/components/ui/failure-state";
import { PageHeader } from "@/components/ui/page";

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
      <div className="commint-page">
        <PageHeader eyebrow="Ayarlar" title={ENTERPRISE_RELIABILITY_TITLE} description={ENTERPRISE_RELIABILITY_SUBTITLE} />
        <AuthorizationGateState code={authorization.code} />
      </div>
    );
  }

  return (
    <div className="commint-page">
      <PageHeader eyebrow="Ayarlar" title={ENTERPRISE_RELIABILITY_TITLE} description={ENTERPRISE_RELIABILITY_SUBTITLE} />
      <SystemStatusWorkspacePanel />
    </div>
  );
}

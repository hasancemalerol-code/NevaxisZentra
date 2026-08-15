import { ShieldAlert } from "lucide-react";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { SaasAdminView } from "@/features/saas-admin/components/saas-admin-view";
import "@/features/saas-admin/components/saas-admin.css";
import { readSaasAdminWorkspace } from "@/features/saas-admin/server/saas-admin-queries";
import { readBillingWorkspaceForAdmin } from "@/features/saas-billing/server/saas-billing-queries";
import { fetchCurrentTenantSubscription } from "@/features/saas-subscription/server/saas-subscription-queries";

export const dynamic = "force-dynamic";

export default async function AyarlarPage() {
  const access = await denyUnlessGranted("admin-access.read");
  if (access.status === "failure") {
    return (
      <main className="saas-admin-page">
        <h1>Erişim reddedildi</h1>
        <p>Bu sayfayı görüntüleme yetkiniz yok.</p>
      </main>
    );
  }

  const [result, subscriptionResult, billingResult] = await Promise.all([
    readSaasAdminWorkspace(),
    fetchCurrentTenantSubscription(),
    readBillingWorkspaceForAdmin(),
  ]);
  if (result.status === "failure") {
    return (
      <main className="saas-admin-page">
        <header className="saas-admin-executive">
          <p className="saas-admin-executive__eyebrow">YÖNETİM VE GÜVENLİK</p>
          <h1>Ayarlar</h1>
        </header>
        <div className="saas-admin-alert" role="alert">
          <ShieldAlert aria-hidden="true" /> {result.message}
        </div>
      </main>
    );
  }

  return (
    <SaasAdminView
      summary={result.summary}
      members={result.members}
      invitations={result.invitations}
      roles={result.roles}
      permissions={result.permissions}
      facilities={result.facilities}
      sessions={result.sessions}
      auditEvents={result.auditEvents}
      commercialCatalog={result.commercialCatalog}
      subscription={subscriptionResult.status === "success" ? subscriptionResult.value : null}
      billingWorkspace={billingResult.status === "success" ? billingResult.workspace : null}
      billingProvider={billingResult.status === "success" ? billingResult.provider : null}
      billingLoadError={billingResult.status === "failure" ? billingResult.message : null}
      capabilities={result.capabilities}
      partialErrors={[
        ...result.partialErrors,
        ...(billingResult.status === "success" ? billingResult.partialErrors : []),
      ]}
    />
  );
}

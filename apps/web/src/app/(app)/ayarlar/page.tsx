

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { SaasAdminView } from "@/features/saas-admin/components/saas-admin-view";
import "@/features/saas-admin/components/saas-admin.css";
import { readSaasAdminWorkspace } from "@/features/saas-admin/server/saas-admin-queries";
import { readBillingWorkspaceForAdmin } from "@/features/saas-billing/server/saas-billing-queries";
import { fetchCurrentTenantSubscription } from "@/features/saas-subscription/server/saas-subscription-queries";

import { FailureStatePage } from "@/components/ui/failure-state";

export const dynamic = "force-dynamic";

export default async function AyarlarPage() {
  const access = await denyUnlessGranted("admin-access.read");
  if (access.status === "failure") {
    return (
      <div className="saas-admin-page">
        <h1>{access.code === "authorization-denied" ? "Erişim reddedildi" : "Yetki doğrulanamadı"}</h1>
        <p>{access.code === "authorization-denied" ? "Bu sayfayı görüntüleme yetkiniz yok." : "Yetki doğrulaması şu anda yapılamıyor."}</p>
      </div>
    );
  }

  const [result, subscriptionResult, billingResult] = await Promise.all([
    readSaasAdminWorkspace(),
    fetchCurrentTenantSubscription(),
    readBillingWorkspaceForAdmin(),
  ]);
  if (result.status === "failure") {
    return (
      <div className="saas-admin-page">
        <header className="saas-admin-executive">
          <p className="saas-admin-executive__eyebrow">AYARLAR</p>
          <h1>Ayarlar</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline />
      </div>
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

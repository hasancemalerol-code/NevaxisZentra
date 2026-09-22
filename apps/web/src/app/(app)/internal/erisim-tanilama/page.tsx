import { notFound } from "next/navigation";

import { fetchCurrentEffectiveAccess } from "@/features/admin-access/server/admin-access-effective-access";
import { fetchCurrentTenantSubscription } from "@/features/saas-subscription/server/saas-subscription-queries";
import {
  acceptanceRoleContract,
  routeAccessForPermissions,
} from "@/lib/authorization/acceptance-role-contract";
import { appsCatalogItems } from "@/lib/navigation/apps-catalog-navigation";
import { resolvePermissionAwareNavigation } from "@/lib/navigation/navigation-visibility";

export const dynamic = "force-dynamic";

export default async function AcceptanceRoleDiagnosticsPage() {
  if (process.env.NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE !== "development-api") {
    notFound();
  }

  const [access, subscription, navigation] = await Promise.all([
    fetchCurrentEffectiveAccess(),
    fetchCurrentTenantSubscription(),
    resolvePermissionAwareNavigation("/fabrika"),
  ]);
  const granted = access?.grantedPermissionKeys ?? [];
  const missing = acceptanceRoleContract.requiredPermissionKeys.filter((key) => !granted.includes(key));
  const unexpectedForbidden = acceptanceRoleContract.forbiddenPermissionKeys.filter((key) => granted.includes(key));
  const deniedRoutes = acceptanceRoleContract.routes.filter(
    (route) => routeAccessForPermissions(route.id, granted) === "deny",
  );
  const entitledModules = subscription.status === "success"
    ? subscription.value.entitlements
        .filter((entry) => entry.code.startsWith("module.") && entry.booleanValue === true)
        .map((entry) => entry.code)
    : [];
  const catalogMatches = access?.permissionCatalogVersion === acceptanceRoleContract.catalogVersion;

  return (
    <main className="fact-workspace" data-testid="acceptance-role-diagnostics">
      <h1>Erişim tanılama</h1>
      <p>Geliştirme tanısı. Oturum belirteci, parola veya bağlantı bilgisi gösterilmez.</p>
      <dl>
        <dt>Üye</dt>
        <dd>{access?.memberReference ?? "oturum yok"}</dd>
        <dt>Roller</dt>
        <dd>{(access?.roleReferences ?? []).join(", ") || "yok"}</dd>
        <dt>Yetki katalog sürümü</dt>
        <dd>
          oturum {access?.permissionCatalogVersion ?? "yok"} / kod {acceptanceRoleContract.catalogVersion}
          {catalogMatches ? " (eşleşiyor)" : " (bayat katalog)"}
        </dd>
        <dt>Oturum yetki sürümü</dt>
        <dd>{access?.authorizationVersion ?? "yok"}</dd>
        <dt>Eksik kabul yetkileri</dt>
        <dd>{missing.join(", ") || "yok"}</dd>
        <dt>Yasak ama verilmiş yetkiler</dt>
        <dd>{unexpectedForbidden.join(", ") || "yok"}</dd>
        <dt>Reddedilen sözleşmeli rotalar</dt>
        <dd>{deniedRoutes.map((route) => route.label).join(", ") || "yok"}</dd>
        <dt>Modül yetkileri</dt>
        <dd>{entitledModules.join(", ") || "yok"}</dd>
        <dt>Görünen gezinme</dt>
        <dd>{navigation.flatMap((group) => group.items).map((item) => item.label).join(", ") || "yok"}</dd>
        <dt>Uygulama kataloğu</dt>
        <dd>{appsCatalogItems.map((item) => item.href).join(", ")}</dd>
      </dl>
    </main>
  );
}

import type { ReactNode } from "react";
import { unstable_noStore as noStore } from "next/cache";
import { redirect } from "next/navigation";

import { AppShellFrame } from "@/components/app-shell/app-shell-frame";
import { NotificationsControlServer } from "@/components/app-shell/notifications-control-server";
import { fetchEffectiveAccessCapabilities, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { resolveFactoryCopilotAccess } from "@/features/factory-copilot/server/resolve-factory-copilot-access";
import { FactoryShowcaseShell } from "@/features/dashboard-showcase/components/factory-showcase-shell";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";
import { SubscriptionRecoveryShell } from "@/features/saas-subscription/components/subscription-recovery-shell";
import { normalizeSubscriptionRoute } from "@/features/saas-subscription/domain/saas-subscription-labels";
import {
  evaluateSubscriptionAccess,
  resolveRequestPathname,
  resolveSubscriptionAccessRedirect,
  resolveSubscriptionGraceBanner,
} from "@/features/saas-subscription/server/require-subscription-access";
import {
  buildShellSearchItems,
  resolvePermissionAwareAppsCatalog,
  resolvePermissionAwareNavigation,
  resolveSettingsHref,
} from "@/lib/navigation/navigation-visibility";
import { displayAccountName, displayTenantName } from "@/lib/presentation/display-name";

export const dynamic = "force-dynamic";

function isClosureDevelopmentApiRuntime(): boolean {
  return (
    process.env.NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE === "development-api" &&
    Boolean(process.env.NEVAXIS_FACTOS_DEV_AUTH_ISSUER?.trim()) &&
    Boolean(process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY?.trim())
  );
}

export default async function ApplicationLayout({ children }: Readonly<{ children: ReactNode }>) {
  noStore();
  const closureDevRuntime = isClosureDevelopmentApiRuntime();
  const session = await resolveSaasIdentitySession();

  if (!closureDevRuntime) {
    if (session.status === "mfa_pending") {
      redirect("/mfa-dogrula");
    }

    if (session.status === "session_expired") {
      redirect("/giris?reason=session-expired");
    }
  }

  const pathname = await resolveRequestPathname();
  const subscriptionAccess = closureDevRuntime
    ? {
        allowed: true,
        graceWarning: false,
        denialReason: null,
        subscription: null,
        failureKind: null,
      }
    : await evaluateSubscriptionAccess(pathname);
  if (!subscriptionAccess.allowed) {
    redirect(resolveSubscriptionAccessRedirect(subscriptionAccess));
  }

  const rawAccountDisplayName =
    session.status === "authenticated"
      ? session.account.displayName
      : closureDevRuntime
        ? process.env.NEVAXIS_FACTOS_DEV_AUTH_SUBJECT ?? "closure-dev-user"
        : null;
  const emailMasked =
    session.status === "authenticated" ? session.account.emailMasked : null;
  const rawOrganizationLabel =
    session.status === "authenticated"
      ? session.account.organizationLabel
      : closureDevRuntime
        ? process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY ?? null
        : null;

  const accountDisplayName = rawAccountDisplayName ? displayAccountName(rawAccountDisplayName) : rawAccountDisplayName;
  const organizationLabel = rawOrganizationLabel ? displayTenantName(rawOrganizationLabel) : rawOrganizationLabel;

  if (normalizeSubscriptionRoute(pathname) === "/abonelik-erisimi") {
    const access = await fetchEffectiveAccessCapabilities();
    return (
      <SubscriptionRecoveryShell
        accountDisplayName={accountDisplayName}
        emailMasked={emailMasked}
        organizationLabel={organizationLabel}
        settingsHref={hasGrantedPermission(access, "admin-access.read") ? "/ayarlar" : null}
      >
        {children}
      </SubscriptionRecoveryShell>
    );
  }

  if (pathname === "/fabrika-vitrini") {
    return <FactoryShowcaseShell>{children}</FactoryShowcaseShell>;
  }

  const [navigation, appsCatalog, canAccessFactoryCopilot] = await Promise.all([
    resolvePermissionAwareNavigation(pathname),
    resolvePermissionAwareAppsCatalog(),
    resolveFactoryCopilotAccess(),
  ]);
  const searchNavigation = [
    ...navigation,
    ...appsCatalog.map((group) => ({ ...group, label: `Uygulamalar · ${group.label}` })),
  ];

  return (
    <AppShellFrame
      accountDisplayName={accountDisplayName}
      emailMasked={emailMasked}
      navigation={navigation}
      notificationsSlot={<NotificationsControlServer />}
      organizationLabel={organizationLabel}
      searchItems={buildShellSearchItems(searchNavigation)}
      settingsHref={resolveSettingsHref([...navigation, ...appsCatalog])}
      subscriptionGraceWarning={resolveSubscriptionGraceBanner(subscriptionAccess)}
      canAccessFactoryCopilot={canAccessFactoryCopilot}
    >
      {children}
    </AppShellFrame>
  );
}

import type { ReactNode } from "react";
import { redirect } from "next/navigation";

import { AppShellFrame } from "@/components/app-shell/app-shell-frame";
import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { FactoryShowcaseShell } from "@/features/dashboard-showcase/components/factory-showcase-shell";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";
import { SubscriptionRecoveryShell } from "@/features/saas-subscription/components/subscription-recovery-shell";
import { normalizeSubscriptionRoute } from "@/features/saas-subscription/domain/saas-subscription-labels";
import {
  evaluateSubscriptionAccess,
  resolveRequestPathname,
  resolveSubscriptionGraceBanner,
} from "@/features/saas-subscription/server/require-subscription-access";
import {
  buildShellSearchItems,
  resolvePermissionAwareNavigation,
  resolveSettingsHref,
} from "@/lib/navigation/navigation-visibility";

export default async function ApplicationLayout({ children }: Readonly<{ children: ReactNode }>) {
  const session = await resolveSaasIdentitySession();

  if (session.status === "mfa_pending") {
    redirect("/mfa-dogrula");
  }

  const pathname = await resolveRequestPathname();
  const subscriptionAccess = await evaluateSubscriptionAccess(pathname);
  if (!subscriptionAccess.allowed) {
    const reason = subscriptionAccess.denialReason ?? "paid-application";
    redirect(`/abonelik-erisimi?reason=${reason}`);
  }

  const accountDisplayName =
    session.status === "authenticated" ? session.account.displayName : null;
  const emailMasked =
    session.status === "authenticated" ? session.account.emailMasked : null;
  const organizationLabel =
    session.status === "authenticated" ? session.account.organizationLabel : null;

  if (normalizeSubscriptionRoute(pathname) === "/abonelik-erisimi") {
    const access = await fetchCurrentEffectiveAccess();
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

  const navigation = await resolvePermissionAwareNavigation();
  return (
    <AppShellFrame
      accountDisplayName={accountDisplayName}
      emailMasked={emailMasked}
      navigation={navigation}
      organizationLabel={organizationLabel}
      searchItems={buildShellSearchItems(navigation)}
      settingsHref={resolveSettingsHref(navigation)}
      subscriptionGraceWarning={resolveSubscriptionGraceBanner(subscriptionAccess)}
    >
      {children}
    </AppShellFrame>
  );
}

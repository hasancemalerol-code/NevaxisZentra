import type { ReactNode } from "react";
import { redirect } from "next/navigation";

import { AppShellFrame } from "@/components/app-shell/app-shell-frame";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";
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

  const navigation = await resolvePermissionAwareNavigation();
  const accountDisplayName =
    session.status === "authenticated" ? session.account.displayName : null;
  const emailMasked =
    session.status === "authenticated" ? session.account.emailMasked : null;
  const organizationLabel =
    session.status === "authenticated" ? session.account.organizationLabel : null;

  return (
    <AppShellFrame
      accountDisplayName={accountDisplayName}
      emailMasked={emailMasked}
      navigation={navigation}
      organizationLabel={organizationLabel}
      searchItems={buildShellSearchItems(navigation)}
      settingsHref={resolveSettingsHref(navigation)}
    >
      {children}
    </AppShellFrame>
  );
}

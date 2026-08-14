import type { ReactNode } from "react";
import { redirect } from "next/navigation";

import { AppShellFrame } from "@/components/app-shell/app-shell-frame";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";
import { resolvePermissionAwareNavigation } from "@/lib/navigation/navigation-visibility";

export default async function ApplicationLayout({ children }: Readonly<{ children: ReactNode }>) {
  const session = await resolveSaasIdentitySession();

  if (session.status === "mfa_pending") {
    redirect("/mfa-dogrula");
  }

  const navigation = await resolvePermissionAwareNavigation();
  const accountDisplayName =
    session.status === "authenticated" ? session.account.displayName : null;

  return (
    <AppShellFrame
      accountDisplayName={accountDisplayName}
      navigation={navigation}
    >
      {children}
    </AppShellFrame>
  );
}

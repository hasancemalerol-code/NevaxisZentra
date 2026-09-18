import { redirect } from "next/navigation";

import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { MfaExitActions } from "@/features/saas-identity/components/mfa-exit-actions";
import { MfaVerifyForm } from "@/features/saas-identity/components/mfa-verify-form";
import { SAAS_IDENTITY_MESSAGES } from "@/features/saas-identity/presentation/saas-identity-messages";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";
import { resolvePostAuthLandingPath } from "@/lib/navigation/resolve-post-auth-landing";

type MfaVerifyPageProps = Readonly<{
  searchParams: Promise<Readonly<{ cikis?: string }>>;
}>;

export default async function MfaVerifyPage({ searchParams }: MfaVerifyPageProps) {
  const session = await resolveSaasIdentitySession();
  const params = await searchParams;

  if (session.status === "unauthenticated") {
    redirect("/giris");
  }

  if (session.status === "authenticated") {
    redirect(await resolvePostAuthLandingPath());
  }

  if (session.status === "session_expired") {
    redirect("/giris");
  }

  const displayName = session.displayName;

  return (
    <AuthShell
      description={
        displayName
          ? `${displayName}, kimlik doğrulama uygulamanızdaki kodu girin.`
          : "Kimlik doğrulama uygulamanızdaki kodu girin."
      }
      title={SAAS_IDENTITY_MESSAGES.mfaTitle}
    >
      <MfaVerifyForm />
      <MfaExitActions
        initialError={
          params.cikis === "hata" ? SAAS_IDENTITY_MESSAGES.mfaExitFailed : null
        }
      />
    </AuthShell>
  );
}

import { redirect } from "next/navigation";

import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { MfaVerifyForm } from "@/features/saas-identity/components/mfa-verify-form";
import { SAAS_IDENTITY_MESSAGES } from "@/features/saas-identity/presentation/saas-identity-messages";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";

export default async function MfaVerifyPage() {
  const session = await resolveSaasIdentitySession();

  if (session.status === "unauthenticated") {
    redirect("/giris");
  }

  if (session.status === "authenticated") {
    redirect("/dashboard");
  }

  return (
    <AuthShell
      description={
        session.displayName
          ? `${session.displayName}, kimlik doğrulama uygulamanızdaki kodu girin.`
          : "Kimlik doğrulama uygulamanızdaki kodu girin."
      }
      title={SAAS_IDENTITY_MESSAGES.mfaTitle}
    >
      <MfaVerifyForm />
    </AuthShell>
  );
}

import { redirect } from "next/navigation";

import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { SignInForm } from "@/features/saas-identity/components/sign-in-form";
import { SAAS_IDENTITY_MESSAGES } from "@/features/saas-identity/presentation/saas-identity-messages";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";

export default async function SignInPage() {
  const session = await resolveSaasIdentitySession();
  if (session.status === "mfa_pending") {
    redirect("/mfa-dogrula");
  }

  return (
    <AuthShell
      description="Kuruluş kodunuz, e-posta adresiniz ve parolanızla oturum açın."
      title={SAAS_IDENTITY_MESSAGES.signInTitle}
    >
      <SignInForm />
    </AuthShell>
  );
}
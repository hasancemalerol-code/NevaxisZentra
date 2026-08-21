import { redirect } from "next/navigation";

import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { SignInForm } from "@/features/saas-identity/components/sign-in-form";
import {
  buildCleanSignInPath,
  hasSensitiveSignInQueryKeys,
  isSafeAppReturnUrl,
} from "@/features/saas-identity/lib/saas-identity-return-url";
import { SAAS_IDENTITY_MESSAGES } from "@/features/saas-identity/presentation/saas-identity-messages";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";

type SignInPageProps = Readonly<{
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}>;

function firstQueryValue(
  value: string | string[] | undefined,
): string | undefined {
  if (Array.isArray(value)) {
    return value[0];
  }

  return value;
}

export default async function SignInPage({ searchParams }: SignInPageProps) {
  const params = await searchParams;
  const queryKeys = Object.keys(params);
  const returnUrl = firstQueryValue(params.returnUrl);

  if (hasSensitiveSignInQueryKeys(queryKeys)) {
    redirect(buildCleanSignInPath(returnUrl));
  }

  const session = await resolveSaasIdentitySession();
  if (session.status === "mfa_pending") {
    redirect("/mfa-dogrula");
  }

  const authDenied = firstQueryValue(params.auth) === "denied";
  const sessionExpired = firstQueryValue(params.reason) === "session-expired";

  return (
    <AuthShell
      description="Kuruluş kodunuz, e-posta adresiniz ve parolanızla oturum açın."
      title={SAAS_IDENTITY_MESSAGES.signInTitle}
    >
      <SignInForm
        initialError={
          authDenied ? SAAS_IDENTITY_MESSAGES.genericInvalidCredentials : null
        }
        initialNotice={
          sessionExpired ? SAAS_IDENTITY_MESSAGES.sessionExpired : null
        }
        returnUrl={isSafeAppReturnUrl(returnUrl) ? returnUrl : null}
      />
    </AuthShell>
  );
}

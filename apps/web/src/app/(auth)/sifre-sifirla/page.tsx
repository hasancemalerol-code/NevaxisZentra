import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { ResetPasswordForm } from "@/features/saas-identity/components/reset-password-form";
import { SAAS_IDENTITY_MESSAGES } from "@/features/saas-identity/presentation/saas-identity-messages";

type ResetPasswordPageProps = Readonly<{
  searchParams: Promise<Readonly<{ token?: string | string[] }>>;
}>;

export default async function ResetPasswordPage({
  searchParams,
}: ResetPasswordPageProps) {
  const params = await searchParams;
  const rawToken = params.token;
  const token =
    typeof rawToken === "string"
      ? rawToken
      : Array.isArray(rawToken)
        ? rawToken[0] ?? null
        : null;

  return (
    <AuthShell
      description="Yeni parolanız en az 12 karakter olmalıdır."
      title={SAAS_IDENTITY_MESSAGES.resetPasswordTitle}
    >
      <ResetPasswordForm token={token} />
    </AuthShell>
  );
}

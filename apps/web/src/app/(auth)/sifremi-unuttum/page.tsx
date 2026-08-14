import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { ForgotPasswordForm } from "@/features/saas-identity/components/forgot-password-form";
import { SAAS_IDENTITY_MESSAGES } from "@/features/saas-identity/presentation/saas-identity-messages";

export default function ForgotPasswordPage() {
  return (
    <AuthShell
      description="Kayıtlı e-posta adresinize parola sıfırlama bağlantısı gönderilir."
      title={SAAS_IDENTITY_MESSAGES.forgotPasswordTitle}
    >
      <ForgotPasswordForm />
    </AuthShell>
  );
}

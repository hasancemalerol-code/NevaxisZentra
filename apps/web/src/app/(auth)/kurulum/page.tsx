import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { FirstRunEnrollmentForm } from "@/features/saas-identity/components/first-run-enrollment-form";

export const dynamic = "force-dynamic";

export default function FirstRunSetupPage() {
  return (
    <AuthShell
      description="Bu cihazdaki ilk kurum hesabını ve yönetici kullanıcıyı oluşturun."
      title="İlk kurulum"
    >
      <FirstRunEnrollmentForm />
    </AuthShell>
  );
}

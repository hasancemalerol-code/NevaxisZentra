import { AuthShell } from "@/features/saas-identity/components/auth-shell";
import { InvitationAcceptForm } from "@/features/saas-identity/components/invitation-accept-form";
import { SAAS_IDENTITY_MESSAGES } from "@/features/saas-identity/presentation/saas-identity-messages";

type InvitationAcceptPageProps = Readonly<{
  searchParams: Promise<Readonly<{ token?: string | string[] }>>;
}>;

export default async function InvitationAcceptPage({
  searchParams,
}: InvitationAcceptPageProps) {
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
      description="Hesabınızı oluşturmak için görünen adınızı ve parolanızı belirleyin."
      title={SAAS_IDENTITY_MESSAGES.invitationTitle}
    >
      <InvitationAcceptForm token={token} />
    </AuthShell>
  );
}

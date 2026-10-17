import { StatePage, StatePageLink } from "@/components/ui/state-page";

export default function ForbiddenPage() {
  return (
    <StatePage
      standalone
      kind="forbidden"
      code="403"
      title="Bu sayfaya erişim yetkiniz yok"
      description="Hesabınızın rolü bu alanı görüntülemeye izin vermiyor. Erişim gerekiyorsa yöneticinizle iletişime geçin."
      actions={<StatePageLink href="/" primary>Ana ekrana dön</StatePageLink>}
    />
  );
}

import { StatePage, StatePageLink } from "@/components/ui/state-page";

export default function RootNotFound() {
  return (
    <StatePage
      standalone
      kind="not-found"
      code="404"
      title="Sayfa bulunamadı"
      description="Aradığınız sayfa mevcut değil ya da taşınmış olabilir. Adresi kontrol edin veya ana ekrana dönün."
      actions={<StatePageLink href="/" primary>Ana ekrana dön</StatePageLink>}
    />
  );
}

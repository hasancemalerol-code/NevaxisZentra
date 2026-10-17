import { PageContent, PageHeader } from "@/components/ui/page";
import { StatePage, StatePageLink } from "@/components/ui/state-page";

/** Teams not-found: keeps the Teams tab bar (rendered by the layout) and the same header offset as list pages. */
export default function TeamsNotFound() {
  return (
    <PageContent>
      <PageHeader
        eyebrow="Operasyon iletişimi"
        title="Operasyon İletişimi"
      />
      <StatePage
        inline
        kind="not-found"
        title="Kayıt Bulunamadı"
        description="Aradığınız kayıt mevcut değil, kaldırılmış ya da yetki alanınızın dışında olabilir."
        actions={
          <StatePageLink href="/teams" primary>
            Operasyon iletişimine dön
          </StatePageLink>
        }
      />
    </PageContent>
  );
}

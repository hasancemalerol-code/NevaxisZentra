import { PageContent, PageHeader } from "@/components/ui/page";
import { StatePage, StatePageLink } from "@/components/ui/state-page";

/** Sipariş taslağı bulunamadı: aynı sayfa başlığı konumu ve tek net dönüş yolu. */
export default function OrdersNotFound() {
  return (
    <PageContent>
      <PageHeader
        eyebrow="Çok kanallı sipariş yönetimi"
        title="Siparişler"
      />
      <StatePage
        inline
        kind="not-found"
        title="Kayıt Bulunamadı"
        description="Aradığınız kayıt mevcut değil, kaldırılmış ya da yetki alanınızın dışında olabilir."
        actions={
          <StatePageLink href="/siparisler" primary>
            Siparişlere dön
          </StatePageLink>
        }
      />
    </PageContent>
  );
}
